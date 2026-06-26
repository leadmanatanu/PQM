IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MacAddressSetup' AND xtype='U')
BEGIN
    CREATE TABLE [MacAddressSetup] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [DeviceId] INT NOT NULL,
        [Name] NVARCHAR(MAX) NULL,
        [ObjectType] NVARCHAR(MAX) NULL,
        [Value] NVARCHAR(MAX) NULL,
        [DateEntered] DATETIME2 NOT NULL
    )
END

DECLARE @HeaderId INT;
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'MacAddressSetup';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'MacAddressSetup');
    SET @HeaderId = SCOPE_IDENTITY();
END

-- Seed DLMSObject and ObjectParameter for MAC Address (AttributeId = 2)
DECLARE @ObjectId INT;
SELECT @ObjectId = Id FROM DLMSObject WHERE HeaderId = @HeaderId AND Name = 'MAC Address';

IF @ObjectId IS NULL
BEGIN
    INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
    VALUES (@HeaderId, 'MAC Address', '0.0.25.2.0.255', 'MacAddressSetup');
    SET @ObjectId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 2)
BEGIN
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 2, 'MAC Address', 'OctetString', 'R/W');
END
