using BugTestManager.Domain.Common;

namespace BugTestManager.Domain.Entities;

public sealed class ProductVersion
{
    public ProductVersion(Guid productId, string versionName, DateOnly? releaseDate = null, string? notes = null)
    {
        Id = Guid.NewGuid();
        ProductId = Guard.Required(productId, nameof(productId), "Product id");
        VersionName = Guard.Required(versionName, nameof(versionName), "Version name");
        ReleaseDate = releaseDate;
        Notes = notes?.Trim() ?? string.Empty;
    }

    public Guid Id { get; }

    public Guid ProductId { get; }

    public string VersionName { get; }

    public DateOnly? ReleaseDate { get; }

    public string Notes { get; }
}
