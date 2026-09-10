using UnityEngine;

public class StarfieldBackground : MonoBehaviour
{
    public int starCount = 450;
    public float fieldWidth = 60f;
    public float fieldHeightMin = -4f;
    public float fieldHeightMax = 28f;
    public float fieldLength = 70f;
    public float speed = 2.0f;

    private ParticleSystem particleSys;
    private ParticleSystem.Particle[] particles;
    private float[] particleSpeeds;

    private void Start()
    {
        particleSys = gameObject.AddComponent<ParticleSystem>();
        var main = particleSys.main;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = float.MaxValue;
        main.maxParticles = starCount;

        var emission = particleSys.emission;
        emission.enabled = false;

        var shape = particleSys.shape;
        shape.enabled = false;

        var renderer = particleSys.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Sprites/Default") ?? Shader.Find("Standard"));

        particles = new ParticleSystem.Particle[starCount];
        particleSpeeds = new float[starCount];

        Color[] starPalettes = new Color[]
        {
            new Color(1f, 1f, 1f, 0.95f),         // Pure White
            new Color(0.3f, 0.85f, 1f, 0.95f),     // Cyan Neon
            new Color(1f, 0.4f, 0.85f, 0.9f),      // Pink / Magenta
            new Color(1f, 0.92f, 0.35f, 0.9f),     // Warm Gold
            new Color(0.5f, 0.7f, 1f, 0.95f),      // Soft Blue
            new Color(0.4f, 1f, 0.6f, 0.9f)        // Emerald Neon
        };

        for (int i = 0; i < starCount; i++)
        {
            particles[i].position = new Vector3(
                Random.Range(-fieldWidth * 0.5f, fieldWidth * 0.5f),
                Random.Range(fieldHeightMin, fieldHeightMax),
                Random.Range(-fieldLength * 0.4f, fieldLength * 0.6f)
            );

            // Layered parallax speeds & sizes
            float layer = Random.value;
            if (layer < 0.6f) // Distant tiny stars
            {
                particles[i].startSize = Random.Range(0.08f, 0.16f);
                particleSpeeds[i] = speed * 0.4f;
            }
            else if (layer < 0.9f) // Mid-ground stars
            {
                particles[i].startSize = Random.Range(0.18f, 0.30f);
                particleSpeeds[i] = speed * 0.9f;
            }
            else // Foreground bright stars
            {
                particles[i].startSize = Random.Range(0.32f, 0.50f);
                particleSpeeds[i] = speed * 1.5f;
            }

            Color baseColor = starPalettes[Random.Range(0, starPalettes.Length)];
            particles[i].startColor = baseColor;
            particles[i].remainingLifetime = float.MaxValue;
        }

        particleSys.SetParticles(particles, starCount);
    }

    private void Update()
    {
        if (particles == null || particleSys == null) return;

        int count = particleSys.GetParticles(particles);
        float minZ = -fieldLength * 0.4f;
        float maxZ = fieldLength * 0.6f;
        float dt = Time.deltaTime;

        for (int i = 0; i < count; i++)
        {
            Vector3 pos = particles[i].position;
            float currentSpeed = (particleSpeeds != null && i < particleSpeeds.Length) ? particleSpeeds[i] : speed;
            pos.z -= currentSpeed * dt;
            if (pos.z < minZ)
            {
                pos.z = maxZ;
                pos.x = Random.Range(-fieldWidth * 0.5f, fieldWidth * 0.5f);
                pos.y = Random.Range(fieldHeightMin, fieldHeightMax);
            }
            particles[i].position = pos;
        }

        particleSys.SetParticles(particles, count);
    }
}

