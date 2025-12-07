using MudBlazor;

namespace Aesir.Client.Web.Infrastructure.Theme;

/// <summary>
/// AESIR application theme definition.
/// Colors based on the AESIR landing page design.
/// </summary>
public static class AesirTheme
{
    // Brand Colors
    public const string AccentBlue = "#54A9FF";
    public const string AccentBlueHover = "#6BB8FF";
    public const string AccentBlueDark = "#2C649F";

    // Dark Mode Colors
    public const string DarkBackground = "#16161A";
    public const string DarkSurface = "#1C1C21";
    public const string DarkSurfaceLight = "#27272A";
    public const string DarkBorder = "#27272A";
    public const string DarkTextPrimary = "#F9F9F9";
    public const string DarkTextSecondary = "#A1A1AA";

    // Light Mode Colors
    public const string LightBackground = "#F4F4F5";
    public const string LightSurface = "#FFFFFF";
    public const string LightSurfaceDark = "#E4E4E7";
    public const string LightBorder = "#D4D4D8";
    public const string LightTextPrimary = "#18181B";
    public const string LightTextSecondary = "#71717A";

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
            // Primary (Accent Blue)
            Primary = AccentBlue,
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = AccentBlueDark,
            PrimaryLighten = AccentBlueHover,

            // Secondary (Muted Blue)
            Secondary = AccentBlueDark,
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = "#1E4A75",
            SecondaryLighten = "#3A7AC4",

            // Tertiary
            Tertiary = "#10B981", // Green for success/tertiary actions
            TertiaryContrastText = "#FFFFFF",

            // Info, Success, Warning, Error
            Info = AccentBlue,
            InfoContrastText = "#FFFFFF",
            Success = "#10B981",
            SuccessContrastText = "#FFFFFF",
            Warning = "#F59E0B",
            WarningContrastText = "#18181B",
            Error = "#EF4444",
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
            TextDisabled = "#A1A1AA",

            // Action
            ActionDefault = LightTextSecondary,
            ActionDisabled = "#D4D4D8",
            ActionDisabledBackground = "#E4E4E7",

            // Dividers & Lines
            Divider = LightBorder,
            DividerLight = "#E4E4E7",

            // Table
            TableLines = LightBorder,
            TableStriped = "#FAFAFA",
            TableHover = "#F4F4F5",

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
            // Primary (Accent Blue)
            Primary = AccentBlue,
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = AccentBlueDark,
            PrimaryLighten = AccentBlueHover,

            // Secondary (Muted Blue)
            Secondary = AccentBlueDark,
            SecondaryContrastText = "#FFFFFF",
            SecondaryDarken = "#1E4A75",
            SecondaryLighten = "#3A7AC4",

            // Tertiary
            Tertiary = "#10B981",
            TertiaryContrastText = "#FFFFFF",

            // Info, Success, Warning, Error
            Info = AccentBlue,
            InfoContrastText = "#FFFFFF",
            Success = "#10B981",
            SuccessContrastText = "#FFFFFF",
            Warning = "#F59E0B",
            WarningContrastText = "#18181B",
            Error = "#EF4444",
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
            TextDisabled = "#52525B",

            // Action
            ActionDefault = DarkTextSecondary,
            ActionDisabled = "#3F3F46",
            ActionDisabledBackground = "#27272A",

            // Dividers & Lines
            Divider = DarkBorder,
            DividerLight = "#3F3F46",

            // Table
            TableLines = DarkBorder,
            TableStriped = "#1C1C21",
            TableHover = "#27272A",

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
