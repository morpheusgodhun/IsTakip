using IsTakip.Domain.Common;

namespace IsTakip.Domain.Entities;

public class CustomFieldDefinition : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public long? WorkItemTypeId { get; set; }
    public string Name { get; set; } = default!;
    public CustomFieldType FieldType { get; set; }
    public bool IsRequired { get; set; }
    public int SortOrder { get; set; }
    public bool IsDeleted { get; set; }
    public ICollection<CustomFieldOption> Options { get; set; } = new List<CustomFieldOption>();
}

public class CustomFieldOption : BaseEntity
{
    public long CustomFieldDefinitionId { get; set; }
    public string Value { get; set; } = default!;
    public int SortOrder { get; set; }
}

public class WorkItemCustomFieldValue : BaseEntity
{
    public long WorkItemId { get; set; }
    public long CustomFieldDefinitionId { get; set; }
    public string? ValueText { get; set; }
}
