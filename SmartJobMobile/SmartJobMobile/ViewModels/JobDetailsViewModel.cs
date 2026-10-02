using SmartJobMobile.Models;
using SmartJobMobile.Views;
using System.Windows.Input;

namespace SmartJobMobile.ViewModels;

public class JobDetailsViewModel
{
    public Job SelectedJob { get; }

    public ICommand StartJobCommand { get; }

    public JobDetailsViewModel(Job job)
    {
        SelectedJob = job;

        StartJobCommand = new Command(
            async () => await StartJobAsync());
    }

    private async Task StartJobAsync()
    {
        await Shell.Current.GoToAsync(nameof(ChecklistPage));
    }
}