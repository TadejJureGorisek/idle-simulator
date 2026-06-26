using System;

namespace IdleSim
{
    // One purchasable upgrade. Cost scales as base * growth^level.
    public class Upgrade
    {
        public string Id;
        public string Name;
        public double BaseCost;
        public float Growth;
        public int Level;
        public int MaxLevel; // -1 = uncapped
        public Action Apply;

        public Upgrade(string id, string name, double baseCost, float growth, Action apply, int maxLevel = -1)
        {
            Id = id;
            Name = name;
            BaseCost = baseCost;
            Growth = growth;
            Apply = apply;
            MaxLevel = maxLevel;
        }

        public double CurrentCost => BaseCost * Math.Pow(Growth, Level);
        public bool IsMaxed => MaxLevel >= 0 && Level >= MaxLevel;
    }
}
