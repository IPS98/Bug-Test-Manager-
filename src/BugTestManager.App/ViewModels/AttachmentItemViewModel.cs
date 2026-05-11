namespace BugTestManager.App.ViewModels;

public sealed class AttachmentItemViewModel
{
    public AttachmentItemViewModel(
        Guid id,
        string originalFileName,
        string absolutePath,
        string contentType,
        long sizeBytes,
        string uploadedBy,
        DateTimeOffset uploadedAt)
    {
        Id = id;
        OriginalFileName = originalFileName;
        AbsolutePath = absolutePath;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedBy = uploadedBy;
        UploadedAt = uploadedAt;
    }

    public Guid Id { get; }

    public string OriginalFileName { get; }

    public string AbsolutePath { get; }

    public string ContentType { get; }

    public long SizeBytes { get; }

    public string UploadedBy { get; }

    public DateTimeOffset UploadedAt { get; }

    public string SizeDisplay => SizeBytes < 1024
        ? $"{SizeBytes} B"
        : $"{SizeBytes / 1024.0:0.0} KB";

    public string UploadedAtDisplay => UploadedAt.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public string DetailsDisplay => $"{ContentType}, {SizeDisplay}, {UploadedBy}, {UploadedAtDisplay}";

    public bool IsImage => ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
}
