using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // Holds the queue and rings up the first customer who has arrived at the till. The queue
    // is computed from the checkout's own transform, so moving the checkout moves the queue.
    public class Checkout : MonoBehaviour
    {
        readonly List<Customer> line = new List<Customer>();
        public float queueY = 0.35f;
        public float queueZOffset = -1.1f;
        public float spacing = 1.1f;

        public int LineCount => line.Count;

        Vector3 BasePos => new Vector3(transform.position.x, queueY, transform.position.z + queueZOffset);

        public void Join(Customer c)
        {
            line.Add(c);
            UpdatePositions();
        }

        public void ServeFront()
        {
            int idx = line.FindIndex(c => c != null && c.InLine);
            if (idx < 0) return;
            var c = line[idx];
            line.RemoveAt(idx);
            c.Served();
            UpdatePositions();
        }

        public void RefreshQueue() => UpdatePositions();

        void UpdatePositions()
        {
            line.RemoveAll(c => c == null);
            var b = BasePos;
            for (int i = 0; i < line.Count; i++)
            {
                Vector3 p = b;
                p.z -= spacing * i;
                line[i].SetQueuePos(p);
            }
        }
    }
}
