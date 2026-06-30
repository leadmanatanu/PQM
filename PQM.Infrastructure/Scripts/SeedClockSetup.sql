IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Clock' AND xtype='U')
BEGIN
    CREATE TABLE [Clock] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [DeviceId] INT NOT NULL,
        [Name] NVARCHAR(MAX) NULL,
        [ObjectType] NVARCHAR(MAX) NULL,
        [Value] NVARCHAR(MAX) NULL,
        [DateEntered] DATETIME2 NOT NULL
    )
END

DECLARE @HeaderId INT;
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'Clock';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'Clock');
    SET @HeaderId = SCOPE_IDENTITY();
END

DECLARE @ObjectId INT;
SELECT @ObjectId = Id FROM DLMSObject WHERE HeaderId = @HeaderId AND Name = 'Clock';

IF @ObjectId IS NULL
BEGIN
    INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
    VALUES (@HeaderId, 'Clock', '0.0.1.0.0.255', 'Clock');
    SET @ObjectId = SCOPE_IDENTITY();
END

-- Seed DLMS parameters (Attributes 2 to 9) for Clock
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 2)
BEGIN
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 2, 'Time', 'OctetString', 'R/W');
END
ELSE
BEGIN
    UPDATE ObjectParameter SET Name = 'Time' WHERE ObjectId = @ObjectId AND AttributeId = 2;
END

IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 3)
BEGIN
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 3, 'Time Zone', 'Integer16', 'R/W');
END

IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 4)
BEGIN
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 4, 'Status', 'Unsigned', 'R/W');
END

IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 5)
BEGIN
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 5, 'Daylight Savings Begin', 'OctetString', 'R/W');
END

IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 6)
BEGIN
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 6, 'Daylight Savings End', 'OctetString', 'R/W');
END

IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 7)
BEGIN
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 7, 'Daylight Savings Deviation', 'Integer8', 'R/W');
END

IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 8)
BEGIN
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 8, 'Daylight Savings Enabled', 'Boolean', 'R/W');
END

IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 9)
BEGIN
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 9, 'Clock Base', 'Enum', 'R/W');
END
