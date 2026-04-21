using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VoidSurvivor.ProceduralArena.Arena;
using VoidSurvivor.ProceduralArena.Build;
using VoidSurvivor.ProceduralArena.Core;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// Owns the single ArenaRoot child. Generates via SingleArenaGenerator,
    /// builds via ArenaBuilder.BuildSingle, spawns door triggers, teleports
    /// the player to the start spawn, and hides everything behind a fade
    /// canvas during transitions.
    /// </summary>
    public class ArenaFlowController : MonoBehaviour
    {
        [Header("Refs")]
        public ArenaRunConfig buildConfig;
        public Transform arenaParent; // parent GO for ArenaRoot; if null, uses self
        public Transform player;      // if null, auto-resolve via Player tag
        public string playerTag = "Player";

        [Header("Fade")]
        public Canvas fadeCanvas;
        public Image fadeImage;
        public Color fadeColor = Color.black;

        public event Action<RunGraphNode> ArenaEntered;
        public event Action<RunGraphNode> ArenaBuilt;

        ArenaRuntimeContext currentCtx;
        GameObject currentArenaRoot;

        public ArenaRuntimeContext CurrentContext => currentCtx;

        void Awake()
        {
            if (arenaParent == null) arenaParent = transform;
            if (fadeCanvas == null || fadeImage == null) BuildDefaultFadeCanvas();
            SetFade(0f);
        }

        public IEnumerator EnterArena(
            RunGraphNode node, RunController controller,
            float fadeIn, float hold, float fadeOut)
        {
            if (node == null) yield break;

            yield return FadeTo(1f, fadeIn);
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

            DestroyCurrent();
            currentCtx = SingleArenaGenerator.Generate(node.arenaSeed, node.typeProfile, buildConfig);
            currentArenaRoot = ArenaBuilder.BuildSingle(currentCtx, buildConfig, arenaParent);

            if (currentArenaRoot != null)
            {
                SpawnExitTriggers(node, controller);
                TeleportPlayerToStart();
                ArenaBuilt?.Invoke(node);
            }

            yield return FadeTo(0f, fadeOut);
            ArenaEntered?.Invoke(node);
        }

        public void DestroyCurrent()
        {
            if (currentArenaRoot != null)
            {
                if (Application.isPlaying) Destroy(currentArenaRoot);
                else DestroyImmediate(currentArenaRoot);
                currentArenaRoot = null;
            }
            else
            {
                // Belt-and-suspenders: clear any stray ArenaRoot under parent
                ArenaBuilder.Clear(arenaParent);
            }
            currentCtx = null;
        }

        void SpawnExitTriggers(RunGraphNode node, RunController controller)
        {
            if (currentCtx == null || currentCtx.layout == null) return;
            if (currentCtx.layout.rooms.Count == 0) return;
            var room = currentCtx.layout.rooms[0];
            if (room.exitDoorAnchors.Count == 0) return;

            // find the Exits GO under ArenaRoot
            Transform exitsRoot = currentArenaRoot != null ? currentArenaRoot.transform.Find("Exits") : null;
            if (exitsRoot == null) return;

            int childCount = node.children != null ? node.children.Count : 0;
            float m = buildConfig.macroCellMeters;
            float doorHeight = Mathf.Min((room.wallHeightMeters > 0 ? room.wallHeightMeters : buildConfig.wallHeightMeters) * 0.7f, 5f);

            for (int i = 0; i < room.exitDoorAnchors.Count; i++)
            {
                var anchor = room.exitDoorAnchors[i];
                // spawn trigger as a sibling of Exit_i visual
                var triggerGo = new GameObject($"ExitTrigger_{i}");
                triggerGo.transform.SetParent(exitsRoot, false);
                triggerGo.transform.position = new Vector3(anchor.worldCenter.x, doorHeight * 0.5f, anchor.worldCenter.z);
                var box = triggerGo.AddComponent<BoxCollider>();
                box.isTrigger = true;
                if (anchor.outwardDir.x != 0)
                    box.size = new Vector3(m * 0.6f, doorHeight, m * 0.9f);
                else
                    box.size = new Vector3(m * 0.9f, doorHeight, m * 0.6f);

                var trig = triggerGo.AddComponent<ExitDoorTrigger>();
                trig.controller = controller;
                trig.childIndex = i;
                trig.playerTag = playerTag;
                trig.isBossVictory = childCount == 0;

                // Solid invisible barrier just outside the opening so the player
                // cannot fall off the map during the fade transition. The trigger
                // fires first (its volume extends inward), fade starts, and if the
                // player keeps pushing forward they bump into this wall.
                var barrierGo = new GameObject($"ExitBarrier_{i}");
                barrierGo.transform.SetParent(exitsRoot, false);
                Vector3 barrierOffset = new Vector3(
                    anchor.outwardDir.x * m * 0.35f, 0f,
                    anchor.outwardDir.y * m * 0.35f);
                barrierGo.transform.position = new Vector3(
                    anchor.worldCenter.x + barrierOffset.x,
                    doorHeight * 0.5f,
                    anchor.worldCenter.z + barrierOffset.z);
                var barrier = barrierGo.AddComponent<BoxCollider>();
                barrier.isTrigger = false;
                if (anchor.outwardDir.x != 0)
                    barrier.size = new Vector3(0.2f, doorHeight, m * 1.0f);
                else
                    barrier.size = new Vector3(m * 1.0f, doorHeight, 0.2f);

                // label
                var labelHost = new GameObject($"ExitLabel_{i}");
                labelHost.transform.SetParent(exitsRoot, false);
                labelHost.transform.position = new Vector3(anchor.worldCenter.x, 0f, anchor.worldCenter.z);
                var lbl = labelHost.AddComponent<DoorChoiceLabel>();
                if (i < childCount)
                {
                    var child = node.children[i];
                    ArenaCategory cat = child.typeProfile != null ? child.typeProfile.category : ArenaCategory.Combat;
                    lbl.Setup(cat, child.arenaIndex, doorHeight);
                }
                else
                {
                    lbl.Setup(ArenaCategory.Boss, 4, doorHeight);
                }
            }
        }

        void TeleportPlayerToStart()
        {
            if (currentCtx == null || currentCtx.layout == null || currentCtx.layout.rooms.Count == 0) return;
            var room = currentCtx.layout.rooms[0];
            Vector3 target = room.startSpawnPoint + arenaParent.position + new Vector3(0f, 1.5f, 0f);
            ResolvePlayer();
            if (player == null) return;
            var cc = player.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
                player.position = target;
                cc.enabled = true;
            }
            else
            {
                player.position = target;
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            }
        }

        void ResolvePlayer()
        {
            if (player != null) return;
            var go = GameObject.FindWithTag(playerTag);
            if (go != null) player = go.transform;
        }

        // ---------------- Fade ----------------

        public IEnumerator FadeTo(float targetAlpha, float duration)
        {
            if (fadeImage == null) yield break;
            if (duration <= 0f) { SetFade(targetAlpha); yield break; }
            float start = fadeImage.color.a;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(start, targetAlpha, Mathf.Clamp01(t / duration));
                SetFade(a);
                yield return null;
            }
            SetFade(targetAlpha);
        }

        public void SetFade(float alpha)
        {
            if (fadeImage == null) return;
            var c = fadeColor; c.a = Mathf.Clamp01(alpha);
            fadeImage.color = c;
            if (fadeCanvas != null) fadeCanvas.enabled = alpha > 0.001f || fadeCanvas.enabled;
        }

        void BuildDefaultFadeCanvas()
        {
            var canvasGo = new GameObject("ArenaFadeCanvas");
            canvasGo.transform.SetParent(transform, false);
            fadeCanvas = canvasGo.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 5000;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var imgGo = new GameObject("FadeImage");
            imgGo.transform.SetParent(canvasGo.transform, false);
            fadeImage = imgGo.AddComponent<Image>();
            var rt = fadeImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            fadeImage.color = new Color(fadeColor.r, fadeColor.g, fadeColor.b, 0f);
            fadeImage.raycastTarget = false;
        }
    }
}
