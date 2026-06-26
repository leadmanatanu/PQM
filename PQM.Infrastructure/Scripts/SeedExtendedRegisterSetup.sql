-- Seed ConnectedHeader for ExtendedRegister
DECLARE @HeaderId INT;
SELECT @HeaderId = Id FROM ConnectedHeader WHERE DeviceId = 1 AND Name = 'ExtendedRegister';
IF @HeaderId IS NULL
BEGIN
    INSERT INTO ConnectedHeader (DeviceId, Name) VALUES (1, 'ExtendedRegister');
    SET @HeaderId = SCOPE_IDENTITY();
END

-- Seed DLMS Objects and their parameters
IF OBJECT_ID('tempdb..#ERSeed') IS NOT NULL DROP TABLE #ERSeed;
CREATE TABLE #ERSeed (
    Name NVARCHAR(200),
    ObisCode NVARCHAR(50)
);

INSERT INTO #ERSeed (Name, ObisCode) VALUES
('MD-W(Imp)',  '1.0.1.6.0.255'),
('MD-VA(Imp)', '1.0.9.6.0.255');

DECLARE @Name NVARCHAR(200), @ObisCode NVARCHAR(50), @ObjectId INT;

DECLARE ERCursor CURSOR FOR
SELECT Name, ObisCode FROM #ERSeed;

OPEN ERCursor;
FETCH NEXT FROM ERCursor INTO @Name, @ObisCode;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Find or create DLMSObject
    SET @ObjectId = NULL;
    SELECT @ObjectId = Id FROM DLMSObject
    WHERE HeaderId = @HeaderId AND ObisCode = @ObisCode AND ObjectType = 'ExtendedRegister';

    IF @ObjectId IS NULL
    BEGIN
        INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
        VALUES (@HeaderId, @Name, @ObisCode, 'ExtendedRegister');
        SET @ObjectId = SCOPE_IDENTITY();
    END

    -- Attribute 2: Value
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 2)
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, 2, 'Value', 'Float64', 'R');

    -- Attribute 3: Scaler + Unit
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 3)
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, 3, 'Scaler + Unit', 'Structure', 'R');

    -- Attribute 4: Status
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 4)
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, 4, 'Status', 'OctetString', 'R');

    -- Attribute 5: Capture Time
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 5)
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, 5, 'Capture Time', 'DateTime', 'R');

    FETCH NEXT FROM ERCursor INTO @Name, @ObisCode;
END

CLOSE ERCursor;
DEALLOCATE ERCursor;
DROP TABLE #ERSeed;
