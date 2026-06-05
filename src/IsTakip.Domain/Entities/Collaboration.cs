using IsTakip.Domain.Common;

namespace IsTakip.Domain.Entities;

public class Comment : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public long WorkItemId { get; set; }
    public long AuthorUserId { get; set; }
    public string Body { get; set; } = default!;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
    public WorkItem WorkItem { get; set; } = default!;
    public AppUser Author { get; set; } = default!;
    public ICollection<CommentMention> Mentions { get; set; } = new List<CommentMention>();
}

public class CommentMention
{
    public long CommentId { get; set; }
    public long MentionedUserId { get; set; }
    public Comment Comment { get; set; } = default!;
}

public class Attachment : BaseEntity, ITenantEntity, ISoftDeletable
{
    public long TenantId { get; set; }
    public long? WorkItemId { get; set; }
    public long? CommentId { get; set; }
    public string FileName { get; set; } = default!;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; } = default!;
    public int Version { get; set; } = 1;
    public long? ParentAttachmentId { get; set; }
    public long? UploadedByUserId { get; set; }
    public DateTime UploadedAtUtc { get; set; }
    public bool IsDeleted { get; set; }
}
