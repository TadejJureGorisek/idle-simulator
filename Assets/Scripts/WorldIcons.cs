using UnityEngine;

namespace IdleSim
{
    // World-anchored IMGUI overlays drawn ABOVE shelves/customers, so they stay readable at any
    // shop or shelf rotation: a rounded fullness bar + "!" over shelves (click = restock), and a
    // "$" over customers waiting at the till.
    public class WorldIcons : MonoBehaviour
    {
        Texture2D pill, exclTex, dollarTex;
        GUIStyle countStyle, exclStyle, dollarStyle;
        bool ready;
        Camera cam;

        void Setup()
        {
            ready = true;
            pill = MakePill(64, 14);
            exclTex = MakeCircle(48, new Color(0.96f, 0.72f, 0.10f));
            dollarTex = MakeCircle(48, new Color(0.20f, 0.78f, 0.32f));
            countStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            countStyle.normal.textColor = Color.white;
            exclStyle = IconStyle(exclTex);
            dollarStyle = IconStyle(dollarTex);
        }

        static GUIStyle IconStyle(Texture2D bg)
        {
            var s = new GUIStyle(GUI.skin.button) { fontSize = 22, fontStyle = FontStyle.Bold, border = new RectOffset(0, 0, 0, 0) };
            s.normal.background = bg; s.normal.textColor = Color.white;
            s.hover.background = bg; s.hover.textColor = Color.white;
            s.active.background = bg; s.active.textColor = Color.white;
            return s;
        }

        void OnGUI()
        {
            var sim = Sim.Instance;
            if (sim == null) return;
            if (GalaxyMap.MapOpen || Franchise.PanelOpen) return;   // hide world badges behind a modal
            if (!ready) Setup();
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            foreach (var s in sim.Shelves)
            {
                if (s == null) continue;
                bool restock = s.Stock == 0
                    ? Badge(s.transform.position + Vector3.up * 2.2f, "!", exclStyle, true)        // empty -> just the "!"
                    : Fullness(s.transform.position + Vector3.up * 2.0f, s.Ratio, s.Stock, s.Capacity);
                if (restock) s.RestockAffordable();
            }

            if (sim.Checkout != null)
                foreach (var c in sim.Checkout.Line)
                    if (c != null && c.InLine)
                        Badge(c.transform.position + Vector3.up * 1.3f, "$", dollarStyle, false);

            GUI.color = Color.white;
        }

        // Rounded fullness bar with a colour-graded fill + count. Returns true if clicked.
        bool Fullness(Vector3 world, float ratio, int stock, int cap)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0) return false;
            const float bw = 64f, bh = 14f;
            float bx = sp.x - bw / 2f, by = Screen.height - sp.y - bh / 2f;

            GUI.color = new Color(0f, 0f, 0f, 0.55f);                       // dark track
            GUI.DrawTexture(new Rect(bx, by, bw, bh), pill);

            float fw = Mathf.Clamp01(ratio) * bw;                          // colour-graded fill
            if (fw > 1f)
            {
                GUI.color = Color.Lerp(new Color(0.88f, 0.27f, 0.22f), new Color(0.26f, 0.82f, 0.36f), ratio);
                GUI.BeginGroup(new Rect(bx, by, fw, bh));
                GUI.DrawTexture(new Rect(0, 0, bw, bh), pill);
                GUI.EndGroup();
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(bx, by - 1f, bw, bh), stock + "/" + cap, countStyle);
            return GUI.Button(new Rect(bx, by, bw, bh), GUIContent.none, GUIStyle.none);
        }

        bool Badge(Vector3 world, string txt, GUIStyle style, bool clickable)
        {
            Vector3 sp = cam.WorldToScreenPoint(world);
            if (sp.z <= 0) return false;
            const float size = 30f;
            var r = new Rect(sp.x - size / 2f, Screen.height - sp.y - size / 2f, size, size);
            GUI.color = Color.white;
            if (clickable) return GUI.Button(r, txt, style);
            GUI.Box(r, txt, style);
            return false;
        }

        static Texture2D MakePill(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = h / 2f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x, r, w - r);
                    float dx = x - cx, dy = y - r;
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            t.Apply();
            return t;
        }

        static Texture2D MakeCircle(int size, Color col)
        {
            var t = new Texture2D(size, size, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            float rad = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - rad + 0.5f, dy = y - rad + 0.5f;
                    float a = Mathf.Clamp01(rad - Mathf.Sqrt(dx * dx + dy * dy));
                    t.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * a));
                }
            t.Apply();
            return t;
        }
    }
}
