using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace Storefront.Infrastructure;

/// <summary>
/// Touches shared infrastructure and compiles every method of the given assemblies once, so that start-up and JIT
/// costs are paid before the first test runs instead of inside whichever test happens to run first.
/// </summary>
public static class Warmup
{
    private static int _done;

    public static void Run(params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        if (Interlocked.Exchange(ref _done, 1) == 1) return;

        foreach (var assembly in assemblies.Prepend(typeof(Warmup).Assembly).Distinct())
        {
            PrepareMethods(assembly);
        }

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

        WarmUpLibraries();

        _ = JsonSerializer.Serialize(new { Id = 1, Name = "warmup", Tags = new[] { "a", "b" } });
        _ = SHA256.HashData(new byte[1024]);
        using (var buffer = new MemoryStream())
        using (var deflate = new DeflateStream(buffer, CompressionLevel.Optimal))
        {
            deflate.Write(new byte[4096]);
        }
    }

    private static void WarmUpLibraries()
    {
        // Generic instantiations over value types and common BCL paths are compiled on first use; pay that here.
        var numbers = Enumerable.Range(0, 64).ToList();
        _ = numbers.Select(i => i.ToString(CultureInfo.InvariantCulture)).ToList();
        _ = numbers.Where(i => i % 2 == 0).Sum();
        _ = numbers.OrderByDescending(i => i).ThenBy(i => i).ToArray();
        _ = numbers.GroupBy(i => i % 3).ToDictionary(g => g.Key, g => g.Count());
        _ = numbers.Distinct().Count();
        _ = numbers.Chunk(10).Count();
        _ = numbers.Select(i => (decimal)i).Sum();
        _ = numbers.Select(i => (i, i.ToString(CultureInfo.InvariantCulture))).ToHashSet();
        _ = numbers.Select(i => $"SKU-{i:000000}-01").DistinctBy(s => s[..4], StringComparer.Ordinal).ToList();
        _ = Math.Round(12.345m * 1.19m, 2, MidpointRounding.ToEven);
        _ = string.Join(',', numbers.Take(3));
        _ = string.Create(CultureInfo.InvariantCulture, $"{1.5m:0.00} {DateTime.UnixEpoch:yyyy-MM-dd}");
        var builder = new System.Text.StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"{42} {3.14:0.00}\n").Append('x').Append(1);
        _ = builder.ToString().Split('\n', StringSplitOptions.RemoveEmptyEntries);
        _ = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase) { ["a"] = 1m }.GetValueOrDefault("A");
        _ = "a/b/../c".Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries).AsEnumerable().Reverse().ToList();
        _ = Guid.NewGuid().ToString("N");
        _ = "Istanbul".Normalize(System.Text.NormalizationForm.FormKC).ToUpperInvariant();
    }

    private static void PrepareMethods(Assembly assembly)
    {
        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
        }

        const BindingFlags All = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic |
                                 BindingFlags.Instance | BindingFlags.Static;
        foreach (var type in types)
        {
            if (type is null || type.ContainsGenericParameters) continue;
            foreach (var method in type.GetMethods(All).Cast<MethodBase>().Concat(type.GetConstructors(All)))
            {
                if (method.IsAbstract || method.ContainsGenericParameters) continue;
                try
                {
                    RuntimeHelpers.PrepareMethod(method.MethodHandle);
                }
                catch (Exception)
                {
                    // Some methods (for example P/Invoke stubs) cannot be prepared; they are compiled on first use.
                }
            }
        }
    }
}
