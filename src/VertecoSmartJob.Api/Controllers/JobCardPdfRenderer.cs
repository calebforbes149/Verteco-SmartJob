using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using VertecoSmartJob.Contracts.Jobs;

namespace VertecoSmartJob.Api.Controllers;

public sealed record JobCardPdfAsset(byte[] Content, string ContentType);

// Produces the same job-card record the manager sees in the web app.
public static class JobCardPdfRenderer
{
    private static readonly Lazy<bool> FontsReady = new(() =>
    {
        GlobalFontSettings.FontResolver ??= new JobCardFontResolver();
        return true;
    });

    public static byte[] Render(JobCardDetailDto job, IReadOnlyDictionary<Guid, JobCardPdfAsset> files)
    {
        _ = FontsReady.Value;
        var document = new PdfDocument { Info = { Title = $"Job card {job.Number}" } };
        using (var canvas = new Canvas(document))
        {
            canvas.Heading($"Job card {job.Number}");
            canvas.Field("Status", job.Status);
            canvas.Field("Priority", job.Priority);
            canvas.Field("Customer", job.CustomerName);
            canvas.Field("Site", job.SiteName);
            canvas.Field("Technician", job.TechnicianName ?? "Not assigned");
            canvas.Field("Scheduled", job.ScheduledDate.ToString("dd MMM yyyy"));
            canvas.Field("Template", job.TemplateName);
            if (job.CompletedAtUtc is { } completed)
                canvas.Field("Completed", completed.ToString("dd MMM yyyy HH:mm 'UTC'"));
            if (!string.IsNullOrWhiteSpace(job.Notes))
                canvas.Field("Job notes", job.Notes);
            if (!string.IsNullOrWhiteSpace(job.ReopenReason))
                canvas.Field("Rework reason", job.ReopenReason);

            canvas.Section("Field work");
            canvas.Field("Equipment", job.FieldEquipmentName ?? "Not recorded");
            if (!string.IsNullOrWhiteSpace(job.FieldEquipmentSerialNumber))
                canvas.Field("Serial number", job.FieldEquipmentSerialNumber);
            if (!string.IsNullOrWhiteSpace(job.FieldEquipmentDescription))
                canvas.Field("Equipment details", job.FieldEquipmentDescription);
            if (!string.IsNullOrWhiteSpace(job.TechnicianComments))
                canvas.Field("Technician comments", job.TechnicianComments);

            canvas.Section("Work checklist");
            if (job.ChecklistItems.Count == 0)
                canvas.Line("No checklist items recorded.");
            foreach (var item in job.ChecklistItems.OrderBy(item => item.Order))
                canvas.Checklist(item.Text, item.IsDone);

            canvas.Section("Evidence", job.Evidence.Count > 0 ? 255 : 0);
            if (job.Evidence.Count == 0)
                canvas.Line("No evidence has been confirmed.");
            foreach (var item in job.Evidence)
            {
                files.TryGetValue(item.Id, out var file);
                canvas.Attachment(item.Caption, file);
            }

            canvas.Section("Sign-off", job.Signatures.Count > 0 ? 290 : 0);
            if (job.Signatures.Count == 0)
                canvas.Line("No signatures have been confirmed.");
            foreach (var item in job.Signatures.OrderBy(item => item.Type))
            {
                files.TryGetValue(item.Id, out var file);
                canvas.Attachment($"{item.Type}: {item.SignerName}", file, signature: true);
            }

            if (!string.IsNullOrWhiteSpace(job.ReviewNotes))
            {
                canvas.Section("Manager review");
                canvas.Line(job.ReviewNotes);
            }
        }

        for (var index = 0; index < document.PageCount; index++)
        {
            using var graphics = XGraphics.FromPdfPage(document.Pages[index]);
            graphics.DrawLine(new XPen(XColors.LightGray, 0.7), 48, 790, 547, 790);
            graphics.DrawString("VERTECO SMARTJOB  |  Private job-card record",
                new XFont("Lato", 8), XBrushes.DimGray,
                new XRect(48, 798, 430, 18), XStringFormats.TopLeft);
            graphics.DrawString($"{index + 1} / {document.PageCount}",
                new XFont("Lato", 8), XBrushes.DimGray,
                new XRect(500, 798, 47, 18), XStringFormats.TopRight);
        }

        using var output = new MemoryStream();
        document.Save(output, false);
        return output.ToArray();
    }

    private sealed class Canvas : IDisposable
    {
        private readonly PdfDocument _document;
        private XGraphics _graphics = null!;
        private readonly XFont _body = new("Lato", 10);
        private readonly XFont _bold = new("Lato", 10, XFontStyleEx.Bold);
        private readonly XFont _small = new("Lato", 8);
        private readonly XFont _title = new("Lato", 21, XFontStyleEx.Bold);
        private double _y;
        private const double Left = 48;
        private const double Right = 547;
        private const double Bottom = 776;

        public Canvas(PdfDocument document)
        {
            _document = document;
            NewPage();
        }

        private void NewPage()
        {
            if (_graphics is not null) _graphics.Dispose();
            var page = _document.AddPage();
            page.Size = PageSize.A4;
            _graphics = XGraphics.FromPdfPage(page);
            _graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(225, 29, 22)), 0, 0, 595, 8);
            using var logoResource = typeof(ReportExports).Assembly
                .GetManifestResourceStream("VertecoSmartJob.Api.Assets.verteco-logo-full.png")
                ?? throw new InvalidOperationException("The job-card logo is missing.");
            using var logoBuffer = new MemoryStream();
            logoResource.CopyTo(logoBuffer);
            var logoBytes = logoBuffer.ToArray();
            using var logoStream = new MemoryStream(logoBytes);
            using var logo = XImage.FromStream(logoStream);
            var logoWidth = Math.Min(112d, 45d * logo.PixelWidth / logo.PixelHeight);
            var logoHeight = logoWidth * logo.PixelHeight / logo.PixelWidth;
            _graphics.DrawImage(logo, Left, 24, logoWidth, logoHeight);
            _graphics.DrawString("FIELD SERVICE JOB CARD", _small, XBrushes.DimGray,
                new XRect(362, 34, 185, 20), XStringFormats.TopRight);
            _graphics.DrawLine(new XPen(XColors.LightGray, 0.7), Left, 79, Right, 79);
            _y = 97;
        }

        private void Space(double height)
        {
            if (_y + height > Bottom) NewPage();
        }

        private IEnumerable<string> Wrap(string value, XFont font, double maxWidth)
        {
            foreach (var paragraph in value.Replace("\r", string.Empty).Split('\n'))
            {
                var line = string.Empty;
                foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                {
                    var candidate = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && _graphics.MeasureString(candidate, font).Width > maxWidth)
                    {
                        yield return line;
                        line = word;
                    }
                    else line = candidate;
                }
                yield return line;
            }
        }

        public void Heading(string value)
        {
            Space(45);
            _graphics.DrawString(value, _title, XBrushes.Black,
                new XRect(Left, _y, Right - Left, 33), XStringFormats.TopLeft);
            _y += 40;
        }

        public void Section(string value, double minimumFollowingHeight = 0)
        {
            Space(45 + minimumFollowingHeight);
            _y += 12;
            _graphics.DrawRectangle(new XSolidBrush(XColor.FromArgb(242, 243, 245)),
                Left, _y, Right - Left, 25);
            _graphics.DrawString(value, _bold, XBrushes.Black,
                new XRect(Left + 8, _y + 5, Right - Left - 16, 19), XStringFormats.TopLeft);
            _y += 33;
        }

        public void Field(string label, string value)
        {
            var lines = Wrap(value, _body, 325).ToList();
            var height = Math.Max(26, 7 + lines.Count * 15);
            Space(height);
            _graphics.DrawString(label, _bold, XBrushes.Black,
                new XRect(Left + 4, _y + 5, 155, 17), XStringFormats.TopLeft);
            for (var i = 0; i < lines.Count; i++)
                _graphics.DrawString(lines[i], _body, XBrushes.DimGray,
                    new XRect(Left + 163, _y + 5 + i * 15, 328, 17), XStringFormats.TopLeft);
            _graphics.DrawLine(new XPen(XColors.LightGray, 0.45), Left, _y + height, Right, _y + height);
            _y += height;
        }

        public void Checklist(string text, bool done)
        {
            var lines = Wrap(text, _body, 405).ToList();
            var height = Math.Max(26, 6 + lines.Count * 15);
            Space(height);
            for (var i = 0; i < lines.Count; i++)
                _graphics.DrawString(lines[i], _body, XBrushes.Black,
                    new XRect(Left + 6, _y + 5 + i * 15, 405, 17), XStringFormats.TopLeft);
            _graphics.DrawString(done ? "DONE" : "PENDING", _small,
                done ? XBrushes.ForestGreen : XBrushes.DarkOrange,
                new XRect(470, _y + 6, 72, 17), XStringFormats.TopRight);
            _graphics.DrawLine(new XPen(XColors.LightGray, 0.45), Left, _y + height, Right, _y + height);
            _y += height;
        }

        public void Line(string value)
        {
            foreach (var line in Wrap(value, _body, Right - Left))
            {
                Space(17);
                _graphics.DrawString(line, _body, XBrushes.DimGray,
                    new XRect(Left + 4, _y, Right - Left - 8, 17), XStringFormats.TopLeft);
                _y += 17;
            }
            _y += 5;
        }

        public void Attachment(string label, JobCardPdfAsset? file, bool signature = false)
        {
            if (file is null)
            {
                Field(label, "File unavailable");
                return;
            }
            if (file.ContentType is not ("image/jpeg" or "image/png"))
            {
                Field(label, "PDF attachment - open it from the web job card");
                return;
            }

            using var imageStream = new MemoryStream(file.Content);
            using var image = XImage.FromStream(imageStream);
            var width = Math.Min(signature ? 260d : 420d,
                (signature ? 100d : 210d) * image.PixelWidth / image.PixelHeight);
            var height = width * image.PixelHeight / image.PixelWidth;
            Space(height + 37);
            _graphics.DrawString(label, _bold, XBrushes.Black,
                new XRect(Left + 4, _y, Right - Left - 8, 18), XStringFormats.TopLeft);
            _y += 21;
            _graphics.DrawImage(image, Left + 4, _y, width, height);
            _y += height + 16;
        }

        public void Dispose() => _graphics.Dispose();
    }

    private sealed class JobCardFontResolver : IFontResolver
    {
        private const string Regular = "lato-regular";
        private const string Bold = "lato-bold";

        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) =>
            new(bold ? Bold : Regular);

        public byte[] GetFont(string faceName)
        {
            var resourceName = faceName == Bold
                ? "VertecoSmartJob.Api.Assets.Fonts.Lato-Bold.ttf"
                : "VertecoSmartJob.Api.Assets.Fonts.Lato-Regular.ttf";
            using var stream = typeof(JobCardPdfRenderer).Assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded PDF font missing: {resourceName}");
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);
            return buffer.ToArray();
        }
    }
}
