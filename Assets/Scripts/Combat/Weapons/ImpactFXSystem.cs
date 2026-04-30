using System.Collections.Generic;
using UnityEngine;

// Phase 5 / PR 5.C — scene singleton that owns muzzle-flash + bullet-impact
// VFX. Centralizing here avoids:
//   * each weapon owning its own ParticleSystem prefab (we don't have the
//     authoring effort to make 5 weapon-specific ones)
//   * unbounded growth of decal GameObjects on long levels
//
// Decal pool: FIFO ring buffer of `decalCapacity` quads. Once full, the oldest
// decal gets repositioned to the new hit instead of allocating a new GO.
//
// Auto-creates on first access via Instance.
public class ImpactFXSystem : MonoBehaviour
{
    public static ImpactFXSystem Instance
    {
        get
        {
            if (s_instance != null) return s_instance;
#if UNITY_2023_1_OR_NEWER
            s_instance = Object.FindFirstObjectByType<ImpactFXSystem>();
#else
            s_instance = Object.FindObjectOfType<ImpactFXSystem>();
#endif
            if (s_instance == null)
            {
                var go = new GameObject("ImpactFXSystem");
                s_instance = go.AddComponent<ImpactFXSystem>();
            }
            return s_instance;
        }
    }
    static ImpactFXSystem s_instance;

    [Header("Bullet Impact Decals")]
    [Tooltip("Maximum number of bullet-impact decals visible at once. Older decals are recycled FIFO.")]
    public int decalCapacity = 14;
    [Tooltip("Lifetime each decal fades over.")]
    public float decalLifetime = 7f;
    [Tooltip("Decal quad radius (world units).")]
    public float decalSize = 0.28f;

    [Header("Muzzle Flash")]
    [Tooltip("Muzzle-flash burst lifetime in seconds.")]
    public float muzzleFlashLifetime = 0.07f;
    [Tooltip("HDR color of the muzzle flash light + emission.")]
    [ColorUsage(true, true)]
    public Color muzzleFlashColor = new Color(1.6f, 1.2f, 0.4f);

    Material decalMaterial;
    Material muzzleFlashMaterial;
    Mesh quadMesh;
    readonly Queue<DecalEntry> decalQueue = new Queue<DecalEntry>();

    class DecalEntry
    {
        public GameObject go;
        public Renderer rend;
        public MaterialPropertyBlock mpb;
    }

    void Awake()
    {
        if (s_instance != null && s_instance != this)
        {
            Destroy(this);
            return;
        }
        s_instance = this;
    }

    /// <summary>
    /// Spawns a bullet-impact scorch on a wall/prop. Recycles the oldest decal
    /// if we are already at capacity.
    /// </summary>
    public void SpawnBulletDecal(Vector3 position, Vector3 normal)
    {
        if (decalMaterial == null) decalMaterial = BuildDecalMaterial();
        DecalEntry e;
        if (decalQueue.Count >= decalCapacity)
        {
            e = decalQueue.Dequeue();
            if (e.go == null) e = BuildDecalEntry();
        }
        else
        {
            e = BuildDecalEntry();
        }

        // Orient the quad to face along the normal. LookRotation expects forward,
        // which is +Z, so the quad's local +Z points along the surface normal —
        // i.e. the quad lies flat on the wall facing outward.
        e.go.transform.position = position + normal * 0.012f;
        e.go.transform.rotation = Quaternion.LookRotation(normal);
        float s = decalSize * 2f;
        e.go.transform.localScale = new Vector3(s, s, 1f);
        e.go.SetActive(true);

        if (e.rend != null)
        {
            e.rend.GetPropertyBlock(e.mpb);
            e.mpb.SetFloat("_BirthTime", Time.time);
            e.mpb.SetFloat("_Lifetime", decalLifetime);
            e.rend.SetPropertyBlock(e.mpb);
        }

        decalQueue.Enqueue(e);
    }

    /// <summary>
    /// Brief muzzle flash at the given position (in the firing direction). A
    /// tiny ParticleSystem burst + a half-frame emissive light.
    /// </summary>
    public void SpawnMuzzleFlash(Vector3 position, Quaternion rotation)
    {
        // Light pop — lights up nearby surfaces for one beat.
        var lightGo = new GameObject("MuzzleFlashLight");
        lightGo.transform.position = position;
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = muzzleFlashColor;
        light.intensity = 8f;
        light.range = 4f;
        light.shadows = LightShadows.None;
        var fader = lightGo.AddComponent<DeathFlashFader>();
        fader.duration = muzzleFlashLifetime;
        fader.startIntensity = light.intensity;

        // Particle puff — small, fast, additive.
        if (muzzleFlashMaterial == null) muzzleFlashMaterial = BuildMuzzleFlashMaterial();
        var psGo = new GameObject("MuzzleFlashFX");
        psGo.transform.SetPositionAndRotation(position, rotation);
        var ps = psGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var psr = psGo.GetComponent<ParticleSystemRenderer>();
        if (psr != null) psr.sharedMaterial = muzzleFlashMaterial;

        var main = ps.main;
        main.playOnAwake = false;
        main.duration = 0.05f;
        main.loop = false;
        main.startLifetime = muzzleFlashLifetime;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = 0.18f;
        main.startColor = muzzleFlashColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 24;
        main.stopAction = ParticleSystemStopAction.Destroy;
        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)10) });
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.05f;
        ps.Play();
    }

    DecalEntry BuildDecalEntry()
    {
        if (quadMesh == null) quadMesh = BuildQuadMesh();
        var go = new GameObject("BulletImpactDecal");
        go.transform.SetParent(transform, true);
        var mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = quadMesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = decalMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;
        return new DecalEntry
        {
            go = go,
            rend = mr,
            mpb = new MaterialPropertyBlock(),
        };
    }

    static Mesh BuildQuadMesh()
    {
        var m = new Mesh { name = "BulletImpactQuad" };
        m.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3( 0.5f, -0.5f, 0f),
            new Vector3( 0.5f,  0.5f, 0f),
            new Vector3(-0.5f,  0.5f, 0f),
        };
        m.uv = new[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f),
        };
        m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        m.RecalculateNormals();
        m.RecalculateBounds();
        return m;
    }

    static Material BuildDecalMaterial()
    {
        Shader sh = Shader.Find("VoidSurvivor/BulletImpactDecal");
        if (sh != null) return new Material(sh) { name = "BulletImpactDecal(Runtime)" };
        // Fallback: emissive transparent so the impact still reads.
        Shader fallback = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        var mat = new Material(fallback) { name = "BulletImpactDecal(Fallback)" };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.1f, 0.1f, 0.1f, 0.85f));
        return mat;
    }

    static Material BuildMuzzleFlashMaterial()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh) { name = "MuzzleFlash(Runtime)" };
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 1f);
        return mat;
    }
}
