using System.Collections.ObjectModel;
using SmartJobMobile.Models;
using SmartJobMobile.Services;

namespace SmartJobMobile.ViewModels;

public class MyJobsViewModel
{
    private readonly ApiService _apiService;

    public ObservableCollection<Job> Jobs { get; } = new();

    public MyJobsViewModel(ApiService apiService)
    {
        _apiService = apiService;

        _ = LoadJobsAsync();
    }

    private async Task LoadJobsAsync()
    {
        try
        {
            MyJobsResponse? response =
                await _apiService.GetMyJobsAsync();

            if (response == null)
                return;

            Jobs.Clear();

            foreach (ApiJobSummary apiJob in response.Items)
            {
                Jobs.Add(new Job
                {
                    ApiId = apiJob.Id,
                    Id = Jobs.Count + 1,

                    JobNumber = apiJob.Number,

                    Title = apiJob.TemplateName,

                    CustomerName = apiJob.CustomerName,

                    Location = apiJob.SiteName,

                    Status = FormatStatus(apiJob.Status),

                    Priority = apiJob.Priority,

                    ScheduledDate =
                        apiJob.ScheduledDate.ToDateTime(
                            TimeOnly.MinValue)
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Failed to load SmartJob jobs: {ex.Message}");

            await Shell.Current.DisplayAlert(
                "Unable to Load Jobs",
                "SmartJob could not retrieve your assigned jobs from the server.",
                "OK");
        }
    }

    private static string FormatStatus(string status)
    {
        return status switch
        {
            "InProgress" => "In Progress",
            "AwaitingApproval" => "Awaiting Approval",
            _ => status
        };
    }
}