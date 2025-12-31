# AESIR Legal AI Validation Tool

A comprehensive testing framework for evaluating AESIR agents on legal questions. The tool runs a bank of 45+ legal questions against agents, uses Claude to evaluate responses, and generates actionable prompt improvement recommendations.

## Features

- **Multi-Agent Testing**: Test multiple AESIR agents simultaneously
- **Intelligent Evaluation**: Uses Claude API to score responses on 5 dimensions
- **45+ Legal Questions**: Organized across 11 specialized categories
- **Custom Prompt Testing**: Override agent prompts to A/B test different system prompts
- **Three Output Formats**: JSON (structured data), Markdown (human-readable), Claude Code instructions
- **Concurrent Processing**: Configurable parallelism for efficient testing

## Prerequisites

- .NET 10.0 SDK
- Running AESIR API server (default: https://aesir.localhost)
- Anthropic API key for Claude evaluation

## Build

```bash
# From solution root
dotnet build Tools/Aesir.Tools.LegalValidator/Aesir.Tools.LegalValidator.csproj

# Or build entire solution
dotnet build Aesir.sln
```

## Usage

### Basic Usage

```bash
# Set your Anthropic API key
export ANTHROPIC_API_KEY="your-api-key"

# Test all agents with default questions
dotnet run --project Tools/Aesir.Tools.LegalValidator -- --agents all

# Or run the compiled executable directly
./Tools/Aesir.Tools.LegalValidator/bin/Debug/net10.0/Aesir.Tools.LegalValidator --agents all
```

### CLI Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--agents` | `-a` | Agent IDs to test (comma-separated or 'all') | - |
| `--agent-name` | `-n` | Filter agents by name pattern (supports `*` and `?`) | - |
| `--categories` | `-c` | Question categories to include (comma-separated) | all |
| `--questions` | `-q` | Path to custom questions JSON file | embedded |
| `--system-prompt` | `-p` | Path to custom system prompt file | agent default |
| `--output-dir` | `-o` | Output directory for reports | `./validation_results` |
| `--concurrency` | - | Concurrent requests per agent | 3 |
| `--claude-api-key` | - | Anthropic API key | `$ANTHROPIC_API_KEY` |
| `--aesir-url` | - | AESIR API base URL | `https://aesir.localhost` |
| `--format` | `-f` | Output formats: json, markdown, claude-code, all | all |
| `--dry-run` | - | List what would be tested without running | false |
| `--verbose` | `-v` | Enable verbose logging | false |

### Examples

```bash
# Test specific agents by ID
dotnet run --project Tools/Aesir.Tools.LegalValidator -- \
  --agents "12345678-1234-1234-1234-123456789abc,87654321-4321-4321-4321-cba987654321"

# Test agents matching a name pattern
dotnet run --project Tools/Aesir.Tools.LegalValidator -- \
  --agent-name "*Legal*"

# Test with specific question categories
dotnet run --project Tools/Aesir.Tools.LegalValidator -- \
  --agents all \
  --categories Contracts,Torts,Criminal

# Test with a custom system prompt (A/B testing)
dotnet run --project Tools/Aesir.Tools.LegalValidator -- \
  --agents all \
  --system-prompt ./my_legal_prompt.txt

# Dry run to see what would be tested
dotnet run --project Tools/Aesir.Tools.LegalValidator -- \
  --agents all \
  --dry-run

# Custom output directory and format
dotnet run --project Tools/Aesir.Tools.LegalValidator -- \
  --agents all \
  --output-dir ./results \
  --format markdown,json

# Full example with all options
dotnet run --project Tools/Aesir.Tools.LegalValidator -- \
  --agents all \
  --agent-name "*Assistant*" \
  --categories FactualDefinitional,Procedural \
  --system-prompt ./legal_prompt.txt \
  --output-dir ./validation_results \
  --concurrency 5 \
  --aesir-url https://aesir.localhost \
  --format all \
  --verbose
```

## Question Categories

The tool includes 45+ legal questions across 11 categories:

| Category | Count | Description |
|----------|-------|-------------|
| FactualDefinitional | 8 | Legal definitions and claim elements |
| Procedural | 6 | Court processes and filing procedures |
| Analytical | 5 | Fact pattern analysis |
| JurisdictionSpecific | 4 | CA, DE, NY, TX specific rules |
| NuancedEdgeCase | 4 | Complex scenarios with exceptions |
| EthicsProfessional | 3 | Attorney obligations |
| Contracts | 3 | Contract law specifics |
| Torts | 3 | Tort law specifics |
| Criminal | 3 | Criminal law basics |
| Constitutional | 2 | Constitutional analysis |
| Corporate | 3 | Business law and bankruptcy |

## Evaluation Metrics

Each response is evaluated by Claude on 5 dimensions:

| Metric | Range | Description |
|--------|-------|-------------|
| Accuracy | 1-10 | Legal correctness and citation accuracy |
| Completeness | 1-10 | Coverage of expected elements |
| Clarity | 1-10 | Organization and readability |
| Source Quality | 1-10 | Authority and relevance of citations |
| Disclaimer Present | Yes/No | Appropriate legal disclaimers included |

### Grade Scale

| Grade | GPA | Description |
|-------|-----|-------------|
| A+/A/A- | 4.0-4.3 | Excellent - accurate, complete, well-sourced |
| B+/B/B- | 3.0-3.3 | Good - mostly accurate, minor issues |
| C+/C/C- | 2.0-2.3 | Satisfactory - significant gaps |
| D | 1.0 | Poor - major errors |
| F | 0.0 | Failing - fundamentally incorrect |

## Output Files

The tool generates three report formats in the output directory:

### 1. JSON Report (`validation_report_TIMESTAMP.json`)

Complete structured data for programmatic access:
- All raw responses
- Evaluation scores
- Agent summaries
- Prompt adjustments

### 2. Markdown Report (`validation_report_TIMESTAMP.md`)

Human-readable report with:
- Executive summary with comparison tables
- Grade distributions per agent
- Detailed results by question
- Recommended prompt adjustments

### 3. Claude Code Instructions (`claude_code_instructions_TIMESTAMP.md`)

Actionable instructions for improving system prompts:
- Prioritized by impact (critical, high, medium, low)
- Exact text to add to prompts
- Location guidance
- Verification criteria

## Custom Questions

You can provide your own questions via a JSON file:

```json
[
  {
    "id": "CUSTOM001",
    "category": "Contracts",
    "question": "What is consideration in contract law?",
    "expected_elements": [
      "Bargained-for exchange",
      "Legal value",
      "Past consideration insufficient"
    ],
    "difficulty": "Basic"
  }
]
```

Valid categories: `FactualDefinitional`, `Procedural`, `Analytical`, `JurisdictionSpecific`, `NuancedEdgeCase`, `EthicsProfessional`, `Contracts`, `Torts`, `Criminal`, `Constitutional`, `Corporate`

Valid difficulty levels: `Basic`, `Intermediate`, `Advanced`

## Configuration

Settings can be configured via `appsettings.json`:

```json
{
  "LegalValidator": {
    "AesirApi": {
      "BaseUrl": "https://aesir.localhost",
      "TimeoutSeconds": 120
    },
    "Claude": {
      "ApiKey": "",
      "Model": "claude-sonnet-4-20250514",
      "TimeoutSeconds": 60
    },
    "Validation": {
      "DefaultConcurrency": 3,
      "OutputDirectory": "./validation_results"
    },
    "Questions": {
      "UseEmbeddedQuestions": true,
      "ExternalQuestionsPath": null,
      "Categories": null
    }
  }
}
```

## Environment Variables

| Variable | Description |
|----------|-------------|
| `ANTHROPIC_API_KEY` | Anthropic API key for Claude evaluation |
| `AESIR_API_URL` | Override AESIR API base URL |

## Workflow for Iterative Improvement

1. **Run Validation**: Test your agent(s) against the question bank
2. **Review Report**: Analyze the Markdown report for patterns
3. **Apply Adjustments**: Use Claude Code instructions to update system prompts
4. **Re-test**: Run validation again to measure improvement
5. **Iterate**: Repeat until desired quality level is achieved

## Troubleshooting

### Connection Issues

If you get connection errors to the AESIR API:
- Ensure the AESIR server is running (`./run-server.sh`)
- Check the URL is correct (`--aesir-url`)
- For local development, the tool accepts self-signed certificates

### Claude API Errors

If evaluation fails:
- Verify your `ANTHROPIC_API_KEY` is set correctly
- Check your API key has sufficient quota
- Try reducing concurrency if rate limited

### No Agents Found

If `--agents all` returns no agents:
- Verify agents exist in the AESIR database
- Check the Configuration API is working: `curl https://aesir.localhost/configuration/agents`
