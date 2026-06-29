#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace IdleSim.EditorTools
{
    // Keep the UI skin tiles small + crisp so the 9-slice corner stays a sensible pixel size.
    public class UITextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            bool ui = assetPath.EndsWith("/ui_panel.png") || assetPath.EndsWith("/ui_button.png");
            bool icon = assetPath.EndsWith("/icon_coin.png") || assetPath.EndsWith("/icon_star.png");
            if (!ui && !icon) return;
            var ti = (TextureImporter)assetImporter;
            ti.maxTextureSize = 256;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.alphaIsTransparency = true;     // clean translucent edges (no dark halo)
            ti.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
#endif
