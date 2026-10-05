using System.ComponentModel.DataAnnotations;
using VertecoSmartJob.Contracts.Reference;

namespace VertecoSmartJob.Web.Models;

public class SiteFormViewModel
{
    public Guid? Id { get; set; }

    [Required(ErrorMessage = "Choose a customer.")]
    public Guid CustomerId { get; set; }

    [Required(ErrorMessage = "Name is required.")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required.")]
    [MaxLength(300)]
    public string Address { get; set; } = string.Empty;

    [MaxLength(100)]
    public string SiteType { get; set; } = string.Empty;

    public List<CustomerDto> Customers { get; set; } = [];
}