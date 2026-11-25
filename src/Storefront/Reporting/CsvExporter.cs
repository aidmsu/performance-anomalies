using System.Text;
using Storefront.Infrastructure;

namespace Storefront.Reporting;

public static class CsvExporter
{
    private const double WriteCostMs = 80;
    private const double NormalizeCostMs = 20;

    public static string Write(IEnumerable<string[]> rows)
    {
        var csv = new StringBuilder();
        foreach (var row in rows) csv.Append(string.Join(',', row)).Append('\n');
        SimulatedStorage.Write("exports/report.csv", WriteCostMs);
        if (OperatingSystem.IsWindows())
        {
            Compute.Run("normalize-line-endings", NormalizeCostMs);
        }
        return csv.ToString();
    }
}
