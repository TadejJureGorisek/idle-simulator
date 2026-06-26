using UnityEngine;

namespace IdleSim
{
    // Round "+" button below the gameplay opens edit mode: shows a floor grid, hard-pauses the
    // store, and lets you drag shelves / checkout / dividers (grid-snapped) and buy new ones.
    // Exiting rebuilds nav, saves the layout, and gives customers a brief walk-through grace.
    public class StoreEditor : MonoBehaviour
    {
        public int DividerCost = 40;
        const float GridCell = 1.0f; // visual grid cell size (an NPC is a bit under one cell)

        Texture2D circle, circleHi;
        GUIStyle round, label, btn, box;
        bool ready;
        Transform dragging;
        Transform selected;
        float dragY;
        GameObject grid;

        bool Editing => Sim.Instance != null && Sim.Instance.Editing;

        void Toggle()
        {
            if (Sim.Instance == null) return;
            if (!Editing)
            {
                EnsureGrid();
                SizeGridToFloor();
                grid.SetActive(true);
                Sim.Instance.EnterEdit();
            }
            else
            {
                Sim.Instance.ResumeFromEdit();
                if (grid != null) grid.SetActive(false);
            }
        }

        void Update()
        {
            if (!Editing) { dragging = null; return; }
            if (Camera.main == null) return;

            if (Input.GetMouseButtonDown(0) && !PointerOverUI())
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Transform picked = null;
                if (Physics.Raycast(ray, out RaycastHit hit, 300f)) picked = PickEditable(hit.collider);
                dragging = picked;
                selected = picked; // click a fixture to select it; click empty to deselect
                if (dragging != null) dragY = dragging.position.y;
            }

            if (dragging != null && Input.GetMouseButton(0))
            {
                var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                var plane = new Plane(Vector3.up, new Vector3(0, dragY, 0));
                if (plane.Raycast(ray, out float ent))
                {
                    var world = ray.GetPoint(ent);
                    var shop = Sim.Instance.ShopRoot;
                    if (shop != null)
                    {
                        var local = shop.InverseTransformPoint(world);   // snap in shop space so it
                        local.x = Mathf.Round(local.x / 0.5f) * 0.5f;     // aligns with the rotated grid
                        local.z = Mathf.Round(local.z / 0.5f) * 0.5f;
                        local.y = shop.InverseTransformPoint(dragging.position).y;
                        var w = shop.TransformPoint(local);
                        dragging.position = new Vector3(w.x, dragY, w.z);
                    }
                    else
                    {
                        world.x = Mathf.Round(world.x / 0.5f) * 0.5f;
                        world.z = Mathf.Round(world.z / 0.5f) * 0.5f;
                        world.y = dragY;
                        dragging.position = world;
                    }
                }
            }

            if (Input.GetMouseButtonUp(0) && dragging != null)
            {
                dragging = null;
                Sim.Instance.RebuildNav();
                Sim.Instance.SaveLayout();
            }

            if (Input.GetKeyDown(KeyCode.Q)) Rotate(-45f);
            if (Input.GetKeyDown(KeyCode.E)) Rotate(45f);
            if (Input.GetKeyDown(KeyCode.R)) RotateSelected(45f);
        }

        void Rotate(float deg)
        {
            var sim = Sim.Instance;
            if (sim == null) return;
            sim.ShopRotation = Mathf.Repeat(sim.ShopRotation + deg, 360f);
            sim.ApplyShopRotation();
            sim.RebuildNav();
            sim.SaveLayout();
        }

        void RotateSelected(float deg)
        {
            if (selected == null || Sim.Instance == null) return;
            selected.Rotate(0f, deg, 0f, Space.Self); // rotate the picked fixture in place
            Sim.Instance.RebuildNav();
            Sim.Instance.SaveLayout();
        }

        Transform PickEditable(Collider c)
        {
            var sh = c.GetComponentInParent<Shelf>(); if (sh != null) return sh.transform;
            var co = c.GetComponentInParent<Checkout>(); if (co != null) return co.transform;
            var dv = c.GetComponentInParent<Divider>(); if (dv != null) return dv.transform;
            var de = c.GetComponentInParent<Decor>(); if (de != null) return de.transform;
            return null;
        }

        Vector2 catScroll;

        // Place a catalog item near the shop front, then grab it for dragging.
        void PlaceItem(CatalogItem it)
        {
            var sim = Sim.Instance; var e = Economy.Instance;
            Vector3 spot = sim.ShopRoot != null ? sim.ShopRoot.TransformPoint(new Vector3(0, 0, 1.5f)) : new Vector3(0, 0, 1.5f);
            Transform t = null;
            if (it.kind == ItemKind.Stand)
            {
                if (e == null || !e.TrySpend(StandCost())) return; // each stand costs more than the last
                var sh = sim.AddStand(it, spot); if (sh != null) t = sh.transform;
            }
            else t = sim.AddDecor(it, spot); // decorations are free
            if (t != null) { selected = dragging = t; dragY = t.position.y; }
            sim.RebuildNav();
            sim.SaveLayout();
        }

        void AddDivider()
        {
            var sim = Sim.Instance;
            Vector3 spot = sim.ShopRoot != null ? sim.ShopRoot.TransformPoint(new Vector3(0, 0, 1.5f)) : new Vector3(0, 0, 1.5f);
            var t = sim.AddDivider(spot);
            if (t != null) { selected = dragging = t; dragY = t.position.y; }
            sim.RebuildNav();
            sim.SaveLayout();
        }

        // Each shelf placed makes the next pricier, so the shop grows slowly; unlocks scale too.
        static double StandCost() => 50.0 * System.Math.Pow(1.18, Sim.Instance.Shelves.Count);
        static double UnlockCost() => 200.0 * System.Math.Pow(1.4, Mathf.Max(0, Sim.Instance.UnlockedItems - 1));

        void DrawCatalog()
        {
            var sim = Sim.Instance; var e = Economy.Instance;
            if (sim == null || e == null) return;
            const float x = 10f, y = 110f, w = 268f, h = 420f;
            GUI.Box(new Rect(x - 4, y - 4, w + 8, h + 8), GUIContent.none, box);
            GUI.Label(new Rect(x, y, w, 20), "CATALOG", label);

            if (sim.UnlockedItems < Catalog.Items.Count)
            {
                double uc = UnlockCost();
                var next = Catalog.Items[sim.UnlockedItems];
                GUI.enabled = e.Money >= uc;
                if (GUI.Button(new Rect(x, y + 22, w - 8, 26), "Unlock: " + next.name + "    " + ProducerEconomy.Money(uc), btn) && e.TrySpend(uc))
                { sim.UnlockedItems++; sim.SaveLayout(); }
                GUI.enabled = true;
            }
            else GUI.Label(new Rect(x, y + 24, w, 20), "All items unlocked.", label);

            int cnt = Mathf.Min(sim.UnlockedItems, Catalog.Items.Count);
            var view = new Rect(x, y + 54, w, h - 54);
            var content = new Rect(0, 0, w - 20, cnt * 30);
            catScroll = GUI.BeginScrollView(view, catScroll, content);
            for (int i = 0; i < cnt; i++)
            {
                var it = Catalog.Items[i];
                string lbl = it.kind == ItemKind.Stand ? it.name + "    " + ProducerEconomy.Money(StandCost()) : it.name + "    (decor)";
                GUI.enabled = it.kind == ItemKind.Decor || e.Money >= StandCost();
                if (GUI.Button(new Rect(0, i * 30, w - 20, 28), lbl, btn)) PlaceItem(it);
                GUI.enabled = true;
            }
            GUI.EndScrollView();
        }

        void EnsureGrid()
        {
            if (grid != null) return;
            grid = GameObject.CreatePrimitive(PrimitiveType.Quad);
            grid.name = "EditGrid";
            var col = grid.GetComponent<Collider>();
            if (col != null) Destroy(col);
            if (Sim.Instance != null && Sim.Instance.ShopRoot != null)
                grid.transform.SetParent(Sim.Instance.ShopRoot, false); // spin with the shop
            grid.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            var mat = new Material(Shader.Find("Unlit/Transparent"));
            mat.mainTexture = MakeGrid(32);
            grid.GetComponent<Renderer>().material = mat;
            grid.SetActive(false);
        }

        // Match the grid exactly to the current store floor, so it stays 1:1 even if the floor
        // is later resized by an upgrade. One tile per 0.5 m snap cell.
        void SizeGridToFloor()
        {
            if (grid == null || Sim.Instance == null) return;
            float w = Sim.Instance.FloorWidth, d = Sim.Instance.FloorDepth;
            var c = Sim.Instance.FloorCenter;
            grid.transform.localPosition = new Vector3(c.x, 0.03f, c.z);
            grid.transform.localScale = new Vector3(w, d, 1f);
            var mat = grid.GetComponent<Renderer>().sharedMaterial;
            if (mat != null) mat.mainTextureScale = new Vector2(w / GridCell, d / GridCell);
        }

        // ---- GUI ----
        Rect PlusRect() => new Rect(Screen.width * 0.5f - 26, Screen.height - 70, 52, 52);
        Rect ToolbarRect() => new Rect(Screen.width * 0.5f - 235, Screen.height - 134, 470, 58);

        bool PointerOverUI()
        {
            Vector2 m = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
            if (PlusRect().Contains(m)) return true;
            if (Editing && ToolbarRect().Contains(m)) return true;
            if (new Rect(8, 8, 344, 150).Contains(m)) return true;                  // HUD money panel
            if (new Rect(Screen.width - 280, 4, 280, 430).Contains(m)) return true;  // HUD upgrades panel
            if (Editing && new Rect(6, 106, 274, 428).Contains(m)) return true;      // catalog panel
            return false;
        }

        void Setup()
        {
            ready = true;
            circle = MakeCircle(64, new Color(0.20f, 0.55f, 0.85f, 1f));
            circleHi = MakeCircle(64, new Color(0.30f, 0.70f, 1.0f, 1f));

            round = new GUIStyle(GUI.skin.button) { fontSize = 30, fontStyle = FontStyle.Bold, border = new RectOffset(0, 0, 0, 0) };
            round.normal.background = circle; round.normal.textColor = Color.white;
            round.hover.background = circleHi; round.hover.textColor = Color.white;
            round.active.background = circleHi; round.active.textColor = Color.white;

            label = new GUIStyle(GUI.skin.label) { fontSize = 12 };
            label.normal.textColor = new Color(0.85f, 0.90f, 1f);
            btn = new GUIStyle(GUI.skin.button) { fontSize = 12 };
            box = new GUIStyle(GUI.skin.box);
        }

        void OnGUI()
        {
            if (Sim.Instance == null) return;
            if (!ready) Setup();

            if (GUI.Button(PlusRect(), Editing ? "✕" : "+", round)) Toggle();

            if (Editing)
            {
                DrawCatalog();

                var tb = ToolbarRect();
                GUI.Box(tb, GUIContent.none, box);
                var e = Economy.Instance;

                GUI.enabled = e != null && e.Money >= DividerCost;
                if (GUI.Button(new Rect(tb.x + 10, tb.y + 8, 150, 20), "Add Divider   $" + DividerCost, btn) && e.TrySpend(DividerCost)) AddDivider();
                GUI.enabled = true;

                if (GUI.Button(new Rect(tb.x + 10, tb.y + 32, 74, 20), "-45° shop", btn)) Rotate(-45f);
                if (GUI.Button(new Rect(tb.x + 88, tb.y + 32, 74, 20), "+45° shop", btn)) Rotate(45f);
                GUI.enabled = selected != null;
                if (GUI.Button(new Rect(tb.x + 170, tb.y + 8, 104, 44), "Rotate Item (R)", btn)) RotateSelected(45f);
                GUI.enabled = true;

                GUI.Label(new Rect(tb.x + 282, tb.y + 8, 184, 46), "Pick from CATALOG (left).\nDrag to move · R rotates item.\nClick ✕ when done.", label);
            }
        }

        static Texture2D MakeGrid(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Repeat };
            var line = new Color(0.40f, 0.80f, 1f, 0.5f);
            var clear = new Color(0, 0, 0, 0);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    t.SetPixel(x, y, (x < 2 || y < 2) ? line : clear);
            t.Apply();
            return t;
        }

        static Texture2D MakeCircle(int size, Color col)
        {
            var t = new Texture2D(size, size, TextureFormat.ARGB32, false) { wrapMode = TextureWrapMode.Clamp };
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f, dy = y - r + 0.5f;
                    float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy));
                    t.SetPixel(x, y, new Color(col.r, col.g, col.b, col.a * a));
                }
            t.Apply();
            return t;
        }
    }
}
