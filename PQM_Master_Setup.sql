-- ============================================================================
-- PQM Master Database Setup & Seed Script
-- Execute this script on a fresh SQL Server database to create all working tables
-- and seed required catalog profiles, meter types, users, and schedules.
-- ============================================================================

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'PQM')
BEGIN
    CREATE DATABASE [PQM];
END
GO

USE [PQM];
GO

-- 1. AuthenticationType
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AuthenticationType]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[AuthenticationType](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] [nvarchar](50) NULL
    );
END
GO

-- 2. MeterType
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[MeterType]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[MeterType](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] [nvarchar](100) NOT NULL
    );
END
GO

-- 3. Profiles
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Profiles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Profiles](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ObisCode] [nvarchar](50) NOT NULL UNIQUE,
        [Name] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](255) NULL
    );
END
GO

-- 4. Parameters
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Parameters]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Parameters](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProfileId] [int] NOT NULL,
        [ObisCode] [nvarchar](50) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Unit] [nvarchar](20) NULL,
        [Scaler] [int] NOT NULL DEFAULT 0,
        FOREIGN KEY ([ProfileId]) REFERENCES [dbo].[Profiles]([Id])
    );
END
GO

-- 5. Devices
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Devices]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Devices](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name] [nvarchar](max) NOT NULL,
        [IP] [nvarchar](max) NOT NULL,
        [PORT] [int] NOT NULL DEFAULT 4059,
        [SerialNumber] [nvarchar](max) NULL,
        [ConsumerNumber] [nvarchar](max) NULL,
        [IsActive] [bit] NOT NULL DEFAULT 1,
        [IsDeleted] [bit] NOT NULL DEFAULT 0,
        [CreatedDate] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [CreatedId] [int] NULL,
        [ModifiedDate] [datetime2](7) NULL,
        [ModifiedId] [int] NULL,
        [LastSync] [datetime2](7) NULL,
        [ClientAddress] [int] NULL DEFAULT 16,
        [Password] [nvarchar](max) NULL,
        [ServerAddress] [int] NULL DEFAULT 1,
        [Timeout] [int] NULL DEFAULT 30000,
        [LastConnectionAttempt] [datetime2](7) NULL,
        [LastError] [nvarchar](max) NULL,
        [Status] [nvarchar](max) NOT NULL DEFAULT 'Offline',
        [TypeName] [nvarchar](max) NOT NULL DEFAULT 'DLMS/COSEM',
        [MeterTypeId] [int] NOT NULL DEFAULT 1,
        [AuthenticationTypeId] [int] NULL DEFAULT 1,
        [TimeZoneId] [nvarchar](50) NULL DEFAULT 'India Standard Time'
    );
END
GO

-- 6. DeviceSyncSchedule
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeviceSyncSchedule]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DeviceSyncSchedule](
        [DeviceId] [int] NOT NULL PRIMARY KEY,
        [IsEnabled] [bit] NOT NULL DEFAULT 1,
        [ScheduledTime] [time](7) NOT NULL DEFAULT '00:00:00',
        [RepeatMode] [nvarchar](20) NOT NULL DEFAULT 'Daily',
        [NextRunAtUtc] [datetime2](7) NULL,
        [LastRunAtUtc] [datetime2](7) NULL,
        [LastRunStatus] [nvarchar](20) NULL,
        FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[Devices]([Id]) ON DELETE CASCADE
    );
END
GO

-- 7. DeviceSyncHistory
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeviceSyncHistory]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DeviceSyncHistory](
        [Id] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [DeviceId] [int] NOT NULL,
        [StartedAt] [datetime2](7) NOT NULL,
        [CompletedAt] [datetime2](7) NULL,
        [Status] [nvarchar](20) NOT NULL,
        [ErrorMessage] [nvarchar](max) NULL,
        [ProfilesRead] [int] NULL,
        [RowsWritten] [int] NULL,
        FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[Devices]([Id]) ON DELETE CASCADE
    );
END
GO

-- 8. DeviceSyncRequest
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DeviceSyncRequest]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[DeviceSyncRequest](
        [Id] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [DeviceId] [int] NOT NULL,
        [RequestedAt] [datetime2](7) NOT NULL DEFAULT GETUTCDATE(),
        [Status] [nvarchar](20) NOT NULL DEFAULT 'Pending',
        [ErrorMessage] [nvarchar](max) NULL,
        FOREIGN KEY ([DeviceId]) REFERENCES [dbo].[Devices]([Id]) ON DELETE CASCADE
    );
END
GO

-- 9. User Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[User]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[User](
        [Id] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Username] [nvarchar](max) NOT NULL,
        [Email] [nvarchar](max) NOT NULL,
        [Password] [nvarchar](max) NOT NULL,
        [CreatedDate] [datetime2](7) NOT NULL DEFAULT GETUTCDATE()
    );
END
GO

-- ============================================================================
-- SEED DATA
-- ============================================================================

-- Seed AuthenticationType (Exact string names expected by Gurux DLMS / C# Device entity mapping)
IF NOT EXISTS (SELECT 1 FROM [dbo].[AuthenticationType])
BEGIN
    INSERT INTO [dbo].[AuthenticationType] ([Name]) VALUES ('None'), ('Low'), ('High');
END

-- Seed MeterType
IF NOT EXISTS (SELECT 1 FROM [dbo].[MeterType])
BEGIN
    INSERT INTO [dbo].[MeterType] ([Name]) VALUES ('DLMS/COSEM'), ('Modbus RTU'), ('Modbus TCP');
END

-- Seed Default Admin User
IF NOT EXISTS (SELECT 1 FROM [dbo].[User])
BEGIN
    INSERT INTO [dbo].[User] ([Username], [Email], [Password], [CreatedDate])
    VALUES ('admin', 'admin@pqm.com', 'AQAAAAIAAYagAAAAEJ0z7R...dummy_hash...', GETUTCDATE());
END

-- Seed Sample Placeholder Device (IP 127.0.0.1:4059 -- replace with your own meter or DLMS simulator IP)
IF NOT EXISTS (SELECT 1 FROM [dbo].[Devices])
BEGIN
    SET IDENTITY_INSERT [dbo].[Devices] ON;
    INSERT INTO [dbo].[Devices] ([Id], [Name], [IP], [PORT], [SerialNumber], [IsActive], [IsDeleted], [CreatedDate], [TypeName], [MeterTypeId], [AuthenticationTypeId], [Status])
    VALUES (5, 'Placeholder Meter', '127.0.0.1', 4059, 'SIM-001', 1, 0, GETUTCDATE(), 'DLMS/COSEM', 1, 1, 'Offline');
    SET IDENTITY_INSERT [dbo].[Devices] OFF;
END

-- Seed Schedule for Placeholder Device 5
IF NOT EXISTS (SELECT 1 FROM [dbo].[DeviceSyncSchedule] WHERE DeviceId = 5)
BEGIN
    INSERT INTO [dbo].[DeviceSyncSchedule] ([DeviceId], [IsEnabled], [ScheduledTime], [RepeatMode], [NextRunAtUtc])
    VALUES (5, 1, '12:00:00', 'Daily', DATEADD(hour, 24, GETUTCDATE()));
END
GO

PRINT 'PQM Master Database Setup & Seed Completed Successfully!';
