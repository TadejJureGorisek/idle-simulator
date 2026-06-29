using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // The connected-locations meta-game. Each location is built once, then upgraded; its "supply level"
    // produces a global effect that feeds back into the main store. Unlocked sequentially (build the
    // previous one first). Shown on the Galaxy Map screen.
    public class LocationDef
    {
        public string id, name, effect;   // effect = short human description
        public Color color;
        public double buildCost;           // cost to build (level 0 -> 1)
        public double upBase;              // base cost of an upgrade (level n -> n+1)
        public float upGrow;               // upgrade cost growth

        // Floorplan art for the Galaxy Map node + zoom. "loc_<id>_empty.png" = the bare level-0 room
        // (player builds it up); "loc_<id>.png" = the populated, built-up facility.
        Texture2D _full, _empty; bool _triedFull, _triedEmpty;
        public Texture2D IconFull  { get { if (!_triedFull)  { _triedFull  = true; _full  = Resources.Load<Texture2D>("loc_" + id); }           return _full; } }
        public Texture2D IconEmpty { get { if (!_triedEmpty) { _triedEmpty = true; _empty = Resources.Load<Texture2D>("loc_" + id + "_empty"); } return _empty; } }
        // level 0 = empty room (start; build it up); level >= 1 = the populated facility
        public Texture2D Art(int level) => level > 0 ? (IconFull ?? IconEmpty) : (IconEmpty ?? IconFull);

        public LocationDef(string id, string name, string effect, Color color, double buildCost, double upBase, float upGrow)
        {
            this.id = id; this.name = name; this.effect = effect; this.color = color;
            this.buildCost = buildCost; this.upBase = upBase; this.upGrow = upGrow;
        }
    }

    public static class Locations
    {
        static List<LocationDef> _all;
        public static List<LocationDef> All { get { if (_all == null) _all = Build(); return _all; } }
        public static LocationDef ById(string id) { foreach (var l in All) if (l.id == id) return l; return All[0]; }
        public static int Index(string id) { for (int i = 0; i < All.Count; i++) if (All[i].id == id) return i; return 0; }

        static List<LocationDef> Build() => new List<LocationDef>
        {
            //              id            name             effect (per level)                  color                              build   upBase  grow
            new LocationDef("warehouse",  "Warehouse",     "-8% cost of goods",                new Color(0.62f,0.55f,0.42f),     3e6,    2e5,    1.30f),
            new LocationDef("fabricator", "Fabricator",    "+12% income",                      new Color(0.80f,0.45f,0.30f),     1e7,    5e5,    1.35f),
            new LocationDef("farm",       "Orbital Farm",  "-20% spoilage (Produce & Cold)",   new Color(0.35f,0.70f,0.40f),     2.5e7,  1e6,    1.35f),
            new LocationDef("reactor",    "Fusion Reactor","+15% income (cheap power)",         new Color(0.40f,0.62f,0.90f),     5e7,    2e6,    1.40f),
            new LocationDef("hypermarket","Hypermarket",   "+25% income (brand synergy)",      new Color(0.78f,0.40f,0.70f),     1e8,    5e6,    1.40f),
        };
    }
}
