using UnityEditor;

namespace IdleSim
{
    // Imports the Meshy NPC character FBX (Resources/npc_*) as LEGACY animation so the walk clip can be
    // played at runtime with a plain Animation component (no AnimatorController asset needed). Materials
    // are applied at runtime by NpcVisuals, so skip importing the FBX's own (avoids pink-material spam).
    public class NpcModelImporter : AssetPostprocessor
    {
        void OnPreprocessModel()
        {
            var p = assetPath.Replace("\\", "/");
            if (!p.Contains("/Resources/npc_")) return;
            var mi = assetImporter as ModelImporter;
            if (mi == null) return;
            mi.animationType = ModelImporterAnimationType.Legacy;
            mi.importAnimation = true;
            mi.materialImportMode = ModelImporterMaterialImportMode.None;
        }
    }
}
