using UnityEngine;

namespace VoidSurvivor.ProceduralArena.Encounter
{
    /// <summary>
    /// Emissive wall covering an exit door while an encounter is active.
    /// Blocks Player and Enemy (via solid collider). Projectiles pass through
    /// by being on a layer excluded from the barrier's collision matrix —
    /// for PR 2.C we rely on a full solid collider; projectile pass-through
    /// is deferred until projectile layers are introduced.
    /// </summary>
    public class SoftLockBarrier : MonoBehaviour
    {
        Collider[] colliders;
        Renderer[] renderers;
        bool isOpen;

        public bool IsOpen => isOpen;

        void Awake()
        {
            colliders = GetComponentsInChildren<Collider>(true);
            renderers = GetComponentsInChildren<Renderer>(true);
        }

        public void Close()
        {
            if (colliders == null) Awake();
            isOpen = false;
            SetActive(true);
        }

        public void Open()
        {
            if (colliders == null) Awake();
            isOpen = true;
            SetActive(false);
        }

        void SetActive(bool active)
        {
            if (colliders != null)
                for (int i = 0; i < colliders.Length; i++)
                    if (colliders[i] != null) colliders[i].enabled = active;
            if (renderers != null)
                for (int i = 0; i < renderers.Length; i++)
                    if (renderers[i] != null) renderers[i].enabled = active;
        }
    }
}
