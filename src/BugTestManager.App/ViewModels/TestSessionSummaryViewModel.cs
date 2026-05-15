namespace BugTestManager.App.ViewModels;

public sealed class TestSessionSummaryViewModel
{
    public TestSessionSummaryViewModel(
        Guid id,
        Guid testSuiteId,
        string name,
        string testSuiteName,
        string? revisionName,
        string testedVersion,
        string buildNumber,
        string createdBy,
        DateTimeOffset createdAt,
        int sectionCount,
        int testCaseCount,
        int stepCount)
    {
        Id = id;
        TestSuiteId = testSuiteId;
        Name = name;
        TestSuiteName = testSuiteName;
        RevisionName = revisionName;
        TestedVersion = testedVersion;
        BuildNumber = buildNumber;
        CreatedBy = createdBy;
        CreatedAt = createdAt;
        SectionCount = sectionCount;
        TestCaseCount = testCaseCount;
        StepCount = stepCount;
    }

    public Guid Id { get; }

    public Guid TestSuiteId { get; }

    public string Name { get; }

    public string TestSuiteName { get; }

    public string? RevisionName { get; }

    public string TestedVersion { get; }

    public string BuildNumber { get; }

    public string CreatedBy { get; }

    public DateTimeOffset CreatedAt { get; }

    public int SectionCount { get; }

    public int TestCaseCount { get; }

    public int StepCount { get; }

    public string RevisionDisplay => string.IsNullOrWhiteSpace(RevisionName) ? "No revision" : RevisionName;

    public string VersionDisplay => string.IsNullOrWhiteSpace(TestedVersion) ? "Version: -" : $"Version: {TestedVersion}";

    public string BuildDisplay => string.IsNullOrWhiteSpace(BuildNumber) ? "Build: -" : $"Build: {BuildNumber}";

    public string RevisionHeaderDisplay => $"Revision: {RevisionDisplay}";

    public string CreatedAtDisplay => CreatedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string SizeDisplay => $"{SectionCount} sections, {TestCaseCount} cases, {StepCount} checks";
}
