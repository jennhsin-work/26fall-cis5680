using UnityEngine;

public class ShieldCell : MonoBehaviour
{
    public int hitPoints = 2;
    public int maxHitPoints = 2;
    public Material[] damageMaterialStages;

    [SerializeField] private GameObject impactEffectPrefab;
    [SerializeField] private AudioClip hitSound;

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        hitPoints = maxHitPoints;
    }

    public void TakeDamage(int damage)
    {
        hitPoints -= damage;

        if (AudioManager.Instance != null && hitSound != null)
        {
            AudioManager.Instance.PlayEffect(hitSound);
        }

        if (hitPoints <= 0)
        {
            DestroyCell();
        }
        else
        {
            UpdateAppearance();
        }
    }

    public void UpdateAppearance()
    {
        if (meshRenderer != null && damageMaterialStages != null && damageMaterialStages.Length > 0)
        {
            int index = Mathf.Clamp(maxHitPoints - hitPoints, 0, damageMaterialStages.Length - 1);
            if (damageMaterialStages[index] != null)
            {
                meshRenderer.material = damageMaterialStages[index];
            }
        }
        // Slightly scale down on damage to show erosion
        float scalePct = Mathf.Clamp01((float)hitPoints / maxHitPoints);
        transform.localScale = Vector3.one * (0.6f + 0.4f * scalePct);
    }

    public void DestroyCell()
    {
        if (impactEffectPrefab != null)
        {
            Instantiate(impactEffectPrefab, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            PlayerBullet pb = other.GetComponent<PlayerBullet>();
            if (pb != null)
            {
                TakeDamage(pb.damage);
                pb.Hit();
            }
        }
        else if (other.CompareTag("EnemyBullet"))
        {
            EnemyBullet eb = other.GetComponent<EnemyBullet>();
            if (eb != null)
            {
                TakeDamage(eb.damage);
                eb.Hit();
            }
        }
        else if (other.CompareTag("Invader"))
        {
            // Invaders destroy shield cells on contact directly
            DestroyCell();
        }
    }
}
