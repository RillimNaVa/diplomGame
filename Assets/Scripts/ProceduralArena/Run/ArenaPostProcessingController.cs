using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VoidSurvivor.ProceduralArena.Arena;

namespace VoidSurvivor.ProceduralArena.Run
{
    /// <summary>
    /// PR 2.F — owns a runtime Global Volume with a runtime-constructed
    /// VolumeProfile. Per-biome settings (bloom / color adjustments / vignette)
    /// are applied when the biome changes. Higher priority than the scene's
    /// SampleSceneProfile so biome tint overrides project defaults.
    ///
    /// Biome color tint lives here instead of RenderSettings.ambient* so it
    /// passes through HDR tonemapping correctly.
    /// </summary>
    public class ArenaPostProcessingController : MonoBehaviour
    {
        Volume volume;
        VolumeProfile profile;
        Bloom bloom;
        ColorAdjustments color;
        Vignette vignette;
        Tonemapping tonemapping;

        void Awake()
        {
            BuildVolume();
        }

        void BuildVolume()
        {
            var go = new GameObject("ArenaBiomeVolume");
            go.transform.SetParent(transform, false);

            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;

            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "ArenaBiomeProfile(Runtime)";
            volume.sharedProfile = profile;

            bloom = profile.Add<Bloom>(overrides: true);
            color = profile.Add<ColorAdjustments>(overrides: true);
            vignette = profile.Add<Vignette>(overrides: true);
            tonemapping = profile.Add<Tonemapping>(overrides: true);

            // Force ACES tonemapping — the single biggest "не-роблокс" switch.
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.ACES;

            // Sensible defaults before the first biome applies.
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.45f;
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 0.9f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.7f;
            bloom.highQualityFiltering.overrideState = true;
            bloom.highQualityFiltering.value = true;

            color.contrast.overrideState = true;
            color.contrast.value = 6f;
            color.saturation.overrideState = true;
            color.saturation.value = 4f;
            color.postExposure.overrideState = true;
            color.postExposure.value = 0f;
            color.colorFilter.overrideState = true;
            color.colorFilter.value = Color.white;

            vignette.intensity.overrideState = true;
            vignette.intensity.value = 0.22f;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.35f;
        }

        public void ApplyBiome(BiomeDefinition biome)
        {
            if (profile == null) BuildVolume();
            if (biome == null)
            {
                color.colorFilter.value = Color.white;
                bloom.intensity.value = 0.45f;
                bloom.tint.overrideState = false;
                vignette.intensity.value = 0.22f;
                return;
            }

            var pp = biome.postProcessing ?? new BiomePostProcessing();

            if (pp.overrideBloom)
            {
                bloom.intensity.value = pp.bloomIntensity;
                bloom.threshold.value = pp.bloomThreshold;
                bloom.scatter.value = pp.bloomScatter;
                bloom.tint.overrideState = true;
                bloom.tint.value = pp.bloomTint;
            }

            if (pp.overrideColor)
            {
                color.postExposure.value = pp.postExposure;
                color.contrast.value = pp.contrast;
                // Blend biome ambientTint into colorFilter — this is where the
                // per-biome "цветной" vibe actually lives now.
                Color filter = pp.colorFilter;
                if (biome.ambientTint != Color.white)
                    filter = Color.Lerp(filter, biome.ambientTint, 0.35f);
                color.colorFilter.value = filter;
                color.hueShift.value = pp.hueShift;
                color.saturation.value = pp.saturation;
            }

            if (pp.overrideVignette)
            {
                vignette.color.overrideState = true;
                vignette.color.value = pp.vignetteColor;
                vignette.intensity.value = pp.vignetteIntensity;
                vignette.smoothness.value = pp.vignetteSmoothness;
            }
        }

        public void ClearBiome()
        {
            ApplyBiome(null);
        }

        // ----- PR 4.A combat readability: damage vignette pulse -----

        float damageVignetteUntil;
        float damageVignetteStart;
        float damageVignetteBaseline;
        Color damageVignetteBaselineColor;
        const float DamageVignettePeak = 0.55f;
        static readonly Color DamageVignetteColor = new Color(0.9f, 0.05f, 0.05f);

        /// <summary>
        /// PR 4.A — flashes the post-FX vignette red, then ramps back to the
        /// biome baseline. Call from PlayerHitFeedback whenever the player
        /// takes damage. Stacks correctly across rapid hits.
        /// </summary>
        public void PulseDamageVignette(float duration = 0.45f)
        {
            if (vignette == null) return;
            // Capture the pre-pulse intensity ONLY if we're not already pulsing,
            // otherwise we'd re-capture an already-elevated intensity and
            // never decay back to the biome baseline.
            if (Time.time >= damageVignetteUntil)
            {
                damageVignetteBaseline = vignette.intensity.value;
                damageVignetteBaselineColor = vignette.color.value;
            }
            damageVignetteStart = Time.time;
            damageVignetteUntil = Time.time + Mathf.Max(0.05f, duration);
            vignette.color.overrideState = true;
        }

        void Update()
        {
            if (vignette == null) return;
            UpdateDamagePulse();
            UpdateLowHealthPulse();
        }

        void UpdateDamagePulse()
        {
            if (Time.time >= damageVignetteUntil) return;
            float total = damageVignetteUntil - damageVignetteStart;
            float t = total > 0f ? (Time.time - damageVignetteStart) / total : 1f;
            // Sharp on, ease-out decay back to baseline.
            float intensity = Mathf.Lerp(DamageVignettePeak, damageVignetteBaseline, t * t);
            Color c = Color.Lerp(DamageVignetteColor, damageVignetteBaselineColor, t * t);
            vignette.intensity.value = intensity;
            vignette.color.value = c;

            if (Time.time >= damageVignetteUntil)
            {
                vignette.intensity.value = damageVignetteBaseline;
                vignette.color.value = damageVignetteBaselineColor;
            }
        }

        // ----- PR 5.C: low-HP heartbeat vignette -----

        bool lowHealthActive;
        float lowHealthBaselineIntensity;
        Color lowHealthBaselineColor;
        const float LowHealthPulseHz = 1.7f;     // ~100 BPM
        const float LowHealthPeak = 0.50f;
        static readonly Color LowHealthColor = new Color(0.85f, 0.05f, 0.05f);

        /// <summary>
        /// PR 5.C — drives the continuous heartbeat vignette while the player
        /// is below 25% HP. Call SetLowHealthPulse(true) when threshold crossed
        /// downward, false when crossed upward. Idempotent.
        /// </summary>
        public void SetLowHealthPulse(bool active)
        {
            if (vignette == null) return;
            if (active == lowHealthActive) return;
            if (active)
            {
                lowHealthBaselineIntensity = vignette.intensity.value;
                lowHealthBaselineColor = vignette.color.value;
                vignette.color.overrideState = true;
            }
            else
            {
                vignette.intensity.value = lowHealthBaselineIntensity;
                vignette.color.value = lowHealthBaselineColor;
            }
            lowHealthActive = active;
        }

        void UpdateLowHealthPulse()
        {
            if (!lowHealthActive) return;
            // Damage pulse takes priority — don't fight it.
            if (Time.time < damageVignetteUntil) return;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.time * LowHealthPulseHz * Mathf.PI * 2f);
            // Sharpen the curve so it reads as a heartbeat, not a sine wave.
            k = Mathf.Pow(k, 2.5f);
            float intensity = Mathf.Lerp(lowHealthBaselineIntensity, LowHealthPeak, k);
            Color c = Color.Lerp(lowHealthBaselineColor, LowHealthColor, k * 0.85f);
            vignette.intensity.value = intensity;
            vignette.color.value = c;
        }

        void OnDestroy()
        {
            if (profile != null)
            {
                if (Application.isPlaying) Destroy(profile);
                else DestroyImmediate(profile);
            }
        }
    }
}
