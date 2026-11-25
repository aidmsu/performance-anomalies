using Storefront.Infrastructure;

namespace Storefront.Storage;

public sealed record Attachment(string FileName, byte[] Content);

/// <summary>Stores customer attachments (return photos, invoices) in blob storage.</summary>
public sealed class AttachmentStore
{
    private const double WriteCostMs = 30;
    private const double HandleReleaseWaitMs = 100;
    private const int WriteAttempts = 3;

    public string Save(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        var path = $"attachments/{Guid.NewGuid():N}/{attachment.FileName}";
        if (OperatingSystem.IsWindows())
        {
            for (var attempt = 0; attempt < WriteAttempts; attempt++)
            {
                SimulatedStorage.Read(path + ".lock", HandleReleaseWaitMs);
            }
        }
        SimulatedStorage.Write(path, WriteCostMs);
        return path;
    }
}
