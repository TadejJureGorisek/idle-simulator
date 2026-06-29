using UnityEngine;

namespace IdleSim
{
    // Attaches a Meshy character model (imported FBX in Resources, e.g. "npc_customer" / "npc_employee")
    // to an agent root, textured, scaled to a target height with feet on the floor. Returns null if the
    // model isn't present (callers fall back to a primitive). Static mesh for now — a rigged/animated
    // version drops in later (same call site).
    public static class NpcVisuals
    {
        public static GameObject Attach(Transform root, string key, float height) => Attach(root, key, height, Color.white);

        public static GameObject Attach(Transform root, string key, float height, Color tint)
        {
            var prefab = Resources.Load<GameObject>("npc_" + key);
            if (prefab == null) return null;

            var inst = Object.Instantiate(prefab);
            inst.name = "Model";
            var t = inst.transform;
            t.SetParent(root, false);
            t.localPosition = Vector3.zero; t.localRotation = Quaternion.identity; t.localScale = Vector3.one;

            foreach (var c in inst.GetComponentsInChildren<Collider>()) c.enabled = false;   // don't block anything

            var tex = Resources.Load<Texture2D>("npc_" + key + "_textures/base_color");
            var mat = new Material(Shader.Find("Standard")); mat.SetFloat("_Glossiness", 0.22f); mat.SetFloat("_Metallic", 0f);
            mat.color = tint;
            if (tex != null) mat.mainTexture = tex;
            foreach (var r in inst.GetComponentsInChildren<Renderer>()) r.sharedMaterial = mat;

            // scale to the target world height (root scale is 1), then drop so feet sit on the floor
            Bounds b = WorldBounds(inst);
            if (b.size.y > 0.001f) t.localScale = Vector3.one * (height / b.size.y);
            b = WorldBounds(inst);
            t.position += new Vector3(0f, -b.min.y, 0f);

            // drive the rigged walk/idle via NpcAnimator (speed synced to movement; idle bob when stopped)
            var anim = inst.GetComponentInChildren<Animation>();
            if (anim != null) inst.AddComponent<NpcAnimator>().Init(anim, key);
            return inst;
        }

        static Bounds WorldBounds(GameObject g)
        {
            var rs = g.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return new Bounds(g.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
            return b;
        }
    }
}
