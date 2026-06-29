using UnityEngine;

namespace IdleSim
{
    // Spawns customers on the (upgradeable) spawn interval, capped to avoid runaway crowds.
    public class CustomerSpawner : MonoBehaviour
    {
        public int MaxConcurrent = 5;
        // all the Meshy-5 models are customers; the Meshy-6 "employee" model is reserved for staff
        static readonly string[] CustomerKeys = { "customer", "customer2", "customer3", "customer4", "employee2", "employee3" };
        float timer;

        void Update()
        {
            if (Sim.Instance == null || Sim.Instance.Editing) return; // paused while editing
            if (!Sim.Instance.IsOpen) return;                          // closed: no new customers
            // Only admit a customer if there's spare stock for it — every customer already shopping claims
            // at least one item, so don't send a new one who'd find empty shelves and just leave.
            if (Sim.Instance.TotalStock <= transform.childCount) return;
            timer += Time.deltaTime;
            if (timer >= Sim.Instance.SpawnInterval)
            {
                timer = 0f;
                if (transform.childCount < MaxConcurrent) Spawn();
            }
        }

        void Spawn()
        {
            var root = new GameObject("Customer");
            root.transform.SetParent(transform);
            Vector3 p = Sim.Instance.Entrance.position; p.y = 0.35f;
            root.transform.position = p;

            var cc = root.AddComponent<CapsuleCollider>();   // for the click-to-serve raycast
            cc.radius = 0.3f; cc.height = 1.6f; cc.center = new Vector3(0, 0.45f, 0);

            // wear a random Meshy customer model if present, else a colored sphere placeholder
            string key = CustomerKeys[Random.Range(0, CustomerKeys.Length)];
            if (NpcVisuals.Attach(root.transform, key, 1.55f) == null)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                s.name = "Vis"; var col = s.GetComponent<Collider>(); if (col != null) col.enabled = false;
                s.transform.SetParent(root.transform, false);
                s.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                s.GetComponent<Renderer>().material.color = Color.HSVToRGB(Random.value, 0.5f, 0.9f);
            }

            root.AddComponent<Customer>().Begin();
        }
    }
}
