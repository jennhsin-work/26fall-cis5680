using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    [Header("Trajectory & Focus")]
    public float speed = 32f;
    public float focalDepth = 14f;
    public float focalTolerance = 2.5f;
    public int baseDamage = 35;
    public int criticalDamage = 120;
    public int damage = 1;
    public Vector3 originPosition;
    public Vector3 moveDirection = Vector3.forward;
    public GameObject owner;
    public float lifetime = 3.5f;

    [SerializeField] private GameObject impactEffectPrefab;

    private float spawnTime;
    private bool hasHit = false;

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
        if (originPosition == Vector3.zero && owner != null)
        {
            originPosition = owner.transform.position;
        }
    }

    public void InitFocus(Vector3 origin, Vector3 dir, float focusDistance)
    {
        originPosition = origin;
        moveDirection = dir.normalized;
        focalDepth = focusDistance;
        transform.forward = moveDirection;
    }

    private void Update()
    {
        if (hasHit) return;

        Vector3 moveStep = moveDirection * (speed * Time.deltaTime);
        Vector3 nextPos = transform.position + moveStep;

        // Perform cast check ahead of movement to prevent tunneling through enemies
        RaycastHit hit;
        if (Physics.SphereCast(transform.position, 0.28f, moveDirection, out hit, moveStep.magnitude, ~0, QueryTriggerInteraction.Collide))
        {
            if (ProcessHit(hit.collider, hit.point))
            {
                return;
            }
        }

        transform.position = nextPos;

        if (Time.time - spawnTime >= lifetime || transform.position.z > 25f || transform.position.z < -10f)
        {
            Hit();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;
        ProcessHit(other, transform.position);
    }

    private bool ProcessHit(Collider other, Vector3 hitPoint)
    {
        if (hasHit || other == null) return false;
        if (other.gameObject == owner || other.CompareTag("Player") || other.CompareTag("PlayerBullet")) return false;

        if (other.CompareTag("Invader") || other.GetComponentInParent<Invader>() != null)
        {
            Invader invader = other.GetComponent<Invader>() ?? other.GetComponentInParent<Invader>();
            if (invader != null && invader.isAlive)
            {
                hasHit = true;
                float targetDist = Vector3.Distance(originPosition, invader.transform.position);
                float focusDelta = Mathf.Abs(focalDepth - targetDist);
                bool isPerfectFocus = focusDelta <= focalTolerance;

                int appliedDamage = isPerfectFocus ? criticalDamage : baseDamage;
                invader.TakeDamage(appliedDamage, isPerfectFocus, hitPoint);

                if (isPerfectFocus)
                {
                    if (AudioManager.Instance != null && AudioManager.Instance.criticalHitClip != null)
                    {
                        AudioManager.Instance.PlayEffect(AudioManager.Instance.criticalHitClip, 0.9f);
                    }
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowFocusHitFeedback(true, targetDist, focalDepth);
                    }
                }
                else
                {
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowFocusHitFeedback(false, targetDist, focalDepth);
                    }
                }

                Hit();
                return true;
            }
        }
        else if (other.CompareTag("UFO") || other.GetComponentInParent<UFOController>() != null)
        {
            UFOController ufo = other.GetComponent<UFOController>() ?? other.GetComponentInParent<UFOController>();
            if (ufo != null)
            {
                hasHit = true;
                ufo.DestroyUFO();
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
                cell.TakeDamage(1);
                Hit();
                return true;
            }
        }
        else if (other.CompareTag("EnemyBullet"))
        {
            EnemyBullet enemyBullet = other.GetComponent<EnemyBullet>();
            if (enemyBullet != null)
            {
                hasHit = true;
                enemyBullet.Hit();
                Hit();
                return true;
            }
        }
        else if (other.CompareTag("TopBoundary"))
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

        if (owner != null)
        {
            PlayerController pc = owner.GetComponent<PlayerController>();
            if (pc != null && pc.activeBullet == gameObject)
            {
                pc.activeBullet = null;
            }
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (owner != null)
        {
            PlayerController pc = owner.GetComponent<PlayerController>();
            if (pc != null && pc.activeBullet == gameObject)
            {
                pc.activeBullet = null;
            }
        }
    }
}

