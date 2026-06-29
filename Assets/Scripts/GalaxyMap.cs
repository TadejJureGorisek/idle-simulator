using UnityEngine;

namespace IdleSim
{
    // The Galaxy Map: zoom out from the store to the supply network. The main store is the hub;
    // connected locations orbit it, each linked by a supply line that brightens/thickens with its
    // level. Build + upgrade locations here; their effects feed back into the store's income.
    // Auto-attached by the HUD. IMGUI like the rest of the UI.
    public class GalaxyMap : MonoBehaviour
    {
        bool open, ready;
        public static bool MapOpen;   // true while the map is the active modal
        string sel = "warehouse";
        GUIStyle title, body, btn, box, node, ctr;
        Texture2D line;

        // hiding the marketplace behind the map: render only the nebula backdrop while open
        const int BackdropLayer = 30;
        Camera cam;
        GameObject backdrop;
        int savedMask = int.MinValue;
        bool hidden;

        void Setup()
        {
            ready = true;
            title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.60f, 0.95f, 1f);
            body = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, wordWrap = true };
            body.normal.textColor = new Color(0.86f, 0.90f, 0.96f);
            ctr = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            ctr.normal.textColor = Color.white;
            btn = new GUIStyle(GUI.skin.button) { fontSize = 12 };
            box = new GUIStyle(GUI.skin.box);
            node = new GUIStyle(GUI.skin.button) { fontSize = 11, richText = true };
            line = new Texture2D(1, 1); line.SetPixel(0, 0, Color.white); line.Apply();
        }

        // Hide the 3D marketplace (and customers) while the map is up, keeping the nebula:
        // switch the camera to render ONLY the backdrop layer. Logic keeps running; instantly reversible.
        void SetStoreHidden(bool hide)
        {
            if (hidden == hide) return;
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            if (hide)
            {
                if (savedMask == int.MinValue) savedMask = cam.cullingMask;
                if (backdrop == null)
                {
                    backdrop = GameObject.Find("SpaceBackdrop");
                    if (backdrop != null) backdrop.layer = BackdropLayer;
                }
                cam.cullingMask = backdrop != null ? (1 << BackdropLayer) : 0;   // backdrop only (or dark)
            }
            else if (savedMask != int.MinValue)
            {
                cam.cullingMask = savedMask;
            }
            hidden = hide;
        }

        // Animated supply shuttles streaming from a location INTO the store. Count, speed and streak
        // length scale with the location level → a visible trickle (Lv1) ramping to a torrent (high Lv).
        void Shuttles(Vector2 from, Vector2 to, int lv)
        {
            if (lv <= 0) return;
            Vector2 dir = to - from; float len = dir.magnitude;
            if (len < 1f) return; dir /= len;

            int n = Mathf.Clamp(lv + 1, 2, 12);
            float speed = 0.12f + 0.05f * lv;                 // cycles/sec — faster supply at higher level
            float half = 4f + Mathf.Min(5f, lv);             // longer streak at higher level
            float t = Time.unscaledTime * speed;
            for (int i = 0; i < n; i++)
            {
                float f = Mathf.Repeat(t + i / (float)n, 1f);
                Vector2 c = Vector2.Lerp(from, to, f);
                Line(c - dir * half, c + dir * half, 3f, new Color(0.55f, 0.92f, 1f, 0.85f)); // cyan streak
                var pc = GUI.color; GUI.color = new Color(1f, 1f, 1f, 0.95f);                  // bright head
                GUI.DrawTexture(new Rect(c.x + dir.x * half - 2.5f, c.y + dir.y * half - 2.5f, 5f, 5f), line);
                GUI.color = pc;
            }
        }

        void Line(Vector2 a, Vector2 b, float w, Color col)
        {
            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            float len = Vector2.Distance(a, b);
            var m = GUI.matrix; var c = GUI.color;
            GUIUtility.RotateAroundPivot(ang, a);
            GUI.color = col; GUI.DrawTexture(new Rect(a.x, a.y - w / 2f, len, w), line);
            GUI.matrix = m; GUI.color = c;
        }

        void OnGUI()
        {
            var sim = Sim.Instance; var e = Economy.Instance;
            if (sim == null || e == null) return;
            if (sim.Editing || Franchise.PanelOpen)   // hidden in edit / when franchise is the modal
            {
                if (open) { open = false; SetStoreHidden(false); }
                MapOpen = false; return;
            }
            if (!ready) Setup();

            float W = Screen.width, H = Screen.height;
            if (open)   // dim backdrop FIRST so the toggle button below stays bright on top
            {
                var dimc = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.5f);
                GUI.DrawTexture(new Rect(0, 0, W, H), line); GUI.color = dimc;
            }
            if (GUI.Button(new Rect(12, Screen.height - 76, 220, 30), open ? "CLOSE MAP" : "GALAXY MAP", btn)) { open = !open; SetStoreHidden(open); }
            MapOpen = open;
            if (!open) return;

            GUI.Box(new Rect(W * 0.5f - 470, 40, 940, H - 120), GUIContent.none, box);
            GUI.Label(new Rect(W * 0.5f - 446, 54, 800, 26), "GALAXY MAP   ·   supply network", title);
            if (GUI.Button(new Rect(W * 0.5f + 392, 50, 70, 28), "Close", btn)) { open = false; SetStoreHidden(false); return; }

            var locs = Locations.All;
            Vector2 store = new Vector2(W * 0.5f, 40 + (H - 120) * 0.64f);
            float R = Mathf.Min(W, H) * 0.30f;

            // connection lines (brightness + width scale with level = the supply ramp)
            for (int i = 0; i < locs.Count; i++)
            {
                Vector2 p = NodePos(store, R, i, locs.Count);
                int lv = sim.LocLev(locs[i].id);
                Color lc = lv > 0 ? new Color(0.40f, 0.85f, 1f, Mathf.Clamp01(0.30f + 0.10f * lv)) : new Color(0.5f, 0.5f, 0.6f, 0.16f);
                Line(store, p, lv > 0 ? 2f + Mathf.Min(8, lv) : 2f, lc);
                Shuttles(p, store, lv);   // supplies flow from the location into the store
            }

            // store hub
            var hr = new Rect(store.x - 74, store.y - 34, 148, 68);
            GUI.Box(hr, GUIContent.none, box);
            GUI.Label(new Rect(hr.x, hr.y + 12, hr.width, 20), "MAIN STORE", ctr);
            GUI.Label(new Rect(hr.x, hr.y + 34, hr.width, 18), "<color=#6BE08A>" + Economy.Fmt(sim.EstIncomePerSec()) + "/s</color>", ctr);

            // location nodes
            for (int i = 0; i < locs.Count; i++)
            {
                var d = locs[i]; Vector2 p = NodePos(store, R, i, locs.Count);
                var r = new Rect(p.x - 66, p.y - 30, 132, 60);
                int lv = sim.LocLev(d.id); bool unl = sim.LocUnlocked(d.id);
                var prev = GUI.color; GUI.color = unl ? Color.white : new Color(1, 1, 1, 0.55f);
                string lab = "<b>" + d.name + "</b>\n" + (lv > 0 ? "Lv " + lv : (unl ? "build" : "locked"));
                if (GUI.Button(r, lab, node)) sel = d.id;
                GUI.color = d.color; GUI.DrawTexture(new Rect(r.x + 8, r.y + 8, 10, 10), line); GUI.color = prev;
            }

            // detail / build panel
            var dr = new Rect(W * 0.5f - 270, H - 150, 540, 100);
            GUI.Box(dr, GUIContent.none, box);
            var sd = Locations.ById(sel); int sl = sim.LocLev(sd.id);
            GUI.Label(new Rect(dr.x + 18, dr.y + 12, dr.width - 36, 20), "<b>" + sd.name + "</b>   <color=#8A98B0>" + sd.effect + " / level</color>", body);
            string now = sl > 0 ? "    now <color=#6BE08A>" + sim.LocBonusText(sd.id) + "</color>" : "";
            GUI.Label(new Rect(dr.x + 18, dr.y + 36, dr.width - 36, 20), "Level <color=#CFE0FF>" + sl + "</color>" + now, body);
            double cost = sim.LocCost(sd.id);
            if (!sim.LocUnlocked(sd.id))
            {
                int idx = Locations.Index(sd.id);
                GUI.Label(new Rect(dr.x + 18, dr.y + 60, dr.width - 36, 20), "<color=#E88A8A>Locked</color> — build " + Locations.All[idx - 1].name + " first", body);
            }
            else
            {
                string t = (sl > 0 ? "Upgrade  Lv " + sl + " → " + (sl + 1) : "Build") + "    " + Economy.Fmt(cost);
                GUI.enabled = e.Money >= cost;
                if (GUI.Button(new Rect(dr.x + dr.width - 184, dr.y + 50, 168, 38), t, btn)) sim.BuildLocation(sd.id);
                GUI.enabled = true;
            }
        }

        static Vector2 NodePos(Vector2 store, float R, int i, int n)
        {
            float a = Mathf.Lerp(160f, 20f, n <= 1 ? 0.5f : (float)i / (n - 1)) * Mathf.Deg2Rad;
            return new Vector2(store.x + R * Mathf.Cos(a), store.y - R * Mathf.Sin(a));
        }
    }
}
