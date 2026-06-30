-- Create ActionSchedule table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ActionSchedule' AND xtype='U')
BEGIN
    CREATE TABLE [ActionSchedule] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [DeviceId] INT NOT NULL,
        [Name] NVARCHAR(MAX) NULL,
        [ObjectType] NVARCHAR(MAX) NULL,
        [Value] NVARCHAR(MAX) NULL,
        [DateEntered] DATETIME2 NOT NULL
    )
END

-- Seed ConnectedHeader
DECLARE @HeaderId INT;
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'ActionSchedule';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'ActionSchedule');
    SET @HeaderId = SCOPE_IDENTITY();
END

-- Seed DLMSObject
DECLARE @ObjectId INT;
SELECT @ObjectId = Id FROM DLMSObject
WHERE HeaderId = @HeaderId AND ObisCode = '0.0.15.0.0.255' AND ObjectType = 'ActionSchedule';

IF @ObjectId IS NULL
BEGIN
    INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
    VALUES (@HeaderId, 'Action Schedule', '0.0.15.0.0.255', 'ActionSchedule');
    SET @ObjectId = SCOPE_IDENTITY();
END

-- Attribute 2: Executed Script
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 2)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 2, 'Executed Script', 'Structure', 'R');

-- Attribute 3: Type
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 3)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 3, 'Type', 'Enum', 'R');

-- Attribute 4: Execution Time
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 4)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 4, 'Execution Time', 'Array', 'R');
