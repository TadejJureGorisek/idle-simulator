using UnityEngine;

namespace IdleSim
{
    // Builds the entire grey-box prototype at runtime so you can just press Play
    // in any scene. Mirrors the code-generated-scene approach used elsewhere.
    public static class IdleBootstrap
    {
        static GameObject shelfParent;
        static bool built;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (built) return;
            if (UnityEngine.Object.FindFirstObjectByType<Sim>() != null) return; // already set up
            Build();
        }

        static void Build()
        {
            built = true;

            var go = new GameObject("IdleSim");
            var econ = go.AddComponent<Economy>();
            var sim = go.AddComponent<Sim>();
            go.AddComponent<HUD>();

            econ.Load();

            SetupCamera();
            SetupLight();
            BuildFloor();

            shelfParent = new GameObject("Shelves");

            // checkout counter
            var coGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            coGO.name = "Checkout";
            coGO.transform.localScale = new Vector3(2f, 1f, 1f);
            coGO.transform.position = new Vector3(0, 0.5f, -1.5f);
            coGO.GetComponent<Renderer>().material.color = new Color(0.30f, 0.50f, 0.70f);
            var checkout = coGO.AddComponent<Checkout>();
            checkout.basePos = new Vector3(0, 0.7f, -2.6f);

            var ent = new GameObject("Entrance").transform;
            ent.position = new Vector3(-7f, 0.7f, -5.5f);
            var ext = new GameObject("Exit").transform;
            ext.position = new Vector3(7f, 0.7f, -5.5f);

            var spGO = new GameObject("Spawner");
            var spawner = spGO.AddComponent<CustomerSpawner>();

            sim.Checkout = checkout;
            sim.Spawner = spawner;
            sim.Entrance = ent;
            sim.Exit = ext;

            // two starting shelves
            AddShelf();
            AddShelf();

            // re-apply any saved progression
            sim.LoadLevels();
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
            cam.orthographicSize = 10f;
            cam.transform.position = new Vector3(0, 17f, -12.5f);
            cam.transform.rotation = Quaternion.Euler(54f, 0, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.10f);
        }

        static void SetupLight()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Light>() != null) return;
            var lgt = new GameObject("Sun");
            var L = lgt.AddComponent<Light>();
            L.type = LightType.Directional;
            L.intensity = 1.1f;
            lgt.transform.rotation = Quaternion.Euler(50f, -30f, 0);
        }

        static void BuildFloor()
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.localScale = new Vector3(2.2f, 1f, 1.6f);
            floor.GetComponent<Renderer>().material.color = new Color(0.18f, 0.20f, 0.26f);
        }

        public static void AddShelf()
        {
            if (shelfParent == null) return;
            var sim = Sim.Instance;

            var rootGO = new GameObject("Shelf");
            rootGO.transform.SetParent(shelfParent.transform);
            var shelf = rootGO.AddComponent<Shelf>();

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(rootGO.transform, false);
            body.transform.localScale = new Vector3(2.2f, 1.2f, 0.8f);
            body.transform.localPosition = new Vector3(0, 0.6f, 0);
            body.GetComponent<Renderer>().material.color = new Color(0.45f, 0.40f, 0.35f);

            shelf.Init();
            sim.Shelves.Add(shelf);
            LayoutShelves();
        }

        static void LayoutShelves()
        {
            var list = Sim.Instance.Shelves;
            int n = list.Count;
            const float span = 12f;
            for (int i = 0; i < n; i++)
            {
                float x = (n == 1) ? 0f : Mathf.Lerp(-span / 2f, span / 2f, (float)i / (n - 1));
                list[i].transform.position = new Vector3(x, 0f, 4.5f);
            }
        }
    }
}
