namespace Advance.Workflow.Contracts;

// ============================================================================
//  IWorkflowNotifier — mirrors EmailSender's role in
//  Services/Workflow/WorkflowService.cs's NotifyRecipientsAsync (best-effort
//  "a job arrived in your queue" email) and the requester notification fired
//  when a job closes. HRM's EmailSender wraps MailKit + SMTP config; a
//  standalone Advance.Workflow deployment can implement this with whatever
//  mail/notification stack it ships with.
//
//  Deliberately just two flat strings (subject/html) rather than a template
//  name + parameters — that matches what WorkflowService already builds
//  in-line today, so the engine keeps composing its own message text and the
//  host only has to know how to actually deliver it (SMTP today, push/SMS/
//  Teams/etc. later without touching the engine).
// ============================================================================

public interface IWorkflowNotifier
{
    /// <summary>Best-effort — a failure here must never fail the workflow action that
    /// triggered it (WorkflowService.NotifyRecipientsAsync catches and logs
    /// per-recipient exceptions for exactly this reason; implementers should too).</summary>
    Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct = default);
}
