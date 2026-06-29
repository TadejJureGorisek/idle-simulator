using UnityEngine;

namespace IdleSim
{
    // Drives a legacy-animated NPC: plays the rigged WALK clip at a speed proportional to how fast the agent
    // actually moves (so the feet stay in sync), and cross-fades to a real IDLE clip (standing, feet together,
    // arms at side, gentle sway) when the agent stops. The idle clip is pulled from "npc_<key>_idle" (a Meshy
    // idle animation of the same rig) and merged onto this model's Animation. Lives on the model child; the
    // Customer/Employee agent calls SetMoving each frame. Added by NpcVisuals.
    public class NpcAnimator : MonoBehaviour
    {
        public float strideTune = 0.5f;   // walk-clip speed per m/s of movement (tune for foot-slide)
        public float idleFrame = 0.15f;   // fallback freeze frame if no idle clip is present

        Animation anim;
        AnimationState walk;
        string walkName;
        bool hasIdle;
        bool moving = true;
        float bobT;
        Vector3 baseLocalPos;

        public void Init(Animation a, string key)
        {
            anim = a;
            baseLocalPos = transform.localPosition;
            if (anim == null || anim.clip == null) return;
            walkName = anim.clip.name;
            walk = anim[walkName];
            if (walk != null) walk.wrapMode = WrapMode.Loop;

            // merge in the matching idle clip (same rig → same bone paths)
            var idleGo = Resources.Load<GameObject>("npc_" + key + "_idle");
            if (idleGo != null)
            {
                var ia = idleGo.GetComponent<Animation>();
                if (ia != null && ia.clip != null)
                {
                    anim.AddClip(ia.clip, "idle");
                    anim["idle"].wrapMode = WrapMode.Loop;
                    hasIdle = true;
                }
            }
            anim.Play(walkName);
        }

        public void SetMoving(bool m, float moveSpeed)
        {
            if (anim == null || walk == null) { moving = m; return; }
            if (m)
            {
                walk.speed = Mathf.Max(0.1f, moveSpeed * strideTune);
                if (!moving) anim.CrossFade(walkName, 0.2f);
            }
            else if (moving)   // just stopped
            {
                if (hasIdle) anim.CrossFade("idle", 0.25f);
                else { walk.speed = 0f; walk.normalizedTime = idleFrame; }
            }
            moving = m;
        }

        void Update()
        {
            // gentle bob only as a fallback when there's no real idle clip
            if (!moving && !hasIdle)
            {
                bobT += Time.deltaTime;
                transform.localPosition = baseLocalPos + new Vector3(0f, Mathf.Sin(bobT * 2.2f) * 0.012f, 0f);
            }
            else if (transform.localPosition != baseLocalPos)
            {
                transform.localPosition = baseLocalPos; bobT = 0f;
            }
        }
    }
}
