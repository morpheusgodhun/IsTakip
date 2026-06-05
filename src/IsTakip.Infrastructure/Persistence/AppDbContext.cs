using System.Linq.Expressions;
using IsTakip.Application.Common;
using IsTakip.Domain.Common;
using IsTakip.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace IsTakip.Infrastructure.Persistence;

/// <summary>
/// Uygulama veritabanı bağlamı. ASP.NET Core Identity'yi (long anahtar) mevcut
/// dbo.Users / dbo.Roles tablolarına map eder, multi-tenant ve soft-delete için
/// global query filter uygular. Şema kaynağı SQL scriptleridir; bu bağlam ona eşlenir.
/// </summary>
public class AppDbContext : IdentityDbContext<AppUser, AppRole, long>
{
    private readonly ICurrentUserService _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    /// <summary>Global tenant filtresinde kullanılır. Oturum yoksa 0 döner (tenant kayıtları boş gelir).</summary>
    public long CurrentTenantId => _currentUser.TenantId ?? 0;

    // --- Çekirdek & sistem ---
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<InventoryCategory> InventoryCategories => Set<InventoryCategory>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryAssignment> InventoryAssignments => Set<InventoryAssignment>();
    public DbSet<InventoryCount> InventoryCounts => Set<InventoryCount>();
    public DbSet<InventoryCountLine> InventoryCountLines => Set<InventoryCountLine>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();

    // --- Organizasyon ---
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Branch> Branches => Set<Branch>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();

    // --- İş akışı & öncelik ---
    public DbSet<Priority> Priorities => Set<Priority>();
    public DbSet<Workflow> Workflows => Set<Workflow>();
    public DbSet<WorkflowState> WorkflowStates => Set<WorkflowState>();
    public DbSet<WorkflowTransition> WorkflowTransitions => Set<WorkflowTransition>();

    // --- Görev / talep ---
    public DbSet<WorkItemType> WorkItemTypes => Set<WorkItemType>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<WorkPeriod> WorkPeriods => Set<WorkPeriod>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<WorkItemLabel> WorkItemLabels => Set<WorkItemLabel>();
    public DbSet<WorkItemWatcher> WorkItemWatchers => Set<WorkItemWatcher>();
    public DbSet<WorkItemStateHistory> WorkItemStateHistory => Set<WorkItemStateHistory>();

    // --- İşbirliği ---
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentMention> CommentMentions => Set<CommentMention>();
    public DbSet<Attachment> Attachments => Set<Attachment>();

    // --- Süreç ---
    public DbSet<TimeLog> TimeLogs => Set<TimeLog>();
    public DbSet<ApprovalRequest> ApprovalRequests => Set<ApprovalRequest>();
    public DbSet<ApprovalStep> ApprovalSteps => Set<ApprovalStep>();
    public DbSet<AutomationRule> AutomationRules => Set<AutomationRule>();
    public DbSet<AutomationCondition> AutomationConditions => Set<AutomationCondition>();
    public DbSet<AutomationAction> AutomationActions => Set<AutomationAction>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();

    // --- Bilgi bankası ---
    public DbSet<KbCategory> KbCategories => Set<KbCategory>();
    public DbSet<KbArticle> KbArticles => Set<KbArticle>();
    public DbSet<KbArticleVersion> KbArticleVersions => Set<KbArticleVersion>();

    // --- Özel alanlar ---
    public DbSet<CustomFieldDefinition> CustomFieldDefinitions => Set<CustomFieldDefinition>();
    public DbSet<CustomFieldOption> CustomFieldOptions => Set<CustomFieldOption>();
    public DbSet<WorkItemCustomFieldValue> WorkItemCustomFieldValues => Set<WorkItemCustomFieldValue>();

    // --- Denetim ---
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        MapIdentityToExistingTables(builder);
        ConfigureKeysAndShape(builder);
        ApplyGlobalFilters(builder);

        // Şemada hiçbir cascade tanımlı değil; EF'i de buna hizalayıp çoklu cascade-path
        // hatalarını ve istenmeyen zincirleme silmeleri önlüyoruz.
        foreach (var fk in builder.Model.GetEntityTypes().SelectMany(t => t.GetForeignKeys()))
            fk.DeleteBehavior = DeleteBehavior.Restrict;
    }

    private static void MapIdentityToExistingTables(ModelBuilder b)
    {
        b.Entity<AppUser>(e =>
        {
            e.ToTable("Users");
            e.Property(u => u.LockoutEnd).HasColumnName("LockoutEndUtc");
        });
        b.Entity<AppRole>().ToTable("Roles");
        b.Entity<IdentityUserRole<long>>().ToTable("UserRoles");
        b.Entity<IdentityUserClaim<long>>().ToTable("UserClaims");
        b.Entity<IdentityUserLogin<long>>().ToTable("UserLogins");
        b.Entity<IdentityUserToken<long>>().ToTable("UserTokens");
        b.Entity<IdentityRoleClaim<long>>().ToTable("RoleClaims");
    }

    private static void ConfigureKeysAndShape(ModelBuilder b)
    {
        // Bileşik anahtarlı bağlantı tabloları
        b.Entity<RolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
        b.Entity<TeamMember>().HasKey(x => new { x.TeamId, x.UserId });
        b.Entity<ProjectMember>().HasKey(x => new { x.ProjectId, x.UserId });
        b.Entity<WorkItemLabel>().HasKey(x => new { x.WorkItemId, x.LabelId });
        b.Entity<WorkItemWatcher>().HasKey(x => new { x.WorkItemId, x.UserId });
        b.Entity<CommentMention>().HasKey(x => new { x.CommentId, x.MentionedUserId });
        b.Entity<NotificationPreference>().HasIndex(x => new { x.UserId, x.NotificationType }).IsUnique();
        b.Entity<WorkItemCustomFieldValue>().HasIndex(x => new { x.WorkItemId, x.CustomFieldDefinitionId }).IsUnique();

        // RolePermission -> Role ilişkisi (RolePermissions koleksiyonu)
        b.Entity<RolePermission>()
            .HasOne(x => x.Role).WithMany(r => r.RolePermissions).HasForeignKey(x => x.RoleId);
        b.Entity<RolePermission>()
            .HasOne(x => x.Permission).WithMany(p => p.RolePermissions).HasForeignKey(x => x.PermissionId);

        // Optimistic concurrency
        b.Entity<WorkItem>().Property(x => x.RowVersion).IsRowVersion();

        // Parasal alan hassasiyeti
        b.Entity<Project>().Property(x => x.Budget).HasPrecision(18, 2);

        // WorkItem ilişkilerinde gölge/çoklu FK belirsizliğini netleştir
        b.Entity<WorkItem>(e =>
        {
            e.HasOne(x => x.Type).WithMany().HasForeignKey(x => x.WorkItemTypeId);
            e.HasOne(x => x.Workflow).WithMany().HasForeignKey(x => x.WorkflowId);
            e.HasOne(x => x.CurrentState).WithMany().HasForeignKey(x => x.CurrentStateId);
            e.HasOne(x => x.Assignee).WithMany().HasForeignKey(x => x.AssigneeUserId);
            e.HasOne(x => x.Reporter).WithMany().HasForeignKey(x => x.ReporterUserId);
            e.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentWorkItemId);
        });

        // AppUser <-> Department arasında İKİ ayrı ilişki var; ikisini de açıkça tanımla:
        //  - Kullanıcının bağlı olduğu departman (AppUser.DepartmentId)
        //  - Departmanın yöneticisi (Department.ManagerUserId)
        // Açık tanım olmazsa EF bu iki navigasyonu yanlışlıkla birbirinin tersi sanar.
        b.Entity<AppUser>().HasOne(u => u.Department).WithMany().HasForeignKey(u => u.DepartmentId);
        b.Entity<Department>().HasOne(x => x.Manager).WithMany().HasForeignKey(x => x.ManagerUserId);

        // Navigation adı ile FK kolon adının uyuşmadığı ilişkiler. Açıkça bağlanmazsa
        // EF convention "<Nav>Id" (AuthorId/LeadId/ParentId) adında olmayan bir gölge
        // kolon uydurur ve INSERT/UPDATE'te "Invalid column name" hatası alınır.
        b.Entity<Comment>().HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorUserId);
        b.Entity<Comment>().HasOne(c => c.WorkItem).WithMany(w => w.Comments).HasForeignKey(c => c.WorkItemId);
        b.Entity<Project>().HasOne(p => p.Lead).WithMany().HasForeignKey(p => p.LeadUserId);
        b.Entity<KbCategory>().HasOne(k => k.Parent).WithMany().HasForeignKey(k => k.ParentCategoryId);

        // Bu tabloların şemasında bazı audit kolonları yok (entity AuditableTenantEntity'den
        // türese de). EF'in olmayan kolonlara yazmasını engellemek için bunları yok say.
        // (CreatedAtUtc Companies/Projects'te var, korunur.)
        b.Entity<Company>().Ignore(x => x.CreatedByUserId).Ignore(x => x.UpdatedAtUtc).Ignore(x => x.UpdatedByUserId);
        b.Entity<Project>().Ignore(x => x.UpdatedByUserId);
    }

    /// <summary>
    /// Tüm ITenantEntity'lere tenant filtresi, tüm ISoftDeletable'lara silinmemiş filtresi uygular.
    /// İstisna: AppUser/AppRole tenant filtresine dahil edilmez (giriş akışında çapraz-tenant
    /// arama gerektiğinden), yalnızca soft-delete filtresi alırlar.
    /// </summary>
    private void ApplyGlobalFilters(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            var clr = entityType.ClrType;
            var isTenant = typeof(ITenantEntity).IsAssignableFrom(clr) && clr != typeof(AppUser) && clr != typeof(AppRole);
            var isSoftDelete = typeof(ISoftDeletable).IsAssignableFrom(clr);

            if (!isTenant && !isSoftDelete) continue;

            var parameter = Expression.Parameter(clr, "e");
            Expression? body = null;

            if (isTenant)
            {
                var tenantProp = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                var currentTenant = Expression.Property(Expression.Constant(this), nameof(CurrentTenantId));
                body = Expression.Equal(tenantProp, currentTenant);
            }

            if (isSoftDelete)
            {
                var deletedProp = Expression.Property(parameter, nameof(ISoftDeletable.IsDeleted));
                var notDeleted = Expression.Not(deletedProp);
                body = body is null ? notDeleted : Expression.AndAlso(body, notDeleted);
            }

            var lambda = Expression.Lambda(body!, parameter);
            builder.Entity(clr).HasQueryFilter(lambda);
        }
    }
}
