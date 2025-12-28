using Aesir.Client.Web.Infrastructure.Services;
using Aesir.Client.Web.Modules.Research.Components;
using Aesir.Common.Models;
using Bunit;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;

namespace Aesir.Client.Web.Tests.Unit.Research.Components;

public class ExportMenuTests : TestContext
{
    public ExportMenuTests()
    {
        Services.AddMudServices();
        JSInterop.SetupVoid("mudPopover.initialize", _ => true);
        JSInterop.SetupVoid("mudKeyInterceptor.connect", _ => true);
        JSInterop.SetupVoid("mudPopover.connect", _ => true);
        JSInterop.SetupVoid("mudPopover.disconnect", _ => true);
        JSInterop.SetupVoid("mudScrollManager.unlockScroll", _ => true);
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void ExportMenu_RendersExportButton()
    {
        // Act
        var cut = RenderComponent<ExportMenu>();

        // Assert
        cut.Markup.Should().Contain("Export");
    }

    [Fact]
    public void ExportMenu_HasMudMenuComponent()
    {
        // Act
        var cut = RenderComponent<ExportMenu>();

        // Assert
        var menu = cut.FindComponent<MudMenu>();
        menu.Should().NotBeNull();
    }

    [Fact]
    public void ExportMenu_HasButtonWithIcon()
    {
        // Act
        var cut = RenderComponent<ExportMenu>();

        // Assert - check for button and icon elements (icon is SVG, not text)
        cut.Find("button").Should().NotBeNull();
        cut.Markup.Should().Contain("svg"); // MudBlazor renders icons as SVG
    }

    [Fact]
    public void ExportMenu_AcceptsReportParameter()
    {
        // Arrange
        var report = new ResearchReportBase
        {
            Id = Guid.NewGuid(),
            Title = "Test Report",
            FullMarkdown = "# Test Content"
        };

        // Act
        var cut = RenderComponent<ExportMenu>(parameters => parameters
            .Add(p => p.Report, report)
            .Add(p => p.ReportTitle, "Test Report"));

        // Assert
        cut.Instance.Report.Should().Be(report);
        cut.Instance.ReportTitle.Should().Be("Test Report");
    }

    [Fact]
    public void ExportMenu_HasMudMenuItems()
    {
        // Act
        var cut = RenderComponent<ExportMenu>();

        // Assert - the menu component should render (menu items may not be visible until activated)
        var menu = cut.FindComponent<MudMenu>();
        menu.Should().NotBeNull();
        // The menu has child content with Markdown, HTML, and Clipboard options
        // but these aren't rendered until the menu is opened
    }

    [Fact]
    public void ExportMenu_ExportFormatEnum_HasExpectedValues()
    {
        // Assert
        Enum.GetNames<ExportMenu.ExportFormat>().Should().Contain("Markdown");
        Enum.GetNames<ExportMenu.ExportFormat>().Should().Contain("Html");
        Enum.GetNames<ExportMenu.ExportFormat>().Should().Contain("Clipboard");
    }

    [Fact]
    public void ExportMenu_ExportResult_HasRequiredProperties()
    {
        // Arrange
        var result = new ExportMenu.ExportResult
        {
            Success = true,
            Format = ExportMenu.ExportFormat.Markdown,
            Filename = "test.md"
        };

        // Assert
        result.Success.Should().BeTrue();
        result.Format.Should().Be(ExportMenu.ExportFormat.Markdown);
        result.Filename.Should().Be("test.md");
    }

    [Fact]
    public void ExportMenu_ExportResult_CanHaveError()
    {
        // Arrange
        var result = new ExportMenu.ExportResult
        {
            Success = false,
            Format = ExportMenu.ExportFormat.Html,
            Error = "Export failed"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.Error.Should().Be("Export failed");
    }

    [Fact]
    public void ExportMenu_ButtonIsEnabledWhenNotExporting()
    {
        // Act
        var cut = RenderComponent<ExportMenu>();

        // Assert
        var button = cut.Find("button");
        button.HasAttribute("disabled").Should().BeFalse();
    }

    [Fact]
    public void ExportMenu_HasExportMenuClass()
    {
        // Act
        var cut = RenderComponent<ExportMenu>();

        // Assert
        cut.Markup.Should().Contain("export-menu");
    }
}
