USE RalseiWarehouse;
GO

SET NOCOUNT ON;

DECLARE @i INT;

/* =====================================================
   UNIT
===================================================== */
SET @i = 1;

WHILE @i <= 100
BEGIN
    INSERT INTO Unit(DisplayName)
    VALUES (N'Unit ' + FORMAT(@i,'000'));

    SET @i += 1;
END

/* =====================================================
   SUPPLIER
===================================================== */
SET @i = 1;

WHILE @i <= 100
BEGIN
    INSERT INTO Supplier
    (
        DisplayName,
        Address,
        Phone,
        Email,
        MoreInfo,
        ContractDate
    )
    VALUES
    (
        N'Supplier ' + FORMAT(@i,'000'),
        N'Address ' + CAST(@i AS NVARCHAR),
        '090' + RIGHT('0000000' + CAST(@i AS VARCHAR),7),
        'supplier' + CAST(@i AS VARCHAR) + '@mail.com',
        N'Fake Supplier',
        DATEADD(DAY,-@i,GETDATE())
    );

    SET @i += 1;
END

/* =====================================================
   CUSTOMER
===================================================== */
SET @i = 1;

WHILE @i <= 100
BEGIN
    INSERT INTO Customer
    (
        DisplayName,
        Address,
        Phone,
        Email,
        MoreInfo,
        ContractDate
    )
    VALUES
    (
        N'Customer ' + FORMAT(@i,'000'),
        N'Address ' + CAST(@i AS NVARCHAR),
        '091' + RIGHT('0000000' + CAST(@i AS VARCHAR),7),
        'customer' + CAST(@i AS VARCHAR) + '@mail.com',
        N'Fake Customer',
        DATEADD(DAY,-@i*2,GETDATE())
    );

    SET @i += 1;
END

/* =====================================================
   ROLE
===================================================== */
SET @i = 1;

WHILE @i <= 100
BEGIN
    INSERT INTO Role(DisplayName)
    VALUES
    (
        CASE
            WHEN @i=1 THEN N'Admin'
            WHEN @i=2 THEN N'Manager'
            WHEN @i=3 THEN N'Staff'
            ELSE N'Role ' + FORMAT(@i,'000')
        END
    );

    SET @i += 1;
END

/* =====================================================
   USER
===================================================== */
SET @i = 1;

WHILE @i <= 100
BEGIN
    INSERT INTO [User]
    (
        DisplayName,
        UserName,
        Password,
        RoleId
    )
    VALUES
    (
        N'Employee ' + FORMAT(@i,'000'),
        'user' + FORMAT(@i,'000'),
        '123456',
        @i
    );

    SET @i += 1;
END

/* =====================================================
   OBJECT
===================================================== */
SET @i = 1;

WHILE @i <= 100
BEGIN
    INSERT INTO Object
    (
        ObjectId,
        DisplayName,
        UnitId,
        SupplierId,
        QRCode,
        BarCode
    )
    VALUES
    (
        'OBJ-' + FORMAT(@i,'000'),
        N'Product ' + FORMAT(@i,'000'),
        @i,
        @i,
        'QR-' + FORMAT(@i,'000'),
        '893' + FORMAT(@i,'000000000')
    );

    SET @i += 1;
END

/* =====================================================
   INPUT
===================================================== */
SET @i = 1;

WHILE @i <= 100
BEGIN
    INSERT INTO Input
    (
        InputId,
        DateInput
    )
    VALUES
    (
        'IN-' + FORMAT(@i,'000'),
        DATEADD(DAY,-ABS(CHECKSUM(NEWID()))%365,GETDATE())
    );

    SET @i += 1;
END

/* =====================================================
   INPUT INFO
===================================================== */
SET @i = 1;

WHILE @i <= 100
BEGIN
    INSERT INTO InputInfo
    (
        Id,
        ObjectId,
        InputId,
        Count,
        InputPrice,
        OutputPrice,
        Status
    )
    VALUES
    (
        'II-' + FORMAT(@i,'000'),
        'OBJ-' + FORMAT(@i,'000'),
        'IN-' + FORMAT(@i,'000'),
        ABS(CHECKSUM(NEWID()))%100+1,
        ABS(CHECKSUM(NEWID()))%500000+50000,
        ABS(CHECKSUM(NEWID()))%700000+70000,
        'Completed'
    );

    SET @i += 1;
END

/* =====================================================
   OUTPUT
===================================================== */
SET @i = 1;

WHILE @i <= 100
BEGIN
    INSERT INTO Output
    (
        OutputId,
        DateOutput
    )
    VALUES
    (
        'OUT-' + FORMAT(@i,'000'),
        DATEADD(DAY,-ABS(CHECKSUM(NEWID()))%180,GETDATE())
    );

    SET @i += 1;
END

/* =====================================================
   OUTPUT INFO
===================================================== */
SET @i = 1;

WHILE @i <= 100
BEGIN
    INSERT INTO OutputInfo
    (
        Id,
        ObjectId,
        OutputId,
        CustomerId,
        Count,
        Status
    )
    VALUES
    (
        'OI-' + FORMAT(@i,'000'),
        'OBJ-' + FORMAT(@i,'000'),
        'OUT-' + FORMAT(@i,'000'),
        @i,
        ABS(CHECKSUM(NEWID()))%20+1,
        'Completed'
    );

    SET @i += 1;
END

PRINT 'Fake data inserted successfully.';