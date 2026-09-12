namespace Advance.Workflow.Contracts;

// ============================================================================
//  Copied from Services/Workflow/WorkflowDocumentWriteback.cs (WorkflowClosedEvent,
//  WorkflowCloseOutcome, IWorkflowDocumentHandler) — the write-back contract
//  the engine uses to tell a document-owning module "your job just closed".
//
//  Per the extraction plan (see ../EXTRACTION-PLAN.md, section "17 write-back
//  handlers"), the 17 concrete handlers registered today in
//  WorkflowDocumentHandlers.AddWorkflowDocumentHandlers stay in HRM — they
//  belong to the document-owning modules (Att_*, Idp_*, Perf_*, Pay_*, ...),
//  not to the engine. Only this interface + the two small records move here,
//  so Advance.Workflow.Engine can raise the event without knowing anything
//  about HRM's modules, and HRM's own dispatcher
//  (WorkflowDocumentWriteback/WorkflowDocumentHandlers) keeps mapping
//  reftable -> handler exactly as it does today.
// ============================================================================

public enum WorkflowCloseOutcome { Approved, Declined, Cancelled }

public sealed record WorkflowClosedEvent(
    long JobMasterId, string WorkflowCode, string RefTable, string RefId,
    WorkflowCloseOutcome Outcome, long? ActorUserId, string? Reason);

public interface IWorkflowDocumentHandler
{
    string RefTable { get; }
    Task OnClosedAsync(IServiceProvider services, WorkflowClosedEvent e, CancellationToken ct);
}
