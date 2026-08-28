using System.Text.Json;
using HRM.Models;

namespace HRM.Services.Org;

// Turns a raw com_organization AuditLog row (OldValuesJson/NewValuesJson —
// a full column dump, keyed by CLR property name per HRMContext.Audit.cs)
// into a short Thai sentence describing what actually mattered, instead of
// showing the raw JSON diff. Only looks at the fields the org-chart history
// page cares about (parent_code/approver/name/isActive) — everything else
// in the dump is ignored.
public static class OrgChangeAuditFormatter
{
    public static string Describe(AuditLog log)
    {
        var old = Deserialize(log.OldValuesJson);
        var @new = Deserialize(log.NewValuesJson);

        if (log.Action == AuditActionType.Create)
            return $"สร้างหน่วยงานใหม่ {Get(@new, "name")} (รหัส {Get(@new, "code")})";
        if (log.Action == AuditActionType.Delete)
            return $"ลบหน่วยงาน {Get(old, "name")} (รหัส {Get(old, "code")})";

        var clauses = new List<string>();

        if (Changed(old, @new, "parent_code"))
            clauses.Add($"ย้าย {Get(@new, "name") ?? Get(old, "name")} จากสังกัด {Get(old, "parent_code") ?? "(สูงสุด)"} ไป {Get(@new, "parent_code") ?? "(สูงสุด)"}");

        if (Changed(old, @new, "approver_empid") || Changed(old, @new, "approver_name"))
            clauses.Add($"เปลี่ยนหัวหน้า {Get(@new, "name") ?? Get(old, "name")} จาก {Get(old, "approver_name") ?? "(ว่าง)"} เป็น {Get(@new, "approver_name") ?? "(ว่าง)"}");

        if (Changed(old, @new, "name"))
            clauses.Add($"เปลี่ยนชื่อจาก {Get(old, "name")} เป็น {Get(@new, "name")}");

        if (Changed(old, @new, "isActive"))
            clauses.Add($"เปลี่ยนสถานะเป็น {(string.Equals(Get(@new, "isActive"), "true", StringComparison.OrdinalIgnoreCase) ? "ใช้งาน" : "ไม่ใช้งาน")}");

        return clauses.Count > 0
            ? string.Join(" / ", clauses)
            : $"แก้ไขข้อมูล {Get(@new, "name") ?? Get(old, "name")} (รายละเอียดอื่นๆ)";
    }

    // Same wording as Describe(AuditLog), but for a still-pending/not-yet-applied
    // Org_OrganizationChangeRequest — used by both the history page's "pending"
    // section and the request detail page, so a request reads identically
    // whether you're looking at it before or after it lands in AuditLog.
    public static string DescribeRequest(Org_OrganizationChangeRequest req) => req.ChangeType switch
    {
        OrgOrganizationChangeType.NewOrganization =>
            $"สร้างหน่วยงานใหม่ {req.NewName} (รหัส {req.NewCode})" + (string.IsNullOrEmpty(req.NewParentCode) ? " เป็นสังกัดสูงสุด" : $" ภายใต้สังกัด {req.NewParentCode}"),
        OrgOrganizationChangeType.ChangeParent =>
            $"ย้าย {req.TargetOrganizationCode} จากสังกัด {req.OldParentCode ?? "(สูงสุด)"} ไป {req.NewParentCode ?? "(สูงสุด)"}",
        OrgOrganizationChangeType.ChangeApprover =>
            $"เปลี่ยนหัวหน้า {req.TargetOrganizationCode} จาก {req.OldApproverName ?? "(ว่าง)"} เป็น {req.NewApproverName ?? "(ว่าง)"}",
        _ => "คำขอเปลี่ยนแปลงผังองค์กร",
    };

    // หัวหน้าแผนก (boss_name) and ผู้อนุมัติ (approver_name) are genuinely
    // separate fields on com_organization — an "acting" approver set via the
    // workflow-gated change-boss flow can differ from the real department
    // head. Shared by the tree view (OrganizationAdmin.razor) and the detail
    // page (OrganizationDetail.razor) so both describe the same org the same
    // way. Returns null when neither is set.
    public static string? DescribeBossApprover(com_organization org)
    {
        var boss = string.IsNullOrWhiteSpace(org.boss_name) ? null : org.boss_name;
        var approver = string.IsNullOrWhiteSpace(org.approver_name) ? null : org.approver_name;

        if (boss is null && approver is null) return null;
        if (boss is not null && approver is not null)
            return boss == approver ? $"หัวหน้า/ผู้อนุมัติ: {boss}" : $"หัวหน้า: {boss} · ผู้อนุมัติ: {approver}";
        return boss is not null ? $"หัวหน้า: {boss}" : $"ผู้อนุมัติ: {approver}";
    }

    private static Dictionary<string, JsonElement>? Deserialize(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? Get(Dictionary<string, JsonElement>? dict, string key)
    {
        if (dict is null || !dict.TryGetValue(key, out var el)) return null;
        if (el.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
        return el.ValueKind == JsonValueKind.String ? el.GetString() : el.ToString();
    }

    private static bool Changed(Dictionary<string, JsonElement>? old, Dictionary<string, JsonElement>? @new, string key)
        => Get(old, key) != Get(@new, key);
}
