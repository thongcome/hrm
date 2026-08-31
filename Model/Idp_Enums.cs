namespace HRM.Models;

public enum IdpAssessmentSource { Self = 1, Manager = 2 }

public enum IdpActionStatus { NotStarted = 1, InProgress = 2, Completed = 3, Cancelled = 4 }

public enum IdpPlanStatus { Draft = 1, PendingApproval = 2, Approved = 3, Rejected = 4 }

// 70-20-10 development-method model (มาตรฐานสากลของการพัฒนาบุคลากร):
// 70% เรียนรู้จากงานจริง (on-the-job experience) / 20% โค้ช-พี่เลี้ยง
// (coaching & mentoring) / 10% อบรมทางการ (formal training).
public enum IdpDevelopmentMethod { OnTheJob = 1, Coaching = 2, FormalTraining = 3 }
