# AESIR Settings Module UI Refinement Plan

## Design Vision: "Nordic Control Center"

A sophisticated, atmospheric interface that elevates AESIR from a functional tool to a premium AI orchestration platform. Inspired by Scandinavian design principles: purposeful simplicity, excellent typography, and depth through lighting and shadow.

**Core Aesthetic Principles:**
- **Dramatic hierarchy** through scale, weight, and spatial relationships
- **Ambient depth** via subtle gradients, layered surfaces, and thoughtful shadows
- **Kinetic feedback** with purposeful microinteractions that guide and delight
- **Information clarity** through visual grouping and consistent patterns

---

## Table of Contents

1. [Phase 1: Foundation & Typography](#phase-1-foundation--typography)
2. [Phase 2: Navigation Sidebar](#phase-2-navigation-sidebar)
3. [Phase 3: Content Headers](#phase-3-content-headers)
4. [Phase 4: DataGrid Enhancement](#phase-4-datagrid-enhancement)
5. [Phase 5: Settings Cards](#phase-5-settings-cards)
6. [Phase 6: Empty States](#phase-6-empty-states)
7. [Phase 7: Dialog Redesign](#phase-7-dialog-redesign)
8. [Phase 8: Microinteractions & Polish](#phase-8-microinteractions--polish)
9. [Component Specifications](#component-specifications)
10. [Testing Checklist](#testing-checklist)

---

## Phase 1: Foundation & Typography

### Objective
Establish the typographic foundation that sets AESIR apart from generic interfaces.

### Files to Modify

#### 1.1 Theme Typography Update
**File:** `Client/Aesir.Client.Web/Aesir.Client.Web.Infrastructure/Theme/AesirTheme.cs`

**Changes:**

```csharp
// Lines 287-298: Update font family
private static Typography CreateTypography()
{
    return new Typography
    {
        Default = new DefaultTypography
        {
            // CHANGE: Replace Inter with Plus Jakarta Sans
            FontFamily = ["Plus Jakarta Sans", "Inter", "system-ui", "-apple-system", "sans-serif"],
            FontSize = ".875rem",
            FontWeight = "400",
            LineHeight = "1.6",  // CHANGE: Slightly increased for readability
            LetterSpacing = "-0.01em"  // CHANGE: Tighter for modern feel
        },
        // ... update all typography variants similarly
    };
}
```

**Full Typography Specification:**

| Variant | Font Size | Weight | Line Height | Letter Spacing | Use Case |
|---------|-----------|--------|-------------|----------------|----------|
| H1 | 2.75rem | 700 | 1.15 | -0.03em | Page titles (unused in settings) |
| H2 | 2.25rem | 700 | 1.2 | -0.025em | Section titles |
| H3 | 1.875rem | 600 | 1.25 | -0.02em | Card titles |
| H4 | 1.5rem | 600 | 1.3 | -0.015em | Content headers |
| H5 | 1.25rem | 600 | 1.35 | -0.01em | Subsection headers |
| H6 | 1rem | 600 | 1.4 | -0.005em | Small headers |
| Body1 | 1rem | 400 | 1.6 | -0.01em | Primary body text |
| Body2 | 0.875rem | 400 | 1.5 | -0.005em | Secondary body text |
| Caption | 0.75rem | 500 | 1.4 | 0.01em | Labels, hints |
| Button | 0.875rem | 600 | 1.75 | 0.02em | Button text |

#### 1.2 Add Font Import
**File:** `Client/Aesir.Client.Web/Aesir.Client.Web.App/wwwroot/index.html`

**Add to `<head>`:**

```html
<!-- Plus Jakarta Sans - Distinctive, modern typography -->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Plus+Jakarta+Sans:wght@400;500;600;700&display=swap" rel="stylesheet">
```

#### 1.3 Create Shared CSS Variables
**File:** `Client/Aesir.Client.Web/Aesir.Client.Web.App/wwwroot/css/settings-refinements.css` (NEW FILE)

```css
/* ==========================================================================
   AESIR Settings Module - Design System Variables
   ========================================================================== */

:root {
    /* Spacing Scale (based on 4px grid) */
    --aesir-space-1: 4px;
    --aesir-space-2: 8px;
    --aesir-space-3: 12px;
    --aesir-space-4: 16px;
    --aesir-space-5: 20px;
    --aesir-space-6: 24px;
    --aesir-space-8: 32px;
    --aesir-space-10: 40px;
    --aesir-space-12: 48px;
    --aesir-space-16: 64px;

    /* Border Radius Scale */
    --aesir-radius-sm: 6px;
    --aesir-radius-md: 10px;
    --aesir-radius-lg: 14px;
    --aesir-radius-xl: 20px;
    --aesir-radius-full: 9999px;

    /* Shadows (Dark Mode Optimized) */
    --aesir-shadow-sm: 0 1px 2px rgba(0, 0, 0, 0.15);
    --aesir-shadow-md: 0 4px 12px rgba(0, 0, 0, 0.2);
    --aesir-shadow-lg: 0 8px 24px rgba(0, 0, 0, 0.25);
    --aesir-shadow-xl: 0 16px 48px rgba(0, 0, 0, 0.3);
    --aesir-shadow-glow-primary: 0 4px 20px rgba(84, 169, 255, 0.25);
    --aesir-shadow-glow-accent: 0 4px 20px rgba(123, 111, 255, 0.25);

    /* Transitions */
    --aesir-transition-fast: 0.15s ease;
    --aesir-transition-normal: 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    --aesir-transition-slow: 0.3s cubic-bezier(0.4, 0, 0.2, 1);

    /* Gradient Definitions */
    --aesir-gradient-primary: linear-gradient(135deg, rgba(84, 169, 255, 0.12) 0%, rgba(123, 111, 255, 0.06) 100%);
    --aesir-gradient-primary-hover: linear-gradient(135deg, rgba(84, 169, 255, 0.18) 0%, rgba(123, 111, 255, 0.10) 100%);
    --aesir-gradient-primary-active: linear-gradient(135deg, rgba(84, 169, 255, 0.24) 0%, rgba(123, 111, 255, 0.14) 100%);
    --aesir-gradient-surface: linear-gradient(180deg, var(--mud-palette-surface) 0%, rgba(30, 30, 34, 0.98) 100%);
}

/* Light mode overrides */
.mud-theme-light {
    --aesir-shadow-sm: 0 1px 3px rgba(0, 0, 0, 0.08);
    --aesir-shadow-md: 0 4px 12px rgba(0, 0, 0, 0.1);
    --aesir-shadow-lg: 0 8px 24px rgba(0, 0, 0, 0.12);
    --aesir-shadow-xl: 0 16px 48px rgba(0, 0, 0, 0.15);
}
```

#### 1.4 Import CSS in App
**File:** `Client/Aesir.Client.Web/Aesir.Client.Web.App/wwwroot/index.html`

**Add before closing `</head>`:**

```html
<link href="css/settings-refinements.css" rel="stylesheet" />
```

---

## Phase 2: Navigation Sidebar

### Objective
Transform the flat navigation into an atmospheric, responsive sidebar with clear visual hierarchy.

### Files to Modify

#### 2.1 SettingsTabs Component
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/SettingsTabs.razor`

**Complete Replacement of `<style>` block (lines 59-183):**

```css
<style>
    /* ==========================================================================
       Settings Panel - Main Container
       ========================================================================== */
    .settings-panel {
        display: flex;
        height: 100%;
        min-height: calc(100vh - 48px);
        padding-top: 48px;
        background-color: var(--mud-palette-background);
    }

    /* ==========================================================================
       Settings Tabs Container - Left Sidebar
       ========================================================================== */
    .settings-tabs-container {
        width: 260px;
        flex-shrink: 0;
        background: var(--aesir-gradient-surface);
        border-right: 1px solid var(--mud-palette-divider);
        display: flex;
        flex-direction: column;
        position: relative;
    }

    /* Subtle top highlight for depth */
    .settings-tabs-container::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        height: 1px;
        background: linear-gradient(90deg,
            transparent 0%,
            rgba(84, 169, 255, 0.2) 50%,
            transparent 100%
        );
    }

    .settings-tabs {
        display: flex;
        flex-direction: column;
        height: 100%;
        padding: var(--aesir-space-4) var(--aesir-space-3);
        overflow-y: auto;
        overflow-x: hidden;
    }

    /* Custom scrollbar for navigation */
    .settings-tabs::-webkit-scrollbar {
        width: 4px;
    }

    .settings-tabs::-webkit-scrollbar-track {
        background: transparent;
    }

    .settings-tabs::-webkit-scrollbar-thumb {
        background: var(--mud-palette-divider);
        border-radius: var(--aesir-radius-full);
    }

    .settings-tabs::-webkit-scrollbar-thumb:hover {
        background: var(--mud-palette-text-secondary);
    }

    /* ==========================================================================
       Tab Groups
       ========================================================================== */
    .tab-group {
        margin-bottom: var(--aesir-space-6);
    }

    .tab-group:first-child {
        margin-top: var(--aesir-space-2);
    }

    .tab-group-header {
        font-size: 0.65rem;
        font-weight: 700;
        text-transform: uppercase;
        letter-spacing: 1.5px;
        color: var(--mud-palette-text-secondary);
        padding: var(--aesir-space-2) var(--aesir-space-4);
        margin-bottom: var(--aesir-space-2);
        opacity: 0.7;
        display: flex;
        align-items: center;
        gap: var(--aesir-space-2);
    }

    .tab-group-header::after {
        content: '';
        flex: 1;
        height: 1px;
        background: linear-gradient(90deg,
            var(--mud-palette-divider) 0%,
            transparent 100%
        );
        margin-left: var(--aesir-space-2);
    }

    /* ==========================================================================
       Settings Footer - Wizard Link
       ========================================================================== */
    .settings-footer {
        margin-top: auto;
        padding: var(--aesir-space-4) var(--aesir-space-3);
        border-top: 1px solid var(--mud-palette-divider);
    }

    .wizard-link {
        display: flex;
        align-items: center;
        gap: var(--aesir-space-3);
        padding: var(--aesir-space-3) var(--aesir-space-4);
        border-radius: var(--aesir-radius-md);
        color: var(--mud-palette-text-secondary);
        text-decoration: none;
        font-size: 0.8rem;
        font-weight: 500;
        transition: var(--aesir-transition-normal);
        background: transparent;
        border: 1px dashed transparent;
    }

    .wizard-link:hover {
        background: var(--aesir-gradient-primary);
        color: var(--mud-palette-primary);
        border-color: rgba(84, 169, 255, 0.3);
    }

    .wizard-icon {
        transition: var(--aesir-transition-normal);
    }

    .wizard-link:hover .wizard-icon {
        transform: rotate(15deg) scale(1.1);
        color: var(--mud-palette-primary);
    }

    /* ==========================================================================
       Settings Content Area
       ========================================================================== */
    .settings-content {
        flex: 1;
        overflow-y: auto;
        background-color: var(--mud-palette-background);
        position: relative;
    }

    /* Subtle inset shadow for depth */
    .settings-content::before {
        content: '';
        position: absolute;
        top: 0;
        left: 0;
        right: 0;
        height: 60px;
        background: linear-gradient(180deg,
            rgba(0, 0, 0, 0.03) 0%,
            transparent 100%
        );
        pointer-events: none;
        z-index: 1;
    }

    /* ==========================================================================
       Responsive - Mobile Layout
       ========================================================================== */
    @@media (max-width: 900px) {
        .settings-panel {
            flex-direction: column;
        }

        .settings-tabs-container {
            width: 100%;
            border-right: none;
            border-bottom: 1px solid var(--mud-palette-divider);
            max-height: none;
        }

        .settings-tabs-container::before {
            display: none;
        }

        .settings-tabs {
            flex-direction: row;
            flex-wrap: nowrap;
            overflow-x: auto;
            overflow-y: hidden;
            padding: var(--aesir-space-3);
            gap: var(--aesir-space-2);
            scrollbar-width: none;
            -ms-overflow-style: none;
        }

        .settings-tabs::-webkit-scrollbar {
            display: none;
        }

        .tab-group {
            display: flex;
            flex-wrap: nowrap;
            gap: var(--aesir-space-2);
            margin-bottom: 0;
        }

        .tab-group-header {
            display: none;
        }

        .settings-footer {
            display: none;
        }

        .settings-content {
            min-height: 500px;
        }

        .settings-content::before {
            display: none;
        }
    }

    @@media (max-width: 600px) {
        .settings-tabs {
            padding: var(--aesir-space-2);
        }
    }
</style>
```

#### 2.2 SettingsTabItem Component
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/SettingsTabItem.razor`

**Complete Replacement:**

```razor
@namespace Aesir.Client.Web.Modules.Settings.Components

<div class="settings-tab-item @(IsSelected ? "selected" : "")"
     @onclick="HandleClick"
     role="tab"
     aria-selected="@IsSelected"
     tabindex="0"
     @onkeydown="HandleKeyDown">
    <div class="tab-indicator"></div>
    <div class="tab-icon-container">
        <MudIcon Icon="@GetIcon()" Size="Size.Small" Class="tab-icon" />
    </div>
    <span class="tab-label">@Label</span>
    @if (BadgeCount > 0)
    {
        <MudBadge Content="@BadgeCount"
                  Color="Color.Primary"
                  Overlap="false"
                  Class="tab-badge" />
    }
</div>

<style>
    .settings-tab-item {
        position: relative;
        display: flex;
        align-items: center;
        gap: var(--aesir-space-3);
        padding: var(--aesir-space-3) var(--aesir-space-4);
        border-radius: var(--aesir-radius-md);
        cursor: pointer;
        transition: var(--aesir-transition-normal);
        color: var(--mud-palette-text-secondary);
        margin-bottom: var(--aesir-space-1);
        background: transparent;
        user-select: none;
        outline: none;
    }

    /* Left indicator bar */
    .tab-indicator {
        position: absolute;
        left: 0;
        top: 50%;
        transform: translateY(-50%);
        width: 3px;
        height: 0;
        background: linear-gradient(180deg,
            var(--mud-palette-primary) 0%,
            var(--mud-palette-secondary) 100%
        );
        border-radius: 0 var(--aesir-radius-sm) var(--aesir-radius-sm) 0;
        transition: height var(--aesir-transition-normal);
    }

    /* Icon container for consistent sizing */
    .tab-icon-container {
        width: 24px;
        height: 24px;
        display: flex;
        align-items: center;
        justify-content: center;
        flex-shrink: 0;
    }

    .tab-icon {
        transition: var(--aesir-transition-fast);
        opacity: 0.7;
    }

    .tab-label {
        font-size: 0.875rem;
        font-weight: 500;
        white-space: nowrap;
        overflow: hidden;
        text-overflow: ellipsis;
        transition: var(--aesir-transition-fast);
    }

    .tab-badge {
        margin-left: auto;
    }

    /* Hover State */
    .settings-tab-item:hover {
        background: var(--aesir-gradient-primary);
        color: var(--mud-palette-text-primary);
    }

    .settings-tab-item:hover .tab-indicator {
        height: 16px;
    }

    .settings-tab-item:hover .tab-icon {
        opacity: 1;
        color: var(--mud-palette-primary);
    }

    /* Focus State (Keyboard Navigation) */
    .settings-tab-item:focus-visible {
        outline: 2px solid var(--mud-palette-primary);
        outline-offset: 2px;
    }

    /* Selected State */
    .settings-tab-item.selected {
        background: var(--aesir-gradient-primary-active);
        color: var(--mud-palette-primary);
    }

    .settings-tab-item.selected .tab-indicator {
        height: 28px;
    }

    .settings-tab-item.selected .tab-icon {
        opacity: 1;
        color: var(--mud-palette-primary);
    }

    .settings-tab-item.selected .tab-label {
        font-weight: 600;
    }

    /* Active/Pressed State */
    .settings-tab-item:active {
        transform: scale(0.98);
    }

    /* ==========================================================================
       Responsive - Mobile Compact Mode
       ========================================================================== */
    @@media (max-width: 900px) {
        .settings-tab-item {
            flex-direction: column;
            padding: var(--aesir-space-2) var(--aesir-space-3);
            gap: var(--aesir-space-1);
            min-width: 72px;
            margin-bottom: 0;
        }

        .tab-indicator {
            top: auto;
            bottom: 0;
            left: 50%;
            transform: translateX(-50%);
            width: 0;
            height: 3px;
            border-radius: var(--aesir-radius-sm) var(--aesir-radius-sm) 0 0;
        }

        .settings-tab-item:hover .tab-indicator,
        .settings-tab-item.selected .tab-indicator {
            width: 24px;
            height: 3px;
        }

        .tab-label {
            font-size: 0.7rem;
            text-align: center;
        }

        .tab-badge {
            position: absolute;
            top: 2px;
            right: 2px;
            margin-left: 0;
        }
    }
</style>

@code {
    /// <summary>
    /// Material Design icon name (e.g., "Settings", "Memory", "SmartToy").
    /// </summary>
    [Parameter]
    public string Icon { get; set; } = "Settings";

    /// <summary>
    /// Display label for the tab.
    /// </summary>
    [Parameter]
    public required string Label { get; set; }

    /// <summary>
    /// Unique identifier for the tab (used in URL query parameter).
    /// </summary>
    [Parameter]
    public required string TabId { get; set; }

    /// <summary>
    /// Whether this tab is currently selected.
    /// </summary>
    [Parameter]
    public bool IsSelected { get; set; }

    /// <summary>
    /// Optional badge count to display (e.g., number of items).
    /// </summary>
    [Parameter]
    public int BadgeCount { get; set; }

    /// <summary>
    /// Callback when the tab is clicked.
    /// </summary>
    [Parameter]
    public EventCallback<string> OnClick { get; set; }

    private async Task HandleClick()
    {
        await OnClick.InvokeAsync(TabId);
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" || e.Key == " ")
        {
            await OnClick.InvokeAsync(TabId);
        }
    }

    private string GetIcon() => Icon.ToLower() switch
    {
        "settings" => Icons.Material.Filled.Settings,
        "memory" => Icons.Material.Filled.Memory,
        "dns" => Icons.Material.Filled.Dns,
        "build" => Icons.Material.Filled.Build,
        "smarttoy" => Icons.Material.Filled.SmartToy,
        "timeline" => Icons.Material.Filled.Timeline,
        "tune" => Icons.Material.Filled.Tune,
        "autoawesome" => Icons.Material.Filled.AutoAwesome,
        "diversity3" => Icons.Material.Filled.Diversity3,
        "extension" => Icons.Material.Filled.Extension,
        "hub" => Icons.Material.Filled.Hub,
        _ => Icons.Material.Filled.Circle
    };
}
```

---

## Phase 3: Content Headers

### Objective
Create prominent, informative page headers that establish context and provide clear actions.

### Files to Modify

#### 3.1 Create Shared Header Component
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/SettingsContentHeader.razor` (NEW FILE)

```razor
@namespace Aesir.Client.Web.Modules.Settings.Components

<div class="settings-content-header">
    <div class="header-main">
        <div class="header-icon-wrapper">
            <div class="header-icon-bg"></div>
            <MudIcon Icon="@Icon" Size="Size.Large" Class="header-icon" />
        </div>
        <div class="header-text">
            <div class="header-title-row">
                <MudText Typo="Typo.h4" Class="header-title">@Title</MudText>
                @if (!string.IsNullOrEmpty(Badge))
                {
                    <MudChip T="string"
                             Size="Size.Small"
                             Color="@BadgeColor"
                             Variant="Variant.Outlined"
                             Class="header-badge">
                        @Badge
                    </MudChip>
                }
            </div>
            <MudText Typo="Typo.body2" Color="Color.Secondary" Class="header-description">
                @Description
            </MudText>
        </div>
    </div>

    <div class="header-actions">
        @if (ShowSaveCancel)
        {
            <MudButton Variant="Variant.Text"
                       Color="Color.Default"
                       OnClick="OnCancel"
                       Disabled="@(!HasChanges || IsSaving)"
                       Class="header-action-btn">
                Cancel
            </MudButton>
            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       StartIcon="@Icons.Material.Filled.Save"
                       OnClick="OnSave"
                       Disabled="@(!HasChanges || IsSaving)"
                       Class="header-action-btn header-action-primary">
                @if (IsSaving)
                {
                    <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                    <span>Saving...</span>
                }
                else
                {
                    <span>Save Changes</span>
                }
            </MudButton>
        }
        else if (ChildContent != null)
        {
            @ChildContent
        }
        else if (!string.IsNullOrEmpty(PrimaryActionText))
        {
            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       StartIcon="@PrimaryActionIcon"
                       OnClick="OnPrimaryAction"
                       Class="header-action-btn header-action-primary">
                @PrimaryActionText
            </MudButton>
        }
    </div>
</div>

@if (ShowWarning && !string.IsNullOrEmpty(WarningText))
{
    <MudAlert Severity="Severity.Warning"
              Dense="true"
              Class="header-warning"
              Icon="@Icons.Material.Outlined.Warning">
        @WarningText
    </MudAlert>
}

@if (HasChanges && ShowUnsavedIndicator)
{
    <MudAlert Severity="Severity.Info"
              Dense="true"
              Class="header-unsaved"
              Icon="@Icons.Material.Outlined.Edit">
        You have unsaved changes
    </MudAlert>
}

<style>
    .settings-content-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        padding: var(--aesir-space-8) 0 var(--aesir-space-6);
        gap: var(--aesir-space-6);
        flex-wrap: wrap;
    }

    .header-main {
        display: flex;
        align-items: flex-start;
        gap: var(--aesir-space-5);
        flex: 1;
        min-width: 280px;
    }

    .header-icon-wrapper {
        position: relative;
        flex-shrink: 0;
    }

    .header-icon-bg {
        width: 64px;
        height: 64px;
        border-radius: var(--aesir-radius-lg);
        background: var(--aesir-gradient-primary);
        position: absolute;
        top: 0;
        left: 0;
    }

    .header-icon {
        position: relative;
        z-index: 1;
        width: 64px;
        height: 64px;
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--mud-palette-primary);
    }

    .header-text {
        flex: 1;
        min-width: 0;
    }

    .header-title-row {
        display: flex;
        align-items: center;
        gap: var(--aesir-space-3);
        margin-bottom: var(--aesir-space-1);
    }

    .header-title {
        font-weight: 700;
        letter-spacing: -0.02em;
        line-height: 1.2;
    }

    .header-badge {
        font-size: 0.7rem;
    }

    .header-description {
        line-height: 1.5;
        max-width: 500px;
    }

    .header-actions {
        display: flex;
        align-items: center;
        gap: var(--aesir-space-3);
        flex-shrink: 0;
    }

    .header-action-btn {
        white-space: nowrap;
    }

    .header-action-primary {
        padding: var(--aesir-space-3) var(--aesir-space-6);
        font-weight: 600;
        border-radius: var(--aesir-radius-md);
        box-shadow: var(--aesir-shadow-glow-primary);
        transition: var(--aesir-transition-normal);
    }

    .header-action-primary:hover:not(:disabled) {
        transform: translateY(-2px);
        box-shadow: 0 6px 24px rgba(84, 169, 255, 0.35);
    }

    .header-action-primary:active:not(:disabled) {
        transform: translateY(0);
    }

    .header-warning {
        margin-top: var(--aesir-space-4);
        border-radius: var(--aesir-radius-md);
    }

    .header-unsaved {
        margin-top: var(--aesir-space-3);
        border-radius: var(--aesir-radius-md);
        animation: pulse-subtle 2s ease-in-out infinite;
    }

    @@keyframes pulse-subtle {
        0%, 100% { opacity: 1; }
        50% { opacity: 0.8; }
    }

    /* Responsive */
    @@media (max-width: 768px) {
        .settings-content-header {
            padding: var(--aesir-space-6) 0 var(--aesir-space-4);
        }

        .header-main {
            flex-direction: column;
            gap: var(--aesir-space-3);
        }

        .header-icon-wrapper {
            display: none;
        }

        .header-actions {
            width: 100%;
            justify-content: flex-end;
        }
    }
</style>

@code {
    [Parameter] public string Icon { get; set; } = Icons.Material.Filled.Settings;
    [Parameter] public string Title { get; set; } = "Settings";
    [Parameter] public string Description { get; set; } = "";
    [Parameter] public string? Badge { get; set; }
    [Parameter] public Color BadgeColor { get; set; } = Color.Info;

    [Parameter] public string? PrimaryActionText { get; set; }
    [Parameter] public string PrimaryActionIcon { get; set; } = Icons.Material.Filled.Add;
    [Parameter] public EventCallback OnPrimaryAction { get; set; }

    [Parameter] public bool ShowSaveCancel { get; set; }
    [Parameter] public bool HasChanges { get; set; }
    [Parameter] public bool IsSaving { get; set; }
    [Parameter] public bool ShowUnsavedIndicator { get; set; } = true;
    [Parameter] public EventCallback OnSave { get; set; }
    [Parameter] public EventCallback OnCancel { get; set; }

    [Parameter] public bool ShowWarning { get; set; }
    [Parameter] public string? WarningText { get; set; }

    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

#### 3.2 Update AgentsContent to Use New Header
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/AgentsContent.razor`

**Replace lines 1-18 (header section) with:**

```razor
@namespace Aesir.Client.Web.Modules.Settings.Components

<div class="settings-content-container">
    <SettingsContentHeader
        Icon="@Icons.Material.Filled.SmartToy"
        Title="Agents"
        Description="Configure AI agents with model parameters, personas, and tool assignments."
        Badge="@(_agentViewModels.Count > 0 ? $"{_agentViewModels.Count} configured" : null)"
        PrimaryActionText="Add Agent"
        PrimaryActionIcon="@Icons.Material.Filled.Add"
        OnPrimaryAction="OpenCreateDialog" />

    @* ... rest of component ... *@
</div>
```

#### 3.3 Update GeneralSettingsContent Header
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/GeneralSettingsContent.razor`

**Replace lines 1-38 (header section) with:**

```razor
@namespace Aesir.Client.Web.Modules.Settings.Components

<div class="settings-content-container">
    <SettingsContentHeader
        Icon="@Icons.Material.Filled.Tune"
        Title="General Settings"
        Description="Configure RAG, Speech, and Search settings for the platform."
        ShowSaveCancel="true"
        HasChanges="_hasChanges"
        IsSaving="_isSaving"
        OnSave="SaveSettings"
        OnCancel="CancelChanges"
        ShowWarning="true"
        WarningText="Changes require a server restart to take effect." />

    @* ... rest of component ... *@
</div>
```

#### 3.4 Update InferenceEnginesContent Header
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/InferenceEnginesContent.razor`

**Replace lines 3-18 (header section) with:**

```razor
<div class="settings-content-container">
    <SettingsContentHeader
        Icon="@Icons.Material.Filled.Memory"
        Title="Inference Engines"
        Description="Configure the AI backends used for chat completions."
        Badge="@(_engines.Count > 0 ? $"{_engines.Count} configured" : null)"
        PrimaryActionText="Add Engine"
        PrimaryActionIcon="@Icons.Material.Filled.Add"
        OnPrimaryAction="OpenCreateDialog" />

    @* ... rest of component ... *@
</div>
```

**Remove existing `<style>` block** - styles now provided by `settings-refinements.css`.

---

#### 3.5 Update McpServersContent Header
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/McpServersContent.razor`

**Replace lines 3-18 (header section) with:**

```razor
<div class="settings-content-container">
    <SettingsContentHeader
        Icon="@Icons.Material.Filled.Dns"
        Title="MCP Servers"
        Description="Configure Model Context Protocol servers for tool integration."
        Badge="@(_servers.Count > 0 ? $"{_servers.Count} configured" : null)"
        PrimaryActionText="Add Server"
        PrimaryActionIcon="@Icons.Material.Filled.Add"
        OnPrimaryAction="OpenCreateDialog" />

    @* ... rest of component ... *@
</div>
```

**Remove existing `<style>` block** - styles now provided by `settings-refinements.css`.

---

#### 3.6 Update ToolsContent Header
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/ToolsContent.razor`

**Replace lines 3-18 (header section) with:**

```razor
<div class="settings-content-container">
    <SettingsContentHeader
        Icon="@Icons.Material.Filled.Build"
        Title="Tools"
        Description="Manage internal tools and view discovered MCP server tools."
        Badge="@(_tools.Count > 0 ? $"{_tools.Count} available" : null)"
        PrimaryActionText="Add Internal Tool"
        PrimaryActionIcon="@Icons.Material.Filled.Add"
        OnPrimaryAction="OpenCreateDialog" />

    @* ... rest of component (filter section, data grid, etc.) ... *@
</div>
```

**Remove existing `<style>` block** - styles now provided by `settings-refinements.css`.

---

#### 3.7 Update ResearchTeamsContent Header
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/ResearchTeamsContent.razor`

**Replace lines 4-19 (header section) with:**

```razor
<div class="settings-content-container">
    <SettingsContentHeader
        Icon="@Icons.Material.Filled.Diversity3"
        Title="Research Teams"
        Description="Configure multi-agent research teams with role assignments and parameter overrides."
        Badge="@(_teams.Count > 0 ? $"{_teams.Count} teams" : null)"
        PrimaryActionText="Add Team"
        PrimaryActionIcon="@Icons.Material.Filled.Add"
        OnPrimaryAction="OpenCreateDialog" />

    @* ... rest of component ... *@
</div>
```

**Remove existing `<style>` block** - styles now provided by `settings-refinements.css`.

---

#### 3.8 Update ObservabilityContent Header
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Observability/Components/ObservabilityContent.razor`

**Note:** Observability is in a different module and has a unique header with stats display. We need to:
1. Add a reference to the Settings module for the SettingsContentHeader component, OR
2. Create a similar header component within the Observability module

**Option A - Use ChildContent for custom stats:**

```razor
<div class="settings-content-container">
    <SettingsContentHeader
        Icon="@Icons.Material.Filled.Timeline"
        Title="Observability"
        Description="Monitor AI operations, document processing, and function executions.">
        <ChildContent>
            @if (ObservabilityService.CurrentUnifiedResponse != null)
            {
                <div class="header-stats">
                    <MudChip T="string" Size="Size.Small" Color="Color.Primary" Variant="Variant.Outlined">
                        @ObservabilityService.CurrentUnifiedResponse.TotalCount total
                    </MudChip>
                    <MudChip T="string" Size="Size.Small" Color="Color.Default" Variant="Variant.Outlined">
                        Page @ObservabilityService.CurrentUnifiedResponse.Page of @ObservabilityService.CurrentUnifiedResponse.TotalPages
                    </MudChip>
                </div>
            }
        </ChildContent>
    </SettingsContentHeader>

    @* ... rest of component ... *@
</div>
```

**Option B - Keep existing header but apply styling:**

If cross-module dependency is undesirable, update the existing `.observability-content` styles to match the Settings design system:

```css
/* Add to Observability module styles or settings-refinements.css */
.observability-content {
    padding: 0 var(--aesir-space-8, 32px) var(--aesir-space-8, 32px);
    max-width: 1400px;
    margin: 0 auto;
}

.observability-content .content-header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    padding: var(--aesir-space-8, 32px) 0 var(--aesir-space-6, 24px);
    gap: var(--aesir-space-6, 24px);
    flex-wrap: wrap;
}

.observability-content .header-stats {
    display: flex;
    gap: var(--aesir-space-3, 12px);
    align-items: center;
}
```

**Recommendation:** Use Option A if the Observability module can reference the Settings module. Otherwise, use Option B for visual consistency without coupling.

---

#### 3.9 Update Content Container Styles
**Add to:** `Client/Aesir.Client.Web/Aesir.Client.Web.App/wwwroot/css/settings-refinements.css`

```css
/* ==========================================================================
   Settings Content Container
   ========================================================================== */

.settings-content-container {
    padding: 0 var(--aesir-space-8) var(--aesir-space-8);
    max-width: 1200px;
    margin: 0 auto;
}

.content-body {
    padding-bottom: var(--aesir-space-12);
}

/* Responsive */
@media (max-width: 768px) {
    .settings-content-container {
        padding: 0 var(--aesir-space-4) var(--aesir-space-6);
    }
}
```

---

## Phase 4: DataGrid Enhancement

### Objective
Elevate data tables with better visual hierarchy, row treatments, and action feedback.

### Files to Modify

#### 4.1 DataGrid Styles
**Add to:** `Client/Aesir.Client.Web/Aesir.Client.Web.App/wwwroot/css/settings-refinements.css`

```css
/* ==========================================================================
   Enhanced DataGrid Styles
   ========================================================================== */

.aesir-data-grid {
    border-radius: var(--aesir-radius-lg);
    overflow: hidden;
    border: 1px solid var(--mud-palette-divider);
    background: var(--mud-palette-surface);
}

/* Header styling */
.aesir-data-grid .mud-table-head {
    background: rgba(0, 0, 0, 0.02);
}

.mud-theme-dark .aesir-data-grid .mud-table-head {
    background: rgba(255, 255, 255, 0.02);
}

.aesir-data-grid .mud-table-head th {
    font-weight: 600;
    font-size: 0.75rem;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    color: var(--mud-palette-text-secondary);
    padding: var(--aesir-space-4) var(--aesir-space-4);
    border-bottom: 1px solid var(--mud-palette-divider);
}

/* Row styling */
.aesir-data-grid .mud-table-row {
    transition: var(--aesir-transition-fast);
}

.aesir-data-grid .mud-table-row:hover {
    background: var(--aesir-gradient-primary) !important;
}

.aesir-data-grid .mud-table-cell {
    padding: var(--aesir-space-4);
    border-bottom: 1px solid rgba(128, 128, 128, 0.1);
}

/* Last row no border */
.aesir-data-grid .mud-table-body tr:last-child .mud-table-cell {
    border-bottom: none;
}

/* Action buttons */
.aesir-data-grid .row-actions {
    opacity: 0.5;
    transition: var(--aesir-transition-fast);
}

.aesir-data-grid .mud-table-row:hover .row-actions {
    opacity: 1;
}

.aesir-data-grid .action-btn {
    border-radius: var(--aesir-radius-sm);
    transition: var(--aesir-transition-fast);
}

.aesir-data-grid .action-btn:hover {
    background: rgba(128, 128, 128, 0.1);
}

.aesir-data-grid .action-btn-danger:hover {
    background: rgba(239, 68, 68, 0.1);
    color: var(--mud-palette-error);
}

/* Name cell with avatar */
.entity-name-cell {
    display: flex;
    align-items: center;
    gap: var(--aesir-space-3);
}

.entity-avatar {
    flex-shrink: 0;
}

.entity-info {
    min-width: 0;
}

.entity-name {
    font-weight: 600;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}

.entity-description {
    font-size: 0.8rem;
    color: var(--mud-palette-text-secondary);
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
    max-width: 300px;
}
```

#### 4.2 Update AgentsContent DataGrid
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/AgentsContent.razor`

**Replace DataGrid (lines 33-79) with:**

```razor
<MudDataGrid T="AgentViewModel"
             Items="@_agentViewModels"
             Dense="false"
             Hover="true"
             Striped="false"
             Elevation="0"
             Class="aesir-data-grid"
             Loading="@_isLoading"
             LoadingProgressColor="Color.Primary">
    <Columns>
        <TemplateColumn Title="Agent" SortBy="x => x.Agent.Name">
            <CellTemplate>
                <div class="entity-name-cell">
                    <MudAvatar Size="Size.Medium"
                               Class="entity-avatar"
                               Style="@GetAgentAvatarStyle(context.Item.Agent.Name)">
                        @(context.Item.Agent.Name?.FirstOrDefault().ToString().ToUpper() ?? "?")
                    </MudAvatar>
                    <div class="entity-info">
                        <div class="entity-name">@context.Item.Agent.Name</div>
                        @if (!string.IsNullOrEmpty(context.Item.Agent.Description))
                        {
                            <div class="entity-description">@context.Item.Agent.Description</div>
                        }
                    </div>
                </div>
            </CellTemplate>
        </TemplateColumn>

        <TemplateColumn Title="Engine" Sortable="false">
            <CellTemplate>
                <MudStack Row="true" Spacing="2" AlignItems="AlignItems.Center">
                    <MudIcon Icon="@Icons.Material.Outlined.Memory"
                             Size="Size.Small"
                             Color="Color.Secondary" />
                    <MudText Typo="Typo.body2">
                        @GetInferenceEngineName(context.Item.Agent.ChatInferenceEngineId)
                    </MudText>
                </MudStack>
            </CellTemplate>
        </TemplateColumn>

        <TemplateColumn Title="Tools" Sortable="false">
            <CellTemplate>
                <MudChip T="string"
                         Size="Size.Small"
                         Color="Color.Info"
                         Variant="Variant.Text"
                         Icon="@Icons.Material.Outlined.Build">
                    @context.Item.ToolCount
                </MudChip>
            </CellTemplate>
        </TemplateColumn>

        <TemplateColumn Title="" Sortable="false" CellStyle="width: 100px;">
            <CellTemplate>
                <MudStack Row="true" Spacing="1" Class="row-actions">
                    <MudTooltip Text="Edit agent" Placement="Placement.Top">
                        <MudIconButton Icon="@Icons.Material.Outlined.Edit"
                                       Size="Size.Small"
                                       Class="action-btn"
                                       OnClick="@(() => OpenEditDialog(context.Item.Agent))" />
                    </MudTooltip>
                    <MudTooltip Text="Delete agent" Placement="Placement.Top">
                        <MudIconButton Icon="@Icons.Material.Outlined.DeleteOutline"
                                       Size="Size.Small"
                                       Class="action-btn action-btn-danger"
                                       OnClick="@(() => ConfirmDelete(context.Item.Agent))" />
                    </MudTooltip>
                </MudStack>
            </CellTemplate>
        </TemplateColumn>
    </Columns>

    <NoRecordsContent>
        <EmptyState
            Icon="@Icons.Material.Outlined.SmartToy"
            Title="No agents yet"
            Description="Create your first AI agent to start orchestrating conversations"
            ActionText="Create First Agent"
            OnAction="OpenCreateDialog" />
    </NoRecordsContent>
</MudDataGrid>
```

**Add helper method to code block:**

```csharp
private string GetAgentAvatarStyle(string? name)
{
    if (string.IsNullOrEmpty(name)) return "";

    // Generate consistent color based on name
    var hash = name.GetHashCode();
    var hue = Math.Abs(hash % 360);

    return $"background: hsl({hue}, 60%, 45%); color: white;";
}
```

---

## Phase 5: Settings Cards

### Objective
Transform flat cards into atmospheric, interactive containers with clear visual grouping.

### Files to Modify

#### 5.1 Settings Card Styles
**Add to:** `Client/Aesir.Client.Web/Aesir.Client.Web.App/wwwroot/css/settings-refinements.css`

```css
/* ==========================================================================
   Settings Cards
   ========================================================================== */

.settings-card {
    padding: var(--aesir-space-6);
    border-radius: var(--aesir-radius-lg);
    border: 1px solid var(--mud-palette-divider);
    background: var(--mud-palette-surface);
    transition: var(--aesir-transition-normal);
    height: 100%;
}

.settings-card:hover {
    border-color: rgba(84, 169, 255, 0.3);
    box-shadow: 0 0 0 3px rgba(84, 169, 255, 0.08);
}

.settings-card:focus-within {
    border-color: var(--mud-palette-primary);
    box-shadow: 0 0 0 3px rgba(84, 169, 255, 0.15);
}

.card-header {
    display: flex;
    align-items: center;
    gap: var(--aesir-space-2);
    margin-bottom: var(--aesir-space-2);
}

.card-header-icon {
    color: var(--mud-palette-primary);
    opacity: 0.8;
}

.card-header-title {
    font-weight: 600;
    font-size: 0.95rem;
}

.card-description {
    font-size: 0.8rem;
    color: var(--mud-palette-text-secondary);
    margin-bottom: var(--aesir-space-5);
    line-height: 1.5;
}

/* Settings Section Grouping */
.settings-section {
    margin-bottom: var(--aesir-space-10);
}

.settings-section:last-child {
    margin-bottom: 0;
}

.section-header {
    display: flex;
    align-items: center;
    gap: var(--aesir-space-3);
    margin-bottom: var(--aesir-space-5);
    padding-bottom: var(--aesir-space-3);
    border-bottom: 1px solid var(--mud-palette-divider);
}

.section-header-icon {
    color: var(--mud-palette-primary);
}

.section-title {
    font-weight: 600;
    font-size: 1rem;
}

.section-description {
    color: var(--mud-palette-text-secondary);
    font-size: 0.875rem;
    margin-left: auto;
}
```

#### 5.2 Update GeneralSettingsContent Cards
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/GeneralSettingsContent.razor`

**Replace content body section (approximately lines 60-204) with new card structure:**

```razor
<div class="content-body">
    @* RAG Configuration Section *@
    <div class="settings-section">
        <div class="section-header">
            <MudIcon Icon="@Icons.Material.Filled.Psychology"
                     Size="Size.Small"
                     Class="section-header-icon" />
            <span class="section-title">RAG Configuration</span>
            <span class="section-description">Retrieval-Augmented Generation models</span>
        </div>

        <MudGrid Spacing="4">
            <MudItem xs="12" md="6">
                <MudPaper Class="settings-card" Elevation="0">
                    <div class="card-header">
                        <MudIcon Icon="@Icons.Material.Outlined.TextFields"
                                 Size="Size.Small"
                                 Class="card-header-icon" />
                        <span class="card-header-title">Embedding Model</span>
                    </div>
                    <p class="card-description">
                        Converts text into vector representations for semantic search and document retrieval.
                    </p>

                    <MudStack Spacing="3">
                        <MudSelect T="Guid?"
                                   Value="_settings.RagEmbeddingInferenceEngineId"
                                   Label="Inference Engine"
                                   Variant="Variant.Outlined"
                                   AnchorOrigin="Origin.BottomCenter"
                                   Disabled="@_isLoading"
                                   ValueChanged="OnEmbeddingEngineChanged">
                            <MudSelectItem Value="@((Guid?)null)">
                                <em>Select an engine</em>
                            </MudSelectItem>
                            @foreach (var engine in _inferenceEngines)
                            {
                                <MudSelectItem Value="@engine.Id">@engine.Name</MudSelectItem>
                            }
                        </MudSelect>

                        <ModelSelector @bind-SelectedModel="_settings.RagEmbeddingModel"
                                       InferenceEngineId="_settings.RagEmbeddingInferenceEngineId"
                                       Category="ModelCategory.Embedding"
                                       Label="Embedding Model"
                                       OnModelSelected="OnEmbeddingModelSelected" />
                    </MudStack>
                </MudPaper>
            </MudItem>

            <MudItem xs="12" md="6">
                <MudPaper Class="settings-card" Elevation="0">
                    <div class="card-header">
                        <MudIcon Icon="@Icons.Material.Outlined.Image"
                                 Size="Size.Small"
                                 Class="card-header-icon" />
                        <span class="card-header-title">Vision Model</span>
                    </div>
                    <p class="card-description">
                        Processes images for understanding and extraction during document retrieval.
                    </p>

                    <MudStack Spacing="3">
                        <MudSelect T="Guid?"
                                   Value="_settings.RagVisionInferenceEngineId"
                                   Label="Inference Engine"
                                   Variant="Variant.Outlined"
                                   AnchorOrigin="Origin.BottomCenter"
                                   Disabled="@_isLoading"
                                   ValueChanged="OnVisionEngineChanged">
                            <MudSelectItem Value="@((Guid?)null)">
                                <em>Select an engine</em>
                            </MudSelectItem>
                            @foreach (var engine in _inferenceEngines)
                            {
                                <MudSelectItem Value="@engine.Id">@engine.Name</MudSelectItem>
                            }
                        </MudSelect>

                        <ModelSelector @bind-SelectedModel="_settings.RagVisionModel"
                                       InferenceEngineId="_settings.RagVisionInferenceEngineId"
                                       Category="ModelCategory.Vision"
                                       Label="Vision Model"
                                       OnModelSelected="OnVisionModelSelected" />
                    </MudStack>
                </MudPaper>
            </MudItem>
        </MudGrid>
    </div>

    @* Voice & Speech Section *@
    <div class="settings-section">
        <div class="section-header">
            <MudIcon Icon="@Icons.Material.Filled.RecordVoiceOver"
                     Size="Size.Small"
                     Class="section-header-icon" />
            <span class="section-title">Voice & Speech</span>
            <span class="section-description">Text-to-speech configuration</span>
        </div>

        <MudGrid Spacing="4">
            <MudItem xs="12" md="6">
                <MudPaper Class="settings-card" Elevation="0">
                    <div class="card-header">
                        <MudIcon Icon="@Icons.Material.Outlined.VolumeUp"
                                 Size="Size.Small"
                                 Class="card-header-icon" />
                        <span class="card-header-title">Speech Model</span>
                    </div>
                    <p class="card-description">
                        Voice used for hands-free mode and audio responses.
                    </p>

                    <MudSelect T="string"
                               Value="@_selectedTtsModel"
                               ValueChanged="OnTtsModelChanged"
                               Label="Voice Model"
                               Variant="Variant.Outlined"
                               AnchorOrigin="Origin.BottomCenter"
                               Disabled="@_isLoading">
                        <MudSelectItem Value="@("Lessac")">
                            <MudStack Row="true" Spacing="2" AlignItems="AlignItems.Center">
                                <span>Lessac</span>
                                <MudChip T="string" Size="Size.Small" Color="Color.Success" Variant="Variant.Text">
                                    High Quality
                                </MudChip>
                            </MudStack>
                        </MudSelectItem>
                        <MudSelectItem Value="@("Joe")">
                            <MudStack Row="true" Spacing="2" AlignItems="AlignItems.Center">
                                <span>Joe</span>
                                <MudChip T="string" Size="Size.Small" Color="Color.Info" Variant="Variant.Text">
                                    Fast
                                </MudChip>
                            </MudStack>
                        </MudSelectItem>
                    </MudSelect>

                    <MudText Typo="Typo.caption" Color="Color.Secondary" Class="mt-3">
                        @GetTtsModelDescription()
                    </MudText>
                </MudPaper>
            </MudItem>
        </MudGrid>
    </div>

    @* Integrations Section *@
    <div class="settings-section">
        <div class="section-header">
            <MudIcon Icon="@Icons.Material.Filled.Extension"
                     Size="Size.Small"
                     Class="section-header-icon" />
            <span class="section-title">Integrations</span>
            <span class="section-description">External service connections</span>
        </div>

        <MudGrid Spacing="4">
            <MudItem xs="12" md="6">
                <MudPaper Class="settings-card" Elevation="0">
                    <div class="card-header">
                        <MudIcon Icon="@Icons.Material.Outlined.Search"
                                 Size="Size.Small"
                                 Class="card-header-icon" />
                        <span class="card-header-title">Google Search</span>
                    </div>
                    <p class="card-description">
                        Enable web search capabilities for agents using Google Custom Search.
                    </p>

                    <MudStack Spacing="3">
                        <MudTextField @bind-Value="_settings.GoogleSearchEngineId"
                                      Label="Search Engine ID"
                                      Variant="Variant.Outlined"
                                      Placeholder="Enter your Search Engine ID"
                                      Immediate="true"
                                      TextChanged="OnSettingsChanged" />

                        <MudTextField @bind-Value="_settings.GoogleApiKey"
                                      Label="API Key"
                                      Variant="Variant.Outlined"
                                      InputType="@(_showApiKey ? InputType.Text : InputType.Password)"
                                      Placeholder="Enter your API Key"
                                      Immediate="true"
                                      TextChanged="OnSettingsChanged"
                                      Adornment="Adornment.End"
                                      AdornmentIcon="@(_showApiKey ? Icons.Material.Filled.VisibilityOff : Icons.Material.Filled.Visibility)"
                                      OnAdornmentClick="ToggleApiKeyVisibility" />
                    </MudStack>

                    <MudStack Row="true" Spacing="2" Class="mt-4">
                        <MudLink Href="https://programmablesearchengine.google.com"
                                 Target="_blank"
                                 Typo="Typo.caption">
                            Create Search Engine
                        </MudLink>
                        <MudText Typo="Typo.caption" Color="Color.Secondary">|</MudText>
                        <MudLink Href="https://console.cloud.google.com"
                                 Target="_blank"
                                 Typo="Typo.caption">
                            Get API Key
                        </MudLink>
                    </MudStack>
                </MudPaper>
            </MudItem>
        </MudGrid>
    </div>
</div>
```

---

## Phase 6: Empty States

### Objective
Transform empty states from plain text into delightful, actionable experiences.

### Files to Create

#### 6.1 EmptyState Component
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/EmptyState.razor` (NEW FILE)

```razor
@namespace Aesir.Client.Web.Modules.Settings.Components

<div class="empty-state @Class">
    <div class="empty-state-icon-container">
        <div class="empty-state-icon-bg"></div>
        <MudIcon Icon="@Icon" Size="Size.Large" Class="empty-state-icon" />
    </div>

    <MudText Typo="Typo.h6" Class="empty-state-title">
        @Title
    </MudText>

    <MudText Typo="Typo.body2" Color="Color.Secondary" Class="empty-state-description">
        @Description
    </MudText>

    @if (!string.IsNullOrEmpty(ActionText))
    {
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   StartIcon="@ActionIcon"
                   OnClick="OnAction"
                   Class="empty-state-action">
            @ActionText
        </MudButton>
    }

    @if (ChildContent != null)
    {
        <div class="empty-state-custom">
            @ChildContent
        </div>
    }
</div>

<style>
    .empty-state {
        display: flex;
        flex-direction: column;
        align-items: center;
        justify-content: center;
        padding: var(--aesir-space-16) var(--aesir-space-6);
        text-align: center;
        min-height: 300px;
    }

    .empty-state-icon-container {
        position: relative;
        margin-bottom: var(--aesir-space-6);
    }

    .empty-state-icon-bg {
        width: 96px;
        height: 96px;
        border-radius: var(--aesir-radius-xl);
        background: var(--aesir-gradient-primary);
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
        animation: empty-state-pulse 3s ease-in-out infinite;
    }

    @@keyframes empty-state-pulse {
        0%, 100% {
            transform: translate(-50%, -50%) scale(1);
            opacity: 1;
        }
        50% {
            transform: translate(-50%, -50%) scale(1.05);
            opacity: 0.8;
        }
    }

    .empty-state-icon {
        position: relative;
        z-index: 1;
        width: 96px;
        height: 96px;
        display: flex;
        align-items: center;
        justify-content: center;
        color: var(--mud-palette-text-secondary);
    }

    .empty-state-title {
        margin-bottom: var(--aesir-space-2);
        font-weight: 600;
    }

    .empty-state-description {
        max-width: 400px;
        margin-bottom: var(--aesir-space-6);
        line-height: 1.6;
    }

    .empty-state-action {
        padding: var(--aesir-space-3) var(--aesir-space-6);
        font-weight: 600;
        border-radius: var(--aesir-radius-md);
        box-shadow: var(--aesir-shadow-glow-primary);
        transition: var(--aesir-transition-normal);
    }

    .empty-state-action:hover {
        transform: translateY(-2px);
        box-shadow: 0 6px 24px rgba(84, 169, 255, 0.35);
    }

    .empty-state-custom {
        margin-top: var(--aesir-space-4);
    }

    /* Compact variant */
    .empty-state.compact {
        padding: var(--aesir-space-8) var(--aesir-space-4);
        min-height: 200px;
    }

    .empty-state.compact .empty-state-icon-bg {
        width: 64px;
        height: 64px;
    }

    .empty-state.compact .empty-state-icon {
        width: 64px;
        height: 64px;
    }
</style>

@code {
    [Parameter] public string Icon { get; set; } = Icons.Material.Outlined.Inbox;
    [Parameter] public string Title { get; set; } = "Nothing here yet";
    [Parameter] public string Description { get; set; } = "";
    [Parameter] public string? ActionText { get; set; }
    [Parameter] public string ActionIcon { get; set; } = Icons.Material.Filled.Add;
    [Parameter] public EventCallback OnAction { get; set; }
    [Parameter] public string? Class { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

---

## Phase 7: Dialog Redesign

### Objective
Transform tabbed dialogs into guided wizard experiences with clear progress indication.

### Files to Modify

#### 7.1 Dialog Styles
**Add to:** `Client/Aesir.Client.Web/Aesir.Client.Web.App/wwwroot/css/settings-refinements.css`

```css
/* ==========================================================================
   Enhanced Dialog Styles
   ========================================================================== */

.aesir-dialog {
    border-radius: var(--aesir-radius-lg);
    overflow: hidden;
}

.aesir-dialog .mud-dialog-title {
    padding: var(--aesir-space-5) var(--aesir-space-6);
    border-bottom: 1px solid var(--mud-palette-divider);
    font-weight: 600;
}

.aesir-dialog .mud-dialog-content {
    padding: 0;
}

.aesir-dialog .mud-dialog-actions {
    padding: var(--aesir-space-4) var(--aesir-space-6);
    border-top: 1px solid var(--mud-palette-divider);
    gap: var(--aesir-space-3);
}

/* Step Progress Indicator */
.dialog-progress {
    display: flex;
    justify-content: center;
    align-items: center;
    gap: var(--aesir-space-2);
    padding: var(--aesir-space-6) var(--aesir-space-6) var(--aesir-space-4);
    background: rgba(0, 0, 0, 0.02);
    border-bottom: 1px solid var(--mud-palette-divider);
}

.mud-theme-dark .dialog-progress {
    background: rgba(255, 255, 255, 0.02);
}

.progress-step {
    display: flex;
    align-items: center;
    gap: var(--aesir-space-2);
}

.step-indicator {
    width: 28px;
    height: 28px;
    border-radius: 50%;
    display: flex;
    align-items: center;
    justify-content: center;
    font-weight: 600;
    font-size: 0.75rem;
    border: 2px solid var(--mud-palette-divider);
    color: var(--mud-palette-text-secondary);
    background: var(--mud-palette-surface);
    transition: var(--aesir-transition-normal);
}

.progress-step.active .step-indicator {
    border-color: var(--mud-palette-primary);
    background: var(--mud-palette-primary);
    color: white;
    box-shadow: var(--aesir-shadow-glow-primary);
}

.progress-step.completed .step-indicator {
    border-color: var(--mud-palette-success);
    background: var(--mud-palette-success);
    color: white;
}

.step-label {
    font-size: 0.75rem;
    font-weight: 500;
    color: var(--mud-palette-text-secondary);
    transition: var(--aesir-transition-fast);
    display: none;
}

.progress-step.active .step-label {
    display: block;
    color: var(--mud-palette-text-primary);
}

.step-connector {
    width: 40px;
    height: 2px;
    background: var(--mud-palette-divider);
    transition: var(--aesir-transition-normal);
}

.step-connector.completed {
    background: var(--mud-palette-success);
}

/* Dialog Panel Content */
.dialog-panel {
    padding: var(--aesir-space-6);
    min-height: 300px;
}

.dialog-panel-title {
    font-weight: 600;
    margin-bottom: var(--aesir-space-1);
}

.dialog-panel-description {
    color: var(--mud-palette-text-secondary);
    margin-bottom: var(--aesir-space-6);
    line-height: 1.5;
}

/* Form improvements inside dialogs */
.aesir-dialog .mud-input-outlined .mud-input-slot {
    border-radius: var(--aesir-radius-sm);
}

.aesir-dialog .mud-tabs {
    border-radius: var(--aesir-radius-md);
    overflow: hidden;
}

.aesir-dialog .mud-tabs-toolbar {
    background: rgba(0, 0, 0, 0.02);
    min-height: 48px;
}

.mud-theme-dark .aesir-dialog .mud-tabs-toolbar {
    background: rgba(255, 255, 255, 0.02);
}
```

#### 7.2 Update AgentEditDialog (Optional Wizard Conversion)

For a full wizard experience, create a new component or modify the existing dialog. Here's a simplified approach that keeps tabs but improves them:

**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/AgentEditDialog.razor`

**Add Class to MudDialog:**

```razor
<MudDialog Class="aesir-dialog">
```

**Update MudTabs for better styling:**

```razor
<MudTabs Elevation="0"
         Rounded="false"
         ApplyEffectsToContainer="true"
         PanelClass="dialog-panel"
         SliderColor="Color.Primary"
         SliderSize="3"
         Class="aesir-dialog-tabs">
```

---

## Phase 8: Microinteractions & Polish

### Objective
Add subtle animations and feedback that make the interface feel responsive and alive.

### Files to Modify

#### 8.1 Animation Keyframes
**Add to:** `Client/Aesir.Client.Web/Aesir.Client.Web.App/wwwroot/css/settings-refinements.css`

```css
/* ==========================================================================
   Microinteractions & Animations
   ========================================================================== */

/* Fade in animation for content */
@@keyframes fadeIn {
    from {
        opacity: 0;
        transform: translateY(8px);
    }
    to {
        opacity: 1;
        transform: translateY(0);
    }
}

.animate-fade-in {
    animation: fadeIn 0.3s ease-out forwards;
}

/* Staggered children animation */
.animate-stagger > * {
    opacity: 0;
    animation: fadeIn 0.3s ease-out forwards;
}

.animate-stagger > *:nth-child(1) { animation-delay: 0.05s; }
.animate-stagger > *:nth-child(2) { animation-delay: 0.1s; }
.animate-stagger > *:nth-child(3) { animation-delay: 0.15s; }
.animate-stagger > *:nth-child(4) { animation-delay: 0.2s; }
.animate-stagger > *:nth-child(5) { animation-delay: 0.25s; }
.animate-stagger > *:nth-child(6) { animation-delay: 0.3s; }

/* Scale on hover for interactive elements */
.hover-lift {
    transition: var(--aesir-transition-normal);
}

.hover-lift:hover {
    transform: translateY(-2px);
}

/* Skeleton loading animation */
@@keyframes skeleton-pulse {
    0%, 100% {
        opacity: 1;
    }
    50% {
        opacity: 0.5;
    }
}

.skeleton {
    background: linear-gradient(
        90deg,
        var(--mud-palette-background-gray) 0%,
        var(--mud-palette-surface) 50%,
        var(--mud-palette-background-gray) 100%
    );
    background-size: 200% 100%;
    animation: skeleton-pulse 1.5s ease-in-out infinite;
    border-radius: var(--aesir-radius-sm);
}

/* Button press effect */
.btn-press:active {
    transform: scale(0.98);
}

/* Focus ring */
.focus-ring:focus-visible {
    outline: 2px solid var(--mud-palette-primary);
    outline-offset: 2px;
}

/* Tooltip improvements */
.mud-tooltip {
    border-radius: var(--aesir-radius-sm) !important;
    font-size: 0.75rem !important;
    padding: var(--aesir-space-2) var(--aesir-space-3) !important;
}

/* Snackbar improvements */
.mud-snackbar {
    border-radius: var(--aesir-radius-md) !important;
}

/* Chip improvements */
.mud-chip {
    border-radius: var(--aesir-radius-sm) !important;
}

/* Alert improvements */
.mud-alert {
    border-radius: var(--aesir-radius-md) !important;
}
```

#### 8.2 Loading Skeleton Component
**File:** `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/SettingsCardSkeleton.razor` (NEW FILE)

```razor
@namespace Aesir.Client.Web.Modules.Settings.Components

<MudPaper Class="settings-card" Elevation="0">
    <div class="card-header">
        <MudSkeleton SkeletonType="SkeletonType.Circle" Width="20px" Height="20px" />
        <MudSkeleton Width="40%" Height="20px" />
    </div>
    <MudSkeleton Width="80%" Height="14px" Class="mt-2" />
    <MudSkeleton Width="60%" Height="14px" Class="mt-1 mb-5" />
    <MudSkeleton Height="56px" Class="mt-4" />
    <MudSkeleton Height="56px" Class="mt-3" />
</MudPaper>

@code {
}
```

---

## Component Specifications

### Color Usage Guide

| Context | Color | Usage |
|---------|-------|-------|
| Primary actions | `var(--mud-palette-primary)` | Add buttons, save actions, primary CTAs |
| Secondary emphasis | `var(--mud-palette-secondary)` | Research features, alternative actions |
| Success states | `var(--mud-palette-success)` | Completed, active, confirmed |
| Warning states | `var(--mud-palette-warning)` | Caution, pending, attention needed |
| Error states | `var(--mud-palette-error)` | Delete, failures, critical issues |
| Subtle backgrounds | `var(--aesir-gradient-primary)` | Hover states, selected items |

### Spacing Scale

| Variable | Value | Usage |
|----------|-------|-------|
| `--aesir-space-1` | 4px | Tight inline spacing |
| `--aesir-space-2` | 8px | Component internal padding |
| `--aesir-space-3` | 12px | Small gaps |
| `--aesir-space-4` | 16px | Standard gaps, padding |
| `--aesir-space-6` | 24px | Section spacing |
| `--aesir-space-8` | 32px | Large section gaps |
| `--aesir-space-12` | 48px | Page-level spacing |

### Border Radius Scale

| Variable | Value | Usage |
|----------|-------|-------|
| `--aesir-radius-sm` | 6px | Buttons, chips, small elements |
| `--aesir-radius-md` | 10px | Cards, inputs, dialogs |
| `--aesir-radius-lg` | 14px | Large cards, panels |
| `--aesir-radius-xl` | 20px | Hero elements, feature cards |

---

## Testing Checklist

### Visual Testing

- [ ] All components render correctly in light mode
- [ ] All components render correctly in dark mode
- [ ] Hover states are visible and consistent
- [ ] Focus states meet accessibility requirements
- [ ] Selected/active states are clearly distinguishable
- [ ] Loading states display skeleton animations
- [ ] Empty states are informative and actionable
- [ ] Responsive layout works at 320px, 768px, 1024px, 1440px widths

### Interaction Testing

- [ ] Tab navigation works with keyboard
- [ ] All buttons have appropriate hover/active feedback
- [ ] Dialogs open/close smoothly
- [ ] Form validation displays clear error states
- [ ] Snackbar notifications appear correctly
- [ ] DataGrid sorting and filtering work
- [ ] Action buttons in rows respond to clicks

### Accessibility Testing

- [ ] All interactive elements have visible focus states
- [ ] ARIA labels are present on icon-only buttons
- [ ] Color contrast meets WCAG AA requirements
- [ ] Screen reader can navigate all content
- [ ] Tab order is logical

### Performance Testing

- [ ] Initial page load < 2 seconds
- [ ] Tab switching is instant (< 100ms)
- [ ] DataGrid with 50+ items scrolls smoothly
- [ ] No layout shift during loading

---

## Implementation Order

### Week 1: Foundation
1. Phase 1: Typography & CSS variables
2. Phase 2: Navigation sidebar

### Week 2: Content Areas
3. Phase 3: Content headers
4. Phase 5: Settings cards
5. Phase 6: Empty states

### Week 3: Data & Dialogs
6. Phase 4: DataGrid enhancement
7. Phase 7: Dialog improvements

### Week 4: Polish
8. Phase 8: Microinteractions
9. Testing & bug fixes
10. Documentation updates

---

## Files Changed Summary

### New Files
- `Client/Aesir.Client.Web/Aesir.Client.Web.App/wwwroot/css/settings-refinements.css`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/SettingsContentHeader.razor`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/EmptyState.razor`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/SettingsCardSkeleton.razor`

### Modified Files
- `Client/Aesir.Client.Web/Aesir.Client.Web.Infrastructure/Theme/AesirTheme.cs`
- `Client/Aesir.Client.Web/Aesir.Client.Web.App/wwwroot/index.html`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/SettingsTabs.razor`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/SettingsTabItem.razor`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/AgentsContent.razor`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/GeneralSettingsContent.razor`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/InferenceEnginesContent.razor`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/McpServersContent.razor`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/ToolsContent.razor`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/ResearchTeamsContent.razor`
- `Client/Aesir.Client.Web/Modules/Aesir.Client.Web.Modules.Settings/Components/AgentEditDialog.razor`

---

## Notes

- All CSS uses CSS custom properties for consistency and theme support
- Animations respect `prefers-reduced-motion` media query (add if needed)
- Typography changes affect entire application, not just settings
- Consider creating a Storybook-like component gallery for documentation
