using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int maxLives = 3;
    public int currentLives = 3;
    public float maxShield = 100f;
    public float currentShield = 100f;
    public float respawnDelay = 1.5f;
    public float invulnerabilityDuration = 2.0f;

    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private AudioClip hitSound;

    private PlayerController playerController;
    private MeshRenderer[] meshRenderers;
    private bool isInvulnerable = false;
    private Vector3 initialSpawnPos;

    private void Awake()
    {
        playerController = GetComponent<PlayerController>();
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        initialSpawnPos = transform.position;
        currentLives = maxLives;
        currentShield = maxShield;
    }

    public void Init(int startingLives)
    {
        maxLives = startingLives;
        currentLives = startingLives;
        currentShield = maxShield;
        isInvulnerable = false;
        transform.position = initialSpawnPos;
        SetVisible(true);
    }

    public void HealShield(float amount)
    {
        currentShield = Mathf.Min(maxShield, currentShield + amount);
    }

    public void DamageShield(float amount)
    {
        currentShield = Mathf.Max(0f, currentShield - amount);
    }

    public void TakeHit()
    {
        if (isInvulnerable) return;

        if (currentShield > 20f)
        {
            currentShield = Mathf.Max(0f, currentShield - 50f);
            if (AudioManager.Instance != null && AudioManager.Instance.shieldDeflectClip != null)
            {
                AudioManager.Instance.PlayEffect(AudioManager.Instance.shieldDeflectClip, 1.0f);
            }
            if (CameraController.Instance != null)
            {
                CameraController.Instance.AddShake(0.35f);
            }
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetWarningMessage("SHIELD DAMAGED!", 1.2f, true);
            }
            return;
        }

        currentLives--;
        currentShield = maxShield;

        if (AudioManager.Instance != null)
        {
            AudioClip clip = hitSound != null ? hitSound : AudioManager.Instance.playerHitClip;
            AudioManager.Instance.PlayEffect(clip, 1.0f);
        }

        if (explosionEffectPrefab != null)
        {
            Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            CreateExplosionVFX(transform.position);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerHit(currentLives);
        }

        if (currentLives > 0)
        {
            StartCoroutine(RespawnRoutine());
        }
        else
        {
            SetVisible(false);
            if (playerController != null) playerController.SetControlEnabled(false);
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver();
            }
        }
    }

    private IEnumerator RespawnRoutine()
    {
        SetVisible(false);
        if (playerController != null)
        {
            playerController.SetControlEnabled(false);
            playerController.ResetPosition(initialSpawnPos);
        }

        yield return new WaitForSeconds(respawnDelay);

        SetVisible(true);
        if (playerController != null)
        {
            playerController.SetControlEnabled(true);
        }

        // Invulnerability flicker
        StartCoroutine(InvulnerabilityFlickerRoutine());
    }

    private IEnumerator InvulnerabilityFlickerRoutine()
    {
        isInvulnerable = true;
        float elapsed = 0f;
        float flickerRate = 0.1f;

        while (elapsed < invulnerabilityDuration)
        {
            SetVisible(!AreRenderersEnabled());
            yield return new WaitForSeconds(flickerRate);
            elapsed += flickerRate;
        }

        SetVisible(true);
        isInvulnerable = false;
    }

    private bool AreRenderersEnabled()
    {
        if (meshRenderers == null || meshRenderers.Length == 0)
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
        }
        if (meshRenderers.Length > 0 && meshRenderers[0] != null)
        {
            return meshRenderers[0].enabled;
        }
        return true;
    }

    private void SetVisible(bool visible)
    {
        if (meshRenderers == null || meshRenderers.Length == 0)
        {
            meshRenderers = GetComponentsInChildren<MeshRenderer>();
        }
        foreach (var mr in meshRenderers)
        {
            if (mr != null) mr.enabled = visible;
        }
    }

    private void CreateExplosionVFX(Vector3 pos)
    {
        GameObject fx = new GameObject("PlayerExplosion");
        fx.transform.position = pos;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.6f;
        main.startSpeed = 5.0f;
        main.startSize = 0.25f;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.cyan, Color.white);
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 35) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        ps.Play();
    }
}
