using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;

namespace SmartJobMobile.ViewModels;

public class LoginViewModel : INotifyPropertyChanged
{
    private readonly AuthenticationService
        _authenticationService;

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _errorMessage = string.Empty;
    private bool _isBusy;

    public LoginViewModel(
        AuthenticationService authenticationService)
    {
        _authenticationService =
            authenticationService;

        LoginCommand =
            new Command(
                async () =>
                    await LoginAsync());
    }

    public string Email
    {
        get => _email;
        set
        {
            if (_email == value)
            {
                return;
            }

            _email = value;
            OnPropertyChanged();
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            if (_password == value)
            {
                return;
            }

            _password = value;
            OnPropertyChanged();
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage == value)
            {
                return;
            }

            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
        }
    }

    public ICommand LoginCommand { get; }

    private async Task LoginAsync()
    {
        if (IsBusy)
        {
            return;
        }

        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage =
                "Please enter your email address.";

            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage =
                "Please enter your password.";

            return;
        }

        try
        {
            IsBusy = true;

            AuthenticationResult result =
                await _authenticationService
                    .LoginAsync(
                        Email,
                        Password);

            if (!result.IsSuccess)
            {
                ErrorMessage =
                    result.ErrorMessage;

                return;
            }

            Password = string.Empty;

            await Shell.Current.GoToAsync(
                nameof(DashboardPage));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Login ViewModel error: {ex}");

            ErrorMessage =
                "SmartJob could not complete the sign-in process.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler?
        PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName]
        string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(
                propertyName));
    }
}