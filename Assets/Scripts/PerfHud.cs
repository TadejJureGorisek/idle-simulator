using UnityEngine;

namespace IdleSim
{
    // Tiny on-screen perf overlay: FPS + total scene vertices/triangles/meshes (refreshed twice a second).
    // Helps decide whether the NPC/prop meshes need optimizing. Toggle with F3.
    public class PerfHud : MonoBehaviour
    {
        float fps; int frames; float acc;
        long verts, tris; int meshes; float recT;
        bool show = true;
        GUIStyle style;

        void Update()
        {
            frames++; acc += Time.unscaledDeltaTime;
            if (acc >= 0.25f) { fps = frames / acc; frames = 0; acc = 0f; }
            recT -= Time.unscaledDeltaTime;
            if (recT <= 0f) { recT = 0.5f; Recount(); }
            if (Input.GetKeyDown(KeyCode.F3)) show = !show;
        }

        void Recount()
        {
            long v = 0, t = 0; int m = 0;
            var rs = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            foreach (var r in rs)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                Mesh mesh = null;
                var smr = r as SkinnedMeshRenderer;
                if (smr != null) mesh = smr.sharedMesh;
                else { var mf = r.GetComponent<MeshFilter>(); if (mf != null) mesh = mf.sharedMesh; }
                if (mesh == null) continue;
                v += mesh.vertexCount;
                for (int s = 0; s < mesh.subMeshCount; s++) t += (long)(mesh.GetIndexCount(s) / 3);
                m++;
            }
            verts = v; tris = t; meshes = m;
        }

        void OnGUI()
        {
            if (!show) return;
            if (style == null) style = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
            var box = new Rect(Screen.width - 372, 2, 366, 22);
            var pc = GUI.color; GUI.color = new Color(0, 0, 0, 0.5f); GUI.DrawTexture(box, Texture2D.whiteTexture); GUI.color = pc;
            style.normal.textColor = fps < 30 ? new Color(1f, 0.5f, 0.4f) : fps < 55 ? new Color(1f, 0.85f, 0.3f) : new Color(0.5f, 1f, 0.6f);
            GUI.Label(new Rect(box.x, box.y, box.width - 8, box.height),
                string.Format("FPS {0:0}    verts {1:0.0}k    tris {2:0.0}k    meshes {3}    [F3]", fps, verts / 1000f, tris / 1000f, meshes), style);
        }
    }
}
