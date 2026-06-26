using UnityEngine;

namespace IdleSim
{
    // Holds stock for a shelf. The fullness / out-of-stock indicators are drawn ABOVE the shelf
    // by WorldIcons (so they stay visible at any rotation), not on the shelf body.
    public class Shelf : MonoBehaviour
    {
        public int Capacity = 10;
        public int Stock = 10;
        public string catalogId = "st_basic";

        public float Ratio => Capacity > 0 ? (float)Stock / Capacity : 0f;

        void Awake()
        {
            var legacy = transform.Find("StockBar"); // remove the old on-shelf bar if a saved scene has one
            if (legacy != null) Destroy(legacy.gameObject);
        }

        public void Init() { }

        public bool Pick()
        {
            if (Stock > 0) { Stock--; return true; }
            return false;
        }

        public void RestockAffordable()
        {
            int need = Capacity - Stock;
            if (need <= 0) return;
            int cost = Sim.Instance.ItemCost;
            int canAfford = (int)(Economy.Instance.Money / cost);
            int units = Mathf.Min(need, canAfford);
            if (units <= 0) return;
            if (Economy.Instance.TrySpend(units * cost)) Stock += units;
        }
    }
}
