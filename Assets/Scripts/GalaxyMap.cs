using UnityEngine;

namespace IdleSim
{
    // The Galaxy Map: zoom out from the store to the supply network. The main store is the hub;
    // connected locations orbit it, each linked by a supply line that brightens/thickens with its
    // level. Build + upgrade locations here; their effects feed back into the store's income.
    // Auto-attached by the HUD. IMGUI like the rest of the UI.
    public class GalaxyMap : MonoBehaviour
    {
        bool ready;
        public static bool MapOpen;   // true while the map is the active modal
        string zoomLoc;   // non-null = zoomed into a location's stage
        GUIStyle title, body, btn, box, node, ctr, lockBig;
        Texture2D line;

        // hiding the marketplace behind the map: render only the nebula backdrop while open
        const int BackdropLayer = 30;
        Camera cam;
        GameObject backdrop;
        int savedMask = int.MinValue;
        bool hidden;

        // store ↔ map ZOOM: a hidden camera mirrors the main camera into an RT, which we draw at a rect
        // that lerps between full-screen (the live store) and the MAIN STORE node — a seamless zoom.
        Camera zoomCam;
        RenderTexture storeRT;
        float zoomT;        // 0 = store full-screen (live), 1 = store shrunk into its map node
        int zoomDir;        // +1 = zooming out to the map, -1 = zooming back into the store, 0 = idle
        const float ZoomDur = 0.5f;
        bool Active => zoomT > 0f || zoomDir != 0;

        void Setup()
        {
            ready = true;
            title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            title.normal.textColor = new Color(0.60f, 0.95f, 1f);
            body = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true, wordWrap = true };
            body.normal.textColor = new Color(0.86f, 0.90f, 0.96f);
            ctr = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            ctr.normal.textColor = Color.white;
            btn = new GUIStyle(GUI.skin.button) { fontSize = 12, richText = true };
            box = new GUIStyle(GUI.skin.box);
            node = new GUIStyle(GUI.skin.button) { fontSize = 11, richText = true };
            lockBig = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
            lockBig.normal.textColor = new Color(1f, 0.55f, 0.55f);
            line = new Texture2D(1, 1); line.SetPixel(0, 0, Color.white); line.Apply();
        }

        // Hide the live 3D marketplace while the map is up (render only the nebula backdrop behind the map).
        // Logic keeps running; instantly reversible.
        void SetWorldHidden(bool hide)
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
                cam.cullingMask = backdrop != null ? (1 << BackdropLayer) : 0;
            }
            else if (savedMask != int.MinValue) cam.cullingMask = savedMask;
            hidden = hide;
        }

        // A hidden camera that MIRRORS the main camera into a screen-aspect RT, so drawing the RT
        // full-screen looks identical to the live game (seamless start/end of the zoom).
        void EnsureZoomCam()
        {
            var main = Camera.main; if (main == null) return;
            int rw = Mathf.Clamp(Screen.width, 16, 1280), rh = Mathf.Clamp(Screen.height, 16, 720);
            if (storeRT == null || storeRT.width != rw || storeRT.height != rh)
            {
                if (storeRT != null) { if (zoomCam != null) zoomCam.targetTexture = null; storeRT.Release(); }
                storeRT = new RenderTexture(rw, rh, 16) { name = "StoreZoomRT" };
                if (zoomCam != null) zoomCam.targetTexture = storeRT;
            }
            if (zoomCam == null)
            {
                zoomCam = new GameObject("StoreZoomCam").AddComponent<Camera>();
                zoomCam.enabled = false;
                zoomCam.targetTexture = storeRT;
            }
            zoomCam.orthographic = main.orthographic;
            zoomCam.orthographicSize = main.orthographicSize;
            zoomCam.fieldOfView = main.fieldOfView;
            zoomCam.nearClipPlane = main.nearClipPlane; zoomCam.farClipPlane = main.farClipPlane;
            zoomCam.transform.SetPositionAndRotation(main.transform.position, main.transform.rotation);
            zoomCam.clearFlags = CameraClearFlags.SolidColor;
            zoomCam.backgroundColor = main.backgroundColor;
            zoomCam.cullingMask = savedMask != int.MinValue ? savedMask : ~0;   // the full live view, incl. nebula
            zoomCam.aspect = (float)rw / rh;
        }

        void StartOpen()
        {
            SetWorldHidden(true);
            EnsureZoomCam();
            if (zoomCam != null) { zoomCam.enabled = true; zoomCam.Render(); }   // prime so frame 0 isn't blank
            zoomDir = 1;
        }

        void StartClose() { zoomDir = -1; }   // animates back; world is restored when zoomT hits 0

        void ResetZoom()
        {
            zoomDir = 0; zoomT = 0f; zoomLoc = null;
            SetWorldHidden(false);
            if (zoomCam != null) zoomCam.enabled = false;
        }

        static Rect LerpRect(Rect a, Rect b, float t) =>
            new Rect(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t), Mathf.Lerp(a.width, b.width, t), Mathf.Lerp(a.height, b.height, t));
        static float Ease(float t) => t * t * (3f - 2f * t);   // smoothstep

        // The MAIN STORE node's inner image rect (where the store sits when fully zoomed out).
        static Rect HubInner(float W, float H)
        {
            const float tw = 152f, thh = 96f;
            Vector2 store = new Vector2(W * 0.5f, 40 + (H - 120) * 0.60f);
            return new Rect(store.x - tw / 2f + 8, store.y - thh / 2f + 8, tw - 16, thh - 16);
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
            if (sim.Editing || Franchise.PanelOpen)   // a higher-priority screen owns the view
            {
                if (Active || hidden) ResetZoom();
                MapOpen = false; return;
            }
            if (!ready) Setup();
            float W = Screen.width, H = Screen.height;
            EnsureZoomCam();   // keep mirroring the main camera each frame

            // advance the zoom animation
            if (zoomDir != 0)
            {
                zoomT = Mathf.MoveTowards(zoomT, zoomDir > 0 ? 1f : 0f, Time.unscaledDeltaTime / ZoomDur);
                if (zoomDir > 0 && zoomT >= 1f) zoomDir = 0;
                else if (zoomDir < 0 && zoomT <= 0f) ResetZoom();
            }

            // bottom-left toggle — opens (zoom OUT to the map) / closes (zoom IN to the store)
            if (GUI.Button(new Rect(12, H - 76, 220, 30), Active ? "CLOSE MAP" : "GALAXY MAP", btn)) { if (Active) StartClose(); else StartOpen(); }
            MapOpen = Active;
            if (!Active) return;

            bool fullyOpen = zoomT >= 1f;
            if (fullyOpen && zoomLoc != null) { DrawZoom(W, H, sim, e); return; }   // inside a location stage

            // dim backdrop (fades in with the zoom)
            { var dc = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.5f * Mathf.Clamp01(zoomT)); GUI.DrawTexture(new Rect(0, 0, W, H), line); GUI.color = dc; }

            if (fullyOpen) DrawMap(W, H, sim, e);   // the network appears once we're zoomed out

            // the store itself — drawn on top, moving between full-screen and its MAIN STORE node
            Rect rt = LerpRect(new Rect(0, 0, W, H), HubInner(W, H), Ease(zoomT));
            if (storeRT != null) GUI.DrawTexture(rt, storeRT, ScaleMode.ScaleAndCrop, false);
            if (fullyOpen && GUI.Button(rt, GUIContent.none, GUIStyle.none)) StartClose();   // click the store node → zoom back in
        }

        // The supply network around the store (everything except the live store render, which OnGUI draws).
        void DrawMap(float W, float H, Sim sim, Economy e)
        {
            GUI.Box(new Rect(W * 0.5f - 470, 40, 940, H - 120), GUIContent.none, box);
            GUI.Label(new Rect(W * 0.5f - 446, 54, 800, 26), "GALAXY MAP   ·   supply network", title);
            if (GUI.Button(new Rect(W * 0.5f + 392, 50, 70, 28), "Close", btn)) { StartClose(); return; }

            var locs = Locations.All;
            Vector2 store = new Vector2(W * 0.5f, 40 + (H - 120) * 0.60f);
            float R = Mathf.Min(W, H) * 0.32f;

            for (int i = 0; i < locs.Count; i++)
            {
                Vector2 p = NodePos(store, R, i, locs.Count);
                int lv = sim.LocLev(locs[i].id);
                Color lc = lv > 0 ? new Color(0.40f, 0.85f, 1f, Mathf.Clamp01(0.30f + 0.10f * lv)) : new Color(0.5f, 0.5f, 0.6f, 0.16f);
                Line(store, p, lv > 0 ? 2f + Mathf.Min(8, lv) : 2f, lc);
                Shuttles(p, store, lv);
            }

            // store hub frame + labels (the live render is drawn by OnGUI at the same inner rect)
            const float tw = 152f, thh = 96f;
            var fr = new Rect(store.x - tw / 2f, store.y - thh / 2f, tw, thh);
            GUI.Box(fr, GUIContent.none, box);
            GUI.Label(new Rect(store.x - 110, fr.y - 24, 220, 20), "MAIN STORE", ctr);
            GUI.Label(new Rect(store.x - 110, fr.yMax + 2, 220, 18), "<color=#6BE08A>" + Economy.Fmt(sim.EstIncomePerSec()) + "/s</color>", ctr);

            for (int i = 0; i < locs.Count; i++)
            {
                var d = locs[i]; Vector2 p = NodePos(store, R, i, locs.Count);
                var r = new Rect(p.x - 62, p.y - 39, 124, 78);
                int lv = sim.LocLev(d.id); bool unl = sim.LocUnlocked(d.id);
                string status = lv > 0 ? "Lv " + lv : (unl ? "build" : "locked");
                var art = d.Art(lv);
                var prev = GUI.color; GUI.color = unl ? Color.white : new Color(1, 1, 1, 0.6f);
                bool clicked = GUI.Button(r, GUIContent.none, node);
                if (art != null) GUI.DrawTexture(new Rect(r.x + 6, r.y + 6, r.width - 12, 44), art, ScaleMode.ScaleAndCrop, false);
                else { GUI.color = d.color; GUI.DrawTexture(new Rect(r.x + (r.width - 16) / 2f, r.y + 18, 16, 16), line); GUI.color = prev; }
                GUI.Label(new Rect(r.x, r.y + 50, r.width, 16), "<b>" + d.name + "</b>", ctr);
                GUI.Label(new Rect(r.x, r.y + 64, r.width, 13), "<color=#8A98B0>" + status + "</color>", ctr);
                GUI.color = prev;
                if (clicked) zoomLoc = d.id;
            }
        }

        // Zoomed-in PLAYABLE STAGE (data-driven; see Stages.cs / StageManager). A 3-step task chain on the
        // location's floorplan — each step done by hand, then automated by hiring its employee. Sustained
        // shipping drives the efficiency that boosts the main store. Same engine for all five locations.
        void DrawZoom(float W, float H, Sim sim, Economy e)
        {
            var d = Locations.ById(zoomLoc);
            var def = Stages.For(zoomLoc);
            var sm = StageManager.Instance;
            bool unl = sim.LocUnlocked(zoomLoc);
            bool built = sim.LocLev(zoomLoc) > 0;

            var pr = new Rect(W * 0.5f - 470, 40, 940, H - 120);
            GUI.Box(pr, GUIContent.none, box);
            if (GUI.Button(new Rect(pr.x + 18, pr.y + 14, 160, 30), "◀  BACK TO MAP", btn)) { zoomLoc = null; return; }
            GUI.Label(new Rect(pr.x + 190, pr.y + 16, pr.width - 380, 26), "<b>" + d.name.ToUpper() + (def != null ? "  —  " + def.title : "") + "</b>", title);

            var ir = new Rect(pr.x + 24, pr.y + 58, pr.width - 48, pr.height - 250);
            GUI.Box(ir, GUIContent.none, box);
            var inner = new Rect(ir.x + 8, ir.y + 8, ir.width - 16, ir.height - 16);
            var art = built ? (d.IconFull ?? d.IconEmpty) : (d.IconEmpty ?? d.IconFull);
            if (art != null) { var pcol = GUI.color; if (!unl) GUI.color = new Color(1, 1, 1, 0.4f); GUI.DrawTexture(inner, art, ScaleMode.ScaleAndCrop, false); GUI.color = pcol; }

            if (!unl)
            {
                GUI.Label(inner, "LOCKED", lockBig);
                int idx = Locations.Index(zoomLoc);
                GUI.Label(new Rect(pr.x + 24, ir.yMax + 16, pr.width - 48, 24), "<color=#E88A8A>Locked</color> — build the <b>" + Locations.All[idx - 1].name + "</b> first", body);
                return;
            }
            if (!built || def == null || sm == null)
            {
                GUI.Label(inner, "<b>EMPTY LOT</b>", lockBig);
                double bc = sim.LocCost(zoomLoc);
                GUI.enabled = e.Money >= bc;
                if (GUI.Button(new Rect(pr.x + pr.width / 2f - 150, ir.yMax + 22, 300, 48), "CONSTRUCT\n<color=#FFD24A>" + Economy.Fmt(bc) + "</color>", btn)) sim.BuildLocation(zoomLoc);
                GUI.enabled = true;
                GUI.Label(new Rect(pr.x + 24, ir.yMax + 82, pr.width - 48, 22), "<color=#8A98B0>Construct this facility, then run its line to boost the store.</color>", body);
                return;
            }

            // --- the 3-step chain over the floorplan ---
            var s = sm.Get(zoomLoc);
            int cap = sm.Cap(s);
            float colW = inner.width / 3f, bandY = inner.y + inner.height * 0.60f, btnW = colW * 0.62f;
            for (int t = 0; t < 3; t++)
            {
                var tk = def.tasks[t];
                float cx = inner.x + colW * (t + 0.5f);
                GUI.Label(new Rect(cx - colW * 0.5f, inner.y + 8, colW, 18), "<b>" + (t + 1) + ". " + tk.role + "</b>", ctr);
                int inAmt = t == 0 ? s.QueueCount : (t == 1 ? s.buf0 : s.buf1);
                int outAmt = t == 0 ? s.buf0 : (t == 1 ? s.buf1 : s.shipped);
                if (t == 0) Tokens(cx - (Mathf.Min(inAmt, 6) * 15) / 2f + 7, inner.y + 30, Mathf.Min(inAmt, 6), new Color(1f, 0.82f, 0.3f, 0.95f));
                else GUI.Label(new Rect(cx - colW * 0.5f, inner.y + 28, colW, 16), "<color=#8A98B0>in " + inAmt + "</color>", ctr);
                if (t < 2)
                {
                    var mb = new Rect(cx - btnW / 2f, inner.y + 46, btnW, 10);
                    GUI.color = new Color(1, 1, 1, 0.15f); GUI.DrawTexture(mb, line);
                    GUI.color = new Color(0.2f, 0.9f, 0.7f, 0.95f); GUI.DrawTexture(new Rect(mb.x, mb.y, mb.width * Mathf.Clamp01(outAmt / (float)cap), mb.height), line);
                    GUI.color = Color.white;
                    GUI.Label(new Rect(cx - colW * 0.5f, inner.y + 58, colW, 14), "<color=#CFE0FF>" + outAmt + "/" + cap + "</color>", ctr);
                }
                else GUI.Label(new Rect(cx - colW * 0.5f, inner.y + 48, colW, 16), "<color=#CFE0FF>shipped " + outAmt + "</color>", ctr);
                GUI.enabled = sm.CanDo(s, t);
                if (GUI.Button(new Rect(cx - btnW / 2f, bandY, btnW, 30), tk.verb + " ▸", btn)) sm.DoTask(s, t);
                GUI.enabled = true;
                if (s.emp[t] > 0) GUI.Label(new Rect(cx - colW * 0.5f, bandY + 32, colW, 16), "<color=#6BE08A>▶ auto ×" + s.emp[t] + "</color>", ctr);
            }
            GUI.Label(new Rect(inner.x, inner.yMax - 22, inner.width, 16), "<color=#6BE08A>" + def.outLabel + "</color>", ctr);

            // --- control area: efficiency + boost, hire the 3 employees, 2 upgrades ---
            float cy = ir.yMax + 10;
            GUI.Label(new Rect(pr.x + 24, cy, pr.width - 48, 22),
                "Efficiency <color=#CFE0FF>" + Mathf.RoundToInt(sm.Eff(zoomLoc) * 100f) + "%</color>  →  " + sm.BoostText(zoomLoc) +
                "     <color=#8A98B0>store</color> <color=#6BE08A>" + Economy.Fmt(sim.EstIncomePerSec()) + "/s</color>", body);
            float gap = 12f, cw3 = (pr.width - 48 - 2 * gap) / 3f, r1 = cy + 28, r2 = r1 + 44;
            for (int t = 0; t < 3; t++) { int ti = t; CtrlBtn(pr.x + 24 + t * (cw3 + gap), r1, cw3, e, "Hire " + def.tasks[t].employee + " ×" + s.emp[t], sm.EmpCost(s, t), () => sm.HireEmp(s, ti)); }
            CtrlBtn(pr.x + 24 + 0 * (cw3 + gap), r2, cw3, e, "Faster intake Lv" + s.upArrival, sm.ArrUpCost(s), () => sm.UpgradeArrival(s));
            CtrlBtn(pr.x + 24 + 1 * (cw3 + gap), r2, cw3, e, "Bigger storage Lv" + s.upCapacity, sm.CapUpCost(s), () => sm.UpgradeCapacity(s));
            GUI.Label(new Rect(pr.x + 24 + 2 * (cw3 + gap), r2, cw3, 40), "<color=#8A98B0>total shipped</color>\n<color=#CFE0FF>" + s.shipped + "</color>", ctr);
        }

        // Tiny token squares (crates / vans waiting in a queue).
        void Tokens(float x, float y, int n, Color c, float s = 11f, float gap = 4f)
        {
            var pc = GUI.color; GUI.color = c;
            for (int i = 0; i < n; i++) GUI.DrawTexture(new Rect(x + i * (s + gap), y, s, s), line);
            GUI.color = pc;
        }

        void CtrlBtn(float x, float y, float w, Economy e, string label, int cost, System.Func<bool> action)
        {
            GUI.enabled = e.Money >= cost;
            if (GUI.Button(new Rect(x, y, w, 40), label + "\n<color=#FFD24A>" + Economy.Fmt(cost) + "</color>", node)) action();
            GUI.enabled = true;
        }

        static Vector2 NodePos(Vector2 store, float R, int i, int n)
        {
            float a = Mathf.Lerp(160f, 20f, n <= 1 ? 0.5f : (float)i / (n - 1)) * Mathf.Deg2Rad;
            return new Vector2(store.x + R * Mathf.Cos(a), store.y - R * Mathf.Sin(a));
        }
    }
}
