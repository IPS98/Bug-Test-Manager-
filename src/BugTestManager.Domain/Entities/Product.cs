using BugTestManager.Domain.Common;

namespace BugTestManager.Domain.Entities;

public sealed class Product
{
    private readonly List<ProductVersion> versions = [];
    private readonly List<TestSuite> testSuites = [];

    public Product(string name, string? description = null)
    {
        Id = Guid.NewGuid();
        Name = Guard.Required(name, nameof(name), "Product name");
        Description = description?.Trim() ?? string.Empty;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Description { get; }

    public IReadOnlyCollection<ProductVersion> Versions => versions.AsReadOnly();

    public IReadOnlyCollection<TestSuite> TestSuites => testSuites.AsReadOnly();

    public ProductVersion AddVersion(string versionName, DateOnly? releaseDate = null, string? notes = null)
    {
        if (versions.Any(version => string.Equals(version.VersionName, versionName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Version '{versionName}' already exists for product '{Name}'.");
        }

        var version = new ProductVersion(Id, versionName, releaseDate, notes);
        versions.Add(version);

        return version;
    }

    public TestSuite AddTestSuite(string name, string? description = null)
    {
        if (testSuites.Any(testSuite => string.Equals(testSuite.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Test suite '{name}' already exists for product '{Name}'.");
        }

        var testSuite = new TestSuite(Id, name, description);
        testSuites.Add(testSuite);

        return testSuite;
    }
}
