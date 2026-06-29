using UnityEngine;

namespace IdleSim
{
    // Marks an extra checkout counter (one spawned per cashier) so the StoreEditor can pick + drag it,
    // and so its moved position can be saved/restored. The original till uses the Checkout component.
    public class CounterTag : MonoBehaviour { }
}
