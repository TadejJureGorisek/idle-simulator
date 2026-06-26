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
        public List<Customer> Line => line;

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
            // Use the till's own axes so the queue rotates with the shop.
            Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
            Vector3 right = transform.right; right.y = 0; right.Normalize();
            Vector3 b = transform.position - fwd * (-queueZOffset); // in front of the till
            b.y = queueY;
            const int perRow = 4;
            const float rowGap = 1.1f;
            for (int i = 0; i < line.Count; i++)
            {
                int row = i / perRow;
                int col = i % perRow;
                if ((row & 1) == 1) col = perRow - 1 - col;                 // snake: alternate row direction
                Vector3 p = b - fwd * (spacing * col) + right * (rowGap * row);
                if (Sim.Instance != null) p = Sim.Instance.ClampToFloor(p);
                line[i].SetQueuePos(p);
            }
        }
    }
}
