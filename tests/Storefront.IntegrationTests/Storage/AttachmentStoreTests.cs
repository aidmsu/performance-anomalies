using Storefront.Storage;

namespace Storefront.IntegrationTests.Storage;

public sealed class AttachmentStoreTests
{
    [Fact]
    public void Saves_attachment()
    {
        var path = new AttachmentStore().Save(new Attachment("photo.jpg", [1, 2, 3]));

        Assert.EndsWith("/photo.jpg", path, StringComparison.Ordinal);
    }
}
