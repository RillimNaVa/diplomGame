using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoidSurvivor.ProceduralArena.Arena;
using VoidSurvivor.ProceduralArena.Build;
using VoidSurvivor.ProceduralArena.Core;
using VoidSurvivor.ProceduralArena.Encounter;
using VoidSurvivor.ProceduralArena.Navigation;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// Owns the single ArenaRoot child. Generates via SingleArenaGenerator,
    /// builds via ArenaBuilder.BuildSingle, bakes NavMesh async, spawns door
    /// triggers + soft-lock barriers + EncounterController, teleports the
    /// player to the start spawn, and hides everything behind a fade canvas
    /// during transitions.
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

        [Header("Encounter (PR 2.C)")]
        [Tooltip("Skip NavMesh bake, encounter controller, and barriers — keeps the PR 2.B walk-through flow for debugging.")]
        public bool skipEncounterSystems = false;

        public event Action<RunGraphNode> ArenaEntered;
        public event Action<RunGraphNode> ArenaBuilt;

        ArenaRuntimeContext currentCtx;
        GameObject currentArenaRoot;
        EncounterController currentEncounter;
        ArenaBuildMaterials buildMats;
        ArenaPostProcessingController postFx;
        ReflectionProbe currentReflectionProbe;
        bool defaultFogEnabled;
        Color defaultFogColor;
        float defaultFogDensity;
        Color defaultAmbientSkyColor;
        Color defaultAmbientEquatorColor;
        Color defaultAmbientGroundColor;

        public ArenaRuntimeContext CurrentContext => currentCtx;
        public EncounterController CurrentEncounter => currentEncounter;

        void Awake()
        {
            if (arenaParent == null) arenaParent = transform;
            if (fadeCanvas == null || fadeImage == null) BuildDefaultFadeCanvas();
            SetFade(0f);
            CacheAtmosphereDefaults();
            postFx = GetComponent<ArenaPostProcessingController>();
            if (postFx == null) postFx = gameObject.AddComponent<ArenaPostProcessingController>();
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
            var biome = node.typeProfile != null ? node.typeProfile.biome : null;
            buildMats = ArenaBuildMaterials.CreateDefaults(biome);
            currentArenaRoot = ArenaBuilder.BuildSingle(currentCtx, buildConfig, arenaParent, buildMats);
            ApplyBiomeAtmosphere(biome);
            if (postFx != null) postFx.ApplyBiome(biome);

            if (currentArenaRoot != null)
            {
                SpawnExitTriggers(node, controller, out var barriers);
                SpawnReflectionProbe();
                SpawnAmbientDust(biome);

                if (!skipEncounterSystems)
                {
                    yield return ArenaNavMeshController.BakeAsync(currentArenaRoot);
                    SetupEncounter(node, barriers, controller);
                }

                TeleportPlayerToStart();
                ArenaBuilt?.Invoke(node);
            }

            yield return FadeTo(0f, fadeOut);

            // Begin encounter after fadeOut so enemies don't spawn under a black screen.
            if (currentEncounter != null) currentEncounter.BeginEncounter();

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
                ArenaBuilder.Clear(arenaParent);
            }
            currentCtx = null;
            currentEncounter = null;
            currentReflectionProbe = null;
        }

        void OnDisable()
        {
            RestoreAtmosphereDefaults();
            if (postFx != null) postFx.ClearBiome();
        }

        void SpawnExitTriggers(RunGraphNode node, RunController controller, out List<SoftLockBarrier> barriers)
        {
            barriers = new List<SoftLockBarrier>();
            if (currentCtx == null || currentCtx.layout == null) return;
            if (currentCtx.layout.rooms.Count == 0) return;
            var room = currentCtx.layout.rooms[0];
            if (room.exitDoorAnchors.Count == 0) return;

            Transform exitsRoot = currentArenaRoot != null ? currentArenaRoot.transform.Find("Exits") : null;
            if (exitsRoot == null) return;

            int childCount = node.children != null ? node.children.Count : 0;
            float m = buildConfig.macroCellMeters;
            float doorHeight = Mathf.Min((room.wallHeightMeters > 0 ? room.wallHeightMeters : buildConfig.wallHeightMeters) * 0.7f, 5f);

            for (int i = 0; i < room.exitDoorAnchors.Count; i++)
            {
                var anchor = room.exitDoorAnchors[i];

                // Trigger volume (player walks through on exit).
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
                // gatingBarrier is wired below after SoftLockBarrier is created.

                // Fall-prevention wall just outside the opening (invisible, thin).
                var fallGo = new GameObject($"ExitFallBarrier_{i}");
                fallGo.transform.SetParent(exitsRoot, false);
                Vector3 fallOffset = new Vector3(
                    anchor.outwardDir.x * m * 0.35f, 0f,
                    anchor.outwardDir.y * m * 0.35f);
                fallGo.transform.position = new Vector3(
                    anchor.worldCenter.x + fallOffset.x,
                    doorHeight * 0.5f,
                    anchor.worldCenter.z + fallOffset.z);
                var fallCol = fallGo.AddComponent<BoxCollider>();
                fallCol.isTrigger = false;
                if (anchor.outwardDir.x != 0)
                    fallCol.size = new Vector3(0.2f, doorHeight, m * 1.0f);
                else
                    fallCol.size = new Vector3(m * 1.0f, doorHeight, 0.2f);

                // Soft-lock barrier — visible emissive box covering the door opening,
                // toggled by EncounterController.
                if (!skipEncounterSystems)
                {
                    var barrierGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    barrierGo.name = $"SoftLockBarrier_{i}";
                    barrierGo.transform.SetParent(exitsRoot, false);
                    barrierGo.transform.position = new Vector3(anchor.worldCenter.x, doorHeight * 0.5f, anchor.worldCenter.z);
                    Vector3 barrierSize;
                    if (anchor.outwardDir.x != 0)
                        barrierSize = new Vector3(0.15f, doorHeight, m * 0.9f);
                    else
                        barrierSize = new Vector3(m * 0.9f, doorHeight, 0.15f);
                    barrierGo.transform.localScale = barrierSize;
                    var rend = barrierGo.GetComponent<MeshRenderer>();
                    if (rend != null && buildMats != null && buildMats.barrier != null)
                        rend.sharedMaterial = buildMats.barrier;
                    var slb = barrierGo.AddComponent<SoftLockBarrier>();
                    barriers.Add(slb);
                    trig.gatingBarrier = slb;
                }

                // Door-choice label.
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

        void SetupEncounter(RunGraphNode node, List<SoftLockBarrier> barriers, RunController controller)
        {
            if (currentArenaRoot == null || currentCtx == null || currentCtx.layout == null) return;
            if (currentCtx.layout.rooms.Count == 0) return;
            var room = currentCtx.layout.rooms[0];
            var profile = node.typeProfile;
            if (profile == null) return;

            room.arenaIndex = node.arenaIndex;
            room.scaledEnemyCount = ResolveScaledEnemyCount(profile.enemySpawnCount, node.arenaIndex, controller);
            room.enemyHealthMultiplier = ResolveScaledHealthMultiplier(node.arenaIndex, controller);

            var enc = currentArenaRoot.AddComponent<EncounterController>();
            enc.clearCondition = profile.clearCondition;
            enc.enemyCount = room.scaledEnemyCount;
            enc.enemyHealthMultiplier = room.enemyHealthMultiplier;
            // PR 3.D — wire arenaIndex + spawnProfile so EncounterController can
            // run the composer. EncounterController is the single owner of
            // arenaIndex per ENEMY_AI_TZ §7.4 Revision.
            enc.arenaIndex = node.arenaIndex;
            enc.spawnProfile = profile.spawnProfile;
            enc.barriers.AddRange(barriers);

            // Convert combatSpawnPoints into real Transforms parented under ArenaRoot.
            var spawnRoot = new GameObject("CombatSpawns");
            spawnRoot.transform.SetParent(currentArenaRoot.transform, false);
            Vector3 offset = arenaParent != null ? arenaParent.position : Vector3.zero;
            for (int i = 0; i < room.combatSpawnPoints.Count; i++)
            {
                var sp = new GameObject($"Spawn_{i}");
                sp.transform.SetParent(spawnRoot.transform, false);
                sp.transform.position = room.combatSpawnPoints[i] + offset + new Vector3(0f, 0.5f, 0f);
                enc.spawnPoints.Add(sp.transform);
            }

            currentEncounter = enc;
        }

        int ResolveScaledEnemyCount(int baseCount, int arenaIndex, RunController controller)
        {
            float perArena = controller != null && controller.Config != null ? controller.Config.enemyCountScalePerArena : 0f;
            float scaled = baseCount * (1f + Mathf.Max(0f, perArena) * Mathf.Max(0, arenaIndex));
            return Mathf.Max(0, Mathf.RoundToInt(scaled));
        }

        float ResolveScaledHealthMultiplier(int arenaIndex, RunController controller)
        {
            float perArena = controller != null && controller.Config != null ? controller.Config.enemyHealthScalePerArena : 0f;
            return 1f + Mathf.Max(0f, perArena) * Mathf.Max(0, arenaIndex);
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

        void CacheAtmosphereDefaults()
        {
            defaultFogEnabled = RenderSettings.fog;
            defaultFogColor = RenderSettings.fogColor;
            defaultFogDensity = RenderSettings.fogDensity;
            defaultAmbientSkyColor = RenderSettings.ambientSkyColor;
            defaultAmbientEquatorColor = RenderSettings.ambientEquatorColor;
            defaultAmbientGroundColor = RenderSettings.ambientGroundColor;
        }

        void ApplyBiomeAtmosphere(BiomeDefinition biome)
        {
            if (biome == null)
            {
                RestoreAtmosphereDefaults();
                return;
            }

            // PR 2.F: fogStrength is the lerp t, not a clamped constant. At 0
            // biome fog is fully absent, at 1 biome fog fully replaces defaults.
            float fogT = Mathf.Clamp01(biome.fogStrength);
            RenderSettings.fog = fogT > 0.001f;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Color.Lerp(defaultFogColor, biome.fogColor, fogT);
            RenderSettings.fogDensity = Mathf.Lerp(0.006f, 0.042f, fogT);

            // PR 2.F: ambient is still nudged (so dark biomes stay dark even
            // without post-fx), but the heavy colored tint now rides through
            // ColorAdjustments.colorFilter in ArenaPostProcessingController.
            RenderSettings.ambientSkyColor = Color.Lerp(defaultAmbientSkyColor, biome.ambientTint, 0.35f);
            RenderSettings.ambientEquatorColor = Color.Lerp(defaultAmbientEquatorColor, biome.ambientTint, 0.25f);
            RenderSettings.ambientGroundColor = Color.Lerp(defaultAmbientGroundColor, biome.ambientTint * 0.6f, 0.3f);
        }

        void SpawnAmbientDust(BiomeDefinition biome)
        {
            if (currentArenaRoot == null || currentCtx == null || currentCtx.layout == null) return;
            if (currentCtx.layout.rooms.Count == 0) return;
            var room = currentCtx.layout.rooms[0];
            var bounds = room.boundsCells;
            float m = buildConfig.macroCellMeters;
            float wh = room.wallHeightMeters > 0f ? room.wallHeightMeters : buildConfig.wallHeightMeters;

            Vector3 center = new Vector3(
                (bounds.xMin + bounds.width * 0.5f) * m,
                wh * 0.5f,
                (bounds.yMin + bounds.height * 0.5f) * m);
            if (arenaParent != null) center += arenaParent.position;

            // Slightly inset so motes don't spawn inside walls or under the floor.
            Vector3 size = new Vector3(
                Mathf.Max(1f, bounds.width * m - m * 0.5f),
                Mathf.Max(1f, wh - 0.4f),
                Mathf.Max(1f, bounds.height * m - m * 0.5f));

            AmbientDustMotes.Spawn(currentArenaRoot.transform, center, size, biome);
        }

        void SpawnReflectionProbe()
        {
            if (currentArenaRoot == null || currentCtx == null || currentCtx.layout == null) return;
            if (currentCtx.layout.rooms.Count == 0) return;
            var room = currentCtx.layout.rooms[0];
            var bounds = room.boundsCells;
            float m = buildConfig.macroCellMeters;
            float wh = room.wallHeightMeters > 0f ? room.wallHeightMeters : buildConfig.wallHeightMeters;

            Vector3 center = new Vector3(
                (bounds.xMin + bounds.width * 0.5f) * m,
                wh * 0.5f,
                (bounds.yMin + bounds.height * 0.5f) * m);
            if (arenaParent != null) center += arenaParent.position;

            var probeGo = new GameObject("ArenaReflectionProbe");
            probeGo.transform.SetParent(currentArenaRoot.transform, false);
            probeGo.transform.position = center;

            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode = UnityEngine.Rendering.ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution = 128;
            probe.importance = 100;
            probe.intensity = 1f;
            probe.boxProjection = true;
            probe.size = new Vector3(bounds.width * m, wh * 2f, bounds.height * m);
            probe.center = Vector3.zero;
            probe.clearFlags = UnityEngine.Rendering.ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor = Color.black;
            probe.nearClipPlane = 0.3f;
            probe.farClipPlane = Mathf.Max(bounds.width, bounds.height) * m * 1.5f;

            // One-shot bake on spawn, then stay static until next arena.
            probe.RenderProbe();
            currentReflectionProbe = probe;
        }

        void RestoreAtmosphereDefaults()
        {
            RenderSettings.fog = defaultFogEnabled;
            RenderSettings.fogColor = defaultFogColor;
            RenderSettings.fogDensity = defaultFogDensity;
            RenderSettings.ambientSkyColor = defaultAmbientSkyColor;
            RenderSettings.ambientEquatorColor = defaultAmbientEquatorColor;
            RenderSettings.ambientGroundColor = defaultAmbientGroundColor;
        }
    }
}
