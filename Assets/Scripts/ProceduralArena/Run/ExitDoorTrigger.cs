using UnityEngine;

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

        bool armed = true;

        void Awake()
        {
            if (controller == null) controller = FindObjectOfType<RunController>();
            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;
        }

        void OnTriggerEnter(Collider other)
        {
            if (!armed) return;
            if (!other.CompareTag(playerTag)) return;
            if (controller == null) return;
            armed = false;
            if (isBossVictory) controller.NotifyExitTriggeredOnBoss();
            else controller.ChooseDoor(childIndex);
        }
    }
}
