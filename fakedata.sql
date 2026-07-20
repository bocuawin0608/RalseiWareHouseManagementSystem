USE RalseiWarehouse;
GO

SET NOCOUNT ON;
PRINT N'=== BẮT ĐẦU SEED DỮ LIỆU MẪU CHO HỆ THỐNG QUẢN LÝ KHO ===';

-- ============================================================================
-- CLEANUP: XÓA DỮ LIỆU THEO ĐÚNG THỨ TỰ RÀNG BUỘC KHÓA NGOẠI
-- ============================================================================
PRINT N'-> Đang dọn dẹp dữ liệu cũ...';
DELETE FROM [OutputInfo];
DELETE FROM [InputInfo];
DELETE FROM [Output];
DELETE FROM [Input];
DELETE FROM [Object];
DELETE FROM [User];
DELETE FROM [Role];
DELETE FROM [Customer];
DELETE FROM [Supplier];
DELETE FROM [Unit];

-- RESET IDENTITY COLUMNS
PRINT N'-> Đang reset bộ đếm Identity...';
DBCC CHECKIDENT ('Unit', RESEED, 0);
DBCC CHECKIDENT ('Supplier', RESEED, 0);
DBCC CHECKIDENT ('Customer', RESEED, 0);
DBCC CHECKIDENT ('Role', RESEED, 0);
DBCC CHECKIDENT ('User', RESEED, 0);

-- ============================================================================
-- LEVEL 1: ROLES
-- ============================================================================
PRINT N'-> Đang nạp vai trò hệ thống...';

INSERT INTO [Role] (DisplayName) VALUES
(N'Admin'),
(N'Manager'),
(N'Staff'),
(N'Warehouse Keeper');

-- ============================================================================
-- LEVEL 2: UNITS OF MEASURE
-- ============================================================================
PRINT N'-> Đang nạp đơn vị tính...';

INSERT INTO [Unit] (DisplayName) VALUES
(N'Cái'),
(N'Hộp'),
(N'Thùng'),
(N'Chai'),
(N'Lon'),
(N'Gói'),
(N'Bao'),
(N'Kg'),
(N'Lít'),
(N'Túi'),
(N'Chục'),
(N'Cuộn');

-- ============================================================================
-- LEVEL 3: SUPPLIERS
-- ============================================================================
PRINT N'-> Đang nạp nhà cung cấp...';

INSERT INTO [Supplier] (DisplayName, Address, Phone, Email, MoreInfo, ContractDate) VALUES
(N'Công ty TNHH Thực phẩm ABC',        N'123 Đường Lê Lợi, Quận 1, TP.HCM',        '0901234567', 'contact@abcfood.vn',        N'Chuyên cung cấp thực phẩm khô, đồ hộp',          '2024-01-15'),
(N'Tập đoàn Vinamilk',                  N'10 Tân Trào, Quận 7, TP.HCM',              '0902345678', 'sales@vinamilk.com.vn',      N'Sữa và các sản phẩm từ sữa',                     '2024-02-01'),
(N'Công ty CP Nước giải khát Suntory',  N'Lô A1, KCN Mỹ Phước, Bình Dương',          '0903456789', 'order@suntorypepsico.vn',    N'Nước ngọt, nước suối, trà đóng chai',            '2024-01-20'),
(N'Công ty TNHH Acecook Việt Nam',      N'Đường số 7, KCN Tân Bình, TP.HCM',        '0904567890', 'sales@acecookvietnam.com',   N'Mì gói, phở gói, hủ tiếu gói',                   '2024-03-10'),
(N'Công ty CP Bia Sài Gòn Sabeco',      N'187 Nguyễn Chí Thanh, Quận 5, TP.HCM',    '0905678901', 'order@sabeco.com.vn',        N'Bia các loại, nước giải khát có ga',             '2024-01-05'),
(N'Công ty TNHH Unilever Việt Nam',     N'156B Nguyễn Lương Bằng, Quận 7, TP.HCM',  '0906789012', 'vietnam@unilever.com',       N'Dầu gội, xà phòng, nước rửa chén, kem đánh răng', '2024-02-15'),
(N'Công ty CP Masan Consumer',          N'Lầu 12, MPlaza, 39 Lê Duẩn, Quận 1, TP.HCM', '0907890123', 'consumer@masan.com.vn',    N'Nước mắm, nước tương, dầu ăn, gia vị',           '2024-01-25'),
(N'Công ty TNHH Nestlé Việt Nam',       N'Lầu 5, Empress Tower, 138-142 Hai Bà Trưng, Quận 1, TP.HCM', '0908901234', 'vietnam@nestle.com', N'Cà phê, sữa đặc, bột dinh dưỡng, kẹo',        '2024-03-01'),
(N'Công ty CP Hóa chất Đức Giang',      N'18 Đường Đức Giang, Long Biên, Hà Nội',   '0909012345', 'sales@ducgiang.vn',          N'Bột giặt, nước tẩy rửa, hóa chất gia dụng',      '2024-02-20'),
(N'Công ty TNHH Panasonic Việt Nam',    N'Lô J1-J2, KCN Thăng Long, Hà Nội',        '0900123456', 'sales@panasonic.vn',         N'Pin, bóng đèn, thiết bị điện gia dụng',          '2024-01-30');

-- ============================================================================
-- LEVEL 4: CUSTOMERS
-- ============================================================================
PRINT N'-> Đang nạp khách hàng...';

INSERT INTO [Customer] (DisplayName, Address, Phone, Email, MoreInfo, ContractDate) VALUES
(N'Siêu thị Co.opmart Sài Gòn',         N'189C Cống Quỳnh, Quận 1, TP.HCM',          '0911234567', 'purchasing@coopmart.vn',      N'Chuỗi siêu thị lớn nhất miền Nam',              '2024-01-10'),
(N'VinMart+',                           N'72 Lê Thánh Tôn, Quận 1, TP.HCM',            '0912345678', 'order@vinmart.vn',            N'Chuỗi cửa hàng tiện lợi VinGroup',              '2024-02-05'),
(N'Bách Hóa Xanh',                      N'268 Tô Hiến Thành, Quận 10, TP.HCM',         '0913456789', 'supply@bachhoaxanh.com',      N'Chuỗi bách hóa giá rẻ',                         '2024-01-20'),
(N'Circle K Việt Nam',                  N'Lầu 3, 235 Đồng Khởi, Quận 1, TP.HCM',     '0914567890', 'vietnam@circlek.com',         N'Cửa hàng tiện lợi quốc tế',                     '2024-03-01'),
(N'FamilyMart Việt Nam',                N'Lầu 7, 102 Nguyễn Huệ, Quận 1, TP.HCM',    '0915678901', 'vn@familymart.com.vn',        N'Chuỗi cửa hàng tiện lợi Nhật Bản',              '2024-02-10'),
(N'Siêu thị Big C Miền Đông',           N'268 Tô Hiến Thành, Quận 10, TP.HCM',       '0916789012', 'bigc.miendong@bigc.vn',       N'Hypermarket lớn khu vực miền Đông',             '2024-01-15'),
(N'Chợ đầu mối Bình Điền',              N'Đường Võ Văn Kiệt, Bình Chánh, TP.HCM',    '0917890123', 'info@chobinhdien.vn',         N'Chợ đầu mối nông sản lớn nhất TP.HCM',          '2024-03-05'),
(N'Lotte Mart Việt Nam',                N'469 Nguyễn Hữu Thọ, Quận 7, TP.HCM',       '0918901234', 'vn@lottemart.com',            N'Siêu thị Hàn Quốc',                              '2024-02-25'),
(N'Aeon Mall Long Biên',                N'27 Cổ Linh, Long Biên, Hà Nội',            '0919012345', 'longbien@aeon.com.vn',        N'Trung tâm thương mại Nhật Bản',                 '2024-01-30'),
(N'GO! Supermarket',                    N'99 Nguyễn Văn Linh, Quận 7, TP.HCM',       '0910123456', 'go@centralgroup.vn',          N'Siêu thị Central Group',                         '2024-03-10');

-- ============================================================================
-- LEVEL 5: USERS
-- ============================================================================
PRINT N'-> Đang nạp tài khoản ngườidùng...';

INSERT INTO [User] (DisplayName, UserName, Password, RoleId) VALUES
(N'Nguyễn Văn Admin',     'admin',       'admin',       1),
(N'Trần Thị Manager',     'manager',     'manager',     2),
(N'Lê Văn Nhân Viên',     'staff',       'staff',       3),
(N'Phạm Thị Thủ Kho',     'keeper',      'keeper',      4),
(N'Hoàng Văn Kiểm Tra',   'inspector',   'inspector',   3);

-- ============================================================================
-- LEVEL 6: PRODUCTS (Objects)
-- ============================================================================
PRINT N'-> Đang nạp sản phẩm...';

-- Thực phẩm khô & đồ hộp (Supplier 1 - ABC Food)
INSERT INTO [Object] (Id, DisplayName, UnitId, SupplierId, QRCode, BarCode) VALUES
('P0001',  N'Gạo ST25 túi 5kg',                    7,    1,    'QR-ST25-5KG',       '8934567890001'),
('P0002',  N'Gạo Jasmine túi 10kg',                7,    1,    'QR-JAS-10KG',       '8934567890002'),
('P0003',  N'Nước mắm Nam Ngư 750ml',              4,    7,    'QR-NAMNGU-750',     '8934567890003'),
('P0004',  N'Dầu ăn Neptune 1L',                   9,    7,    'QR-NEPTUNE-1L',     '8934567890004'),
('P0005',  N'Mì gói Hảo Hảo tôm chua cay',          6,    4,    'QR-HAOHAO-TCC',     '8934567890005'),
('P0006',  N'Phở gói Vifon bò',                     6,    4,    'QR-VIFON-BO',       '8934567890006'),
('P0007',  N'Nước tương Maggi 300ml',              4,    7,    'QR-MAGGI-300',      '8934567890007'),
('P0008',  N'Đường trắng Biên Hòa 1kg',            8,    7,    'QR-DUONG-BH1',      '8934567890008'),
('P0009',  N'Muối iốt 500g',                        6,    7,    'QR-MUOI-500',       '8934567890009'),
('P0010',  N'Cà phê G7 3in1 hộp 20 gói',           2,    8,    'QR-G7-20P',         '8934567890010');

-- Sữa & đồ uống (Supplier 2 - Vinamilk)
INSERT INTO [Object] (Id, DisplayName, UnitId, SupplierId, QRCode, BarCode) VALUES
('P0011',  N'Sữa tươi Vinamilk không đường 1L',    4,    2,    'QR-VNM-1L-KD',      '8934567890011'),
('P0012',  N'Sữa tươi Vinamilk có đường 1L',       4,    2,    'QR-VNM-1L-CD',      '8934567890012'),
('P0013',  N'Sữa chua Vinamilk hộp 4x100g',         2,    2,    'QR-VNM-SC-4X100',   '8934567890013'),
('P0014',  N'Sữa đặc Ông Thọ 380g',                5,    2,    'QR-ONGTHO-380',     '8934567890014'),
('P0015',  N'Sữa Ensure Gold 850g',                2,    2,    'QR-ENSURE-850',     '8934567890015');

-- Nước giải khát (Supplier 3 - Suntory)
INSERT INTO [Object] (Id, DisplayName, UnitId, SupplierId, QRCode, BarCode) VALUES
('P0016',  N'Nước suối Aquafina 500ml',            4,    3,    'QR-AQUAFINA-500',   '8934567890016'),
('P0017',  N'Pepsi lon 330ml',                      5,    3,    'QR-PEPSI-330',      '8934567890017'),
('P0018',  N'7Up lon 330ml',                        5,    3,    'QR-7UP-330',        '8934567890018'),
('P0019',  N'Sting vàng chai 330ml',                4,    3,    'QR-STING-330',      '8934567890019'),
('P0020',  N'Trà xanh không độ chai 455ml',         4,    3,    'QR-TRAXANH-455',    '8934567890020');

-- Bia (Supplier 5 - Sabeco)
INSERT INTO [Object] (Id, DisplayName, UnitId, SupplierId, QRCode, BarCode) VALUES
('P0021',  N'Bia Saigon Special thùng 24 lon',      3,    5,    'QR-SGS-24LON',      '8934567890021'),
('P0022',  N'Bia Saigon Lager thùng 24 chai',       3,    5,    'QR-SGL-24CHAI',     '8934567890022'),
('P0023',  N'Bia 333 thùng 24 lon',                 3,    5,    'QR-333-24LON',      '8934567890023'),
('P0024',  N'Bia Tiger thùng 24 lon',               3,    5,    'QR-TIGER-24LON',    '8934567890024'),
('P0025',  N'Bia Heineken thùng 24 lon',            3,    5,    'QR-HEINEKEN-24',    '8934567890025');

-- Hóa mỹ phẩm (Supplier 6 - Unilever)
INSERT INTO [Object] (Id, DisplayName, UnitId, SupplierId, QRCode, BarCode) VALUES
('P0026',  N'Dầu gội Clear 650ml',                  4,    6,    'QR-CLEAR-650',      '8934567890026'),
('P0027',  N'Dầu gội Sunsilk 650ml',                4,    6,    'QR-SUNSILK-650',    '8934567890027'),
('P0028',  N'Xà phòng Lifebuoy 90g',                1,    6,    'QR-LIFEBUOY-90',    '8934567890028'),
('P0029',  N'Nước rửa chén Sunlight 750ml',         4,    6,    'QR-SUNLIGHT-750',   '8934567890029'),
('P0030',  N'Kem đánh răng P/S 230g',               1,    6,    'QR-PS-230',         '8934567890030');

-- Hóa chất (Supplier 9 - Đức Giang)
INSERT INTO [Object] (Id, DisplayName, UnitId, SupplierId, QRCode, BarCode) VALUES
('P0031',  N'Bột giặt Omo 3.8kg',                   8,    9,    'QR-OMO-38KG',       '8934567890031'),
('P0032',  N'Bột giặt Ariel 3.6kg',                 8,    9,    'QR-ARIEL-36KG',     '8934567890032'),
('P0033',  N'Nước lau sàn Mr. Muscle 1L',           9,    9,    'QR-MRMUSCLE-1L',    '8934567890033'),
('P0034',  N'Nước tẩy bồn cầu Duck 500ml',          4,    9,    'QR-DUCK-500',       '8934567890034'),
('P0035',  N'Nước xả Downy 1.5L',                   9,    9,    'QR-DOWNY-15L',      '8934567890035');

-- Điện tử (Supplier 10 - Panasonic)
INSERT INTO [Object] (Id, DisplayName, UnitId, SupplierId, QRCode, BarCode) VALUES
('P0036',  N'Pin Panasonic AA vỉ 2 viên',           2,    10,   'QR-PANA-AA2',       '8934567890036'),
('P0037',  N'Pin Panasonic AAA vỉ 2 viên',          2,    10,   'QR-PANA-AAA2',      '8934567890037'),
('P0038',  N'Bóng đèn LED Panasonic 9W',            1,    10,   'QR-LED-PANA-9W',    '8934567890038'),
('P0039',  N'Ổ cắm điện Panasonic 3 lỗ',            1,    10,   'QR-OCAM-3LO',       '8934567890039'),
('P0040',  N'Dây điện Panasonic 5m',               1,    10,   'QR-DAYDIEN-5M',     '8934567890040');

-- Đồ uống có ga & snack (Supplier 1 & 3)
INSERT INTO [Object] (Id, DisplayName, UnitId, SupplierId, QRCode, BarCode) VALUES
('P0041',  N'Coca Cola lon 330ml',                  5,    3,    'QR-COCA-330',       '8934567890041'),
('P0042',  N'Sprite lon 330ml',                     5,    3,    'QR-SPRITE-330',     '8934567890042'),
('P0043',  N'Fanta cam lon 330ml',                  5,    3,    'QR-FANTA-330',      '8934567890043'),
('P0044',  N'Khoai tây chiên Lay''s 85g',            6,    1,    'QR-LAYS-85',        '8934567890044'),
('P0045',  N'Bánh Oreo 133g',                        2,    1,    'QR-OREO-133',       '8934567890045'),
('P0046',  N'Bánh Choco-Pie hộp 12 bánh',           2,    1,    'QR-CHOCO-12B',      '8934567890046'),
('P0047',  N'Bánh gạo One One 150g',                 6,    1,    'QR-ONEONE-150',     '8934567890047'),
('P0048',  N'Kẹo Alpenliebe gói 40 viên',            6,    8,    'QR-ALPEN-40V',      '8934567890048'),
('P0049',  N'Kẹo Mentos gói 11 viên',                6,    8,    'QR-MENTOS-11V',     '8934567890049'),
('P0050',  N'Sô-cô-la KitKat 4 fingers',             1,    8,    'QR-KITKAT-4F',      '8934567890050');

-- ============================================================================
-- LEVEL 7: IMPORT RECEIPTS (Stock In)
-- ============================================================================
PRINT N'-> Đang nạp phiếu nhập kho...';

DECLARE @InputCounter INT = 1;
DECLARE @InputDate DATETIME = '2025-01-05';

WHILE @InputCounter <= 15
BEGIN
    DECLARE @InputId VARCHAR(128) = 'IN-' + FORMAT(@InputDate, 'yyyyMMdd') + '-' + RIGHT('000' + CAST(@InputCounter AS VARCHAR(3)), 3);
    INSERT INTO [Input] (InputId, DateInput) VALUES (@InputId, @InputDate);
    SET @InputDate = DATEADD(day, 2, @InputDate);
    SET @InputCounter = @InputCounter + 1;
END;

-- ============================================================================
-- LEVEL 8: IMPORT LINE ITEMS
-- ============================================================================
PRINT N'-> Đang nạp chi tiết phiếu nhập...';

DECLARE @InfoId INT = 1;
DECLARE @ProductTable TABLE (RowIdx INT IDENTITY(1,1), ProductId VARCHAR(128));
INSERT INTO @ProductTable (ProductId) SELECT Id FROM [Object] ORDER BY Id;
DECLARE @TotalProducts INT = (SELECT COUNT(*) FROM @ProductTable);

DECLARE @InputTable TABLE (RowIdx INT IDENTITY(1,1), InputId VARCHAR(128));
INSERT INTO @InputTable (InputId) SELECT InputId FROM [Input] ORDER BY InputId;
DECLARE @TotalInputs INT = (SELECT COUNT(*) FROM @InputTable);

DECLARE @i INT = 1;
DECLARE @j INT;
DECLARE @TargetProductId VARCHAR(128);
DECLARE @TargetInputId VARCHAR(128);
DECLARE @Qty INT;
DECLARE @InPrice FLOAT;
DECLARE @OutPrice FLOAT;

WHILE @i <= @TotalInputs
BEGIN
    SELECT @TargetInputId = InputId FROM @InputTable WHERE RowIdx = @i;
    SET @j = 1;
    WHILE @j <= (3 + (@i % 4))
    BEGIN
        SELECT @TargetProductId = ProductId FROM @ProductTable WHERE RowIdx = (((@i + @j) % @TotalProducts) + 1);
        SET @Qty = (20 + (@i * 7) + (@j * 13)) % 100 + 10;
        SET @InPrice = 10000 + ((@i * 17 + @j * 23) % 200) * 1000;
        SET @OutPrice = @InPrice * 1.3;

        INSERT INTO [InputInfo] (Id, ObjectId, InputId, Count, InputPrice, OutputPrice, Status)
        VALUES (NEWID(), @TargetProductId, @TargetInputId, @Qty, @InPrice, @OutPrice, 'Completed');

        SET @j = @j + 1;
    END;
    SET @i = @i + 1;
END;

-- ============================================================================
-- LEVEL 9: EXPORT RECEIPTS (Stock Out)
-- ============================================================================
PRINT N'-> Đang nạp phiếu xuất kho...';

DECLARE @OutputCounter INT = 1;
DECLARE @OutputDate DATETIME = '2025-01-06';

WHILE @OutputCounter <= 10
BEGIN
    DECLARE @OutputId VARCHAR(128) = 'OUT-' + FORMAT(@OutputDate, 'yyyyMMdd') + '-' + RIGHT('000' + CAST(@OutputCounter AS VARCHAR(3)), 3);
    INSERT INTO [Output] (OutputId, DateOutput) VALUES (@OutputId, @OutputDate);
    SET @OutputDate = DATEADD(day, 3, @OutputDate);
    SET @OutputCounter = @OutputCounter + 1;
END;

-- ============================================================================
-- LEVEL 10: EXPORT LINE ITEMS
-- ============================================================================
PRINT N'-> Đang nạp chi tiết phiếu xuất...';

DECLARE @OutputTable TABLE (RowIdx INT IDENTITY(1,1), OutputId VARCHAR(128));
INSERT INTO @OutputTable (OutputId) SELECT OutputId FROM [Output] ORDER BY OutputId;
DECLARE @TotalOutputs INT = (SELECT COUNT(*) FROM @OutputTable);

DECLARE @CustomerTable TABLE (RowIdx INT IDENTITY(1,1), CustomerId INT);
INSERT INTO @CustomerTable (CustomerId) SELECT CustomerId FROM [Customer] ORDER BY CustomerId;
DECLARE @TotalCustomers INT = (SELECT COUNT(*) FROM @CustomerTable);

DECLARE @oi INT = 1;
DECLARE @oj INT;
DECLARE @TargetOutputId VARCHAR(128);
DECLARE @TargetCustomerId INT;
DECLARE @OutQty INT;

WHILE @oi <= @TotalOutputs
BEGIN
    SELECT @TargetOutputId = OutputId FROM @OutputTable WHERE RowIdx = @oi;
    SELECT @TargetCustomerId = CustomerId FROM @CustomerTable WHERE RowIdx = ((@oi % @TotalCustomers) + 1);

    SET @oj = 1;
    WHILE @oj <= (2 + (@oi % 3))
    BEGIN
        SELECT @TargetProductId = ProductId FROM @ProductTable WHERE RowIdx = (((@oi + @oj + 5) % @TotalProducts) + 1);
        SET @OutQty = (5 + (@oi * 3) + (@oj * 7)) % 50 + 5;

        INSERT INTO [OutputInfo] (Id, ObjectId, OutputId, CustomerId, Count, Status)
        VALUES (NEWID(), @TargetProductId, @TargetOutputId, @TargetCustomerId, @OutQty, 'Completed');

        SET @oj = @oj + 1;
    END;
    SET @oi = @oi + 1;
END;

-- ============================================================================
-- RECHECK QUERY AFTER SEED
-- ============================================================================
PRINT N'';
PRINT N'=== SEED HOÀN TẤT ===';
PRINT N'';

SELECT N'Role' AS [Table], COUNT(*) AS [Count] FROM [Role]
UNION ALL
SELECT N'Unit', COUNT(*) FROM [Unit]
UNION ALL
SELECT N'Supplier', COUNT(*) FROM [Supplier]
UNION ALL
SELECT N'Customer', COUNT(*) FROM [Customer]
UNION ALL
SELECT N'User', COUNT(*) FROM [User]
UNION ALL
SELECT N'Object (Products)', COUNT(*) FROM [Object]
UNION ALL
SELECT N'Input (Receipts)', COUNT(*) FROM [Input]
UNION ALL
SELECT N'InputInfo (Lines)', COUNT(*) FROM [InputInfo]
UNION ALL
SELECT N'Output (Receipts)', COUNT(*) FROM [Output]
UNION ALL
SELECT N'OutputInfo (Lines)', COUNT(*) FROM [OutputInfo]
ORDER BY [Table];

PRINT N'';
PRINT N'=== TÀI KHOẢN ĐĂNG NHẬP ===';
SELECT u.UserName, u.Password, u.DisplayName, r.DisplayName AS [Role]
FROM [User] u
JOIN [Role] r ON u.RoleId = r.RoleId
ORDER BY u.UserId;
GO
