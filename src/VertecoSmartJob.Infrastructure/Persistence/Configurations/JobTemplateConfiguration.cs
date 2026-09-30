using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VertecoSmartJob.Domain.Entities;

namespace VertecoSmartJob.Infrastructure.Persistence.Configurations;

public class JobTemplateConfiguration : IEntityTypeConfiguration<JobTemplate>
{
    public void Configure(EntityTypeBuilder<JobTemplate> b)
    {
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Category).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsConcurrencyToken();

        b.HasMany(x => x.ChecklistItems)
            .WithOne(i => i.JobTemplate)
            .HasForeignKey(i => i.JobTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TemplateChecklistItemConfiguration : IEntityTypeConfiguration<TemplateChecklistItem>
{
    public void Configure(EntityTypeBuilder<TemplateChecklistItem> b)
    {
        b.Property(x => x.Text).IsRequired().HasMaxLength(300);
    }
}
