IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Ip4Setup' AND xtype='U')
BEGIN
    CREATE TABLE [Ip4Setup] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [DeviceId] INT NOT NULL,
        [Name] NVARCHAR(MAX) NULL,
        [ObjectType] NVARCHAR(MAX) NULL,
        [Value] NVARCHAR(MAX) NULL,
        [DateEntered] DATETIME2 NOT NULL
    )
END

DECLARE @HeaderId INT;
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'Ip4Setup';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'Ip4Setup');
    SET @HeaderId = SCOPE_IDENTITY();
END

-- Seed the 9 attributes in DLMSObject and ObjectParameter
IF OBJECT_ID('tempdb..#Ip4Seed') IS NOT NULL DROP TABLE #Ip4Seed;
CREATE TABLE #Ip4Seed (
    Name NVARCHAR(150),
    AttributeId INT,
    DataType NVARCHAR(100),
    AccessType NVARCHAR(100)
);

INSERT INTO #Ip4Seed (Name, AttributeId, DataType, AccessType) VALUES
('Data Link Layer Reference', 2, 'OctetString', 'R/W'),
('IP Address', 3, 'IPAddress', 'R/W'),
('Multicast IP Address', 4, 'Array', 'R/W'),
('IP Options', 5, 'OctetString', 'R/W'),
('Subnet Mask', 6, 'IPAddress', 'R/W'),
('Gateway IP Address', 7, 'IPAddress', 'R/W'),
('Use DHCP', 8, 'Boolean', 'R/W'),
('Primary DNS Address', 9, 'IPAddress', 'R/W'),
('Secondary DNS Address', 10, 'IPAddress', 'R/W');

DECLARE @Name NVARCHAR(150), @AttrId INT, @DataType NVARCHAR(100), @AccessType NVARCHAR(100);
DECLARE @ObjectId INT;

DECLARE Ip4Cursor CURSOR FOR 
SELECT Name, AttributeId, DataType, AccessType FROM #Ip4Seed;

OPEN Ip4Cursor;
FETCH NEXT FROM Ip4Cursor INTO @Name, @AttrId, @DataType, @AccessType;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Find or create DLMSObject
    SET @ObjectId = NULL;
    SELECT @ObjectId = Id FROM DLMSObject WHERE HeaderId = @HeaderId AND Name = @Name;
    
    IF @ObjectId IS NULL
    BEGIN
        INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
        VALUES (@HeaderId, @Name, '0.0.25.1.0.255', 'Ip4Setup');
        SET @ObjectId = SCOPE_IDENTITY();
    END

    -- Find or create ObjectParameter
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = @AttrId)
    BEGIN
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, @AttrId, @Name, @DataType, @AccessType);
    END

    FETCH NEXT FROM Ip4Cursor INTO @Name, @AttrId, @DataType, @AccessType;
END

CLOSE Ip4Cursor;
DEALLOCATE Ip4Cursor;
DROP TABLE #Ip4Seed;
