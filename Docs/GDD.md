# Galaxy Grocery — Game Design Document

*Supermarket simulator × idle/tycoon. You start as the solo owner of a tiny space-station
grocery and grow it into a galaxy-spanning logistics empire.*

Status: pre-production / design. Living doc — expect churn.

---

## 1. Pitch
You arrive at a run-down kiosk on a space station with **$50** to your name. You order your
first batch of stock, and alien customers start trickling in. At first **you do everything by
hand** — ring up each customer at the register, reorder stock when shelves go empty. Then you
start hiring: a cashier rings them up for you, a restocker refills the shelves, a manager runs
the floor while you're away. You expand the store, add new sections, and eventually buy
**whole new places** — warehouses, production lines, farms — that connect back to your main
store through a supply line that starts as a trickle and ramps into a torrent. Build the
logistics network, out-grow the galaxy.

## 2. Art direction & pipeline
Target look = the reference key art: glossy **pre-rendered 2.5D isometric** sci-fi supermarket,
neon-on-dark, cute chibi alien customers + robot staff.

- **Meshy** → all 3D (fixtures, characters, upgrade hero props), rendered to sprites.
- **Higgsfield** → all 2D (background plate, full UI kit, icons, FX, key art, trailer).
- **The render rig is the backbone:** one fixed isometric Blender camera + neon 3-point light +
  transparent PNG/shadow output. Every Meshy model goes through the same rig so the whole set
  matches. (Same headless `bpy` approach used on Nights at Data Center.)

See the production roadmap in §10.

## 3. Core loop — "you are the staff, until you hire the staff"
Every early **chore is a manual click**; every hire **deletes a chore**. That transition is the hook.

| Manual chore (Tier 0) | The hire that kills it |
|---|---|
| Click each customer to ring them up | **Cashier** → auto-checkout |
| Click to reorder stock when shelves empty | **Restocker** → auto-restock |
| Babysit the floor / no offline income | **Manager** → multipliers + offline earnings |

Minute 1: frantically checking out aliens by hand. Ten minutes in: watching it run, deciding
what to buy next.

## 4. The tensions (what makes it a *sim*, not a flat clicker)
1. **Stock.** Shelves hold N units; customers deplete them; an empty shelf = lost sale + rating
   ding. Restocking costs cash + time. This is the loop staff automate and supply chains cheapen.
2. **Store rating → customer flow.** Availability + checkout speed + cleanliness raise rating,
   which raises the customers/min spawn rate. Flywheel.
3. **Shrinkage (mid-game).** Shoplifting + spoilage start quietly nibbling income (visible
   "−$X lost" tick). Justifies the loss-prevention upgrades.

## 5. The seven levers
The whole upgrade economy optimizes seven dials. New levers unlock as you progress so it never
feels like "+more of the same."

1. **Throughput** — checkout speed & lanes (Tier 1)
2. **Stock** — shelf capacity & restock speed (Tier 1)
3. **Sections** — new product categories = new income streams (Tier 2)
4. **Demand** — customer spawn rate via marketing (Tier 2.5)
5. **Basket size** — $ spent per customer (Tier 2.5)
6. **Loss prevention** — cut shrinkage (Tier 2.5)
7. **Supply** — cost-of-goods & restock via connected locations (Tier 3, the meta-game)

## 6. Upgrade list

### Tier 0 — Solo
- Start: **$50**, hand-checkout (~$3 profit/sale), 1 customer / ~3s.
- Initial Stock Order — $40 (~20 units @ $2 cost, sell $5).

### Tier 1 — First automation
| Upgrade | Cost | Effect | Growth |
|---|---|---|---|
| Extra Shelf | $120 | +capacity / parallel sales | 1.10 |
| Hire Cashier | $200 | auto-checkout | 1.12 |
| Hire Restocker | $400 | auto-restock | 1.12 |
| Extra Checkout Lane | $600 | +parallel checkout | 1.12 |
| Hire Manager | $2.5K | +25% income, enables **offline earnings** | 1.15 |
| Expand Floor I | $5K | +customer cap, +1 section slot | 1.18 |

### Tier 2 — Sections (diversify)
| Section | Cost | Note |
|---|---|---|
| Snacks | $3K | first extra line |
| Gaming | $50K | premium margin |
| Bakery / Fresh | $75K | high turnover |
| Cantina / Beverages | $120K | |
| Med-Bay / Bio-Stims | $180K | |
| Appliances | $250K | big-ticket, low volume |
| Exotic Pets | $350K | |
| Galactic Luxury / Gems | $700K | top margin |

### Tier 2.5 — Mid-game levers
**Demand:** Neon Storefront Sign $40K (+20% spawn) · Holo-Billboard Ads $150K (+35%) ·
GalaxyNet Ad Campaign $600K (+50%).
**Basket size:** Hover-Trolleys $30K (+1 item) · Free Samples $120K (+25% basket) ·
Impulse-Buy Endcaps $250K · Loyalty Program (Galaxy Points) $500K (repeat customers).
**Loss prevention:** Security Droids $180K (−shoplifting) · Cold Storage/Freezers $300K
(−spoilage) · Surveillance Hub $700K (−theft, +rating).
**Throughput depth:** Self-Checkout Kiosks $80K (lanes, no wage) · Express Lane $90K ·
Cashier Academy $200K (+30% speed).
**Experience/rating:** Aisle Ambience $45K (+spend) · Cleaning Bots $60K (+cleanliness) ·
Wider Aisles Remodel $400K (+concurrent shoppers).
**Supply on-ramp:** Bulk Supplier Contract $250K (−10% goods) · Just-in-Time Logistics $800K
(faster restock, −5% goods). *(These seed the Warehouse so it reads as a graduation.)*

### Tier 3 — Connected locations (the meta-game, see §7)
| Location | Cost | Supplies / effect |
|---|---|---|
| Warehouse | $3M | bulk inventory → cheap/free restock |
| Production Line / Fabricator | $10M | manufactures a category → ~100% margin on it |
| Hydroponics / Orbital Farm | $25M | fresh produce supply |
| Fusion Reactor / Power Plant | $50M | powers automation → lowers operating cost |
| Interstellar Hypermarket (new store) | $100M | own customers + brand-synergy % to main |

### Tier 4 — Prestige
- **Franchise Buyout** — sell the empire, restart with a permanent global multiplier scaled to
  lifetime earnings. Each run reaches further into the galaxy.

## 7. ⭐ Connected locations & the supply chain (meta-game)
The Tier-3 megas are **not** "+X%" buttons. Each is a **new place you build out**, connected to
the main store by a **visible supply line that starts as a trickle and ramps up**. The visual
*is* the progress bar: one beat-up cargo shuttle hauling a single crate → a constant stream of
automated drones across glowing full pipelines.

**Per-location structure (each is a mini idle game with its own tree):**
- **Output rate** — how fast it produces its resource.
- **Storage cap** — how much it can stockpile.
- **Logistics / link** — how fast it ships to the hub (shuttle speed, count, capacity, automation).
- **Quality tier** — better goods → higher margin / premium products unlocked.
- **Local staff & automation** — the location's own cashiers/workers/managers.

**The feedback into the main store** — each location moves one main-store stat, so you *watch*
the trickle become a torrent and the main store's profit climb:
- Warehouse → cost-of-goods ↓ + restock instant (store pulls from stockpile free).
- Fabricator → stop buying a category → margin ↑ on it.
- Farm → perishable supply, high turnover.
- Reactor → operating cost ↓ / unlocks higher staff tiers.
- Hypermarket → its own income + synergy % to the hub.

**Supply vs demand tension (the balancing dial):** if the main store grows faster than supply →
shortages → motivates link/output upgrades. If supply outpaces demand → surplus stockpiles
(sell the surplus B2B for bonus cash, or it caps). Push-pull keeps both halves engaged — it
links two production curves instead of one.

**The network grows into a web:** locations eventually feed each other (Reactor powers the
Fabricator, which supplies the Store). The reference's **Galaxy Map** screen = the network view.

## 8. Economy formulas
- **Upgrade cost:** `cost(n) = base × r^n` — `r ≈ 1.07` cheap helpers, `1.15` big producers.
- **Income/sec:** `base × count × section_mult × manager_mult × global_mult` (mults stack
  multiplicatively; global = warehouse/research/prestige).
- **Profit/checkout:** `sell − cost_of_goods × (1 − warehouse_disc − production_disc)`.
- **Customer spawn:** `base × rating_mult × marketing_mult`.
- **Offline earnings:** `income/sec × seconds_away × efficiency` (start ~50%, raised by
  managers/research), capped ~2h early → 12h+ upgraded.
- **Prestige points:** `floor(k × √(lifetime_earnings / scale))`, each point = +Y% global.

Seed numbers live in `Galaxy_Grocery_Economy.csv` (open as a spreadsheet to eyeball the curve).

## 9. Future possibilities / parking lot
- **Contracts / achievements:** "serve 5,000 customers → reward" (reference already shows this).
- **Random events:** supply ship delayed, festival rush, station blackout, alien strike.
- **Seasonal / limited products** and timed sale events (boosts).
- **VIP / celebrity alien customers** — big spenders, mini-events.
- **Competitors** — rival stores you out-compete or buy out.
- **Managers as collectible characters** with perks (auto-run a location).
- **Research tree** — global tech unlocks (separate currency).
- **Multi-currency:** cash + premium Galactic Credits (the hex G) + research points.
- **Black-market / smuggling section** — risk/reward.
- **Cosmetic customization** — store themes, staff skins.
- **Prestige layers = galaxies** — each prestige opens a new galaxy with new product types.

## 10. Production roadmap (~4–5 weeks, Meshy + Higgsfield)
- **P1 — Foundation:** GDD (this) + economy sheet; Higgsfield style frame to lock look; build
  the Blender iso render rig; pick engine; grey-box the screen layout.
- **P2 — Environment + idle loop:** Meshy shell/shelves/checkout/decor → render; Higgsfield
  floor + base UI; working idle core (tap, per-sec, one upgrade, save/load, offline).
- **P3 — Characters + upgrades:** Meshy customers (rig + walk → sheets), staff, 5 upgrade
  heroes → render; wire all upgrades + cost scaling; customer pathing.
- **P4 — UI + systems:** full Higgsfield UI kit; boosts, prestige + Galaxy Map, managers,
  research, daily rewards, achievements; juice (floating numbers, coin pops, glow).
- **P5 — Polish + marketing:** balance, audio, particles; Higgsfield key art + trailer + icon +
  screenshots; playtest build.

## 11. Open decisions
- **Engine:** Unity 2D (recommended — known) vs web/Godot. Pipeline-agnostic.
- **v1 scope:** polish the main Store screen to reference quality first; other screens
  functional-then-pretty.
