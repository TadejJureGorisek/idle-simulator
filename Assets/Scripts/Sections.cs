using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // A department / section of the store. Shelves (stands) belong to a section and are
    // color-coded by it. "common" is unlocked from the start; the rest are bought one-by-one
    // from the SECTIONS list in the right-hand panel.
    public class Section
    {
        public string id, name;
        public Color color;
        public double unlockCost;
        public double sell;        // sell price per item
        public double cost;        // cost of goods per item (what restocking 1 unit costs)
        public float demand;       // relative customer pull (higher = more shoppers head for it)
        public bool perishable;    // stock spoils over time -> needs frequent restock (Produce, Cold)
        public double upgradeBase; // base cost of one section upgrade level

        public double margin => sell - cost;   // profit per item before level / global multipliers

        public Section(string id, string name, Color color, double unlockCost, double sell, double cost, float demand, bool perishable, double upgradeBase)
        {
            this.id = id; this.name = name; this.color = color; this.unlockCost = unlockCost;
            this.sell = sell; this.cost = cost; this.demand = demand; this.perishable = perishable; this.upgradeBase = upgradeBase;
        }
    }

    public static class Sections
    {
        static List<Section> _all;
        public static List<Section> All { get { if (_all == null) _all = Build(); return _all; } }

        public static Section ById(string id)
        {
            foreach (var s in All) if (s.id == id) return s;
            return All[0]; // fall back to "common"
        }

        // Short display name for the tight HUD section rows (the full name is used elsewhere).
        public static string Short(string id)
        {
            switch (id)
            {
                case "veg": return "Produce";
                case "refrigerated": return "Cold";
                case "electronics": return "Tech";
                default: return ById(id).name;   // Common / Pantry / Sweets are already short
            }
        }

        // Order = unlock progression (common first, then cheap -> premium).
        // Each department has its own economics: cheap/high-demand staples vs. premium/low-demand
        // big-ticket goods; Produce + Cold spoil. This is what makes diversify-vs-specialize a real choice.
        static List<Section> Build() => new List<Section>
        {
            //          id              name                   color                            unlock  sell  cost  demand perish upgrade
            new Section("common",       "Common",              new Color(0.50f, 0.46f, 0.40f),     0,    5,    2,   1.0f, false,    100),
            new Section("veg",          "Vegetables & Fruits", new Color(0.32f, 0.62f, 0.32f),  1000,    6,  2.5,   1.3f, true,     250),
            new Section("pantry",       "Pantry",              new Color(0.74f, 0.56f, 0.32f),  3000,    9,    3,   1.1f, false,    600),
            new Section("sweets",       "Sweets",              new Color(0.86f, 0.45f, 0.62f),  8000,   14,    4,   1.2f, false,   1500),
            new Section("refrigerated", "Refrigerated",        new Color(0.42f, 0.72f, 0.86f), 20000,   22,    8,   0.9f, true,    4000),
            new Section("electronics",  "Electronics",         new Color(0.34f, 0.44f, 0.58f), 60000,   75,   35,   0.5f, false,  12000),
        };
    }
}
