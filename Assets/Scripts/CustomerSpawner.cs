using UnityEngine;

namespace IdleSim
{
    // Spawns customers on the (upgradeable) spawn interval, capped to avoid runaway crowds.
    public class CustomerSpawner : MonoBehaviour
    {
        public int MaxConcurrent = 18;
        float timer;

        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= Sim.Instance.SpawnInterval)
            {
                timer = 0f;
                if (transform.childCount < MaxConcurrent) Spawn();
            }
        }

        void Spawn()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "Customer";
            go.transform.SetParent(transform);
            go.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            Vector3 p = Sim.Instance.Entrance.position;
            p.y = 0.7f;
            go.transform.position = p;

            go.GetComponent<Renderer>().material.color = Color.HSVToRGB(Random.value, 0.5f, 0.9f);

            go.AddComponent<Customer>().Begin();
        }
    }
}
