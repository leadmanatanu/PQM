-- Create a temp table to hold seed data
IF OBJECT_ID('tempdb..#SeedData') IS NOT NULL DROP TABLE #SeedData;

CREATE TABLE #SeedData (
    RowId INT IDENTITY(1,1) PRIMARY KEY,
    Name NVARCHAR(150),
    ObisCode NVARCHAR(100),
    ObjectType NVARCHAR(100)
);

INSERT INTO #SeedData (Name, ObisCode, ObjectType) VALUES
('Cum. Billing Count', '0.0.0.1.0.255', 'Data'),
('Available Billing Periods', '0.0.0.1.1.255', 'Data'),
('COSEM Logical Device Name', '0.0.42.0.0.255', 'Data'),
('Cumulative Tamper Count', '0.0.94.91.0.255', 'Data'),
('Meter Type', '0.0.94.91.9.255', 'Data'),
('Meter Category', '0.0.94.91.11.255', 'Data'),
('Current Rating', '0.0.94.91.12.255', 'Data'),
('Meter Serial Number', '0.0.96.1.0.255', 'Data'),
('Manufacturer Name', '0.0.96.1.1.255', 'Data'),
('Year Of Manufacture', '0.0.96.1.4.255', 'Data'),
('Cum. Programming Count', '0.0.96.2.0.255', 'Data'),
('No of Power Failures', '0.0.96.7.0.255', 'Data'),
('Event:Voltage Related', '0.0.96.11.0.255', 'Data'),
('Event:Current Related', '0.0.96.11.1.255', 'Data'),
('Event:Power Related', '0.0.96.11.2.255', 'Data'),
('Event:Transaction Related', '0.0.96.11.3.255', 'Data'),
('Event:Others', '0.0.96.11.4.255', 'Data'),
('Manufacturer specific', '0.128.140.128.128.255', 'Data'),
('Manufacturer specific', '0.128.141.128.128.255', 'Data'),
('Manufacturer specific', '0.128.144.128.128.255', 'Data'),
('Manufacturer specific', '0.128.145.128.128.255', 'Data'),
('Manufacturer specific', '0.128.146.128.128.255', 'Data'),
('Manufacturer specific', '0.128.147.128.128.255', 'Data'),
('Manufacturer specific', '0.128.150.128.128.255', 'Data'),
('Manufacturer specific', '0.128.152.128.128.255', 'Data'),
('Manufacturer specific', '0.128.153.128.128.255', 'Data'),
('Manufacturer specific', '0.128.154.128.128.255', 'Data'),
('Manufacturer specific', '0.128.155.0.128.255', 'Data'),
('Manufacturer specific', '0.128.155.1.128.255', 'Data'),
('Manufacturer specific', '0.128.156.0.128.255', 'Data'),
('Manufacturer specific', '0.128.156.1.128.255', 'Data'),
('Manufacturer specific', '0.128.156.2.128.255', 'Data'),
('Manufacturer specific', '0.128.156.3.128.255', 'Data'),
('Manufacturer specific', '0.128.156.4.128.255', 'Data'),
('Manufacturer specific', '0.128.156.5.128.255', 'Data'),
('Manufacturer specific', '0.128.162.0.128.255', 'Data'),
('Manufacturer specific', '0.128.162.1.128.255', 'Data'),
('Manufacturer specific', '0.128.164.128.128.255', 'Data'),
('Manufacturer specific', '0.128.165.128.128.255', 'Data'),
('Firmware Version For Meter', '1.0.0.2.0.255', 'Data'),
('CTR', '1.0.0.4.2.255', 'Data'),
('PTR', '1.0.0.4.3.255', 'Data'),
('Demand Integration Period', '1.0.0.8.0.255', 'Data'),
('Profile Capture Period', '1.0.0.8.4.255', 'Data'),
('Ch. 0 Recording interval 2, for load profile', '1.0.0.8.5.255', 'Data');

DECLARE @HeaderId INT = 1;
DECLARE @TotalRows INT = (SELECT COUNT(*) FROM #SeedData);
DECLARE @CurrentRow INT = 1;

DECLARE @Name NVARCHAR(150), @ObisCode NVARCHAR(100), @ObjectType NVARCHAR(100);
DECLARE @ObjectId INT;

WHILE @CurrentRow <= @TotalRows
BEGIN
    SET @ObjectId = NULL;
    
    SELECT @Name = Name, @ObisCode = ObisCode, @ObjectType = ObjectType 
    FROM #SeedData 
    WHERE RowId = @CurrentRow;

    -- Check if DLMSObject exists
    SELECT @ObjectId = Id FROM DLMSObject WHERE HeaderId = @HeaderId AND ObisCode = @ObisCode;
    
    IF @ObjectId IS NULL
    BEGIN
        INSERT INTO DLMSObject (HeaderId, Name, ObisCode, ObjectType)
        VALUES (@HeaderId, @Name, @ObisCode, @ObjectType);
        
        SET @ObjectId = SCOPE_IDENTITY();
    END
    
    -- Insert Attribute 2 ObjectParameter if not exists
    IF NOT EXISTS (SELECT 1 FROM ObjectParameter WHERE ObjectId = @ObjectId AND AttributeId = 2)
    BEGIN
        INSERT INTO ObjectParameter (ObjectId, AttributeId, Name, DataType, AccessType)
        VALUES (@ObjectId, 2, 'Value', 'Double', 'R/W');
    END

    SET @CurrentRow = @CurrentRow + 1;
END

DROP TABLE #SeedData;
