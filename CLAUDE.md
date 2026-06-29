# CLAUDE.md

Guidance for Claude Code working in this repo.

## What this is
**Galaxy Grocery** — a Unity game fusing a **supermarket simulator** with a **Cookie-Clicker-style
idle/clicker**. You run a space supermarket hovering on a pad in a nebula: customers spawn, shop the
shelves, queue, and check out; you spend the profit on upgrades (cashiers, restockers, managers…),
unlock store **sections/departments**, lay out **stands + decorations** in an edit mode, and grow the
shop visually over a long idle curve. Unity **6.3 LTS (6000.3.5f1)**, **Built-in Render Pipeline**,
namespace `IdleSim`. GitHub: `https://github.com/TadejJureGorisek/idle-simulator` (binary art via Git LFS).

## ⚠️ The scene is code-generated — don't hand-edit it
`Assets/Scenes/Main.unity` is a **generated artifact**. The source of truth is `SceneBuilder.Build()`.
- `SceneBuilder.Build()` runs two ways: a **runtime fallback** via `[RuntimeInitializeOnLoadMethod]`
  (so just pressing Play rebuilds the scene), and the editor menu **`Idle Simulator → Build Default
  Layout (Greybox)`** (`LayoutBuilder.cs`) which does NewScene → Build → saves `Main.unity`.
- **Big `Main.unity` diffs are normal** (every rebuild reshuffles object order). Not a red flag.
- To change the world, edit the scripts, not the scene.

## UI is IMGUI (OnGUI), not uGUI
There is **no uGUI/Canvas** package — only built-in modules. Every panel/HUD is drawn in `OnGUI`
with cached `GUIStyle`s and code-generated textures. Don't reach for `UnityEngine.UI` / prefabs;
follow the existing OnGUI pattern (see `HUD.cs`, `StoreEditor.cs`).

## Verifying changes (I can't drive Play)
I cannot compile-test inside an open editor or drive Play mode. Two real options:
- **Batch compile when Unity is CLOSED** (no `Temp/UnityLockfile`, no `Unity.exe` process):
  ```
  "C:\Program Files\Unity\Hub\Editor\6000.3.5f1\Editor\Unity.exe" -batchmode \
    -projectPath "<proj>" -quit -logFile "<log>"
  ```
  then grep the log for `error CS`. A rebuilt `Library/ScriptAssemblies/Assembly-CSharp.dll`
  (newer than the edits) + zero `error CS` = clean compile. **Batch does NOT run `SceneBuilder`**
  (that's `RuntimeInitializeOnLoadMethod` = Play only), so it verifies *compilation*, not runtime.
- **Otherwise ask the user to Play and paste Console errors.**
- ⚠️ Per-project logs live in `<proj>/Logs/` (AssetImportWorker*.log) — reliable. The shared
  `%LOCALAPPDATA%\Unity\Editor\Editor.log` is whichever editor wrote last; if multiple projects are
  open it may show a *different* project. Each project locks only its own folder.
- **`F12` (in Play) wipes all PlayerPrefs** — use it to reset to a fresh game.

## Code layout (`Assets/Scripts/`, namespace `IdleSim`)
- **`Sim.cs`** — the central hub (singleton). Floor footprint (`FloorWidth/Depth/Center`, `ApplyFloorSize`),
  whole-shop rotation (`ShopRoot`/`ShopRotation`/`ApplyShopRotation`, with `ShopLocal`/`ShopWorld`),
  the **day/shift** system (06:00 start, `ShiftHours` 8 → up to 24, `ShiftRealSeconds` 1200, mess/cleaning
  downtime, `NewDay`, auto-day at 24/7), **upgrades** (`Upgrades` list + `Buy`), fixtures
  (`AddStand`/`AddShelf`/`AddDecor`/`AddDivider`, `Shelves`/`DecorItems`/`Dividers`), A* nav
  (`RebuildNav` + `NavGrid` confined to the pad), **sections** + **floor paint** (below), and **save/load**
  (`SaveLayout`/`LoadLayout`, `SaveLevels`/`LoadLevels`, `SaveSections`/`LoadSections` via PlayerPrefs).
- **`Economy.cs`** — money, lifetime stats, offline earnings. ⚠️ The balance is the **field** `public double Money`.
  The K/M/B money **formatter** is the static `Economy.Fmt(double)` — it is NOT called `Money` because that
  collides with the field (**CS0102** "already contains a definition for 'Money'"). `HUD` has its own private
  instance formatter also (historically) named `Money`; that's fine (different class).
- **`Catalog.cs`** (`CatalogItem` + `Catalog`) — the placeable catalog: **10 stands** (functional shelves,
  varied footprint/capacity, **per-item `cost`**) + **30 decor** (cosmetic, free). `Catalog.Build(item)`
  is the factory (stands get a `Shelf` + a sized Cube body named `"Body"`; decor gets a `Decor` marker +
  primitive shapes). Items unlock **one-by-one** in list order; **index 0 = `st_small` (Small Shelf, 1×1) is
  the unlocked starter**, `st_basic` (Basic Shelf, 1×2) is index 1 and intentionally pricier. Helpers
  `St(id,name,w,d,cap,cost)` / `Dc(id,name,shape,size,color)`.
- **`Sections.cs`** (`Section` + `Sections`) — the 6 **departments**: `common` (free/unlocked), `veg`,
  `pantry`, `sweets`, `refrigerated`, `electronics`. **Per-section products:** `sell`/`cost` (margin),
  `demand` (customer pull), `perishable` (Produce + Cold spoil), plus `color`, `unlockCost`, `upgradeBase`.
  `Sections.Short(id)` = compact HUD name. Order = unlock progression (cheap → premium).
- **`StoreEditor.cs`** — edit mode (round **+** button toggles it; hard-pauses via timeScale). The left
  **CATALOG** panel: a **Section:** brush (cycles unlocked sections, color-tinted), a **Tool: Move / Paint
  floor / Erase floor** cycle, an **Unlock: next item** button, and the scrollable item list (per-item
  `StandCost(it) = it.cost × 1.18^(shelves placed)`). Drag fixtures (shop-local 0.5 snap), `Q/E` rotate
  the whole shop ±45°, `R` rotates the selected item.
- **`HUD.cs`** — IMGUI HUD: money panel (coin icon, money, green income, served/lost/queue, milestone bonus),
  clock + **game-speed control** (`[-] x [+]`, `Sim.GameSpeed`→`Time.timeScale`) + **End Day** / **NEW DAY**,
  and the right panel = **UPGRADES** + **SECTIONS** as **flat translucent rows** (flush, no gaps, thin
  dividers; section names tinted by department; amber costs). Auto-attaches `WorldIcons` + `Franchise` +
  `GalaxyMap` in `Start`.
- **`UISkin.cs`** — applies the Higgsfield holographic-glass tiles to the built-in IMGUI box/button styles
  globally (9-slice, translucent). Call `UISkin.EnsureApplied()` atop each `OnGUI`. Tiles in `Assets/Resources`
  (`ui_panel`/`ui_button`); `Assets/Editor/UITextureImporter.cs` keeps them + the icons at 256/uncompressed.
- **`Franchise.cs`** — prestige: sell the chain for **Franchise Points** (FP = √(runEarned/100)); each FP =
  +2% income forever (`Franchise.Mult`) + 6 one-time perks. Bottom-left button + panel. `Sim.HardResetRun`
  wipes the run (FP / perks / locations persist).
- **`Milestones.cs`** — achievement milestones (served + lifetime-earned thresholds), each +2% global income
  (`Sim.MilestoneMult`); shown in the money panel. Derived from monotonic stats → no save needed.
- **`Locations.cs` + `GalaxyMap.cs`** — the connected-locations meta-game + its zoom-out map screen. 5
  locations (Warehouse/Fabricator/Orbital Farm/Fusion Reactor/Hypermarket), built + upgraded ON the map
  (sequential unlock); effects feed the store (`SupplyCostFactor`, `SpoilFactor`, `LocationMult`). Bottom-left
  **GALAXY MAP** button → store hub + orbiting nodes + supply lines (brighten with level) + build/upgrade panel.
- `Customer.cs` (state machine; `basketValue` accrues per item by section), `Checkout.cs` (snaking queue),
  `CustomerSpawner.cs`, `NavGrid.cs` (A* + `KeepOnly`), `Shelf.cs` (stock + `catalogId` + `section`,
  per-section restock cost), `Decor.cs`, `Producer*.cs` **removed**.
- `StoreLayoutData.cs` — save schema: `XZ{x,z,rot,id,sec}`, `PaintCell{i,j,sec}`,
  `LayoutData{shopRotation, unlocked, checkout, shelves[], dividers[], decor[], paint[]}` (PlayerPrefs `storeLayout`).

## Key mechanics
- **Sections = painted floor zones, and shelves inherit from the floor.** In edit mode you Paint the floor
  with an unlocked section's color (1×1 cells, drawn under `ShopRoot` with a `Sprites/Default` material).
  A shelf's **effective section = the floor cell it stands on** (`SectionAt` → `RefreshShelfSection`;
  `RefreshAllShelfSections` runs after placing/moving a shelf, after a paint stroke, and after load). So
  repainting a zone re-tags + recolors the shelves on it. **Strategy:** diversify (multi-color floor, spread
  section upgrades) vs **specialize** (paint the whole floor one color → all shelves that section → dump
  upgrades into it).
- **Income (`Sim.ItemValue`):** each item a customer buys yields
  `EffMargin × SectionMult × IncomeMult × Franchise.Mult × MilestoneMult × LocationMult`, accrued into a basket
  paid at checkout (`Customer.basketValue`). `EffMargin = sell − cost×SupplyCostFactor` (warehouse discount);
  `SectionMult = 1.15^sectionLevel`; `IncomeMult = 1 + 0.20×managers` (cap 12); customers pick shelves
  **weighted by section demand**; `Sim.AvgItemValue()` feeds the HUD estimate.
- **The stacking multipliers** are the long curve: section levels (×1.15 value, **cost ×1.35** so it stretches —
  ⚠️ cost growth MUST exceed the value step or income runs away; old 1.15/1.15 hit $1T in 33 min), Manager
  (finite +240%), Milestones (+2% each), Franchise FP (prestige, +2% each), Locations. Tuned via greedy-player
  sims (scratchpad `simgg*.ps1`): ~$1M in ~3 h, ~$1B in ~3 days. **Advertising** = the customer-rate upgrade
  (~20→73+/min over levels, capped ~150/min).
- **Catalog unlock + escalating cost:** `Sim.UnlockedItems` (1 at start) gates the catalog; each placed
  stand makes the next pricier (`× 1.18^count`) so the shop grows slowly. A fresh game starts with **2 small
  shelves** (`SceneBuilder` places `st_small` ×2) — only on a fresh save; an existing `storeLayout` restores
  that instead (F12 to reset).

## Gotchas
- **`Economy.Money` is a field, not the formatter** — use `Economy.Fmt(double)` for display (see CS0102 above).
- **Floor-derived sections need the floor restored first** — `LoadLayout` rebuilds shelves + decor, restores
  `paint`, THEN calls `RefreshAllShelfSections()`.
- **Wing/scene coords:** fixtures live under `ShopRoot` (rotatable); convert with `ShopLocal`/`ShopWorld`.
  The edit grid + floor paint also sit under `ShopRoot` so they spin with the shop.
- **PlayerPrefs keys:** `money/earned/runEarned/served/lost/lastQuit/lastInc`, `storeLayout`, `lvl_<id>`
  (upgrades), `sec_<id>`+`seclvl_<id>` (sections), `loc_<id>` (locations), `fp`+`perk_<id>` (franchise), `day`.
  Orphaned `prod_*` (old producers) harmless. `F12` deletes all.
- **9-slice UI tiles:** the neon must reach the very edge (the tiles are auto-cropped to the bright neon bbox)
  or stacked elements show a gap; the tile needs a SMALL corner radius or the 9-slice smears the corner. List
  rows are **flat translucent** (not glass tiles) so they stack gapless. ⚠️ OnGUI does NOT render in batch, so
  the way to preview the HUD headlessly is a GDI 9-slice composite over the nebula at the REAL rects (scratchpad
  `*.ps1`) — a standalone mock with made-up rects hides real layout/overflow bugs.
- **New `.cs` files need their `.meta`** committed — Unity generates `.meta` on import / batch compile. Commit
  the `.meta` alongside the script so GUIDs are tracked.

## AI asset generation + shared library (same rules as the other projects)
Backgrounds/art are generated with **Higgsfield** (e.g. `bg_space_nebula.png` in `Assets/Art/`, runtime copy
in `Assets/Resources/`). ⚠️ **MANDATORY:** the moment credits are spent, mirror the asset (portable form,
no `.meta`) to `C:\Users\gorisektj\Desktop\icarus\custom_assets` **in the same turn**, and log a row in
`Docs/AssetGenLog.md`.

## Design docs
`Docs/EconomyDesign.md` (the fusion design + validated idle curve + the vertical-integration / locations-tree
/ zoom roadmap), `Docs/GDD.md`, `Docs/Galaxy_Grocery_Economy.csv`.

## Roadmap
**Done:** holographic UI skin + icons/logo, prestige (Franchise), per-section products (margin/demand/spoilage),
achievement milestones, connected locations + Galaxy Map (v1), economy tuning, game-speed + End-Day controls.
**Next:** richer locations (per-location sub-trees; animated supply shuttles that ramp trickle→torrent), a
main-menu/title screen (`Assets/Resources/logo_galaxygrocery` is ready), more sections, a research tree,
living-world milestone unlocks (garage at 20 cashiers → vans → trucks), and balance passes from real Play.

## Don't commit / push unless asked.
