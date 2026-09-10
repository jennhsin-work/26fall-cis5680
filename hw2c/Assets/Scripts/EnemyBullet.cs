using UnityEngine;

public enum DepthLane
{
    Back,
    Mid,
    Front
}

public class EnemyBullet : MonoBehaviour
{
    public float speed = 12.0f;
    public int damage = 1;
    public float lifetime = 5f;
    public Vector3 moveDirection = Vector3.back;

    [SerializeField] private GameObject impactEffectPrefab;

    private float spawnTime;
    private bool hasHit = false;
    private Light pointLight;

    private void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void Start()
    {
        spawnTime = Time.time;
        SetupVisuals();
    }

    public void InitDirection(Vector3 dir, float bulletSpeed = 12f)
    {
        moveDirection = dir.normalized;
        speed = bulletSpeed;
        transform.forward = moveDirection;
        SetupVisuals();
    }

    public void InitDepthLane(DepthLane lane, float customTargetZ = 0f)
    {
        InitDirection(Vector3.back, 11f);
    }

    private void SetupVisuals()
    {
        Color bulletColor = new Color(1.0f, 0.15f, 0.35f); // Crimson Plasma

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.material = ProceduralMeshBuilder.GetGlowMaterial(bulletColor, 2.2f);
        }

        if (pointLight == null)
        {
            GameObject lightObj = new GameObject("BulletLight");
            lightObj.transform.SetParent(transform, false);
            pointLight = lightObj.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = bulletColor;
            pointLight.range = 3.0f;
            pointLight.intensity = 2.5f;
        }
    }

    private void Update()
    {
        if (hasHit) return;

        Vector3 moveStep = moveDirection * (speed * Time.deltaTime);
        Vector3 nextPos = transform.position + moveStep;

        // Check proximity / collision against Player Ship / Cockpit
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            // Check if active Deflector Shield is raised
            if (player.IsShieldActive())
            {
                float shieldDist = Vector3.Distance(transform.position, player.transform.position + Vector3.forward * 0.9f);
                if (shieldDist < 1.8f)
                {
                    // Deflected by Shield!
                    hasHit = true;
                    if (AudioManager.Instance != null && AudioManager.Instance.shieldDeflectClip != null)
                    {
                        AudioManager.Instance.PlayEffect(AudioManager.Instance.shieldDeflectClip, 0.8f);
                    }
                    if (CameraController.Instance != null)
                    {
                        CameraController.Instance.AddShake(0.15f);
                    }
                    Hit();
                    return;
                }
            }

            // Direct Cockpit Hit
            float dx = Mathf.Abs(transform.position.x - player.transform.position.x);
            float dy = Mathf.Abs(transform.position.y - player.transform.position.y);
            float dz = Mathf.Abs(transform.position.z - player.transform.position.z);
            if (dx < 1.1f && dy < 1.0f && dz < 0.9f)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null)
                {
                    hasHit = true;
                    ph.TakeHit();
                    Hit();
                    return;
                }
            }
        }

        // Check collision against Shields, PlayerBullets, Boundaries
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, 0.25f, moveDirection, out hit, moveStep.magnitude, ~0, QueryTriggerInteraction.Collide))
        {
            if (ProcessHit(hit.collider))
            {
                return;
            }
        }

        transform.position = nextPos;

        if (Time.time - spawnTime >= lifetime || transform.position.z < -11f || transform.position.z > 25f)
        {
            Hit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        ProcessHit(other);
    }

    private bool ProcessHit(Collider other)
    {
        if (hasHit || other == null) return false;
        if (other.CompareTag("Invader") || other.CompareTag("EnemyBullet") || other.CompareTag("UFO")) return false;

        if (other.CompareTag("Player") || other.GetComponentInParent<PlayerHealth>() != null)
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>() ?? other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                hasHit = true;
                playerHealth.TakeHit();
                Hit();
                return true;
            }
        }
        else if (other.CompareTag("ShieldCell") || other.GetComponentInParent<ShieldCell>() != null)
        {
            ShieldCell cell = other.GetComponent<ShieldCell>() ?? other.GetComponentInParent<ShieldCell>();
            if (cell != null)
            {
                hasHit = true;
                cell.TakeDamage(damage);
                Hit();
                return true;
            }
        }
        else if (other.CompareTag("PlayerBullet"))
        {
            PlayerBullet pb = other.GetComponent<PlayerBullet>();
            if (pb != null)
            {
                hasHit = true;
                pb.Hit();
                Hit();
                return true;
            }
        }
        else if (other.CompareTag("BottomBoundary"))
        {
            hasHit = true;
            Hit();
            return true;
        }

        return false;
    }

    public void Hit()
    {
        hasHit = true;
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            CreateImpactVFX(transform.position);
        }
        Destroy(gameObject);
    }

    private void CreateImpactVFX(Vector3 pos)
    {
        GameObject fx = new GameObject("EnemyBulletImpact");
        fx.transform.position = pos;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.25f;
        main.startSpeed = 2.5f;
        main.startSize = 0.12f;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.magenta, Color.red);
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 12) });

        ps.Play();
    }
}


