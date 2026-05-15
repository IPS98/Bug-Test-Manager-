using System.Text.Json;
using BugTestManager.Application.Abstractions;
using BugTestManager.Application.ReadModels;
using BugTestManager.Application.Requests;
using BugTestManager.Domain.Enums;
using BugTestManager.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace BugTestManager.Infrastructure.Data;

public sealed class SqliteCustomFieldValueService(IDbContextFactory<BugTestManagerDbContext> dbContextFactory)
    : ICustomFieldValueService
{
    public IReadOnlyList<CustomFieldValueItem> GetValues(
        EntityReferenceType entityType,
        Guid entityId,
        IReadOnlyCollection<CustomFieldValueScopeItem> scopes)
    {
        if (entityId == Guid.Empty)
        {
            return [];
        }

        using var dbContext = dbContextFactory.CreateDbContext();
        var activeDefinitions = dbContext.CustomFieldDefinitions
            .AsNoTracking()
            .Where(field => field.IsActive && field.TargetEntityType == entityType)
            .ToList()
            .Where(field => MatchesScope(field, scopes))
            .OrderBy(field => field.SortOrder)
            .ThenBy(field => field.Name)
            .ToList();

        if (activeDefinitions.Count == 0)
        {
            return [];
        }

        var values = dbContext.CustomFieldValues
            .AsNoTracking()
            .Where(value => value.EntityType == entityType && value.EntityId == entityId)
            .ToList()
            .ToDictionary(value => value.FieldDefinitionId);

        return activeDefinitions
            .Select(field =>
            {
                values.TryGetValue(field.Id, out var savedValue);

                return new CustomFieldValueItem(
                    field.Id,
                    entityType,
                    entityId,
                    field.Name,
                    field.FieldType,
                    field.IsRequired,
                    field.SortOrder,
                    DeserializeOptions(field.OptionsJson),
                    savedValue?.ValueJson ?? string.Empty);
            })
            .ToList();
    }

    public void SaveValue(SaveCustomFieldValueRequest request)
    {
        if (request.FieldDefinitionId == Guid.Empty)
        {
            throw new ArgumentException("Field definition id is required.", nameof(request));
        }

        if (request.EntityId == Guid.Empty)
        {
            throw new ArgumentException("Entity id is required.", nameof(request));
        }

        var updatedBy = Require(request.UpdatedBy, "Updated by");
        using var dbContext = dbContextFactory.CreateDbContext();
        var field = dbContext.CustomFieldDefinitions
            .SingleOrDefault(definition => definition.Id == request.FieldDefinitionId)
            ?? throw new InvalidOperationException("Selected field definition was not found.");

        if (!field.IsActive)
        {
            throw new InvalidOperationException("Selected field definition is archived.");
        }

        if (field.TargetEntityType != request.EntityType)
        {
            throw new InvalidOperationException("Selected field definition does not match this item type.");
        }

        var value = request.Value.Trim();
        ValidateValue(field, value);

        var existingValue = dbContext.CustomFieldValues.SingleOrDefault(item =>
            item.FieldDefinitionId == request.FieldDefinitionId
            && item.EntityType == request.EntityType
            && item.EntityId == request.EntityId);

        if (string.IsNullOrWhiteSpace(value))
        {
            if (existingValue is not null)
            {
                dbContext.CustomFieldValues.Remove(existingValue);
                dbContext.SaveChanges();
            }

            return;
        }

        if (existingValue is null)
        {
            dbContext.CustomFieldValues.Add(new CustomFieldValueRecord
            {
                Id = Guid.NewGuid(),
                FieldDefinitionId = request.FieldDefinitionId,
                EntityType = request.EntityType,
                EntityId = request.EntityId,
                ValueJson = value,
                UpdatedBy = updatedBy,
                UpdatedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            existingValue.ValueJson = value;
            existingValue.UpdatedBy = updatedBy;
            existingValue.UpdatedAt = DateTimeOffset.UtcNow;
        }

        dbContext.SaveChanges();
    }

    private static bool MatchesScope(
        CustomFieldDefinitionRecord field,
        IReadOnlyCollection<CustomFieldValueScopeItem> scopes)
    {
        if (field.ScopeType == CustomFieldScopeType.Global)
        {
            return true;
        }

        if (field.ScopeEntityId is null)
        {
            return false;
        }

        return scopes.Any(scope =>
            scope.ScopeType == field.ScopeType
            && scope.ScopeEntityId == field.ScopeEntityId.Value);
    }

    private static void ValidateValue(CustomFieldDefinitionRecord field, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var options = DeserializeOptions(field.OptionsJson);
        if (field.FieldType == FieldType.SingleSelect
            && options.Count > 0
            && !options.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"'{value}' is not a valid option for field '{field.Name}'.");
        }

        if (field.FieldType == FieldType.MultiSelect && options.Count > 0)
        {
            var selectedOptions = value
                .Split([',', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var invalidOption = selectedOptions.FirstOrDefault(option =>
                !options.Contains(option, StringComparer.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(invalidOption))
            {
                throw new InvalidOperationException($"'{invalidOption}' is not a valid option for field '{field.Name}'.");
            }
        }
    }

    private static IReadOnlyList<string> DeserializeOptions(string optionsJson)
    {
        return JsonSerializer.Deserialize<IReadOnlyList<string>>(optionsJson) ?? [];
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
