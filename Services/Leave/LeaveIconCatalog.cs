namespace HRM.Services.Leave;

using MudBlazor;

// Curated name -> MudBlazor icon lookup for Lve_LeaveType.IconName. Kept as a
// fixed picker list (rather than letting HR type any string) so the admin
// page can offer a dropdown instead of an error-prone free-text icon name,
// while still storing just the plain name string in the DB — the actual SVG
// constant is only resolved here, at render time.
public static class LeaveIconCatalog
{
    public const string DefaultIconName = "EventNote";

    public static readonly (string Name, string Icon, string LabelTh)[] Options =
    [
        ("EventNote", Icons.Material.Filled.EventNote, "ทั่วไป"),
        ("LocalHospital", Icons.Material.Filled.LocalHospital, "ป่วย/การแพทย์"),
        ("Event", Icons.Material.Filled.Event, "กิจธุระ"),
        ("BeachAccess", Icons.Material.Filled.BeachAccess, "พักร้อน"),
        ("ChildCare", Icons.Material.Filled.ChildCare, "คลอดบุตร"),
        ("FamilyRestroom", Icons.Material.Filled.FamilyRestroom, "ดูแลครอบครัว"),
        ("MilitaryTech", Icons.Material.Filled.MilitaryTech, "ราชการทหาร"),
        ("School", Icons.Material.Filled.School, "ฝึกอบรม"),
        ("SelfImprovement", Icons.Material.Filled.SelfImprovement, "บวช/ศาสนา"),
        ("Favorite", Icons.Material.Filled.Favorite, "สมรส"),
        ("LocalFlorist", Icons.Material.Filled.LocalFlorist, "งานศพ/ไว้อาลัย"),
        ("DirectionsCar", Icons.Material.Filled.DirectionsCar, "ดูงาน/เดินทาง"),
        ("HelpOutline", Icons.Material.Filled.HelpOutline, "อื่นๆ"),
    ];

    public static string Resolve(string? iconName)
    {
        var match = Options.FirstOrDefault(o => o.Name == iconName);
        return match.Icon ?? Options.First(o => o.Name == DefaultIconName).Icon;
    }
}
