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
  `pantry`, `sweets`, `refrigerated`, `electronics`. Each has a **color**, `unlockCost`, `valueMult`
  (per-item profit vs common), and `upgradeBase`. Order = unlock progression (cheap → premium).
- **`StoreEditor.cs`** — edit mode (round **+** button toggles it; hard-pauses via timeScale). The left
  **CATALOG** panel: a **Section:** brush (cycles unlocked sections, color-tinted), a **Tool: Move / Paint
  floor / Erase floor** cycle, an **Unlock: next item** button, and the scrollable item list (per-item
  `StandCost(it) = it.cost × 1.18^(shelves placed)`). Drag fixtures (shop-local 0.5 snap), `Q/E` rotate
  the whole shop ±45°, `R` rotates the selected item.
- **`HUD.cs`** — IMGUI HUD: money/income/stats (top-left), clock+day (top-centre, NEW DAY button when
  closed), and the right panel = **UPGRADES** + a **SECTIONS** block (per-section Unlock or `Lv N` upgrade
  buttons with color swatches, plus an **All +1** button). Auto-attaches `WorldIcons` in `Start`.
- `Customer.cs` (state machine: ToShelf/ToQueue/InLine/Leaving, grid pathfinding, anti-trap escape),
  `Checkout.cs` (snaking queue), `CustomerSpawner.cs`, `NavGrid.cs` (A* + `KeepOnly` pad confinement),
  `Shelf.cs` (stock + `catalogId` + `section`), `Decor.cs` (marker), `Producer*.cs` **removed**.
- `StoreLayoutData.cs` — save schema: `XZ{x,z,rot,id,sec}`, `PaintCell{i,j,sec}`,
  `LayoutData{shopRotation, unlocked, checkout, shelves[], dividers[], decor[], paint[]}` (JsonUtility →
  PlayerPrefs key `storeLayout`).

## Key mechanics
- **Sections = painted floor zones, and shelves inherit from the floor.** In edit mode you Paint the floor
  with an unlocked section's color (1×1 cells, drawn under `ShopRoot` with a `Sprites/Default` material).
  A shelf's **effective section = the floor cell it stands on** (`SectionAt` → `RefreshShelfSection`;
  `RefreshAllShelfSections` runs after placing/moving a shelf, after a paint stroke, and after load). So
  repainting a zone re-tags + recolors the shelves on it. **Strategy:** diversify (multi-color floor, spread
  section upgrades) vs **specialize** (paint the whole floor one color → all shelves that section → dump
  upgrades into it).
- **Income:** each item a customer buys pays `Profit × SectionMult(shelf.section)`, accumulated into a
  basket and paid at checkout (`Customer.basketValue`). `Profit = (ItemPrice − ItemCost) × IncomeMult`;
  `SectionMult = section.valueMult × 1.15^level`. `Economy.Money` rises live; `Sim.AvgSectionMult()` feeds
  the HUD income estimate.
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
- **PlayerPrefs keys:** `money/earned/served/lost/lastQuit/lastInc`, `storeLayout`, `lvl_<id>` (upgrades),
  `sec_<id>` + `seclvl_<id>` (sections), `day`. Orphaned `prod_*`/`prodMult` may linger from the removed
  producer system — harmless. `F12` deletes all.
- **New `.cs` files need their `.meta`** committed — Unity only generates `.meta` once it has focused/imported
  (or after a batch compile). Commit the `.meta` alongside the script so GUIDs are tracked.

## AI asset generation + shared library (same rules as the other projects)
Backgrounds/art are generated with **Higgsfield** (e.g. `bg_space_nebula.png` in `Assets/Art/`, runtime copy
in `Assets/Resources/`). ⚠️ **MANDATORY:** the moment credits are spent, mirror the asset (portable form,
no `.meta`) to `C:\Users\gorisektj\Desktop\icarus\custom_assets` **in the same turn**, and log a row in
`Docs/AssetGenLog.md`.

## Design docs
`Docs/EconomyDesign.md` (the fusion design + validated idle curve + the vertical-integration / locations-tree
/ zoom roadmap), `Docs/GDD.md`, `Docs/Galaxy_Grocery_Economy.csv`.

## Roadmap (next)
Per-section **products** (price/demand/restock-cost, maybe spoilage for Refrigerated/Produce) — sections are
currently value-tier + color only. Then the supply layer (stock/order/storage/margin), supplier facilities +
transport, the **locations tree / Galaxy-Map zoom**, living-world milestone unlocks (garage at 20 cashiers →
vans → trucks), and prestige.

## Don't commit / push unless asked.
