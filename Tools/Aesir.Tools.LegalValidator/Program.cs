using System.CommandLine;
using System.Net.Http.Headers;
using Aesir.Tools.LegalValidator.Configuration;
using Aesir.Tools.LegalValidator.Models;
using Aesir.Tools.LegalValidator.Orchestration;
using Aesir.Tools.LegalValidator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

namespace Aesir.Tools.LegalValidator;

public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        // Define CLI options
        var agentsOption = new Option<string?>(
            ["--agents", "-a"],
            "Comma-separated agent IDs to test, or 'all' for all agents");

        var agentNameOption = new Option<string?>(
            ["--agent-name", "-n"],
            "Filter agents by name pattern (supports * and ? wildcards)");

        var categoriesOption = new Option<string?>(
            ["--categories", "-c"],
            "Comma-separated question categories to include");

        var questionsOption = new Option<string?>(
            ["--questions", "-q"],
            "Path to custom questions JSON file");

        var systemPromptOption = new Option<string?>(
            ["--system-prompt", "-p"],
            "Path to custom system prompt file (overrides agent's prompt)");

        var outputDirOption = new Option<string>(
            ["--output-dir", "-o"],
            () => "./validation_results",
            "Output directory for reports");

        var concurrencyOption = new Option<int>(
            ["--concurrency"],
            () => 3,
            "Number of concurrent requests per agent");

        var claudeApiKeyOption = new Option<string?>(
            ["--claude-api-key"],
            "Anthropic API key (or use ANTHROPIC_API_KEY env var)");

        var aesirUrlOption = new Option<string>(
            ["--aesir-url"],
            () => "https://aesir.localhost",
            "AESIR API base URL");

        var formatOption = new Option<string>(
            ["--format", "-f"],
            () => "all",
            "Output formats: json, markdown, claude-code, or all");

        var dryRunOption = new Option<bool>(
            ["--dry-run"],
            "List what would be tested without running validation");

        var verboseOption = new Option<bool>(
            ["--verbose", "-v"],
            "Enable verbose logging");

        // Create root command
        var rootCommand = new RootCommand("AESIR Legal AI Validation Tool")
        {
            agentsOption,
            agentNameOption,
            categoriesOption,
            questionsOption,
            systemPromptOption,
            outputDirOption,
            concurrencyOption,
            claudeApiKeyOption,
            aesirUrlOption,
            formatOption,
            dryRunOption,
            verboseOption
        };

        rootCommand.SetHandler(async (context) =>
        {
            var agents = context.ParseResult.GetValueForOption(agentsOption);
            var agentName = context.ParseResult.GetValueForOption(agentNameOption);
            var categories = context.ParseResult.GetValueForOption(categoriesOption);
            var questionsPath = context.ParseResult.GetValueForOption(questionsOption);
            var systemPromptPath = context.ParseResult.GetValueForOption(systemPromptOption);
            var outputDir = context.ParseResult.GetValueForOption(outputDirOption)!;
            var concurrency = context.ParseResult.GetValueForOption(concurrencyOption);
            var claudeApiKey = context.ParseResult.GetValueForOption(claudeApiKeyOption);
            var aesirUrl = context.ParseResult.GetValueForOption(aesirUrlOption)!;
            var format = context.ParseResult.GetValueForOption(formatOption)!;
            var dryRun = context.ParseResult.GetValueForOption(dryRunOption);
            var verbose = context.ParseResult.GetValueForOption(verboseOption);

            var exitCode = await RunValidationAsync(
                agents, agentName, categories, questionsPath, systemPromptPath,
                outputDir, concurrency, claudeApiKey, aesirUrl, format, dryRun, verbose,
                context.GetCancellationToken());

            context.ExitCode = exitCode;
        });

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<int> RunValidationAsync(
        string? agents,
        string? agentName,
        string? categories,
        string? questionsPath,
        string? systemPromptPath,
        string outputDir,
        int concurrency,
        string? claudeApiKey,
        string aesirUrl,
        string format,
        bool dryRun,
        bool verbose,
        CancellationToken ct)
    {
        try
        {
            // Build host with DI
            var host = CreateHost(claudeApiKey, aesirUrl, outputDir, verbose);
            var services = host.Services;

            var logger = services.GetRequiredService<ILogger<ValidationOrchestrator>>();
            var orchestrator = services.GetRequiredService<IValidationOrchestrator>();
            var reportGenerator = services.GetRequiredService<IReportGenerator>();

            // Parse agent IDs
            IEnumerable<Guid>? agentIds = null;
            if (!string.IsNullOrEmpty(agents) && !agents.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                agentIds = agents.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => Guid.TryParse(s, out _))
                    .Select(Guid.Parse);
            }

            // Parse categories
            IEnumerable<QuestionCategory>? categoryFilter = null;
            if (!string.IsNullOrEmpty(categories))
            {
                categoryFilter = categories.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => Enum.TryParse<QuestionCategory>(s, true, out _))
                    .Select(s => Enum.Parse<QuestionCategory>(s, true));
            }

            // Load custom system prompt if provided
            string? customSystemPrompt = null;
            if (!string.IsNullOrEmpty(systemPromptPath))
            {
                if (!File.Exists(systemPromptPath))
                {
                    Console.WriteLine($"Error: System prompt file not found: {systemPromptPath}");
                    return 1;
                }

                customSystemPrompt = await File.ReadAllTextAsync(systemPromptPath, ct);
                logger.LogInformation("Loaded custom system prompt from {Path}", systemPromptPath);
            }

            // Run validation
            Console.WriteLine("Starting legal validation...");
            Console.WriteLine();

            var report = await orchestrator.RunValidationAsync(
                agentIds,
                agentName,
                categoryFilter,
                questionsPath,
                customSystemPrompt,
                concurrency,
                dryRun,
                ct);

            // Output results
            if (dryRun)
            {
                Console.WriteLine("=== DRY RUN ===");
                Console.WriteLine($"Would test {report.AgentsTested.Count} agents:");
                foreach (var name in report.AgentsTested)
                {
                    Console.WriteLine($"  - {name}");
                }

                Console.WriteLine();
                Console.WriteLine($"Would run {report.TotalQuestions} questions across categories:");
                foreach (var category in report.CategoriesTested)
                {
                    Console.WriteLine($"  - {category}");
                }

                return 0;
            }

            // Create output directory
            Directory.CreateDirectory(outputDir);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            // Generate and save reports
            var formats = format.ToLowerInvariant().Split(',').Select(f => f.Trim()).ToHashSet();
            var generateAll = formats.Contains("all");

            if (generateAll || formats.Contains("json"))
            {
                var jsonReport = await reportGenerator.GenerateJsonReportAsync(report, ct);
                var jsonPath = Path.Combine(outputDir, $"validation_report_{timestamp}.json");
                await File.WriteAllTextAsync(jsonPath, jsonReport, ct);
                Console.WriteLine($"JSON report saved: {jsonPath}");
            }

            if (generateAll || formats.Contains("markdown"))
            {
                var mdReport = await reportGenerator.GenerateMarkdownReportAsync(report, ct);
                var mdPath = Path.Combine(outputDir, $"validation_report_{timestamp}.md");
                await File.WriteAllTextAsync(mdPath, mdReport, ct);
                Console.WriteLine($"Markdown report saved: {mdPath}");
            }

            if (generateAll || formats.Contains("claude-code"))
            {
                var ccReport = await reportGenerator.GenerateClaudeCodeInstructionsAsync(report, ct);
                var ccPath = Path.Combine(outputDir, $"claude_code_instructions_{timestamp}.md");
                await File.WriteAllTextAsync(ccPath, ccReport, ct);
                Console.WriteLine($"Claude Code instructions saved: {ccPath}");
            }

            // Print summary
            Console.WriteLine();
            Console.WriteLine("=== VALIDATION SUMMARY ===");
            Console.WriteLine($"Duration: {report.ValidationDurationSeconds:F2} seconds");
            Console.WriteLine($"Questions tested: {report.TotalQuestions}");
            Console.WriteLine();

            foreach (var summary in report.AgentSummaries.Values.OrderByDescending(s => s.AverageGpa))
            {
                Console.WriteLine($"{summary.AgentName}:");
                Console.WriteLine($"  GPA: {summary.AverageGpa:F2}");
                Console.WriteLine($"  Accuracy: {summary.AverageAccuracy:F1}/10");
                Console.WriteLine($"  Completeness: {summary.AverageCompleteness:F1}/10");
                Console.WriteLine($"  Disclaimer Rate: {summary.DisclaimerRate:F1}%");

                if (summary.ErrorCount > 0)
                {
                    Console.WriteLine($"  Errors: {summary.ErrorCount}");
                }

                Console.WriteLine();
            }

            if (report.PromptAdjustments.Count > 0)
            {
                Console.WriteLine($"Prompt adjustments recommended: {report.PromptAdjustments.Count}");
            }

            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");

            if (verbose)
            {
                Console.WriteLine(ex.StackTrace);
            }

            return 1;
        }
    }

    private static IHost CreateHost(
        string? claudeApiKey,
        string aesirUrl,
        string outputDir,
        bool verbose)
    {
        // Get Claude API key from env if not provided
        claudeApiKey ??= Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");

        if (string.IsNullOrEmpty(claudeApiKey))
        {
            throw new InvalidOperationException(
                "Claude API key is required. Provide via --claude-api-key or ANTHROPIC_API_KEY environment variable.");
        }

        var builder = Host.CreateApplicationBuilder();

        // Configuration
        builder.Configuration.AddJsonFile("appsettings.json", optional: true);
        builder.Configuration.AddEnvironmentVariables();

        // Override config with CLI values
        builder.Configuration["LegalValidator:AesirApi:BaseUrl"] = aesirUrl;
        builder.Configuration["LegalValidator:Claude:ApiKey"] = claudeApiKey;
        builder.Configuration["LegalValidator:Validation:OutputDirectory"] = outputDir;

        // Logging
        builder.Logging.ClearProviders();
        builder.Logging.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
        builder.Logging.AddNLog();

        // Options
        builder.Services.Configure<LegalValidatorOptions>(
            builder.Configuration.GetSection(LegalValidatorOptions.SectionName));

        // HTTP Clients
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        builder.Services.AddHttpClient<IAesirApiClient, AesirApiClient>(client =>
            {
                client.BaseAddress = new Uri(aesirUrl);
                client.Timeout = TimeSpan.FromSeconds(120);
            })
            .AddPolicyHandler(retryPolicy)
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true // Dev cert
            });

        builder.Services.AddHttpClient<IClaudeEvaluator, ClaudeEvaluator>(client =>
            {
                client.BaseAddress = new Uri("https://api.anthropic.com");
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Add("x-api-key", claudeApiKey);
                client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
            })
            .AddPolicyHandler(retryPolicy);

        // Services
        builder.Services.AddSingleton<IQuestionLoader, QuestionLoader>();
        builder.Services.AddSingleton<IReportGenerator, ReportGenerator>();
        builder.Services.AddSingleton<IValidationOrchestrator, ValidationOrchestrator>();

        return builder.Build();
    }
}
