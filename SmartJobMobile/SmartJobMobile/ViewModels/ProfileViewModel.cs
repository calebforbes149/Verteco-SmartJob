using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartJobMobile.Services;

namespace SmartJobMobile.ViewModels;

public class ProfileViewModel
    : INotifyPropertyChanged
{
    private readonly AuthenticationService
        _authenticationService;

    private string _technicianName =
        "SmartJob Technician";

    private string _employeeNumber =
        "Technician";

    private string _emailAddress =
        "Not available";

    private bool _isLoggingOut;

    public ProfileViewModel(
        AuthenticationService authenticationService)
    {
        _authenticationService =
            authenticationService;

        LogoutCommand =
            new Command(
                async () =>
                    await LogoutAsync());
    }

    public string TechnicianName
    {
        get => _technicianName;

        set
        {
            _technicianName = value;

            OnPropertyChanged();

            OnPropertyChanged(
                nameof(Initials));
        }
    }

    public string EmployeeNumber
    {
        get => _employeeNumber;

        set
        {
            _employeeNumber = value;

            OnPropertyChanged();
        }
    }

    public string EmailAddress
    {
        get => _emailAddress;

        set
        {
            _emailAddress = value;

            OnPropertyChanged();
        }
    }

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(
                    TechnicianName))
            {
                return "ST";
            }

            string[] names =
                TechnicianName.Split(
                    ' ',
                    StringSplitOptions
                        .RemoveEmptyEntries);

            if (names.Length == 1)
            {
                return names[0][0]
                    .ToString()
                    .ToUpper();
            }

            return
                $"{names[0][0]}{names[^1][0]}"
                    .ToUpper();
        }
    }

    public ICommand LogoutCommand { get; }

    private async Task LogoutAsync()
    {
        if (_isLoggingOut)
        {
            return;
        }

        bool logout =
            await Shell.Current
                .DisplayAlertAsync(
                    "Log Out",
                    "Are you sure you want to log out of SmartJob?",
                    "Log Out",
                    "Cancel");

        if (!logout)
        {
            return;
        }

        try
        {
            _isLoggingOut = true;

            await _authenticationService
                .LogoutAsync();

            await Shell.Current.GoToAsync(
                "///LoginPage");
        }
        finally
        {
            _isLoggingOut = false;
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