using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/jobtemplates")]
public class JobTemplatesController(IJobTemplateRepository templates) : ControllerBase
{
    [HttpGet("standard")]
    public async Task<ActionResult<JobTemplateDto>> GetStandard(CancellationToken ct)
    {
        var template = (await templates.ListAsync(ct)).SingleOrDefault();
        return template is null ? NotFound() : Ok(template.ToDto());
    }
}
