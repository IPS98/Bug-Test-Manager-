using BugTestManager.Domain.Enums;

namespace BugTestManager.Application.Abstractions;

public interface IUserContext
{
    string UserName { get; }

    UserRole Role { get; }
}
