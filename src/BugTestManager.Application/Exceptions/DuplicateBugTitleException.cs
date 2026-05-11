namespace BugTestManager.Application.Exceptions;

public sealed class DuplicateBugTitleException(string title)
    : InvalidOperationException($"A bug with title '{title}' already exists.")
{
    public string Title { get; } = title;
}
