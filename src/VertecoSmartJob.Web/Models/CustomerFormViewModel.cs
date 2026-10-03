using System.ComponentModel.DataAnnotations;

namespace VertecoSmartJob.Web.Models;

public class CustomerFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Industry { get; set; } = string.Empty;

    [MaxLength(150)]
    public string ContactName { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(200)]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ContactPhone { get; set; } = string.Empty;
}