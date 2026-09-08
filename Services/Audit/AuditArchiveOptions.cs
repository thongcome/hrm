namespace HRM.Services.Audit;

// Where archived audit files go, and how much stays in the database.
// CEO, 8 ก.ย. 2569: "audit log ... คุณทำเป็น file ได้ไหม เอาไปใส่ใน path ใน
// file server" — the AuditLog table was 445 MB of a 912 MB database.
public sealed class AuditArchiveOptions
{
    public const string SectionName = "AuditArchive";

    // UNC path or local folder, e.g. \fileserver\hrm\audit. Empty disables
    // archiving entirely (the admin page then explains why it can't run).
    public string Path { get; set; } = "";

    // Rows younger than this stay in the database no matter what. Never set
    // below 90: พ.ร.บ. คอมพิวเตอร์ มาตรา 26 requires computer-traffic data be
    // retained at least 90 days, and the archive step DELETES from the table
    // once the file is written and verified. Archived files still satisfy the
    // retention requirement, but keeping a 90-day live window means the
    // normal admin screens can answer a request without restoring anything.
    public int KeepDaysInDatabase { get; set; } = 90;

    // Rows per batch when reading out and deleting. Keeps memory flat and
    // each transaction short on a table that can hold millions of rows.
    public int BatchSize { get; set; } = 5000;
}
