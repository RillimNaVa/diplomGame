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
            tm.text = BuildLabelText(category, arenaIndex);
            tm.fontSize = 48;
            tm.characterSize = 0.08f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = ColorForCategory(category);

            var mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            var billboard = labelGo.AddComponent<Billboard>();
            billboard.enabled = true;
        }

        // Phase 4 / PR 4.PD — door preview text (TZ §6.3). Two lines:
        // category headline + reward / purpose hint.
        static string BuildLabelText(ArenaCategory cat, int index)
        {
            string title = cat.ToString().ToUpperInvariant();
            string hint;
            switch (cat)
            {
                case ArenaCategory.Combat: hint = "Reward: Upgrade Card"; break;
                case ArenaCategory.Elite:  hint = "Reward: Rare+ Chance"; break;
                case ArenaCategory.Shop:   hint = "Spend Kill Points";    break;
                case ArenaCategory.Rest:   hint = "Recover / Prepare";    break;
                case ArenaCategory.Boss:   hint = "Final Encounter";      break;
                default:                   hint = $"Stage {index + 1}";   break;
            }
            return $"{title}\n{hint}";
        }

        static Color ColorForCategory(ArenaCategory cat)
        {
            switch (cat)
            {
                case ArenaCategory.Combat: return new Color32(0xE8, 0xF4, 0xFF, 0xFF);
                case ArenaCategory.Elite:  return new Color32(0xFF, 0x95, 0x40, 0xFF);
                case ArenaCategory.Shop:   return new Color32(0x7F, 0xFF, 0xCB, 0xFF);
                case ArenaCategory.Rest:   return new Color32(0x9A, 0xC8, 0xFF, 0xFF);
                case ArenaCategory.Boss:   return new Color32(0xFF, 0x3A, 0x4C, 0xFF);
                default:                   return Color.white;
            }
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
