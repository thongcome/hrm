using MudBlazor;

namespace HRM.Components.Layout;

// Palette pulled from the HumanOk logo (wwwroot/images/humanok3_small.jpg):
// the orange-red figures/heart mark as Primary, the navy wordmark as the
// dark surface tone. Kept in one place so MainLayout and any future page
// that needs a MudTheme reference the same source instead of guessing hex
// values (the old .header-orange/#FF7F50 in app.css was picked ad hoc and
// didn't match the logo at all).
public static class HumanOkTheme
{
    public static readonly MudTheme Theme = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = "#E8491D",
            PrimaryDarken = "#C43A15",
            Secondary = "#1D2B4F",
            Background = "#FAF7F5",
            Surface = "#FFFFFF",
            AppbarBackground = "#1D2B4F",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FFFFFF",
            DrawerText = "#1D2B4F",
            TextPrimary = "#231F20",
            TextSecondary = "#5B5651",
            ActionDefault = "#8A8580",
            Divider = "#EDE7E2",
            LinesDefault = "#EDE7E2",
        },
        PaletteDark = new PaletteDark
        {
            Primary = "#FF6B3D",
            PrimaryDarken = "#E8491D",
            Secondary = "#8CA0D6",
            Background = "#12182A",
            Surface = "#1B2440",
            AppbarBackground = "#0F1526",
            AppbarText = "#FFFFFF",
            DrawerBackground = "#1B2440",
            DrawerText = "#E7E9F0",
            TextPrimary = "#EDEFF5",
            TextSecondary = "#A7ADC2",
            ActionDefault = "#9098B0",
            Divider = "#2A3358",
            LinesDefault = "#2A3358",
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Segoe UI", "Noto Sans Thai", "Leelawadee UI", "Roboto", "Helvetica", "Arial", "sans-serif"],
            },
            H6 = new H6Typography { FontWeight = "700" },
            Button = new ButtonTypography { FontWeight = "600", TextTransform = "none" },
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "10px",
        },
    };
}
