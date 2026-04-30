using UnityEngine;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// PR 5.C — runtime-built ParticleSystem of slow-moving dust motes drifting
    /// through the arena. Spawned by ArenaFlowController right after each
    /// arena builds. Uses biome.ambientTint so motes blend with the room's
    /// color cast.
    ///
    /// World-space simulation, gravity 0, low emission rate. Particles spawn
    /// across a Box volume that covers the arena bounds.
    /// </summary>
    public class AmbientDustMotes : MonoBehaviour
    {
        public static GameObject Spawn(Transform parent, Vector3 worldCenter, Vector3 worldSize, BiomeDefinition biome)
        {
            var go = new GameObject("AmbientDustMotes");
            go.transform.SetParent(parent, true);
            go.transform.position = worldCenter;
            var motes = go.AddComponent<AmbientDustMotes>();
            motes.Configure(worldSize, biome);
            return go;
        }

        ParticleSystem ps;
        static Material s_motesMaterial;

        void Configure(Vector3 worldSize, BiomeDefinition biome)
        {
            ps = gameObject.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var psr = GetComponent<ParticleSystemRenderer>();
            if (psr != null) psr.sharedMaterial = ResolveMaterial();

            Color tint = biome != null ? biome.ambientTint : new Color(0.85f, 0.88f, 0.95f);
            // Pull motes a touch brighter than the ambient tint so they read
            // through bloom without being neon.
            Color moteColor = Color.Lerp(Color.white, tint, 0.45f);
            moteColor.a = 0.5f;

            var main = ps.main;
            main.playOnAwake = false;
            main.duration = 5f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.07f);
            main.startColor = moteColor;
            main.gravityModifier = 0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            // Cap so a long run doesn't spiral into thousands of motes.
            main.maxParticles = 220;

            var emission = ps.emission;
            emission.enabled = true;
            // ~22 motes per second sustained; volume + lifetime keep ~150 alive.
            emission.rateOverTime = 22f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = worldSize;

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // Slow horizontal drift + gentle bob via curves.
            var driftCurve = new AnimationCurve(
                new Keyframe(0f, -0.05f),
                new Keyframe(0.5f, 0.05f),
                new Keyframe(1f, -0.05f));
            var bobCurve = new AnimationCurve(
                new Keyframe(0f, -0.04f),
                new Keyframe(0.5f, 0.06f),
                new Keyframe(1f, -0.04f));
            velocity.x = new ParticleSystem.MinMaxCurve(0.4f, driftCurve);
            velocity.z = new ParticleSystem.MinMaxCurve(0.4f, driftCurve);
            velocity.y = new ParticleSystem.MinMaxCurve(1f, bobCurve);

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(moteColor, 0f), new GradientColorKey(moteColor, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.7f, 0.2f),
                    new GradientAlphaKey(0.7f, 0.8f),
                    new GradientAlphaKey(0f, 1f),
                });
            colorOverLifetime.color = new ParticleSystem.MinMaxGradient(grad);

            ps.Play();
        }

        static Material ResolveMaterial()
        {
            if (s_motesMaterial != null) return s_motesMaterial;
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            s_motesMaterial = new Material(sh) { name = "AmbientDustMotes(Runtime)" };
            if (s_motesMaterial.HasProperty("_Surface")) s_motesMaterial.SetFloat("_Surface", 1f);
            if (s_motesMaterial.HasProperty("_Blend")) s_motesMaterial.SetFloat("_Blend", 0f);
            return s_motesMaterial;
        }
    }
}
