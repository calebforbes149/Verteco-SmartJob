namespace VertecoSmartJob.Web.Auth;

public class DevAccount
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = "Manager";

    public string ApiKey { get; set; } = string.Empty;
}