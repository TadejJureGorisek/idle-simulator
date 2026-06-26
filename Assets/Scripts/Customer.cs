using System.Collections.Generic;
using UnityEngine;

namespace IdleSim
{
    // Grid-routed (straight lines, 90-degree turns, avoids fixtures), wants a random 1..MaxCart
    // products, collects them from shelves, queues, pays for all of them, leaves. Overlaps other
    // customers. Ghost/escape logic keeps it from ever getting trapped by edits.
    public class Customer : MonoBehaviour
    {
        enum St { ToShelf, ToQueue, InLine, Leaving }

        St state;
        const float Speed = 3.2f;
        Shelf target;
        int wanted;
        int collected;
        Vector3 queuePos;
        Vector3 dest;
        List<Vector3> path;
        int pathIndex;
        bool ghosting;
        bool escaping;

        public bool InLine => state == St.InLine;

        public void Begin()
        {
            wanted = Random.Range(1, Sim.Instance.MaxCart + 1); // buy 1 or more products
            collected = 0;
            target = Sim.Instance.GetStockedShelf();
            if (target == null) { GoLeave(true); return; }
            state = St.ToShelf;
            SetPath(FrontOf(target.transform));
        }

        Vector3 FrontOf(Transform t)
        {
            var p = t.position; p.z -= 1.2f; p.y = transform.position.y; return p;
        }

        public void SetQueuePos(Vector3 p) { queuePos = p; }

        void SetPath(Vector3 d)
        {
            dest = d;
            var nav = Sim.Instance.Nav;
            if (nav == null)
            {
                escaping = false;
                path = new List<Vector3> { new Vector3(d.x, transform.position.y, d.z) };
                pathIndex = 0;
                return;
            }
            path = nav.FindPath(transform.position, d, transform.position.y);
            if (path == null || path.Count == 0)
            {
                // Trapped: no route to the destination -> walk straight out of the store through anything.
                escaping = true;
                state = St.Leaving;
                dest = Sim.Instance.Exit.position;
                return;
            }
            escaping = false;
            pathIndex = 0;
        }

        public void Repath()
        {
            switch (state)
            {
                case St.ToShelf: if (target != null) SetPath(FrontOf(target.transform)); else GoToCheckoutOrLeave(); break;
                case St.ToQueue: SetPath(queuePos); break;
                case St.Leaving: SetPath(Sim.Instance.Exit.position); break;
            }
        }

        void Update()
        {
            if (Sim.Instance != null && Sim.Instance.Editing) return; // frozen while editing

            bool ghost = Sim.Instance != null && Sim.Instance.IsGhost;
            if (ghosting && !ghost) { ghosting = false; if (state != St.InLine) SetPath(dest); }
            ghosting = ghost;

            switch (state)
            {
                case St.ToShelf:
                    if (Advance(ghost)) ArriveAtShelf();
                    break;

                case St.ToQueue:
                    if (Advance(ghost)) state = St.InLine;
                    break;

                case St.InLine:
                    transform.position = MoveOrtho(transform.position, queuePos, Speed * Time.deltaTime);
                    break;

                case St.Leaving:
                    if (Advance(ghost)) Destroy(gameObject);
                    break;
            }
        }

        void ArriveAtShelf()
        {
            int want = wanted - collected;
            for (int i = 0; i < want; i++)
            {
                if (target != null && target.Pick()) collected++;
                else break; // shelf ran dry
            }

            if (collected >= wanted) { GoToCheckoutOrLeave(); return; }

            // need more and this shelf is empty -> try another stocked shelf, else go pay
            var next = Sim.Instance.GetStockedShelf();
            if (next != null) { target = next; state = St.ToShelf; SetPath(FrontOf(next.transform)); }
            else GoToCheckoutOrLeave();
        }

        void GoToCheckoutOrLeave()
        {
            if (collected > 0)
            {
                Sim.Instance.Checkout.Join(this);
                state = St.ToQueue;
                SetPath(queuePos);
            }
            else GoLeave(true); // bought nothing
        }

        bool Advance(bool ghost)
        {
            if (ghost || escaping)
            {
                Vector3 t = Flat(dest);
                transform.position = Vector3.MoveTowards(transform.position, t, Speed * Time.deltaTime);
                return Vector3.Distance(transform.position, t) < 0.04f;
            }
            return FollowPath();
        }

        Vector3 Flat(Vector3 p) { p.y = transform.position.y; return p; }

        // Move toward the queue slot orthogonally: settle the row depth (z) first, then step to
        // the side (x). So when the line wraps, customers shuffle forward then side-step into the
        // new row instead of cutting diagonally across (or off) the floor.
        Vector3 MoveOrtho(Vector3 cur, Vector3 target, float maxDist)
        {
            target.y = cur.y;
            if (Mathf.Abs(target.z - cur.z) > 0.02f)
                return Vector3.MoveTowards(cur, new Vector3(cur.x, cur.y, target.z), maxDist);
            return Vector3.MoveTowards(cur, target, maxDist);
        }

        bool FollowPath()
        {
            if (path == null || pathIndex >= path.Count) return true;
            Vector3 wp = path[pathIndex];
            wp.y = transform.position.y;
            transform.position = Vector3.MoveTowards(transform.position, wp, Speed * Time.deltaTime);
            if (Vector3.Distance(transform.position, wp) < 0.04f) pathIndex++;
            return pathIndex >= path.Count;
        }

        public void Served()
        {
            Economy.Instance.Add(Sim.Instance.Profit * collected); // pay for everything in the basket
            Economy.Instance.RecordServed();
            Sim.Instance.RecordServedToday();
            GoLeave(false);
        }

        void GoLeave(bool unhappy)
        {
            if (unhappy) Economy.Instance.RecordLost();
            state = St.Leaving;
            SetPath(Sim.Instance.Exit.position);
        }
    }
}
