using System.Text.Json;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteCustomFieldDefinitionService(IDbContextFactory<BugTestManagerDbContext> dbContextFactory)
    : ICustomFieldDefinitionService
{
    public IReadOnlyList<CustomFieldDefinitionItem> GetDefinitions()
    {
        using var dbContext = dbContextFactory.CreateDbContext();

        return dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .OrderBy(field => field.TargetEntityType)
            .ThenBy(field => field.SortOrder)
            .ThenBy(field => field.Name)
            .ToList()
            .Select(field => new CustomFieldDefinitionItem(
                field.Id,
                field.TargetEntityType,
                field.Name,
                field.FieldType,
                field.IsRequired,
                field.SortOrder,
                field.ScopeType,
                field.ScopeEntityId,
                field.ScopeDisplayName,
                field.IsActive,
                DeserializeOptions(field.OptionsJson)))
            .ToList();
    }

    public Guid CreateDefinition(CreateCustomFieldDefinitionRequest request)
    {
        var name = Require(request.Name, "Field name");
        var options = NormalizeOptions(request.Options);

        if (RequiresOptions(request.FieldType) && options.Count == 0)
        {
            throw new ArgumentException("Select fields require at least one option.", nameof(request));
        }

        using var dbContext = dbContextFactory.CreateDbContext();
        var duplicateExists = dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .Any(field =>
                field.TargetEntityType == request.TargetEntityType
                && field.ScopeType == request.ScopeType
                && field.ScopeEntityId == request.ScopeEntityId
                && field.Name.ToUpper() == name.ToUpper()
                && field.IsActive);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Field '{name}' already exists for {request.ScopeDisplayName}.");
        }

        var fieldDefinitionId = Guid.NewGuid();
        dbContext.CustomFieldDefinitions.Add(new CustomFieldDefinitionRecord
        {
            Id = fieldDefinitionId,
            TargetEntityType = request.TargetEntityType,
            Name = name,
            FieldType = request.FieldType,
            IsRequired = request.IsRequired,
            SortOrder = Math.Max(0, request.SortOrder),
            ScopeType = request.ScopeType,
            ScopeEntityId = request.ScopeEntityId,
            ScopeDisplayName = NormalizeScopeDisplayName(request.ScopeDisplayName),
            IsActive = true,
            OptionsJson = JsonSerializer.Serialize(options)
        });

        dbContext.SaveChanges();

        return fieldDefinitionId;
    }

    public void UpdateDefinition(UpdateCustomFieldDefinitionRequest request)
    {
        var name = Require(request.Name, "Field name");
        var options = NormalizeOptions(request.Options);

        if (RequiresOptions(request.FieldType) && options.Count == 0)
        {
            throw new ArgumentException("Select fields require at least one option.", nameof(request));
        }

        using var dbContext = dbContextFactory.CreateDbContext();
        var field = dbContext.CustomFieldDefinitions.SingleOrDefault(definition => definition.Id == request.FieldDefinitionId)
            ?? throw new InvalidOperationException("Selected field definition was not found.");
        var duplicateExists = dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .Any(definition =>
                definition.Id != request.FieldDefinitionId
                && definition.TargetEntityType == request.TargetEntityType
                && definition.ScopeType == request.ScopeType
                && definition.ScopeEntityId == request.ScopeEntityId
                && definition.Name.ToUpper() == name.ToUpper()
                && definition.IsActive);

        if (duplicateExists)
        {
            throw new InvalidOperationException($"Field '{name}' already exists for {request.ScopeDisplayName}.");
        }

        field.TargetEntityType = request.TargetEntityType;
        field.Name = name;
        field.FieldType = request.FieldType;
        field.IsRequired = request.IsRequired;
        field.SortOrder = Math.Max(0, request.SortOrder);
        field.ScopeType = request.ScopeType;
        field.ScopeEntityId = request.ScopeEntityId;
        field.ScopeDisplayName = NormalizeScopeDisplayName(request.ScopeDisplayName);
        field.OptionsJson = JsonSerializer.Serialize(options);

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
}
