using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    public enum ItemKind { Stand, Decor }

    public class CatalogItem
    {
        public string id, name;
        public ItemKind kind;
        public string shape;   // stand / box / cyl / sphere / plant / lamp / sign
        public Vector3 size;
        public Color color;
        public int capacity;   // stands: stock capacity

        public CatalogItem(string id, string name, ItemKind kind, string shape, Vector3 size, Color color, int cap = 0)
        {
            this.id = id; this.name = name; this.kind = kind; this.shape = shape; this.size = size; this.color = color; capacity = cap;
        }
    }

    // The placeable catalog: 10 stands (functional shelves, varied footprints) + 30 decorations.
    // Items unlock one-by-one in this order. Index 0 (Basic Shelf) is available from the start.
    public static class Catalog
    {
        static List<CatalogItem> _items;
        public static List<CatalogItem> Items { get { if (_items == null) _items = Build(); return _items; } }

        public static CatalogItem ById(string id)
        {
            foreach (var it in Items) if (it.id == id) return it;
            return Items[0];
        }

        static List<CatalogItem> Build()
        {
            var L = new List<CatalogItem>();
            // --- 10 STANDS (functional shelves; basic first = unlocked at start) ---
            L.Add(St("st_basic", "Basic Shelf", 1, 2, 20));
            L.Add(St("st_small", "Small Stand", 1, 1, 10));
            L.Add(St("st_wide", "Wide Shelf", 2, 1, 20));
            L.Add(St("st_long", "Long Aisle", 1, 3, 30));
            L.Add(St("st_double", "Double Shelf", 2, 2, 40));
            L.Add(St("st_big", "Big Aisle", 1, 4, 40));
            L.Add(St("st_endcap", "End Cap", 1, 1, 16));
            L.Add(St("st_island", "Island Display", 2, 2, 36));
            L.Add(St("st_corner", "Corner Stand", 2, 2, 30));
            L.Add(St("st_mega", "Mega Rack", 2, 3, 60));
            // --- 30 DECOR (cosmetic) ---
            Color green = new Color(0.25f, 0.6f, 0.25f), grey = new Color(0.5f, 0.5f, 0.55f), red = new Color(0.8f, 0.3f, 0.25f);
            Color blue = new Color(0.3f, 0.5f, 0.8f), wood = new Color(0.5f, 0.38f, 0.25f), gold = new Color(0.85f, 0.7f, 0.3f), cyan = new Color(0.2f, 0.7f, 0.8f);
            L.Add(Dc("dc_plant1", "Potted Plant", "plant", V(0.5f, 0.9f, 0.5f), green));
            L.Add(Dc("dc_plant2", "Big Plant", "plant", V(0.7f, 1.5f, 0.7f), green));
            L.Add(Dc("dc_tree", "Indoor Tree", "plant", V(0.9f, 2.4f, 0.9f), green));
            L.Add(Dc("dc_cactus", "Cactus", "cyl", V(0.35f, 1.1f, 0.35f), new Color(0.3f, 0.55f, 0.3f)));
            L.Add(Dc("dc_bin", "Trash Bin", "cyl", V(0.5f, 0.9f, 0.5f), grey));
            L.Add(Dc("dc_barrel", "Barrel", "cyl", V(0.6f, 1.0f, 0.6f), wood));
            L.Add(Dc("dc_bollard", "Bollard", "cyl", V(0.25f, 1.0f, 0.25f), grey));
            L.Add(Dc("dc_pillar", "Pillar", "cyl", V(0.6f, 3.0f, 0.6f), new Color(0.7f, 0.7f, 0.72f)));
            L.Add(Dc("dc_bench", "Bench", "box", V(2.0f, 0.5f, 0.6f), wood));
            L.Add(Dc("dc_table", "Cafe Table", "box", V(0.9f, 0.8f, 0.9f), wood));
            L.Add(Dc("dc_crate", "Crate", "box", V(0.8f, 0.8f, 0.8f), wood));
            L.Add(Dc("dc_crates", "Crate Stack", "box", V(0.9f, 1.6f, 0.9f), wood));
            L.Add(Dc("dc_pallet", "Pallet", "box", V(1.2f, 0.2f, 1.0f), wood));
            L.Add(Dc("dc_rug", "Floor Rug", "box", V(2.5f, 0.06f, 1.8f), red));
            L.Add(Dc("dc_planter", "Planter Box", "box", V(1.6f, 0.5f, 0.5f), new Color(0.4f, 0.45f, 0.4f)));
            L.Add(Dc("dc_sign", "Aisle Sign", "sign", V(0.8f, 2.0f, 0.1f), blue));
            L.Add(Dc("dc_sign2", "Hanging Sign", "sign", V(1.2f, 1.6f, 0.1f), red));
            L.Add(Dc("dc_neon", "Neon Sign", "sign", V(1.4f, 0.7f, 0.1f), cyan));
            L.Add(Dc("dc_board", "Sandwich Board", "box", V(0.7f, 1.0f, 0.5f), wood));
            L.Add(Dc("dc_lamp", "Floor Lamp", "lamp", V(0.3f, 2.0f, 0.3f), gold));
            L.Add(Dc("dc_post", "Light Post", "lamp", V(0.3f, 3.2f, 0.3f), grey));
            L.Add(Dc("dc_lantern", "Lantern", "lamp", V(0.4f, 1.2f, 0.4f), gold));
            L.Add(Dc("dc_gumball", "Gumball Machine", "sphere", V(0.5f, 1.1f, 0.5f), red));
            L.Add(Dc("dc_atm", "ATM", "box", V(0.7f, 1.6f, 0.6f), new Color(0.3f, 0.35f, 0.45f)));
            L.Add(Dc("dc_vending", "Vending Machine", "box", V(1.0f, 2.0f, 0.8f), red));
            L.Add(Dc("dc_cooler", "Display Cooler", "box", V(1.2f, 2.0f, 0.8f), cyan));
            L.Add(Dc("dc_cart", "Shopping Cart", "box", V(0.6f, 1.0f, 0.9f), grey));
            L.Add(Dc("dc_baskets", "Basket Stack", "box", V(0.6f, 1.0f, 0.6f), blue));
            L.Add(Dc("dc_statue", "Statue", "cyl", V(0.6f, 2.2f, 0.6f), new Color(0.72f, 0.68f, 0.6f)));
            L.Add(Dc("dc_fountain", "Fountain", "cyl", V(1.6f, 0.8f, 1.6f), new Color(0.5f, 0.6f, 0.7f)));
            return L;
        }

        static Vector3 V(float a, float b, float c) => new Vector3(a, b, c);
        static CatalogItem St(string id, string name, float w, float d, int cap) =>
            new CatalogItem(id, name, ItemKind.Stand, "stand", new Vector3(w, 1.2f, d), new Color(0.45f, 0.40f, 0.35f), cap);
        static CatalogItem Dc(string id, string name, string shape, Vector3 size, Color color) =>
            new CatalogItem(id, name, ItemKind.Decor, shape, size, color);

        // Builds a placed instance (a stand has a Shelf; decor has a Decor marker).
        public static GameObject Build(CatalogItem it)
        {
            var root = new GameObject(it.name);
            if (it.shape == "stand")
            {
                var shelf = root.AddComponent<Shelf>();
                shelf.Capacity = it.capacity; shelf.Stock = it.capacity; shelf.catalogId = it.id;
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "Body";
                body.transform.SetParent(root.transform, false);
                body.transform.localScale = it.size;
                body.transform.localPosition = new Vector3(0, it.size.y / 2f, 0);
                body.GetComponent<Renderer>().material.color = it.color;
                return root;
            }

            switch (it.shape)
            {
                case "plant": BuildPlant(root.transform, it); break;
                case "lamp": BuildLamp(root.transform, it); break;
                case "sign": BuildSign(root.transform, it); break;
                case "cyl": Prim(root.transform, PrimitiveType.Cylinder, it.size, it.color); break;
                case "sphere": Prim(root.transform, PrimitiveType.Sphere, it.size, it.color); break;
                default: Prim(root.transform, PrimitiveType.Cube, it.size, it.color); break;
            }
            root.AddComponent<Decor>().catalogId = it.id;
            return root;
        }

        static void Prim(Transform parent, PrimitiveType t, Vector3 size, Color c)
        {
            var go = GameObject.CreatePrimitive(t);
            go.transform.SetParent(parent, false);
            go.transform.localScale = (t == PrimitiveType.Cylinder) ? new Vector3(size.x, size.y / 2f, size.z) : size;
            go.transform.localPosition = new Vector3(0, size.y / 2f, 0);
            go.GetComponent<Renderer>().material.color = c;
        }

        static void BuildPlant(Transform p, CatalogItem it)
        {
            float ph = it.size.y * 0.3f, fh = it.size.y * 0.7f;
            var pot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pot.transform.SetParent(p, false);
            pot.transform.localScale = new Vector3(it.size.x * 0.7f, ph / 2f, it.size.z * 0.7f);
            pot.transform.localPosition = new Vector3(0, ph / 2f, 0);
            pot.GetComponent<Renderer>().material.color = new Color(0.4f, 0.25f, 0.18f);
            var fol = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fol.transform.SetParent(p, false);
            fol.transform.localScale = new Vector3(it.size.x, fh, it.size.z);
            fol.transform.localPosition = new Vector3(0, ph + fh / 2f, 0);
            fol.GetComponent<Renderer>().material.color = it.color;
        }

        static void BuildLamp(Transform p, CatalogItem it)
        {
            var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.transform.SetParent(p, false);
            pole.transform.localScale = new Vector3(it.size.x * 0.4f, it.size.y / 2f, it.size.z * 0.4f);
            pole.transform.localPosition = new Vector3(0, it.size.y / 2f, 0);
            pole.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.32f);
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(p, false);
            head.transform.localScale = Vector3.one * it.size.x;
            head.transform.localPosition = new Vector3(0, it.size.y, 0);
            var m = head.GetComponent<Renderer>().material;
            m.color = it.color; m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", it.color * 0.8f);
        }

        static void BuildSign(Transform p, CatalogItem it)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.transform.SetParent(p, false);
            post.transform.localScale = new Vector3(0.1f, it.size.y / 2f, 0.1f);
            post.transform.localPosition = new Vector3(0, it.size.y / 2f, 0);
            post.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.33f);
            var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
            board.transform.SetParent(p, false);
            board.transform.localScale = new Vector3(it.size.x, it.size.x * 0.6f, it.size.z);
            board.transform.localPosition = new Vector3(0, it.size.y - it.size.x * 0.3f, 0);
            board.GetComponent<Renderer>().material.color = it.color;
        }
    }
}
