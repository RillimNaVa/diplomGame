using UnityEngine;

// Phase 4 / PR 4.PF follow-up — trigger volume placed on the generated Shop
// platform. The Shop UI opens only when the player enters this trigger.
public sealed class ShopTerminalTrigger : MonoBehaviour
{
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
        ShopController.Instance?.OpenPreparedShop();
    }
}
