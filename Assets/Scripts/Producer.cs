using System;

namespace IdleSim
{
    // One idle producer (a shop department). Buy many; cost scales, output is $/s per unit.
    [Serializable]
    public class Producer
    {
        public string id;
        public string name;
        public double baseCost;
        public double output;  // $/s per unit owned
        public double growth;  // cost multiplier per unit owned
        public int owned;

        public Producer(string id, string name, double baseCost, double output, double growth = 1.15)
        {
            this.id = id; this.name = name; this.baseCost = baseCost; this.output = output; this.growth = growth;
        }

        public double Cost => baseCost * Math.Pow(growth, owned);
        public double Rate => owned * output;
    }
}
