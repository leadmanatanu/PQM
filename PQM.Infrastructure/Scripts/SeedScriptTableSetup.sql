IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ScriptTable' AND xtype='U')
BEGIN
    CREATE TABLE [ScriptTable] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [DeviceId] INT NOT NULL,
        [Name] NVARCHAR(MAX) NULL,
        [ObjectType] NVARCHAR(MAX) NULL,
        [Value] NVARCHAR(MAX) NULL,
        [DateEntered] DATETIME2 NOT NULL
    )
END

DECLARE @HeaderId INT;
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'ScriptTable';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'ScriptTable');
    SET @HeaderId = SCOPE_IDENTITY();
END

DECLARE @ObjectId INT;
SELECT @ObjectId = Id FROM DLMSObject WHERE HeaderId = @HeaderId AND Name = 'Script Table';

IF @ObjectId IS NULL
BEGIN
    INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
    VALUES (@HeaderId, 'Script Table', '0.0.10.0.1.255', 'ScriptTable');
    SET @ObjectId = SCOPE_IDENTITY();
END

IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 2)
BEGIN
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 2, 'Scripts', 'OctetString', 'R/W');
END
