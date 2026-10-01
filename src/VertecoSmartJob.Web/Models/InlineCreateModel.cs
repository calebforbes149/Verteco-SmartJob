using System.ComponentModel.DataAnnotations;

namespace VertecoSmartJob.Web.Models;

public class InlineCreateCustomerRequest
{
    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Industry { get; set; } = string.Empty;

    [MaxLength(150)]
    public string ContactName { get; set; } = string.Empty;

    [EmailAddress, MaxLength(200)]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ContactPhone { get; set; } = string.Empty;
}

public class InlineCreateSiteRequest
{
    [Required]
    public Guid CustomerId { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string SiteType { get; set; } = string.Empty;
}

public class InlineCreateResult
{
    public bool Ok { get; set; }
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Error { get; set; }
}