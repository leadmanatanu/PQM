-- Create ProfileGeneric table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='ProfileGeneric' AND xtype='U')
BEGIN
    CREATE TABLE [ProfileGeneric] (
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
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'ProfileGeneric';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'ProfileGeneric');
    SET @HeaderId = SCOPE_IDENTITY();
END

-- Seed DLMS Objects and their parameters
IF OBJECT_ID('tempdb..#PGSeed') IS NOT NULL DROP TABLE #PGSeed;
CREATE TABLE #PGSeed (
    Name NVARCHAR(200),
    ObisCode NVARCHAR(50)
);

INSERT INTO #PGSeed (Name, ObisCode) VALUES
('Profile Generic (0.0.94.91.10.255)', '0.0.94.91.10.255'),
('Event Log 1',                         '0.0.99.98.0.255'),
('Event Log 2',                         '0.0.99.98.1.255'),
('Event Log 3',                         '0.0.99.98.2.255'),
('Event Log 4',                         '0.0.99.98.3.255'),
('Event Log 5',                         '0.0.99.98.4.255'),
('Profile Generic (0.128.187.0.128.255)','0.128.187.0.128.255'),
('Load Profile 1',                       '1.0.94.91.0.255'),
('Load Profile 2',                       '1.0.94.91.3.255'),
('Load Profile 3',                       '1.0.94.91.4.255'),
('Load Profile 4',                       '1.0.94.91.5.255'),
('Load Profile 5',                       '1.0.94.91.6.255'),
('Load Profile 6',                       '1.0.94.91.7.255'),
('Billing Profile',                      '1.0.98.1.0.255'),
('Load Profile 7',                       '1.0.99.1.0.255'),
('Load Profile 8',                       '1.0.99.2.0.255'),
('Profile Generic (1.0.128.7.90.255)',   '1.0.128.7.90.255');

DECLARE @Name NVARCHAR(200), @ObisCode NVARCHAR(50), @ObjectId INT;

DECLARE PGCursor CURSOR FOR
SELECT Name, ObisCode FROM #PGSeed;

OPEN PGCursor;
FETCH NEXT FROM PGCursor INTO @Name, @ObisCode;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Find or create DLMSObject
    SET @ObjectId = NULL;
    SELECT @ObjectId = Id FROM DLMSObject
    WHERE HeaderId = @HeaderId AND ObisCode = @ObisCode AND ObjectType = 'ProfileGeneric';

    IF @ObjectId IS NULL
    BEGIN
        INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
        VALUES (@HeaderId, @Name, @ObisCode, 'ProfileGeneric');
        SET @ObjectId = SCOPE_IDENTITY();
    END

    -- Attribute 2: Buffer
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 2)
    BEGIN
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, 2, 'Buffer', 'Array', 'R');
    END

    -- Attribute 3: Capture Objects
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 3)
    BEGIN
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, 3, 'Capture Objects', 'Array', 'R');
    END

    FETCH NEXT FROM PGCursor INTO @Name, @ObisCode;
END

CLOSE PGCursor;
DEALLOCATE PGCursor;
DROP TABLE #PGSeed;
