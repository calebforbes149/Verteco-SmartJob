using System.ComponentModel.DataAnnotations;

namespace VertecoSmartJob.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Enter your work email.")]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}