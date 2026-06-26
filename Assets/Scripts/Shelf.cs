using UnityEngine;

namespace IdleSim
{
    // A stocked shelf. Customers deplete it; you (or a restocker) refill it for cost-of-goods.
    public class Shelf : MonoBehaviour
    {
        public int Capacity = 10;
        public int Stock = 10;

        Transform bar;
        Renderer barRend;
        static readonly Color Empty = new Color(0.80f, 0.22f, 0.22f);
        static readonly Color Full = new Color(0.22f, 0.80f, 0.32f);

        public float Ratio => Capacity > 0 ? (float)Stock / Capacity : 0f;

        public void Init()
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Cube);
            b.name = "StockBar";
            Destroy(b.GetComponent<Collider>());
            b.transform.SetParent(transform, false);
            bar = b.transform;
            barRend = b.GetComponent<Renderer>();
            UpdateVisual();
        }

        public bool Pick()
        {
            if (Stock > 0) { Stock--; UpdateVisual(); return true; }
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
            if (Economy.Instance.TrySpend(units * cost))
            {
                Stock += units;
                UpdateVisual();
            }
        }

        void UpdateVisual()
        {
            if (bar == null) return;
            float r = Ratio;
            float h = 0.1f + r * 1.4f;
            bar.localScale = new Vector3(0.8f, h, 0.2f);
            bar.localPosition = new Vector3(0, 0.5f + h / 2f, -0.55f);
            if (barRend != null) barRend.material.color = Color.Lerp(Empty, Full, r);
        }
    }
}
