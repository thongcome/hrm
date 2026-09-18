namespace HRM.Services.Pay;

public enum PayrollAction
{
    Calculate,
    SubmitForReview,
    Approve,
    Post,
    MarkPaid,
    Cancel
    // CreateAdjustment / Reverse were removed (CEO, 17 ก.ย. 2569): a posted or paid
    // run is never corrected by a whole-period negative run. Whoever was paid wrong
    // gets a one-off earning/deduction (Pay_AdhocPayItem) in the next period.
}
