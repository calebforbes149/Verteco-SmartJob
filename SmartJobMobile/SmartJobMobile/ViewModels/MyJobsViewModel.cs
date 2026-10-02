using System.Collections.ObjectModel;
using SmartJobMobile.Models;

namespace SmartJobMobile.ViewModels;

public class MyJobsViewModel
{
    public ObservableCollection<Job> Jobs { get; }

    public MyJobsViewModel()
    {
        Jobs = new ObservableCollection<Job>
        {
            new Job
            {
                Id = 1,
                JobNumber = "JOB-1001",
                Title = "Equipment Inspection",
                CustomerName = "Verteco Client Site",
                Location = "Johannesburg",
                Status = "Assigned",
                Priority = "High",
                ScheduledDate = DateTime.Today.AddHours(9)
            },

            new Job
            {
                Id = 2,
                JobNumber = "JOB-1002",
                Title = "Preventative Maintenance",
                CustomerName = "Verteco Client Site",
                Location = "Midrand",
                Status = "In Progress",
                Priority = "Medium",
                ScheduledDate = DateTime.Today.AddHours(11)
            },

            new Job
            {
                Id = 3,
                JobNumber = "JOB-1003",
                Title = "System Inspection",
                CustomerName = "Verteco Client Site",
                Location = "Sandton",
                Status = "Assigned",
                Priority = "Normal",
                ScheduledDate = DateTime.Today.AddDays(1).AddHours(8)
            }
        };
    }
}