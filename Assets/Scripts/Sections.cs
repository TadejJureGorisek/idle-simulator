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
        public double valueMult;   // per-item profit vs. common (premium sections sell for more)
        public double upgradeBase; // base cost of investing one level into this section

        public Section(string id, string name, Color color, double unlockCost, double valueMult, double upgradeBase)
        {
            this.id = id; this.name = name; this.color = color;
            this.unlockCost = unlockCost; this.valueMult = valueMult; this.upgradeBase = upgradeBase;
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
        static List<Section> Build() => new List<Section>
        {
            //          id              name                   color                            unlock  value  upgrade
            new Section("common",       "Common",              new Color(0.50f, 0.46f, 0.40f),     0,   1.0,    100),
            new Section("veg",          "Vegetables & Fruits", new Color(0.32f, 0.62f, 0.32f),  1000,   1.3,    250),
            new Section("pantry",       "Pantry",              new Color(0.74f, 0.56f, 0.32f),  3000,   1.6,    600),
            new Section("sweets",       "Sweets",              new Color(0.86f, 0.45f, 0.62f),  8000,   2.2,   1500),
            new Section("refrigerated", "Refrigerated",        new Color(0.42f, 0.72f, 0.86f), 20000,   3.0,   4000),
            new Section("electronics",  "Electronics",         new Color(0.34f, 0.44f, 0.58f), 60000,   5.0,  12000),
        };
    }
}
