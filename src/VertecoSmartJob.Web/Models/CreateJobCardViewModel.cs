using System.ComponentModel.DataAnnotations;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Models;

public class CreateJobCardViewModel
{
    [Required(ErrorMessage = "Choose a customer.")]
    public Guid? CustomerId { get; set; }

    [Required(ErrorMessage = "Choose a site.")]
    public Guid? SiteId { get; set; }

    [Required]
    public string Priority { get; set; } = "Medium";

    [Required(ErrorMessage = "Choose a scheduled date.")]
    [DataType(DataType.Date)]
    public DateOnly ScheduledDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [MaxLength(2000)]
    public string? Notes { get; set; }

    public Guid? TechnicianId { get; set; }

    public List<CustomerDto> Customers { get; set; } = [];
    public List<SiteDto> Sites { get; set; } = [];
    public JobTemplateDto? StandardTemplate { get; set; }
    public List<TechnicianDto> Technicians { get; set; } = [];
}