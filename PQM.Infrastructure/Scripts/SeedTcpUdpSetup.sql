IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='TcpUdpSetup' AND xtype='U')
BEGIN
    CREATE TABLE [TcpUdpSetup] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [DeviceId] INT NOT NULL,
        [Name] NVARCHAR(MAX) NULL,
        [ObjectType] NVARCHAR(MAX) NULL,
        [Value] NVARCHAR(MAX) NULL,
        [DateEntered] DATETIME2 NOT NULL
    )
END

DECLARE @HeaderId INT;
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'TcpUdpSetup';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'TcpUdpSetup');
    SET @HeaderId = SCOPE_IDENTITY();
END

-- Seed the 5 attributes in DLMSObject and ObjectParameter
IF OBJECT_ID('tempdb..#TcpSeed') IS NOT NULL DROP TABLE #TcpSeed;
CREATE TABLE #TcpSeed (
    Name NVARCHAR(150),
    AttributeId INT,
    DataType NVARCHAR(100),
    AccessType NVARCHAR(100)
);

INSERT INTO #TcpSeed (Name, AttributeId, DataType, AccessType) VALUES
('Port', 2, 'Integer', 'R/W'),
('IP Reference', 3, 'IPAddress', 'R/W'),
('Max Segment Size', 4, 'Integer', 'R/W'),
('Max Connections', 5, 'Integer', 'R/W'),
('Inactivity Timeout', 6, 'Integer', 'R/W');

DECLARE @Name NVARCHAR(150), @AttrId INT, @DataType NVARCHAR(100), @AccessType NVARCHAR(100);
DECLARE @ObjectId INT;

DECLARE TcpCursor CURSOR FOR 
SELECT Name, AttributeId, DataType, AccessType FROM #TcpSeed;

OPEN TcpCursor;
FETCH NEXT FROM TcpCursor INTO @Name, @AttrId, @DataType, @AccessType;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Find or create DLMSObject
    SET @ObjectId = NULL;
    SELECT @ObjectId = Id FROM DLMSObject WHERE HeaderId = @HeaderId AND Name = @Name;
    
    IF @ObjectId IS NULL
    BEGIN
        INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
        VALUES (@HeaderId, @Name, '0.0.25.0.0.255', 'TcpUdpSetup');
        SET @ObjectId = SCOPE_IDENTITY();
    END

    -- Find or create ObjectParameter
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = @AttrId)
    BEGIN
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, @AttrId, @Name, @DataType, @AccessType);
    END

    FETCH NEXT FROM TcpCursor INTO @Name, @AttrId, @DataType, @AccessType;
END

CLOSE TcpCursor;
DEALLOCATE TcpCursor;
DROP TABLE #TcpSeed;
