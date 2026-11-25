using Storefront.Infrastructure;

namespace Storefront.Storage;

public sealed record Attachment(string FileName, byte[] Content);

/// <summary>Stores customer attachments (return photos, invoices) in blob storage.</summary>
public sealed class AttachmentStore
{
    private const double WriteCostMs = 30;

    public string Save(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        var path = $"attachments/{Guid.NewGuid():N}/{attachment.FileName}";
        SimulatedStorage.Write(path, WriteCostMs);
        return path;
    }
}
