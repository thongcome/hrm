namespace Advance.Host.Web.SecurityCore;

/// <summary>
/// PLACEHOLDER — reconcile before shipping. As of this scaffold (Phase 0, see
/// DESIGN-NOTES.md), D:\GitWorkspace\HRM\src\Advance.SecurityCore\ does not
/// exist on disk yet, so this project cannot reference its real
/// <c>AddAdvanceSecurityCore</c>/<c>UseAdvanceSecurityCore</c> extension
/// methods (another agent is scaffolding that project in a separate,
/// isolated worktree per this task's instructions).
///
/// This interface stands in for "SecurityCore is wired into DI" so
/// <see cref="ServiceCollectionExtensions.AddAdvanceHost"/> has something
/// concrete to check/compose with instead of silently assuming Identity
/// exists. Once Advance.SecurityCore's real extension methods exist:
///   1. Add a ProjectReference to Advance.SecurityCore.Web (or wherever
///      AddAdvanceSecurityCore lives).
///   2. Delete this file.
///   3. Change AddAdvanceHost's signature/body to call the real
///      AddAdvanceSecurityCore(...) directly (or require the caller to call
///      it first and just verify registration, whichever SecurityCore's own
///      design settled on — see DESIGN-NOTES.md "Reconciling with
///      Advance.SecurityCore" for the two options and why this scaffold
///      could not decide between them without seeing the real signature).
/// </summary>
public interface IAdvanceSecurityCoreMarker
{
    /// <summary>True once a consuming Program.cs has registered SecurityCore (real or, today, this placeholder).</summary>
    bool IsRegistered { get; }
}

/// <summary>No-op default so AddAdvanceHost can resolve <see cref="IAdvanceSecurityCoreMarker"/> even if the consumer never registers a real one — logged as a warning, not a hard failure, since Phase 0 has nothing real to plug in yet.</summary>
internal sealed class NullAdvanceSecurityCoreMarker : IAdvanceSecurityCoreMarker
{
    public bool IsRegistered => false;
}
