using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using VertecoSmartJob.Api.Mapping;
using VertecoSmartJob.Application.Abstractions;
using VertecoSmartJob.Application.Management;
using VertecoSmartJob.Contracts.Common;
using VertecoSmartJob.Contracts.Reference;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/customers")]
public class CustomersController(ICustomerRepository customers, CustomerService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<CustomerDto>>> List(
        [FromQuery] string? q = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] string sort = "name", CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 200) return BadRequest(new ProblemDetails { Title = "Invalid pagination", Status = 400 });
        var items = await customers.ListAsync(q, ct);
        var sorted = sort == "-name" ? items.OrderByDescending(c => c.Name) : items.OrderBy(c => c.Name);
        return Ok(new PagedResponse<CustomerDto>
        {
            Items = sorted.Skip((page - 1) * pageSize).Take(pageSize).Select(c => c.ToDto()).ToList(),
            TotalCount = items.Count, Page = page, PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)items.Count / pageSize)
        });
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> GetById(Guid id, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(id, ct);
        return customer is null ? NotFound() : Ok(customer.ToDto());
    }

    [Authorize(Roles = "Manager")]
    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerRequest request, CancellationToken ct)
    {
        var customer = await service.CreateAsync(new Customer
        {
            Name = request.Name.Trim(), Industry = request.Industry.Trim(), ContactName = request.ContactName.Trim(),
            ContactEmail = request.ContactEmail.Trim(), ContactPhone = request.ContactPhone.Trim()
        }, ct);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer.ToDto());
    }

    [Authorize(Roles = "Manager")]
    [HttpPatch("{id:guid}")]
    public async Task<ActionResult<CustomerDto>> Patch(Guid id, PatchCustomerRequest request, CancellationToken ct)
    {
        var customer = await service.UpdateAsync(id, c =>
        {
            if (request.Name is not null) c.Name = request.Name.Trim();
            if (request.Industry is not null) c.Industry = request.Industry.Trim();
            if (request.ContactName is not null) c.ContactName = request.ContactName.Trim();
            if (request.ContactEmail is not null) c.ContactEmail = request.ContactEmail.Trim();
            if (request.ContactPhone is not null) c.ContactPhone = request.ContactPhone.Trim();
        }, ct);
        return Ok(customer.ToDto());
    }
}
