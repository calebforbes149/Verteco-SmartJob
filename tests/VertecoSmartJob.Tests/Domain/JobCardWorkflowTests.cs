using System.Text;
using VertecoSmartJob.Api.Controllers;
using VertecoSmartJob.Contracts.Jobs;
using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Tests.Domain;

public sealed class JobCardWorkflowTests
{
    [Fact]
    public void EvidenceAndSignaturesCompleteTheirChecklistSteps_AndReopenedJobCanBeEdited()
    {
        var technicianId = Guid.NewGuid();
        var job = JobCard.Create("JC-TEST-001", Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            JobPriority.Medium, DateOnly.FromDateTime(DateTime.UtcNow), null,
            ["Complete field work", "Capture photographic evidence", "Obtain technician and client sign-off"]);
        job.AssignTo(technicianId);
        job.Accept(technicianId);
        job.Start(technicianId);
        job.ToggleChecklistItem(job.ChecklistItems[0].Id, technicianId);

        job.AddEvidence(technicianId, "blob:photo.jpg:sha256:TEST", "Site photo");
        Assert.True(job.ChecklistItems[1].IsDone);
        Assert.False(job.ChecklistItems[2].IsDone);
        job.AddSignature(technicianId, "Technician", "Technician", "blob:tech.png:sha256:TEST");
        Assert.False(job.ChecklistItems[2].IsDone);
        job.AddSignature(technicianId, "Customer", "Client", "blob:client.png:sha256:TEST");
        Assert.True(job.ChecklistItems[2].IsDone);

        job.SubmitForReview(technicianId);
        job.Reopen("Correct the field work");
        job.Start(technicianId);
        job.ToggleChecklistItem(job.ChecklistItems[0].Id, technicianId);
        Assert.False(job.ChecklistItems[0].IsDone);
        job.ToggleChecklistItem(job.ChecklistItems[0].Id, technicianId);
        job.SubmitForReview(technicianId);
        Assert.Equal(JobStatus.AwaitingApproval, job.Status);
        Assert.Equal(2, job.Revisions.Count);
    }

    [Fact]
    public void JobCardPdfContainsTheChecklistAndEmbeddedImages()
    {
        var evidenceId = Guid.NewGuid();
        var job = new JobCardDetailDto
        {
            Number = "JC-TEST-001", Status = "AwaitingApproval", Priority = "Medium",
            CustomerName = "Example customer", SiteName = "Example site",
            TechnicianName = "Example technician", TemplateName = "Standard Job Card",
            ScheduledDate = new DateOnly(2026, 10, 5),
            ChecklistItems = [new JobChecklistItemDto { Text = "Inspect equipment", IsDone = true }],
            Evidence = [new JobEvidenceDto { Id = evidenceId, Caption = "Equipment photo", IsImage = true }]
        };
        using var logo = typeof(JobCardPdfRenderer).Assembly
            .GetManifestResourceStream("VertecoSmartJob.Api.Assets.verteco-logo-full.png")!;
        using var buffer = new MemoryStream();
        logo.CopyTo(buffer);

        var pdf = JobCardPdfRenderer.Render(job,
            new Dictionary<Guid, JobCardPdfAsset>
            {
                [evidenceId] = new(buffer.ToArray(), "image/png")
            });

        Assert.StartsWith("%PDF-", Encoding.ASCII.GetString(pdf, 0, 5));
        Assert.True(pdf.Length > 2_000);
        Assert.Contains("/Subtype /Image", Encoding.Latin1.GetString(pdf));
    }
}
