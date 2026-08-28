using MudBlazor;

namespace HRM.Components.Layout;

// Small, hand-picked set of selectable color themes — same idea as a design
// system's swappable palette files, but delivered through MudBlazor's own
// theming (MudThemeProvider + MudTheme), the mechanism every page in this
// app already runs on, rather than a separate hand-rolled CSS layer. Each
// theme defines both a light and a dark palette using the same property set
// HumanOkTheme.cs already established, so ThemeState/ThemeService can flip
// IsDarkMode independently of which color theme is selected.
public static class ThemeCatalog
{
    // Shared across every theme — font choice and corner-radius aren't part
    // of "which color theme", so they're not duplicated per palette.
    private static readonly Typography SharedTypography = new()
    {
        Default = new DefaultTypography
        {
            FontFamily = ["Segoe UI", "Noto Sans Thai", "Leelawadee UI", "Roboto", "Helvetica", "Arial", "sans-serif"],
        },
        H6 = new H6Typography { FontWeight = "700" },
        Button = new ButtonTypography { FontWeight = "600", TextTransform = "none" },
    };

    private static readonly LayoutProperties SharedLayout = new()
    {
        DefaultBorderRadius = "10px",
    };

    public record ThemeOption(string Id, string Label, MudTheme Theme);

    public static readonly ThemeOption[] Options =
    [
        new("humanok", "HumanOk (ค่าเริ่มต้น)", HumanOkTheme.Theme),

        new("blue", "มหาสมุทร (Blue)", new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#1565C0",
                PrimaryDarken = "#0D47A1",
                Secondary = "#37474F",
                Background = "#F5F8FC",
                Surface = "#FFFFFF",
                AppbarBackground = "#0D47A1",
                AppbarText = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#1A237E",
                TextPrimary = "#1A2027",
                TextSecondary = "#5A6472",
                ActionDefault = "#8895A7",
                Divider = "#E3E9F0",
                LinesDefault = "#E3E9F0",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#5C9CE6",
                PrimaryDarken = "#1565C0",
                Secondary = "#8CA6C7",
                Background = "#0F1720",
                Surface = "#17212E",
                AppbarBackground = "#0A121C",
                AppbarText = "#FFFFFF",
                DrawerBackground = "#17212E",
                DrawerText = "#E7ECF3",
                TextPrimary = "#E7ECF3",
                TextSecondary = "#A6B2C2",
                ActionDefault = "#8896A8",
                Divider = "#28323F",
                LinesDefault = "#28323F",
            },
            Typography = SharedTypography,
            LayoutProperties = SharedLayout,
        }),

        new("green", "ป่าไม้ (Green)", new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#2E7D32",
                PrimaryDarken = "#1B5E20",
                Secondary = "#4E5D52",
                Background = "#F5FAF5",
                Surface = "#FFFFFF",
                AppbarBackground = "#1B5E20",
                AppbarText = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#1B3A1E",
                TextPrimary = "#1D251E",
                TextSecondary = "#5A6B5C",
                ActionDefault = "#87968A",
                Divider = "#E1EBE2",
                LinesDefault = "#E1EBE2",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#66BB6A",
                PrimaryDarken = "#2E7D32",
                Secondary = "#93A697",
                Background = "#10160F",
                Surface = "#182018",
                AppbarBackground = "#0D130C",
                AppbarText = "#FFFFFF",
                DrawerBackground = "#182018",
                DrawerText = "#E4EDE4",
                TextPrimary = "#E4EDE4",
                TextSecondary = "#A8B5A9",
                ActionDefault = "#8A988B",
                Divider = "#28312A",
                LinesDefault = "#28312A",
            },
            Typography = SharedTypography,
            LayoutProperties = SharedLayout,
        }),

        new("purple", "ราชวงศ์ (Purple)", new MudTheme
        {
            PaletteLight = new PaletteLight
            {
                Primary = "#6A3FA0",
                PrimaryDarken = "#4E2A79",
                Secondary = "#4A4458",
                Background = "#F8F5FB",
                Surface = "#FFFFFF",
                AppbarBackground = "#4E2A79",
                AppbarText = "#FFFFFF",
                DrawerBackground = "#FFFFFF",
                DrawerText = "#382253",
                TextPrimary = "#231F2B",
                TextSecondary = "#5F5768",
                ActionDefault = "#8F869B",
                Divider = "#EBE5F2",
                LinesDefault = "#EBE5F2",
            },
            PaletteDark = new PaletteDark
            {
                Primary = "#A487D3",
                PrimaryDarken = "#6A3FA0",
                Secondary = "#B0A6C0",
                Background = "#160F1D",
                Surface = "#20172A",
                AppbarBackground = "#120C18",
                AppbarText = "#FFFFFF",
                DrawerBackground = "#20172A",
                DrawerText = "#EBE5F2",
                TextPrimary = "#EBE5F2",
                TextSecondary = "#B8AEC5",
                ActionDefault = "#968CA3",
                Divider = "#2E2438",
                LinesDefault = "#2E2438",
            },
            Typography = SharedTypography,
            LayoutProperties = SharedLayout,
        }),
    ];

    public static ThemeOption GetById(string? id) =>
        Options.FirstOrDefault(o => o.Id == id) ?? Options[0];
}
