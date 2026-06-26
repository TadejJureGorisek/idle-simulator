# Galaxy Grocery — Idle Economy Design

How we fuse **Cookie-Clicker idle/clicker progression** with **supermarket-sim management**.
Goal: a relaxing-but-deep idle game whose curve lasts **months → years** (validated by sim).

> Why a rework: the prototype's economy (cashier/restocker/manager + min(serve,spawn) throughput)
> is **hard-capped** — the sim showed it reaches only ~$33/s after a week and then crawls (linear
> income, exponential cost). It's a great *active front-end*, but it has no long-game engine.

---

## 1. The fusion principle
Two interlocking systems that map cleanly onto the two genres:

| | Cookie Clicker side | Supermarket-sim side |
|---|---|---|
| **DEMAND** — what makes money | **Producers** = departments you buy many of, each generating $/sec, cost ×1.15 each. This is the exponential idle engine. | The departments *are* the store (Produce, Snacks, Bakery, Gaming, Appliances, Pharmacy, Luxury…). |
| **SUPPLY** — what gates the money | (Cookie Clicker has none.) | **Supply chain**: producers can only sell if there's **stock**. You **order** goods (cost-of-goods → margin), **store** them (warehouse capacity), and **restock** (throughput). Keep supply ≥ demand. |

So: **buy producers to grow demand, manage the supply chain to feed it.** Two parallel upgrade
tracks that must stay balanced — that balance *is* the supermarket sim, layered on idle scaling.

## 2. Currencies & core loop
- **$ (cash)** — main currency. Earned by selling stock; spent on producers, upgrades, and orders.
- **Stock (units)** — inventory. Consumed by sales (`unitsSold/s = revenue/s ÷ avgPrice`); replenished by orders.
- **Franchise Tokens** — prestige currency (permanent multipliers). See §8.

Per second: `revenue = min(demand, sellableStock × cashierThroughput) × margin`, stock decremented
by units sold, stock incremented by `min(orderRate, storageRoom)`. If stock hits 0 → income pauses
(the stock-out tension) until reorders catch up.

## 3. Producers (the idle engine) — Cookie-Clicker curve, supermarket-themed
Buy many of each; cost = `baseCost × 1.15^owned`. Output is the department's sell rate ($/s).
Numbers are the **proven Cookie Clicker building curve** (~10–15× cost, ~6× output per tier), which
is what guarantees the long progression.

| # | Department | baseCost | output $/s |
|---|---|---|---|
| 1 | Snack Rack | 15 | 0.1 |
| 2 | Produce Stand | 100 | 1 |
| 3 | Dairy Case | 1,100 | 8 |
| 4 | Bakery Counter | 12,000 | 47 |
| 5 | Deli | 130,000 | 260 |
| 6 | Gaming Aisle | 1.4M | 1,400 |
| 7 | Electronics Dept | 20M | 7,800 |
| 8 | Appliance Showroom | 330M | 44,000 |
| 9 | Pharmacy / Med-Bay | 5.1B | 260,000 |
| 10 | Luxury / Gems | 75B | 1.6M |
| 11 | Warehouse Club | 1T | 10M |
| 12 | Hypermarket | 14T | 65M |
| 13 | Distribution Hub | 170T | 430M |
| 14 | Galactic Chain | 2.1Qa | 2.9B |
| … | (extend as needed) | ×~15 | ×~6 |

Each producer unlocks (becomes visible) once you can roughly afford it. New tiers are the
"what's next" carrot.

## 4. Supply chain (the supermarket-sim layer)
The differentiator. A global supply pool gated by:
- **Order rate** (units/s auto-ordered). Each unit costs **cost-of-goods**. Upgradeable (faster orders).
- **Storage capacity** (max stock) — the **Warehouse** producers + storage upgrades. Bigger buffer =
  more idle-friendly (runs longer unattended / offline).
- **Restock throughput** — how fast stored stock becomes sellable (Restocker staff/upgrades).
- **Margin** = `1 − costOfGoods/price`. Start ~60%. Raised by **Bulk Supplier** (−cost),
  **Production Line** (make own goods → ~100% margin), warehouse synergies.

**The arc (this is how it stays idle-friendly):**
- **Early (active):** order/restock by hand; watch shelves; the current 3D store *is* the income.
- **Mid (automating):** buy Cashiers (sell throughput), Restockers (auto-restock), Auto-Order, first
  Warehouse. The pipeline starts running itself.
- **Late (idle):** warehouses + production lines make supply effectively infinite → pure idle
  producers; you just keep order/storage upgrades a step ahead of producer growth.

## 4b. Progression spine — locations & vertical integration
The producers above aren't one shop growing forever — they're a **network of locations** you build
out, which gives concrete "what's next" unlocks (a new facility) instead of just bigger numbers.

- **Phase 1 — one shop (slow growth).** Main shop; you handle customers by hand, then cashiers take
  over fast; departments grow demand. Stock is **ordered externally** (cost-of-goods, low margin).
  The current 3D store.
- **Phase 2 — take over the supply chain (vertical integration).** Instead of ordering everything,
  **acquire/build supplier facilities**, one category at a time: cold-chain plant (refrigerated/
  dairy/frozen), produce farm/hydroponics (veg & fruit), electronics factory, bakery/production
  line… Each facility **produces its category's stock and feeds the main shop via transport**.
  Owning a supplier drops that category's cost toward ~0 → **margin jumps** and you control supply.
  Each facility = its own producer + upgrade tree (output, capacity, automation) + a **transport
  link** you upgrade (trickle → torrent). Every facility both *multiplies revenue* and is a new idle
  producer — the two genres fused.
- **Phase 3 — more shops / the network.** A 2nd main shop (new sector) with its own departments fed
  by your suppliers → multiplies revenue; then a 3rd, etc. The **Galaxy Map** shows the whole network
  (shops ⇄ transport ⇄ suppliers); the view **scales up (zoom 10–20×)** from one store to an empire.
- **Phase 4 — prestige.** Franchise Buyout → reset the network for permanent multipliers, rebuild bigger.

**Living world (milestone unlocks):** crossing thresholds physically grows the scene *and* feeds the
loop — e.g. ~20 cashiers unlocks a **garage** beside the shop where delivery **vans** start pulling in
to grab loads, scaling to **trucks** and bigger rigs as you progress. The fleet you see *is* the
transport link (suppliers → shop): bigger/faster vehicles = more supply throughput (trickle → torrent).
Each unlock is a tangible reward, not just a number — keep a list of milestone → world-change → effect.

**Gating & navigation:** progress one location at a time — **max out** a location (its producers /
upgrades to their tier) to earn **1–2 unlock points**, spent to **unlock the next tree node** (a
supplier or a new shop). Two views: the **shop view** (zoomed in — the 3D store you run) and the
**tree / Galaxy Map** (zoomed out — all locations + their supply links); **click a node to zoom in**.
The view scales up (10–20×) as the network grows.

Spine: **shopkeeper → retail chain → vertically-integrated empire (suppliers + transport + stores).**
The Cookie-Clicker curve still drives the math; locations are how it's presented and unlocked.

## 5. Staff & infra (global modifiers)
- **Cashiers** → sell throughput (cap on revenue/s). Keep ahead of demand.
- **Restockers** → supply throughput (storage → shelf).
- **Managers** → ×income multiplier (and "run a department" automation).
- These are **upgrades**, not producers — they multiply/enable the producer engine.

## 6. Upgrades (multipliers — the backbone of idle scaling)
- **Per-department ×2** at ownership thresholds (own 1 / 5 / 25 / 50 / 100 → that department ×2),
  Cookie-Clicker style. Cheap dopamine + keeps every tier relevant.
- **Global research**: "×2 all income", "Loyalty Program +X% demand", "Self-Checkout: cashier
  throughput ×2", "Bulk Supplier: −20% cost-of-goods", etc.
- **Synergies** (CC-style): "each Warehouse boosts all departments +1%", "Production Lines boost
  Appliances ×2", etc. — gives a reason to spread investment.

## 7. Clicking & events (the "clicker" feel)
- **Manual click** = ring up / make a sale. Value = `base + p% of income/s` (so it stays relevant
  but never dominates idle). Early game it matters; it's the tactile hook.
- **Golden-customer events** (the golden-cookie analog): a roaming "VIP / Rush Hour" icon appears;
  click within a few seconds for a big timed bonus (×7 income 30 s, "Stock Surge", instant cash).
  This is the single biggest engagement/retention mechanic in Cookie Clicker — we want it.

## 8. Prestige — the multi-year arc
- **Franchise Buyout**: reset all producers/upgrades/stock; gain **Franchise Tokens** =
  `floor( (lifetimeEarnings / SCALE)^0.5 )` (tune SCALE so a first prestige is ~a day or two in).
- Each token = **permanent +X% global income**, plus a **Franchise upgrade tree** bought with tokens
  (permanent boosts: start with N free departments, +offline time, click power, etc.).
- Prestige is what makes runs accelerate and the game last years — each reset blows through earlier
  tiers faster and reaches new ones.

## 9. How the existing prototype fits
Nothing is wasted — the current build becomes the **front-end + early game**:
- The 3D store, customers, restock/ring-up = the **early active layer** and the ongoing **visual**
  (it represents the producer engine; income is the engine, not literally 5 shoppers).
- The **store editor / rotation** = customizing your store (cosmetic + optional layout bonuses).
- The **day/shift cycle** = an early-game rhythm you **automate away** (Longer Shift → 24/7 →
  AUTO NEW DAY = continuous idle), and downtime **cleaning** = a small side income / active option.
- `ShiftClock`-style time can drive golden-customer event cadence.

## 10. Validated progression (greedy-player sim, producers only)
$100/s @ 25 min · $10K/s @ 1.6 hr · $1M/s @ 15 hr · $100M/s @ 7.4 days · $1B/s @ 18.7 days ·
$10B/s @ 42 days — and this is **before** multiplier upgrades (§6) and prestige (§8), which extend
it into the months/years range. (Old economy for contrast: ~$33/s after a week, then flat.)

## 11. Implementation plan (stages)
1. **Producer engine** — `Producer` data + `EconomyV2` ($/s = Σ producers × mult), buy/cost curve,
   a producers panel UI. (Income starts flowing exponentially.) *Sim-tunable in isolation.*
2. **Supply layer** — global stock pool, order rate, storage, margin; income gated by stock;
   stock-out pauses income. Wire Cashiers/Restockers as throughput.
3. **Multiplier upgrades & research** — per-department ×2 + global research + synergies.
4. **Clicking + golden-customer events.**
5. **Prestige** — Franchise Tokens + upgrade tree.
6. **Re-tune** the whole curve in sim (target: satisfying first hour, exponential for weeks,
   prestige loop for the long haul), then balance pass.

Keep the active front-end (store/editor/day-cycle) throughout as the early game + visual skin.
