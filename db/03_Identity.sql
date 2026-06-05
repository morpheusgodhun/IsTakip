/* ============================================================================
   Faz 2 - ASP.NET Core Identity Uyum Scripti
   ----------------------------------------------------------------------------
   01_Schema.sql ve 02_Seed.sql'den SONRA çalıştırın.
   EF Core Identity modelinin mevcut dbo.Users / dbo.Roles tablolarına
   sorunsuz map edilebilmesi için gereken ek kolonu ve uydu tabloları ekler.
   Tekrar çalıştırmaya karşı korumalıdır.
   ============================================================================ */

USE [IsTakip];
GO
SET NOCOUNT ON;
GO

/* Users tablosunda Identity'nin beklediği eksik kolon */
IF COL_LENGTH('dbo.Users', 'PhoneNumberConfirmed') IS NULL
    ALTER TABLE dbo.Users ADD PhoneNumberConfirmed BIT NOT NULL CONSTRAINT DF_Users_PhoneConfirmed DEFAULT(0);
GO

/* IdentityUserClaim<long> -> dbo.UserClaims */
IF OBJECT_ID('dbo.UserClaims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserClaims
    (
        Id          INT             IDENTITY(1,1) NOT NULL,
        UserId      BIGINT          NOT NULL,
        ClaimType   NVARCHAR(MAX)   NULL,
        ClaimValue  NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_UserClaims PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_UserClaims_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
    CREATE INDEX IX_UserClaims_UserId ON dbo.UserClaims(UserId);
END
GO

/* IdentityUserLogin<long> -> dbo.UserLogins */
IF OBJECT_ID('dbo.UserLogins', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserLogins
    (
        LoginProvider       NVARCHAR(450)   NOT NULL,
        ProviderKey         NVARCHAR(450)   NOT NULL,
        ProviderDisplayName NVARCHAR(MAX)   NULL,
        UserId              BIGINT          NOT NULL,
        CONSTRAINT PK_UserLogins PRIMARY KEY CLUSTERED (LoginProvider, ProviderKey),
        CONSTRAINT FK_UserLogins_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
    CREATE INDEX IX_UserLogins_UserId ON dbo.UserLogins(UserId);
END
GO

/* IdentityUserToken<long> -> dbo.UserTokens */
IF OBJECT_ID('dbo.UserTokens', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserTokens
    (
        UserId        BIGINT          NOT NULL,
        LoginProvider NVARCHAR(450)   NOT NULL,
        Name          NVARCHAR(450)   NOT NULL,
        Value         NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_UserTokens PRIMARY KEY CLUSTERED (UserId, LoginProvider, Name),
        CONSTRAINT FK_UserTokens_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );
END
GO

/* IdentityRoleClaim<long> -> dbo.RoleClaims */
IF OBJECT_ID('dbo.RoleClaims', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RoleClaims
    (
        Id          INT             IDENTITY(1,1) NOT NULL,
        RoleId      BIGINT          NOT NULL,
        ClaimType   NVARCHAR(MAX)   NULL,
        ClaimValue  NVARCHAR(MAX)   NULL,
        CONSTRAINT PK_RoleClaims PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RoleClaims_Role FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id)
    );
    CREATE INDEX IX_RoleClaims_RoleId ON dbo.RoleClaims(RoleId);
END
GO

PRINT N'Identity uyum scripti tamamlandı.';
GO
