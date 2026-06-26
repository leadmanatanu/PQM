IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='IecHdlcSetup' AND xtype='U')
BEGIN
    CREATE TABLE [IecHdlcSetup] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [DeviceId] INT NOT NULL,
        [Name] NVARCHAR(MAX) NULL,
        [ObjectType] NVARCHAR(MAX) NULL,
        [Value] NVARCHAR(MAX) NULL,
        [DateEntered] DATETIME2 NOT NULL
    )
END

DECLARE @HeaderId INT;
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'lecHdlcSetup';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'lecHdlcSetup');
    SET @HeaderId = SCOPE_IDENTITY();
END

-- Seed the 8 attributes in DLMSObject and ObjectParameter
IF OBJECT_ID('tempdb..#HdlcSeed') IS NOT NULL DROP TABLE #HdlcSeed;
CREATE TABLE #HdlcSeed (
    Name NVARCHAR(150),
    AttributeId INT,
    DataType NVARCHAR(100),
    AccessType NVARCHAR(100)
);

INSERT INTO #HdlcSeed (Name, AttributeId, DataType, AccessType) VALUES
('Speed', 2, 'Baudrate', 'R/W'),
('Transmit Window Size', 3, 'Integer', 'R/W'),
('Receive Window Size', 4, 'Integer', 'R/W'),
('Transmit Maximum Length', 5, 'Integer', 'R/W'),
('Receive Maximum Length', 6, 'Integer', 'R/W'),
('Internal Timeout', 7, 'Integer', 'R/W'),
('Inactivity Timeout', 8, 'Integer', 'R/W'),
('Device Address', 9, 'Integer', 'R/W');

DECLARE @Name NVARCHAR(150), @AttrId INT, @DataType NVARCHAR(100), @AccessType NVARCHAR(100);
DECLARE @ObjectId INT;

DECLARE HdlcCursor CURSOR FOR 
SELECT Name, AttributeId, DataType, AccessType FROM #HdlcSeed;

OPEN HdlcCursor;
FETCH NEXT FROM HdlcCursor INTO @Name, @AttrId, @DataType, @AccessType;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Find or create DLMSObject
    SET @ObjectId = NULL;
    SELECT @ObjectId = Id FROM DLMSObject WHERE HeaderId = @HeaderId AND Name = @Name;
    
    IF @ObjectId IS NULL
    BEGIN
        INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
        VALUES (@HeaderId, @Name, '0.0.22.0.0.255', 'IecHdlcSetup');
        SET @ObjectId = SCOPE_IDENTITY();
    END

    -- Find or create ObjectParameter
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = @AttrId)
    BEGIN
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, @AttrId, @Name, @DataType, @AccessType);
    END

    FETCH NEXT FROM HdlcCursor INTO @Name, @AttrId, @DataType, @AccessType;
END

CLOSE HdlcCursor;
DEALLOCATE HdlcCursor;
DROP TABLE #HdlcSeed;
