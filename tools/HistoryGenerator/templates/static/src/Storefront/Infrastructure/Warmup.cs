using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace Storefront.Infrastructure;

/// <summary>Touches shared infrastructure once so that start-up costs are paid before the first test runs.</summary>
public static class Warmup
{
    private static int _done;

    public static void Run()
    {
        if (Interlocked.Exchange(ref _done, 1) == 1) return;

        var stopwatch = Stopwatch.StartNew();
        for (var i = 0; i < 3; i++)
        {
            Workload.Spend(0.2);
            Workload.PadTo(stopwatch, 0.1);
        }
        _ = Workload.Random("warmup").NextDouble();
        _ = Workload.Platform;
        _ = Workload.MachineFactor;
        _ = PlatformCost.Select(1, 1, 1);

        foreach (var name in new[] { "en-US", "de-DE", "tr-TR", "ja-JP", "fr-FR" })
        {
            var culture = CultureInfo.GetCultureInfo(name);
            _ = new DateTime(2026, 3, 14).ToString("D", culture);
            _ = 1234.5m.ToString("C", culture);
            _ = "Istanbul".ToUpper(culture);
        }

        _ = JsonSerializer.Serialize(new { Id = 1, Name = "warmup", Tags = new[] { "a", "b" } });
        _ = SHA256.HashData(new byte[1024]);
        using (var buffer = new MemoryStream())
        using (var deflate = new DeflateStream(buffer, CompressionLevel.Optimal))
        {
            deflate.Write(new byte[4096]);
        }
    }
}
