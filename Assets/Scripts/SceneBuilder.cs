using UnityEngine;

namespace IdleSim
{
    // Constructs the greybox layout. Used by the editor menu (LayoutBuilder) to author a
    // saved scene, and by IdleBootstrap as a runtime fallback for an empty scene.
    public static class SceneBuilder
    {
        public static GameObject Build()
        {
            var root = new GameObject("IdleSim");
            root.AddComponent<Economy>();
            var sim = root.AddComponent<Sim>();
            root.AddComponent<HUD>();
            root.AddComponent<StoreEditor>();

            var cam = SetupCamera();
            SetupLight();
            BuildBackdrop(cam);

            // Everything that makes up the shop hangs off ShopRoot, so the whole thing can rotate.
            var shop = new GameObject("ShopRoot").transform;
            sim.ShopRoot = shop;

            BuildSpacePad(sim);
            sim.Pad.SetParent(shop, true);
            sim.PadRim.SetParent(shop, true);

            var shelfParent = new GameObject("Shelves").transform;
            shelfParent.SetParent(shop, true);

            var checkout = BuildCheckoutL(new Vector3(0f, 0f, -1f));
            checkout.transform.SetParent(shop, true);

            var spawner = new GameObject("Spawner").AddComponent<CustomerSpawner>();
            spawner.transform.SetParent(shop, true);

            sim.ShelfParent = shelfParent;
            sim.Checkout = checkout;
            sim.Spawner = spawner;

            BuildEntryDoor(sim); // double glass door at the front edge; sets Entrance + Exit
            if (sim.Entrance != null && sim.Entrance.parent != null) sim.Entrance.parent.SetParent(shop, true);

            sim.AddShelf(new Vector3(-3f, 0, 4f));
            sim.AddShelf(new Vector3(3f, 0, 4f));

            BuildPlaceholders(shop);

            sim.ApplyShopRotation();
            return root;
        }

        static Camera SetupCamera()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var cg = new GameObject("Main Camera") { tag = "MainCamera" };
                cam = cg.AddComponent<Camera>();
                cg.AddComponent<AudioListener>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 8.5f;          // a bit bigger (zoomed in)
            cam.transform.position = new Vector3(3f, 17f, -12.5f); // shifted so the store sits left of the panel
            cam.transform.rotation = Quaternion.Euler(54f, 0, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.04f, 0.08f);
            return cam;
        }

        static void SetupLight()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Light>() != null) return;
            var L = new GameObject("Sun").AddComponent<Light>();
            L.type = LightType.Directional;
            L.intensity = 1.1f;
            L.transform.rotation = Quaternion.Euler(50f, -30f, 0);
        }

        static void BuildBackdrop(Camera cam)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = "SpaceBackdrop";
            var col = quad.GetComponent<Collider>(); if (col != null) col.enabled = false;
            quad.transform.SetParent(cam.transform, false);
            quad.transform.localPosition = new Vector3(0, 0, 60f);
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(40f, 24f, 1f); // refined each frame by BackdropFit
            quad.AddComponent<BackdropFit>().cam = cam;
        }

        static void BuildSpacePad(Sim sim)
        {
            // dark glossy rectangular store platform that floats over the nebula (top at y = 0).
            // Sized from sim.FloorWidth/Depth/Center so the edit grid can match it exactly 1:1.
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "SpacePad";
            var pc = pad.GetComponent<Collider>(); if (pc != null) pc.enabled = false;
            var pm = new Material(Shader.Find("Standard"));
            pm.color = new Color(0.12f, 0.13f, 0.18f);
            pm.SetFloat("_Glossiness", 0.7f);
            pm.SetFloat("_Metallic", 0.4f);
            pad.GetComponent<Renderer>().material = pm;

            // glowing neon rim, slightly larger and lower so a border peeks out around the pad
            var rim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rim.name = "SpacePadRim";
            var rc = rim.GetComponent<Collider>(); if (rc != null) rc.enabled = false;
            var rm = new Material(Shader.Find("Standard"));
            var glow = new Color(0.0f, 0.72f, 0.92f);
            rm.color = glow;
            rm.EnableKeyword("_EMISSION");
            rm.SetColor("_EmissionColor", glow * 0.9f);
            rim.GetComponent<Renderer>().material = rm;

            sim.Pad = pad.transform;
            sim.PadRim = rim.transform;
            sim.ApplyFloorSize();
        }

        // L-shaped checkout counter = 3 cells (1x1 each).
        static Checkout BuildCheckoutL(Vector3 pos)
        {
            var root = new GameObject("Checkout");
            root.transform.position = pos;
            var checkout = root.AddComponent<Checkout>();

            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.30f, 0.50f, 0.70f);
            mat.SetFloat("_Glossiness", 0.4f);

            Vector2[] cells = { new Vector2(-0.5f, -0.5f), new Vector2(0.5f, -0.5f), new Vector2(-0.5f, 0.5f) };
            foreach (var c in cells)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "CounterCell";
                cube.transform.SetParent(root.transform, false);
                cube.transform.localScale = new Vector3(1f, 0.9f, 1f);
                cube.transform.localPosition = new Vector3(c.x, 0.45f, c.y);
                cube.GetComponent<Renderer>().material = mat;
            }
            return checkout;
        }

        // Double glass door at the front edge of the pad; customers spawn and exit here.
        static void BuildEntryDoor(Sim sim)
        {
            float frontZ = sim.FloorCenter.z - sim.FloorDepth / 2f;
            float doorZ = frontZ + 0.4f;
            const float W = 3.2f, H = 2.4f, postT = 0.18f;

            var root = new GameObject("EntryDoor");
            root.transform.position = new Vector3(sim.FloorCenter.x, 0, doorZ);

            var frame = new Material(Shader.Find("Standard"));
            frame.color = new Color(0.18f, 0.20f, 0.26f);
            frame.SetFloat("_Metallic", 0.7f);
            frame.SetFloat("_Glossiness", 0.6f);
            var glass = GlassMaterial(new Color(0.45f, 0.80f, 1f, 0.30f));

            MakeBox(root.transform, new Vector3(-W / 2f, H / 2f, 0), new Vector3(postT, H, 0.30f), frame, "Post_L");
            MakeBox(root.transform, new Vector3(W / 2f, H / 2f, 0), new Vector3(postT, H, 0.30f), frame, "Post_R");
            MakeBox(root.transform, new Vector3(0, H, 0), new Vector3(W + postT, 0.25f, 0.30f), frame, "Header");
            MakeBox(root.transform, new Vector3(0, 0.05f, 0), new Vector3(W + postT, 0.10f, 0.36f), frame, "Threshold");
            MakeBox(root.transform, new Vector3(0, H / 2f, 0), new Vector3(0.10f, H, 0.26f), frame, "Mullion");
            var glassL = MakeBox(root.transform, new Vector3(-W / 4f, H / 2f, 0), new Vector3(W / 2f - 0.18f, H - 0.25f, 0.06f), glass, "Glass_L");
            var glassR = MakeBox(root.transform, new Vector3(W / 4f, H / 2f, 0), new Vector3(W / 2f - 0.18f, H - 0.25f, 0.06f), glass, "Glass_R");

            var dc = root.AddComponent<DoorController>();
            dc.leafL = glassL.transform;
            dc.leafR = glassR.transform;
            dc.slide = W / 4f;
            sim.Door = dc;

            var ent = new GameObject("Entrance").transform;   // spawn just inside, left leaf
            ent.SetParent(root.transform);
            ent.position = new Vector3(sim.FloorCenter.x - 0.8f, 0.35f, doorZ + 1.0f);
            var ext = new GameObject("Exit").transform;        // exit through the right leaf
            ext.SetParent(root.transform);
            ext.position = new Vector3(sim.FloorCenter.x + 0.8f, 0.35f, doorZ + 1.0f);
            sim.Entrance = ent;
            sim.Exit = ext;
        }

        static GameObject MakeBox(Transform parent, Vector3 localPos, Vector3 scale, Material mat, string name)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            var col = go.GetComponent<Collider>(); if (col != null) col.enabled = false; // decorative
            go.transform.SetParent(parent, false);
            go.transform.localScale = scale;
            go.transform.localPosition = localPos;
            go.GetComponent<Renderer>().material = mat;
            return go;
        }

        static Material GlassMaterial(Color c)
        {
            var m = new Material(Shader.Find("Standard"));
            m.SetFloat("_Mode", 3f);
            m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.DisableKeyword("_ALPHATEST_ON");
            m.EnableKeyword("_ALPHABLEND_ON");
            m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            m.renderQueue = 3000;
            m.SetFloat("_Glossiness", 0.8f);
            m.color = c;
            return m;
        }

        static void BuildPlaceholders(Transform shop)
        {
            // Sphere "shoppers" so the layout reads in edit mode; auto-removed at Play.
            var parent = new GameObject("Shoppers (placeholder)");
            parent.transform.SetParent(shop, true);
            parent.AddComponent<PlaceholderDecor>();
            Vector3[] spots =
            {
                new Vector3(-3f, 0.35f, 1.5f),
                new Vector3(1.5f, 0.35f, 2.2f),
                new Vector3(4f, 0.35f, 0.5f),
                new Vector3(-1f, 0.35f, -3f),
                new Vector3(2.5f, 0.35f, -2f),
            };
            foreach (var s in spots)
            {
                var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                go.transform.SetParent(parent.transform);
                go.transform.localScale = Vector3.one * 0.7f;
                go.transform.position = s;
                go.GetComponent<Renderer>().material.color = Color.HSVToRGB(Random.value, 0.5f, 0.9f);
            }
        }
    }
}
