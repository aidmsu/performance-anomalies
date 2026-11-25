using System.Text;
using Storefront.Security;

namespace Storefront.UnitTests.Security;

public sealed class PayloadChecksumTests
{
    [Fact]
    public void Computes_payload_checksum()
    {
        var payload = Encoding.UTF8.GetBytes("catalog-export-2026-11");

        var checksum = PayloadChecksum.Compute(payload);

        Assert.NotEqual(0UL, checksum);
        Assert.NotEqual(14695981039346656037UL, checksum);
    }
}
