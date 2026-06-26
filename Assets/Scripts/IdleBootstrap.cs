using UnityEngine;

namespace IdleSim
{
    // Runtime safety net: if you press Play on a scene with no authored layout, build the
    // greybox so the game is still playable. The authored Main.unity already has a Sim,
    // so this no-ops there.
    public static class IdleBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (UnityEngine.Object.FindFirstObjectByType<Sim>() != null) return;
            SceneBuilder.Build();
        }
    }
}
