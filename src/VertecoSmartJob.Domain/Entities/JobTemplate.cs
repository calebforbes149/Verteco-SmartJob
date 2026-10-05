using VertecoSmartJob.Domain.Common;

namespace VertecoSmartJob.Domain.Entities;

public class JobTemplate : SoftDeletableEntity
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int TimesUsed { get; set; }

    public List<TemplateChecklistItem> ChecklistItems { get; set; } = [];
}

public class TemplateChecklistItem : Entity
{
    public Guid JobTemplateId { get; set; }
    public JobTemplate? JobTemplate { get; set; }

    public int Order { get; set; }
    public string Text { get; set; } = string.Empty;
}
