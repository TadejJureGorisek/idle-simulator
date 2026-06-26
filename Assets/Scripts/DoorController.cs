using UnityEngine;

namespace IdleSim
{
    // Slides the two glass leaves apart when the shop is open, together when closed.
    public class DoorController : MonoBehaviour
    {
        public Transform leafL, leafR;
        public float slide = 0.8f;
        public float speed = 2.5f;

        Vector3 closedL, closedR;
        bool cached;
        bool open = true;
        float t = 1f; // 0 = closed, 1 = open (start open)

        void Awake() { Cache(); }

        void Cache()
        {
            if (cached) return;
            if (leafL != null) closedL = leafL.localPosition;
            if (leafR != null) closedR = leafR.localPosition;
            cached = true;
        }

        public void SetOpen(bool o) { Cache(); open = o; }

        void Update()
        {
            t = Mathf.MoveTowards(t, open ? 1f : 0f, speed * Time.deltaTime);
            if (leafL != null) leafL.localPosition = closedL + Vector3.left * (slide * t);
            if (leafR != null) leafR.localPosition = closedR + Vector3.right * (slide * t);
        }
    }
}
