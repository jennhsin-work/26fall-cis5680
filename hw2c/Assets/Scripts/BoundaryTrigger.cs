using UnityEngine;

public enum BoundaryType
{
    Left,
    Right,
    Top,
    Bottom
}

public class BoundaryTrigger : MonoBehaviour
{
    public BoundaryType boundaryType;

    private void OnTriggerEnter(Collider other)
    {
        NotifyFormation(other);
        RemoveProjectile(other);
    }

    public void NotifyFormation(Collider other)
    {
        if (other.CompareTag("Invader"))
        {
            Invader invader = other.GetComponent<Invader>();
            if (invader != null && invader.isAlive)
            {
                FormationController formation = FindFirstObjectByType<FormationController>();
                if (formation != null)
                {
                    formation.OnBoundaryReached(boundaryType);
                }
            }
        }
    }

    public void RemoveProjectile(Collider other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            PlayerBullet bullet = other.GetComponent<PlayerBullet>();
            if (bullet != null)
            {
                bullet.Hit();
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
        else if (other.CompareTag("EnemyBullet"))
        {
            EnemyBullet bullet = other.GetComponent<EnemyBullet>();
            if (bullet != null)
            {
                bullet.Hit();
            }
            else
            {
                Destroy(other.gameObject);
            }
        }
    }
}
