using UnityEngine;

namespace IdleSim
{
    // An employee NPC. Cashiers stand behind their counter (facing the queue); restockers / cleaners /
    // managers wander the floor with a simple walk → idle loop. Placeholder capsule for now — swapped for
    // a rigged Meshy character model later (this controller drives whatever visual is parented to it).
    public class Employee : MonoBehaviour
    {
        public enum Role { Cashier, Restocker, Cleaner, Manager }
        public Role role;
        public Transform counter;   // cashiers: the counter they man

        const float Speed = 2.0f;
        Vector3 dest; bool moving; float idleT;
        NpcAnimator npcAnim; Vector3 prevPos; bool prevInit;

        void Update()
        {
            var sim = Sim.Instance;
            if (sim == null || sim.Editing) return;   // frozen while editing

            if (role == Role.Cashier) ManCounter();
            else Wander(sim);

            // drive walk/idle from actual movement (walk speed synced; idle bob when stopped)
            Vector3 v = transform.position - prevPos; v.y = 0f;
            float spd = (prevInit && Time.deltaTime > 0f) ? v.magnitude / Time.deltaTime : 0f;
            prevPos = transform.position; prevInit = true;
            if (npcAnim == null) npcAnim = GetComponentInChildren<NpcAnimator>();
            if (npcAnim != null) npcAnim.SetMoving(spd > 0.15f, Speed);
        }

        void ManCounter()
        {
            if (counter == null) { Wander(Sim.Instance); return; }
            Vector3 fwd = counter.forward; fwd.y = 0;
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward; fwd.Normalize();
            Vector3 stand = counter.position + fwd * 0.7f; stand.y = transform.position.y;   // behind the till (opposite the queue)
            transform.position = Vector3.MoveTowards(transform.position, stand, Speed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-fwd), 8f * Time.deltaTime);   // face the customers
        }

        void Wander(Sim sim)
        {
            if (moving)
            {
                Vector3 d = dest; d.y = transform.position.y;
                Vector3 dir = d - transform.position; dir.y = 0;
                if (dir.sqrMagnitude > 0.0004f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8f * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, d, Speed * Time.deltaTime);
                if (Vector3.Distance(transform.position, d) < 0.06f) { moving = false; idleT = Random.Range(0.8f, 2.5f); }
            }
            else
            {
                idleT -= Time.deltaTime;
                if (idleT <= 0f) { dest = sim.RandomFloorPoint(); moving = true; }
            }
        }
    }
}
