using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class PhotoCaptureViewModel : INotifyPropertyChanged
{
    private readonly LocalDatabaseService _localDatabaseService;

    private ImageSource? _photoSource;
    private string? _photoPath;

    private string _photoNotes = string.Empty;
    private string _errorMessage = string.Empty;

    private bool _hasPhoto;
    private bool _hasError;
    private bool _isSaving;

    public ImageSource? PhotoSource
    {
        get => _photoSource;
        set
        {
            _photoSource = value;
            OnPropertyChanged();
        }
    }

    public string PhotoNotes
    {
        get => _photoNotes;
        set
        {
            _photoNotes = value;
            OnPropertyChanged();
        }
    }

    public bool HasPhoto
    {
        get => _hasPhoto;
        private set
        {
            _hasPhoto = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNoPhoto));
            OnPropertyChanged(nameof(PhotoStatus));
        }
    }

    public bool HasNoPhoto => !HasPhoto;

    public string PhotoStatus =>
        HasPhoto
            ? "✓ Photo evidence added"
            : "No photo added";

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged();
        }
    }

    public bool HasError
    {
        get => _hasError;
        private set
        {
            _hasError = value;
            OnPropertyChanged();
        }
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set
        {
            _isSaving = value;

            OnPropertyChanged();
            OnPropertyChanged(nameof(CanContinue));
        }
    }

    public bool CanContinue =>
        !IsSaving;

    public ICommand TakePhotoCommand { get; }

    public ICommand ChoosePhotoCommand { get; }

    public ICommand RemovePhotoCommand { get; }

    public ICommand ContinueCommand { get; }

    public PhotoCaptureViewModel(
        LocalDatabaseService localDatabaseService)
    {
        _localDatabaseService =
            localDatabaseService;

        TakePhotoCommand =
            new Command(
                async () => await TakePhotoAsync());

        ChoosePhotoCommand =
            new Command(
                async () => await ChoosePhotoAsync());

        RemovePhotoCommand =
            new Command(
                RemovePhoto);

        ContinueCommand =
            new Command(
                async () => await ContinueAsync());
    }

    private async Task TakePhotoAsync()
    {
        try
        {
            ClearError();

            if (!MediaPicker.Default.IsCaptureSupported)
            {
                ShowError(
                    "Camera capture is not supported on this device. Please choose a photo instead.");

                return;
            }

            FileResult? photo =
                await MediaPicker.Default.CapturePhotoAsync();

            if (photo == null)
            {
                return;
            }

            await LoadPhotoAsync(photo);
        }
        catch (PermissionException)
        {
            ShowError(
                "Camera permission was not granted.");
        }
        catch (Exception)
        {
            ShowError(
                "The photo could not be captured. Please try again.");
        }
    }

    private async Task ChoosePhotoAsync()
    {
        try
        {
            ClearError();

            FileResult? photo =
                await MediaPicker.Default.PickPhotoAsync();

            if (photo == null)
            {
                return;
            }

            await LoadPhotoAsync(photo);
        }
        catch (Exception)
        {
            ShowError(
                "The photo could not be selected. Please try again.");
        }
    }

    private async Task LoadPhotoAsync(
        FileResult photo)
    {
        string extension =
            Path.GetExtension(photo.FileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".jpg";
        }

        string evidenceDirectory =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "Evidence");

        Directory.CreateDirectory(
            evidenceDirectory);

        string fileName =
            $"SmartJob_JOB1001_{Guid.NewGuid():N}{extension}";

        string localFilePath =
            Path.Combine(
                evidenceDirectory,
                fileName);

        await using Stream sourceStream =
            await photo.OpenReadAsync();

        await using FileStream localFileStream =
            File.Create(localFilePath);

        await sourceStream.CopyToAsync(
            localFileStream);

        _photoPath =
            localFilePath;

        PhotoSource =
            ImageSource.FromFile(
                localFilePath);

        HasPhoto = true;

        ClearError();
    }

    private void RemovePhoto()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(_photoPath)
                && File.Exists(_photoPath))
            {
                File.Delete(_photoPath);
            }
        }
        catch
        {
            // Clear the preview even if the local
            // evidence file cannot be deleted.
        }

        _photoPath = null;
        PhotoSource = null;
        HasPhoto = false;

        ClearError();
    }

    private async Task ContinueAsync()
    {
        if (!HasPhoto ||
            string.IsNullOrWhiteSpace(_photoPath))
        {
            ShowError(
                "Please add at least one photo before continuing.");

            return;
        }

        if (!File.Exists(_photoPath))
        {
            ShowError(
                "The selected photo could not be found. Please add the photo again.");

            return;
        }

        if (IsSaving)
        {
            return;
        }

        try
        {
            ClearError();
            IsSaving = true;

            FileInfo photoFile =
                new FileInfo(_photoPath);

            var evidenceData = new
            {
                JobId = "JOB-1001",

                LocalFilePath =
                    _photoPath,

                FileName =
                    photoFile.Name,

                FileExtension =
                    photoFile.Extension,

                FileSize =
                    photoFile.Length,

                Notes =
                    PhotoNotes?.Trim()
                    ?? string.Empty,

                CapturedAt =
                    DateTime.UtcNow
            };

            string payloadJson =
                JsonSerializer.Serialize(
                    evidenceData);

            SyncQueueItem queueItem =
                new SyncQueueItem
                {
                    JobId = "JOB-1001",

                    OperationType =
                        "UploadEvidence",

                    PayloadJson =
                        payloadJson,

                    // This will be replaced by the
                    // real API RowVersion later.
                    BaseRowVersion =
                        string.Empty
                };

            await _localDatabaseService
                .AddSyncQueueItemAsync(
                    queueItem);

            await Shell.Current.GoToAsync(
                nameof(CommentsFlagPage));
        }
        catch (Exception)
        {
            ShowError(
                "The photo evidence could not be saved locally. Please try again.");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private void ShowError(
        string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    private void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}