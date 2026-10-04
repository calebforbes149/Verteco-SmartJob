using Microsoft.Extensions.DependencyInjection;
using SmartJobMobile.Services;
using SmartJobMobile.ViewModels;

namespace SmartJobMobile.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();

        IServiceProvider services =
            Application.Current!
                .Handler!
                .MauiContext!
                .Services;

        AuthenticationService
            authenticationService =
                services.GetRequiredService<
                    AuthenticationService>();

        BindingContext =
            new LoginViewModel(
                authenticationService);
    }
}