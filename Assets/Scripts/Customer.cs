using UnityEngine;

namespace IdleSim
{
    // Walks in, grabs an item from a stocked shelf, queues at the till, gets rung up, leaves.
    public class Customer : MonoBehaviour
    {
        enum St { ToShelf, ToQueue, InLine, Leaving }

        St state;
        const float Speed = 3.2f;
        Shelf target;
        Vector3 dest;
        Vector3 queuePos;

        public bool InLine => state == St.InLine;

        public void Begin()
        {
            target = Sim.Instance.GetStockedShelf();
            if (target == null) { GoLeave(true); return; }
            state = St.ToShelf;
            dest = FrontOf(target.transform);
        }

        Vector3 FrontOf(Transform t)
        {
            var p = t.position;
            p.z -= 1.2f;
            p.y = transform.position.y;
            return p;
        }

        public void SetQueuePos(Vector3 p) { queuePos = p; }

        void Update()
        {
            switch (state)
            {
                case St.ToShelf:
                    if (MoveTo(dest))
                    {
                        if (target != null && target.Pick())
                        {
                            Sim.Instance.Checkout.Join(this);
                            state = St.ToQueue;
                        }
                        else GoLeave(true); // shelf emptied before we got there
                    }
                    break;

                case St.ToQueue:
                    if (MoveTo(queuePos)) state = St.InLine;
                    break;

                case St.InLine:
                    MoveTo(queuePos); // shuffle forward as the line advances
                    break;

                case St.Leaving:
                    if (MoveTo(dest)) Destroy(gameObject);
                    break;
            }
        }

        public void Served()
        {
            Economy.Instance.Add(Sim.Instance.Profit);
            Economy.Instance.RecordServed();
            GoLeave(false);
        }

        void GoLeave(bool unhappy)
        {
            if (unhappy) Economy.Instance.RecordLost();
            state = St.Leaving;
            dest = Sim.Instance.Exit.position;
            dest.y = transform.position.y;
        }

        bool MoveTo(Vector3 p)
        {
            p.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, p, Speed * Time.deltaTime);
            return Vector3.Distance(transform.position, p) < 0.05f;
        }
    }
}
