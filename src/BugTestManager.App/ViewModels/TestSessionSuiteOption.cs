namespace BugTestManager.App.ViewModels;

public sealed class TestSessionSuiteOption
{
    public TestSessionSuiteOption(
        Guid id,
        string name,
        bool revisionIsRequired,
        IReadOnlyList<TestSessionRevisionOption> revisions)
    {
        Id = id;
        Name = name;
        RevisionIsRequired = revisionIsRequired;
        Revisions = revisions;
    }

    public Guid Id { get; }

    public string Name { get; }

    public bool RevisionIsRequired { get; }

    public IReadOnlyList<TestSessionRevisionOption> Revisions { get; }
}

public sealed record TestSessionRevisionOption(Guid? Id, string Name);
