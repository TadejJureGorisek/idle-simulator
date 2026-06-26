using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // Holds the queue and rings up the first customer who has actually arrived at the till.
    public class Checkout : MonoBehaviour
    {
        readonly List<Customer> line = new List<Customer>();
        public Vector3 basePos;
        public float spacing = 1.1f;

        public int LineCount => line.Count;

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

        void UpdatePositions()
        {
            line.RemoveAll(c => c == null);
            for (int i = 0; i < line.Count; i++)
            {
                Vector3 p = basePos;
                p.z -= spacing * i;
                line[i].SetQueuePos(p);
            }
        }
    }
}
