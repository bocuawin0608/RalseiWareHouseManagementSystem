# RalseiWarehouse v2 — Design Q&A

Living document: every design question asked about the WMS database, with answers.
Schema lives in [`sql/ddl.sql`](sql/ddl.sql). Updated as new questions come in.

---

## Contents

1. [What are the business rules of this database?](#q1-what-are-the-business-rules-of-this-database)
2. [What is `CustomerRules`?](#q2-what-is-customerrules)
3. [What roles does this database contain?](#q3-what-roles-does-this-database-contain)
4. [What does each stock-related table do?](#q4-what-does-each-stock-related-table-do)
5. [Why do customers have no login?](#q5-why-do-customers-have-no-login)
6. [How does the workflow work, field by field?](#q6-how-does-the-workflow-work-field-by-field)
7. [What does each table do?](#q7-what-does-each-table-do)

---

## Q1: What are the business rules of this database?

Each rule below shows **where the database enforces it**.

### 1. Parties (Accounts)

| # | Rule | Enforced by |
|---|---|---|
| 1.1 | Everyone — staff, customers, suppliers — is one `Account`; what they *are* comes from `Role`, not from separate tables | `Account.RoleId` |
| 1.2 | Only Staff/Admin can log in; customers & suppliers have no credentials | `UserName`/`PasswordHash` NULL-able + filtered unique index |
| 1.3 | Login names must be unique when they exist | `UX_Account_UserName WHERE UserName IS NOT NULL` |
| 1.4 | A customer may impose packaging / lot / expiry / labeling / documentation rules on us | `Account.CustomerRules` (must be valid JSON — `ISJSON` CHECK) |
| 1.5 | Parties are never deleted, only deactivated (orders reference them forever) | `IsActive` flag, no cascading deletes |

### 2. Orders (inbound & outbound)

| # | Rule | Enforced by |
|---|---|---|
| 2.1 | A receipt from a supplier and a shipment to a customer are the same kind of document — direction is just a flag | `OrderHeader.OrderType IN ('IN','OUT')` |
| 2.2 | `IN` → `AccountId` is a supplier; `OUT` → a customer *(app-enforced, DB keeps it flexible)* | FK to `Account` |
| 2.3 | Orders live through **Draft → Confirmed → InProgress → Completed** (or Cancelled) | `Status` CHECK |
| 2.4 | An order may carry a scheduled collection/delivery window; end can't be before start | `ScheduledStart/End` + table CHECK |
| 2.5 | Price is frozen at order time — later price changes never rewrite history | `OrderLine.UnitPrice` |
| 2.6 | You can never allocate / pick / ship more than was ordered | `QtyAllocated/Picked/Shipped BETWEEN 0 AND QtyOrdered` |

### 3. Physical inventory (core rule of the SRS)

| # | Rule | Enforced by |
|---|---|---|
| 3.1 | Inventory is **not** a number on a product — it is physical stock sitting in an identifiable `Location` | `Stock.LocationId` (NOT NULL) |
| 3.2 | Stock is optionally separated by lot, serial number, pallet (`HandlingUnit`), owner, quality status, expiry date, and the receipt that brought it in | nullable columns on `Stock` |
| 3.3 | One physical pallet may hold several products (mixed pallet); a full pallet is a pallet holding one product | `HandlingUnit` is just a label — many rows can share it |
| 3.4 | Quantities can never go negative | `CHECK (QtyOnHand >= 0)` |
| 3.5 | Stock rows are **zeroed, never deleted** — history must always have a valid target | design convention + FK from `StockMovement` |
| 3.6 | Two workers must not corrupt the same stock row simultaneously | `RowVer ROWVERSION` (optimistic concurrency) |

### 4. Quality

| # | Rule | Enforced by |
|---|---|---|
| 4.1 | Only `Available` stock can be allocated/picked/shipped | `QualityStatus` CHECK + FEFO query/filtered index |
| 4.2 | `Quarantine`, `Damaged`, `Hold` stock stays visible but untouchable | same column |
| 4.3 | Quality changes are themselves inventory events and must leave a trail | `MovementType IN ('Damage','Quarantine','Release',...)` |

### 5. Reservation & allocation

| # | Rule | Enforced by |
|---|---|---|
| 5.1 | Reserved stock is still physically in the location, but not available to anyone else | `Stock.QtyReserved` |
| 5.2 | You can never reserve more than is on hand | `CHECK (QtyReserved BETWEEN 0 AND QtyOnHand)` |
| 5.3 | Allocation follows **FEFO** — first expiry, first out; stock with no expiry is picked last | allocation CTE pattern in `sql/ddl.sql` |
| 5.4 | Every reservation knows *which order line* owns it and *which stock row* it sits on | `StockReservation(OrderLineId, StockId, Qty)` |

### 6. Execution (worker tasks)

| # | Rule | Enforced by |
|---|---|---|
| 6.1 | Physical work (Receive, Putaway, Pick, Pack, Stage, Load, Count) is assigned as tasks to workers | `WorkTask` + `AssignedTo` → Staff account |
| 6.2 | Tasks flow **Open → Assigned → InProgress → Done** (or Cancelled); timestamps prove when work started/finished | `Status` CHECK + `CreatedAt/StartedAt/CompletedAt` |
| 6.3 | Urgent work jumps the queue | `Priority` + queue index |

### 7. Audit (non-negotiable)

| # | Rule | Enforced by |
|---|---|---|
| 7.1 | **Every** inventory-changing action writes to `StockMovement` in the *same transaction* as the stock update — no movement without evidence | transaction pattern in `sql/ddl.sql` |
| 7.2 | The ledger answers who / when / what / from-where / to-where / why | `PerformedBy`, `MovementDate`, `MovementType` + signed `Qty`, `From/ToLocationId`, `Note` |
| 7.3 | Receipt comes from "nowhere" (From = NULL); shipment goes to "nowhere" (To = NULL) | nullable location FKs |
| 7.4 | The ledger is append-only — corrections are new rows (`Adjust`, `CountAdjust`), never edits | convention |

### 8. Static vs time-based separation

- **Static** (master data, edited rarely, no history): `Role, Account, Unit, Product, Location`
- **Time-based** (everything with a date/quantity lifecycle): `OrderHeader/Line`, `Stock`, `StockReservation`, `WorkTask`, `StockMovement`
- Consequence: things whose value depends on *when* (price, expiry, schedule, stock level) are never stored on master tables.

---

## Q2: What is `CustomerRules`?

From SRS §1.2: *"Customer-specific packaging requirements. Customer-specific lot, expiry, labeling, or documentation rules."*

Some customers are picky about **how** their goods are packed and shipped. Instead of adding more tables for every possible requirement (violates KISS), each customer account gets **one JSON column**:

```sql
Account.CustomerRules NVARCHAR(MAX)   -- must be valid JSON (ISJSON CHECK)
```

Example content:

```json
{
  "minShelfLifeDays": 180,          // don't ship stock expiring within 6 months
  "singleLotPerPallet": true,       // never mix two lots on one pallet
  "maxCartonsPerPallet": 40,
  "palletType": "EUR",
  "labelLanguage": "EN",
  "requireCOA": true,               // certificate of analysis must ship with goods
  "requirePhoto": true              // photo before sealing the truck
}
```

**Usage:** when the app creates pick/pack tasks for an order, it reads the customer's `CustomerRules` and applies them (e.g., FEFO allocation skips lots violating `minShelfLifeDays`). Only meaningful when `Role = 'Customer'`; staff/supplier accounts leave it NULL.

---

## Q3: What roles does this database contain?

Four, seeded in `dbo.Role`:

| Role | Who they are | What they do in the system |
|---|---|---|
| **Admin** | System owner/manager | Logs in. Manages master data (products, locations, accounts), does adjustments, sees audit trail |
| **Staff** | Warehouse worker | Logs in. Gets `WorkTask`s assigned, executes receive/putaway/pick/pack/ship, recorded in `StockMovement.PerformedBy` |
| **Customer** | Who we ship goods **to** | No login. Referenced by `OrderHeader.AccountId` on `OUT` orders; may carry `CustomerRules` |
| **Supplier** | Who we receive goods **from** | No login. Referenced by `OrderHeader.AccountId` on `IN` orders and by `Product.SupplierId` |

Note: a company that is **both** customer and supplier gets two account rows (one per role) in this simple design.

---

## Q4: What does each stock-related table do?

| Table | Real-world analogy | What it stores |
|---|---|---|
| **`Stock`** | *What's physically on the shelf right now* | Current state: product + location + lot/serial/pallet/owner/quality/expiry, with `QtyOnHand` and `QtyReserved`. One row per unique combination |
| **`StockReservation`** | *Sticky notes on the shelf* | "These 80 units of LOT-B are promised to order OUT-2026-0001." Still on the shelf, but nobody else can take it |
| **`StockMovement`** | *The CCTV footage / logbook* | Append-only history of **every** change: +100 received, −80 shipped, −5 damaged... who, when, from where, to where, why |
| **`WorkTask`** | *The foreman's job tickets* | The to-do list that causes stock to move: "Worker 7, pick 80 of LOT-B from A-01-02" |
| `vwAvailableStock` (view) | *What you can still promise to customers* | Read-only: `QtyOnHand − QtyReserved` for sellable stock only |

### One concrete flow through the tables

**Receiving (IN-2026-0001: 100 of LOT-A, 80 of LOT-B arrive):**
```
WorkTask:  "Receive 180 units at RCV-01"           → Done by worker
Stock:     +2 new rows (LOT-A 100, LOT-B 80)       ← the shelf now
Movement:  Receipt +100, Receipt +80               ← the logbook
```

**Customer orders 150 → allocation:**
```
Stock:        LOT-B.QtyReserved = 80, LOT-A.QtyReserved = 70
Reservation:  (OUT-line → LOT-B, 80), (OUT-line → LOT-A, 70)   ← sticky notes
```

**Worker picks & ships LOT-B:**
```
WorkTask:     "Pick 80 of LOT-B from A-01-02"      → Done
Stock:        LOT-B: QtyOnHand 80→0, QtyReserved 80→0
Reservation:  sticky note removed (row deleted)
Movement:     Ship −80, by Administrator           ← permanent evidence
```

**Why three tables and not one?** Each answers a different question fast:
- *"What do I have?"* → `Stock` (current, small, hot)
- *"What's already promised?"* → `StockReservation` (small, transactional)
- *"What happened last Tuesday?"* → `StockMovement` (huge, append-only)

Mixing them would force the hot "what do I have" query to wade through years of history — that is the static-vs-time separation.

---

## Q5: Why do customers have no login?

The WMS is an *internal* operational system — customers and suppliers never touch the warehouse floor, so they never touch the software either.

1. **The system's job is physical execution.** Per SRS §1.1, the WMS records receiving, put-away, picking, packing, staging, dispatch, and worker tasks. Only warehouse staff perform those actions. A customer just *appears on paperwork* (`OrderHeader.AccountId`).
2. **Orders come from upstream.** A customer order arrives via sales staff, phone, email, or ERP — then a staff member creates the `OUT` order. The customer is *data* in the order, not a *user* of the system.
3. **Security.** Every login is attack surface. An external party with credentials could see stock levels, other customers' orders, or prices. NULL login columns = impossible to log in, by construction.
4. **Simplicity.** No password resets or permission management for parties who gain nothing from logging in.
5. **The door is not welded shut.** Customers/suppliers share the `Account` table with nullable `UserName`/`PasswordHash`. Giving a customer a portal account later is just an `UPDATE` — **no schema change needed**. The filtered unique index (`WHERE UserName IS NOT NULL`) already handles the mix. (Same applies to suppliers.)

---

## Q6: How does the workflow work, field by field?

The two main pipelines plus exception flows. Running example: *Green Tea, LOT-A/LOT-B, IN-2026-0001, OUT-2026-0001*.

```
INBOUND :  [Register IN order] → [Receive @ dock] → [QC?] → [Putaway to shelf]
OUTBOUND:  [Register OUT order] → [Allocate FEFO] → [Pick] → [Pack] → [Stage] → [Ship confirm]
EXCEPTIONS: Damage / Quarantine / Cycle count — any time
```

### WORKFLOW A — INBOUND

#### Step A1: Register the inbound order (goods haven't arrived yet)

**`OrderHeader`** — the document:

| Field | Example | Why we need it |
|---|---|---|
| `OrderId` | 1 | Internal handle all child rows point to |
| `OrderNo` | `IN-2026-0001` | Human paperwork number — matched against the supplier's delivery note |
| `OrderType` | `IN` | Direction. One table serves both flows |
| `AccountId` | Acme (Supplier) | **Who** sends the goods — contact, contract, history |
| `Status` | `Draft → Confirmed` | Gate: only `Confirmed` orders may be received |
| `ScheduledStart/End` | 08:00–10:00 | **Delivery window** — dock/labor scheduling |
| `CreatedBy` | Administrator | Accountability: who registered it |
| `CreatedAt` | now | Audit + "orders per day" reports |

**`OrderLine`** — what's expected:

| Field | Example | Why we need it |
|---|---|---|
| `OrderLineId` | 1 | **The receipt's identity** — `Stock.ReceiptLineId` points here forever |
| `OrderId` | 1 | Parent document |
| `ProductId` | Green Tea | What's expected |
| `UnitId` | Carton | How it's counted on arrival |
| `QtyOrdered` | 500 | **Expected qty** — received vs ordered = discrepancy detection |
| `UnitPrice` | 2.50 | Cost **at that moment** — valuation history never rewrites |

#### Step A2: Receive at the dock

**`WorkTask`** (Receive):

| Field | Example | Why we need it |
|---|---|---|
| `TaskType` | `Receive` | Routes the task to the right queue |
| `Status` | `Open→Done` | Progress + proof the work happened |
| `OrderLineId` | 1 | Which line is being received |
| `ToLocationId` | RCV-01 | Where goods physically land |
| `Qty` | 500 | How much to receive |
| `AssignedTo` | Worker 7 | **Whose job it is** |
| `StartedAt/CompletedAt` | 08:12 / 08:40 | Labor timing, worker KPI |

**`Stock`** rows are born (one per lot):

| Field | Example | Why we need it |
|---|---|---|
| `ProductId` | Green Tea | What's on the shelf |
| `LocationId` | RCV-01 | Stock **physically exists here** — inventory is location-bound |
| `LotNumber` | LOT-A | Supplier's lot — **recalls** |
| `ExpiryDate` | 2027-01-01 | mfg date + `Product.ShelfLifeDays` → drives **FEFO** |
| `HandlingUnit` | PAL-0001 | Pallet id if it arrived palletized |
| `QualityStatus` | `Available` / `Quarantine` | Can it be sold **right now**? |
| `ReceiptLineId` | 1 | **Trace back to the exact receipt** |
| `QtyOnHand / QtyReserved` | 500 / 0 | Physical qty / promised qty |
| `OwnerId` | NULL | Whose goods (3PL scenario) |

**`StockMovement`** — the evidence:

| Field | Example | Why we need it |
|---|---|---|
| `MovementType` | `Receipt` | What kind of event |
| `Qty` | **+500** | Signed: in = positive |
| `FromLocationId` | **NULL** | Came from outside |
| `ToLocationId` | RCV-01 | Where it landed |
| `StockId` + `ProductId` | … | What moved (`ProductId` denormalized for fast reports) |
| `OrderLineId` / `TaskId` | 1 / 1 | Which **document** and which **job** caused it |
| `PerformedBy` | Worker 7 | **Who** did it |
| `Note` | "2 cartons dented" | The *why* |

#### Step A3: Put-away to storage

```
WorkTask(Putaway): StockId = LOT-A, FromLocationId = RCV-01, ToLocationId = A-01-01, Qty = 500
Stock:    LocationId  RCV-01 → A-01-01        (or row splits if partial)
Movement: Putaway +500, From RCV-01, To A-01-01
```

`FromLocationId`/`ToLocationId` on the task tell the worker exactly: *take THIS stock, FROM there, TO there.*

> Sign convention: `Receipt/Ship/Adjust` are **signed net changes** (+/−). `Putaway/Pick/Pack/Move` are **transfer events** — positive qty, both locations set; they net to zero warehouse-wide.

### WORKFLOW B — OUTBOUND

#### Step B1: Register the customer order

Same tables, `OrderType = 'OUT'`. `AccountId` = customer, `ScheduledStart/End` = **collection window**, `UnitPrice` = **selling** price. The app reads `CustomerRules` now (e.g. `minShelfLifeDays` filters allocation).

#### Step B2: Allocate (FEFO) — no physical movement yet

| Write | Example | Why we need it |
|---|---|---|
| `StockReservation` row | (line 2 → LOT-B, 80) | The **sticky note**: which lot is promised to which order — released on cancel |
| `Stock.QtyReserved` ↑ | LOT-B: 0→80 | Fast availability (`OnHand − Reserved`) without summing reservations |
| `OrderLine.QtyAllocated` ↑ | 80 | Order-side progress: "fully allocated?" |

Stock side answers *"can I sell this?"*; order side answers *"is the order ready?"*

#### Step B3: Pick

```
WorkTask(Pick): StockId = LOT-B, FromLocationId = A-01-02, Qty = 80, AssignedTo = Worker 3
Stock:        QtyOnHand 80→0, QtyReserved 80→0   (reservation becomes a real pick)
Reservation:  row deleted
Movement:     Pick 80, From A-01-02, To STG-01
OrderLine:    QtyPicked 80
```

#### Step B4: Pack

```
WorkTask(Pack): ToLocationId = STG-01, Qty = 80
Stock:    HandlingUnit = PAL-0099   ← pallets/cartons are BUILT here
Movement: Pack 80, To STG-01
```

`CustomerRules` bites here: `maxCartonsPerPallet`, `singleLotPerPallet`, `labelLanguage`. Mixed pallets = several stock rows sharing one `HandlingUnit`.

#### Step B5–B6: Stage & ship confirmation

```
WorkTask(Load): From STG-01, against the ScheduledStart/End collection window
Movement:  Ship −80, From STG-01, To NULL   ← left the building
OrderLine: QtyShipped 80 → all lines complete →
OrderHeader.Status = Completed              ← dispatch confirmation
```

### WORKFLOW C — EXCEPTIONS (any time)

| Event | What happens | Why those fields matter |
|---|---|---|
| **Damage found** | `QualityStatus = 'Damaged'` + movement `Damage −5`, `Note = "forklift"` | `Note` + `PerformedBy` = who found it and why |
| **QC hold** | `QualityStatus = 'Quarantine'` (or move to `QTN-01`); `Release` movement when QC passes | Invisible to allocation, physically still counted |
| **Cycle count** | `WorkTask(Count)`; counted 95 vs 100 → `QtyOnHand = 95` + `CountAdjust −5` | Book stock = physical stock, with evidence |
| **Order cancelled** | Delete reservations, `QtyReserved`/`QtyAllocated` ↓, `Status = Cancelled` — **no movement row** | Movements are only for *physical* events |

### The golden rule

Every physical action is **one transaction with three writes**:

```
UPDATE Stock          (state changed)
INSERT StockMovement  (evidence written)
UPDATE WorkTask       (job completed)
-- all-or-nothing: no stock change without evidence, no evidence without a job
```

That is how the schema delivers the SRS promise: *"audit evidence for every inventory-changing action."*
