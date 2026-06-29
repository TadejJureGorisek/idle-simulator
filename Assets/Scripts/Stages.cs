using System.Collections.Generic;

namespace IdleSim
{
    // Per-location stage definition: a 3-step task chain (each step manual-first, then automatable by an
    // employee) whose sustained throughput boosts the main store. Pure data — StageManager runs it,
    // GalaxyMap draws it. effect: "cost" / "spoil" → multiplicative discount (1-perLevel)^infra;
    // "income" → +perLevel*infra income. arrivalBase = seconds between work arriving at step 0.
    public class StageTask
    {
        public string role, verb, employee;   // e.g. "INTAKE", "Accept", "Clerk"
        public StageTask(string role, string verb, string employee) { this.role = role; this.verb = verb; this.employee = employee; }
    }

    public class StageDef
    {
        public string id, title, outLabel, effect;
        public float perLevel, arrivalBase;
        public StageTask[] tasks;   // length 3
        public StageDef(string id, string title, string outLabel, string effect, float perLevel, float arrivalBase, StageTask[] tasks)
        { this.id = id; this.title = title; this.outLabel = outLabel; this.effect = effect; this.perLevel = perLevel; this.arrivalBase = arrivalBase; this.tasks = tasks; }
    }

    public static class Stages
    {
        static Dictionary<string, StageDef> _m;
        public static StageDef For(string id) { if (_m == null) Build(); return _m.TryGetValue(id, out var d) ? d : null; }

        static void Build()
        {
            _m = new Dictionary<string, StageDef>();
            void A(StageDef d) => _m[d.id] = d;

            // Warehouse — supply logistics: suppliers in → shelve → load vans out. Cuts cost of goods.
            A(new StageDef("warehouse", "SUPPLY LINE", "supply ▶ to MAIN STORE", "cost", 0.08f, 2.5f, new[] {
                new StageTask("INTAKE",   "Accept",   "Clerk"),
                new StageTask("STORAGE",  "Shelve",   "Stocker"),
                new StageTask("DISPATCH", "Load van", "Loader"),
            }));
            // Fabricator — make parts: receive stock → run the press → pack & ship. Raises income.
            A(new StageDef("fabricator", "ASSEMBLY LINE", "parts ▶ to MAIN STORE", "income", 0.12f, 2.5f, new[] {
                new StageTask("INTAKE",    "Receive",   "Handler"),
                new StageTask("FABRICATE", "Run press", "Machinist"),
                new StageTask("PACK",      "Pack",      "Packer"),
            }));
            // Orbital Farm — grow cycle: plant trays → tend crops → harvest. Cuts spoilage.
            A(new StageDef("farm", "GROW CYCLE", "produce ▶ to MAIN STORE", "spoil", 0.20f, 1.4f, new[] {
                new StageTask("PLANT",   "Plant",   "Planter"),
                new StageTask("TEND",    "Tend",    "Botanist"),
                new StageTask("HARVEST", "Harvest", "Picker"),
            }));
            // Fusion Reactor — power cycle: load fuel → regulate reaction → route power. Raises income.
            A(new StageDef("reactor", "POWER CYCLE", "power ▶ to MAIN STORE", "income", 0.15f, 1.6f, new[] {
                new StageTask("FUEL",   "Load fuel", "Fueler"),
                new StageTask("REACT",  "Regulate",  "Operator"),
                new StageTask("OUTPUT", "Route",     "Dispatcher"),
            }));
            // Hypermarket — retail floor: stock aisles → serve customers → fulfil orders. Raises income.
            A(new StageDef("hypermarket", "RETAIL FLOOR", "sales ▶ to MAIN STORE", "income", 0.25f, 1.4f, new[] {
                new StageTask("STOCK",  "Stock",  "Stocker"),
                new StageTask("SELL",   "Serve",  "Cashier"),
                new StageTask("FULFIL", "Fulfil", "Runner"),
            }));
        }
    }
}
