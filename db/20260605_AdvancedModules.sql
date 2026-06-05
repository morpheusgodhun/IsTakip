/*
  Jira-benzeri iş takip uygulaması için gelişmiş modüller şema güncellemesi.
  SQL Server üzerinde mevcut veritabanına idempotent şekilde uygulanabilir.
*/

SET NOCOUNT ON;
GO

/* ---------------------------
   Mevcut tabloları genişlet
----------------------------*/
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Attachments', N'Version') IS NULL
    ALTER TABLE dbo.Attachments ADD [Version] INT NOT NULL CONSTRAINT DF_Attachments_Version DEFAULT(1);
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Attachments', N'ParentAttachmentId') IS NULL
    ALTER TABLE dbo.Attachments ADD ParentAttachmentId BIGINT NULL;
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Attachments', N'DocumentCategory') IS NULL
    ALTER TABLE dbo.Attachments ADD DocumentCategory NVARCHAR(80) NOT NULL CONSTRAINT DF_Attachments_DocumentCategory DEFAULT(N'Genel');
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Attachments', N'Description') IS NULL
    ALTER TABLE dbo.Attachments ADD [Description] NVARCHAR(1000) NULL;
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Attachments', N'Visibility') IS NULL
    ALTER TABLE dbo.Attachments ADD [Visibility] TINYINT NOT NULL CONSTRAINT DF_Attachments_Visibility DEFAULT(1);
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Attachments', N'AllowedRoleId') IS NULL
    ALTER TABLE dbo.Attachments ADD AllowedRoleId BIGINT NULL;
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Attachments', N'UploadedByUserId') IS NULL
    ALTER TABLE dbo.Attachments ADD UploadedByUserId BIGINT NULL;
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Attachments', N'UploadedAtUtc') IS NULL
    ALTER TABLE dbo.Attachments ADD UploadedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_Attachments_UploadedAtUtc DEFAULT(SYSUTCDATETIME());
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Attachments', N'IsDeleted') IS NULL
    ALTER TABLE dbo.Attachments ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Attachments_IsDeleted DEFAULT(0);
GO

IF OBJECT_ID(N'dbo.ApprovalRequests', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ApprovalRequests', N'TemplateId') IS NULL
    ALTER TABLE dbo.ApprovalRequests ADD TemplateId BIGINT NULL;
GO
IF OBJECT_ID(N'dbo.ApprovalRequests', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ApprovalRequests', N'Subject') IS NULL
    ALTER TABLE dbo.ApprovalRequests ADD [Subject] NVARCHAR(300) NULL;
GO
IF OBJECT_ID(N'dbo.ApprovalRequests', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ApprovalRequests', N'RequestedByUserId') IS NULL
    ALTER TABLE dbo.ApprovalRequests ADD RequestedByUserId BIGINT NULL;
GO
IF OBJECT_ID(N'dbo.ApprovalRequests', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ApprovalRequests', N'CompletedAtUtc') IS NULL
    ALTER TABLE dbo.ApprovalRequests ADD CompletedAtUtc DATETIME2 NULL;
GO

IF OBJECT_ID(N'dbo.ApprovalSteps', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.ApprovalSteps', N'Name') IS NULL
    ALTER TABLE dbo.ApprovalSteps ADD [Name] NVARCHAR(200) NULL;
GO

/* ---------------------------
   Onay motoru
----------------------------*/
IF OBJECT_ID(N'dbo.ApprovalWorkflowTemplates', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApprovalWorkflowTemplates
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApprovalWorkflowTemplates PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        WorkItemTypeId BIGINT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_ApprovalWorkflowTemplates_IsActive DEFAULT(1),
        IsDeleted BIT NOT NULL CONSTRAINT DF_ApprovalWorkflowTemplates_IsDeleted DEFAULT(0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_ApprovalWorkflowTemplates_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        CreatedByUserId BIGINT NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedByUserId BIGINT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.ApprovalWorkflowTemplateSteps', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ApprovalWorkflowTemplateSteps
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ApprovalWorkflowTemplateSteps PRIMARY KEY,
        ApprovalWorkflowTemplateId BIGINT NOT NULL,
        StepOrder INT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        ApproverType TINYINT NOT NULL CONSTRAINT DF_ApprovalWorkflowTemplateSteps_ApproverType DEFAULT(1),
        ApproverUserId BIGINT NULL,
        ApproverRoleId BIGINT NULL
    );
END
GO

/* ---------------------------
   SLA
----------------------------*/
IF OBJECT_ID(N'dbo.SlaPolicies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SlaPolicies
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SlaPolicies PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        WorkItemTypeId BIGINT NULL,
        PriorityId BIGINT NULL,
        FirstResponseMinutes INT NULL,
        ResolutionMinutes INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SlaPolicies_IsActive DEFAULT(1),
        IsDeleted BIT NOT NULL CONSTRAINT DF_SlaPolicies_IsDeleted DEFAULT(0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SlaPolicies_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        CreatedByUserId BIGINT NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedByUserId BIGINT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.WorkItemSlas', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.WorkItemSlas
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WorkItemSlas PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        WorkItemId BIGINT NOT NULL,
        SlaPolicyId BIGINT NOT NULL,
        FirstResponseDueAtUtc DATETIME2 NULL,
        ResolutionDueAtUtc DATETIME2 NOT NULL,
        FirstResponseAtUtc DATETIME2 NULL,
        CompletedAtUtc DATETIME2 NULL,
        [Status] TINYINT NOT NULL CONSTRAINT DF_WorkItemSlas_Status DEFAULT(1)
    );
END
GO

/* ---------------------------
   Dashboard tasarımcısı
----------------------------*/
IF OBJECT_ID(N'dbo.UserDashboards', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserDashboards
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserDashboards PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        UserId BIGINT NOT NULL,
        [Name] NVARCHAR(200) NOT NULL CONSTRAINT DF_UserDashboards_Name DEFAULT(N'Ana Dashboard'),
        IsDefault BIT NOT NULL CONSTRAINT DF_UserDashboards_IsDefault DEFAULT(1),
        IsDeleted BIT NOT NULL CONSTRAINT DF_UserDashboards_IsDeleted DEFAULT(0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_UserDashboards_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        CreatedByUserId BIGINT NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedByUserId BIGINT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.DashboardWidgets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DashboardWidgets
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_DashboardWidgets PRIMARY KEY,
        UserDashboardId BIGINT NOT NULL,
        WidgetType TINYINT NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        ConfigJson NVARCHAR(MAX) NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_DashboardWidgets_SortOrder DEFAULT(0),
        GridX INT NOT NULL CONSTRAINT DF_DashboardWidgets_GridX DEFAULT(0),
        GridY INT NOT NULL CONSTRAINT DF_DashboardWidgets_GridY DEFAULT(0),
        GridW INT NOT NULL CONSTRAINT DF_DashboardWidgets_GridW DEFAULT(4),
        GridH INT NOT NULL CONSTRAINT DF_DashboardWidgets_GridH DEFAULT(2)
    );
END
GO

/* ---------------------------
   İK
----------------------------*/
IF OBJECT_ID(N'dbo.HrRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HrRequests
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_HrRequests PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        RequestType TINYINT NOT NULL,
        RequestedByUserId BIGINT NOT NULL,
        WorkItemId BIGINT NULL,
        StartDate DATE NULL,
        EndDate DATE NULL,
        Amount DECIMAL(18,2) NULL,
        AssetName NVARCHAR(200) NULL,
        [Description] NVARCHAR(MAX) NULL,
        [Status] TINYINT NOT NULL CONSTRAINT DF_HrRequests_Status DEFAULT(2),
        DecisionNote NVARCHAR(1000) NULL,
        DecidedByUserId BIGINT NULL,
        DecidedAtUtc DATETIME2 NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_HrRequests_IsDeleted DEFAULT(0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_HrRequests_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        CreatedByUserId BIGINT NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedByUserId BIGINT NULL
    );
END
GO

/* ---------------------------
   Satın alma
----------------------------*/
IF OBJECT_ID(N'dbo.PurchaseRequests', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseRequests
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseRequests PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        RequestedByUserId BIGINT NOT NULL,
        WorkItemId BIGINT NULL,
        Title NVARCHAR(300) NOT NULL,
        [Description] NVARCHAR(MAX) NULL,
        EstimatedBudget DECIMAL(18,2) NULL,
        NeededDate DATE NULL,
        [Status] TINYINT NOT NULL CONSTRAINT DF_PurchaseRequests_Status DEFAULT(1),
        IsDeleted BIT NOT NULL CONSTRAINT DF_PurchaseRequests_IsDeleted DEFAULT(0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PurchaseRequests_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        CreatedByUserId BIGINT NULL,
        UpdatedAtUtc DATETIME2 NULL,
        UpdatedByUserId BIGINT NULL
    );
END
GO

IF OBJECT_ID(N'dbo.PurchaseOffers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOffers
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOffers PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        PurchaseRequestId BIGINT NOT NULL,
        SupplierName NVARCHAR(200) NOT NULL,
        Amount DECIMAL(18,2) NOT NULL,
        Currency NVARCHAR(8) NOT NULL CONSTRAINT DF_PurchaseOffers_Currency DEFAULT(N'TRY'),
        Notes NVARCHAR(1000) NULL,
        IsSelected BIT NOT NULL CONSTRAINT DF_PurchaseOffers_IsSelected DEFAULT(0),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_PurchaseOffers_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        IsDeleted BIT NOT NULL CONSTRAINT DF_PurchaseOffers_IsDeleted DEFAULT(0)
    );
END
GO

IF OBJECT_ID(N'dbo.PurchaseOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PurchaseOrders
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PurchaseOrders PRIMARY KEY,
        TenantId BIGINT NOT NULL,
        PurchaseRequestId BIGINT NOT NULL,
        OrderNo NVARCHAR(80) NOT NULL,
        OrderedAtUtc DATETIME2 NOT NULL,
        DeliveredAtUtc DATETIME2 NULL,
        Notes NVARCHAR(1000) NULL,
        IsDeleted BIT NOT NULL CONSTRAINT DF_PurchaseOrders_IsDeleted DEFAULT(0)
    );
END
GO

/* ---------------------------
   İndeksler
----------------------------*/
IF OBJECT_ID(N'dbo.WorkItemSlas', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_WorkItemSlas_WorkItemId' AND object_id = OBJECT_ID(N'dbo.WorkItemSlas'))
    CREATE UNIQUE INDEX IX_WorkItemSlas_WorkItemId ON dbo.WorkItemSlas(WorkItemId);
GO
IF OBJECT_ID(N'dbo.UserDashboards', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserDashboards_UserId_IsDefault' AND object_id = OBJECT_ID(N'dbo.UserDashboards'))
    CREATE INDEX IX_UserDashboards_UserId_IsDefault ON dbo.UserDashboards(UserId, IsDefault);
GO
IF OBJECT_ID(N'dbo.DashboardWidgets', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DashboardWidgets_UserDashboardId_SortOrder' AND object_id = OBJECT_ID(N'dbo.DashboardWidgets'))
    CREATE INDEX IX_DashboardWidgets_UserDashboardId_SortOrder ON dbo.DashboardWidgets(UserDashboardId, SortOrder);
GO
IF OBJECT_ID(N'dbo.HrRequests', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_HrRequests_Status' AND object_id = OBJECT_ID(N'dbo.HrRequests'))
    CREATE INDEX IX_HrRequests_Status ON dbo.HrRequests(TenantId, [Status]);
GO
IF OBJECT_ID(N'dbo.PurchaseRequests', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_PurchaseRequests_Status' AND object_id = OBJECT_ID(N'dbo.PurchaseRequests'))
    CREATE INDEX IX_PurchaseRequests_Status ON dbo.PurchaseRequests(TenantId, [Status]);
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Attachments_ParentAttachmentId' AND object_id = OBJECT_ID(N'dbo.Attachments'))
    CREATE INDEX IX_Attachments_ParentAttachmentId ON dbo.Attachments(ParentAttachmentId);
GO

/* ---------------------------
   Foreign key'ler: tablo varsa ve FK yoksa eklenir.
----------------------------*/
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Attachments_Roles_AllowedRoleId')
    ALTER TABLE dbo.Attachments ADD CONSTRAINT FK_Attachments_Roles_AllowedRoleId FOREIGN KEY (AllowedRoleId) REFERENCES dbo.Roles(Id);
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Attachments_Users_UploadedByUserId')
    ALTER TABLE dbo.Attachments ADD CONSTRAINT FK_Attachments_Users_UploadedByUserId FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users(Id);
GO
IF OBJECT_ID(N'dbo.Attachments', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Attachments_Attachments_ParentAttachmentId')
    ALTER TABLE dbo.Attachments ADD CONSTRAINT FK_Attachments_Attachments_ParentAttachmentId FOREIGN KEY (ParentAttachmentId) REFERENCES dbo.Attachments(Id);
GO
IF OBJECT_ID(N'dbo.ApprovalWorkflowTemplateSteps', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ApprovalWorkflowTemplateSteps_ApprovalWorkflowTemplates')
    ALTER TABLE dbo.ApprovalWorkflowTemplateSteps ADD CONSTRAINT FK_ApprovalWorkflowTemplateSteps_ApprovalWorkflowTemplates FOREIGN KEY (ApprovalWorkflowTemplateId) REFERENCES dbo.ApprovalWorkflowTemplates(Id);
GO
IF OBJECT_ID(N'dbo.ApprovalRequests', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.ApprovalWorkflowTemplates', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ApprovalRequests_ApprovalWorkflowTemplates_TemplateId')
    ALTER TABLE dbo.ApprovalRequests ADD CONSTRAINT FK_ApprovalRequests_ApprovalWorkflowTemplates_TemplateId FOREIGN KEY (TemplateId) REFERENCES dbo.ApprovalWorkflowTemplates(Id);
GO
IF OBJECT_ID(N'dbo.WorkItemSlas', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_WorkItemSlas_SlaPolicies')
    ALTER TABLE dbo.WorkItemSlas ADD CONSTRAINT FK_WorkItemSlas_SlaPolicies FOREIGN KEY (SlaPolicyId) REFERENCES dbo.SlaPolicies(Id);
GO
IF OBJECT_ID(N'dbo.UserDashboards', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_UserDashboards_Users_UserId')
    ALTER TABLE dbo.UserDashboards ADD CONSTRAINT FK_UserDashboards_Users_UserId FOREIGN KEY (UserId) REFERENCES dbo.Users(Id);
GO
IF OBJECT_ID(N'dbo.DashboardWidgets', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DashboardWidgets_UserDashboards')
    ALTER TABLE dbo.DashboardWidgets ADD CONSTRAINT FK_DashboardWidgets_UserDashboards FOREIGN KEY (UserDashboardId) REFERENCES dbo.UserDashboards(Id);
GO
IF OBJECT_ID(N'dbo.HrRequests', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_HrRequests_Users_RequestedByUserId')
    ALTER TABLE dbo.HrRequests ADD CONSTRAINT FK_HrRequests_Users_RequestedByUserId FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(Id);
GO
IF OBJECT_ID(N'dbo.PurchaseRequests', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseRequests_Users_RequestedByUserId')
    ALTER TABLE dbo.PurchaseRequests ADD CONSTRAINT FK_PurchaseRequests_Users_RequestedByUserId FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(Id);
GO
IF OBJECT_ID(N'dbo.PurchaseOffers', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOffers_PurchaseRequests')
    ALTER TABLE dbo.PurchaseOffers ADD CONSTRAINT FK_PurchaseOffers_PurchaseRequests FOREIGN KEY (PurchaseRequestId) REFERENCES dbo.PurchaseRequests(Id);
GO
IF OBJECT_ID(N'dbo.PurchaseOrders', N'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_PurchaseOrders_PurchaseRequests')
    ALTER TABLE dbo.PurchaseOrders ADD CONSTRAINT FK_PurchaseOrders_PurchaseRequests FOREIGN KEY (PurchaseRequestId) REFERENCES dbo.PurchaseRequests(Id);
GO

/* ---------------------------
   Yeni izin kataloğu
----------------------------*/
IF OBJECT_ID(N'dbo.Permissions', N'U') IS NOT NULL
BEGIN
    DECLARE @NewPermissions TABLE([Key] NVARCHAR(200), Module NVARCHAR(100), DisplayName NVARCHAR(200), SortOrder INT);
    INSERT INTO @NewPermissions([Key], Module, DisplayName, SortOrder)
    VALUES
        (N'Document.Manage', N'Dosya', N'Dosya merkezini yönet', 600),
        (N'Approval.Manage', N'Onay', N'Onay süreçlerini yönet', 610),
        (N'FormDesigner.Manage', N'Form', N'Form tasarımcısını yönet', 620),
        (N'Automation.Manage', N'Otomasyon', N'Otomasyon kurallarını yönet', 630),
        (N'Sla.Manage', N'SLA', N'SLA politikalarını yönet', 640),
        (N'Dashboard.Manage', N'Dashboard', N'Dashboard tasarlayabilme', 650),
        (N'Calendar.View', N'Takvim', N'Takvim görüntüleme', 660),
        (N'HR.Manage', N'İK', N'İK taleplerini yönet', 670),
        (N'Purchasing.Manage', N'Satın Alma', N'Satın alma süreçlerini yönet', 680),
        (N'Performance.View', N'Performans', N'Performans puanlarını görüntüleme', 690),
        (N'AI.Use', N'AI', N'AI çalışma alanını kullanma', 700);

    INSERT INTO dbo.Permissions([Key], Module, DisplayName, SortOrder)
    SELECT np.[Key], np.Module, np.DisplayName, np.SortOrder
    FROM @NewPermissions np
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Permissions p WHERE p.[Key] = np.[Key]);

    IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL
    BEGIN
        DECLARE @AdminRoleId BIGINT;
        SELECT TOP 1 @AdminRoleId = Id
        FROM dbo.Roles
        WHERE NormalizedName IN (N'ADMIN', N'YONETICI', N'YÖNETICI', N'YÖNETİCİ')
           OR [Name] LIKE N'%Admin%'
           OR [Name] LIKE N'%Yönet%'
        ORDER BY IsSystem DESC, Id;

        IF @AdminRoleId IS NOT NULL
        BEGIN
            INSERT INTO dbo.RolePermissions(RoleId, PermissionId)
            SELECT @AdminRoleId, p.Id
            FROM dbo.Permissions p
            WHERE p.[Key] IN (SELECT [Key] FROM @NewPermissions)
              AND NOT EXISTS (
                  SELECT 1 FROM dbo.RolePermissions rp
                  WHERE rp.RoleId = @AdminRoleId AND rp.PermissionId = p.Id
              );
        END
    END
END
GO
