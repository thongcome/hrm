namespace HRM.Models;

// PendingApproval = 4 (not 2) on purpose — existing rows store the old int
// values, so new members are appended with explicit values, never renumbered.
public enum ArticleStatus { Draft = 1, PendingApproval = 4, Published = 2, Archived = 3 }
