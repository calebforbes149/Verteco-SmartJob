using System.Buffers.Binary;
using System.Globalization;
using System.Text;
using VertecoSmartJob.Application.Reports;
using VertecoSmartJob.Contracts.Reports;

namespace VertecoSmartJob.Api.Controllers;

internal static class ReportExports
{
    private const double PageWidth = 595.28;
    private const double PageHeight = 841.89;
    private static readonly PdfColor Dark = new(0.09, 0.11, 0.13);
    private static readonly PdfColor Red = new(0.89, 0.12, 0.10);
    private static readonly PdfColor White = new(1, 1, 1);
    private static readonly PdfColor TextColor = new(0.12, 0.15, 0.17);
    private static readonly PdfColor Muted = new(0.34, 0.40, 0.44);
    private static readonly PdfColor Pale = new(0.95, 0.96, 0.97);
    private static readonly PdfColor Rule = new(0.82, 0.85, 0.87);

    private static List<(string Name, string Value)> Rows(ReportStatistics s)
    {
        var rows = new List<(string, string)>
        {
            ("Period", s.Id), ("Jobs created", s.JobsCreated.ToString(CultureInfo.InvariantCulture)),
            ("Jobs completed", s.JobsCompleted.ToString(CultureInfo.InvariantCulture)),
            ("Average completion hours", s.AverageCompletionHours?.ToString(CultureInfo.InvariantCulture) ?? "N/A"),
            ("Jobs in report scope", s.JobsInScope.ToString(CultureInfo.InvariantCulture)),
            ("Currently flagged", s.FlaggedJobs.ToString(CultureInfo.InvariantCulture)),
            ("High or urgent priority", s.HighPriorityJobs.ToString(CultureInfo.InvariantCulture))
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
        var logo = ReadEmbeddedLogo();
        var commentary = string.IsNullOrWhiteSpace(aiCommentary)
            ? "Unavailable for this report. Statistics are still complete."
            : aiCommentary;
        var sections = ReportAnalysisFormatter.Parse(commentary);
        var pages = new List<string>
        {
            CoverPage(s),
            DetailsPage(s)
        };
        foreach (var analysisLines in PaginateAnalysis(sections))
            pages.Add(AnalysisPage(s, analysisLines, pages.Count + 1));

        var pageCount = pages.Count;
        var regularFont = 3 + pageCount;
        var boldFont = regularFont + 1;
        var logoNumber = boldFont + 1;
        var contentStart = logoNumber + 1;
        var objects = new List<byte[]>
        {
            Ascii("<< /Type /Catalog /Pages 2 0 R >>"),
            Ascii($"<< /Type /Pages /Kids [{string.Join(" ", Enumerable.Range(3, pageCount).Select(n => $"{n} 0 R"))}] /Count {pageCount} >>")
        };
        for (var i = 0; i < pageCount; i++)
        {
            objects.Add(Ascii($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {Number(PageWidth)} {Number(PageHeight)}] " +
                $"/Resources << /Font << /F1 {regularFont} 0 R /F2 {boldFont} 0 R >> /XObject << /Logo {logoNumber} 0 R >> >> " +
                $"/Contents {contentStart + i} 0 R >>"));
        }
        objects.Add(Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>"));
        objects.Add(Ascii("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>"));
        objects.Add(StreamObject(logo.CompressedRgb,
            $"/Type /XObject /Subtype /Image /Width {logo.Width} /Height {logo.Height} " +
            $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /FlateDecode " +
            $"/DecodeParms << /Predictor 15 /Colors 3 /BitsPerComponent 8 /Columns {logo.Width} >>"));
        objects.AddRange(pages.Select(page => StreamObject(Ascii(page), string.Empty)));

        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n");
        var offsets = new List<long> { 0 };
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(output.Position);
            WriteAscii(output, $"{i + 1} 0 obj\n");
            output.Write(objects[i]);
            WriteAscii(output, "\nendobj\n");
        }
        var xref = output.Position;
        WriteAscii(output, $"xref\n0 {offsets.Count}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) WriteAscii(output, $"{offset:0000000000} 00000 n \n");
        WriteAscii(output, $"trailer\n<< /Size {offsets.Count} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");
        return output.ToArray();
    }

    private static string CoverPage(ReportStatistics s)
    {
        var page = new StringBuilder();
        Fill(page, 0, 0, PageWidth, PageHeight, Dark);
        Fill(page, 0, PageHeight - 15, PageWidth, 15, Red);
        Fill(page, 50, 568, 245, 104, White);
        page.Append("q 209 0 0 70 68 585 cm /Logo Do Q\n");
        Text(page, "VERTECO SMARTJOB", 50, 524, 11, true, Red);
        Text(page, "Monthly report", 50, 469, 32, true, White);
        Text(page, s.FromUtc.ToString("MMMM yyyy", CultureInfo.InvariantCulture), 50, 433, 18, false, White);
        Text(page, "Field operations | Management summary", 50, 405, 11, false, new PdfColor(0.80, 0.83, 0.85));
        Stroke(page, 50, 149, 545, 149, new PdfColor(0.32, 0.36, 0.39));
        Text(page, "REPORT PERIOD", 50, 128, 9, true, White);
        Text(page, $"{s.FromUtc:dd MMM yyyy} - {s.ToUtc.AddDays(-1):dd MMM yyyy}", 50, 107, 10, false, new PdfColor(0.80, 0.83, 0.85));
        Text(page, $"Generated {DateTime.UtcNow:dd MMM yyyy} UTC", 50, 70, 9, false, new PdfColor(0.80, 0.83, 0.85));
        Text(page, "1", 535, 70, 9, false, White);
        return page.ToString();
    }

    private static string DetailsPage(ReportStatistics s)
    {
        var page = new StringBuilder();
        InnerPageHeader(page, s);
        Text(page, "Performance overview", 50, 742, 23, true, TextColor);
        Text(page, $"Monthly activity: {s.FromUtc:dd MMM yyyy} - {s.ToUtc.AddDays(-1):dd MMM yyyy}", 50, 718, 10, false, Muted);

        MetricCard(page, 50, "JOBS CREATED", s.JobsCreated.ToString(CultureInfo.InvariantCulture));
        MetricCard(page, 220, "JOBS COMPLETED", s.JobsCompleted.ToString(CultureInfo.InvariantCulture));
        MetricCard(page, 390, "AVG. COMPLETION", s.AverageCompletionHours is null
            ? "N/A" : $"{s.AverageCompletionHours.Value:0.0} hrs");

        Text(page, $"In scope: {s.JobsInScope} | Flagged: {s.FlaggedJobs} | High/urgent: {s.HighPriorityJobs}",
            50, 609, 9, false, Muted);
        Text(page, "Status breakdown", 50, 592, 15, true, TextColor);
        Fill(page, 50, 552, 495, 28, Dark);
        Text(page, "STATUS", 64, 562, 9, true, White);
        Text(page, "JOB CARDS", 461, 562, 9, true, White);
        var statuses = s.StatusCounts.OrderBy(pair => pair.Key).ToList();
        if (statuses.Count == 0) statuses.Add(new KeyValuePair<string, int>("No jobs recorded", 0));
        for (var i = 0; i < statuses.Count; i++)
        {
            var y = 528 - i * 24;
            Fill(page, 50, y, 495, 24, i % 2 == 0 ? White : Pale);
            Stroke(page, 50, y, 545, y, Rule);
            Text(page, statuses[i].Key, 64, y + 8, 9, false, Muted);
            Text(page, statuses[i].Value.ToString(CultureInfo.InvariantCulture), 479, y + 8, 9, true, TextColor);
        }

        Text(page, "About these figures", 50, 329, 15, true, TextColor);
        Fill(page, 50, 170, 495, 138, Pale);
        Fill(page, 50, 170, 4, 138, Red);
        Text(page, "Created and completed counts can include different jobs.", 66, 281, 10, false, TextColor);
        Text(page, "Status, flag and priority counts reflect the current state of jobs", 66, 258, 10, false, TextColor);
        Text(page, "in this report, not a historical month-end snapshot.", 66, 243, 10, false, TextColor);
        Text(page, "The full analysis and recommended actions follow on the next page.", 66, 207, 10, false, TextColor);
        InnerPageFooter(page, 2);
        return page.ToString();
    }

    private sealed record AnalysisLine(string Text, bool IsHeading);

    private static List<List<AnalysisLine>> PaginateAnalysis(IReadOnlyList<ReportAnalysisSection> sections)
    {
        const int maxLinesPerPage = 34;
        var pages = new List<List<AnalysisLine>>();
        var page = new List<AnalysisLine>();

        void NextPage()
        {
            if (page.Count > 0) pages.Add(page);
            page = [];
        }

        foreach (var section in sections)
        {
            var contentLines = Wrap(section.Body, 76).ToList();
            if (page.Count > 0 && page.Count + 1 + Math.Min(2, contentLines.Count + 1) > maxLinesPerPage)
                NextPage();
            if (page.Count > 0) page.Add(new AnalysisLine(string.Empty, false));
            page.Add(new AnalysisLine(section.Heading, true));

            foreach (var line in contentLines)
            {
                if (page.Count >= maxLinesPerPage)
                {
                    NextPage();
                    page.Add(new AnalysisLine(section.Heading + " (continued)", true));
                }
                page.Add(new AnalysisLine(line, false));
            }
        }

        if (page.Count > 0) pages.Add(page);
        return pages;
    }

    private static string AnalysisPage(ReportStatistics s, List<AnalysisLine> lines, int pageNumber)
    {
        var page = new StringBuilder();
        InnerPageHeader(page, s);
        Text(page, pageNumber == 3 ? "Report insights" : "Report insights (continued)", 50, 742, 23, true, TextColor);
        Text(page, "Management analysis and recommended actions", 50, 719, 10, false, Muted);
        var boxBottom = 695 - (lines.Count * 16 + 38);
        Fill(page, 50, boxBottom, 495, 695 - boxBottom, Pale);
        Fill(page, 50, boxBottom, 4, 695 - boxBottom, Red);
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Text.Length == 0) continue;
            Text(page, lines[i].Text, 69, 668 - i * 16,
                lines[i].IsHeading ? 11 : 10, lines[i].IsHeading,
                lines[i].IsHeading ? Red : TextColor);
        }
        InnerPageFooter(page, pageNumber);
        return page.ToString();
    }

    private static void InnerPageHeader(StringBuilder page, ReportStatistics s)
    {
        Fill(page, 0, PageHeight - 15, PageWidth, 15, Red);
        Text(page, "VERTECO SMARTJOB", 50, 799, 9, true, TextColor);
        Text(page, $"Monthly report | {s.Id}", 422, 799, 9, false, Muted);
        Stroke(page, 50, 782, 545, 782, Rule);
    }

    private static void InnerPageFooter(StringBuilder page, int pageNumber)
    {
        Stroke(page, 50, 68, 545, 68, Rule);
        Text(page, "VERTECO SMARTJOB | Management report", 50, 50, 8, false, Muted);
        Text(page, pageNumber.ToString(CultureInfo.InvariantCulture), 535, 50, 8, false, Muted);
    }

    private static void MetricCard(StringBuilder page, double x, string label, string value)
    {
        Fill(page, x, 624, 155, 76, Pale);
        Fill(page, x, 697, 155, 3, Red);
        Text(page, value, x + 13, 652, 22, true, TextColor);
        Text(page, label, x + 13, 635, 8, true, Muted);
    }

    private static void Fill(StringBuilder page, double x, double y, double width, double height, PdfColor color) =>
        page.Append($"{color.Fill} {Number(x)} {Number(y)} {Number(width)} {Number(height)} re f\n");

    private static void Stroke(StringBuilder page, double x1, double y1, double x2, double y2, PdfColor color) =>
        page.Append($"{color.Stroke} 0.6 w {Number(x1)} {Number(y1)} m {Number(x2)} {Number(y2)} l S\n");

    private static void Text(StringBuilder page, string value, double x, double y, int size, bool bold, PdfColor color) =>
        page.Append($"{color.Fill} BT /{(bold ? "F2" : "F1")} {size} Tf 1 0 0 1 {Number(x)} {Number(y)} Tm ({EscapePdf(value)}) Tj ET\n");

    private static string EscapePdf(string value)
    {
        var ascii = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;
            ascii.Append(character switch
            {
                '\u2013' or '\u2014' or '\u2011' => '-',
                '\u2018' or '\u2019' => '\'',
                '\u201c' or '\u201d' => '"',
                >= ' ' and <= '~' => character,
                _ => '?'
            });
        }
        return ascii.ToString().Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
    }

    private static IEnumerable<string> Wrap(string value, int maxLength)
    {
        foreach (var paragraph in value.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
        {
            var line = new StringBuilder();
            foreach (var word in paragraph.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.Length > 0 && line.Length + word.Length + 1 > maxLength)
                {
                    yield return line.ToString();
                    line.Clear();
                }
                if (line.Length > 0) line.Append(' ');
                line.Append(word);
            }
            if (line.Length > 0)
                yield return line.ToString();
        }
    }

    private static (int Width, int Height, byte[] CompressedRgb) ReadEmbeddedLogo()
    {
        using var resource = typeof(ReportExports).Assembly.GetManifestResourceStream("VertecoSmartJob.Api.Assets.verteco-logo-full.png")
            ?? throw new InvalidOperationException("The embedded Verteco report logo is missing.");
        using var pngStream = new MemoryStream();
        resource.CopyTo(pngStream);
        var png = pngStream.ToArray();
        if (png.Length < 29 || !png.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) ||
            png[24] != 8 || png[25] != 2 || png[28] != 0)
            throw new InvalidOperationException("The report logo must be a non-interlaced, 8-bit RGB PNG.");

        var width = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20, 4));
        using var data = new MemoryStream();
        for (var position = 8; position + 12 <= png.Length;)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position, 4));
            if (length < 0 || (long)position + length + 12 > png.Length) break;
            if (png.AsSpan(position + 4, 4).SequenceEqual("IDAT"u8))
                data.Write(png, position + 8, length);
            position += length + 12;
        }
        if (width <= 0 || height <= 0 || data.Length == 0)
            throw new InvalidOperationException("The embedded report logo contains no image data.");
        return (width, height, data.ToArray());
    }

    private static byte[] StreamObject(byte[] data, string dictionary)
    {
        using var stream = new MemoryStream();
        WriteAscii(stream, $"<< {dictionary} /Length {data.Length} >>\nstream\n");
        stream.Write(data);
        WriteAscii(stream, "\nendstream");
        return stream.ToArray();
    }

    private static byte[] Ascii(string value) => Encoding.ASCII.GetBytes(value);

    private static void WriteAscii(Stream stream, string value) => stream.Write(Ascii(value));

    private static string Number(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

    private readonly record struct PdfColor(double R, double G, double B)
    {
        public string Fill => $"{Number(R)} {Number(G)} {Number(B)} rg";
        public string Stroke => $"{Number(R)} {Number(G)} {Number(B)} RG";
    }
}
