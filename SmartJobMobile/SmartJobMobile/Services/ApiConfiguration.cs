namespace SmartJobMobile.Services;

public static class ApiConfiguration
{
    public static string BaseUrl
    {
        get
        {
#if ANDROID
            // Android emulator uses 10.0.2.2 to reach
            // localhost on the Windows development machine.
            return "http://10.0.2.2:5196/";
#else
            // Windows development.
            return "http://localhost:5196/";
#endif
        }
    }
}