namespace BugTestManager.Domain.Enums;

public enum BugStatus
{
    Open = 0,
    InProgress = 1,
    Fixed = 2,
    ReadyForRetest = 3,
    Reopened = 4,
    Closed = 5,
    Rejected = 6
}
