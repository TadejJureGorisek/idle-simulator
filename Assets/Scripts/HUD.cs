using UnityEngine;

namespace IdleSim
{
    // Code-only IMGUI HUD for the prototype (no Canvas/prefabs needed). Styles are cached.
    public class HUD : MonoBehaviour
    {
        GUIStyle big, mid, small, btn;
        bool init;
        string welcome;

        void Start()
        {
            var e = Economy.Instance;
            if (e != null && e.OfflineGain > 0.5)
                welcome = "Welcome back  +" + Money(e.OfflineGain) + " while away";

            // attach runtime overlays (works without re-running the scene builder)
            if (GetComponent<WorldIcons>() == null) gameObject.AddComponent<WorldIcons>();
            if (GetComponent<ProducerEconomy>() == null) gameObject.AddComponent<ProducerEconomy>();
        }

        void Setup()
        {
            init = true;
            big = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold };
            big.normal.textColor = Color.white;
            mid = new GUIStyle(GUI.skin.label) { fontSize = 15 };
            mid.normal.textColor = new Color(0.80f, 0.86f, 0.96f);
            small = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            small.normal.textColor = new Color(0.72f, 0.78f, 0.90f);
            btn = new GUIStyle(GUI.skin.button) { fontSize = 12, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(10, 6, 4, 4) };
        }

        void OnGUI()
        {
            if (!init) Setup();
            var e = Economy.Instance;
            var sim = Sim.Instance;
            if (e == null || sim == null) return;

            // money / stats panel
            GUI.Box(new Rect(10, 10, 340, 98), GUIContent.none);
            GUI.Label(new Rect(22, 16, 326, 34), Money(e.Money), big);
            double inc = sim.EstIncomePerSec();
            if (ProducerEconomy.Instance != null) inc += ProducerEconomy.Instance.IncomePerSec;
            GUI.Label(new Rect(24, 52, 326, 20), "Income  " + Money(inc) + " / sec", mid);
            GUI.Label(new Rect(24, 74, 326, 20),
                "Served " + e.CustomersServed + "    Lost " + e.LostSales + "    Queue " + sim.Checkout.LineCount, small);

            GUI.Label(new Rect(12, 114, 460, 36),
                "Click a customer at the till (or SPACE) to ring up   •   click a shelf (or R) to restock", small);

            // clock / day panel (top centre)
            float cx = Screen.width / 2f;
            GUI.Box(new Rect(cx - 95, 6, 190, 48), GUIContent.none);
            GUI.Label(new Rect(cx - 85, 9, 170, 24), "DAY " + sim.Day + "      " + ClockStr(sim.Clock), mid);
            GUI.Label(new Rect(cx - 85, 31, 170, 20), sim.IsOpen ? "OPEN" : ("CLOSED   ·   Mess " + sim.Mess), small);
            if (!sim.IsOpen)
            {
                if (GUI.Button(new Rect(cx - 70, 58, 140, 30), "NEW DAY")) sim.NewDay();
                GUI.Label(new Rect(cx - 120, 92, 240, 20), "Click trash to clean for +$" + sim.CleanReward + " each", small);
            }

            // upgrades panel
            float w = 250, h = 50, x = Screen.width - w - 14, y = 12;
            GUI.Box(new Rect(x - 8, y - 8, w + 16, sim.Upgrades.Count * (h + 6) + 38), GUIContent.none);
            GUI.Label(new Rect(x, y, w, 22), "UPGRADES", mid);
            y += 28;
            foreach (var u in sim.Upgrades)
            {
                if (u.Id == "autoday" && !sim.Is247) continue; // extreme upgrade hidden until 24/7
                string label = u.IsMaxed
                    ? u.Name + "   (MAX)"
                    : u.Name + "   Lv " + u.Level + "\n" + Money(u.CurrentCost);
                GUI.enabled = !u.IsMaxed && e.Money >= u.CurrentCost;
                if (GUI.Button(new Rect(x, y, w, h), label, btn)) sim.Buy(u);
                GUI.enabled = true;
                y += h + 6;
            }

            if (!string.IsNullOrEmpty(welcome))
            {
                GUI.Box(new Rect(Screen.width / 2f - 180, 10, 360, 30), GUIContent.none);
                GUI.Label(new Rect(Screen.width / 2f - 170, 14, 360, 24), welcome, mid);
            }
        }

        static string ClockStr(float clock)
        {
            float c = Mathf.Repeat(clock, 24f);
            int h = Mathf.FloorToInt(c);
            int m = Mathf.FloorToInt((c - h) * 60f);
            return h.ToString("00") + ":" + m.ToString("00");
        }

        static readonly string[] Suffix = { "", "K", "M", "B", "T", "Qa", "Qi" };

        string Money(double v)
        {
            if (v < 1000) return "$" + v.ToString("0");
            int i = 0;
            double x = v;
            while (x >= 1000 && i < Suffix.Length - 1) { x /= 1000; i++; }
            return "$" + x.ToString("0.00") + Suffix[i];
        }
    }
}
