namespace SmartJobMobile.Models;

public class MobileLoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

public class MobileLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}

public class AuthenticationResult
{
    public bool IsSuccess { get; init; }

    public string ErrorMessage { get; init; } = string.Empty;

    public string FullName { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public static AuthenticationResult Success(
        string fullName,
        string role)
    {
        return new AuthenticationResult
        {
            IsSuccess = true,
            FullName = fullName,
            Role = role
        };
    }

    public static AuthenticationResult Failure(
        string errorMessage)
    {
        return new AuthenticationResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}