using UnityEngine;

public class UFOController : MonoBehaviour
{
    public float speed = 5.0f;
    public float minSpawnInterval = 15.0f;
    public float maxSpawnInterval = 25.0f;
    public float spawnZ = 12.0f;
    public float boundaryX = 14.0f;
    public int[] possiblePoints = { 50, 100, 150, 300 };

    [SerializeField] private GameObject explosionEffectPrefab;
    [SerializeField] private AudioClip humSound;
    [SerializeField] private AudioClip hitSound;

    private float timer = 0f;
    private bool isFlying = false;
    private int direction = 1; // 1 = left to right, -1 = right to left
    private AudioSource humAudioSource;

    private void Awake()
    {
        timer = Random.Range(minSpawnInterval, maxSpawnInterval);
        humAudioSource = gameObject.AddComponent<AudioSource>();
        humAudioSource.loop = true;
        humAudioSource.playOnAwake = false;
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying())
        {
            if (isFlying) HideUFO();
            return;
        }

        if (!isFlying)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                SpawnUFO();
            }
        }
        else
        {
            transform.position += Vector3.right * (direction * speed * Time.deltaTime);

            if ((direction > 0 && transform.position.x > boundaryX) ||
                (direction < 0 && transform.position.x < -boundaryX))
            {
                HideUFO();
            }
        }
    }

    public void SpawnUFO()
    {
        direction = Random.value > 0.5f ? 1 : -1;
        float startX = direction > 0 ? -boundaryX : boundaryX;
        transform.position = new Vector3(startX, 0f, spawnZ);
        gameObject.SetActive(true);
        isFlying = true;

        if (AudioManager.Instance != null && AudioManager.Instance.ufoHumClip != null)
        {
            humAudioSource.clip = AudioManager.Instance.ufoHumClip;
            humAudioSource.volume = 0.5f;
            humAudioSource.Play();
        }
    }

    public void HideUFO()
    {
        isFlying = false;
        timer = Random.Range(minSpawnInterval, maxSpawnInterval);
        if (humAudioSource != null && humAudioSource.isPlaying)
        {
            humAudioSource.Stop();
        }
        transform.position = new Vector3(999f, 999f, 999f);
    }

    public void DestroyUFO()
    {
        if (!isFlying) return;

        int score = possiblePoints[Random.Range(0, possiblePoints.Length)];

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(score);
        }

        if (AudioManager.Instance != null)
        {
            AudioClip clip = hitSound != null ? hitSound : AudioManager.Instance.ufoDestroyClip;
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

        HideUFO();
    }

    private void CreateExplosionVFX(Vector3 pos)
    {
        GameObject fx = new GameObject("UFOExplosion");
        fx.transform.position = pos;
        ParticleSystem ps = fx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 6.0f;
        main.startSize = 0.2f;
        main.startColor = new ParticleSystem.MinMaxGradient(Color.magenta, Color.yellow);
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        ps.Play();
    }
}
