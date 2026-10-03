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
// Matches JobCardDetailDto from the backend
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