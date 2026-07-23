/* ============================================================================
   RalseiWarehouseV2 - FAKE DATA for testing
   Scenario: "Ralsei Tea & Beverage Distribution Warehouse"
   - 60 days of operational history (inbound, outbound, exceptions)
   - Today's live work queue (open Receive / Putaway / Pick / Load tasks)
   Everything reconciles: stock = receipts - shipments +/- adjustments.

   RUN AFTER sql/ddl.sql. Safe to run once; re-running is blocked by a guard.
   ========================================================================== */
-- Required for the filtered index on Stock from any client
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE RalseiWarehouseV2;
GO

-- Guard: do not load twice (ddl.sql recreate = clean slate for a re-load)
IF EXISTS (SELECT 1 FROM dbo.Account WHERE UserName = N'worker2')
BEGIN
    PRINT 'Fake data already loaded - aborting. Recreate the DB with sql/ddl.sql first for a clean load.';
    SET NOEXEC ON;
END
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRAN;

PRINT '--- 0. Lookups ---';
DECLARE @RoleStaff INT     = (SELECT RoleId FROM dbo.Role WHERE Name = N'Staff');
DECLARE @RoleCustomer INT  = (SELECT RoleId FROM dbo.Role WHERE Name = N'Customer');
DECLARE @RoleSupplier INT  = (SELECT RoleId FROM dbo.Role WHERE Name = N'Supplier');
DECLARE @U INT             = (SELECT UnitId FROM dbo.Unit WHERE DisplayName = N'Piece');

DECLARE @Admin INT   = (SELECT AccountId FROM dbo.Account WHERE UserName = N'admin');
DECLARE @Worker1 INT = (SELECT AccountId FROM dbo.Account WHERE UserName = N'worker1');
DECLARE @Acme INT    = (SELECT AccountId FROM dbo.Account WHERE DisplayName = N'Acme Supplies');

DECLARE @RCV INT  = (SELECT LocationId FROM dbo.Location WHERE Code = N'RCV-01');
DECLARE @STG INT  = (SELECT LocationId FROM dbo.Location WHERE Code = N'STG-01');
DECLARE @QTN INT  = (SELECT LocationId FROM dbo.Location WHERE Code = N'QTN-01');
DECLARE @A0101 INT = (SELECT LocationId FROM dbo.Location WHERE Code = N'A-01-01');
DECLARE @A0102 INT = (SELECT LocationId FROM dbo.Location WHERE Code = N'A-01-02');
DECLARE @A0201 INT = (SELECT LocationId FROM dbo.Location WHERE Code = N'A-02-01');
DECLARE @A0202 INT = (SELECT LocationId FROM dbo.Location WHERE Code = N'A-02-02');

DECLARE @Now DATETIME2 = SYSDATETIME();
DECLARE @o INT, @l INT, @l2 INT, @t INT, @s INT;   -- working identity variables

PRINT '--- 1. More accounts: 2 workers, 3 suppliers, 4 customers ---';
INSERT dbo.Account(RoleId, DisplayName, UserName, PasswordHash, Phone, Email) VALUES
    (@RoleStaff, N'Minh Tran',  N'worker2', N'123', N'0912002002', N'minh.tran@ralsei.vn'),
    (@RoleStaff, N'Lan Pham',   N'worker3', N'123', N'0912003003', N'lan.pham@ralsei.vn');
DECLARE @Worker2 INT = (SELECT AccountId FROM dbo.Account WHERE UserName = N'worker2');
DECLARE @Worker3 INT = (SELECT AccountId FROM dbo.Account WHERE UserName = N'worker3');

INSERT dbo.Account(RoleId, DisplayName, Phone, Email, [Address], ContractDate) VALUES
    (@RoleSupplier, N'Highland Tea Co.',   N'0263388111', N'sales@highlandtea.vn',  N'12 Tran Phu, Da Lat',      DATEADD(year,-2,@Now)),
    (@RoleSupplier, N'Saigon Coffee JSC',  N'0283999222', N'b2b@saigoncoffee.vn',   N'88 Nguyen Hue, HCMC',      DATEADD(year,-3,@Now)),
    (@RoleSupplier, N'VietPack Packaging', N'0276555333', N'order@vietpack.vn',     N'Lot C5, Binh Duong IP',    DATEADD(year,-1,@Now));
DECLARE @Highland INT = (SELECT AccountId FROM dbo.Account WHERE DisplayName = N'Highland Tea Co.');
DECLARE @Coffee  INT  = (SELECT AccountId FROM dbo.Account WHERE DisplayName = N'Saigon Coffee JSC');
DECLARE @VietPack INT = (SELECT AccountId FROM dbo.Account WHERE DisplayName = N'VietPack Packaging');

INSERT dbo.Account(RoleId, DisplayName, Phone, Email, [Address], ContractDate, CustomerRules) VALUES
    (@RoleCustomer, N'Lotte Mart',        N'0287300001', N'scm@lottemart.vn',     N'469 Nguyen Huu Tho, HCMC',  DATEADD(year,-2,@Now),
        N'{"minShelfLifeDays":120,"palletType":"EUR","requireCOA":true}'),
    (@RoleCustomer, N'Circle K Vietnam',  N'0287300002', N'supply@circlek.vn',    N'72 Le Thanh Ton, HCMC',     DATEADD(year,-1,@Now),
        N'{"minShelfLifeDays":90,"singleLotPerPallet":true,"labelLanguage":"VI"}'),
    (@RoleCustomer, N'Phuc Long Tea House',N'0287300003', N'nhaphang@phuclong.vn', N'42 Mac Thi Buoi, HCMC',    DATEADD(month,-8,@Now),
        N'{"minShelfLifeDays":60}'),
    (@RoleCustomer, N'Metro Wholesale',   N'0287300004', N'inbound@metro.vn',     N'KCN An Ha, Ha Noi',         DATEADD(year,-4,@Now),
        N'{"minShelfLifeDays":150,"maxCartonsPerPallet":40,"requirePhoto":true}');
DECLARE @Lotte INT    = (SELECT AccountId FROM dbo.Account WHERE DisplayName = N'Lotte Mart');
DECLARE @CircleK INT  = (SELECT AccountId FROM dbo.Account WHERE DisplayName = N'Circle K Vietnam');
DECLARE @PhucLong INT = (SELECT AccountId FROM dbo.Account WHERE DisplayName = N'Phuc Long Tea House');
DECLARE @Metro INT    = (SELECT AccountId FROM dbo.Account WHERE DisplayName = N'Metro Wholesale');

PRINT '--- 2. More products + more storage locations ---';
INSERT dbo.Product(SKU, DisplayName, UnitId, SupplierId, BarCode, UnitsPerCarton, CartonsPerPallet, ShelfLifeDays) VALUES
    (N'SKU-003', N'Jasmine Tea 250g',     @U, @Highland,  N'8936000000031', 24, 40, 365),
    (N'SKU-004', N'Black Tea 500g',       @U, @Highland,  N'8936000000048', 12, 30, 540),
    (N'SKU-005', N'Arabica Coffee 250g',  @U, @Coffee,    N'8936000000055', 24, 48, 730),
    (N'SKU-006', N'Robusta Coffee 500g',  @U, @Coffee,    N'8936000000062', 12, 24, 730),
    (N'SKU-007', N'Matcha Powder 100g',   @U, @Highland,  N'8936000000079', 40, 60, 365),
    (N'SKU-008', N'Paper Carton (pack)',  @U, @VietPack,  N'8936000000086',  1,100, NULL),  -- no shelf life: tests FEFO NULL-expiry-last
    (N'SKU-009', N'Herbal Infusion 20bags',@U, @Acme,     N'8936000000093', 24, 40, 540),
    (N'SKU-010', N'Earl Grey 250g',       @U, @Acme,      N'8936000000109', 24, 40, 365);
DECLARE @P001 INT = (SELECT ProductId FROM dbo.Product WHERE SKU = N'SKU-001');
DECLARE @P002 INT = (SELECT ProductId FROM dbo.Product WHERE SKU = N'SKU-002');
DECLARE @P003 INT = (SELECT ProductId FROM dbo.Product WHERE SKU = N'SKU-003');
DECLARE @P004 INT = (SELECT ProductId FROM dbo.Product WHERE SKU = N'SKU-004');
DECLARE @P005 INT = (SELECT ProductId FROM dbo.Product WHERE SKU = N'SKU-005');
DECLARE @P006 INT = (SELECT ProductId FROM dbo.Product WHERE SKU = N'SKU-006');
DECLARE @P007 INT = (SELECT ProductId FROM dbo.Product WHERE SKU = N'SKU-007');
DECLARE @P008 INT = (SELECT ProductId FROM dbo.Product WHERE SKU = N'SKU-008');
DECLARE @P009 INT = (SELECT ProductId FROM dbo.Product WHERE SKU = N'SKU-009');
DECLARE @P010 INT = (SELECT ProductId FROM dbo.Product WHERE SKU = N'SKU-010');

INSERT dbo.Location(Code, LocationType, Zone) VALUES
    (N'A-03-01', 'Storage', N'A'), (N'A-03-02', 'Storage', N'A'),
    (N'B-01-01', 'Storage', N'B'), (N'B-01-02', 'Storage', N'B'),
    (N'B-02-01', 'Storage', N'B'), (N'B-02-02', 'Storage', N'B');
DECLARE @A0301 INT = (SELECT LocationId FROM dbo.Location WHERE Code = N'A-03-01');
DECLARE @A0302 INT = (SELECT LocationId FROM dbo.Location WHERE Code = N'A-03-02');
DECLARE @B0101 INT = (SELECT LocationId FROM dbo.Location WHERE Code = N'B-01-01');
DECLARE @B0102 INT = (SELECT LocationId FROM dbo.Location WHERE Code = N'B-01-02');
DECLARE @B0201 INT = (SELECT LocationId FROM dbo.Location WHERE Code = N'B-02-01');

/* ==========================================================================
   SECTION 1 - INBOUND HISTORY (fully received & put away)
   Pattern per lot: Done Receive task -> Stock row -> Receipt movement
                    -> Done Putaway task -> Putaway movement
   The stock row sits at its FINAL shelf location; movements tell the journey.
   ========================================================================== */
PRINT '--- 3. IN-2026-0007 (Highland Tea, 60 days ago, Completed) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, Note, CreatedBy, CreatedAt)
VALUES (N'IN-2026-0007', 'IN', @Highland, 'Completed',
        DATEADD(day,-60,@Now), DATEADD(minute,120,DATEADD(day,-60,@Now)), N'2 trucks, morning slot', @Admin, DATEADD(day,-61,@Now));
SET @o = SCOPE_IDENTITY();

-- line: Jasmine Tea 480
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P003, @U, 480, 1.80);
SET @l = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,480,@Worker1,DATEADD(day,-60,@Now),DATEADD(minute,12,DATEADD(day,-60,@Now)),DATEADD(minute,60,DATEADD(day,-60,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P003, @A0201, N'LOT-JA-01', DATEADD(day, 305, @Now), 'Available', @l, 360);   -- 480 - 120 shipped later
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,60,DATEADD(day,-60,@Now)), 'Receipt', @s, @P003, 480, @RCV, @l, @t, @Worker1);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Putaway','Done',@l,@s,@RCV,480,@Worker2,DATEADD(minute,60,DATEADD(day,-60,@Now)),DATEADD(minute,72,DATEADD(day,-60,@Now)),DATEADD(minute,120,DATEADD(day,-60,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,120,DATEADD(day,-60,@Now)), 'Putaway', @s, @P003, 480, @RCV, @A0201, @l, @t, @Worker2);

-- line: Black Tea 240
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P004, @U, 240, 2.40);
SET @l = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,240,@Worker1,DATEADD(day,-60,@Now),DATEADD(minute,18,DATEADD(day,-60,@Now)),DATEADD(minute,66,DATEADD(day,-60,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P004, @A0202, N'LOT-BT-01', DATEADD(day, 480, @Now), 'Available', @l, 230);   -- 240 - 10 damaged later
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,66,DATEADD(day,-60,@Now)), 'Receipt', @s, @P004, 240, @RCV, @l, @t, @Worker1);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Putaway','Done',@l,@s,@RCV,240,@Worker2,DATEADD(minute,66,DATEADD(day,-60,@Now)),DATEADD(minute,78,DATEADD(day,-60,@Now)),DATEADD(minute,126,DATEADD(day,-60,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,126,DATEADD(day,-60,@Now)), 'Putaway', @s, @P004, 240, @RCV, @A0202, @l, @t, @Worker2);

-- line: Matcha 240 (later quarantined & released - same row, movements narrate)
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P007, @U, 240, 6.50);
SET @l = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,240,@Worker3,DATEADD(day,-60,@Now),DATEADD(minute,24,DATEADD(day,-60,@Now)),DATEADD(minute,72,DATEADD(day,-60,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P007, @A0301, N'LOT-MA-01', DATEADD(day, 305, @Now), 'Available', @l, 240);
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,72,DATEADD(day,-60,@Now)), 'Receipt', @s, @P007, 240, @RCV, @l, @t, @Worker3);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Putaway','Done',@l,@s,@RCV,240,@Worker3,DATEADD(minute,72,DATEADD(day,-60,@Now)),DATEADD(minute,90,DATEADD(day,-60,@Now)),DATEADD(minute,132,DATEADD(day,-60,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,132,DATEADD(day,-60,@Now)), 'Putaway', @s, @P007, 240, @RCV, @A0301, @l, @t, @Worker3);

PRINT '--- 4. IN-2026-0012 (Saigon Coffee, 45 days ago, Completed) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, Note, CreatedBy, CreatedAt)
VALUES (N'IN-2026-0012', 'IN', @Coffee, 'Completed',
        DATEADD(day,-45,@Now), DATEADD(minute,120,DATEADD(day,-45,@Now)), NULL, @Admin, DATEADD(day,-46,@Now));
SET @o = SCOPE_IDENTITY();

INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P005, @U, 480, 3.90);
SET @l = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,480,@Worker2,DATEADD(day,-45,@Now),DATEADD(minute,12,DATEADD(day,-45,@Now)),DATEADD(minute,60,DATEADD(day,-45,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P005, @B0101, N'LOT-AR-01', DATEADD(day, 685, @Now), 'Available', @l, 120);   -- 480 - 360 shipped later
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,60,DATEADD(day,-45,@Now)), 'Receipt', @s, @P005, 480, @RCV, @l, @t, @Worker2);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Putaway','Done',@l,@s,@RCV,480,@Worker1,DATEADD(minute,60,DATEADD(day,-45,@Now)),DATEADD(minute,78,DATEADD(day,-45,@Now)),DATEADD(minute,120,DATEADD(day,-45,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,120,DATEADD(day,-45,@Now)), 'Putaway', @s, @P005, 480, @RCV, @B0101, @l, @t, @Worker1);

INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P006, @U, 240, 3.10);
SET @l = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,240,@Worker2,DATEADD(day,-45,@Now),DATEADD(minute,18,DATEADD(day,-45,@Now)),DATEADD(minute,66,DATEADD(day,-45,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P006, @B0102, N'LOT-RO-01', DATEADD(day, 685, @Now), 'Available', @l, 240);
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,66,DATEADD(day,-45,@Now)), 'Receipt', @s, @P006, 240, @RCV, @l, @t, @Worker2);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Putaway','Done',@l,@s,@RCV,240,@Worker1,DATEADD(minute,66,DATEADD(day,-45,@Now)),DATEADD(minute,84,DATEADD(day,-45,@Now)),DATEADD(minute,126,DATEADD(day,-45,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,126,DATEADD(day,-45,@Now)), 'Putaway', @s, @P006, 240, @RCV, @B0102, @l, @t, @Worker1);

PRINT '--- 5. IN-2026-0019 (Acme, 30 days ago, Completed) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, Note, CreatedBy, CreatedAt)
VALUES (N'IN-2026-0019', 'IN', @Acme, 'Completed',
        DATEADD(day,-30,@Now), DATEADD(minute,180,DATEADD(day,-30,@Now)), N'Mixed tea container', @Admin, DATEADD(day,-31,@Now));
SET @o = SCOPE_IDENTITY();

INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P001, @U, 500, 2.50);
SET @l = SCOPE_IDENTITY();      -- Green Tea receipt line (referenced by staged row later)
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,500,@Worker1,DATEADD(day,-30,@Now),DATEADD(minute,12,DATEADD(day,-30,@Now)),DATEADD(minute,90,DATEADD(day,-30,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P001, @A0101, N'LOT-GR-01', DATEADD(day, 335, @Now), 'Available', @l, 360);   -- 500 - 100 shipped - 40 picked (staged)
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,90,DATEADD(day,-30,@Now)), 'Receipt', @s, @P001, 500, @RCV, @l, @t, @Worker1);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Putaway','Done',@l,@s,@RCV,500,@Worker2,DATEADD(minute,90,DATEADD(day,-30,@Now)),DATEADD(minute,102,DATEADD(day,-30,@Now)),DATEADD(minute,150,DATEADD(day,-30,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,150,DATEADD(day,-30,@Now)), 'Putaway', @s, @P001, 500, @RCV, @A0101, @l, @t, @Worker2);

INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P002, @U, 120, 3.20);
SET @l = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,120,@Worker1,DATEADD(day,-30,@Now),DATEADD(minute,24,DATEADD(day,-30,@Now)),DATEADD(minute,96,DATEADD(day,-30,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P002, @A0302, N'LOT-OL-01', DATEADD(day, 510, @Now), 'Available', @l, 120);
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,96,DATEADD(day,-30,@Now)), 'Receipt', @s, @P002, 120, @RCV, @l, @t, @Worker1);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Putaway','Done',@l,@s,@RCV,120,@Worker3,DATEADD(minute,96,DATEADD(day,-30,@Now)),DATEADD(minute,108,DATEADD(day,-30,@Now)),DATEADD(minute,156,DATEADD(day,-30,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,156,DATEADD(day,-30,@Now)), 'Putaway', @s, @P002, 120, @RCV, @A0302, @l, @t, @Worker3);

INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P009, @U, 240, 2.90);
SET @l = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,240,@Worker2,DATEADD(day,-30,@Now),DATEADD(minute,30,DATEADD(day,-30,@Now)),DATEADD(minute,102,DATEADD(day,-30,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P009, @A0102, N'LOT-HB-01', DATEADD(day, 510, @Now), 'Available', @l, 240);
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,102,DATEADD(day,-30,@Now)), 'Receipt', @s, @P009, 240, @RCV, @l, @t, @Worker2);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Putaway','Done',@l,@s,@RCV,240,@Worker3,DATEADD(minute,102,DATEADD(day,-30,@Now)),DATEADD(minute,114,DATEADD(day,-30,@Now)),DATEADD(minute,162,DATEADD(day,-30,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,162,DATEADD(day,-30,@Now)), 'Putaway', @s, @P009, 240, @RCV, @A0102, @l, @t, @Worker3);

INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P010, @U, 240, 3.40);
SET @l = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,240,@Worker2,DATEADD(day,-30,@Now),DATEADD(minute,36,DATEADD(day,-30,@Now)),DATEADD(minute,108,DATEADD(day,-30,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P010, @A0102, N'LOT-EG-01', DATEADD(day, 335, @Now), 'Available', @l, 180);   -- 240 - 60 shipped later
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,108,DATEADD(day,-30,@Now)), 'Receipt', @s, @P010, 240, @RCV, @l, @t, @Worker2);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Putaway','Done',@l,@s,@RCV,240,@Worker3,DATEADD(minute,108,DATEADD(day,-30,@Now)),DATEADD(minute,120,DATEADD(day,-30,@Now)),DATEADD(minute,168,DATEADD(day,-30,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,168,DATEADD(day,-30,@Now)), 'Putaway', @s, @P010, 240, @RCV, @A0102, @l, @t, @Worker3);

PRINT '--- 6. IN-2026-0023 (VietPack, 10 days ago, Completed - packaging, NO expiry) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, Note, CreatedBy, CreatedAt)
VALUES (N'IN-2026-0023', 'IN', @VietPack, 'Completed',
        DATEADD(day,-10,@Now), DATEADD(minute,60,DATEADD(day,-10,@Now)), NULL, @Admin, DATEADD(day,-11,@Now));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P008, @U, 1000, 0.35);
SET @l = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,1000,@Worker3,DATEADD(day,-10,@Now),DATEADD(minute,12,DATEADD(day,-10,@Now)),DATEADD(minute,60,DATEADD(day,-10,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P008, @B0201, N'LOT-PK-01', NULL, 'Available', @l, 995);   -- 1000 - 5 count variance
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,60,DATEADD(day,-10,@Now)), 'Receipt', @s, @P008, 1000, @RCV, @l, @t, @Worker3);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Putaway','Done',@l,@s,@RCV,1000,@Worker1,DATEADD(minute,60,DATEADD(day,-10,@Now)),DATEADD(minute,72,DATEADD(day,-10,@Now)),DATEADD(minute,120,DATEADD(day,-10,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,120,DATEADD(day,-10,@Now)), 'Putaway', @s, @P008, 1000, @RCV, @B0201, @l, @t, @Worker1);

/* ==========================================================================
   SECTION 2 - OUTBOUND HISTORY (Completed orders with full audit trail)
   Pattern per line: Done Pick + Pack + Load tasks, Pick/Pack/Ship movements,
   line counters fully set. Stock rows above are already at post-ship qty.
   ========================================================================== */
PRINT '--- 7. OUT-2026-0011 (Lotte Mart, 55 days ago, Completed) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, CreatedBy, CreatedAt)
VALUES (N'OUT-2026-0011', 'OUT', @Lotte, 'Completed',
        DATEADD(day,-55,@Now), DATEADD(minute,120,DATEADD(day,-55,@Now)), @Admin, DATEADD(day,-56,@Now));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice, QtyAllocated, QtyPicked, QtyShipped)
VALUES (@o, @P003, @U, 120, 3.50, 120, 120, 120);
SET @l = SCOPE_IDENTITY();
DECLARE @sJA01 INT = (SELECT StockId FROM dbo.Stock WHERE LotNumber = N'LOT-JA-01');
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pick','Done',@l,@sJA01,@A0201,120,@Worker1,DATEADD(day,-55,@Now),DATEADD(minute,18,DATEADD(day,-55,@Now)),DATEADD(minute,60,DATEADD(day,-55,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,60,DATEADD(day,-55,@Now)), 'Pick', @sJA01, @P003, 120, @A0201, @STG, @l, @t, @Worker1);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pack','Done',@l,@sJA01,@STG,@STG,120,@Worker2,DATEADD(minute,60,DATEADD(day,-55,@Now)),DATEADD(minute,72,DATEADD(day,-55,@Now)),DATEADD(minute,96,DATEADD(day,-55,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,96,DATEADD(day,-55,@Now)), 'Pack', @sJA01, @P003, 120, @STG, @STG, @l, @t, @Worker2);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Load','Done',@l,@sJA01,@STG,120,@Worker2,DATEADD(minute,96,DATEADD(day,-55,@Now)),DATEADD(minute,108,DATEADD(day,-55,@Now)),DATEADD(minute,120,DATEADD(day,-55,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,120,DATEADD(day,-55,@Now)), 'Ship', @sJA01, @P003, -120, @STG, NULL, @l, @t, @Worker2);

PRINT '--- 8. OUT-2026-0015 (Circle K, 40 days ago, Completed, 2 lines) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, CreatedBy, CreatedAt)
VALUES (N'OUT-2026-0015', 'OUT', @CircleK, 'Completed',
        DATEADD(day,-40,@Now), DATEADD(minute,120,DATEADD(day,-40,@Now)), @Admin, DATEADD(day,-41,@Now));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice, QtyAllocated, QtyPicked, QtyShipped)
VALUES (@o, @P001, @U, 100, 4.20, 100, 100, 100);
SET @l = SCOPE_IDENTITY();
DECLARE @sGR01 INT = (SELECT StockId FROM dbo.Stock WHERE LotNumber = N'LOT-GR-01' AND LocationId = @A0101);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pick','Done',@l,@sGR01,@A0101,100,@Worker1,DATEADD(day,-40,@Now),DATEADD(minute,18,DATEADD(day,-40,@Now)),DATEADD(minute,60,DATEADD(day,-40,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,60,DATEADD(day,-40,@Now)), 'Pick', @sGR01, @P001, 100, @A0101, @STG, @l, @t, @Worker1);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pack','Done',@l,@sGR01,@STG,@STG,100,@Worker1,DATEADD(minute,60,DATEADD(day,-40,@Now)),DATEADD(minute,66,DATEADD(day,-40,@Now)),DATEADD(minute,90,DATEADD(day,-40,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,90,DATEADD(day,-40,@Now)), 'Pack', @sGR01, @P001, 100, @STG, @STG, @l, @t, @Worker1);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Load','Done',@l,@sGR01,@STG,100,@Worker3,DATEADD(minute,90,DATEADD(day,-40,@Now)),DATEADD(minute,102,DATEADD(day,-40,@Now)),DATEADD(minute,120,DATEADD(day,-40,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,120,DATEADD(day,-40,@Now)), 'Ship', @sGR01, @P001, -100, @STG, NULL, @l, @t, @Worker3);

INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice, QtyAllocated, QtyPicked, QtyShipped)
VALUES (@o, @P005, @U, 120, 6.80, 120, 120, 120);
SET @l2 = SCOPE_IDENTITY();
DECLARE @sAR01 INT = (SELECT StockId FROM dbo.Stock WHERE LotNumber = N'LOT-AR-01');
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pick','Done',@l2,@sAR01,@B0101,120,@Worker2,DATEADD(day,-40,@Now),DATEADD(minute,24,DATEADD(day,-40,@Now)),DATEADD(minute,66,DATEADD(day,-40,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,66,DATEADD(day,-40,@Now)), 'Pick', @sAR01, @P005, 120, @B0101, @STG, @l2, @t, @Worker2);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pack','Done',@l2,@sAR01,@STG,@STG,120,@Worker2,DATEADD(minute,66,DATEADD(day,-40,@Now)),DATEADD(minute,78,DATEADD(day,-40,@Now)),DATEADD(minute,96,DATEADD(day,-40,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,96,DATEADD(day,-40,@Now)), 'Pack', @sAR01, @P005, 120, @STG, @STG, @l2, @t, @Worker2);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Load','Done',@l2,@sAR01,@STG,120,@Worker3,DATEADD(minute,96,DATEADD(day,-40,@Now)),DATEADD(minute,108,DATEADD(day,-40,@Now)),DATEADD(minute,126,DATEADD(day,-40,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,126,DATEADD(day,-40,@Now)), 'Ship', @sAR01, @P005, -120, @STG, NULL, @l2, @t, @Worker3);

PRINT '--- 9. OUT-2026-0021 (Metro, 20 days ago, Completed) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, CreatedBy, CreatedAt)
VALUES (N'OUT-2026-0021', 'OUT', @Metro, 'Completed',
        DATEADD(day,-20,@Now), DATEADD(minute,180,DATEADD(day,-20,@Now)), @Admin, DATEADD(day,-21,@Now));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice, QtyAllocated, QtyPicked, QtyShipped)
VALUES (@o, @P005, @U, 240, 6.50, 240, 240, 240);
SET @l = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pick','Done',@l,@sAR01,@B0101,240,@Worker3,DATEADD(day,-20,@Now),DATEADD(minute,30,DATEADD(day,-20,@Now)),DATEADD(minute,90,DATEADD(day,-20,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,90,DATEADD(day,-20,@Now)), 'Pick', @sAR01, @P005, 240, @B0101, @STG, @l, @t, @Worker3);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pack','Done',@l,@sAR01,@STG,@STG,240,@Worker1,DATEADD(minute,90,DATEADD(day,-20,@Now)),DATEADD(minute,102,DATEADD(day,-20,@Now)),DATEADD(minute,132,DATEADD(day,-20,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,132,DATEADD(day,-20,@Now)), 'Pack', @sAR01, @P005, 240, @STG, @STG, @l, @t, @Worker1);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Load','Done',@l,@sAR01,@STG,240,@Worker1,DATEADD(minute,132,DATEADD(day,-20,@Now)),DATEADD(minute,150,DATEADD(day,-20,@Now)),DATEADD(minute,180,DATEADD(day,-20,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,180,DATEADD(day,-20,@Now)), 'Ship', @sAR01, @P005, -240, @STG, NULL, @l, @t, @Worker1);

PRINT '--- 10. OUT-2026-0024 (Phuc Long, 5 days ago, Completed) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, CreatedBy, CreatedAt)
VALUES (N'OUT-2026-0024', 'OUT', @PhucLong, 'Completed',
        DATEADD(day,-5,@Now), DATEADD(minute,120,DATEADD(day,-5,@Now)), @Admin, DATEADD(day,-6,@Now));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice, QtyAllocated, QtyPicked, QtyShipped)
VALUES (@o, @P010, @U, 60, 5.10, 60, 60, 60);
SET @l = SCOPE_IDENTITY();
DECLARE @sEG01 INT = (SELECT StockId FROM dbo.Stock WHERE LotNumber = N'LOT-EG-01');
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pick','Done',@l,@sEG01,@A0102,60,@Worker2,DATEADD(day,-5,@Now),DATEADD(minute,18,DATEADD(day,-5,@Now)),DATEADD(minute,60,DATEADD(day,-5,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,60,DATEADD(day,-5,@Now)), 'Pick', @sEG01, @P010, 60, @A0102, @STG, @l, @t, @Worker2);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pack','Done',@l,@sEG01,@STG,@STG,60,@Worker2,DATEADD(minute,60,DATEADD(day,-5,@Now)),DATEADD(minute,72,DATEADD(day,-5,@Now)),DATEADD(minute,90,DATEADD(day,-5,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,90,DATEADD(day,-5,@Now)), 'Pack', @sEG01, @P010, 60, @STG, @STG, @l, @t, @Worker2);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Load','Done',@l,@sEG01,@STG,60,@Worker1,DATEADD(minute,90,DATEADD(day,-5,@Now)),DATEADD(minute,102,DATEADD(day,-5,@Now)),DATEADD(minute,120,DATEADD(day,-5,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,120,DATEADD(day,-5,@Now)), 'Ship', @sEG01, @P010, -60, @STG, NULL, @l, @t, @Worker1);

/* ==========================================================================
   SECTION 3 - EXCEPTIONS HISTORY
   ========================================================================== */
PRINT '--- 11. Damage (-15d), Quarantine (-8d), Release (-2d), CountAdjust (-1d) ---';
-- forklift damaged 10 Black Tea -> quarantine area as Damaged (separate row)
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
SELECT @P004, @QTN, N'LOT-BT-01', DATEADD(day, 480, @Now), 'Damaged', ReceiptLineId, 10
FROM dbo.Stock WHERE LotNumber = N'LOT-BT-01' AND LocationId = @A0202;
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, PerformedBy, Note)
VALUES (DATEADD(day,-15,@Now), 'Damage', SCOPE_IDENTITY(), @P004, 10, @A0202, @QTN, @Worker2, N'Forklift dented cartons');

-- moisture suspected on 24 Matcha -> quarantine, then QC released back
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, PerformedBy, Note)
SELECT DATEADD(day,-8,@Now), 'Quarantine', StockId, @P007, 24, @A0301, @QTN, @Worker1, N'Moisture stains on outer cartons'
FROM dbo.Stock WHERE LotNumber = N'LOT-MA-01';
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, PerformedBy, Note)
SELECT DATEADD(day,-2,@Now), 'Release', StockId, @P007, 24, @QTN, @A0301, @Admin, N'QC passed, goods dry'
FROM dbo.Stock WHERE LotNumber = N'LOT-MA-01';

-- annual count: 995 of 1000 cartons found
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, PerformedBy, Note)
SELECT DATEADD(day,-1,@Now), 'CountAdjust', StockId, @P008, -5, @B0201, @Worker3, N'Annual cycle count variance'
FROM dbo.Stock WHERE LotNumber = N'LOT-PK-01';

/* ==========================================================================
   SECTION 4 - TODAY'S LIVE WORK
   - IN-2026-0026 yesterday: half received, stock still at dock
   - IN-2026-0027 tomorrow: confirmed, tasks waiting
   - OUT-2026-0028 today: allocated (reservations + open Pick tasks)
   - OUT-2026-0029 today: picked & packed, waiting for the truck (open Load)
   - OUT-2026-0030 next week: confirmed, not allocated
   - OUT-2026-0031: draft
   ========================================================================== */
PRINT '--- 12. IN-2026-0026 (yesterday, InProgress: 120 of 240 received) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, Note, CreatedBy, CreatedAt)
VALUES (N'IN-2026-0026', 'IN', @Highland, 'InProgress',
        DATEADD(day,-1,@Now), DATEADD(minute,120,DATEADD(day,-1,@Now)), N'Truck arrived late, half unloaded', @Admin, DATEADD(day,-2,@Now));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P003, @U, 240, 1.85);
SET @l = SCOPE_IDENTITY();
-- done receive for first 120 -> stock sitting at the dock (putaway task still open)
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Receive','Done',@l,@RCV,120,@Worker1,DATEADD(day,-1,@Now),DATEADD(minute,12,DATEADD(day,-1,@Now)),DATEADD(minute,60,DATEADD(day,-1,@Now)));
SET @t = SCOPE_IDENTITY();
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, ReceiptLineId, QtyOnHand)
VALUES (@P003, @RCV, N'LOT-JA-02', DATEADD(day, 364, @Now), 'Available', @l, 120);
SET @s = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,60,DATEADD(day,-1,@Now)), 'Receipt', @s, @P003, 120, @RCV, @l, @t, @Worker1);
-- OPEN work for today: put away the 120 + receive the remaining 120
INSERT dbo.WorkTask(TaskType, Status, Priority, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt)
VALUES ('Putaway','Assigned',5,@l,@s,@RCV,120,@Worker2,DATEADD(minute,60,DATEADD(day,-1,@Now)));
INSERT dbo.WorkTask(TaskType, Status, Priority, OrderLineId, ToLocationId, Qty, CreatedAt)
VALUES ('Receive','Open',5,@l,@RCV,120,DATEADD(hour,-6,@Now));

PRINT '--- 13. IN-2026-0027 (tomorrow, Confirmed, tasks waiting) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, Note, CreatedBy, CreatedAt)
VALUES (N'IN-2026-0027', 'IN', @Coffee, 'Confirmed',
        DATEADD(day,1,DATEADD(hour,8,DATEADD(hour,-DATEPART(hour,@Now),@Now))), DATEADD(day,1,DATEADD(hour,10,DATEADD(hour,-DATEPART(hour,@Now),@Now))),
        N'Morning dock slot 1', @Admin, DATEADD(hour,-3,@Now));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P005, @U, 240, 4.00);
SET @l = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P006, @U, 120, 3.20);
SET @l2 = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, CreatedAt)
VALUES ('Receive','Open',@l,@RCV,240,DATEADD(hour,-3,@Now));
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, ToLocationId, Qty, CreatedAt)
VALUES ('Receive','Open',@l2,@RCV,120,DATEADD(hour,-3,@Now));

PRINT '--- 14. OUT-2026-0028 (today, allocated: reservations + open Pick tasks) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, Note, CreatedBy, CreatedAt)
VALUES (N'OUT-2026-0028', 'OUT', @Lotte, 'InProgress',
        DATEADD(hour,4,@Now), DATEADD(hour,6,@Now), N'COA must ship with goods', @Admin, DATEADD(hour,-2,@Now));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice, QtyAllocated)
VALUES (@o, @P003, @U, 100, 3.60, 100);
SET @l = SCOPE_IDENTITY();
UPDATE dbo.Stock SET QtyReserved = 100 WHERE StockId = @sJA01;
INSERT dbo.StockReservation(OrderLineId, StockId, Qty, CreatedAt) VALUES (@l, @sJA01, 100, DATEADD(hour,-2,@Now));
INSERT dbo.WorkTask(TaskType, Status, Priority, OrderLineId, StockId, FromLocationId, Qty, CreatedAt)
VALUES ('Pick','Open',10,@l,@sJA01,@A0201,100,DATEADD(hour,-2,@Now));

INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice, QtyAllocated)
VALUES (@o, @P005, @U, 50, 6.90, 50);
SET @l2 = SCOPE_IDENTITY();
UPDATE dbo.Stock SET QtyReserved = 50 WHERE StockId = @sAR01;
INSERT dbo.StockReservation(OrderLineId, StockId, Qty, CreatedAt) VALUES (@l2, @sAR01, 50, DATEADD(hour,-2,@Now));
INSERT dbo.WorkTask(TaskType, Status, Priority, OrderLineId, StockId, FromLocationId, Qty, CreatedAt)
VALUES ('Pick','Open',10,@l2,@sAR01,@B0101,50,DATEADD(hour,-2,@Now));

PRINT '--- 15. OUT-2026-0029 (today: picked & packed, truck due - open Load task) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, Note, CreatedBy, CreatedAt)
VALUES (N'OUT-2026-0029', 'OUT', @CircleK, 'InProgress',
        DATEADD(hour,2,@Now), DATEADD(hour,4,@Now), N'Driver: 090x-555-888', @Admin, DATEADD(minute,-60,DATEADD(day,-1,@Now)));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice, QtyAllocated, QtyPicked)
VALUES (@o, @P001, @U, 40, 4.30, 40, 40);
SET @l = SCOPE_IDENTITY();
-- staged pallet built from LOT-GR-01, waiting at STG-01
INSERT dbo.Stock(ProductId, LocationId, LotNumber, ExpiryDate, QualityStatus, HandlingUnit, ReceiptLineId, QtyOnHand)
VALUES (@P001, @STG, N'LOT-GR-01', DATEADD(day, 335, @Now), 'Available', N'PAL-00042', @l, 40);
SET @s = SCOPE_IDENTITY();
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pick','Done',@l,@s,@A0101,40,@Worker1,DATEADD(hour,-4,@Now),DATEADD(minute,-228,@Now),DATEADD(minute,-210,@Now));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(minute,-210,@Now), 'Pick', @s, @P001, 40, @A0101, @STG, @l, @t, @Worker1);
INSERT dbo.WorkTask(TaskType, Status, OrderLineId, StockId, FromLocationId, ToLocationId, Qty, AssignedTo, CreatedAt, StartedAt, CompletedAt)
VALUES ('Pack','Done',@l,@s,@STG,@STG,40,@Worker1,DATEADD(minute,-210,@Now),DATEADD(minute,-204,@Now),DATEADD(hour,-3,@Now));
SET @t = SCOPE_IDENTITY();
INSERT dbo.StockMovement(MovementDate, MovementType, StockId, ProductId, Qty, FromLocationId, ToLocationId, OrderLineId, TaskId, PerformedBy)
VALUES (DATEADD(hour,-3,@Now), 'Pack', @s, @P001, 40, @STG, @STG, @l, @t, @Worker1);
INSERT dbo.WorkTask(TaskType, Status, Priority, OrderLineId, StockId, FromLocationId, Qty, CreatedAt)
VALUES ('Load','Open',20,@l,@s,@STG,40,DATEADD(hour,-3,@Now));

PRINT '--- 16. OUT-2026-0030 (next week, Confirmed) + OUT-2026-0031 (Draft) ---';
INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, ScheduledStart, ScheduledEnd, Note, CreatedBy, CreatedAt)
VALUES (N'OUT-2026-0030', 'OUT', @Metro, 'Confirmed',
        DATEADD(day,7,@Now), DATEADD(minute,180,DATEADD(day,7,@Now)), N'Full pallet preferred', @Admin, DATEADD(hour,-1,@Now));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P006, @U, 120, 5.20);

INSERT dbo.OrderHeader(OrderNo, OrderType, AccountId, Status, Note, CreatedBy, CreatedAt)
VALUES (N'OUT-2026-0031', 'OUT', @PhucLong, 'Draft', N'Waiting for customer confirmation', @Admin, DATEADD(hour,-1,@Now));
SET @o = SCOPE_IDENTITY();
INSERT dbo.OrderLine(OrderId, ProductId, UnitId, QtyOrdered, UnitPrice) VALUES (@o, @P009, @U, 60, 4.10);

COMMIT TRAN;
PRINT '=== Fake data committed. ===';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRAN;
    PRINT 'FAILED - rolled back. ' + ERROR_MESSAGE();
    THROW;
END CATCH;
GO
SET NOEXEC OFF;
GO

/* ==========================================================================
   VERIFICATION SUMMARY
   ========================================================================== */
SET NOCOUNT ON;
PRINT '=== STOCK ON HAND PER PRODUCT ===';
SELECT p.SKU, p.DisplayName AS Product,
       SUM(s.QtyOnHand)   AS OnHand,
       SUM(s.QtyReserved) AS Reserved,
       SUM(s.QtyOnHand - s.QtyReserved) AS Available
FROM dbo.Stock s JOIN dbo.Product p ON p.ProductId = s.ProductId
GROUP BY p.SKU, p.DisplayName
ORDER BY p.SKU;

PRINT '=== ORDERS ===';
SELECT o.OrderNo, o.OrderType, a.DisplayName AS Party, o.Status,
       COUNT(l.OrderLineId) AS Lines, o.ScheduledStart
FROM dbo.OrderHeader o
JOIN dbo.Account a ON a.AccountId = o.AccountId
LEFT JOIN dbo.OrderLine l ON l.OrderId = o.OrderId
GROUP BY o.OrderNo, o.OrderType, a.DisplayName, o.Status, o.ScheduledStart, o.CreatedAt
ORDER BY o.CreatedAt;

PRINT '=== OPEN WORK QUEUE ===';
SELECT t.TaskId, t.TaskType, t.Status, t.Priority, t.Qty,
       p.DisplayName AS Product, lf.Code AS FromLoc, lt.Code AS ToLoc, w.DisplayName AS AssignedTo
FROM dbo.WorkTask t
LEFT JOIN dbo.OrderLine l ON l.OrderLineId = t.OrderLineId
LEFT JOIN dbo.Product p ON p.ProductId = COALESCE(l.ProductId, (SELECT s2.ProductId FROM dbo.Stock s2 WHERE s2.StockId = t.StockId))
LEFT JOIN dbo.Location lf ON lf.LocationId = t.FromLocationId
LEFT JOIN dbo.Location lt ON lt.LocationId = t.ToLocationId
LEFT JOIN dbo.Account w ON w.AccountId = t.AssignedTo
WHERE t.Status IN ('Open','Assigned','InProgress')
ORDER BY t.Priority DESC, t.CreatedAt;

PRINT '=== MOVEMENT LEDGER COUNT BY TYPE ===';
SELECT MovementType, COUNT(*) AS Rows, SUM(Qty) AS NetQty
FROM dbo.StockMovement
GROUP BY MovementType
ORDER BY MovementType;
GO
