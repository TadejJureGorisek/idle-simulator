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

            SetupCamera();
            SetupLight();
            BuildFloor();

            var shelfParent = new GameObject("Shelves").transform;

            var coGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coGO.name = "Checkout";
            coGO.transform.localScale = new Vector3(2f, 1f, 1f);
            coGO.transform.position = new Vector3(0, 0.5f, -1.5f);
            coGO.GetComponent<Renderer>().material.color = new Color(0.30f, 0.50f, 0.70f);
            var checkout = coGO.AddComponent<Checkout>();

            var ent = new GameObject("Entrance").transform;
            ent.position = new Vector3(-7f, 0.35f, -5.5f);
            var ext = new GameObject("Exit").transform;
            ext.position = new Vector3(7f, 0.35f, -5.5f);

            var spawner = new GameObject("Spawner").AddComponent<CustomerSpawner>();

            sim.ShelfParent = shelfParent;
            sim.Checkout = checkout;
            sim.Spawner = spawner;
            sim.Entrance = ent;
            sim.Exit = ext;

            sim.AddShelf(new Vector3(-4f, 0, 4.5f));
            sim.AddShelf(new Vector3(4f, 0, 4.5f));

            BuildPlaceholders();
            return root;
        }

        static void SetupCamera()
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
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.10f);
        }

        static void SetupLight()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Light>() != null) return;
            var L = new GameObject("Sun").AddComponent<Light>();
            L.type = LightType.Directional;
            L.intensity = 1.1f;
            L.transform.rotation = Quaternion.Euler(50f, -30f, 0);
        }

        static void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2.2f, 1f, 1.6f);
            floor.GetComponent<Renderer>().material.color = new Color(0.18f, 0.20f, 0.26f);
        }

        static void BuildPlaceholders()
        {
            // Sphere "shoppers" so the layout reads in edit mode; auto-removed at Play.
            var parent = new GameObject("Shoppers (placeholder)");
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
