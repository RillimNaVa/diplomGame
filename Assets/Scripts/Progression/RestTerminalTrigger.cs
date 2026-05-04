using UnityEngine;

// Phase 4 / PR 4.PG — trigger volume on the generated Rest platform.
// Opens the Rest UI when the player steps onto the platform.
public sealed class RestTerminalTrigger : MonoBehaviour
{
    public string playerTag = "Player";

    void OnTriggerEnter(Collider other)
    {
        if (other == null) return;
        if (!string.IsNullOrEmpty(playerTag) && !other.CompareTag(playerTag)) return;
        RestRoomController.Instance?.OpenPreparedRest();
    }
}
