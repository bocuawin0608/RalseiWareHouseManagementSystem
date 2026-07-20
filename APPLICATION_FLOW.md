# Application Flow

```

┌─────────────────────────────────────────────────────────────────────────────┐
│                      App.OnStartup()                                        │
│  (WpfApp1\App.xaml.cs:14)                                                   │
│                                                                             │
│  1. Set ShutdownMode = OnExplicitShutdown                                   │
│  2. Load appsettings.json → get connectionString                            │
│  3. WarehouseService.GetInstance(connectionString)                          │
│  4. service.EnsureDatabaseAsync()                                           │
│     → If no DB: Delete, recreate, seed Roles (Admin, Staff) & Users         │
│     → If DB exists: skip seeding                                           │
│                                                                             │
│  ┌─ LOOP ──────────────────────────────────────────────────────────────┐   │
│  │                                                                      │   │
│  │  LoginWindow.ShowDialog()                                            │   │
│  │       ↓                                                              │   │
│  │  ┌─ BtnLogin_Click ──────────────────────────────────────────────┐   │   │
│  │  │  service.AuthenticateAsync(username, password)                 │   │   │
│  │  │    → db.Users.Include(Role).FirstOrDefaultAsync(u,p match)     │   │   │
│  │  │                                                                  │   │   │
│  │  │  ┌── Success ──→ LoginSucceeded=true, AuthenticatedUser=user     │   │   │
│  │  │  │               Close() → exit dialog                          │   │   │
│  │  │  └── Fail ──→ Show "Invalid username or password"               │   │   │
│  │  │                Stay in dialog (retry)                           │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  │       ↓                                                              │   │
│  │  If (!LoginSucceeded || AuthenticatedUser is null) → Shutdown()      │   │
│  │       ↓                                                              │   │
│  │  MainWindow(authenticatedUser).ShowDialog()                          │   │
│  │       ↓                                                              │   │
│  │  Constructor:                                                        │   │
│  │    - Set welcome text                                                │   │
│  │    - Hide Admin tab if role ≠ Admin                                 │   │
│  │    - Bind ImportLine/ExportLine DataGrids                            │   │
│  │    - On Loaded: LoadMasterDataAsync() + LoadTransactionDataAsync()   │   │
│  │       ↓                                                              │   │
│  │  ┌─ Tab Switch (TabMain_SelectionChanged) ───────────────────────┐   │   │
│  │  │  tabInventory → LoadInventoryAsync()                          │   │   │
│  │  │  tabDashboard → LoadDashboardAsync()                          │   │   │
│  │  │  tabAdmin    → LoadAdminDataAsync()                           │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  │       ↓                                                              │   │
│  │  If (LoggedOut from BtnLogout_Click) → Continue LOOP (re-login)     │   │
│  │  Else → Shutdown()                                                   │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Per‑Tab Flow

### 1. Master Data (Units, Suppliers, Customers, Products)

```
CRUD Pattern (all 4 entities follow the same shape):

User clicks entity row in DataGrid
  → SelectionChanged handler
    → Populate text fields from selected entity
  → User edits fields
  → User clicks Save/Delete

── Save ─────────────────────────────────────────────────
  MainWindow.BtnSaveXxx_Click()
    → Validate inputs (name required, combo selected, etc.)
    → If no row selected: create new entity
    → If row selected: update existing
    → service.SaveXxxAsync(entity)
      → If Id == 0: db.Xxx.Add(entity)           [INSERT]
      → Else: find existing, copy fields          [UPDATE]
      → db.SaveChangesAsync()
    → LoadMasterDataAsync() (refresh all DataGrids)

── Delete ───────────────────────────────────────────────
  MainWindow.BtnDeleteXxx_Click()
    → Validate selection
    → service.DeleteXxxAsync(id)
      → db.Xxx.FindAsync(id) → Remove → SaveChangesAsync
    → LoadMasterDataAsync()

── New ──────────────────────────────────────────────────
  BtnNewXxx_Click()
    → Clear selection field, reset text fields
    → (next Save will create new)
```

### 2. Transactions (Import / Export)

```
── IMPORT ─────────────────────────────────────────────────
  BtnAddImportLine_Click()
    → _importLines.Add(new ImportLine())
    → DataGrid auto-binds, user picks Product/Count/Prices per row
  BtnRemoveImportLine_Click()
    → _importLines.Remove(line) via Button.Tag binding
  BtnCompleteImport_Click()
    → service.CreateImportAsync(lines)
      → Validate: count>0, prices≥0, no duplicate products
      → Build Input header with line items (InputInfos)
      → db.Inputs.Add → SaveChangesAsync
    → Clear lines, show success message
    → Refresh grids

── EXPORT ─────────────────────────────────────────────────
  (Same pattern as Import)
  BtnCompleteExport_Click()
    → Validate customer selected
    → service.CreateExportAsync(customerId, lines)
      → Same validation as Import
      → Stock check: Σ(InputInfo.Count) − Σ(OutputInfo.Count) per product
      → If insufficient stock → throw
      → Build Output header with line items (OutputInfos)
      → db.Outputs.Add → SaveChangesAsync
```

### 3. Inventory

```
Tab switch to Inventory tab (or search/refresh)
  → LoadInventoryAsync()
    → service.GetInventoryReportAsync(search?)
      → db.Objects
        .Where(search filter on Id or DisplayName)
        .Select({ Product, Unit, Supplier, TotalIn, TotalOut, Stock })
      → returns anonymous object list
    → Bind to DataGrid

TxtInventorySearch_TextChanged + BtnInventoryRefresh_Click
  → LoadInventoryAsync()  (re-query with current filter)
```

### 4. Dashboard

```
Tab switch to Dashboard tab
  → LoadDashboardAsync()
    → service.GetDashboardStatsAsync()
      → LINQ aggregates: Count, Sum, Average (via Sum/Count)
      → Returns Dictionary<string, object> with:
        Total Products, Suppliers, Customers
        Total Stock In/Out, Current Stock
        Total Import/Export Value
        Transactions Today
    → Display as formatted text

── CSV Export ────────────────────────────────────────────
  BtnExportCsv_Click()
    → SaveFileDialog
    → service.ExportToCsvAsync(path)
      → Query all products with Unit/Supplier
      → StringBuilder → CSV header + rows
      → File.WriteAllTextAsync

── Report Export (TXT) ──────────────────────────────────
  BtnExportReport_Click()
    → SaveFileDialog
    → service.ExportReportToFileAsync(path)
      → Query products with stock totals (LINQ projection)
      → StreamWriter → formatted report
      → Write line by line

── JSON Export ───────────────────────────────────────────
  BtnExportJson_Click()
    → SaveFileDialog
    → service.ExportToJsonAsync(path)
      → Query products with Unit/Supplier
      → JsonSerializer.Serialize with indented option
      → File.WriteAllTextAsync

── JSON Import ───────────────────────────────────────────
  BtnImportJson_Click()
    → OpenFileDialog
    → service.ImportFromJsonAsync(path)
      → File.ReadAllTextAsync
      → JsonSerializer.Deserialize<List<Product>>
      → For each product (skip if name exists):
        → Clear nav properties, add to db
      → db.SaveChangesAsync
      → Refresh master data
```

### 5. Administration

```
── Roles ── (Same CRUD as Master Data entities)
  Load/New/Save/Delete via service.GetRolesAsync / SaveRoleAsync / DeleteRoleAsync

── Users ──
  Load: service.GetUsersAsync() → db.Users.Include(Role)
  SaveUserAsync(user, isNew):
    → isNew=true:  check dupe username, db.Users.Add
    → isNew=false: find existing, update fields
                   (only update password if non-empty)
  DeleteUserAsync(id): find → Remove → Save
    Guard: cannot delete yourself (UI-level check in MainWindow)
```

---

## Slide Chapter Mapping

| Chapter | Concept | Where Used |
|---------|---------|------------|
| Ch3 | OOP (classes, properties, inheritance) | All 10 Model classes (`Models/*.cs`) |
| Ch4 | Collections (List, ObservableCollection) | `_importLines`, `_exportLines`, all `List<T>` returns in Service |
| Ch5 | Singleton pattern | `WarehouseService.GetInstance()` with double-check locking |
| Ch6 | LINQ (Count, Sum, Where, Select, GroupBy) & events | `GetDashboardStatsAsync`, `GetInventoryReportAsync`, all query methods; button click events |
| Ch7 | WPF (Window, DataGrid, TabControl, Binding) | `LoginWindow.xaml`, `MainWindow.xaml` |
| Ch8 | Entity Framework Core (DbContext, relationships) | `WarehouseDbContext.cs` — full 10‑table mapping with FK constraints |
| Ch9 | System.IO (File, StreamWriter) | `ExportToCsvAsync`, `ExportReportToFileAsync`, `ImportFromJsonAsync` |
| Ch10 | JSON (System.Text.Json: Serialize/Deserialize) | `ExportToJsonAsync`, `ImportFromJsonAsync` |
| Ch11 | async/await (Task, async void events) | All `async Task` / `async void` methods everywhere |

---

## Alternative / Error Flows

| Situation | Path |
|-----------|------|
| **DB doesn't exist** | `EnsureDatabaseAsync()` tries `Users.AnyAsync()` → catches exception → `EnsureDeletedAsync()` → `EnsureCreatedAsync()` → seed data |
| **DB exists** | `Users.AnyAsync()` succeeds → `EnsureCreatedAsync()` is no-op → skip seeding |
| **Login fails** | `AuthenticateAsync` returns null → `txtMessage` shows error → dialog stays open for retry |
| **Login blank fields** | `AuthenticateAsync` returns null at the null/whitespace check → "Please enter username and password" |
| **Login Exception** | Catch → display error in `txtMessage` (dialog stays open) |
| **App Startup Exception** | Catch → `MessageBox.Show(error)` → `Shutdown()` |
| **User closes Login window** (X button) | `ShowDialog()` returns → `LoginSucceeded` is false → `Shutdown()` |
| **User clicks Logout** | `BtnLogout_Click` → `LoggedOut=true` → `Close()` → App loop continues → re‑show Login |
| **User closes Main window** (X button) | `ShowDialog()` returns → `LoggedOut` is false → `Shutdown()` |
| **Import: empty lines** | `CreateImportAsync` → throws "Add at least one product" |
| **Import: duplicate product** | LINQ `GroupBy(l => l.Product.Id).Any(g.Count > 1)` → throws "A product may appear only once" |
| **Import: negative price** | `lines.Any(l.InputPrice < 0)` → throws "Prices cannot be negative" |
| **Export: insufficient stock** | `CreateExportAsync` → computes `stock = totalIn − totalOut` → if `line.Count > stock` → throws "Insufficient stock for {product}" |
| **Save User: dupe username** | `SaveUserAsync` → `db.Users.AnyAsync(x.UserName == input)` → throws "Username already exists" |
| **Delete self** | MainWindow checks `_selectedUser.UserId == _currentUser.UserId` → MessageBox "Cannot delete yourself" |
| **JSON Import: same name** | `ImportFromJsonAsync` → `AnyAsync(x.DisplayName == p.DisplayName)` → skip that product |
| **JSON Import: invalid file** | `JsonSerializer.Deserialize` returns null → throws "Invalid JSON file" |
| **CRUD: entity not found** | `FindAsync` returns null → throws "Xxx not found" (re‑catch in UI as `ShowError`) |
| **CRUD: missing required field** | Service throws `ArgumentException("Xxx name is required")` → caught by `ShowError` |
