# Demo Script — Ralsei Warehouse Management System

> A short walkthrough of all features, meant to be read aloud or recorded.

---

## 1. Login

When you launch the app, you get a login dialog. Type your username and password — plain text, no encryption, this is a school project.

**Default accounts:**
- `admin` / `admin` — Full access (sees Admin tab)
- `staff` / `staff` — No Admin tab
- `manager`, `keeper`, `inspector` — seeded via fakedata.sql

If credentials are wrong, the dialog stays open. If you close the window, the app shuts down. On success, the authenticated user is passed to the main window.

---

## 2. Main Window — Master Data tab

This is where you manage the 4 core entities: **Units, Suppliers, Customers, Products**.

**All 4 work the same way:**
- Click a row → fields populate → edit → Save
- Click New → fields clear → fill → Save creates a new record
- Select a row → Delete removes it

**Product IDs** are auto-generated: P0001, P0002, etc. You never type an ID. The backend queries the max existing number, increments by one, and pads to 4 digits.

**The Admin tab** is hidden for non-admin users — checked at startup via role name.

---

## 3. Transactions — Import (Stock In)

Click the Import sub-tab. You see:
- A table of recent import receipts
- A line-item editor below

**To create an import:**
1. Click "Add Line" → a new row appears
2. Pick a product from the ComboBox, enter quantity, enter buy price and sell price
3. Repeat for more products
4. Click "Complete Import"

**Validation:**
- At least one product, positive quantity, non-negative prices
- No duplicate products in the same receipt
- If validation fails, a red message appears — nothing is saved

The system generates a receipt ID like `IN-20250721-143022-abc123...` and saves everything in one transaction.

---

## 4. Transactions — Export (Stock Out)

Same UI pattern as Import, but with a **customer selector** and **stock check**.

**To create an export:**
1. Select a customer
2. Add lines with product + quantity
3. Click "Complete Export"

**The stock check:** Before saving, the system computes:
```
stock = Σ(InputInfo.Count) − Σ(OutputInfo.Count)
```
If any product has insufficient stock, the whole export is rejected with a message like *"Insufficient stock for Gạo ST25. Available: 42."*

---

## 5. Inventory

Switch to the Inventory tab. It shows a computed report:
- Product ID, name, unit, supplier
- Total received (`TotalIn`), total shipped (`TotalOut`), and current `Stock`

**Search**: Type in the search box — it filters by product ID or name in real time. No button press needed (handled by `TextChanged` event).

**Refresh**: The Refresh button re-queries the database.

The stock is calculated per product using LINQ Sum over the InputInfo and OutputInfo tables — not a stored column.

---

## 6. Dashboard

Shows a text summary of key metrics:
- Total Products, Suppliers, Customers
- Total Stock In/Out, Current Stock
- Total Import/Export Value
- Transactions Today

All computed with LINQ aggregates (`Count`, `Sum`, `FirstOrDefault`).

---

## 7. File Export

From the Dashboard tab, click any of the 4 buttons:

| Button | Format | Method |
|--------|--------|--------|
| Export CSV | `Id,Name,Unit,Supplier,QRCode,BarCode` | `StringBuilder` → `File.WriteAllTextAsync` |
| Export Report | Formatted text table with totals | `StreamWriter` line-by-line |
| Export JSON | Indented JSON of all products | `JsonSerializer.Serialize` → `File.WriteAllTextAsync` |
| Import JSON | Reads JSON, skips duplicates by name | `File.ReadAllTextAsync` → `JsonSerializer.Deserialize` → add to DB |

A `SaveFileDialog` / `OpenFileDialog` lets the user pick the location.

---

## 8. Administration

Available only if the logged-in user's role contains "Admin".

**Roles:** Simple CRUD — name only. Admin, Manager, Staff, Warehouse Keeper.

**Users:**
- CRUD with display name, username, password, role assignment
- Duplicate username check on save
- Password field is optional when editing (leave blank to keep existing)
- Cannot delete yourself (UI-level guard)

---

## 9. Logout

Click the Logout button → the main window closes → the login dialog reappears. This is an infinite loop: Login → Main → Logout → Login → ... until you close the login window instead of logging in.

---

## 10. Slide Chapter Coverage

The project ticks boxes for each chapter:

| Chapter | What we used |
|---------|-------------|
| Ch3 OOP | 10 model classes with properties, FK navigation |
| Ch4 Collections | `List<T>`, `ObservableCollection<T>` for DataGrid binding |
| Ch5 Singleton | `WarehouseService.GetInstance()` with double-check locking |
| Ch6 LINQ / Events | `.Sum()`, `.Count()`, `.GroupBy()`, `FirstOrDefault()`; button click events |
| Ch7 WPF | `Window`, `TabControl`, `DataGrid`, `ComboBox`, `TextBox` |
| Ch8 EF Core | `DbContext` with 10 tables, FK relationships, `EnsureCreatedAsync` |
| Ch9 System.IO | `File.WriteAllTextAsync`, `StreamWriter` for report export |
| Ch10 JSON | `JsonSerializer.Serialize` / `Deserialize` |
| Ch11 async/await | Every data method is async — no blocking calls on UI thread |

---

## Quick Troubleshooting

| Symptom | Likely fix |
|---------|-----------|
| App launches but all DataGrids empty | Run `fakedata.sql` against `RalseiWarehouse` database, then restart app |
| Login says "Invalid username or password" | App seeded only `admin`/`admin` and `staff`/`staff` by default. fakedata.sql adds more users |
| Empty DataGrid after running fakedata.sql | The old code had a bug that could silently delete the DB. Update to latest commit and re-run fakedata.sql |
| Connection error at startup | Check `appsettings.json` — `Server=localhost,1433;User ID=sa;Password=123` must match your SQL Server |
