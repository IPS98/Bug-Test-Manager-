using System.Text.Json;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.Defaults;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteCustomFieldDefinitionService(IDbContextFactory<BugTestManagerDbContext> dbContextFactory)
    : ICustomFieldDefinitionService
{
    public IReadOnlyList<CustomFieldDefinitionItem> GetDefinitions(Guid? projectId = null)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var resolvedProjectId = ResolveProjectId(projectId);

        return dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .Include(field => field.Scopes)
            .Where(field => field.ProjectId == resolvedProjectId)
            .OrderBy(field => field.TargetEntityType)
            .ThenBy(field => field.SortOrder)
            .ThenBy(field => field.Name)
            .ToList()
            .Select(MapDefinition)
            .ToList();
    }

    public Guid CreateDefinition(CreateCustomFieldDefinitionRequest request)
    {
        var name = Require(request.Name, "Field name");
        var options = NormalizeOptions(request.Options);
        var scopes = NormalizeScopes(request.Scopes, request.ScopeType, request.ScopeEntityId, request.ScopeDisplayName);
        var projectId = ResolveProjectId(request.ProjectId);

        if (RequiresOptions(request.FieldType) && options.Count == 0)
        {
            throw new ArgumentException("Select fields require at least one option.", nameof(request));
        }

        using var dbContext = dbContextFactory.CreateDbContext();
        var duplicateExists = dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .Include(field => field.Scopes)
            .Where(field =>
                field.ProjectId == projectId
                && field.TargetEntityType == request.TargetEntityType
                && field.Name.ToUpper() == name.ToUpper()
                && field.IsActive)
            .ToList()
            .Any(field => HasConflictingScope(field, scopes));

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Field '{name}' already exists for one of the selected targets.");
        }

        var fieldDefinitionId = Guid.NewGuid();
        dbContext.CustomFieldDefinitions.Add(new CustomFieldDefinitionRecord
        {
            Id = fieldDefinitionId,
            ProjectId = projectId,
            TargetEntityType = request.TargetEntityType,
            Name = name,
            FieldType = request.FieldType,
            IsRequired = request.IsRequired,
            SortOrder = Math.Max(0, request.SortOrder),
            ScopeType = scopes[0].ScopeType,
            ScopeEntityId = scopes[0].ScopeEntityId,
            ScopeDisplayName = scopes[0].DisplayName,
            IsActive = true,
            OptionsJson = JsonSerializer.Serialize(options),
            Scopes = scopes
                .Select(scope => new CustomFieldDefinitionScopeRecord
                {
                    Id = Guid.NewGuid(),
                    FieldDefinitionId = fieldDefinitionId,
                    ScopeType = scope.ScopeType,
                    ScopeEntityId = scope.ScopeEntityId,
                    ScopeDisplayName = scope.DisplayName
                })
                .ToList()
        });

        dbContext.SaveChanges();

        return fieldDefinitionId;
    }

    public void UpdateDefinition(UpdateCustomFieldDefinitionRequest request)
    {
        var name = Require(request.Name, "Field name");
        var options = NormalizeOptions(request.Options);
        var scopes = NormalizeScopes(request.Scopes, request.ScopeType, request.ScopeEntityId, request.ScopeDisplayName);

        if (RequiresOptions(request.FieldType) && options.Count == 0)
        {
            throw new ArgumentException("Select fields require at least one option.", nameof(request));
        }

        using var dbContext = dbContextFactory.CreateDbContext();
        var field = dbContext.CustomFieldDefinitions
            .SingleOrDefault(definition => definition.Id == request.FieldDefinitionId)
            ?? throw new InvalidOperationException("Selected field definition was not found.");
        var duplicateExists = dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .Include(definition => definition.Scopes)
            .Where(definition =>
                definition.Id != request.FieldDefinitionId
                && definition.ProjectId == field.ProjectId
                && definition.TargetEntityType == request.TargetEntityType
                && definition.Name.ToUpper() == name.ToUpper()
                && definition.IsActive)
            .ToList()
            .Any(definition => HasConflictingScope(definition, scopes));

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Field '{name}' already exists for one of the selected targets.");
        }

        field.TargetEntityType = request.TargetEntityType;
        field.Name = name;
        field.FieldType = request.FieldType;
        field.IsRequired = request.IsRequired;
        field.SortOrder = Math.Max(0, request.SortOrder);
        field.ScopeType = scopes[0].ScopeType;
        field.ScopeEntityId = scopes[0].ScopeEntityId;
        field.ScopeDisplayName = scopes[0].DisplayName;
        field.OptionsJson = JsonSerializer.Serialize(options);
        dbContext.CustomFieldDefinitionScopes
            .Where(scope => scope.FieldDefinitionId == field.Id)
            .ExecuteDelete();
        foreach (var scope in scopes)
        {
            dbContext.CustomFieldDefinitionScopes.Add(new CustomFieldDefinitionScopeRecord
            {
                Id = Guid.NewGuid(),
                FieldDefinitionId = field.Id,
                ScopeType = scope.ScopeType,
                ScopeEntityId = scope.ScopeEntityId,
                ScopeDisplayName = scope.DisplayName
            });
        }

        dbContext.SaveChanges();
    }

    public void ArchiveDefinition(Guid fieldDefinitionId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var field = dbContext.CustomFieldDefinitions.SingleOrDefault(definition => definition.Id == fieldDefinitionId)
            ?? throw new InvalidOperationException("Selected field definition was not found.");

        field.IsActive = false;
        dbContext.SaveChanges();
    }

    public void DeleteDefinition(Guid fieldDefinitionId)
    {
        using var dbContext = dbContextFactory.CreateDbContext();
        var field = dbContext.CustomFieldDefinitions.SingleOrDefault(definition => definition.Id == fieldDefinitionId)
            ?? throw new InvalidOperationException("Selected field definition was not found.");

        dbContext.CustomFieldValues
            .Where(value => value.FieldDefinitionId == fieldDefinitionId)
            .ExecuteDelete();
        dbContext.CustomFieldDefinitions.Remove(field);
        dbContext.SaveChanges();
    }

    private static bool RequiresOptions(FieldType fieldType)
    {
        return fieldType is FieldType.SingleSelect or FieldType.MultiSelect;
    }

    private static List<string> NormalizeOptions(IEnumerable<string> options)
    {
        return options
            .Select(option => option.Trim())
            .Where(option => !string.IsNullOrWhiteSpace(option))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> DeserializeOptions(string optionsJson)
    {
        return JsonSerializer.Deserialize<IReadOnlyList<string>>(optionsJson) ?? [];
    }

    private static CustomFieldDefinitionItem MapDefinition(CustomFieldDefinitionRecord field)
    {
        var scopes = GetScopes(field);
        var scopeDisplayName = string.Join("; ", scopes.Select(scope => scope.DisplayName));

        return new CustomFieldDefinitionItem(
            field.Id,
            field.ProjectId,
            field.TargetEntityType,
            field.Name,
            field.FieldType,
            field.IsRequired,
            field.SortOrder,
            scopes[0].ScopeType,
            scopes[0].ScopeEntityId,
            scopeDisplayName,
            field.IsActive,
            DeserializeOptions(field.OptionsJson),
            scopes);
    }

    private static List<CustomFieldDefinitionScopeItem> GetScopes(CustomFieldDefinitionRecord field)
    {
        var scopes = field.Scopes.Count == 0
            ?
            [
                new CustomFieldDefinitionScopeItem(
                    field.ScopeType,
                    field.ScopeEntityId,
                    NormalizeScopeDisplayName(field.ScopeDisplayName))
            ]
            : field.Scopes
                .OrderBy(scope => scope.ScopeType)
                .ThenBy(scope => scope.ScopeDisplayName)
                .Select(scope => new CustomFieldDefinitionScopeItem(
                    scope.ScopeType,
                    scope.ScopeEntityId,
                    NormalizeScopeDisplayName(scope.ScopeDisplayName)))
                .ToList();

        return scopes;
    }

    private static List<CustomFieldDefinitionScopeRequest> NormalizeScopes(
        IReadOnlyCollection<CustomFieldDefinitionScopeRequest>? scopes,
        CustomFieldScopeType fallbackScopeType,
        Guid? fallbackScopeEntityId,
        string fallbackScopeDisplayName)
    {
        var source = scopes is { Count: > 0 }
            ? scopes
            :
            [
                new CustomFieldDefinitionScopeRequest(
                    fallbackScopeType,
                    fallbackScopeEntityId,
                    fallbackScopeDisplayName)
            ];

        return source
            .GroupBy(scope => new { scope.ScopeType, scope.ScopeEntityId })
            .Select(group => group.First())
            .Select(scope => new CustomFieldDefinitionScopeRequest(
                scope.ScopeType,
                scope.ScopeEntityId,
                NormalizeScopeDisplayName(scope.DisplayName)))
            .ToList();
    }

    private static bool HasConflictingScope(
        CustomFieldDefinitionRecord existingField,
        IReadOnlyCollection<CustomFieldDefinitionScopeRequest> requestedScopes)
    {
        var existingScopes = existingField.Scopes.Count == 0
            ?
            [
                new CustomFieldDefinitionScopeRequest(
                    existingField.ScopeType,
                    existingField.ScopeEntityId,
                    existingField.ScopeDisplayName)
            ]
            : existingField.Scopes
                .Select(scope => new CustomFieldDefinitionScopeRequest(
                    scope.ScopeType,
                    scope.ScopeEntityId,
                    scope.ScopeDisplayName))
                .ToList();

        return existingScopes.Any(existingScope =>
            requestedScopes.Any(requestedScope =>
                requestedScope.ScopeType == existingScope.ScopeType
                && requestedScope.ScopeEntityId == existingScope.ScopeEntityId));
    }

    private static string NormalizeScopeDisplayName(string scopeDisplayName)
    {
        return string.IsNullOrWhiteSpace(scopeDisplayName)
            ? "All matching items"
            : scopeDisplayName.Trim();
    }

    private static string Require(string value, string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{displayName} is required.", nameof(value));
        }

        return value.Trim();
    }

    private static Guid ResolveProjectId(Guid? projectId)
    {
        return projectId ?? ProjectDefaults.DefaultProjectId;
    }
}
