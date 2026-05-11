using BugTestManager.Application.Abstractions;
using BugTestManager.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BugTestManager.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string? databasePath = null,
        string? attachmentRootPath = null)
    {
        var resolvedDatabasePath = databasePath ?? DatabasePaths.GetDefaultDatabasePath();
        var databaseDirectory = Path.GetDirectoryName(resolvedDatabasePath);

        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        services.AddDbContextFactory<BugTestManagerDbContext>(options =>
            options.UseSqlite($"Data Source={resolvedDatabasePath}"));

        services.AddSingleton<IDatabaseInitializer, SqliteDatabaseInitializer>();
        services.AddSingleton<ITestSuiteCatalogService, SqliteTestSuiteCatalogService>();
        services.AddSingleton<ITestSuiteManagementService, SqliteTestSuiteManagementService>();
        services.AddSingleton<ICustomFieldDefinitionService, SqliteCustomFieldDefinitionService>();
        services.AddSingleton<ITestSessionService, SqliteTestSessionService>();
        services.AddSingleton<IBugReportService, SqliteBugReportService>();
        services.AddSingleton<IDiscussionService, SqliteDiscussionService>();
        services.AddSingleton<IAttachmentService>(_ =>
            new SqliteAttachmentService(
                _.GetRequiredService<IDbContextFactory<BugTestManagerDbContext>>(),
                attachmentRootPath));

        return services;
    }
}
