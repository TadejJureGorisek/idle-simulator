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
  - **Animated supply shuttles** stream from each built location INTO the store (`GalaxyMap.Shuttles`, cyan
    streak + bright head, `Time.unscaledTime` so they run regardless of game-speed/pause). Count + speed +
    streak length scale with the location's level → a visible *trickle (Lv1) → torrent (high Lv)*.
  - **Animated store ↔ map ZOOM** (`GalaxyMap`, `zoomT`/`zoomDir`/`StartOpen`/`StartClose`): clicking GALAXY MAP
    smoothly zooms the live store OUT into its MAIN STORE node; clicking the store node (or CLOSE) zooms back IN.
    A hidden **`zoomCam`** MIRRORS the main camera (same pos/rot/ortho/aspect, full `cullingMask`) into a
    screen-sized **`storeRT`**, so drawing that RT full-screen is pixel-identical to the live view — seamless at
    both ends. OnGUI lerps the RT's draw-rect between full-screen and `HubInner()` (smoothstep, `ZoomDur` 0.5s).
    The real marketplace is hidden during the map (`SetWorldHidden`: main `cullingMask` → only the nebula backdrop
    on layer 30) and restored when the zoom returns to 0; the nebula stays as the map backdrop. The network UI
    (`DrawMap`: lines/shuttles/nodes/labels) is drawn only once `fullyOpen`. ⚠️ All runtime — Play-only to see
    (headless can't render the RT/animation). `MapOpen` (modal flag) is true for the whole transition.
    The `SpaceBackdrop` quad must stay named that.
  - **Per-location floorplan art** (`Assets/Resources/loc_<id>.png` + `loc_<id>_empty.png`, AI top-down floorplans):
    `LocationDef.Art(level)` returns the EMPTY room at level 0 and the populated facility once built. Each node on
    the map shows its floorplan (cropped); clicking a node ZOOMS into its stage.
  - **Every node is a PLAYABLE STAGE — one data-driven engine for all 5** (`Stages.cs` = data, `StageManager.cs` =
    runtime, `GalaxyMap.DrawZoom` = UI). Each location is a **3-step task chain** (work arrives at step 0 → `buf0` →
    `buf1` → shipped). **Each step is done by hand (click the step's button), then automated by hiring its employee**;
    plus 2 generic upgrades (Faster intake, Bigger storage). Sustained shipping keeps a live **efficiency** (0..1, decays
    when idle, bumps on each shipment) and that efficiency scales the location's boost to the main store. The chains:
    - **Warehouse** INTAKE→STORAGE→DISPATCH (Clerk/Stocker/Loader) → −cost of goods
    - **Fabricator** INTAKE→FABRICATE→PACK (Handler/Machinist/Packer) → +income
    - **Orbital Farm** PLANT→TEND→HARVEST (Planter/Botanist/Picker) → −spoilage
    - **Fusion Reactor** FUEL→REACT→OUTPUT (Fueler/Operator/Dispatcher) → +income
    - **Hypermarket** STOCK→SELL→FULFIL (Stocker/Cashier/Runner) → +income
  - **Boost math** (`StageManager`): effect kind in `StageDef` — `cost`/`spoil` → `Lerp(1, (1-perLevel)^InfraLevel, eff)`;
    `income` → `1 + perLevel*InfraLevel*eff`. `InfraLevel = LocLev + upArrival + upCapacity`. `Sim.SupplyCostFactor` /
    `SpoilFactor` / `LocationMult` read `StageManager.Instance.Factor(id)`/`IncomeMult(id)` when the location is built
    (else the flat per-level fallback). StageManager runs every frame (employees work even when you're not looking) and
    saves per-location under `st_<id>_*` PlayerPrefs.
  - **Construct** a node = `Sim.BuildLocation(id)` (charges `LocCost`, sets level 1 → unlocks the next location + flips
    the node to the populated floorplan). Sequential lock applies. The map node shows the empty floorplan until built.
    ⚠️ PLAY-only feel/balance (IMGUI + the sim don't run headless; GDI previews only confirm layout). Tunables in
    `StageManager` (rates `Rate0/1/2`, `EffDecay`/`EffBump`, `Patience`, costs) + per-location `perLevel`/`arrivalBase`
    in `Stages.cs`. To add/edit a node's loop, just edit its `StageDef` — no per-location code.
- `Customer.cs` (state machine; `basketValue` accrues per item by section), `Checkout.cs` (snaking queue),
  `CustomerSpawner.cs`, `NavGrid.cs` (A* + `KeepOnly`), `Shelf.cs` (stock + `catalogId` + `section`,
  per-section restock cost), `Decor.cs`, `Producer*.cs` **removed**.
- **No-overlap placement** (`Sim.FindFreeSpot`): buying a shelf scans the floor grid for the first clear
  footprint (`CollectFootprints` AABBs of shelves+decor+counters, rotation-aware) so fixtures never overlap.
  `StoreEditor.PlaceItem` uses it for stands.
- **Counters scale with cashiers** (`Sim.SyncCounters`): one checkout counter per cashier (the original till is
  #1); extras spawned via `BuildCounterVisual` + `FindFreeSpot`. Called on hire + load. (Queue logic is still the
  single `Checkout`; the extra counters are physical lanes a cashier mans.)
- **Employee NPCs** (`Employee.cs` + `Sim.SyncEmployees`/`SyncRole`/`SpawnEmployee`): cashiers/restockers/cleaners/
  managers are walking agents synced to the upgrade counts. Cashiers stand behind their counter (face the queue);
  others walk→idle to random floor points (`Sim.RandomFloorPoint`). Colliders disabled (don't block nav).
- **Meshy NPC characters** (customers + employees) — `NpcVisuals.Attach(root, key, height)` wears an imported
  rigged FBX (`Assets/Resources/npc_customer.fbx` = Meshy-5, `npc_employee.fbx` = Meshy-6→remeshed), textured at
  runtime from `npc_<key>_textures/base_color`, scaled to height with feet on the floor; falls back to the
  sphere/capsule primitive if absent. Pipeline: text→3D (t-pose) → refine (PBR) → **rig (walk+run free)** →
  download `basic_animations.walking_fbx_url`. Imported as **Legacy animation** (`Assets/Editor/NpcModelImporter`
  → `animationType=Legacy`, materials None) so `NpcVisuals` loops the walk clip via a plain `Animation` component
  (no AnimatorController asset). ⚠️ **Meshy-6 meshes exceed the 300k-face rig limit → must `meshy_remesh` first**
  (the employee was remeshed to 120k; its texture atlas changes, so re-pull the remesh's base_color). ⚠️ Walk
  loops ALWAYS for now (no idle yet → standing cashiers march) — idle is a follow-up (`meshy_animate` idle, or a
  Mecanim walk/idle blend). ⚠️ If the walk clip has root motion the model may drift vs the code-driven movement —
  PLAY-check; if so switch to an Animator with `applyRootMotion=false`. Mirrored to `custom_assets/models/npc_*`.
  - **Customers vs employees are visually disjoint** (user rule: no shared model): **ALL employees use the Meshy-6
    `npc_employee`** hero model (`Sim.SpawnEmployee`), tinted subtly by role (cashier natural / restocker warm /
    cleaner green / manager lilac). **Customers** pick a RANDOM model per spawn from `CustomerSpawner.CustomerKeys` —
    the six Meshy-5 models (`npc_customer`, `npc_customer2` elderly, `npc_customer3` young woman, `npc_customer4` teal
    alien, plus `npc_employee2`/`npc_employee3` reused as shoppers). To add more: generate → drop `npc_<key>.fbx`
    (+ `npc_<key>_textures/base_color.png`) in Resources → add the key to the customer array (or swap the employee key).
  - **NPCs face their travel direction:** employees rotate in `Employee` (LookRotation toward dest); customers rotate
    in `Customer.Update` (slerp toward last-frame velocity). Model forward = +Z. All rigged/walking (legacy pipeline).
  - **Walk/idle via `NpcAnimator`** (on the model child, added by NpcVisuals): plays the rigged WALK clip at a speed
    proportional to actual movement (`walk.speed = moveSpeed × strideTune`, 0.5) so feet don't slide, and cross-fades
    to a real **IDLE** clip when the agent stops (Customer/Employee call `SetMoving(moving, speed)` each frame; threshold
    0.15 m/s). NPC move speed is 2.0 m/s. The idle is **Meshy animation Idle_02 (action_id 11)** generated per rig and
    merged at runtime: `npc_<key>_idle.fbx` (Legacy import) → `Animation.AddClip(idleClip, "idle")` (same rig → bone
    paths match). To pick a different idle: browse the Meshy animation library (`animation_actions`, previews on
    cdn.meshy.ai), re-`meshy_animate` each rig, replace the `_idle.fbx`. ⚠️ **Perf:** Resources now holds 14 character
    FBX (7 walk + 7 idle, ~8 MB each) — the idle FBX meshes are redundant (only their clip is used); a future
    optimization is to strip the mesh / decimate. Use the **PerfHud** (F3, top-right: FPS + verts/tris/meshes) to judge.
  - **Extra counters are editable:** each carries a `CounterTag` so `StoreEditor.PickEditable` can drag it; positions
    persist via `LayoutData.counters` (saved in `SaveLayout`, restored in `SyncCounters` from `savedCounters` before
    falling back to `FindFreeSpot`). They also block nav like the original till.
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
- **Modal screens** set static flags `GalaxyMap.MapOpen` / `Franchise.PanelOpen` (mutually exclusive). Every
  other OnGUI component (`HUD`, `StoreEditor`, `WorldIcons`) returns early when either is true, and each modal
  draws a dim backdrop. A new full-screen panel must do the same or it'll show *through* the others' translucent
  overlays.

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
