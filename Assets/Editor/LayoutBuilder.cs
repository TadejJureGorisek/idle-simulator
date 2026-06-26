using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IdleSim
{
    // Authors the default greybox layout into a fresh scene and saves it to
    // Assets/Scenes/Main.unity. Run from the menu, then press Play.
    public static class LayoutBuilder
    {
        [MenuItem("Idle Simulator/Build Default Layout (Greybox)")]
        public static void BuildLayout()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            SceneBuilder.Build();
            System.IO.Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            Debug.Log("Idle Simulator: default layout built and saved to Assets/Scenes/Main.unity. Press Play.");
        }
    }
}
