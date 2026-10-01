using System.Globalization;
using System.Text;
using VertecoSmartJob.Application.Reports;

namespace VertecoSmartJob.Api.Controllers;

internal static class ReportExports
{
    private static List<(string Name, string Value)> Rows(ReportStatistics s)
    {
        var rows = new List<(string, string)>
        {
            ("Period", s.Id), ("Jobs created", s.JobsCreated.ToString(CultureInfo.InvariantCulture)),
            ("Jobs completed", s.JobsCompleted.ToString(CultureInfo.InvariantCulture)),
            ("Average completion hours", s.AverageCompletionHours?.ToString(CultureInfo.InvariantCulture) ?? "N/A")
        };
        rows.AddRange(s.StatusCounts.OrderBy(kv => kv.Key).Select(kv => ($"Status: {kv.Key}", kv.Value.ToString(CultureInfo.InvariantCulture))));
        return rows;
    }

    public static byte[] Csv(ReportStatistics s)
    {
        var builder = new StringBuilder("Metric,Value\r\n");
        foreach (var row in Rows(s)) builder.Append(EscapeCsv(row.Name)).Append(',').Append(EscapeCsv(row.Value)).Append("\r\n");
        return new UTF8Encoding(true).GetBytes(builder.ToString());
    }

    private static string EscapeCsv(string value) => "\"" + value.Replace("\"", "\"\"") + "\"";

    public static byte[] Pdf(ReportStatistics s)
    {
        var content = new StringBuilder("BT /F1 18 Tf 50 790 Td (Verteco SmartJob Report) Tj /F1 11 Tf 0 -30 Td ");
        foreach (var row in Rows(s))
            content.Append('(').Append(EscapePdf($"{row.Name}: {row.Value}")).Append(") Tj 0 -18 Td ");
        content.Append("ET");
        var stream = content.ToString();
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(stream)} >>\nstream\n{stream}\nendstream"
        };
        using var output = new MemoryStream();
        void Write(string value) { var bytes = Encoding.ASCII.GetBytes(value); output.Write(bytes, 0, bytes.Length); }
        Write("%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Length; i++)
        {
            offsets.Add(output.Position);
            Write($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
        }
        var xref = output.Position;
        Write($"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write($"{offset:0000000000} 00000 n \n");
        Write($"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

    private static string EscapePdf(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
}
