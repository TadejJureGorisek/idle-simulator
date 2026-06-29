using UnityEngine;

namespace IdleSim
{
    // Code-only IMGUI HUD for the prototype (no Canvas/prefabs needed). Styles are cached.
    public class HUD : MonoBehaviour
    {
        GUIStyle big, mid, small, btn, head, rowBtn, ctr;
        Texture2D swatch, rowNormal, rowHover, coinIcon;
        bool init;
        string welcome;

        void Start()
        {
            var e = Economy.Instance;
            if (e != null && e.OfflineGain > 0.5)
                welcome = "Welcome back  +" + Money(e.OfflineGain) + " while away";

            // attach runtime overlays (works without re-running the scene builder)
            if (GetComponent<WorldIcons>() == null) gameObject.AddComponent<WorldIcons>();
            if (GetComponent<Franchise>() == null) gameObject.AddComponent<Franchise>();
        }

        void Setup()
        {
            init = true;
            big = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, richText = true };
            big.normal.textColor = Color.white;
            mid = new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };
            mid.normal.textColor = new Color(0.80f, 0.86f, 0.96f);
            small = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
            small.normal.textColor = new Color(0.72f, 0.78f, 0.90f);
            head = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, richText = true };
            head.normal.textColor = new Color(0.55f, 0.92f, 1f);
            btn = new GUIStyle(GUI.skin.button) { fontSize = 12, alignment = TextAnchor.MiddleCenter, padding = new RectOffset(8, 8, 3, 3), wordWrap = false, richText = true };
            swatch = new Texture2D(1, 1); swatch.SetPixel(0, 0, Color.white); swatch.Apply();
            // flat translucent list rows — stack flush with zero gap (the panel's neon frame is the hero)
            rowNormal = Solid(new Color(0.12f, 0.11f, 0.22f, 0.45f));
            rowHover = Solid(new Color(0.22f, 0.40f, 0.55f, 0.62f));
            rowBtn = new GUIStyle(GUI.skin.button) { fontSize = 12, alignment = TextAnchor.MiddleCenter, wordWrap = false, richText = true, padding = new RectOffset(10, 10, 2, 2) };
            rowBtn.border = new RectOffset(0, 0, 0, 0);
            rowBtn.normal.background = rowNormal; rowBtn.onNormal.background = rowNormal; rowBtn.focused.background = rowNormal;
            rowBtn.hover.background = rowHover; rowBtn.active.background = rowHover; rowBtn.onHover.background = rowHover; rowBtn.onActive.background = rowHover;
            rowBtn.normal.textColor = new Color(0.90f, 0.94f, 1f); rowBtn.hover.textColor = Color.white; rowBtn.active.textColor = Color.white;
            coinIcon = Resources.Load<Texture2D>("icon_coin");
            ctr = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            ctr.normal.textColor = new Color(0.85f, 0.92f, 1f);
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(4, 4, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[16]; for (int i = 0; i < px.Length; i++) px[i] = c;
            t.SetPixels(px); t.Apply();
            return t;
        }

        void Divider(float x, float y, float w)
        {
            var c = GUI.color; GUI.color = new Color(0.40f, 0.85f, 1f, 0.45f);
            GUI.DrawTexture(new Rect(x, y, w, 1f), swatch); GUI.color = c;
        }

        void OnGUI()
        {
            UISkin.EnsureApplied();
            if (!init) Setup();
            var e = Economy.Instance;
            var sim = Sim.Instance;
            if (e == null || sim == null) return;

            // money / stats panel (content inset ~20px so it clears the neon frame)
            GUI.Box(new Rect(10, 10, 344, 138), GUIContent.none);
            if (coinIcon != null) GUI.DrawTexture(new Rect(30, 26, 30, 30), coinIcon, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(coinIcon != null ? 66 : 30, 24, 280, 34), Money(e.Money), big);
            double inc = sim.EstIncomePerSec();
            GUI.Label(new Rect(30, 62, 300, 20), "Income  <color=#6BE08A>" + Money(inc) + "</color> <color=#8A98B0>/sec</color>", mid);
            GUI.Label(new Rect(30, 84, 300, 20),
                "Served <color=#CFE0FF>" + e.CustomersServed + "</color>   Lost <color=#E88A8A>" + e.LostSales + "</color>   Queue <color=#CFE0FF>" + sim.Checkout.LineCount + "</color>", small);
            int mc = Milestones.Completed(e.CustomersServed, e.TotalEarned);
            GUI.Label(new Rect(30, 106, 300, 20), "Milestones  <color=#FFD24A>+" + (int)(mc * Milestones.PerMilestone * 100) + "%</color>  <color=#8A98B0>(" + mc + "/" + Milestones.Total + ")</color>", small);

            GUI.Label(new Rect(12, 154, 470, 36),
                "Click a customer at the till (or SPACE) to ring up   •   click a shelf (or R) to restock", small);

            // clock / day panel (top centre)
            float cx = Screen.width / 2f;
            GUI.Box(new Rect(cx - 100, 6, 200, 68), GUIContent.none);
            GUI.Label(new Rect(cx - 82, 20, 164, 22), "DAY " + sim.Day + "      " + ClockStr(sim.Clock), mid);
            GUI.Label(new Rect(cx - 82, 42, 164, 18), sim.IsOpen ? "<color=#6BE08A>OPEN</color>" : ("<color=#E88A8A>CLOSED</color>   ·   Mess " + sim.Mess), small);

            // game-speed control: - halves, + doubles (0.25x .. 16x)
            GUI.Box(new Rect(cx - 74, 78, 148, 32), GUIContent.none);
            if (GUI.Button(new Rect(cx - 68, 82, 34, 24), "-", btn)) sim.SlowDown();
            GUI.Label(new Rect(cx - 34, 80, 68, 26), sim.GameSpeed.ToString("0.##") + "x", ctr);
            if (GUI.Button(new Rect(cx + 34, 82, 34, 24), "+", btn)) sim.SpeedUp();

            if (!sim.IsOpen)
            {
                if (GUI.Button(new Rect(cx - 70, 116, 140, 32), "NEW DAY")) sim.NewDay();
                GUI.Label(new Rect(cx - 120, 152, 240, 20), "Click trash to clean for +$" + sim.CleanReward + " each", small);
            }

            // upgrades + sections panel (right). Content is inset by `pad` so it stays INSIDE the
            // neon frame (the box is the outer panel; cxr/cw are the usable interior).
            const float pad = 20f, H = 44f;   // ONE button height for every list row, rows stacked flush (no gaps)
            float boxW = 280f;
            float bx = Screen.width - boxW - 12f, by = 8f;
            float cxr = bx + pad, cw = boxW - pad * 2f;
            int upRows = 0; foreach (var u in sim.Upgrades) { if (u.Id == "autoday" && !sim.Is247) continue; upRows++; }
            float boxH = pad * 2f + 28f + upRows * H + 14f + 28f + Sections.All.Count * H;
            GUI.Box(new Rect(bx, by, boxW, boxH), GUIContent.none);

            float cy = by + pad;
            GUI.Label(new Rect(cxr, cy, cw, 20), "UPGRADES", head);
            Divider(cxr, cy + 20f, cw); cy += 28f;
            bool firstUp = true;
            foreach (var u in sim.Upgrades)
            {
                if (u.Id == "autoday" && !sim.Is247) continue;   // extreme upgrade hidden until 24/7
                if (u.Id == "supply" && !sim.WarehouseBuilt) continue; // supply line appears once the warehouse is built
                string label = u.IsMaxed
                    ? "<b>" + u.Name + "</b>   <color=#8A98B0>(MAX)</color>"
                    : "<b>" + u.Name + "</b>  <color=#8A98B0>Lv " + u.Level + "</color>\n<color=#FFD24A>" + Money(u.CurrentCost) + "</color>";
                GUI.enabled = !u.IsMaxed && e.Money >= u.CurrentCost;
                bool divv = !firstUp; firstUp = false;
                if (GUI.Button(new Rect(cxr, cy, cw, H), label, rowBtn)) sim.Buy(u);
                if (divv) Divider(cxr, cy, cw);   // thin separator between flush rows
                GUI.enabled = true;
                cy += H;
            }

            // store sections / departments — "common" is free, the rest unlock here. Name is tinted
            // by the section colour (replaces the old swatch) so rows are identical-size pure buttons.
            cy += 14f;
            GUI.Label(new Rect(cxr, cy, cw - 62f, 20), "SECTIONS", head);
            if (GUI.Button(new Rect(cxr + cw - 58f, cy - 3f, 58f, 24f), "All +1", btn)) sim.UpgradeAllSections();
            Divider(cxr, cy + 20f, cw); cy += 28f;
            bool firstSec = true;
            foreach (var s in Sections.All)
            {
                string nm = "<color=#" + ColorUtility.ToHtmlStringRGB(s.color) + "><b>" + Sections.Short(s.id) + "</b></color>";
                bool divv = !firstSec; firstSec = false;
                if (sim.IsSectionUnlocked(s.id))
                {
                    double uc = sim.SectionUpgradeCost(s.id);
                    GUI.enabled = e.Money >= uc;
                    if (GUI.Button(new Rect(cxr, cy, cw, H), nm + "  <color=#8A98B0>Lv " + sim.GetSectionLevel(s.id) + "</color>  <color=#FFD24A>" + Money(uc) + "</color>", rowBtn)) sim.UpgradeSection(s.id);
                    GUI.enabled = true;
                }
                else
                {
                    GUI.enabled = e.Money >= s.unlockCost;
                    if (GUI.Button(new Rect(cxr, cy, cw, H), nm + "   <color=#FFD24A>" + Money(s.unlockCost) + "</color>", rowBtn)) sim.UnlockSection(s.id);
                    GUI.enabled = true;
                }
                if (divv) Divider(cxr, cy, cw);
                cy += H;
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
