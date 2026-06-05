using IsTakip.Domain.Common;

namespace IsTakip.Domain.Entities;

public class KbCategory : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public string Name { get; set; } = default!;
    public long? ParentCategoryId { get; set; }
    public bool IsDeleted { get; set; }
    public KbCategory? Parent { get; set; }
}

public class KbArticle : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public long? CategoryId { get; set; }
    public string Title { get; set; } = default!;
    public string? Body { get; set; }
    public int CurrentVersion { get; set; } = 1;
    public long? CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public KbCategory? Category { get; set; }
}

public class KbArticleVersion : BaseEntity
{
    public long ArticleId { get; set; }
    public int Version { get; set; }
    public string? Body { get; set; }
    public long? EditedByUserId { get; set; }
    public DateTime EditedAtUtc { get; set; }
}
