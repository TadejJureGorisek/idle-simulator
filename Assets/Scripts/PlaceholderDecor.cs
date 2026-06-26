using UnityEngine;

namespace IdleSim
{
    // Marks edit-time-only decoration; self-destructs at Play so it never interferes.
    public class PlaceholderDecor : MonoBehaviour
    {
        void Awake() { Destroy(gameObject); }
    }
}
