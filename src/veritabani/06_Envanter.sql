/* =====================================================================
   IsTakip - Envanter / Zimmet Modülü tabloları
   Idempotent: tablo yoksa oluşturur. SSMS'te IsTakip veritabanında çalıştırın.
   ===================================================================== */
USE IsTakip;
GO

IF OBJECT_ID(N'dbo.InventoryCategories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryCategories (
        Id        BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryCategories PRIMARY KEY,
        TenantId  BIGINT       NOT NULL,
        Name      NVARCHAR(200) NOT NULL,
        IsDeleted BIT          NOT NULL CONSTRAINT DF_InvCat_IsDeleted DEFAULT(0)
    );
    CREATE INDEX IX_InventoryCategories_Tenant ON dbo.InventoryCategories(TenantId);
END
GO

IF OBJECT_ID(N'dbo.InventoryItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryItems (
        Id                   BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryItems PRIMARY KEY,
        TenantId             BIGINT        NOT NULL,
        Name                 NVARCHAR(200) NOT NULL,
        CategoryId           BIGINT        NULL,
        SerialNo             NVARCHAR(100) NULL,
        Code                 NVARCHAR(100) NULL,
        Status               TINYINT       NOT NULL CONSTRAINT DF_InvItem_Status DEFAULT(1),
        CurrentHolderUserId  BIGINT        NULL,
        Notes                NVARCHAR(1000) NULL,
        CreatedAtUtc         DATETIME2     NOT NULL CONSTRAINT DF_InvItem_Created DEFAULT(SYSUTCDATETIME()),
        IsDeleted            BIT           NOT NULL CONSTRAINT DF_InvItem_IsDeleted DEFAULT(0)
    );
    CREATE INDEX IX_InventoryItems_Tenant   ON dbo.InventoryItems(TenantId);
    CREATE INDEX IX_InventoryItems_Category ON dbo.InventoryItems(CategoryId);
    CREATE INDEX IX_InventoryItems_Holder   ON dbo.InventoryItems(CurrentHolderUserId);
END
GO

IF OBJECT_ID(N'dbo.InventoryAssignments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryAssignments (
        Id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryAssignments PRIMARY KEY,
        TenantId         BIGINT     NOT NULL,
        InventoryItemId  BIGINT     NOT NULL,
        AssignedToUserId BIGINT     NOT NULL,
        AssignedByUserId BIGINT     NULL,
        AssignedAtUtc    DATETIME2  NOT NULL CONSTRAINT DF_InvAsg_Assigned DEFAULT(SYSUTCDATETIME()),
        ReturnedAtUtc    DATETIME2  NULL,
        ReturnedByUserId BIGINT     NULL,
        Notes            NVARCHAR(1000) NULL
    );
    CREATE INDEX IX_InventoryAssignments_Item   ON dbo.InventoryAssignments(InventoryItemId);
    CREATE INDEX IX_InventoryAssignments_ToUser ON dbo.InventoryAssignments(AssignedToUserId);
    CREATE INDEX IX_InventoryAssignments_Active ON dbo.InventoryAssignments(ReturnedAtUtc);
END
GO

IF OBJECT_ID(N'dbo.InventoryCounts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryCounts (
        Id             BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryCounts PRIMARY KEY,
        TenantId       BIGINT     NOT NULL,
        Name           NVARCHAR(200) NOT NULL,
        CountedByUserId BIGINT    NULL,
        CreatedAtUtc   DATETIME2  NOT NULL CONSTRAINT DF_InvCount_Created DEFAULT(SYSUTCDATETIME()),
        CompletedAtUtc DATETIME2  NULL,
        Notes          NVARCHAR(1000) NULL
    );
    CREATE INDEX IX_InventoryCounts_Tenant ON dbo.InventoryCounts(TenantId);
END
GO

IF OBJECT_ID(N'dbo.InventoryCountLines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.InventoryCountLines (
        Id               BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_InventoryCountLines PRIMARY KEY,
        InventoryCountId BIGINT NOT NULL,
        InventoryItemId  BIGINT NOT NULL,
        IsFound          BIT    NOT NULL CONSTRAINT DF_InvCountLine_Found DEFAULT(0),
        Note             NVARCHAR(500) NULL
    );
    CREATE INDEX IX_InventoryCountLines_Count ON dbo.InventoryCountLines(InventoryCountId);
END
GO

PRINT 'Envanter tabloları hazır.';
