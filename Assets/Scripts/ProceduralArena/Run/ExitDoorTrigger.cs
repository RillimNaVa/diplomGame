using UnityEngine;
using VoidSurvivor.ProceduralArena.Encounter;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// Placed on an exit door GameObject inside a built arena. When the
    /// player enters its trigger collider, notifies RunController to pick
    /// the corresponding door-choice child. Auto-resolves the controller
    /// via FindObjectOfType at Awake if not wired.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class ExitDoorTrigger : MonoBehaviour
    {
        public int childIndex;
        public RunController controller;
        public string playerTag = "Player";
        public bool isBossVictory;
        [Tooltip("If assigned, the trigger only fires when this barrier is open (encounter cleared).")]
        public SoftLockBarrier gatingBarrier;

        bool armed = true;

        void Awake()
        {
            if (controller == null) controller = FindFirstObjectByType<RunController>();
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!armed) return;
            if (!other.CompareTag(playerTag)) return;
            if (controller == null) return;
            // Gate: if a SoftLockBarrier is assigned and still closed, refuse to fire
            // so the player cannot skip the encounter. The trigger stays armed —
            // once the barrier opens, the next OnTriggerEnter will pass.
            if (gatingBarrier != null && !gatingBarrier.IsOpen) return;
            armed = false;
            if (isBossVictory) controller.NotifyExitTriggeredOnBoss();
            else controller.ChooseDoor(childIndex);
        }
    }
}
