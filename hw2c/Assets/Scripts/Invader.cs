using UnityEngine;

public enum InvaderType
{
    Squid,   // Top row - 30 pts
    Crab,    // Middle rows - 20 pts
    Octopus  // Bottom rows - 10 pts
}

public enum InvaderState
{
    InFormation,
    DepthCharging,
    Retreating
}

public class Invader : MonoBehaviour
{
    public InvaderType invaderType = InvaderType.Octopus;
    public int points = 10;
    public int rowIndex = 0;
    public int colIndex = 0;
    public bool isAlive = true;
    public int maxHealth = 100;
    public int currentHealth = 100;

    [Header("Depth-Charging State")]
    public InvaderState currentState = InvaderState.InFormation;
    public float diveSpeed = 9.0f;
    public float diveShootInterval = 0.85f;
    public float minDiveZ = -4.2f;

    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private AudioClip deathSound;

    private InvaderAnimation invaderAnimation;
    private Vector3 formationLocalOffset;
    private Transform formationTransform;
    private Vector3 diveStartPos;
    private Vector3 diveTargetPos;
    private float diveProgress = 0f;
    private float diveShootTimer = 0f;
    private float diveSwayFreq = 3.5f;
    private float diveSwayAmp = 2.0f;
    private BoxCollider boxCollider;

    private void Awake()
    {
        invaderAnimation = GetComponent<InvaderAnimation>();
        if (invaderAnimation == null)
        {
            invaderAnimation = gameObject.AddComponent<InvaderAnimation>();
        }
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size = new Vector3(1.3f, 1.1f, 1.2f);
        }
    }

    public void Init(InvaderType type, int row, int col)
    {
        invaderType = type;
        rowIndex = row;
        colIndex = col;
        isAlive = true;
        currentState = InvaderState.InFormation;
        formationTransform = transform.parent;
        formationLocalOffset = transform.localPosition;

        switch (type)
        {
            case InvaderType.Squid:
                points = 30;
                maxHealth = 80;
                break;
            case InvaderType.Crab:
                points = 20;
                maxHealth = 100;
                break;
            case InvaderType.Octopus:
                points = 10;
                maxHealth = 120;
                break;
        }
        currentHealth = maxHealth;
    }

    private void Update()
    {
        if (!isAlive) return;

        if (currentState == InvaderState.DepthCharging)
        {
            UpdateDepthCharge();
        }
        else if (currentState == InvaderState.Retreating)
        {
            UpdateRetreat();
        }
    }

    public void StartDepthCharge(Vector3 targetPlayerPos)
    {
        if (!isAlive || currentState != InvaderState.InFormation) return;

        currentState = InvaderState.DepthCharging;
        diveStartPos = transform.position;
        diveTargetPos = new Vector3(targetPlayerPos.x, 0.4f, minDiveZ);
        diveProgress = 0f;
        diveShootTimer = 0.3f;
        diveSwayFreq = Random.Range(3f, 5f);
        diveSwayAmp = Random.Range(1.5f, 3.0f);

        // Notify HUD/Cockpit Warning
        if (UIManager.Instance != null)
        {
            UIManager.Instance.SetWarningMessage("DEPTH CHARGE DETECTED! INCOMING ENEMY", 2.0f, true);
        }
        if (AudioManager.Instance != null && AudioManager.Instance.depthChargeDiveClip != null)
        {
            AudioManager.Instance.PlayEffect(AudioManager.Instance.depthChargeDiveClip, 0.75f);
        }
    }

    private void UpdateDepthCharge()
    {
        diveProgress += Time.deltaTime * (diveSpeed / Mathf.Max(1f, Vector3.Distance(diveStartPos, diveTargetPos)));

        // Interpolate along Z and weave in X
        float currentZ = Mathf.Lerp(diveStartPos.z, diveTargetPos.z, diveProgress);
        float baseX = Mathf.Lerp(diveStartPos.x, diveTargetPos.x, diveProgress);
        float swayX = Mathf.Sin(diveProgress * Mathf.PI * diveSwayFreq) * diveSwayAmp;
        float currentY = Mathf.Sin(diveProgress * Mathf.PI) * 1.5f + 0.3f;

        transform.position = new Vector3(baseX + swayX, currentY, currentZ);

        // Look toward player
        Vector3 dir = (diveTargetPos - transform.position).normalized;
        if (dir.sqrMagnitude > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 8f);
        }

        // Rapid shooting during dive
        diveShootTimer -= Time.deltaTime;
        if (diveShootTimer <= 0f)
        {
            diveShootTimer = diveShootInterval;
            ShootAimedPlasma();
        }

        // Check collision threat to Player
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distToPlayer < 1.4f)
            {
                // Crash into cockpit / player ship!
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeHit();
                }
                DestroyInvader(true);
                return;
            }
        }

        // Reached dive apex / bottom
        if (diveProgress >= 1.0f || transform.position.z <= minDiveZ)
        {
            StartRetreat();
        }
    }

    private void StartRetreat()
    {
        currentState = InvaderState.Retreating;
    }

    private void UpdateRetreat()
    {
        Vector3 targetSlotWorld = formationTransform != null ? formationTransform.TransformPoint(formationLocalOffset) : diveStartPos;
        transform.position = Vector3.MoveTowards(transform.position, targetSlotWorld, Time.deltaTime * (diveSpeed * 0.9f));
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.identity, Time.deltaTime * 6f);

        if (Vector3.Distance(transform.position, targetSlotWorld) < 0.2f)
        {
            transform.position = targetSlotWorld;
            transform.rotation = Quaternion.identity;
            currentState = InvaderState.InFormation;
        }
    }

    public void Shoot()
    {
        if (!isAlive || currentState != InvaderState.InFormation) return;

        Vector3 spawnPos = transform.position + Vector3.back * 0.8f + Vector3.up * 0.2f;
        Spawn3DProjectile(spawnPos, Vector3.back);
    }

    public void ShootAimedPlasma()
    {
        if (!isAlive) return;

        Vector3 spawnPos = transform.position + Vector3.back * 0.7f;
        Vector3 targetPos = Vector3.back * 6.5f;

        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            targetPos = player.transform.position + Vector3.up * 0.4f;
        }

        Vector3 aimDir = (targetPos - spawnPos).normalized;
        Spawn3DProjectile(spawnPos, aimDir);
    }

    private void Spawn3DProjectile(Vector3 spawnPos, Vector3 direction)
    {
        if (bulletPrefab != null)
        {
            GameObject bObj = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(direction));
            EnemyBullet eb = bObj.GetComponent<EnemyBullet>();
            if (eb != null) eb.InitDirection(direction);
        }
        else
        {
            GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bullet.name = "EnemyBullet";
            bullet.tag = "EnemyBullet";
            bullet.transform.position = spawnPos;
            bullet.transform.localScale = new Vector3(0.38f, 0.38f, 0.38f);

            Collider col = bullet.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            EnemyBullet eb = bullet.AddComponent<EnemyBullet>();
            eb.InitDirection(direction);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect(AudioManager.Instance.enemyShotClip, 0.65f);
        }
    }

    public void TakeDamage(int damage, bool isCritical, Vector3 hitPoint)
    {
        if (!isAlive) return;

        currentHealth -= damage;

        if (isCritical || currentHealth <= 0)
        {
            int awardedPoints = points;
            if (currentState == InvaderState.DepthCharging)
            {
                awardedPoints += 150; // High bonus for intercepting depth charge!
            }
            if (isCritical)
            {
                awardedPoints = Mathf.RoundToInt(awardedPoints * 1.5f);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(awardedPoints);
            }

            DestroyInvader(false);
        }
        else
        {
            // Glancing hit sparks
            CreateGlanceSparks(hitPoint);
        }
    }

    public void DestroyInvader(bool silent = false)
    {
        if (!isAlive) return;
        isAlive = false;

        if (!silent && AudioManager.Instance != null)
        {
            AudioClip clip = deathSound != null ? deathSound : AudioManager.Instance.invaderHitClip;
            AudioManager.Instance.PlayEffect(clip, 0.85f);
        }

        if (explosionPrefab != null)
        {
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            CreateExplosionVFX(transform.position);
        }

        FormationController formation = FindFirstObjectByType<FormationController>();
        if (formation != null)
        {
            formation.OnInvaderKilled(this);
        }

        Destroy(gameObject);
    }

    private void CreateGlanceSparks(Vector3 pos)
    {
        GameObject fx = new GameObject("GlanceSparks");
        fx.transform.position = pos;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.2f;
        main.startSpeed = 2.5f;
        main.startSize = 0.08f;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.cyan, Color.white);
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });

        ps.Play();
    }

    private void CreateExplosionVFX(Vector3 pos)
    {
        GameObject fx = new GameObject("InvaderExplosion");
        fx.transform.position = pos;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.45f;
        main.startSpeed = 4.2f;
        main.startSize = 0.22f;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.yellow, Color.red);
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 28) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        ps.Play();
    }
}

