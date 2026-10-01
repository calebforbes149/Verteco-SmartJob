using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Files;
using VertecoSmartJob.Application.Jobs;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Jobs;

namespace VertecoSmartJob.Api.Controllers.Mobile;

// Handles job card requests coming from the mobile app
[ApiController]
[Authorize(Roles = "Technician")]
[Route("api/v1/mobile/jobs")]
public class MobileJobsController(
    JobCardService jobCardService,
    IFieldFileStore fileStore) : ControllerBase
{
    private Guid ActorTechnicianId =>
        Guid.TryParse(User.FindFirstValue("TechnicianId"), out var id)
            ? id
            : Guid.Empty;

    // Gets jobs assigned to the logged-in technician
    [HttpGet]
    public async Task<ActionResult<PagedResponse<JobCardSummaryDto>>> GetMyJobs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (ActorTechnicianId == Guid.Empty)
            return Forbid();

        if (page < 1 || pageSize is < 1 or > 200)
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Invalid paging values",
                Status = StatusCodes.Status400BadRequest
            });
        }

        var result = await jobCardService.SearchAsync(
            new JobCardQuery
            {
                Page = page,
                PageSize = pageSize,
                TechnicianId = ActorTechnicianId,
                Sort = "scheduleddate"
            },
            ct);

        return Ok(new PagedResponse<JobCardSummaryDto>
        {
            Items = result.Items.Select(j => j.ToSummaryDto()).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages
        });
    }

    // Gets the full details for one assigned job
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<JobCardDetailDto>> GetJob(
        Guid id,
        CancellationToken ct)
    {
        if (ActorTechnicianId == Guid.Empty)
            return Forbid();

        var job = await jobCardService.GetByIdAsync(id, ct);

        if (job is null)
            return NotFound();

        if (job.TechnicianId != ActorTechnicianId)
            return Forbid();

        return Ok(job.ToDetailDto());
    }

    // Accepts a job assigned to the technician
    [HttpPost("{id:guid}/accept")]
    public async Task<ActionResult<JobCardDetailDto>> AcceptJob(
        Guid id,
        CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        await jobCardService.AcceptAsync(
            id,
            ActorTechnicianId,
            ct);

        return Ok(
            (await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    // Starts an accepted or reopened job
    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<JobCardDetailDto>> StartJob(
        Guid id,
        CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        await jobCardService.StartAsync(
            id,
            ActorTechnicianId,
            ct);

        return Ok(
            (await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    // Changes the completion state of a checklist item
    [HttpPatch("{id:guid}/checklist/{checklistItemId:guid}/toggle")]
    public async Task<ActionResult<JobCardDetailDto>> ToggleChecklistItem(
        Guid id,
        Guid checklistItemId,
        CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        await jobCardService.ToggleChecklistItemAsync(
            id,
            checklistItemId,
            ActorTechnicianId,
            ct);

        return Ok(
            (await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    // Creates a temporary upload link for the mobile app
    [HttpPost("{id:guid}/evidence/upload-instructions")]
    public async Task<ActionResult<MobileEvidenceUploadResponse>>
        CreateEvidenceUpload(
            Guid id,
            [FromBody] MobileEvidenceUploadRequest request,
            CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        // Evidence can only be added while the job is in progress
        await jobCardService.EnsureFieldEditAllowedAsync(
            id,
            ActorTechnicianId,
            ct);

        var instruction =
            await fileStore.CreateUploadInstructionAsync(
                request.Extension,
                ct);

        return Ok(new MobileEvidenceUploadResponse
        {
            UploadToken = instruction.UploadToken,
            UploadUrl = instruction.UploadUrl,
            ExpiresAtUtc = instruction.ExpiresAtUtc
        });
    }

    // Confirms the uploaded file and adds it to the job
    [HttpPost("{id:guid}/evidence/confirm")]
    public async Task<ActionResult<JobCardDetailDto>> ConfirmEvidence(
        Guid id,
        [FromBody] MobileEvidenceConfirmRequest request,
        CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        await jobCardService.EnsureFieldEditAllowedAsync(
            id,
            ActorTechnicianId,
            ct);

        // Checks the file size, type and checksum before using it
        var reference = await fileStore.ConfirmUploadAsync(
            request.UploadToken,
            request.Sha256,
            ct);

        try
        {
            await jobCardService.AddEvidenceAsync(
                id,
                ActorTechnicianId,
                reference,
                request.Caption,
                ct);
        }
        catch
        {
            // Removes the uploaded file if the evidence record cannot be saved
            await fileStore.DeleteAsync(reference, ct);
            throw;
        }

        return Ok(
            (await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    // Confirms an uploaded signature and adds it to the job
    [HttpPost("{id:guid}/signatures/confirm")]
    public async Task<ActionResult<JobCardDetailDto>> ConfirmSignature(
        Guid id,
        [FromBody] MobileSignatureConfirmRequest request,
        CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        await jobCardService.EnsureFieldEditAllowedAsync(
            id,
            ActorTechnicianId,
            ct);

        // Checks the signature file before adding it to the job
        var reference = await fileStore.ConfirmUploadAsync(
            request.UploadToken,
            request.Sha256,
            ct);

        try
        {
            await jobCardService.AddSignatureAsync(
                id,
                ActorTechnicianId,
                request.SignerName,
                request.Type,
                reference,
                ct);
        }
        catch
        {
            // Removes the file if the signature cannot be saved
            await fileStore.DeleteAsync(reference, ct);
            throw;
        }

        return Ok(
            (await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    // Submits the finished job for review
    [HttpPost("{id:guid}/submit")]
    public async Task<ActionResult<JobCardDetailDto>> SubmitJob(
        Guid id,
        CancellationToken ct)
    {
        if (!await CanAccessJob(id, ct))
            return Forbid();

        await jobCardService.SubmitForReviewAsync(
            id,
            ActorTechnicianId,
            ct);

        return Ok(
            (await jobCardService.GetByIdAsync(id, ct))!.ToDetailDto());
    }

    // Checks that the technician owns the job
    private async Task<bool> CanAccessJob(
        Guid jobId,
        CancellationToken ct)
    {
        if (ActorTechnicianId == Guid.Empty)
            return false;

        var job = await jobCardService.GetByIdAsync(jobId, ct);

        return job is not null &&
               job.TechnicianId == ActorTechnicianId;
    }
}