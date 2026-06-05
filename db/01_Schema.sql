/* ============================================================================
   KURUMSAL İŞ TAKİP & SÜREÇ YÖNETİMİ PLATFORMU
   Faz 1 - Veritabanı Şeması (MSSQL)
   ----------------------------------------------------------------------------
   Mimari kararlar (bu script bu kararları somutlaştırır):
     - Multi-tenant: Tek veritabanı + TenantId ayraç kolonu.
       Kiracıya özelleşen tablolar TenantId taşır (Roles, Workflows,
       WorkItemTypes, CustomFields, Priorities vb.). Yalnızca sistem-geneli
       kayıtlar (Tenants, Permissions kataloğu) tenant bağımsızdır.
     - Tüm PK'lar tutarlılık için BIGINT IDENTITY(1,1).
     - İş tablolarında ortak denetim kolonları: CreatedAtUtc, CreatedByUserId,
       UpdatedAtUtc, UpdatedByUserId, IsDeleted (soft delete).
     - Kimlik: ASP.NET Core Identity (long anahtar) bu tablolara map edilir.
     - Dinamik durum/iş akışı ve dinamik özel alanlar şemaya baştan dahildir.
     - Tablo/kolon isimleri İngilizce (EF Core convention dostu); uygulama
       arayüzü tamamen Türkçedir.
   ----------------------------------------------------------------------------
   Çalıştırma sırası: Bu dosya bağımlılık sırasına göre düzenlenmiştir.
   Önce veritabanını oluşturun, sonra bu dosyayı tek seferde çalıştırın.
   ============================================================================ */

/* ----------------------------------------------------------------------------
   0. VERİTABANI
   ---------------------------------------------------------------------------- */
IF DB_ID(N'IsTakip') IS NULL
BEGIN
    CREATE DATABASE [IsTakip];
END
GO

USE [IsTakip];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* ============================================================================
   1. SİSTEM & MULTI-TENANT
   ============================================================================ */

CREATE TABLE dbo.Tenants
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    Name               NVARCHAR(200)   NOT NULL,           -- Şirket / kiracı adı
    Code               NVARCHAR(50)    NOT NULL,           -- Benzersiz kısa kod (örn. KOSIFLER)
    IsActive           BIT             NOT NULL CONSTRAINT DF_Tenants_IsActive DEFAULT(1),
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_Tenants_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAtUtc       DATETIME2(3)    NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Tenants_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Tenants PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Tenants_Code UNIQUE (Code)
);
GO

-- Tenant bazlı genel ayarlar (anahtar/değer). Tema, dil, e-posta yapılandırması vb.
CREATE TABLE dbo.SystemSettings
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    [Key]              NVARCHAR(150)   NOT NULL,
    [Value]            NVARCHAR(MAX)   NULL,
    Description        NVARCHAR(400)   NULL,
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_SystemSettings_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAtUtc       DATETIME2(3)    NULL,
    CONSTRAINT PK_SystemSettings PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_SystemSettings_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT UQ_SystemSettings_Tenant_Key UNIQUE (TenantId, [Key])
);
GO

/* ============================================================================
   2. KİMLİK & YETKİLENDİRME
   ============================================================================ */

CREATE TABLE dbo.Users
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    UserName           NVARCHAR(256)   NOT NULL,           -- Identity uyumlu
    NormalizedUserName NVARCHAR(256)   NOT NULL,
    Email              NVARCHAR(256)   NOT NULL,
    NormalizedEmail    NVARCHAR(256)   NOT NULL,
    EmailConfirmed     BIT             NOT NULL CONSTRAINT DF_Users_EmailConfirmed DEFAULT(0),
    PasswordHash       NVARCHAR(MAX)   NULL,
    SecurityStamp      NVARCHAR(MAX)   NULL,
    ConcurrencyStamp   NVARCHAR(MAX)   NULL,
    PhoneNumber        NVARCHAR(30)    NULL,
    TwoFactorEnabled   BIT             NOT NULL CONSTRAINT DF_Users_TwoFactor DEFAULT(0),
    LockoutEndUtc      DATETIMEOFFSET  NULL,
    LockoutEnabled     BIT             NOT NULL CONSTRAINT DF_Users_LockoutEnabled DEFAULT(1),
    AccessFailedCount  INT             NOT NULL CONSTRAINT DF_Users_AccessFailed DEFAULT(0),
    -- İş alanları
    FirstName          NVARCHAR(100)   NOT NULL,
    LastName           NVARCHAR(100)   NOT NULL,
    AvatarUrl          NVARCHAR(500)   NULL,
    Title              NVARCHAR(150)   NULL,               -- Unvan
    DepartmentId       BIGINT          NULL,
    Status             TINYINT         NOT NULL CONSTRAINT DF_Users_Status DEFAULT(1), -- 1 Aktif,2 Pasif,3 İzinli,4 İşten Ayrıldı
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT(SYSUTCDATETIME()),
    CreatedByUserId    BIGINT          NULL,
    UpdatedAtUtc       DATETIME2(3)    NULL,
    UpdatedByUserId    BIGINT          NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Users_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);
GO
CREATE UNIQUE INDEX UX_Users_Tenant_NormalizedUserName ON dbo.Users(TenantId, NormalizedUserName) WHERE IsDeleted = 0;
CREATE INDEX IX_Users_Tenant_NormalizedEmail ON dbo.Users(TenantId, NormalizedEmail);
CREATE INDEX IX_Users_Department ON dbo.Users(DepartmentId) WHERE DepartmentId IS NOT NULL;
GO

CREATE TABLE dbo.Roles
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    Name               NVARCHAR(150)   NOT NULL,           -- örn. Satış Müdürü
    NormalizedName     NVARCHAR(150)   NOT NULL,
    Description        NVARCHAR(400)   NULL,
    IsSystem           BIT             NOT NULL CONSTRAINT DF_Roles_IsSystem DEFAULT(0), -- Silinemez sistem rolü (örn. Yönetici)
    ConcurrencyStamp   NVARCHAR(MAX)   NULL,
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAtUtc       DATETIME2(3)    NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Roles_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Roles_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);
GO
CREATE UNIQUE INDEX UX_Roles_Tenant_NormalizedName ON dbo.Roles(TenantId, NormalizedName) WHERE IsDeleted = 0;
GO

CREATE TABLE dbo.UserRoles
(
    UserId             BIGINT          NOT NULL,
    RoleId             BIGINT          NOT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY CLUSTERED (UserId, RoleId),
    CONSTRAINT FK_UserRoles_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_UserRoles_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id)
);
GO
CREATE INDEX IX_UserRoles_Role ON dbo.UserRoles(RoleId);
GO

-- İzin kataloğu: SİSTEM GENELİ (tenant bağımsız). Kod tarafında sabit anahtarlarla eşlenir.
-- Modül + işlem bazlı: örn. "WorkItem.Create", "Report.View", "Role.Manage".
CREATE TABLE dbo.Permissions
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    [Key]              NVARCHAR(150)   NOT NULL,           -- Benzersiz teknik anahtar
    Module             NVARCHAR(100)   NOT NULL,           -- Gruplama (örn. Görev, Rapor, Kullanıcı)
    DisplayName        NVARCHAR(200)   NOT NULL,           -- Türkçe görünen ad (örn. Görev Oluştur)
    SortOrder          INT             NOT NULL CONSTRAINT DF_Permissions_Sort DEFAULT(0),
    CONSTRAINT PK_Permissions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT UQ_Permissions_Key UNIQUE ([Key])
);
GO

CREATE TABLE dbo.RolePermissions
(
    RoleId             BIGINT          NOT NULL,
    PermissionId       BIGINT          NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY CLUSTERED (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id),
    CONSTRAINT FK_RolePermissions_Permission FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(Id)
);
GO
CREATE INDEX IX_RolePermissions_Permission ON dbo.RolePermissions(PermissionId);
GO

-- Refresh token (API/JWT akışı için)
CREATE TABLE dbo.RefreshTokens
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    UserId             BIGINT          NOT NULL,
    Token              NVARCHAR(200)   NOT NULL,
    ExpiresAtUtc       DATETIME2(3)    NOT NULL,
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT(SYSUTCDATETIME()),
    CreatedByIp        NVARCHAR(64)    NULL,
    RevokedAtUtc       DATETIME2(3)    NULL,
    RevokedByIp        NVARCHAR(64)    NULL,
    ReplacedByToken    NVARCHAR(200)   NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_RefreshTokens_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO
CREATE INDEX IX_RefreshTokens_Token ON dbo.RefreshTokens(Token);
GO

-- Oturum / giriş geçmişi (oturum yönetimi + güvenlik logu)
CREATE TABLE dbo.UserSessions
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    UserId             BIGINT          NOT NULL,
    LoginAtUtc         DATETIME2(3)    NOT NULL CONSTRAINT DF_UserSessions_LoginAt DEFAULT(SYSUTCDATETIME()),
    LogoutAtUtc        DATETIME2(3)    NULL,
    IpAddress          NVARCHAR(64)    NULL,
    UserAgent          NVARCHAR(400)   NULL,
    IsActive           BIT             NOT NULL CONSTRAINT DF_UserSessions_IsActive DEFAULT(1),
    CONSTRAINT PK_UserSessions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_UserSessions_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO
CREATE INDEX IX_UserSessions_User_Active ON dbo.UserSessions(UserId, IsActive);
GO

/* ============================================================================
   3. ORGANİZASYON YAPISI
   ============================================================================ */

CREATE TABLE dbo.Companies
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    Name               NVARCHAR(200)   NOT NULL,
    TaxNumber          NVARCHAR(50)    NULL,
    IsActive           BIT             NOT NULL CONSTRAINT DF_Companies_IsActive DEFAULT(1),
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_Companies_CreatedAt DEFAULT(SYSUTCDATETIME()),
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Companies_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Companies PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Companies_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);
GO

CREATE TABLE dbo.Locations
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    Name               NVARCHAR(200)   NOT NULL,
    City               NVARCHAR(100)   NULL,
    Address            NVARCHAR(500)   NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Locations_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Locations PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Locations_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);
GO

CREATE TABLE dbo.Branches
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    CompanyId          BIGINT          NOT NULL,
    LocationId         BIGINT          NULL,
    Name               NVARCHAR(200)   NOT NULL,
    IsActive           BIT             NOT NULL CONSTRAINT DF_Branches_IsActive DEFAULT(1),
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Branches_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Branches PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Branches_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Branches_Company FOREIGN KEY (CompanyId) REFERENCES dbo.Companies(Id),
    CONSTRAINT FK_Branches_Location FOREIGN KEY (LocationId) REFERENCES dbo.Locations(Id)
);
GO
CREATE INDEX IX_Branches_Company ON dbo.Branches(CompanyId);
GO

CREATE TABLE dbo.Departments
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    BranchId           BIGINT          NULL,
    Name               NVARCHAR(200)   NOT NULL,           -- örn. İnsan Kaynakları
    ManagerUserId      BIGINT          NULL,               -- Departman müdürü (otomasyon ataması için)
    IsActive           BIT             NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT(1),
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Departments_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Departments PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Departments_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Departments_Branch FOREIGN KEY (BranchId) REFERENCES dbo.Branches(Id),
    CONSTRAINT FK_Departments_Manager FOREIGN KEY (ManagerUserId) REFERENCES dbo.Users(Id)
);
GO
CREATE INDEX IX_Departments_Branch ON dbo.Departments(BranchId);
GO

-- Users.DepartmentId FK'ı artık eklenebilir (Departments oluştuktan sonra)
ALTER TABLE dbo.Users
    ADD CONSTRAINT FK_Users_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id);
GO

CREATE TABLE dbo.Units
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    DepartmentId       BIGINT          NOT NULL,
    Name               NVARCHAR(200)   NOT NULL,           -- Birim
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Units_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Units PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Units_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Units_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id)
);
GO
CREATE INDEX IX_Units_Department ON dbo.Units(DepartmentId);
GO

CREATE TABLE dbo.Teams
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    DepartmentId       BIGINT          NULL,
    Name               NVARCHAR(200)   NOT NULL,           -- Takım
    LeadUserId         BIGINT          NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Teams_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Teams PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Teams_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Teams_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id),
    CONSTRAINT FK_Teams_Lead FOREIGN KEY (LeadUserId) REFERENCES dbo.Users(Id)
);
GO

CREATE TABLE dbo.TeamMembers
(
    TeamId             BIGINT          NOT NULL,
    UserId             BIGINT          NOT NULL,
    CONSTRAINT PK_TeamMembers PRIMARY KEY CLUSTERED (TeamId, UserId),
    CONSTRAINT FK_TeamMembers_Team FOREIGN KEY (TeamId) REFERENCES dbo.Teams(Id),
    CONSTRAINT FK_TeamMembers_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO

/* ============================================================================
   4. İŞ AKIŞI & ÖNCELİK (DİNAMİK DURUMLAR)
   ============================================================================ */

CREATE TABLE dbo.Priorities
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    Name               NVARCHAR(100)   NOT NULL,           -- örn. Kritik, Yüksek
    ColorHex           NVARCHAR(9)     NULL,               -- örn. #E53935
    SortOrder          INT             NOT NULL CONSTRAINT DF_Priorities_Sort DEFAULT(0),
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Priorities_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Priorities PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Priorities_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);
GO

-- İş akışı tanımı (admin tarafından oluşturulur)
CREATE TABLE dbo.Workflows
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    Name               NVARCHAR(150)   NOT NULL,           -- örn. Destek Talebi Akışı
    Description        NVARCHAR(400)   NULL,
    IsActive           BIT             NOT NULL CONSTRAINT DF_Workflows_IsActive DEFAULT(1),
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Workflows_IsDeleted DEFAULT(0),
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_Workflows_CreatedAt DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT PK_Workflows PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Workflows_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);
GO

-- Dinamik durumlar (admin sınırsız durum ekleyebilir)
CREATE TABLE dbo.WorkflowStates
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    WorkflowId         BIGINT          NOT NULL,
    Name               NVARCHAR(100)   NOT NULL,           -- örn. İnceleniyor, Devam Ediyor
    Category           TINYINT         NOT NULL CONSTRAINT DF_WorkflowStates_Cat DEFAULT(1), -- 1 Yapılacak,2 Devam,3 Tamamlandı (raporlama/kanban gruplaması)
    ColorHex           NVARCHAR(9)     NULL,
    SortOrder          INT             NOT NULL CONSTRAINT DF_WorkflowStates_Sort DEFAULT(0),
    IsInitial          BIT             NOT NULL CONSTRAINT DF_WorkflowStates_Init DEFAULT(0), -- Başlangıç durumu
    IsFinal            BIT             NOT NULL CONSTRAINT DF_WorkflowStates_Final DEFAULT(0),
    CONSTRAINT PK_WorkflowStates PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_WorkflowStates_Workflow FOREIGN KEY (WorkflowId) REFERENCES dbo.Workflows(Id)
);
GO
CREATE INDEX IX_WorkflowStates_Workflow ON dbo.WorkflowStates(WorkflowId);
GO

-- İzinli durum geçişleri (state machine kenarları)
CREATE TABLE dbo.WorkflowTransitions
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    WorkflowId         BIGINT          NOT NULL,
    FromStateId        BIGINT          NOT NULL,
    ToStateId          BIGINT          NOT NULL,
    Name               NVARCHAR(100)   NULL,               -- örn. Onayla, Reddet
    RequiredPermissionKey NVARCHAR(150) NULL,              -- Geçiş için gereken izin (opsiyonel guard)
    CONSTRAINT PK_WorkflowTransitions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_WorkflowTransitions_Workflow FOREIGN KEY (WorkflowId) REFERENCES dbo.Workflows(Id),
    CONSTRAINT FK_WorkflowTransitions_From FOREIGN KEY (FromStateId) REFERENCES dbo.WorkflowStates(Id),
    CONSTRAINT FK_WorkflowTransitions_To FOREIGN KEY (ToStateId) REFERENCES dbo.WorkflowStates(Id)
);
GO
CREATE INDEX IX_WorkflowTransitions_From ON dbo.WorkflowTransitions(FromStateId);
GO

/* ============================================================================
   5. GÖREV / TALEP YÖNETİMİ
   ============================================================================ */

-- Görev türleri (dinamik): Görev, Talep, Hata Kaydı, Satın Alma Talebi, İK Talebi...
-- Her tür bir iş akışına bağlanır. Departmana göre kısıtlanabilir.
CREATE TABLE dbo.WorkItemTypes
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    Name               NVARCHAR(100)   NOT NULL,           -- örn. Satın Alma Talebi
    IconName           NVARCHAR(100)   NULL,
    ColorHex           NVARCHAR(9)     NULL,
    DefaultWorkflowId  BIGINT          NULL,               -- Bu türde açılan kayıtların varsayılan akışı
    KeyPrefix          NVARCHAR(10)    NULL,               -- Kayıt anahtarı öneki (örn. SAT -> SAT-128)
    IsActive           BIT             NOT NULL CONSTRAINT DF_WorkItemTypes_IsActive DEFAULT(1),
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_WorkItemTypes_IsDeleted DEFAULT(0),
    CONSTRAINT PK_WorkItemTypes PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_WorkItemTypes_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_WorkItemTypes_Workflow FOREIGN KEY (DefaultWorkflowId) REFERENCES dbo.Workflows(Id)
);
GO

CREATE TABLE dbo.Projects
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    Name               NVARCHAR(200)   NOT NULL,
    [Key]              NVARCHAR(20)    NOT NULL,           -- Proje kısa kodu (kayıt anahtarında kullanılır)
    Description        NVARCHAR(MAX)   NULL,
    DepartmentId       BIGINT          NULL,
    LeadUserId         BIGINT          NULL,               -- Proje yöneticisi
    Budget             DECIMAL(18,2)   NULL,
    Status             TINYINT         NOT NULL CONSTRAINT DF_Projects_Status DEFAULT(1), -- 1 Aktif,2 Kapalı,3 Arşiv
    StartDate          DATE            NULL,
    EndDate            DATE            NULL,
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_Projects_CreatedAt DEFAULT(SYSUTCDATETIME()),
    CreatedByUserId    BIGINT          NULL,
    UpdatedAtUtc       DATETIME2(3)    NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Projects_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Projects PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Projects_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Projects_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id),
    CONSTRAINT FK_Projects_Lead FOREIGN KEY (LeadUserId) REFERENCES dbo.Users(Id)
);
GO
CREATE UNIQUE INDEX UX_Projects_Tenant_Key ON dbo.Projects(TenantId, [Key]) WHERE IsDeleted = 0;
GO

CREATE TABLE dbo.ProjectMembers
(
    ProjectId          BIGINT          NOT NULL,
    UserId             BIGINT          NOT NULL,
    IsManager          BIT             NOT NULL CONSTRAINT DF_ProjectMembers_IsManager DEFAULT(0),
    CONSTRAINT PK_ProjectMembers PRIMARY KEY CLUSTERED (ProjectId, UserId),
    CONSTRAINT FK_ProjectMembers_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id),
    CONSTRAINT FK_ProjectMembers_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO

-- Çalışma Dönemi (Sprint yerine)
CREATE TABLE dbo.WorkPeriods
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    ProjectId          BIGINT          NULL,
    Name               NVARCHAR(150)   NOT NULL,           -- örn. 2026 Mart Dönemi
    StartDate          DATE            NULL,
    EndDate            DATE            NULL,
    Status             TINYINT         NOT NULL CONSTRAINT DF_WorkPeriods_Status DEFAULT(1), -- 1 Planlandı,2 Başladı,3 Bitti
    Goal               NVARCHAR(MAX)   NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_WorkPeriods_IsDeleted DEFAULT(0),
    CONSTRAINT PK_WorkPeriods PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_WorkPeriods_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_WorkPeriods_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id)
);
GO

-- Ana iş kaydı (görev/talep/hata vb. tek tablo, tür ile ayrışır)
CREATE TABLE dbo.WorkItems
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    [Key]              NVARCHAR(40)    NOT NULL,           -- İnsan-okur kayıt no (örn. SAT-128)
    WorkItemTypeId     BIGINT          NOT NULL,
    ProjectId          BIGINT          NULL,
    WorkPeriodId       BIGINT          NULL,
    Title              NVARCHAR(300)   NOT NULL,
    Description        NVARCHAR(MAX)   NULL,
    PriorityId         BIGINT          NULL,
    WorkflowId         BIGINT          NOT NULL,
    CurrentStateId     BIGINT          NOT NULL,           -- Mevcut durum (dinamik)
    AssigneeUserId     BIGINT          NULL,               -- Sorumlu
    ReporterUserId     BIGINT          NULL,               -- Talebi açan
    DepartmentId       BIGINT          NULL,
    ParentWorkItemId   BIGINT          NULL,               -- Alt görev hiyerarşisi
    StartDate          DATE            NULL,
    DueDate            DATE            NULL,                -- Son tarih
    CompletedAtUtc     DATETIME2(3)    NULL,
    EstimatedMinutes   INT             NULL,
    RowVersion         ROWVERSION      NOT NULL,           -- Optimistic concurrency (kanban sürükle-bırak çakışmaları)
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_WorkItems_CreatedAt DEFAULT(SYSUTCDATETIME()),
    CreatedByUserId    BIGINT          NULL,
    UpdatedAtUtc       DATETIME2(3)    NULL,
    UpdatedByUserId    BIGINT          NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_WorkItems_IsDeleted DEFAULT(0),
    CONSTRAINT PK_WorkItems PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_WorkItems_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_WorkItems_Type FOREIGN KEY (WorkItemTypeId) REFERENCES dbo.WorkItemTypes(Id),
    CONSTRAINT FK_WorkItems_Project FOREIGN KEY (ProjectId) REFERENCES dbo.Projects(Id),
    CONSTRAINT FK_WorkItems_Period FOREIGN KEY (WorkPeriodId) REFERENCES dbo.WorkPeriods(Id),
    CONSTRAINT FK_WorkItems_Priority FOREIGN KEY (PriorityId) REFERENCES dbo.Priorities(Id),
    CONSTRAINT FK_WorkItems_Workflow FOREIGN KEY (WorkflowId) REFERENCES dbo.Workflows(Id),
    CONSTRAINT FK_WorkItems_State FOREIGN KEY (CurrentStateId) REFERENCES dbo.WorkflowStates(Id),
    CONSTRAINT FK_WorkItems_Assignee FOREIGN KEY (AssigneeUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_WorkItems_Reporter FOREIGN KEY (ReporterUserId) REFERENCES dbo.Users(Id),
    CONSTRAINT FK_WorkItems_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(Id),
    CONSTRAINT FK_WorkItems_Parent FOREIGN KEY (ParentWorkItemId) REFERENCES dbo.WorkItems(Id)
);
GO
CREATE UNIQUE INDEX UX_WorkItems_Tenant_Key ON dbo.WorkItems(TenantId, [Key]) WHERE IsDeleted = 0;
CREATE INDEX IX_WorkItems_Tenant_State ON dbo.WorkItems(TenantId, CurrentStateId) WHERE IsDeleted = 0;
CREATE INDEX IX_WorkItems_Tenant_Assignee ON dbo.WorkItems(TenantId, AssigneeUserId) WHERE IsDeleted = 0;
CREATE INDEX IX_WorkItems_Tenant_Department ON dbo.WorkItems(TenantId, DepartmentId) WHERE IsDeleted = 0;
CREATE INDEX IX_WorkItems_Project ON dbo.WorkItems(ProjectId) WHERE ProjectId IS NOT NULL;
CREATE INDEX IX_WorkItems_Parent ON dbo.WorkItems(ParentWorkItemId) WHERE ParentWorkItemId IS NOT NULL;
GO

-- Etiketler
CREATE TABLE dbo.Labels
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    Name               NVARCHAR(100)   NOT NULL,
    ColorHex           NVARCHAR(9)     NULL,
    CONSTRAINT PK_Labels PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Labels_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);
GO

CREATE TABLE dbo.WorkItemLabels
(
    WorkItemId         BIGINT          NOT NULL,
    LabelId            BIGINT          NOT NULL,
    CONSTRAINT PK_WorkItemLabels PRIMARY KEY CLUSTERED (WorkItemId, LabelId),
    CONSTRAINT FK_WorkItemLabels_WorkItem FOREIGN KEY (WorkItemId) REFERENCES dbo.WorkItems(Id),
    CONSTRAINT FK_WorkItemLabels_Label FOREIGN KEY (LabelId) REFERENCES dbo.Labels(Id)
);
GO

-- Takipçiler (watchers)
CREATE TABLE dbo.WorkItemWatchers
(
    WorkItemId         BIGINT          NOT NULL,
    UserId             BIGINT          NOT NULL,
    CONSTRAINT PK_WorkItemWatchers PRIMARY KEY CLUSTERED (WorkItemId, UserId),
    CONSTRAINT FK_WorkItemWatchers_WorkItem FOREIGN KEY (WorkItemId) REFERENCES dbo.WorkItems(Id),
    CONSTRAINT FK_WorkItemWatchers_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO

-- Durum geçiş geçmişi (her hareket loglanır; raporlama: ortalama çözüm süresi vb.)
CREATE TABLE dbo.WorkItemStateHistory
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    WorkItemId         BIGINT          NOT NULL,
    FromStateId        BIGINT          NULL,
    ToStateId          BIGINT          NOT NULL,
    ChangedByUserId    BIGINT          NULL,
    ChangedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_WorkItemStateHistory_At DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT PK_WorkItemStateHistory PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_WISH_WorkItem FOREIGN KEY (WorkItemId) REFERENCES dbo.WorkItems(Id),
    CONSTRAINT FK_WISH_From FOREIGN KEY (FromStateId) REFERENCES dbo.WorkflowStates(Id),
    CONSTRAINT FK_WISH_To FOREIGN KEY (ToStateId) REFERENCES dbo.WorkflowStates(Id)
);
GO
CREATE INDEX IX_WorkItemStateHistory_WorkItem ON dbo.WorkItemStateHistory(WorkItemId);
GO

/* ============================================================================
   6. YORUM & DOSYA YÖNETİMİ
   ============================================================================ */

CREATE TABLE dbo.Comments
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    WorkItemId         BIGINT          NOT NULL,
    AuthorUserId       BIGINT          NOT NULL,
    Body               NVARCHAR(MAX)   NOT NULL,           -- Zengin metin (HTML)
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_Comments_CreatedAt DEFAULT(SYSUTCDATETIME()),
    UpdatedAtUtc       DATETIME2(3)    NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Comments_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Comments PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Comments_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Comments_WorkItem FOREIGN KEY (WorkItemId) REFERENCES dbo.WorkItems(Id),
    CONSTRAINT FK_Comments_Author FOREIGN KEY (AuthorUserId) REFERENCES dbo.Users(Id)
);
GO
CREATE INDEX IX_Comments_WorkItem ON dbo.Comments(WorkItemId) WHERE IsDeleted = 0;
GO

CREATE TABLE dbo.CommentMentions
(
    CommentId          BIGINT          NOT NULL,
    MentionedUserId    BIGINT          NOT NULL,
    CONSTRAINT PK_CommentMentions PRIMARY KEY CLUSTERED (CommentId, MentionedUserId),
    CONSTRAINT FK_CommentMentions_Comment FOREIGN KEY (CommentId) REFERENCES dbo.Comments(Id),
    CONSTRAINT FK_CommentMentions_User FOREIGN KEY (MentionedUserId) REFERENCES dbo.Users(Id)
);
GO

-- Dosyalar + versiyonlama
CREATE TABLE dbo.Attachments
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    WorkItemId         BIGINT          NULL,
    CommentId          BIGINT          NULL,
    FileName           NVARCHAR(300)   NOT NULL,
    ContentType        NVARCHAR(150)   NULL,
    SizeBytes          BIGINT          NOT NULL CONSTRAINT DF_Attachments_Size DEFAULT(0),
    StoragePath        NVARCHAR(700)   NOT NULL,           -- Soyut depolama yolu (disk/blob)
    [Version]          INT             NOT NULL CONSTRAINT DF_Attachments_Version DEFAULT(1),
    ParentAttachmentId BIGINT          NULL,               -- Önceki versiyon
    UploadedByUserId   BIGINT          NULL,
    UploadedAtUtc      DATETIME2(3)    NOT NULL CONSTRAINT DF_Attachments_At DEFAULT(SYSUTCDATETIME()),
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_Attachments_IsDeleted DEFAULT(0),
    CONSTRAINT PK_Attachments PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Attachments_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Attachments_WorkItem FOREIGN KEY (WorkItemId) REFERENCES dbo.WorkItems(Id),
    CONSTRAINT FK_Attachments_Comment FOREIGN KEY (CommentId) REFERENCES dbo.Comments(Id),
    CONSTRAINT FK_Attachments_Parent FOREIGN KEY (ParentAttachmentId) REFERENCES dbo.Attachments(Id)
);
GO
CREATE INDEX IX_Attachments_WorkItem ON dbo.Attachments(WorkItemId) WHERE WorkItemId IS NOT NULL;
GO

/* ============================================================================
   7. DİNAMİK ÖZEL ALANLAR (departmanlar arası uyarlanabilirlik)
   ============================================================================ */

-- Alan tanımı: hangi görev türünde hangi ek alan görünsün
CREATE TABLE dbo.CustomFieldDefinitions
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    WorkItemTypeId     BIGINT          NULL,               -- NULL ise tüm türler için
    Name               NVARCHAR(150)   NOT NULL,           -- örn. Tedarikçi, Bütçe Kalemi
    FieldType          TINYINT         NOT NULL,           -- 1 Metin,2 Sayı,3 Tarih,4 Seçim,5 ÇokluSeçim,6 EvetHayır,7 Kullanıcı
    IsRequired         BIT             NOT NULL CONSTRAINT DF_CFD_Required DEFAULT(0),
    SortOrder          INT             NOT NULL CONSTRAINT DF_CFD_Sort DEFAULT(0),
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_CFD_IsDeleted DEFAULT(0),
    CONSTRAINT PK_CustomFieldDefinitions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CFD_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_CFD_Type FOREIGN KEY (WorkItemTypeId) REFERENCES dbo.WorkItemTypes(Id)
);
GO

CREATE TABLE dbo.CustomFieldOptions
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    CustomFieldDefinitionId BIGINT     NOT NULL,
    [Value]            NVARCHAR(200)   NOT NULL,
    SortOrder          INT             NOT NULL CONSTRAINT DF_CFO_Sort DEFAULT(0),
    CONSTRAINT PK_CustomFieldOptions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CFO_Definition FOREIGN KEY (CustomFieldDefinitionId) REFERENCES dbo.CustomFieldDefinitions(Id)
);
GO

CREATE TABLE dbo.WorkItemCustomFieldValues
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    WorkItemId         BIGINT          NOT NULL,
    CustomFieldDefinitionId BIGINT     NOT NULL,
    ValueText          NVARCHAR(MAX)   NULL,               -- Tüm tipler metne serileştirilir; tip tanımdan okunur
    CONSTRAINT PK_WorkItemCustomFieldValues PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_WICFV_WorkItem FOREIGN KEY (WorkItemId) REFERENCES dbo.WorkItems(Id),
    CONSTRAINT FK_WICFV_Definition FOREIGN KEY (CustomFieldDefinitionId) REFERENCES dbo.CustomFieldDefinitions(Id),
    CONSTRAINT UQ_WICFV UNIQUE (WorkItemId, CustomFieldDefinitionId)
);
GO

/* ============================================================================
   8. ZAMAN TAKİBİ
   ============================================================================ */

CREATE TABLE dbo.TimeLogs
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    WorkItemId         BIGINT          NOT NULL,
    UserId             BIGINT          NOT NULL,
    StartedAtUtc       DATETIME2(3)    NULL,
    EndedAtUtc         DATETIME2(3)    NULL,
    DurationMinutes    INT             NOT NULL CONSTRAINT DF_TimeLogs_Duration DEFAULT(0),
    IsOvertime         BIT             NOT NULL CONSTRAINT DF_TimeLogs_Overtime DEFAULT(0), -- Fazla mesai
    [Description]      NVARCHAR(500)   NULL,
    LogDate            DATE            NOT NULL,
    CONSTRAINT PK_TimeLogs PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_TimeLogs_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_TimeLogs_WorkItem FOREIGN KEY (WorkItemId) REFERENCES dbo.WorkItems(Id),
    CONSTRAINT FK_TimeLogs_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
);
GO
CREATE INDEX IX_TimeLogs_User_Date ON dbo.TimeLogs(UserId, LogDate);
CREATE INDEX IX_TimeLogs_WorkItem ON dbo.TimeLogs(WorkItemId);
GO

/* ============================================================================
   9. ONAY MEKANİZMASI (Satın Alma / İK talepleri için çok adımlı onay)
   ============================================================================ */

CREATE TABLE dbo.ApprovalRequests
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    WorkItemId         BIGINT          NOT NULL,
    Status             TINYINT         NOT NULL CONSTRAINT DF_ApprovalRequests_Status DEFAULT(1), -- 1 Beklemede,2 Onaylandı,3 Reddedildi,4 İptal
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_ApprovalRequests_At DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT PK_ApprovalRequests PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ApprovalRequests_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_ApprovalRequests_WorkItem FOREIGN KEY (WorkItemId) REFERENCES dbo.WorkItems(Id)
);
GO

CREATE TABLE dbo.ApprovalSteps
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    ApprovalRequestId  BIGINT          NOT NULL,
    StepOrder          INT             NOT NULL,
    ApproverUserId     BIGINT          NOT NULL,
    Status             TINYINT         NOT NULL CONSTRAINT DF_ApprovalSteps_Status DEFAULT(1), -- 1 Beklemede,2 Onayladı,3 Reddetti
    Comment            NVARCHAR(500)   NULL,
    DecidedAtUtc       DATETIME2(3)    NULL,
    CONSTRAINT PK_ApprovalSteps PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ApprovalSteps_Request FOREIGN KEY (ApprovalRequestId) REFERENCES dbo.ApprovalRequests(Id),
    CONSTRAINT FK_ApprovalSteps_Approver FOREIGN KEY (ApproverUserId) REFERENCES dbo.Users(Id)
);
GO
CREATE INDEX IX_ApprovalSteps_Request ON dbo.ApprovalSteps(ApprovalRequestId);
GO

/* ============================================================================
   10. OTOMASYON MOTORU (Eğer ... ise ... yap)
   ============================================================================ */

CREATE TABLE dbo.AutomationRules
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    Name               NVARCHAR(200)   NOT NULL,
    TriggerEvent       TINYINT         NOT NULL,           -- 1 Oluşturuldu,2 GüncellendiDurum,3 Atama,4 Yorum,5 SonTarihYaklaştı
    IsActive           BIT             NOT NULL CONSTRAINT DF_AutomationRules_IsActive DEFAULT(1),
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_AutomationRules_IsDeleted DEFAULT(0),
    CONSTRAINT PK_AutomationRules PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_AutomationRules_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id)
);
GO

-- Koşullar (örn. Öncelik = Kritik). Alan/operatör/değer üçlüsü.
CREATE TABLE dbo.AutomationConditions
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    AutomationRuleId   BIGINT          NOT NULL,
    FieldKey           NVARCHAR(100)   NOT NULL,           -- örn. PriorityId, DepartmentId
    [Operator]         NVARCHAR(20)    NOT NULL,           -- eq, ne, gt, lt, contains
    [Value]            NVARCHAR(400)   NULL,
    CONSTRAINT PK_AutomationConditions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_AutomationConditions_Rule FOREIGN KEY (AutomationRuleId) REFERENCES dbo.AutomationRules(Id)
);
GO

-- Aksiyonlar (örn. Departman Müdürüne Ata, E-posta Gönder, Bildirim Gönder)
CREATE TABLE dbo.AutomationActions
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    AutomationRuleId   BIGINT          NOT NULL,
    ActionType         TINYINT         NOT NULL,           -- 1 Ata,2 EpostaGönder,3 BildirimGönder,4 DurumDeğiştir,5 EtiketEkle
    ParametersJson     NVARCHAR(MAX)   NULL,               -- Aksiyona özel parametreler
    CONSTRAINT PK_AutomationActions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_AutomationActions_Rule FOREIGN KEY (AutomationRuleId) REFERENCES dbo.AutomationRules(Id)
);
GO

/* ============================================================================
   11. BİLDİRİM MERKEZİ
   ============================================================================ */

CREATE TABLE dbo.Notifications
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    RecipientUserId    BIGINT          NOT NULL,
    Title              NVARCHAR(200)   NOT NULL,
    Body               NVARCHAR(1000)  NULL,
    LinkUrl            NVARCHAR(500)   NULL,               -- İlgili kayda yönlendirme
    [Type]             TINYINT         NOT NULL CONSTRAINT DF_Notifications_Type DEFAULT(1), -- 1 Atama,2 Yorum,3 DurumDeğişti,4 Sistem
    IsRead             BIT             NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT(0),
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_Notifications_At DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Notifications_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_Notifications_Recipient FOREIGN KEY (RecipientUserId) REFERENCES dbo.Users(Id)
);
GO
CREATE INDEX IX_Notifications_Recipient_Unread ON dbo.Notifications(RecipientUserId, IsRead);
GO

CREATE TABLE dbo.NotificationPreferences
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    UserId             BIGINT          NOT NULL,
    NotificationType   TINYINT         NOT NULL,
    InApp              BIT             NOT NULL CONSTRAINT DF_NotifPref_InApp DEFAULT(1),
    Email              BIT             NOT NULL CONSTRAINT DF_NotifPref_Email DEFAULT(1),
    CONSTRAINT PK_NotificationPreferences PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_NotificationPreferences_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
    CONSTRAINT UQ_NotificationPreferences UNIQUE (UserId, NotificationType)
);
GO

/* ============================================================================
   12. BİLGİ BANKASI
   ============================================================================ */

CREATE TABLE dbo.KbCategories
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    Name               NVARCHAR(150)   NOT NULL,
    ParentCategoryId   BIGINT          NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_KbCategories_IsDeleted DEFAULT(0),
    CONSTRAINT PK_KbCategories PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_KbCategories_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_KbCategories_Parent FOREIGN KEY (ParentCategoryId) REFERENCES dbo.KbCategories(Id)
);
GO

CREATE TABLE dbo.KbArticles
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NOT NULL,
    CategoryId         BIGINT          NULL,
    Title              NVARCHAR(300)   NOT NULL,
    Body               NVARCHAR(MAX)   NULL,
    CurrentVersion     INT             NOT NULL CONSTRAINT DF_KbArticles_Version DEFAULT(1),
    CreatedByUserId    BIGINT          NULL,
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_KbArticles_At DEFAULT(SYSUTCDATETIME()),
    UpdatedAtUtc       DATETIME2(3)    NULL,
    IsDeleted          BIT             NOT NULL CONSTRAINT DF_KbArticles_IsDeleted DEFAULT(0),
    CONSTRAINT PK_KbArticles PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_KbArticles_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(Id),
    CONSTRAINT FK_KbArticles_Category FOREIGN KEY (CategoryId) REFERENCES dbo.KbCategories(Id)
);
GO

CREATE TABLE dbo.KbArticleVersions
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    ArticleId          BIGINT          NOT NULL,
    [Version]          INT             NOT NULL,
    Body               NVARCHAR(MAX)   NULL,
    EditedByUserId     BIGINT          NULL,
    EditedAtUtc        DATETIME2(3)    NOT NULL CONSTRAINT DF_KbArticleVersions_At DEFAULT(SYSUTCDATETIME()),
    CONSTRAINT PK_KbArticleVersions PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_KbArticleVersions_Article FOREIGN KEY (ArticleId) REFERENCES dbo.KbArticles(Id)
);
GO

/* ============================================================================
   13. DENETİM LOGU (kim, ne, ne zaman, eski değer, yeni değer)
   ============================================================================ */

CREATE TABLE dbo.AuditLogs
(
    Id                 BIGINT          IDENTITY(1,1) NOT NULL,
    TenantId           BIGINT          NULL,
    UserId             BIGINT          NULL,               -- Kim
    [Action]           NVARCHAR(50)    NOT NULL,           -- Insert / Update / Delete / Login vb. (Ne)
    EntityName         NVARCHAR(150)   NOT NULL,           -- Hangi tablo/entity
    EntityId          NVARCHAR(80)     NULL,
    OldValues          NVARCHAR(MAX)   NULL,               -- Eski değer (JSON)
    NewValues          NVARCHAR(MAX)   NULL,               -- Yeni değer (JSON)
    IpAddress          NVARCHAR(64)    NULL,
    CreatedAtUtc       DATETIME2(3)    NOT NULL CONSTRAINT DF_AuditLogs_At DEFAULT(SYSUTCDATETIME()), -- Ne zaman
    CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id)
);
GO
CREATE INDEX IX_AuditLogs_Tenant_Entity ON dbo.AuditLogs(TenantId, EntityName, EntityId);
CREATE INDEX IX_AuditLogs_CreatedAt ON dbo.AuditLogs(CreatedAtUtc);
GO

PRINT N'Şema oluşturuldu. Sıradaki adım: 02_Seed.sql';
GO
