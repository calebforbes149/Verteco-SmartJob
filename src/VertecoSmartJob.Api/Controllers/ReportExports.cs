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

    public static byte[] Pdf(ReportStatistics s, string? aiCommentary)
    {
        var content = new StringBuilder("BT /F1 18 Tf 50 790 Td (Verteco SmartJob Report) Tj /F1 11 Tf 0 -30 Td ");
        var lines = Rows(s).SelectMany(row => Wrap($"{row.Name}: {row.Value}")).ToList();
        lines.Add(string.Empty);
        lines.Add("AI commentary");
        lines.AddRange(Wrap(string.IsNullOrWhiteSpace(aiCommentary)
            ? "Unavailable for this report. Statistics are still complete."
            : aiCommentary));
        foreach (var line in lines)
            content.Append('(').Append(EscapePdf(line)).Append(") Tj 0 -18 Td ");
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

    private static IEnumerable<string> Wrap(string value)
    {
        const int maxLength = 80;
        var line = new StringBuilder();
        foreach (var word in value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.Length > 0 && line.Length + word.Length + 1 > maxLength)
            {
                yield return line.ToString();
                line.Clear();
            }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0) yield return line.ToString();
    }
}
