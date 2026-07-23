/* ============================================================================
   RalseiWarehouse v2 - Warehouse Management System schema (SQL Server / T-SQL)

   Design rules (KISS):
     1. ONE party table: Account + Role. Customers and suppliers are roles,
        not separate tables. Only Staff/Admin rows have login columns.
     2. Static master data (Role, Account, Unit, Product, Location) is kept
        apart from time/date-driven data (OrderHeader/Line, Stock,
        StockReservation, WorkTask, StockMovement).
     3. StockMovement is the append-only audit ledger: EVERY inventory-
        changing action writes row(s) here in the same transaction as the
        Stock update. Who / when / what / where / why.
     4. No lookup tables for status enums -> CHECK constraints (fewer tables).

   v1 -> v2 mapping:
     User + Role            -> Account (+ Customer/Supplier merged in)
     Customer / Supplier    -> Account (Role = 'Customer' / 'Supplier')
     Object                 -> Product
     Unit                   -> Unit
     Input / InputInfo      -> OrderHeader / OrderLine (OrderType = 'IN')
     Output / OutputInfo    -> OrderHeader / OrderLine (OrderType = 'OUT')
     (new)                  -> Location, Stock, StockReservation, WorkTask,
                               StockMovement
   ========================================================================== */

-- Required for filtered indexes from any client (sqlcmd defaults QUOTED_IDENTIFIER OFF)
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

USE master;
GO
-- DEV ONLY: drops and recreates the v2 database. The old RalseiWarehouse
-- database is NOT touched.
DROP DATABASE IF EXISTS RalseiWarehouseV2;
CREATE DATABASE RalseiWarehouseV2;
GO
USE RalseiWarehouseV2;
GO

/* ============================================================================
   SECTION A - STATIC MASTER DATA (changes rarely, no history needed)
   ========================================================================== */

CREATE TABLE dbo.Role(
    RoleId       INT IDENTITY(1,1) PRIMARY KEY,
    Name         NVARCHAR(50) NOT NULL UNIQUE          -- Admin / Staff / Customer / Supplier
);
GO

CREATE TABLE dbo.Account(
    AccountId    INT IDENTITY(1,1) PRIMARY KEY,
    RoleId       INT           NOT NULL REFERENCES dbo.Role(RoleId),
    DisplayName  NVARCHAR(200) NOT NULL,
    -- login: only for Staff/Admin accounts; NULL for customers & suppliers
    UserName     NVARCHAR(100) NULL,
    PasswordHash NVARCHAR(256) NULL,                   -- store a hash, never plaintext
    -- contact info
    Phone        NVARCHAR(20)  NULL,
    Email        NVARCHAR(200) NULL,
    [Address]    NVARCHAR(400) NULL,
    ContractDate DATE          NULL,
    -- customer-specific packaging / lot / expiry / labeling / documentation
    -- rules, as JSON; only meaningful when Role = 'Customer'
    CustomerRules NVARCHAR(MAX) NULL CHECK (CustomerRules IS NULL OR ISJSON(CustomerRules) = 1),
    IsActive     BIT NOT NULL DEFAULT 1                -- soft delete
);
GO
-- Logins must be unique, but many NULLs (customers/suppliers) are allowed
CREATE UNIQUE INDEX UX_Account_UserName ON dbo.Account(UserName)
    WHERE UserName IS NOT NULL;
GO

CREATE TABLE dbo.Unit(
    UnitId       INT IDENTITY(1,1) PRIMARY KEY,
    DisplayName  NVARCHAR(50) NOT NULL UNIQUE          -- Piece / Carton / Pallet
);
GO

CREATE TABLE dbo.Product(
    ProductId        INT IDENTITY(1,1) PRIMARY KEY,
    SKU              NVARCHAR(50)  NOT NULL UNIQUE,
    DisplayName      NVARCHAR(200) NOT NULL,
    UnitId           INT NOT NULL REFERENCES dbo.Unit(UnitId),        -- base (smallest) unit
    SupplierId       INT NOT NULL REFERENCES dbo.Account(AccountId),  -- default supplier account
    BarCode          NVARCHAR(64) NULL,
    UnitsPerCarton   INT NULL CHECK (UnitsPerCarton   > 0),           -- pack hierarchy:
    CartonsPerPallet INT NULL CHECK (CartonsPerPallet > 0),           -- full/mixed pallet & carton maths
    ShelfLifeDays    INT NULL CHECK (ShelfLifeDays    > 0),           -- sets ExpiryDate at receiving
    IsActive         BIT NOT NULL DEFAULT 1
);
GO
CREATE UNIQUE INDEX UX_Product_BarCode ON dbo.Product(BarCode)
    WHERE BarCode IS NOT NULL;
CREATE INDEX IX_Product_Supplier ON dbo.Product(SupplierId);          -- inbound-by-supplier lookups
GO

CREATE TABLE dbo.Location(
    LocationId   INT IDENTITY(1,1) PRIMARY KEY,
    Code         NVARCHAR(20) NOT NULL UNIQUE,        -- RCV-01, A-01-02, STG-01, SHP-01 ...
    LocationType NVARCHAR(20) NOT NULL CHECK (LocationType IN
                     ('Storage','Receiving','Staging','Shipping','Quarantine')),
    Zone         NVARCHAR(20) NULL,
    IsActive     BIT NOT NULL DEFAULT 1
);
GO

/* ============================================================================
   SECTION B - TIME / DATE-DRIVEN DATA (operational, grows over time)
   ========================================================================== */

-- One document type for BOTH inbound receipts and outbound shipments.
CREATE TABLE dbo.OrderHeader(
    OrderId        INT IDENTITY(1,1) PRIMARY KEY,
    OrderNo        NVARCHAR(20) NOT NULL UNIQUE,      -- e.g. IN-2026-0001 / OUT-2026-0001
    OrderType      NVARCHAR(3)  NOT NULL CHECK (OrderType IN ('IN','OUT')),  -- IN=receipt, OUT=shipment
    AccountId      INT NOT NULL REFERENCES dbo.Account(AccountId),  -- supplier (IN) / customer (OUT)
    Status         NVARCHAR(20) NOT NULL DEFAULT 'Draft' CHECK (Status IN
                       ('Draft','Confirmed','InProgress','Completed','Cancelled')),
    -- scheduled collection / delivery window
    ScheduledStart DATETIME2 NULL,
    ScheduledEnd   DATETIME2 NULL,
    Note           NVARCHAR(400) NULL,
    CreatedBy      INT NULL REFERENCES dbo.Account(AccountId),      -- staff member
    CreatedAt      DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    CHECK (ScheduledEnd >= ScheduledStart)
);
GO
CREATE INDEX IX_OrderHeader_WorkQueue ON dbo.OrderHeader(OrderType, Status, CreatedAt);
CREATE INDEX IX_OrderHeader_Account   ON dbo.OrderHeader(AccountId, CreatedAt);
GO

CREATE TABLE dbo.OrderLine(
    OrderLineId  INT IDENTITY(1,1) PRIMARY KEY,
    OrderId      INT NOT NULL REFERENCES dbo.OrderHeader(OrderId),
    ProductId    INT NOT NULL REFERENCES dbo.Product(ProductId),
    UnitId       INT NOT NULL REFERENCES dbo.Unit(UnitId),   -- ordered as Pallet/Carton/Piece
    QtyOrdered   INT NOT NULL CHECK (QtyOrdered > 0),
    UnitPrice    DECIMAL(18,2) NULL,                          -- price at order time (time-based!)
    QtyAllocated INT NOT NULL DEFAULT 0,                      -- lifecycle counters, each updated
    QtyPicked    INT NOT NULL DEFAULT 0,                      -- by exactly one operation
    QtyShipped   INT NOT NULL DEFAULT 0,
    CHECK (QtyAllocated BETWEEN 0 AND QtyOrdered
       AND QtyPicked    BETWEEN 0 AND QtyOrdered
       AND QtyShipped   BETWEEN 0 AND QtyOrdered)
);
GO
-- Covering index: order detail screens read the whole line set of one order
CREATE INDEX IX_OrderLine_Order ON dbo.OrderLine(OrderId)
    INCLUDE (ProductId, UnitId, QtyOrdered, QtyAllocated, QtyPicked, QtyShipped);
CREATE INDEX IX_OrderLine_Product ON dbo.OrderLine(ProductId);
GO

-- Physical stock: ONE row per unique (product, location, lot, serial,
-- pallet, quality, owner, receipt). Rows are zeroed, never deleted,
-- so StockMovement history always has a valid FK target.
CREATE TABLE dbo.Stock(
    StockId       INT IDENTITY(1,1) PRIMARY KEY,
    ProductId     INT NOT NULL REFERENCES dbo.Product(ProductId),
    LocationId    INT NOT NULL REFERENCES dbo.Location(LocationId),
    -- optional traceability dimensions
    LotNumber     NVARCHAR(50) NULL,
    SerialNumber  NVARCHAR(50) NULL,
    HandlingUnit  NVARCHAR(50) NULL,  -- pallet/carton LPN; several products on one HU = mixed pallet
    ExpiryDate    DATE         NULL,
    QualityStatus NVARCHAR(20) NOT NULL DEFAULT 'Available' CHECK (QualityStatus IN
                      ('Available','Quarantine','Damaged','Hold')),
    OwnerId       INT NULL REFERENCES dbo.Account(AccountId),      -- stock ownership (NULL = own)
    ReceiptLineId INT NULL REFERENCES dbo.OrderLine(OrderLineId),  -- receipt that brought it in
    QtyOnHand     INT NOT NULL DEFAULT 0 CHECK (QtyOnHand >= 0),
    QtyReserved   INT NOT NULL DEFAULT 0,
    RowVer        ROWVERSION,                                       -- optimistic concurrency (2 workers, 1 stock row)
    CHECK (QtyReserved BETWEEN 0 AND QtyOnHand)
);
GO
-- Hottest query in the WMS: FEFO allocation of available stock.
-- Filtered + covering -> pure index seek, no table touch.
CREATE INDEX IX_Stock_Fefo ON dbo.Stock(ProductId, ExpiryDate)
    INCLUDE (LocationId, LotNumber, HandlingUnit, QualityStatus, QtyOnHand, QtyReserved)
    WHERE QualityStatus = 'Available' AND QtyOnHand > 0;
-- "What is in this location?" / cycle counting
CREATE INDEX IX_Stock_Location ON dbo.Stock(LocationId, ProductId)
    INCLUDE (QtyOnHand, QtyReserved, QualityStatus);
GO

-- Allocation: which stock rows are reserved for which order line.
-- Qty is reduced / row deleted as picks consume it; picks stay audited
-- in StockMovement.
CREATE TABLE dbo.StockReservation(
    ReservationId INT IDENTITY(1,1) PRIMARY KEY,
    OrderLineId   INT NOT NULL REFERENCES dbo.OrderLine(OrderLineId),
    StockId       INT NOT NULL REFERENCES dbo.Stock(StockId),
    Qty           INT NOT NULL CHECK (Qty > 0),
    CreatedAt     DATETIME2 NOT NULL DEFAULT SYSDATETIME()
);
GO
CREATE INDEX IX_Reservation_Line  ON dbo.StockReservation(OrderLineId) INCLUDE (StockId, Qty);
CREATE INDEX IX_Reservation_Stock ON dbo.StockReservation(StockId)     INCLUDE (OrderLineId, Qty);
GO

-- Worker task assignment (named WorkTask to avoid clashing with C# Task in EF Core).
CREATE TABLE dbo.WorkTask(
    TaskId         BIGINT IDENTITY(1,1) PRIMARY KEY,
    TaskType       NVARCHAR(20) NOT NULL CHECK (TaskType IN
                       ('Receive','Putaway','Pick','Pack','Stage','Load','Count')),
    Status         NVARCHAR(20) NOT NULL DEFAULT 'Open' CHECK (Status IN
                       ('Open','Assigned','InProgress','Done','Cancelled')),
    Priority       INT NOT NULL DEFAULT 0,
    OrderLineId    INT NULL REFERENCES dbo.OrderLine(OrderLineId),
    StockId        INT NULL REFERENCES dbo.Stock(StockId),
    FromLocationId INT NULL REFERENCES dbo.Location(LocationId),
    ToLocationId   INT NULL REFERENCES dbo.Location(LocationId),
    Qty            INT NULL CHECK (Qty > 0),
    AssignedTo     INT NULL REFERENCES dbo.Account(AccountId),   -- worker (Role = Staff)
    CreatedAt      DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    StartedAt      DATETIME2 NULL,
    CompletedAt    DATETIME2 NULL
);
GO
-- "Next task" queue for the gun/terminal: seek on Status+Type, ordered by priority
CREATE INDEX IX_WorkTask_Queue ON dbo.WorkTask(Status, TaskType, Priority DESC, CreatedAt)
    INCLUDE (AssignedTo, OrderLineId, StockId);
CREATE INDEX IX_WorkTask_Worker ON dbo.WorkTask(AssignedTo, Status)
    WHERE AssignedTo IS NOT NULL;
GO

-- Append-only audit ledger: movement history AND audit evidence in one table.
CREATE TABLE dbo.StockMovement(
    MovementId     BIGINT IDENTITY(1,1) PRIMARY KEY,
    MovementDate   DATETIME2 NOT NULL DEFAULT SYSDATETIME(),
    MovementType   NVARCHAR(20) NOT NULL CHECK (MovementType IN
                       ('Receipt','Putaway','Pick','Pack','Move','Ship',
                        'Adjust','Damage','Quarantine','Release','Return','CountAdjust')),
    StockId        INT NOT NULL REFERENCES dbo.Stock(StockId),
    ProductId      INT NOT NULL REFERENCES dbo.Product(ProductId), -- denormalized for fast product history (append-only, no update risk)
    Qty            INT NOT NULL,                                 -- signed: + in, - out
    FromLocationId INT NULL REFERENCES dbo.Location(LocationId), -- NULL on Receipt
    ToLocationId   INT NULL REFERENCES dbo.Location(LocationId), -- NULL on Ship
    OrderLineId    INT NULL REFERENCES dbo.OrderLine(OrderLineId),
    TaskId         BIGINT NULL REFERENCES dbo.WorkTask(TaskId),
    PerformedBy    INT NULL REFERENCES dbo.Account(AccountId),   -- audit: who
    Note           NVARCHAR(400) NULL                            -- audit: why (adjust/damage reason)
);
GO
CREATE INDEX IX_Movement_ProductDate ON dbo.StockMovement(ProductId, MovementDate)
    INCLUDE (MovementType, Qty, FromLocationId, ToLocationId);   -- product history reports
CREATE INDEX IX_Movement_StockDate   ON dbo.StockMovement(StockId, MovementDate);
CREATE INDEX IX_Movement_OrderLine   ON dbo.StockMovement(OrderLineId)
    WHERE OrderLineId IS NOT NULL;                               -- audit trail per order
GO

/* ============================================================================
   VIEW - available-to-promise per stock row (on-hand minus reserved)
   ========================================================================== */
CREATE VIEW dbo.vwAvailableStock
AS
SELECT  s.ProductId,
        s.LocationId,
        s.LotNumber,
        s.HandlingUnit,
        s.ExpiryDate,
        s.QtyOnHand,
        s.QtyReserved,
        s.QtyOnHand - s.QtyReserved AS QtyAvailable
FROM    dbo.Stock s
WHERE   s.QualityStatus = 'Available'
  AND   s.QtyOnHand > 0              -- explicit so IX_Stock_Fefo filter matches
  AND   s.QtyOnHand > s.QtyReserved;
GO

/* ============================================================================
   SEED - minimal lookup data + default admin
   ========================================================================== */
INSERT dbo.Role(Name)        VALUES (N'Admin'), (N'Staff'), (N'Customer'), (N'Supplier');
INSERT dbo.Unit(DisplayName) VALUES (N'Piece'), (N'Carton'), (N'Pallet');

INSERT dbo.Location(Code, LocationType, Zone) VALUES
    (N'RCV-01', 'Receiving',  N'DOCK'),
    (N'STG-01', 'Staging',    N'DOCK'),
    (N'SHP-01', 'Shipping',   N'DOCK'),
    (N'QTN-01', 'Quarantine', N'QC'),
    (N'A-01-01','Storage',    N'A'),
    (N'A-01-02','Storage',    N'A'),
    (N'A-02-01','Storage',    N'A'),
    (N'A-02-02','Storage',    N'A');

-- Default logins. PLAIN TEXT passwords for now (course requirement) - hash later.
INSERT dbo.Account(RoleId, DisplayName, UserName, PasswordHash) VALUES
    (1, N'Administrator',    N'admin',   N'admin123'),   -- AccountId 1
    (2, N'Warehouse Worker', N'worker1', N'123');        -- AccountId 2

-- Demo master data so the app has something to work with out of the box
INSERT dbo.Account(RoleId, DisplayName, Phone, Email, CustomerRules) VALUES
    (4, N'Acme Supplies', N'0901111222', N'sales@acme.vn', NULL),                 -- AccountId 3 (Supplier)
    (3, N'Best Customer', N'0903334444', N'order@bestcust.vn',
        N'{"minShelfLifeDays":30,"singleLotPerPallet":true}');                    -- AccountId 4 (Customer)

INSERT dbo.Product(SKU, DisplayName, UnitId, SupplierId, UnitsPerCarton, CartonsPerPallet, ShelfLifeDays)
VALUES (N'SKU-001', N'Green Tea 250g', 1, 3, 24, 40, 365),
       (N'SKU-002', N'Oolong Tea 500g', 1, 3, 12, 30, 540);
GO

/* ============================================================================
   CORE QUERY PATTERNS (reference for the EF Core app)

   1) FEFO allocation - oldest expiry first, no-expiry stock last.
      Uses IX_Stock_Fefo: index seek on ProductId, no table access.

      DECLARE @ProductId INT = 1, @Needed INT = 100;
      WITH fefo AS (
          SELECT s.StockId,
                 s.QtyOnHand - s.QtyReserved AS QtyAvailable,
                 SUM(s.QtyOnHand - s.QtyReserved) OVER (
                     ORDER BY CASE WHEN s.ExpiryDate IS NULL THEN 1 ELSE 0 END,
                              s.ExpiryDate, s.StockId
                     ROWS UNBOUNDED PRECEDING) AS RunningQty
          FROM dbo.Stock s
          WHERE s.ProductId = @ProductId
            AND s.QualityStatus = 'Available'
            AND s.QtyOnHand > 0              -- implied by the line below; stated so the
            AND s.QtyOnHand > s.QtyReserved  -- IX_Stock_Fefo filter matches (index seek)
      )
      SELECT StockId,
             CASE WHEN RunningQty <= @Needed
                  THEN QtyAvailable                              -- take the whole row
                  ELSE @Needed - (RunningQty - QtyAvailable)     -- partial last row
             END AS QtyToReserve
      FROM fefo
      WHERE RunningQty - QtyAvailable < @Needed;               -- only rows we need

   2) Reserve inside ONE transaction (repeatable read on the stock rows):
      - UPDATE Stock SET QtyReserved += qty (CHECK stops over-reservation)
      - INSERT StockReservation rows
      - UPDATE OrderLine SET QtyAllocated += qty

   3) Every physical action (receive/putaway/pick/ship/adjust/damage...):
      - UPDATE Stock (QtyOnHand / QtyReserved / LocationId / QualityStatus)
      - INSERT StockMovement (type, signed qty, from/to, who, why)
      - complete the WorkTask
      ...all in one transaction -> StockMovement is the audit evidence.

   4) Worker next-task queue (uses IX_WorkTask_Queue):
      SELECT TOP 1 TaskId, TaskType, OrderLineId, StockId, Qty
      FROM dbo.WorkTask
      WHERE Status = 'Open' AND TaskType = 'Pick'
      ORDER BY Priority DESC, CreatedAt;
   ========================================================================== */
