using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Encounter
{
    /// <summary>
    /// One-shot trigger volume covering the interior of an arena.
    /// When the player first enters, the owning EncounterController
    /// receives a BeginEncounter call. "Capsule fully inside" simplifies
    /// to a plain OnTriggerEnter for PR 2.C — the volume is inset by
    /// the player radius so the player is effectively fully inside
    /// by the time it fires.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class EncounterTrigger : MonoBehaviour
    {
        public EncounterController controller;
        public string playerTag = "Player";
        bool fired;

        void Reset()
        {
            var c = GetComponent<Collider>();
            if (c != null) c.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (fired) return;
            if (!other.CompareTag(playerTag)) return;
            fired = true;
            if (controller != null) controller.BeginEncounter();
        }
    }
}
