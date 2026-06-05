/* ============================================================================
   KURUMSAL İŞ TAKİP PLATFORMU - Faz 1 Seed Verisi
   ----------------------------------------------------------------------------
   01_Schema.sql çalıştırıldıktan SONRA çalıştırın.
   Tek seferlik kurulum verisini yükler. Tekrar çalıştırmaya karşı korumalıdır.
   ============================================================================ */

USE [IsTakip];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

BEGIN TRY
BEGIN TRANSACTION;

/* ---------------------------------------------------------------------------
   1. Varsayılan kiracı (tenant)
   --------------------------------------------------------------------------- */
DECLARE @TenantId BIGINT;

IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Code = N'KOSIFLER')
BEGIN
    INSERT INTO dbo.Tenants (Name, Code, IsActive)
    VALUES (N'Kosifler Oto Servis ve Ticaret A.Ş.', N'KOSIFLER', 1);
    SET @TenantId = SCOPE_IDENTITY();
END
ELSE
    SELECT @TenantId = Id FROM dbo.Tenants WHERE Code = N'KOSIFLER';

/* ---------------------------------------------------------------------------
   2. İzin kataloğu (sistem geneli). Anahtarlar koddaki sabitlerle eşleşir.
   --------------------------------------------------------------------------- */
;WITH Perms([Key], Module, DisplayName, SortOrder) AS
(
    SELECT * FROM (VALUES
        (N'WorkItem.View',     N'Görev', N'Görev Görüntüle',     10),
        (N'WorkItem.Create',   N'Görev', N'Görev Oluştur',       20),
        (N'WorkItem.Update',   N'Görev', N'Görev Güncelle',      30),
        (N'WorkItem.Delete',   N'Görev', N'Görev Sil',           40),
        (N'WorkItem.Assign',   N'Görev', N'Görev Ata',           50),
        (N'Board.View',        N'İş Panosu', N'İş Panosu Görüntüle', 60),
        (N'Project.View',      N'Proje', N'Proje Görüntüle',     70),
        (N'Project.Manage',    N'Proje', N'Proje Yönet',         80),
        (N'Report.View',       N'Rapor', N'Rapor Görüntüle',     90),
        (N'User.View',         N'Kullanıcı', N'Kullanıcı Görüntüle', 100),
        (N'User.Manage',       N'Kullanıcı', N'Kullanıcı Yönet', 110),
        (N'Role.Manage',       N'Yetki', N'Rol Yönet',           120),
        (N'Workflow.Manage',   N'Yetki', N'İş Akışı Yönet',      130),
        (N'Automation.Manage', N'Otomasyon', N'Otomasyon Yönet', 140),
        (N'Organization.Manage', N'Organizasyon', N'Organizasyon Yönet', 150),
        (N'Kb.View',           N'Bilgi Bankası', N'Bilgi Bankası Görüntüle', 160),
        (N'Kb.Manage',         N'Bilgi Bankası', N'Bilgi Bankası Yönet', 170),
        (N'Settings.Manage',   N'Sistem', N'Sistem Ayarları Yönet', 180)
    ) AS v([Key], Module, DisplayName, SortOrder)
)
INSERT INTO dbo.Permissions ([Key], Module, DisplayName, SortOrder)
SELECT p.[Key], p.Module, p.DisplayName, p.SortOrder
FROM Perms p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permissions x WHERE x.[Key] = p.[Key]);

/* ---------------------------------------------------------------------------
   3. "Yönetici" sistem rolü + tüm izinler
   --------------------------------------------------------------------------- */
DECLARE @AdminRoleId BIGINT;

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE TenantId = @TenantId AND NormalizedName = N'YÖNETİCİ')
BEGIN
    INSERT INTO dbo.Roles (TenantId, Name, NormalizedName, Description, IsSystem)
    VALUES (@TenantId, N'Yönetici', N'YÖNETİCİ', N'Tüm yetkilere sahip sistem rolü', 1);
    SET @AdminRoleId = SCOPE_IDENTITY();
END
ELSE
    SELECT @AdminRoleId = Id FROM dbo.Roles WHERE TenantId = @TenantId AND NormalizedName = N'YÖNETİCİ';

INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @AdminRoleId, p.Id
FROM dbo.Permissions p
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = @AdminRoleId AND rp.PermissionId = p.Id);

/* ---------------------------------------------------------------------------
   4. Yönetici kullanıcı kaydı
      NOT: PasswordHash uygulama katmanındaki seeder tarafından
      (ASP.NET Core Identity PasswordHasher ile) atanır. Burada NULL bırakılır.
   --------------------------------------------------------------------------- */
DECLARE @AdminUserId BIGINT;

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE TenantId = @TenantId AND NormalizedUserName = N'ADMIN')
BEGIN
    INSERT INTO dbo.Users
        (TenantId, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed,
         FirstName, LastName, Status, SecurityStamp, ConcurrencyStamp)
    VALUES
        (@TenantId, N'admin', N'ADMIN', N'admin@kosifler.local', N'ADMIN@KOSIFLER.LOCAL', 1,
         N'Sistem', N'Yöneticisi', 1, NEWID(), NEWID());
    SET @AdminUserId = SCOPE_IDENTITY();
END
ELSE
    SELECT @AdminUserId = Id FROM dbo.Users WHERE TenantId = @TenantId AND NormalizedUserName = N'ADMIN';

IF NOT EXISTS (SELECT 1 FROM dbo.UserRoles WHERE UserId = @AdminUserId AND RoleId = @AdminRoleId)
    INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (@AdminUserId, @AdminRoleId);

/* ---------------------------------------------------------------------------
   5. Öncelikler
   --------------------------------------------------------------------------- */
;WITH Pr(Name, ColorHex, SortOrder) AS
(
    SELECT * FROM (VALUES
        (N'Kritik', N'#E53935', 10),
        (N'Yüksek', N'#FB8C00', 20),
        (N'Orta',   N'#FDD835', 30),
        (N'Düşük',  N'#43A047', 40)
    ) AS v(Name, ColorHex, SortOrder)
)
INSERT INTO dbo.Priorities (TenantId, Name, ColorHex, SortOrder)
SELECT @TenantId, p.Name, p.ColorHex, p.SortOrder
FROM Pr p
WHERE NOT EXISTS (SELECT 1 FROM dbo.Priorities x WHERE x.TenantId = @TenantId AND x.Name = p.Name);

/* ---------------------------------------------------------------------------
   6. Varsayılan iş akışı + dinamik durumlar
   --------------------------------------------------------------------------- */
DECLARE @WorkflowId BIGINT;

IF NOT EXISTS (SELECT 1 FROM dbo.Workflows WHERE TenantId = @TenantId AND Name = N'Standart İş Akışı')
BEGIN
    INSERT INTO dbo.Workflows (TenantId, Name, Description, IsActive)
    VALUES (@TenantId, N'Standart İş Akışı', N'Varsayılan görev/talep akışı', 1);
    SET @WorkflowId = SCOPE_IDENTITY();

    -- Durumlar (Category: 1 Yapılacak, 2 Devam, 3 Tamamlandı)
    INSERT INTO dbo.WorkflowStates (WorkflowId, Name, Category, ColorHex, SortOrder, IsInitial, IsFinal)
    VALUES
        (@WorkflowId, N'Yeni Kayıt',     1, N'#90A4AE', 10, 1, 0),
        (@WorkflowId, N'Atandı',         1, N'#42A5F5', 20, 0, 0),
        (@WorkflowId, N'Devam Ediyor',   2, N'#1E88E5', 30, 0, 0),
        (@WorkflowId, N'Beklemede',      2, N'#FFB300', 40, 0, 0),
        (@WorkflowId, N'Onay Bekliyor',  2, N'#8E24AA', 50, 0, 0),
        (@WorkflowId, N'Tamamlandı',     3, N'#43A047', 60, 0, 1),
        (@WorkflowId, N'İptal Edildi',   3, N'#757575', 70, 0, 1),
        (@WorkflowId, N'Reddedildi',     3, N'#E53935', 80, 0, 1);
END
ELSE
    SELECT @WorkflowId = Id FROM dbo.Workflows WHERE TenantId = @TenantId AND Name = N'Standart İş Akışı';

/* ---------------------------------------------------------------------------
   7. Görev türleri (dinamik) - varsayılan iş akışına bağlı
   --------------------------------------------------------------------------- */
;WITH Types(Name, KeyPrefix, ColorHex, SortOrder) AS
(
    SELECT * FROM (VALUES
        (N'Görev',               N'GRV', N'#1565C0', 10),
        (N'Talep',               N'TLP', N'#00897B', 20),
        (N'Hata Kaydı',          N'HTA', N'#C62828', 30),
        (N'İyileştirme Talebi',  N'IYI', N'#6A1B9A', 40),
        (N'Destek Talebi',       N'DST', N'#00838F', 50),
        (N'Satın Alma Talebi',   N'SAT', N'#EF6C00', 60),
        (N'İnsan Kaynakları Talebi', N'IK', N'#AD1457', 70),
        (N'Bakım Talebi',        N'BKM', N'#4E342E', 80),
        (N'Arıza Talebi',        N'ARZ', N'#D84315', 90),
        (N'Proje Görevi',        N'PRJ', N'#283593', 100)
    ) AS v(Name, KeyPrefix, ColorHex, SortOrder)
)
INSERT INTO dbo.WorkItemTypes (TenantId, Name, KeyPrefix, ColorHex, DefaultWorkflowId, IsActive)
SELECT @TenantId, t.Name, t.KeyPrefix, t.ColorHex, @WorkflowId, 1
FROM Types t
WHERE NOT EXISTS (SELECT 1 FROM dbo.WorkItemTypes x WHERE x.TenantId = @TenantId AND x.Name = t.Name);

COMMIT TRANSACTION;
PRINT N'Seed verisi yüklendi. Yönetici kullanıcısı: admin (şifre uygulama seeder''ında atanır).';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
