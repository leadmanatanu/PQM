IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='AssociationLogicalName' AND xtype='U')
BEGIN
    CREATE TABLE [AssociationLogicalName] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [DeviceId] INT NOT NULL,
        [Name] NVARCHAR(MAX) NULL,
        [ObjectType] NVARCHAR(MAX) NULL,
        [Value] NVARCHAR(MAX) NULL,
        [DateEntered] DATETIME2 NOT NULL
    )
END

DECLARE @HeaderId INT;
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'AssociationLogicalName';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'AssociationLogicalName');
    SET @HeaderId = SCOPE_IDENTITY();
END

-- Seed the 9 attributes in DLMSObject and ObjectParameter for BOTH standard OBIS codes
IF OBJECT_ID('tempdb..#AssocSeed') IS NOT NULL DROP TABLE #AssocSeed;
CREATE TABLE #AssocSeed (
    Name NVARCHAR(150),
    AttributeId INT,
    DataType NVARCHAR(100),
    AccessType NVARCHAR(100)
);

INSERT INTO #AssocSeed (Name, AttributeId, DataType, AccessType) VALUES
('Object List', 2, 'Array', 'R'),
('Associated Partners ID', 3, 'Structure', 'R'),
('Application Context Name', 4, 'OctetString', 'R'),
('xDLMS Context Info', 5, 'Structure', 'R'),
('Authentication Mechanism Name', 6, 'OctetString', 'R'),
('LLS Secret', 7, 'OctetString', 'R/W'),
('Association Status', 8, 'Enum', 'R'),
('Security Setup Reference', 9, 'OctetString', 'R'),
('User List', 10, 'Array', 'R/W');

-- Seed for 0.0.40.0.1.255 (Public Client Association)
DECLARE @Name NVARCHAR(150), @AttrId INT, @DataType NVARCHAR(100), @AccessType NVARCHAR(100);
DECLARE @ObjectId INT;

DECLARE AssocCursor CURSOR FOR 
SELECT Name, AttributeId, DataType, AccessType FROM #AssocSeed;

OPEN AssocCursor;
FETCH NEXT FROM AssocCursor INTO @Name, @AttrId, @DataType, @AccessType;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Find or create DLMSObject for Public Client
    SET @ObjectId = NULL;
    SELECT @ObjectId = Id FROM DLMSObject WHERE HeaderId = @HeaderId AND Name = @Name + ' (Public)' AND ObisCode = '0.0.40.0.1.255';
    
    IF @ObjectId IS NULL
    BEGIN
        INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
        VALUES (@HeaderId, @Name + ' (Public)', '0.0.40.0.1.255', 'AssociationLogicalName');
        SET @ObjectId = SCOPE_IDENTITY();
    END

    -- Find or create ObjectParameter
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = @AttrId)
    BEGIN
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, @AttrId, @Name, @DataType, @AccessType);
    END

    FETCH NEXT FROM AssocCursor INTO @Name, @AttrId, @DataType, @AccessType;
END

CLOSE AssocCursor;
DEALLOCATE AssocCursor;

-- Seed for 0.0.40.0.2.255 (Management Client Association)
DECLARE AssocCursor2 CURSOR FOR 
SELECT Name, AttributeId, DataType, AccessType FROM #AssocSeed;

OPEN AssocCursor2;
FETCH NEXT FROM AssocCursor2 INTO @Name, @AttrId, @DataType, @AccessType;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Find or create DLMSObject for Management Client
    SET @ObjectId = NULL;
    SELECT @ObjectId = Id FROM DLMSObject WHERE HeaderId = @HeaderId AND Name = @Name + ' (Management)' AND ObisCode = '0.0.40.0.2.255';
    
    IF @ObjectId IS NULL
    BEGIN
        INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
        VALUES (@HeaderId, @Name + ' (Management)', '0.0.40.0.2.255', 'AssociationLogicalName');
        SET @ObjectId = SCOPE_IDENTITY();
    END

    -- Find or create ObjectParameter
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = @AttrId)
    BEGIN
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, @AttrId, @Name, @DataType, @AccessType);
    END

    FETCH NEXT FROM AssocCursor2 INTO @Name, @AttrId, @DataType, @AccessType;
END

CLOSE AssocCursor2;
DEALLOCATE AssocCursor2;

DROP TABLE #AssocSeed;
