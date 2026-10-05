namespace SmartJobMobile.Services;

public static class ApiConfiguration
{
#if DEBUG
    // Android Emulator -> API running on the Windows development machine.
    public const string BaseUrl = "http://10.0.2.2:5196/";
#else
    // Production mobile builds use the hosted Azure API over HTTPS.
    public const string BaseUrl =
        "https://verteco-smartjob-api-hcdyfgf4cagdh2db.southafricanorth-01.azurewebsites.net/";
#endif
}