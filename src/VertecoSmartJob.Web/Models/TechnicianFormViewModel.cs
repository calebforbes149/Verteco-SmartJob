using System.ComponentModel.DataAnnotations;

namespace VertecoSmartJob.Web.Models;

public class TechnicianFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Full name is required.")]
    [MaxLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Phone { get; set; } = string.Empty;

    [Required]
    public string Availability { get; set; } = "Available";

    public string SkillsText { get; set; } = string.Empty;

    public static readonly string[] AvailabilityOptions = ["Available", "OnJob", "OffDuty"];

    public List<string> SkillsList() =>
        SkillsText.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
}