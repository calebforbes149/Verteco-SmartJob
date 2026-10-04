using VertecoSmartJob.Domain.Entities;
using VertecoSmartJob.Domain.Enums;

namespace VertecoSmartJob.Infrastructure.Persistence.Seed;

/// <summary>
/// South African seed dataset for Verteco SmartJob, adapted from the Part 1 prototype's dummy
/// data (same technicians, customers, sites and job types) so screenshots and demo walkthroughs
/// stay consistent between the prototype and the real system.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (db.Technicians.Any())
        {
            if (!db.JobTemplates.Any(t => t.Name == "Standard Job Card"))
            {
                db.JobTemplates.Add(BuildTemplates()[0]);
                await db.SaveChangesAsync();
            }
            if (!db.Users.Any())
            {
                db.Users.AddRange(BuildUsers(db.Technicians.ToList()));
                await db.SaveChangesAsync();
            }
            return;
        }

        var technicians = BuildTechnicians();
        db.Technicians.AddRange(technicians);
        db.Users.AddRange(BuildUsers(technicians));

        var customers = BuildCustomersWithSites();
        db.Customers.AddRange(customers);

        var templates = BuildTemplates();
        db.JobTemplates.AddRange(templates);

        await db.SaveChangesAsync();

        var jobs = BuildJobs(customers, templates, technicians);
        db.JobCards.AddRange(jobs);
        foreach (var job in jobs)
        {
            var scheduledUtc = DateTime.SpecifyKind(job.ScheduledDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
            var createdUtc = scheduledUtc.AddDays(-3);
            if (createdUtc > DateTime.UtcNow) createdUtc = DateTime.UtcNow.AddDays(-1);
            db.Entry(job).Property(j => j.CreatedAtUtc).CurrentValue = createdUtc;
            if (job.Status == JobStatus.Completed)
                db.Entry(job).Property(j => j.CompletedAtUtc).CurrentValue = scheduledUtc.AddHours(18);
        }
        await db.SaveChangesAsync();

    }

    private static List<Technician> BuildTechnicians() =>
    [
        new() { FullName = "Thabo Nkosi", Email = "thabo.nkosi@verteco.co.za", Phone = "071 220 4481", Skills = ["Power Systems", "Generators"], Availability = TechnicianAvailability.OnJob, JobsCompleted = 38 },
        new() { FullName = "Sipho Dlamini", Email = "sipho.dlamini@verteco.co.za", Phone = "082 663 1190", Skills = ["IT Infrastructure", "Networking"], Availability = TechnicianAvailability.Available, JobsCompleted = 24 },
        new() { FullName = "Johan van der Merwe", Email = "johan.vdm@verteco.co.za", Phone = "083 015 7742", Skills = ["Power Systems", "Solar & Inverter"], Availability = TechnicianAvailability.OnJob, JobsCompleted = 51 },
        new() { FullName = "Fatima Adams", Email = "fatima.adams@verteco.co.za", Phone = "076 902 3315", Skills = ["Switchgear & Cabinets", "Networking", "IT Infrastructure"], Availability = TechnicianAvailability.Available, JobsCompleted = 29 },
        new() { FullName = "Lerato Molefe", Email = "lerato.molefe@verteco.co.za", Phone = "079 554 6620", Skills = ["Solar & Inverter", "Power Systems"], Availability = TechnicianAvailability.OffDuty, JobsCompleted = 33 },
        new() { FullName = "Kabelo Mokoena", Email = "kabelo.mokoena@verteco.co.za", Phone = "084 771 9903", Skills = ["IT Infrastructure", "Server Hardware"], Availability = TechnicianAvailability.Available, JobsCompleted = 19 }
    ];

    private static List<User> BuildUsers(List<Technician> technicians)
    {
        var users = technicians.Select(t => new User
        {
            FullName = t.FullName, Email = t.Email, Phone = t.Phone,
            Role = "Technician", TechnicianId = t.Id
        }).ToList();
        users.Add(new User
        {
            FullName = "SmartJob Manager", Email = "manager@verteco.example", Phone = "011 000 0000", Role = "Manager"
        });
        return users;
    }

    private static List<Customer> BuildCustomersWithSites()
    {
        var nexus = new Customer
        {
            Name = "Nexus Data Centres (Pty) Ltd", Industry = "Data Centre",
            ContactName = "Priya Naidoo", ContactEmail = "priya.naidoo@nexusdc.co.za", ContactPhone = "011 402 8871",
            Sites =
            [
                new Site
                {
                    Name = "Nexus DC1 — Midrand", Address = "14 Allandale Rd, Midrand, Gauteng", SiteType = "Data Centre",
                    Equipment = [
                        new Equipment { Name = "60kVA UPS Unit A", SerialNumber = "UPS-60-MID-001" },
                        new Equipment { Name = "60kVA UPS Unit B", SerialNumber = "UPS-60-MID-002" },
                        new Equipment { Name = "Precision Cooling Unit", SerialNumber = "PCU-MID-014" }
                    ]
                },
                new Site
                {
                    Name = "Nexus DC2 — Cape Town", Address = "7 Century Blvd, Century City, Cape Town", SiteType = "Data Centre",
                    Equipment = [ new Equipment { Name = "40kVA UPS Unit", SerialNumber = "UPS-40-CPT-003" } ]
                }
            ]
        };
        var randPlatinum = new Customer
        {
            Name = "Rand Platinum Mining Co.", Industry = "Industrial",
            ContactName = "Willem Botha", ContactEmail = "w.botha@randplatinum.co.za", ContactPhone = "014 592 3310",
            Sites =
            [
                new Site
                {
                    Name = "Rustenburg Processing Plant", Address = "R565 Industrial Rd, Rustenburg, North West", SiteType = "Industrial Plant",
                    Equipment = [ new Equipment { Name = "500kVA Standby Generator", SerialNumber = "GEN-500-RBG-009" } ]
                }
            ]
        };
        var sandton = new Customer
        {
            Name = "Sandton Square Retail Park", Industry = "Commercial / Retail",
            ContactName = "Naledi Khumalo", ContactEmail = "naledi.k@sandtonsquare.co.za", ContactPhone = "011 883 4420",
            Sites =
            [
                new Site
                {
                    Name = "Sandton Square — Mall Wing", Address = "5th St, Sandton, Gauteng", SiteType = "Retail",
                    Equipment = [ new Equipment { Name = "250kVA Standby Generator", SerialNumber = "GEN-250-SAN-011" } ]
                },
                new Site
                {
                    Name = "Sandton Square — Parkade", Address = "5th St, Sandton, Gauteng", SiteType = "Retail / Parking",
                    Equipment = [ new Equipment { Name = "LV Switchgear Cabinet", SerialNumber = "SWG-CAB-SAN-004" } ]
                }
            ]
        };
        var vaalWater = new Customer
        {
            Name = "Vaal Water Utilities", Industry = "Utilities",
            ContactName = "Pieter Jacobs", ContactEmail = "p.jacobs@vaalwater.gov.za", ContactPhone = "016 340 7765",
            Sites =
            [
                new Site
                {
                    Name = "Vaal Pump Station 3", Address = "Vereeniging Rd, Vanderbijlpark, Gauteng", SiteType = "Utility Infrastructure",
                    Equipment = [ new Equipment { Name = "350kVA Generator", SerialNumber = "GEN-350-VAL-002" } ]
                }
            ]
        };
        var bakery = new Customer
        {
            Name = "Thabo's Fresh Bakery Group", Industry = "Commercial / Food",
            ContactName = "Zanele Mahlangu", ContactEmail = "zanele@freshbakery.co.za", ContactPhone = "010 224 5581",
            Sites =
            [
                new Site
                {
                    Name = "Bakery HQ — Germiston", Address = "22 Booysens Ave, Germiston, Gauteng", SiteType = "Commercial / Industrial",
                    Equipment = [ new Equipment { Name = "Rooftop Solar Array 80kWp", SerialNumber = "SOL-80K-GER-001" } ]
                },
                new Site
                {
                    Name = "Bryanston Branch", Address = "Main Rd, Bryanston, Gauteng", SiteType = "Retail",
                    Equipment = [ new Equipment { Name = "Back Office Server Rack", SerialNumber = "RACK-BRY-007" } ]
                }
            ]
        };

        return [nexus, randPlatinum, sandton, vaalWater, bakery];
    }

    private static List<JobTemplate> BuildTemplates() =>
    [
        NewTemplate("Standard Job Card", "All field work",
            "The single standard checklist used for every Verteco job card.", 0,
            "Confirm site access and safety controls",
            "Record equipment and serial numbers",
            "Complete installation, inspection or maintenance work",
            "Record test results and outstanding issues",
            "Capture photographic evidence",
            "Obtain technician and client sign-off")
    ];

    private static JobTemplate NewTemplate(string name, string category, string description, int used, params string[] checklist)
    {
        var template = new JobTemplate { Name = name, Category = category, Description = description, TimesUsed = used };
        var order = 0;
        foreach (var text in checklist)
            template.ChecklistItems.Add(new TemplateChecklistItem { Order = order++, Text = text });
        return template;
    }

    private static List<JobCard> BuildJobs(List<Customer> customers, List<JobTemplate> templates, List<Technician> technicians)
    {
        var nexus = customers[0]; var randPlatinum = customers[1]; var sandton = customers[2];
        var vaalWater = customers[3]; var bakery = customers[4];
        var upsTpl = templates[0]; var genTpl = templates[0]; var rackTpl = templates[0]; var solarTpl = templates[0]; var switchTpl = templates[0];
        var thabo = technicians[0]; var sipho = technicians[1]; var johan = technicians[2];
        var fatima = technicians[3]; var lerato = technicians[4]; var kabelo = technicians[5];

        var n = 1000;
        var jobs = new List<JobCard>
        {
            // Completed
            Build(++n, sandton, sandton.Sites[1], switchTpl, "New LV distribution cabinet for parkade boom gate circuits.", JobPriority.Medium, -25, fatima, JobLifecycle.Completed),
            Build(++n, bakery, bakery.Sites[1], rackTpl, "Back-office rack refresh — swap ageing switch for managed unit.", JobPriority.Low, -21, kabelo, JobLifecycle.Completed),
            Build(++n, nexus, nexus.Sites[0], upsTpl, "Second 60kVA UPS added for N+1 redundancy.", JobPriority.High, -15, thabo, JobLifecycle.Completed),

            // Awaiting approval
            Build(++n, nexus, nexus.Sites[1], rackTpl, "New rack B9 for expansion — client wants sign-off before going live.", JobPriority.High, -4, fatima, JobLifecycle.AwaitingApproval),

            // In progress
            Build(++n, randPlatinum, randPlatinum.Sites[0], genTpl, "Routine quarterly service on main standby generator.", JobPriority.Urgent, -1, lerato, JobLifecycle.InProgress),
            Build(++n, randPlatinum, randPlatinum.Sites[0], genTpl, "Follow-up service after flag — replacing coolant sensor.", JobPriority.Urgent, 2, thabo, JobLifecycle.InProgress),
            Build(++n, bakery, bakery.Sites[0], solarTpl, "80kWp rooftop array — inverter commissioning in progress.", JobPriority.Medium, 1, johan, JobLifecycle.InProgress),
            Build(++n, sandton, sandton.Sites[0], genTpl, "Generator service — access issue raised during visit.", JobPriority.High, -3, lerato, JobLifecycle.InProgress),

            // Reopened (checklist was sent back for rework by the manager)
            Build(++n, vaalWater, vaalWater.Sites[0], genTpl, "Annual service — manager sent back for a re-check on coolant readings.", JobPriority.High, -2, sipho, JobLifecycle.Reopened),

            // Accepted (technician confirmed, not yet started)
            Build(++n, vaalWater, vaalWater.Sites[0], upsTpl, "New UPS for control room — coordinating with Rustenburg team for parts.", JobPriority.Medium, 6, thabo, JobLifecycle.Accepted),

            // Assigned (waiting on technician acceptance)
            Build(++n, sandton, sandton.Sites[0], rackTpl, "New POS server rack for mall management office.", JobPriority.Medium, 4, kabelo, JobLifecycle.Assigned),

            // Draft (needs a technician)
            Build(++n, vaalWater, vaalWater.Sites[0], switchTpl, "Annual service ahead of load-shedding season — needs Power Systems tech.", JobPriority.High, 5, null, JobLifecycle.Draft),
            Build(++n, nexus, nexus.Sites[0], switchTpl, "Add a second distribution cabinet for server hall expansion circuit.", JobPriority.Low, 8, null, JobLifecycle.Draft),

            // Cancelled
            Build(++n, bakery, bakery.Sites[1], rackTpl, "Duplicate request — superseded by JC covering the same rack.", JobPriority.Low, -6, null, JobLifecycle.Cancelled)
        };

        return jobs;
    }

    private enum JobLifecycle { Draft, Assigned, Accepted, InProgress, AwaitingApproval, Completed, Reopened, Cancelled }

    private static JobCard Build(int number, Customer customer, Site site, JobTemplate template, string notes, JobPriority priority,
        int scheduledOffsetDays, Technician? technician, JobLifecycle target)
    {
        var scheduledDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(scheduledOffsetDays));
        var job = JobCard.Create($"JC-{DateTime.UtcNow.Year}-{number}", customer.Id, site.Id, template.Id, priority,
            scheduledDate, notes, template.ChecklistItems.OrderBy(i => i.Order).Select(i => i.Text));

        if (target == JobLifecycle.Draft) return job;

        if (target == JobLifecycle.Cancelled)
        {
            job.Cancel("Duplicate request superseded by another job card.");
            return job;
        }

        job.AssignTo(technician!.Id);
        if (target == JobLifecycle.Assigned) return job;

        job.Accept(technician.Id);
        if (target == JobLifecycle.Accepted) return job;

        job.Start(technician.Id);
        if (target == JobLifecycle.InProgress)
        {
            // leave a realistic partial checklist for jobs still being worked
            var doneCount = job.ChecklistItems.Count / 2;
            for (var i = 0; i < doneCount; i++) job.ToggleChecklistItem(job.ChecklistItems[i].Id, technician.Id);
            return job;
        }

        foreach (var item in job.ChecklistItems) job.ToggleChecklistItem(item.Id, technician.Id);

        job.AddEvidence(technician.Id, $"demo-evidence/{job.Number}/site-photo.jpg", "Site work photo");
        job.AddSignature(technician.Id, technician.FullName, "Technician", $"demo-signatures/{job.Number}/technician");
        job.AddSignature(technician.Id, "Site representative", "Client", $"demo-signatures/{job.Number}/client");

        if (target == JobLifecycle.Reopened)
        {
            job.SubmitForReview(technician.Id);
            job.Reopen("Coolant temperature readings look off — please re-run the load test and log fresh values.");
            return job;
        }

        job.SubmitForReview(technician.Id);
        if (target == JobLifecycle.AwaitingApproval) return job;

        job.Complete("Verified on site walkthrough. Signed off by client.");
        return job;
    }
}
