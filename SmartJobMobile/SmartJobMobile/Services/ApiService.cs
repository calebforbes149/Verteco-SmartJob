using System.Net.Http.Json;
using System.Text.Json;

namespace SmartJobMobile.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    private const string ApiKey =
        "SmartJob-Local-Thabo-2026";

    public ApiService()
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(
                "http://localhost:5196/")
        };

        _httpClient.DefaultRequestHeaders.Add(
            "X-SmartJob-Key",
            ApiKey);
    }

    // =========================================================
    // GET MY JOBS
    // =========================================================

    public async Task<MyJobsResponse?> GetMyJobsAsync()
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(
                    "api/v1/mobile/jobs?page=1&pageSize=20");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<MyJobsResponse>(
                    CreateJsonOptions());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - My Jobs: {ex.Message}");

            throw;
        }
    }

    // =========================================================
    // GET ONE JOB'S FULL DETAILS
    // =========================================================

    public async Task<ApiJobDetail?> GetJobDetailsAsync(
        Guid jobId)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(
                    $"api/v1/mobile/jobs/{jobId}");

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<ApiJobDetail>(
                    CreateJsonOptions());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Job Details: {ex.Message}");

            throw;
        }
    }

    // =========================================================
    // START JOB
    // =========================================================

    public async Task<bool> StartJobAsync(
        Guid jobId)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsync(
                    $"api/v1/mobile/jobs/{jobId}/start",
                    null);

            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Start Job: {ex.Message}");

            throw;
        }
    }

    // =========================================================
    // TOGGLE CHECKLIST ITEM
    // =========================================================

    public async Task<ApiJobDetail?> ToggleChecklistItemAsync(
        Guid jobId,
        Guid checklistItemId)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PatchAsync(
                    $"api/v1/mobile/jobs/{jobId}/checklist/{checklistItemId}/toggle",
                    null);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<ApiJobDetail>(
                    CreateJsonOptions());
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Toggle Checklist: {ex.Message}");

            throw;
        }
    }

    // =========================================================
    // ADD EQUIPMENT
    // =========================================================

    public async Task<bool> AddEquipmentAsync(
        Guid jobId,
        MobileEquipmentRequest request)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(
                    $"api/v1/mobile/jobs/{jobId}/equipment",
                    request,
                    CreateJsonOptions());

            string responseBody =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"EQUIPMENT STATUS: {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"EQUIPMENT RESPONSE: {responseBody}");

            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Equipment: {ex}");

            throw;
        }
    }

    // =========================================================
    // CREATE EVIDENCE UPLOAD INSTRUCTIONS
    // =========================================================

    public async Task<MobileEvidenceUploadResponse>
        CreateEvidenceUploadAsync(
            Guid jobId,
            string extension)
    {
        try
        {
            MobileEvidenceUploadRequest request =
                new MobileEvidenceUploadRequest
                {
                    Extension = extension
                };

            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(
                    $"api/v1/mobile/jobs/{jobId}/evidence/upload-instructions",
                    request,
                    CreateJsonOptions());

            string responseBody =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"EVIDENCE UPLOAD INSTRUCTIONS STATUS: {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"EVIDENCE UPLOAD INSTRUCTIONS RESPONSE: {responseBody}");

            response.EnsureSuccessStatusCode();

            MobileEvidenceUploadResponse? result =
                JsonSerializer.Deserialize<MobileEvidenceUploadResponse>(
                    responseBody,
                    CreateJsonOptions());

            if (result == null ||
                string.IsNullOrWhiteSpace(result.UploadToken) ||
                string.IsNullOrWhiteSpace(result.UploadUrl))
            {
                throw new InvalidOperationException(
                    "The SmartJob API did not return valid evidence upload instructions.");
            }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Evidence Upload Instructions: {ex}");

            throw;
        }
    }

    // =========================================================
    // UPLOAD EVIDENCE FILE
    // =========================================================

    public async Task UploadEvidenceFileAsync(
        string uploadUrl,
        string filePath)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(uploadUrl))
            {
                throw new ArgumentException(
                    "The evidence upload URL is missing.");
            }

            if (string.IsNullOrWhiteSpace(filePath) ||
                !File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    "The evidence photo could not be found.",
                    filePath);
            }

            string extension =
                Path.GetExtension(filePath)
                    .ToLowerInvariant();

            string contentType =
                extension switch
                {
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    _ => "application/octet-stream"
                };

            await using FileStream fileStream =
                File.OpenRead(filePath);

            using StreamContent content =
                new StreamContent(fileStream);

            content.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue(
                    contentType);

            content.Headers.Add(
                "x-ms-blob-type",
                "BlockBlob");

            using HttpClient uploadClient =
                new HttpClient();

            HttpResponseMessage response =
                await uploadClient.PutAsync(
                    uploadUrl,
                    content);

            string responseBody =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"EVIDENCE FILE UPLOAD STATUS: {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"EVIDENCE FILE UPLOAD RESPONSE: {responseBody}");

            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob Evidence File Upload Error: {ex}");

            throw;
        }
    }

    // =========================================================
    // CALCULATE SHA-256
    // =========================================================

    public async Task<string> CalculateSha256Async(
        string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) ||
            !File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "The evidence file could not be found.",
                filePath);
        }

        await using FileStream stream =
            File.OpenRead(filePath);

        using System.Security.Cryptography.SHA256 sha256 =
            System.Security.Cryptography.SHA256.Create();

        byte[] hash =
            await sha256.ComputeHashAsync(stream);

        return Convert.ToHexString(hash)
            .ToLowerInvariant();
    }

    // =========================================================
    // CONFIRM EVIDENCE
    // =========================================================

    public async Task<bool> ConfirmEvidenceAsync(
        Guid jobId,
        string uploadToken,
        string sha256,
        string caption)
    {
        try
        {
            MobileEvidenceConfirmRequest request =
                new MobileEvidenceConfirmRequest
                {
                    UploadToken = uploadToken,
                    Sha256 = sha256,
                    Caption = caption
                };

            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(
                    $"api/v1/mobile/jobs/{jobId}/evidence/confirm",
                    request,
                    CreateJsonOptions());

            string responseBody =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"EVIDENCE CONFIRM STATUS: {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"EVIDENCE CONFIRM RESPONSE: {responseBody}");

            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Evidence Confirm: {ex}");

            throw;
        }
    }

    // =========================================================
    // CONFIRM SIGNATURE
    // Confirms an uploaded technician or client signature
    // =========================================================

    public async Task<bool> ConfirmSignatureAsync(
        Guid jobId,
        string uploadToken,
        string sha256,
        string signerName,
        string type)
    {
        try
        {
            MobileSignatureConfirmRequest request =
                new MobileSignatureConfirmRequest
                {
                    UploadToken = uploadToken,
                    Sha256 = sha256,
                    SignerName = signerName,
                    Type = type
                };

            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(
                    $"api/v1/mobile/jobs/{jobId}/signatures/confirm",
                    request,
                    CreateJsonOptions());

            string responseBody =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"SIGNATURE CONFIRM STATUS: {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"SIGNATURE CONFIRM RESPONSE: {responseBody}");

            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Signature Confirm: {ex}");

            throw;
        }
    }

    // =========================================================
    // SUBMIT JOB FOR REVIEW
    // =========================================================

    public async Task<bool> SubmitJobAsync(
        Guid jobId)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsync(
                    $"api/v1/mobile/jobs/{jobId}/submit",
                    null);

            string responseBody =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"SUBMIT JOB STATUS: {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"SUBMIT JOB RESPONSE: {responseBody}");

            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Submit Job: {ex}");

            throw;
        }
    }

    // =========================================================
    // FLAG JOB
    // Allows technician to flag their assigned job
    // =========================================================

    public async Task<bool> FlagJobAsync(
        Guid jobId,
        string reason)
    {
        try
        {
            FlagJobRequest request =
                new FlagJobRequest
                {
                    Reason = reason
                };

            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(
                    $"api/v1/jobs/{jobId}/flag",
                    request,
                    CreateJsonOptions());

            string responseBody =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"FLAG JOB STATUS: {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"FLAG JOB RESPONSE: {responseBody}");

            response.EnsureSuccessStatusCode();

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Flag Job: {ex}");

            throw;
        }
    }

    // =========================================================
    // SYNC PULL
    // =========================================================

    public async Task<MobileSyncPullResponse?>
        PullSyncDataAsync()
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.GetAsync(
                    "api/v1/mobile/sync/pull");

            string rawJson =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"SYNC PULL STATUS: {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"SYNC PULL JSON: {rawJson}");

            response.EnsureSuccessStatusCode();

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                throw new Exception(
                    "The sync API returned an empty response.");
            }

            MobileSyncPullResponse? result =
                JsonSerializer.Deserialize<MobileSyncPullResponse>(
                    rawJson,
                    CreateJsonOptions());

            if (result == null)
            {
                throw new Exception(
                    "The sync API response could not be converted into MobileSyncPullResponse.");
            }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Sync Pull: {ex}");

            throw;
        }
    }

    // =========================================================
    // SYNC PUSH
    // =========================================================

    public async Task<MobileSyncPushResponse?>
        PushSyncDataAsync(
            MobileSyncPushRequest request)
    {
        try
        {
            HttpResponseMessage response =
                await _httpClient.PostAsJsonAsync(
                    "api/v1/mobile/sync/push",
                    request,
                    CreateJsonOptions());

            string rawJson =
                await response.Content.ReadAsStringAsync();

            System.Diagnostics.Debug.WriteLine(
                $"SYNC PUSH STATUS: {(int)response.StatusCode} {response.StatusCode}");

            System.Diagnostics.Debug.WriteLine(
                $"SYNC PUSH JSON: {rawJson}");

            response.EnsureSuccessStatusCode();

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                throw new Exception(
                    "The sync push API returned an empty response.");
            }

            MobileSyncPushResponse? result =
                JsonSerializer.Deserialize<MobileSyncPushResponse>(
                    rawJson,
                    CreateJsonOptions());

            if (result == null)
            {
                throw new Exception(
                    "The sync push response could not be converted into MobileSyncPushResponse.");
            }

            return result;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"SmartJob API Error - Sync Push: {ex}");

            throw;
        }
    }

    // =========================================================
    // JSON OPTIONS
    // =========================================================

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}


// =============================================================
// MY JOBS RESPONSE
// =============================================================

public class MyJobsResponse
{
    public List<ApiJobSummary> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }
}


// =============================================================
// JOB SUMMARY
// =============================================================

public class ApiJobSummary
{
    public Guid Id { get; set; }

    public string Number { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public string Priority { get; set; } =
        string.Empty;

    public string CustomerName { get; set; } =
        string.Empty;

    public string SiteName { get; set; } =
        string.Empty;

    public string TemplateName { get; set; } =
        string.Empty;

    public string TechnicianName { get; set; } =
        string.Empty;

    public DateOnly ScheduledDate { get; set; }

    public bool IsFlagged { get; set; }

    public string? FlagReason { get; set; }
}


// =============================================================
// FULL JOB DETAILS
// =============================================================

public class ApiJobDetail
{
    public Guid Id { get; set; }

    public string Number { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public string Priority { get; set; } =
        string.Empty;

    public Guid CustomerId { get; set; }

    public string CustomerName { get; set; } =
        string.Empty;

    public Guid SiteId { get; set; }

    public string SiteName { get; set; } =
        string.Empty;

    public Guid TemplateId { get; set; }

    public string TemplateName { get; set; } =
        string.Empty;

    public string TemplateCategory { get; set; } =
        string.Empty;

    public Guid? TechnicianId { get; set; }

    public string? TechnicianName { get; set; }

    public List<string> TechnicianSkills { get; set; } = [];

    public DateOnly ScheduledDate { get; set; }

    public DateTime? CompletedAtUtc { get; set; }

    public string? Notes { get; set; }

    public string? ReviewNotes { get; set; }

    public string? ReopenReason { get; set; }

    public string? CancelReason { get; set; }

    public bool IsFlagged { get; set; }

    public string? FlagReason { get; set; }

    public DateTime? FlaggedAtUtc { get; set; }

    public int EvidenceCount { get; set; }

    public bool HasTechnicianSignature { get; set; }

    public bool HasClientSignature { get; set; }

    public List<ApiChecklistItem> ChecklistItems { get; set; } = [];
}


// =============================================================
// CHECKLIST ITEM
// =============================================================

public class ApiChecklistItem
{
    public Guid Id { get; set; }

    public int Order { get; set; }

    public string Text { get; set; } =
        string.Empty;

    public bool IsDone { get; set; }
}


// =============================================================
// MOBILE SYNC PULL RESPONSE
// =============================================================

public class MobileSyncPullResponse
{
    public DateTime ServerTimeUtc { get; set; }

    public List<MobileSyncJob> Jobs { get; set; } = [];
}


// =============================================================
// MOBILE SYNC JOB
// =============================================================

public class MobileSyncJob
{
    public Guid Id { get; set; }

    public string Number { get; set; } =
        string.Empty;

    public string Status { get; set; } =
        string.Empty;

    public string RowVersion { get; set; } =
        string.Empty;

    public DateOnly ScheduledDate { get; set; }

    public string? Notes { get; set; }

    public List<MobileSyncChecklistItem>
        ChecklistItems
    { get; set; } = [];
}


// =============================================================
// MOBILE SYNC CHECKLIST ITEM
// =============================================================

public class MobileSyncChecklistItem
{
    public Guid Id { get; set; }

    public string Text { get; set; } =
        string.Empty;

    public bool IsDone { get; set; }

    public string RowVersion { get; set; } =
        string.Empty;
}


// =============================================================
// MOBILE SYNC PUSH REQUEST
// =============================================================

public class MobileSyncPushRequest
{
    public List<MobileSyncOperationRequest>
        Operations
    { get; set; } = [];
}


// =============================================================
// MOBILE SYNC OPERATION REQUEST
// =============================================================

public class MobileSyncOperationRequest
{
    public Guid OperationId { get; set; }

    public Guid JobCardId { get; set; }

    public string OperationType { get; set; } =
        string.Empty;

    public string RowVersion { get; set; } =
        string.Empty;

    public Guid? ChecklistItemId { get; set; }

    public string? ChecklistItemRowVersion { get; set; }

    public bool? IsDone { get; set; }
}


// =============================================================
// MOBILE SYNC PUSH RESPONSE
// =============================================================

public class MobileSyncPushResponse
{
    public List<MobileSyncOperationResult>
        Results
    { get; set; } = [];
}


// =============================================================
// MOBILE SYNC OPERATION RESULT
// =============================================================

public class MobileSyncOperationResult
{
    public Guid OperationId { get; set; }

    public Guid JobCardId { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public string? RowVersion { get; set; }

    public string? Message { get; set; }
}


// =============================================================
// MOBILE EQUIPMENT REQUEST
// =============================================================

public class MobileEquipmentRequest
{
    public string Name { get; set; } =
        string.Empty;

    public string SerialNumber { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;
}


// =============================================================
// MOBILE EVIDENCE UPLOAD REQUEST
// =============================================================

public class MobileEvidenceUploadRequest
{
    public string Extension { get; set; } =
        string.Empty;
}


// =============================================================
// MOBILE EVIDENCE UPLOAD RESPONSE
// =============================================================

public class MobileEvidenceUploadResponse
{
    public string UploadToken { get; set; } =
        string.Empty;

    public string UploadUrl { get; set; } =
        string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }
}


// =============================================================
// MOBILE EVIDENCE CONFIRM REQUEST
// =============================================================

public class MobileEvidenceConfirmRequest
{
    public string UploadToken { get; set; } =
        string.Empty;

    public string Sha256 { get; set; } =
        string.Empty;

    public string Caption { get; set; } =
        string.Empty;
}


// =============================================================
// FLAG JOB REQUEST
// =============================================================

public class FlagJobRequest
{
    public string Reason { get; set; } =
        string.Empty;
}

// =============================================================
// MOBILE SIGNATURE CONFIRM REQUEST
// =============================================================

public class MobileSignatureConfirmRequest
{
    public string UploadToken { get; set; } =
        string.Empty;

    public string Sha256 { get; set; } =
        string.Empty;

    public string SignerName { get; set; } =
        string.Empty;

    public string Type { get; set; } =
        string.Empty;
}