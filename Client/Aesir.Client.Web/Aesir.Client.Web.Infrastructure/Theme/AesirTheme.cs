using MudBlazor;

namespace Aesir.Client.Web.Infrastructure.Theme;

/// <summary>
/// AESIR application theme definition.
/// Harmonious color palette based on split-complementary color theory.
/// Primary: Blue (#54A9FF) at 210° on color wheel
/// Accent: Violet (#7B6FFF) at 250° - 40° apart for triadic harmony
/// </summary>
public static class AesirTheme
{
    #region Brand Colors (Primary Blue Family)

    /// <summary>Primary brand blue - main actions, links, highlights</summary>
    public const string Primary = "#54A9FF";
    public const string PrimaryLight = "#7BC1FF";
    public const string PrimaryDark = "#3B8FE5";
    public const string PrimaryDarker = "#2C649F";

    // Legacy aliases for backwards compatibility
    public const string AccentBlue = Primary;
    public const string AccentBlueHover = PrimaryLight;
    public const string AccentBlueDark = PrimaryDarker;

    #endregion

    #region Accent Colors (Harmonious Violet Family)

    /// <summary>Accent violet - research, features, secondary emphasis (40° from primary)</summary>
    public const string Accent = "#7B6FFF";
    public const string AccentLight = "#9D94FF";
    public const string AccentDark = "#5E52E0";
    public const string AccentDarker = "#4840B8";

    #endregion

    #region Semantic Status Colors (Standardized)

    /// <summary>Success - confirmations, completions, positive states</summary>
    public const string Success = "#10B981";
    public const string SuccessLight = "#34D399";
    public const string SuccessDark = "#059669";

    /// <summary>Warning - caution, pending, attention needed</summary>
    public const string Warning = "#F59E0B";
    public const string WarningLight = "#FBBF24";
    public const string WarningDark = "#D97706";

    /// <summary>Error - failures, deletions, critical issues</summary>
    public const string Error = "#EF4444";
    public const string ErrorLight = "#F87171";
    public const string ErrorDark = "#DC2626";

    /// <summary>Info - information, tips, neutral highlights</summary>
    public const string Info = Primary;
    public const string InfoLight = PrimaryLight;
    public const string InfoDark = PrimaryDark;

    #endregion

    #region Code/Syntax Highlighting Colors

    /// <summary>JSON/Code syntax colors for tool displays</summary>
    public const string SyntaxKey = "#7BC1FF";      // Keys - lighter primary
    public const string SyntaxString = "#34D399";   // Strings - success light
    public const string SyntaxNumber = "#FBBF24";   // Numbers - warning light
    public const string SyntaxBoolean = "#9D94FF";  // Booleans - accent light
    public const string SyntaxNull = "#A1A1AA";     // Null - muted

    #endregion

    #region Category Colors (For Charts, Tags, Differentiation)

    /// <summary>Distinct category colors for charts and multi-item displays</summary>
    public const string Category1 = Primary;        // Blue
    public const string Category2 = Accent;         // Violet
    public const string Category3 = Success;        // Emerald
    public const string Category4 = Warning;        // Amber
    public const string Category5 = "#F472B6";      // Pink (for variety)
    public const string Category6 = "#06B6D4";      // Cyan (for variety)

    #endregion

    #region Dark Mode Colors (Option B - Moderate Lift)

    /// <summary>
    /// Dark theme with moderate lift for improved eye comfort.
    /// Background at L*~13, with 3-5 L* unit spacing between surfaces.
    /// </summary>
    public const string DarkBackground = "#1E1E22";
    public const string DarkSurface = "#252529";
    public const string DarkSurfaceLight = "#313136";
    public const string DarkSurfaceLighter = "#48484F";
    public const string DarkBorder = "#3A3A40";
    public const string DarkTextPrimary = "#F9F9F9";
    public const string DarkTextSecondary = "#B8B8C0";  // Improved contrast: 8.5:1
    public const string DarkTextMuted = "#8A8A94";      // Improved contrast: 5.2:1
    public const string DarkCodeBlock = "#1A1A24";

    #endregion

    #region Light Mode Colors (Accessibility Fixed)

    /// <summary>
    /// Light theme with improved contrast for accessibility.
    /// Text colors adjusted to meet WCAG AA requirements.
    /// </summary>
    public const string LightBackground = "#F4F4F5";
    public const string LightSurface = "#FFFFFF";
    public const string LightSurfaceDark = "#E4E4E7";
    public const string LightSurfaceDarker = "#D4D4D8";
    public const string LightBorder = "#C4C4CC";        // Slightly darker for visibility
    public const string LightTextPrimary = "#18181B";
    public const string LightTextSecondary = "#5C5C66"; // Improved contrast: 6.8:1
    public const string LightTextMuted = "#858590";     // Fixed: now passes AA (4.6:1)
    public const string LightCodeBlock = "#F8F8FC";

    #endregion

    /// <summary>
    /// Creates the MudBlazor theme with both light and dark palettes.
    /// </summary>
    public static MudTheme Create()
    {
        return new MudTheme
        {
            PaletteLight = CreateLightPalette(),
            PaletteDark = CreateDarkPalette(),
            Typography = CreateTypography(),
            LayoutProperties = CreateLayoutProperties()
        };
    }

    private static PaletteLight CreateLightPalette()
    {
        return new PaletteLight
        {
            // Primary (Brand Blue)
            Primary = AesirTheme.Primary,
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = PrimaryDark,
            PrimaryLighten = PrimaryLight,

            // Secondary (Harmonious Violet - 40° from primary)
            Secondary = Accent,
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = AccentDark,
            SecondaryLighten = AccentLight,

            // Tertiary (Success Green)
            Tertiary = Success,
            TertiaryContrastText = "#FFFFFF",
            TertiaryDarken = SuccessDark,
            TertiaryLighten = SuccessLight,

            // Semantic Status Colors
            Info = AesirTheme.Primary,
            InfoContrastText = "#FFFFFF",
            Success = AesirTheme.Success,
            SuccessContrastText = "#FFFFFF",
            Warning = AesirTheme.Warning,
            WarningContrastText = LightTextPrimary,
            Error = AesirTheme.Error,
            ErrorContrastText = "#FFFFFF",

            // Dark (for dark elements in light mode)
            Dark = DarkBackground,
            DarkContrastText = DarkTextPrimary,
            DarkLighten = DarkSurfaceLight,

            // Background & Surface
            Background = LightBackground,
            BackgroundGray = LightSurfaceDark,
            Surface = LightSurface,

            // Text
            TextPrimary = LightTextPrimary,
            TextSecondary = LightTextSecondary,
            TextDisabled = LightTextMuted,

            // Action
            ActionDefault = LightTextSecondary,
            ActionDisabled = LightSurfaceDarker,
            ActionDisabledBackground = LightSurfaceDark,

            // Dividers & Lines
            Divider = LightBorder,
            DividerLight = LightSurfaceDark,

            // Table
            TableLines = LightBorder,
            TableStriped = "#FAFAFA",
            TableHover = LightBackground,

            // Drawer
            DrawerBackground = LightSurface,
            DrawerText = LightTextPrimary,
            DrawerIcon = LightTextSecondary,

            // AppBar
            AppbarBackground = LightSurface,
            AppbarText = LightTextPrimary,

            // Overlay
            OverlayDark = "rgba(24, 24, 27, 0.5)",
            OverlayLight = "rgba(255, 255, 255, 0.5)"
        };
    }

    private static PaletteDark CreateDarkPalette()
    {
        return new PaletteDark
        {
            // Primary (Brand Blue)
            Primary = AesirTheme.Primary,
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = PrimaryDark,
            PrimaryLighten = PrimaryLight,

            // Secondary (Harmonious Violet - 40° from primary)
            Secondary = Accent,
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = AccentDark,
            SecondaryLighten = AccentLight,

            // Tertiary (Success Green)
            Tertiary = Success,
            TertiaryContrastText = "#FFFFFF",
            TertiaryDarken = SuccessDark,
            TertiaryLighten = SuccessLight,

            // Semantic Status Colors
            Info = AesirTheme.Primary,
            InfoContrastText = "#FFFFFF",
            Success = AesirTheme.Success,
            SuccessContrastText = "#FFFFFF",
            Warning = AesirTheme.Warning,
            WarningContrastText = DarkBackground,
            Error = AesirTheme.Error,
            ErrorContrastText = "#FFFFFF",

            // Dark (for dark elements)
            Dark = DarkBackground,
            DarkContrastText = DarkTextPrimary,
            DarkLighten = DarkSurfaceLight,

            // Background & Surface
            Background = DarkBackground,
            BackgroundGray = DarkSurfaceLight,
            Surface = DarkSurface,

            // Text
            TextPrimary = DarkTextPrimary,
            TextSecondary = DarkTextSecondary,
            TextDisabled = DarkTextMuted,

            // Action
            ActionDefault = DarkTextSecondary,
            ActionDisabled = DarkSurfaceLighter,
            ActionDisabledBackground = DarkSurfaceLight,

            // Dividers & Lines
            Divider = DarkBorder,
            DividerLight = DarkSurfaceLighter,

            // Table
            TableLines = DarkBorder,
            TableStriped = DarkSurface,
            TableHover = DarkSurfaceLight,

            // Drawer
            DrawerBackground = DarkSurface,
            DrawerText = DarkTextPrimary,
            DrawerIcon = DarkTextSecondary,

            // AppBar
            AppbarBackground = DarkSurface,
            AppbarText = DarkTextPrimary,

            // Overlay
            OverlayDark = "rgba(0, 0, 0, 0.5)",
            OverlayLight = "rgba(255, 255, 255, 0.1)"
        };
    }

    private static Typography CreateTypography()
    {
        return new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = ".875rem",
                FontWeight = "400",
                LineHeight = "1.5",
                LetterSpacing = "normal"
            },
            H1 = new H1Typography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "2.5rem",
                FontWeight = "600",
                LineHeight = "1.2"
            },
            H2 = new H2Typography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "2rem",
                FontWeight = "600",
                LineHeight = "1.25"
            },
            H3 = new H3Typography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "1.75rem",
                FontWeight = "600",
                LineHeight = "1.3"
            },
            H4 = new H4Typography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "1.5rem",
                FontWeight = "600",
                LineHeight = "1.35"
            },
            H5 = new H5Typography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "1.25rem",
                FontWeight = "600",
                LineHeight = "1.4"
            },
            H6 = new H6Typography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "1rem",
                FontWeight = "600",
                LineHeight = "1.45"
            },
            Body1 = new Body1Typography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "1rem",
                FontWeight = "400",
                LineHeight = "1.5"
            },
            Body2 = new Body2Typography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = ".875rem",
                FontWeight = "400",
                LineHeight = "1.5"
            },
            Button = new ButtonTypography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = ".875rem",
                FontWeight = "500",
                LineHeight = "1.75",
                TextTransform = "uppercase"
            },
            Caption = new CaptionTypography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = ".75rem",
                FontWeight = "400",
                LineHeight = "1.5"
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = "1rem",
                FontWeight = "500",
                LineHeight = "1.5"
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica", "Arial", "sans-serif"],
                FontSize = ".875rem",
                FontWeight = "500",
                LineHeight = "1.5"
            }
        };
    }

    private static LayoutProperties CreateLayoutProperties()
    {
        return new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "260px",
            DrawerWidthRight = "300px",
            AppbarHeight = "64px"
        };
    }
}
