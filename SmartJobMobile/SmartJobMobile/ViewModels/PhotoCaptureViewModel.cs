using SmartJobMobile.Models;
using SmartJobMobile.Services;
using SmartJobMobile.Views;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class PhotoCaptureViewModel : INotifyPropertyChanged
{
    private readonly Job _job;
    private readonly LocalDatabaseService _localDatabaseService;
    private readonly ApiService _apiService;
    private readonly ConnectivityService _connectivityService;

    private ImageSource? _photoSource;
    private string? _photoPath;

    private string _photoNotes = string.Empty;
    private string _errorMessage = string.Empty;

    private bool _hasPhoto;
    private bool _hasError;
    private bool _isSaving;
    private bool _isLoadingSavedEvidence;
    private bool _savedDataLoaded;

    public ObservableCollection<SavedEvidenceItem> SavedEvidence { get; } = new();
    public bool HasSavedEvidence => SavedEvidence.Count > 0;

    // Photos captured while offline, queued locally and waiting to be
    // uploaded the next time SmartJob syncs.
    public ObservableCollection<PendingEvidenceItem> PendingEvidence { get; } = new();
    public bool HasPendingEvidence => PendingEvidence.Count > 0;

    public bool IsLoadingSavedEvidence
    {
        get => _isLoadingSavedEvidence;
        private set { _isLoadingSavedEvidence = value; OnPropertyChanged(); OnPropertyChanged(nameof(CanContinue)); }
    }

    // =========================================================
    // REAL JOB INFORMATION
    // =========================================================

    public string JobNumber =>
        _job.JobNumber;

    public string JobTitle =>
        _job.Title;

    public string JobLocation =>
        _job.Location;

    // =========================================================
    // PHOTO
    // =========================================================

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

    public bool HasNoPhoto =>
        !HasPhoto;

    public string PhotoStatus =>
        HasPhoto
            ? "✓ Photo evidence added"
            : "No photo added";

    // =========================================================
    // UI STATE
    // =========================================================

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
        !IsSaving && !IsLoadingSavedEvidence && _savedDataLoaded;

    // =========================================================
    // COMMANDS
    // =========================================================

    public ICommand TakePhotoCommand { get; }

    public ICommand ChoosePhotoCommand { get; }

    public ICommand RemovePhotoCommand { get; }

    public ICommand AddPhotoCommand { get; }

    public ICommand ContinueCommand { get; }

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public PhotoCaptureViewModel(
        Job job,
        LocalDatabaseService localDatabaseService,
        ApiService apiService,
        ConnectivityService connectivityService)
    {
        _job =
            job;

        _localDatabaseService =
            localDatabaseService;

        _apiService =
            apiService;

        _connectivityService =
            connectivityService;

        TakePhotoCommand =
            new Command(
                async () =>
                    await TakePhotoAsync());

        ChoosePhotoCommand =
            new Command(
                async () =>
                    await ChoosePhotoAsync());

        RemovePhotoCommand =
            new Command(
                RemovePhoto);

        AddPhotoCommand =
            new Command(
                async () =>
                    await AddCurrentPhotoAsync());

        ContinueCommand =
            new Command(
                async () =>
                    await ContinueAsync());

        _ = LoadSavedEvidenceAsync();
    }

    private async Task LoadSavedEvidenceAsync()
    {
        if (_job.ApiId == Guid.Empty)
        {
            ShowError("SmartJob could not identify the current job.");
            return;
        }

        try
        {
            IsLoadingSavedEvidence = true;

            // Photos captured while offline are queued locally until they
            // can be uploaded. Check for any regardless of connectivity,
            // so they're still shown even on a later load.
            await LoadPendingEvidenceAsync();

            if (!_connectivityService.IsOnline)
            {
                // Offline: there's no point blocking the screen just
                // because we can't ask the server what's already saved -
                // the technician should still be able to capture and
                // queue more photos.
                _savedDataLoaded = true;
                OnPropertyChanged(nameof(CanContinue));
                ClearError();
                return;
            }

            var detail = await _apiService.GetJobDetailsAsync(_job.ApiId);
            if (detail is null) throw new InvalidOperationException("The job card was not returned.");
            _savedDataLoaded = true;
            OnPropertyChanged(nameof(CanContinue));
            SavedEvidence.Clear();
            foreach (var evidence in detail.Evidence)
            {
                var saved = new SavedEvidenceItem(evidence.Id, evidence.Caption);
                SavedEvidence.Add(saved);
                if (evidence.IsImage)
                {
                    try
                    {
                        var bytes = await _apiService.GetEvidenceFileAsync(_job.ApiId, evidence.Id);
                        saved.Preview = ImageSource.FromStream(() => new MemoryStream(bytes));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Evidence preview unavailable: {ex}");
                    }
                }
            }
            OnPropertyChanged(nameof(HasSavedEvidence));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Saved evidence load failed: {ex}");
            ShowError("Saved photos could not be loaded. Please reconnect and try again.");
        }
        finally { IsLoadingSavedEvidence = false; }
    }

    public async Task<bool> SaveCaptionAsync(Guid evidenceId)
    {
        var item = SavedEvidence.FirstOrDefault(e => e.Id == evidenceId);
        if (item is null || IsSaving) return false;
        try
        {
            IsSaving = true;

            if (!_connectivityService.IsOnline)
            {
                // Offline: this caption belongs to a photo that's already
                // on the job card, so there's no local file to queue it
                // against. Rather than blocking Continue over a connection
                // the technician doesn't have, leave the edit showing on
                // screen and simply don't mark it saved - the next time
                // Continue runs (including automatically, next visit to
                // this screen) it will try the real save again.
                ClearError();
                return true;
            }

            await _apiService.UpdateEvidenceCaptionAsync(_job.ApiId, evidenceId, item.Caption);
            item.MarkSaved();
            ClearError();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Evidence caption update failed: {ex}");
            ShowError("The photo description could not be saved. Please try again.");
            return false;
        }
        finally { IsSaving = false; }
    }

    public async Task RemoveSavedEvidenceAsync(Guid evidenceId)
    {
        var item = SavedEvidence.FirstOrDefault(e => e.Id == evidenceId);
        if (item is null || IsSaving) return;

        if (!_connectivityService.IsOnline)
        {
            ShowError("Removing a saved photo currently requires an internet connection. Please reconnect before continuing.");
            return;
        }

        try
        {
            IsSaving = true;
            await _apiService.RemoveEvidenceAsync(_job.ApiId, evidenceId);
            SavedEvidence.Remove(item);
            OnPropertyChanged(nameof(HasSavedEvidence));
            ClearError();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Saved evidence removal failed: {ex}");
            ShowError("The saved photo could not be removed. Please try again.");
        }
        finally { IsSaving = false; }
    }

    // =========================================================
    // OFFLINE EVIDENCE QUEUE
    // =========================================================

    private async Task LoadPendingEvidenceAsync()
    {
        string jobId = _job.ApiId.ToString();

        List<SyncQueueItem> pending =
            await _localDatabaseService.GetPendingSyncItemsAsync();

        PendingEvidence.Clear();

        foreach (SyncQueueItem item in pending)
        {
            if (item.JobId != jobId ||
                !item.OperationType.Equals("UploadEvidence", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            EvidenceSyncPayload? payload = TryParseEvidencePayload(item.PayloadJson);
            if (payload is null || !File.Exists(payload.LocalFilePath)) continue;

            var pendingItem = new PendingEvidenceItem(item.Id, payload.Caption)
            {
                Preview = ImageSource.FromFile(payload.LocalFilePath)
            };
            PendingEvidence.Add(pendingItem);
        }

        OnPropertyChanged(nameof(HasPendingEvidence));
    }

    private async Task SaveEvidenceForLaterSyncAsync(string filePath, string caption)
    {
        // The photo is already in persistent app storage (see
        // LoadPhotoAsync), so there's nothing to copy - just queue it.
        EvidenceSyncPayload payload = new EvidenceSyncPayload
        {
            LocalFilePath = filePath,
            Caption = caption
        };

        await _localDatabaseService.AddSyncQueueItemAsync(
            new SyncQueueItem
            {
                JobId = _job.ApiId.ToString(),
                OperationType = "UploadEvidence",
                PayloadJson = JsonSerializer.Serialize(payload)
            });

        await LoadPendingEvidenceAsync();
    }

    public async Task RemovePendingEvidenceAsync(int queueItemId)
    {
        if (IsSaving) return;
        try
        {
            IsSaving = true;

            List<SyncQueueItem> pending =
                await _localDatabaseService.GetPendingSyncItemsAsync();

            SyncQueueItem? queueItem =
                pending.FirstOrDefault(item => item.Id == queueItemId);

            if (queueItem is not null)
            {
                EvidenceSyncPayload? payload = TryParseEvidencePayload(queueItem.PayloadJson);

                await _localDatabaseService.DeleteSyncQueueItemAsync(queueItem);

                if (payload is not null && File.Exists(payload.LocalFilePath))
                {
                    try { File.Delete(payload.LocalFilePath); }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Could not delete queued evidence file: {ex}");
                    }
                }
            }

            await LoadPendingEvidenceAsync();
            ClearError();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Pending evidence removal failed: {ex}");
            ShowError("The queued photo could not be removed. Please try again.");
        }
        finally { IsSaving = false; }
    }

    private static EvidenceSyncPayload? TryParseEvidencePayload(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<EvidenceSyncPayload>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    // =========================================================
    // TAKE PHOTO
    // =========================================================

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

            await LoadPhotoAsync(
                photo);
        }
        catch (PermissionException)
        {
            ShowError(
                "Camera permission was not granted.");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Photo capture failed: {ex}");

            ShowError(
                "The photo could not be captured. Please try again.");
        }
    }

    // =========================================================
    // CHOOSE PHOTO
    // =========================================================

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

            await LoadPhotoAsync(
                photo);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Photo selection failed: {ex}");

            ShowError(
                "The photo could not be selected. Please try again.");
        }
    }

    // =========================================================
    // COPY PHOTO INTO APP STORAGE
    // =========================================================

    private async Task LoadPhotoAsync(
        FileResult photo)
    {
        string extension =
            Path.GetExtension(
                photo.FileName);

        if (extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            extension = ".jpg";
        }

        if (string.IsNullOrWhiteSpace(
            extension))
        {
            extension =
                ".jpg";
        }

        string evidenceDirectory =
            Path.Combine(
                FileSystem.AppDataDirectory,
                "Evidence");

        Directory.CreateDirectory(
            evidenceDirectory);

        string safeJobNumber =
            string.IsNullOrWhiteSpace(
                _job.JobNumber)
                ? "JOB"
                : _job.JobNumber
                    .Replace("-", string.Empty)
                    .Replace(" ", string.Empty);

        string fileName =
            $"SmartJob_{safeJobNumber}_{Guid.NewGuid():N}{extension}";

        string localFilePath =
            Path.Combine(
                evidenceDirectory,
                fileName);

        await using Stream sourceStream =
            await photo.OpenReadAsync();

        await using FileStream localFileStream =
            File.Create(
                localFilePath);

        await sourceStream.CopyToAsync(
            localFileStream);

        _photoPath =
            localFilePath;

        PhotoSource =
            ImageSource.FromFile(
                localFilePath);

        HasPhoto =
            true;

        ClearError();
    }

    // =========================================================
    // REMOVE PHOTO
    // =========================================================

    private void RemovePhoto()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(
                    _photoPath)
                && File.Exists(
                    _photoPath))
            {
                File.Delete(
                    _photoPath);
            }
        }
        catch
        {
            // Still remove the preview if the
            // temporary file cannot be deleted.
        }

        _photoPath =
            null;

        PhotoSource =
            null;

        HasPhoto =
            false;

        ClearError();
    }

    // =========================================================
    // ADD PHOTO TO THE JOB
    //
    // Uploads (online) or queues (offline) whichever photo is
    // currently staged in the preview above, then clears that
    // staging slot so the technician can immediately capture
    // another one. Does NOT navigate anywhere - a job can need
    // any number of installation photos, so moving on is a
    // separate, explicit step (see ContinueAsync).
    // =========================================================

    private async Task<bool> AddCurrentPhotoAsync()
    {
        if (!HasPhoto ||
            string.IsNullOrWhiteSpace(_photoPath))
        {
            ShowError(
                "Please take or choose a photo first.");

            return false;
        }

        if (!File.Exists(_photoPath))
        {
            ShowError(
                "The selected photo could not be found. Please add the photo again.");

            return false;
        }

        Guid jobId =
            _job.ApiId;

        if (jobId == Guid.Empty)
        {
            ShowError(
                "SmartJob could not identify the current job.");

            return false;
        }

        if (IsSaving)
        {
            return false;
        }

        string caption =
            string.IsNullOrWhiteSpace(PhotoNotes)
                ? "Job evidence photo"
                : PhotoNotes.Trim();

        string extension =
            Path.GetExtension(_photoPath)
                .ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(extension))
        {
            extension =
                ".jpg";
        }

        try
        {
            IsSaving =
                true;

            if (!_connectivityService.IsOnline)
            {
                // Offline: a technician in the field often has no signal,
                // so capturing evidence can't require one. The photo is
                // already saved to this device - just queue it to upload
                // the next time SmartJob syncs.
                await SaveEvidenceForLaterSyncAsync(_photoPath, caption);

                _photoPath = null;
                PhotoSource = null;
                HasPhoto = false;
                PhotoNotes = string.Empty;

                ClearError();
                return true;
            }

            // -----------------------------------------------------
            // 1. REQUEST TEMPORARY UPLOAD URL
            // -----------------------------------------------------

            MobileEvidenceUploadResponse uploadInstruction =
                await _apiService.CreateEvidenceUploadAsync(
                    jobId,
                    extension);

            // -----------------------------------------------------
            // 2. UPLOAD ACTUAL PHOTO
            // -----------------------------------------------------

            await _apiService.UploadEvidenceFileAsync(
                uploadInstruction.UploadUrl,
                _photoPath);

            // -----------------------------------------------------
            // 3. CALCULATE SHA-256
            // -----------------------------------------------------

            string sha256 =
                await _apiService.CalculateSha256Async(
                    _photoPath);

            // -----------------------------------------------------
            // 4. CONFIRM EVIDENCE WITH SMARTJOB
            // -----------------------------------------------------

            bool confirmed =
                await _apiService.ConfirmEvidenceAsync(
                    jobId,
                    uploadInstruction.UploadToken,
                    sha256,
                    caption);

            if (!confirmed)
            {
                ShowError(
                    "The photo was uploaded but SmartJob could not confirm the evidence.");

                return false;
            }

            // -----------------------------------------------------
            // 5. CLEAR THE STAGING SLOT AND REFRESH THE SAVED LIST
            //    SO THE TECHNICIAN CAN SEE IT AND ADD ANOTHER PHOTO
            // -----------------------------------------------------

            _photoPath = null;
            PhotoSource = null;
            HasPhoto = false;
            PhotoNotes = string.Empty;

            await LoadSavedEvidenceAsync();

            ClearError();
            return true;
        }
        catch (HttpRequestException ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Evidence HTTP error: {ex}");

            ShowError(
                "SmartJob could not upload the photo evidence. Please check the connection and try again.");

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Evidence upload failed: {ex}");

            ShowError(
                "The photo evidence could not be uploaded. Please try again.");

            return false;
        }
        finally
        {
            IsSaving =
                false;
        }
    }

    // =========================================================
    // CONTINUE
    // =========================================================

    private async Task ContinueAsync()
    {
        ClearError();

        if (IsLoadingSavedEvidence || IsSaving) return;
        if (!_savedDataLoaded)
        {
            ShowError("Saved photos have not loaded. Reopen this screen while connected to the API.");
            return;
        }

        foreach (var changed in SavedEvidence.Where(item => item.HasUnsavedChanges).ToArray())
            if (!await SaveCaptionAsync(changed.Id)) return;

        // A photo sitting in the preview hasn't been added to the job
        // yet - the technician may have captured it and gone straight
        // for Continue without pressing "Add Photo". Add it now rather
        // than silently losing it.
        if (HasPhoto)
        {
            if (!await AddCurrentPhotoAsync()) return;
        }

        if (!HasSavedEvidence && !HasPendingEvidence)
        {
            ShowError(
                "Please add at least one photo before continuing.");

            return;
        }

        await Shell.Current.Navigation.PushAsync(
            new CommentsFlagPage(_job));
    }

    // =========================================================
    // ERROR HELPERS
    // =========================================================

    private void ShowError(
        string message)
    {
        ErrorMessage =
            message;

        HasError =
            true;
    }

    private void ClearError()
    {
        ErrorMessage =
            string.Empty;

        HasError =
            false;
    }

    // =========================================================
    // PROPERTY CHANGED
    // =========================================================

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

public sealed class SavedEvidenceItem : INotifyPropertyChanged
{
    private string _caption;
    private string _savedCaption;
    private ImageSource? _preview;

    public SavedEvidenceItem(Guid id, string caption)
    {
        Id = id;
        _caption = caption;
        _savedCaption = caption;
    }

    public Guid Id { get; }
    public bool HasUnsavedChanges => _caption != _savedCaption;
    public void MarkSaved() => _savedCaption = _caption;
    public string Caption
    {
        get => _caption;
        set { _caption = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Caption))); }
    }
    public ImageSource? Preview
    {
        get => _preview;
        set { _preview = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Preview))); }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

// A photo captured offline, queued locally (by its SyncQueueItem.Id)
// and waiting to be uploaded the next time SmartJob syncs.
public sealed class PendingEvidenceItem
{
    public PendingEvidenceItem(int queueItemId, string caption)
    {
        QueueItemId = queueItemId;
        Caption = caption;
    }

    public int QueueItemId { get; }
    public string Caption { get; }
    public ImageSource? Preview { get; set; }
}

// =============================================================
// EVIDENCE SYNC PAYLOAD
//
// What gets stored in a SyncQueueItem.PayloadJson for a photo that
// was captured offline and is waiting to be uploaded. Read back
// here when reloading the Photo Capture screen, and read back by
// OfflineSyncViewModel when it actually uploads it.
// =============================================================

public class EvidenceSyncPayload
{
    public string LocalFilePath { get; set; } = string.Empty;

    public string Caption { get; set; } = string.Empty;
}