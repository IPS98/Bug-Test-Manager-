using BugTestManager.Application.Abstractions;
using BugTestManager.Domain.Enums;

namespace BugTestManager.Infrastructure.UserContext;

public sealed class WindowsUserContext : IUserContext
{
    public string UserName => Environment.UserName;

    public UserRole Role => UserRole.Admin;
}
