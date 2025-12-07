using Bunit;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Aesir.Client.Web.Modules.Wizard.Components;

namespace Aesir.Client.Web.Tests.Unit.Wizard.Components;

public class WizardWelcomeStepTests : TestContext
{
    public WizardWelcomeStepTests()
    {
        Services.AddMudServices(options =>
        {
            options.PopoverOptions.CheckForPopoverProvider = false;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void Renders_WelcomeMessage()
    {
        // Act
        var cut = RenderComponent<WizardWelcomeStep>();

        // Assert
        cut.Markup.Should().Contain("Welcome to AESIR");
    }

    [Fact]
    public void Renders_SetupDescription()
    {
        // Act
        var cut = RenderComponent<WizardWelcomeStep>();

        // Assert
        cut.Markup.Should().Contain("Let's get you set up!");
        cut.Markup.Should().Contain("This wizard will guide you through the initial configuration");
    }

    [Fact]
    public void Renders_ConfigurationItems()
    {
        // Act
        var cut = RenderComponent<WizardWelcomeStep>();

        // Assert
        cut.Markup.Should().Contain("Inference Engine");
        cut.Markup.Should().Contain("RAG Settings");
        cut.Markup.Should().Contain("AI Agent");
    }

    [Fact]
    public void Renders_GetStartedButton()
    {
        // Act
        var cut = RenderComponent<WizardWelcomeStep>();

        // Assert
        var buttons = cut.FindAll("button");
        buttons.Any(b => b.TextContent.Contains("Get Started")).Should().BeTrue();
    }

    [Fact]
    public async Task OnNext_Callback_IsCalled_WhenGetStartedClicked()
    {
        // Arrange
        var nextCalled = false;
        var cut = RenderComponent<WizardWelcomeStep>(parameters => parameters
            .Add(p => p.OnNext, () => { nextCalled = true; return Task.CompletedTask; }));

        // Act
        var button = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Get Started"));
        await cut.InvokeAsync(() => button!.Click());

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public void DoesNotHaveBackButton()
    {
        // Act
        var cut = RenderComponent<WizardWelcomeStep>();

        // Assert
        var buttons = cut.FindAll("button");
        buttons.Any(b => b.TextContent.Contains("Back")).Should().BeFalse();
    }
}
