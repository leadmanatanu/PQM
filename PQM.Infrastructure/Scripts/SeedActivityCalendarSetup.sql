-- Create ActivityCalendar table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ActivityCalendar' AND xtype='U')
BEGIN
    CREATE TABLE [ActivityCalendar] (
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
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'ActivityCalendar';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'ActivityCalendar');
    SET @HeaderId = SCOPE_IDENTITY();
END

-- Seed DLMSObject
DECLARE @ObjectId INT;
SELECT @ObjectId = Id FROM DLMSObject
WHERE HeaderId = @HeaderId AND ObisCode = '0.0.13.0.0.255' AND ObjectType = 'ActivityCalendar';

IF @ObjectId IS NULL
BEGIN
    INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
    VALUES (@HeaderId, 'Activity Calendar', '0.0.13.0.0.255', 'ActivityCalendar');
    SET @ObjectId = SCOPE_IDENTITY();
END

-- Attribute 2: Calendar Name Active
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 2)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 2, 'Calendar Name Active', 'OctetString', 'R');

-- Attribute 3: Season Profile Active
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 3)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 3, 'Season Profile Active', 'Array', 'R');

-- Attribute 4: Week Profile Active
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 4)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 4, 'Week Profile Active', 'Array', 'R');

-- Attribute 5: Day Profile Active
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 5)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 5, 'Day Profile Active', 'Array', 'R');

-- Attribute 6: Calendar Name Passive
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 6)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 6, 'Calendar Name Passive', 'OctetString', 'R');

-- Attribute 7: Season Profile Passive
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 7)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 7, 'Season Profile Passive', 'Array', 'R');

-- Attribute 8: Week Profile Passive
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 8)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 8, 'Week Profile Passive', 'Array', 'R');

-- Attribute 9: Day Profile Passive
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 9)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 9, 'Day Profile Passive', 'Array', 'R');

-- Attribute 10: Activate Passive Calendar Time
IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 10)
    INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
    VALUES (@ObjectId, 10, 'Activate Passive Calendar Time', 'DateTime', 'R');
