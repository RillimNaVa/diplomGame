using UnityEngine;
using UnityEngine.UI;

// Center crosshair + hit / kill markers. Subscribes to Health.AnyDamaged
// (filtered to non-player) for hit feedback and to GameManager.OnEnemyKilled
// for kill markers.
public class CrosshairBlock
{
    RectTransform root;
    Image[] crossArms;       // 4 short ticks
    Image dot;
    RectTransform hitMarker;  // 4 tiny corner ticks
    Image[] hitMarkerArms;
    RectTransform killMarker; // 4 thicker red ticks rotated 45
    Image[] killMarkerArms;

    float hitMarkerTimer;
    float killMarkerTimer;

    Health playerHealth;

    public static CrosshairBlock Build(RectTransform canvasRoot, Health playerHp)
    {
        var block = new CrosshairBlock { playerHealth = playerHp };
        block.BuildInternal(canvasRoot);
        block.Subscribe();
        return block;
    }

    void BuildInternal(RectTransform canvasRoot)
    {
        var go = new GameObject("CrosshairBlock", typeof(RectTransform));
        root = (RectTransform)go.transform;
        root.SetParent(canvasRoot, false);
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = Vector2.zero;
        root.sizeDelta = new Vector2(64f, 64f);

        // Center dot
        var dotGo = new GameObject("Dot", typeof(RectTransform));
        var dotRt = (RectTransform)dotGo.transform;
        dotRt.SetParent(root, false);
        dotRt.anchorMin = dotRt.anchorMax = new Vector2(0.5f, 0.5f);
        dotRt.pivot = new Vector2(0.5f, 0.5f);
        dotRt.sizeDelta = new Vector2(2.5f, 2.5f);
        dotRt.anchoredPosition = Vector2.zero;
        dot = dotGo.AddComponent<Image>();
        dot.sprite = CombatHUDStyle.SoftCircleSprite();
        dot.color = CombatHUDStyle.White;
        dot.raycastTarget = false;

        // 4 small ticks at +/-X, +/-Y
        crossArms = new Image[4];
        Vector2[] offsets = {
            new Vector2(0f, 9f),  new Vector2(0f, -9f),
            new Vector2(9f, 0f),  new Vector2(-9f, 0f),
        };
        Vector2[] sizes = {
            new Vector2(2f, 6f), new Vector2(2f, 6f),
            new Vector2(6f, 2f), new Vector2(6f, 2f),
        };
        for (int i = 0; i < 4; i++)
        {
            var armGo = new GameObject($"Arm_{i}", typeof(RectTransform));
            var armRt = (RectTransform)armGo.transform;
            armRt.SetParent(root, false);
            armRt.anchorMin = armRt.anchorMax = new Vector2(0.5f, 0.5f);
            armRt.pivot = new Vector2(0.5f, 0.5f);
            armRt.sizeDelta = sizes[i];
            armRt.anchoredPosition = offsets[i];
            var img = armGo.AddComponent<Image>();
            img.sprite = CombatHUDStyle.WhiteSprite();
            img.color = CombatHUDStyle.White;
            img.raycastTarget = false;
            crossArms[i] = img;
        }

        BuildHitMarker(canvasRoot);
        BuildKillMarker(canvasRoot);
    }

    void BuildHitMarker(RectTransform canvasRoot)
    {
        var hmGo = new GameObject("HitMarker", typeof(RectTransform));
        hitMarker = (RectTransform)hmGo.transform;
        hitMarker.SetParent(root, false);
        hitMarker.anchorMin = hitMarker.anchorMax = new Vector2(0.5f, 0.5f);
        hitMarker.pivot = new Vector2(0.5f, 0.5f);
        hitMarker.anchoredPosition = Vector2.zero;
        hitMarker.sizeDelta = new Vector2(28f, 28f);

        // Four diagonal ticks: rotated 45 degrees, placed at corners
        hitMarkerArms = new Image[4];
        Vector2[] offsets = {
            new Vector2( 9f,  9f), new Vector2(-9f,  9f),
            new Vector2( 9f, -9f), new Vector2(-9f, -9f),
        };
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject($"HmArm_{i}", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(hitMarker, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(2.5f, 8f);
            rt.anchoredPosition = offsets[i];
            rt.localEulerAngles = new Vector3(0f, 0f, 45f);
            var img = go.AddComponent<Image>();
            img.sprite = CombatHUDStyle.WhiteSprite();
            img.color = new Color(CombatHUDStyle.White.r, CombatHUDStyle.White.g, CombatHUDStyle.White.b, 0f);
            img.raycastTarget = false;
            hitMarkerArms[i] = img;
        }
    }

    void BuildKillMarker(RectTransform canvasRoot)
    {
        var kmGo = new GameObject("KillMarker", typeof(RectTransform));
        killMarker = (RectTransform)kmGo.transform;
        killMarker.SetParent(root, false);
        killMarker.anchorMin = killMarker.anchorMax = new Vector2(0.5f, 0.5f);
        killMarker.pivot = new Vector2(0.5f, 0.5f);
        killMarker.anchoredPosition = Vector2.zero;
        killMarker.sizeDelta = new Vector2(40f, 40f);

        killMarkerArms = new Image[4];
        Vector2[] offsets = {
            new Vector2( 13f,  13f), new Vector2(-13f,  13f),
            new Vector2( 13f, -13f), new Vector2(-13f, -13f),
        };
        for (int i = 0; i < 4; i++)
        {
            var go = new GameObject($"KmArm_{i}", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(killMarker, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(4f, 12f);
            rt.anchoredPosition = offsets[i];
            rt.localEulerAngles = new Vector3(0f, 0f, 45f);
            var img = go.AddComponent<Image>();
            img.sprite = CombatHUDStyle.WhiteSprite();
            img.color = new Color(CombatHUDStyle.WarnRed.r, CombatHUDStyle.WarnRed.g, CombatHUDStyle.WarnRed.b, 0f);
            img.raycastTarget = false;
            killMarkerArms[i] = img;
        }
    }

    public void Subscribe()
    {
        Health.AnyDamaged += OnAnyDamaged;
        if (GameManager.instance != null) GameManager.instance.OnEnemyKilled += OnEnemyKilled;
    }

    public void Unsubscribe()
    {
        Health.AnyDamaged -= OnAnyDamaged;
        if (GameManager.instance != null) GameManager.instance.OnEnemyKilled -= OnEnemyKilled;
    }

    void OnAnyDamaged(Health victim, float amount)
    {
        if (victim == null || victim == playerHealth) return;
        // Filter out other player-tagged objects too (defensive)
        if (victim.gameObject.CompareTag("Player")) return;
        hitMarkerTimer = 0.18f;
    }

    void OnEnemyKilled()
    {
        killMarkerTimer = 0.32f;
    }

    public void Tick(float dt)
    {
        if (hitMarkerTimer > 0f)
        {
            hitMarkerTimer -= dt;
            float a = Mathf.Clamp01(hitMarkerTimer / 0.18f);
            for (int i = 0; i < hitMarkerArms.Length; i++)
            {
                var c = CombatHUDStyle.White; c.a = a;
                hitMarkerArms[i].color = c;
            }
        }
        else
        {
            for (int i = 0; i < hitMarkerArms.Length; i++)
            {
                var c = hitMarkerArms[i].color; c.a = 0f;
                hitMarkerArms[i].color = c;
            }
        }

        if (killMarkerTimer > 0f)
        {
            killMarkerTimer -= dt;
            float a = Mathf.Clamp01(killMarkerTimer / 0.32f);
            // Pulse slight scale-down over life
            float s = Mathf.Lerp(1.15f, 1f, 1f - a);
            killMarker.localScale = new Vector3(s, s, 1f);
            for (int i = 0; i < killMarkerArms.Length; i++)
            {
                var c = CombatHUDStyle.WarnRed; c.a = a;
                killMarkerArms[i].color = c;
            }
        }
        else
        {
            for (int i = 0; i < killMarkerArms.Length; i++)
            {
                var c = killMarkerArms[i].color; c.a = 0f;
                killMarkerArms[i].color = c;
            }
        }
    }
}
