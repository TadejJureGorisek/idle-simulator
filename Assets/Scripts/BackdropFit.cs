using UnityEngine;

namespace IdleSim
{
    // Full-screen space backdrop: a quad parented to the camera, textured with the nebula and
    // resized every frame to exactly fill the orthographic view at any aspect ratio.
    [ExecuteAlways]
    public class BackdropFit : MonoBehaviour
    {
        public Camera cam;

        void OnEnable() { Apply(); }

        void Apply()
        {
            var r = GetComponent<Renderer>();
            if (r == null) return;
            if (r.sharedMaterial == null || r.sharedMaterial.mainTexture == null)
            {
                var tex = Resources.Load<Texture2D>("bg_space_nebula");
                var mat = new Material(Shader.Find("Unlit/Texture"));
                if (tex != null) mat.mainTexture = tex;
                else mat.color = new Color(0.06f, 0.05f, 0.12f);
                r.sharedMaterial = mat;
            }
        }

        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || !cam.orthographic) return;
            float h = cam.orthographicSize * 2f;
            transform.localScale = new Vector3(h * cam.aspect, h, 1f);
        }
    }
}
