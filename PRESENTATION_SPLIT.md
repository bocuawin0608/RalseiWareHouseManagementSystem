# Presentation Split — Two-Person Code Walkthrough

## How to split: One explains the FRAMEWORK, the other explains the ENGINE.

---

# PERSON 1: "The Framework" — How the App Hangs Together

**Theme:** How does a WPF app with MVVM and DI actually start, route commands, and display data?

## Part 1-A: Application Startup — The DI Chain

**What you explain:**
The app doesn't have a `Main()` method. Everything starts from `App.xaml.cs:OnStartup`.

```
Double-click .exe
  │
  └─► App.OnStartup()
       ├─ Read appsettings.json → get connection string
       ├─ Build DI container (ServiceCollection)
       │    ├─ DbContext factory  (one fresh DB context per operation)
       │    ├─ WarehouseService   (SINGLETON — holds currentUser)
       │    └─ All 5 ViewModels   (TRANSIENT — new each time)
       ├─ BuildServiceProvider()
       └─ ShowLogin()
```

**Key to explain:** The difference between Singleton and Transient. Why the service MUST be singleton (it holds `currentUser` — the logged-in session. If transient, each ViewModel gets a different service and login is lost). Why ViewModels are transient (fresh state per screen).

**Code references:**
- `App.xaml.cs:18-22` — OnStartup, DI registration
- `App.xaml.cs:28-32` — ShowLogin, window transition via event

## Part 1-B: The Login Flow — From Button Click to Window Switch

**What you explain:**
Trace a login click all the way through:

```
Button click → LoginCommand → LoginViewModel.LoginAsync()
  → ExecuteAsync() [IsBusy guard, error handling]
    → service.LoginAsync(userName, password)
      → Return user or null
    → if null: throw "Invalid username or password."
    → if not null: Succeeded?.Invoke(user)
```

Then back in `App.xaml.cs`, the event lambda fires:
- Resolves all ViewModels from DI
- Creates `MainViewModel(user, masterData, transactions, inventory, administration)`
- Creates `MainWindow`, sets DataContext = shell
- Pre-loads data based on role (Manager? Admin?)
- Closes login window

**Key to explain:** The event pattern — `LoginViewModel` doesn't know about windows. It just fires an event. The `App` coordinates the transition. This is decoupling.

**Code references:**
- `LoginViewModel.cs:10-16` — the ViewModel
- `App.xaml.cs:31` — the event subscriber and window switch
- `ViewModelBase.cs:13-19` — ExecuteAsync (guard, error, finally)

## Part 1-C: MVVM Binding — How XAML Talks to C#

**What you explain:**
How does a button click in XAML reach a C# method?

```
[RelayCommand] private Task SaveUnitAsync()  ← C# method
         │
         ▼  (CommunityToolkit source generator creates)
         │
public IRelayCommand SaveUnitCommand { get; }  ← Auto-generated property
         │
         ▼  (XAML binding)
         │
<Button Command="{Binding SaveUnitCommand}" />  ← WPF resolves at runtime
```

The magic: `[RelayCommand]` on a `private` method generates a `public` `ICommand` property. The source generator writes the boilerplate. You write 3 lines, you get 30 lines of generated MVVM infrastructure.

Same for `[ObservableProperty]`:
```
[ObservableProperty] private string message;
  → Generates: public string Message { get; set; } with PropertyChanged notification
```

**Also explain DataContext propagation:**
```
MainWindow.DataContext = MainViewModel
  │
  ├─ TabItem → views:MasterDataView DataContext="{Binding MasterData}"
  │    └─ MasterDataView's DataContext = MainViewModel.MasterData
  │       └─ MasterDataViewModel now handles all bindings inside that tab
  │
  └─ TabItem → views:TransactionView DataContext="{Binding Transactions}"
       └─ Same pattern
```

**Code references:**
- `MainWindow.xaml:27-38` — TabControl with DataContext bindings
- `MasterDataViewModel.cs:16` — `[RelayCommand] public async Task LoadAsync()`
- `MasterDataView.xaml:10` — `Command="{Binding LoadCommand}"`

## Part 1-D: How Data Gets to the Grid — The Replace() Pattern

**What you explain:**
When data loads, it doesn't just set a property. It uses `Replace()`:

```
Replace(Units, await service.GetUnitsAsync())
  │
  ├─► target.Clear()       // Remove all rows from ObservableCollection
  │                         // WPF detects removal → DataGrid rows vanish
  │
  └─► foreach (var item in items)
       target.Add(item)    // Add each row back
                           // WPF detects additions → DataGrid rows appear
```

**Key to explain:** Why not just `Units = new ObservableCollection(list)`? Because WPF is watching the OLD collection reference. Reassigning breaks the binding. Clear+Add keeps the same collection reference alive.

**Code references:**
- `ViewModelBase.cs:25` — the Replace method
- `MasterDataViewModel.cs:16` — calling Replace for all 4 collections

## Part 1-E: The Design System — App.xaml Resources

**What you explain briefly:**
The entire app's look comes from one file. `App.xaml` defines:
- Color palette (Primary indigo tones, Neutral grays, Semantic red/green)
- Typography tokens (12px to 26px)
- Spacing tokens (4px to 32px)
- Global styles for every control type (TextBox, Button, DataGrid, TabControl, etc.)

Every view just uses `Style="{StaticResource Card}"` or `Background="{StaticResource Neutral100}"` — the same tokens everywhere. Change `App.xaml`, change the entire app's appearance.

**Code references:**
- `App.xaml:3-324` — the full design system

---

# PERSON 2: "The Engine" — How Data Actually Moves

**Theme:** How does data get from SQL Server to the screen and back? How are critical operations kept safe?

## Part 2-A: The Database Model — What's Actually Stored

**What you explain:**
The 10 database tables and their relationships:

```
User ─── N:1 ─── Role          (each user has one role)
Object ── N:1 ─── Unit         (each product has a unit of measure)
Object ── N:1 ─── Supplier     (each product has a supplier)
Input ─── 1:N ─── InputInfo ─── N:1 ─── Object
Output ── 1:N ─── OutputInfo ── N:1 ─── Object
OutputInfo ── N:1 ─── Customer
```

**Key to explain:** The two transaction tables (Input/InputInfo and Output/OutputInfo) form an immutable ledger. Stock is NEVER stored — it's always calculated as `SUM(InputInfo.Count) - SUM(OutputInfo.Count)`. This is the "ledger pattern."

**Code references:**
- `Models/` — all 10 entity files
- `Data/WarehouseDbContext.cs:35-185` — the Fluent API relationships

## Part 2-B: The DbContext Factory — Why Every Operation Is a Fresh Connection

**What you explain:**
```
IDbContextFactory<WarehouseDbContext>
  │
  └─► Every method call:
       await using var db = await factory.CreateDbContextAsync();
       // ... query or save ...
       // context auto-disposed at end of method
```

**Key to explain:** In web apps, you typically have one DbContext per request. In a desktop app, you could have the app open for hours. A single DbContext would accumulate tracked entities in memory and eventually slow down or crash. The factory creates a short-lived context per operation — open connection, do work, close connection. Clean slate every time.

**Code references:**
- `App.xaml.cs:21` — registering the factory
- `WarehouseService.cs:77` — `await using var db = await factory.CreateDbContextAsync()`
- `WarehouseService.cs:87` — `.AsNoTracking()` for read queries

## Part 2-C: Password Security — PBKDF2 from Scratch

**What you explain:**
When an admin creates a user with password `"mypassword"`:

```
SecurityHelper.HashPassword("mypassword")
  │
  ├─► Generate 16 random bytes (salt)
  ├─► Rfc2898DeriveBytes.Pbkdf2(
  │     password, salt, 210_000 iterations, SHA256, 32 bytes output)
  │
  └─► Format: "PBKDF2-SHA256$210000$base64salt$base64hash"
       └─ Stored as one string in User.Password column
```

When that user logs in:
```
SecurityHelper.VerifyPassword("mypassword", stored)
  │
  ├─► Parse stored string: algorithm, iterations, salt, expected hash
  ├─► Re-compute hash with same salt and iterations
  └─► FixedTimeEquals(actual, expected)  ← Timing-safe comparison!
```

**Key to explain TWO things:**
1. **Why 210,000 iterations?** Each iteration takes ~0.001ms. 210K iterations = ~200ms per login. For a user, that's instant. For a brute-force attacker trying millions of passwords, it makes each guess 100x slower.
2. **Why `FixedTimeEquals`?** Normal `==` compares byte by byte and stops at the first mismatch. An attacker can measure response time: a comparison that takes 0.1ms means the first byte is wrong; one that takes 0.2ms means the first byte is right. `FixedTimeEquals` always compares ALL bytes regardless of mismatch — no timing signal leaks.

**Password auto-upgrade:** If the stored password is plain text (old system), `LoginAsync` detects it, verifies directly, then overwrites with a hash. The user never knows their password was upgraded.

**Code references:**
- `Helpers/SecurityHelper.cs:13-18` — HashPassword
- `Helpers/SecurityHelper.cs:25-37` — VerifyPassword with FixedTimeEquals
- `WarehouseService.cs:80-82` — auto-upgrade on login

## Part 2-D: The Export Flow — Why Serializable Matters

**This is the most important part to get right.**

**What you explain:**
When someone clicks "Complete Export", here's the full chain:

```
1. CreateOutputAsync is called
2. Opens a SERIALIZABLE transaction
3. FOR EACH product in the export:
     input  = SUM(InputInfo.Count  WHERE ObjectId = productId)
     output = SUM(OutputInfo.Count WHERE ObjectId = productId)
     stock  = input - output
     if requested > stock → THROW "Insufficient stock"
4. If all lines pass → INSERT header + line items
5. COMMIT → release locks
```

**The race condition this prevents (explain with a concrete example):**

```
Scenario: Product X has 10 units in stock.

WITHOUT Serializable (bad):
  User A: reads stock = 10
  User B: reads stock = 10   (happens before A commits)
  User A: exports 8 → commits → actual stock is now 2
  User B: exports 8 → commits → negative stock created!

WITH Serializable (good):
  User A: BEGIN SERIALIZABLE TRANSACTION
  User A: reads stock = 10 → SQL Server LOCKS the rows for Product X
  User B: tries to read → BLOCKED, waits for A
  User A: exports 8 → commits → UNLOCKS → actual stock now 2
  User B: now reads stock = 2
  User B: tries to export 8 → "Insufficient stock. Available: 2."
```

**Key to explain:** `IsolationLevel.Serializable` isn't just "more safe." It specifically uses RANGE LOCKS — it locks not just the rows you read, but the gaps between them, preventing anyone from inserting or modifying anything in that range. This is the exact protection needed for inventory.

**Code references:**
- `WarehouseService.cs:133-140` — CreateOutputAsync with Serializable
- `WarehouseService.cs:138` — the stock check loop with SUM queries

## Part 2-E: The Inventory Calculation — Stock That Doesn't Exist

**What you explain:**
The inventory report creates a projection query:

```
FROM Object o
LEFT JOIN Unit u, Supplier s
SUBQUERY: (SELECT SUM(Count) FROM InputInfo WHERE ObjectId = o.Id)  AS Input
SUBQUERY: (SELECT SUM(Count) FROM OutputInfo WHERE ObjectId = o.Id) AS Output
Stock = Input - Output  (computed in memory after query)
```

**Key to explain:** There is NO `Stock` column anywhere in the database. Stock is a derived value — a function of the entire transaction history. You can NEVER have stock that disagrees with the ledger, because stock IS the ledger summed up.

This is the same pattern used by banks for account balances and by accounting systems for ledger balances. Mutating a counter column is fragile; summing immutable transactions is reliable.

**Code references:**
- `WarehouseService.cs:147-155` — GetInventoryAsync with projection

---

# THE SPLIT SUMMARY

| Person 1 "The Framework" | Person 2 "The Engine" |
|---|---|
| DI container setup | Database model (10 tables) |
| App startup → login → main window transition | DbContext factory pattern |
| Event-based navigation (Succeeded event) | Password hashing (PBKDF2, salt, FixedTimeEquals) |
| MVVM binding (RelayCommand, ObservableProperty) | Serializable transaction & race condition prevention |
| DataContext propagation (MainWindow → tabs) | Stock as derived value (SUM queries) |
| Replace() pattern for ObservableCollection | Export stock check logic |
| App.xaml design system | Service authorization guards |

**Person 1 explains:** "How does the app get from a double-click to a working window with buttons that do things?"

**Person 2 explains:** "When a button actually works, what happens to the data? How do we keep it safe?"
