namespace HRM.Models;

public enum ProvidentFundRequestStatus { PendingApproval = 1, Approved = 2, Rejected = 3 }

public enum ProvidentFundMatrixResultType { Fixed = 1, MatchEmployeeRate = 2 }

// How an exit reason affects the employer-contribution vesting outcome:
// UseNormalTier defers to Pay_ProvidentFundVestingTier (the years-of-service
// table) exactly as before this feature existed; ForceZero/ForceFull
// override that table outright regardless of tenure — matching real fund
// regulations where e.g. death/retirement always pay 100% and fraud always
// forfeits 100%, independent of how long the person had been a member.
public enum ProvidentFundExitVestingOverride { UseNormalTier = 1, ForceZero = 2, ForceFull = 3 }

public enum ProvidentFundCalculationType { RateMatrix = 1, VestingExit = 2 }
