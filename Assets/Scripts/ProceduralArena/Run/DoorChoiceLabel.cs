using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// World-space placeholder label that shows the next arena's category
    /// above an exit door. Phase 5 will replace this with a proper UI panel.
    /// Implementation: a flat Quad carrying a Text via OnGUI-style TextMesh
    /// would require UGUI; instead we generate a 3D TextMesh as a child.
    /// </summary>
    public class DoorChoiceLabel : MonoBehaviour
    {
        public ArenaCategory category;
        public int arenaIndex;

        public void Setup(ArenaCategory cat, int index, float doorHeightMeters)
        {
            category = cat;
            arenaIndex = index;
            RefreshVisual(doorHeightMeters);
        }

        void RefreshVisual(float doorHeight)
        {
            // Destroy previous label child if any
            for (int i = transform.childCount - 1; i >= 0; i--)
                DestroyImmediate(transform.GetChild(i).gameObject);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, doorHeight + 1.2f, 0f);

            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = $"{category}\n[{arenaIndex + 1}/5]";
            tm.fontSize = 48;
            tm.characterSize = 0.08f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.white;

            var mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            var billboard = labelGo.AddComponent<Billboard>();
            billboard.enabled = true;
        }
    }

    public class Billboard : MonoBehaviour
    {
        Camera cam;
        void LateUpdate()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null) return;
            // TextMesh reads correctly when the camera looks at its +Z face,
            // i.e. the label's forward axis must point AT the camera.
            var dir = cam.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
