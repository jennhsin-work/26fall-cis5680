using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FormationController : MonoBehaviour
{
    [Header("Grid Configuration")]
    public int rows = 5;
    public int columns = 11;
    public float spacingX = 1.6f;
    public float spacingZ = 1.3f;
    public Vector3 startPosition = new Vector3(-8.0f, 0f, 10f);

    [Header("Prefabs & Materials")]
    public GameObject squidPrefab;
    public GameObject crabPrefab;
    public GameObject octopusPrefab;
    public Material squidMaterial;
    public Material crabMaterial;
    public Material octopusMaterial;

    [Header("Movement & Step Timing")]
    public float stepDistanceX = 0.4f;
    public float stepDistanceZ = 0.8f;
    public float initialStepInterval = 1.0f;
    public float minStepInterval = 0.08f;
    public float currentDirection = 1f; // 1 = right, -1 = left
    public float xBoundaryLimit = 11.0f;
    public float bottomLossZ = -7.0f;

    [Header("Shooting")]
    public float minShootDelay = 1.0f;
    public float maxShootDelay = 3.0f;
    public GameObject enemyBulletPrefab;

    private List<Invader> activeInvaders = new List<Invader>();
    private int totalInitialInvaders = 55;
    private float stepTimer = 0f;
    private float currentStepInterval;
    private bool pendingDropAndReverse = false;
    private float shootTimer = 0f;
    private bool isPaused = false;

    private void Start()
    {
        currentStepInterval = initialStepInterval;
        shootTimer = Random.Range(minShootDelay, maxShootDelay);
    }

    public void SpawnFormation(int waveIndex = 1)
    {
        ClearFormation();
        transform.position = Vector3.zero;

        // Wave scaling: slightly lower start position and faster speed on higher waves
        float waveZOffset = Mathf.Min((waveIndex - 1) * 0.5f, 3.0f);
        Vector3 origin = startPosition + Vector3.back * waveZOffset;

        for (int r = 0; r < rows; r++)
        {
            InvaderType type;
            float rowY;
            if (r == 0)
            {
                type = InvaderType.Squid;
                rowY = 3.6f; // High in the sky
            }
            else if (r == 1 || r == 2)
            {
                type = InvaderType.Crab;
                rowY = 2.0f; // Mid altitude
            }
            else
            {
                type = InvaderType.Octopus;
                rowY = 0.45f; // Ground level
            }

            for (int c = 0; c < columns; c++)
            {
                Vector3 localPos = new Vector3(origin.x + c * spacingX, rowY, origin.z - r * spacingZ);
                GameObject invaderObj = CreateInvaderGameObject(type, r, c, localPos);
                Invader invader = invaderObj.GetComponent<Invader>();
                invader.Init(type, r, c);
                activeInvaders.Add(invader);
            }
        }

        totalInitialInvaders = activeInvaders.Count;
        currentDirection = 1f;
        pendingDropAndReverse = false;
        currentStepInterval = Mathf.Max(minStepInterval, initialStepInterval - (waveIndex - 1) * 0.1f);
        stepTimer = 0f;
        isPaused = false;
    }

    private GameObject CreateInvaderGameObject(InvaderType type, int r, int c, Vector3 pos)
    {
        GameObject prefab = null;
        Material mat = null;
        Color defaultColor = Color.white;

        switch (type)
        {
            case InvaderType.Squid:
                prefab = squidPrefab;
                mat = squidMaterial;
                defaultColor = new Color(0.9f, 0.2f, 0.4f); // Neon Magenta/Pink
                break;
            case InvaderType.Crab:
                prefab = crabPrefab;
                mat = crabMaterial;
                defaultColor = new Color(0.2f, 0.8f, 1.0f); // Cyan
                break;
            case InvaderType.Octopus:
                prefab = octopusPrefab;
                mat = octopusMaterial;
                defaultColor = new Color(0.3f, 1.0f, 0.3f); // Neon Green
                break;
        }

        GameObject obj;
        if (prefab != null)
        {
            obj = Instantiate(prefab, pos, Quaternion.identity, transform);
        }
        else
        {
            // Procedural stylized 3D invader model
            obj = ProceduralMeshBuilder.CreateInvaderMesh(type, defaultColor);
            obj.transform.position = pos;
            obj.transform.SetParent(transform, true);
        }

        obj.name = $"Invader_{type}_{r}_{c}";
        obj.tag = "Invader";

        Collider col = obj.GetComponent<Collider>();
        if (col == null)
        {
            BoxCollider box = obj.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(1.2f, 1.0f, 1.0f);
        }
        else
        {
            col.isTrigger = true;
        }

        if (obj.GetComponent<Invader>() == null)
        {
            obj.AddComponent<Invader>();
        }

        return obj;
    }

    public void ClearFormation()
    {
        for (int i = activeInvaders.Count - 1; i >= 0; i--)
        {
            if (activeInvaders[i] != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(activeInvaders[i].gameObject);
                }
                else
                {
                    DestroyImmediate(activeInvaders[i].gameObject);
                }
            }
        }
        activeInvaders.Clear();
    }

    private void Update()
    {
        if (isPaused || activeInvaders.Count == 0) return;

        // Step movement timer
        stepTimer += Time.deltaTime;
        if (stepTimer >= currentStepInterval)
        {
            stepTimer = 0f;
            ExecuteStep();
        }

        // Enemy shooting
        shootTimer -= Time.deltaTime;
        if (shootTimer <= 0f)
        {
            shootTimer = Random.Range(minShootDelay, maxShootDelay);
            AttemptEnemyShoot();
        }
    }

    private void ExecuteStep()
    {
        if (pendingDropAndReverse)
        {
            // Move down
            transform.position += Vector3.back * stepDistanceZ;
            currentDirection = -currentDirection;
            pendingDropAndReverse = false;
        }
        else
        {
            // Move horizontal
            transform.position += Vector3.right * (stepDistanceX * currentDirection);

            // Check if bounds exceeded
            CheckBounds();
        }

        // Check if lowest invader passed bottom limit
        CheckBottomLimit();

        // Audio beat
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayMarchBeat();
        }
    }

    private void CheckBounds()
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;

        for (int i = 0; i < activeInvaders.Count; i++)
        {
            if (activeInvaders[i] == null || !activeInvaders[i].isAlive) continue;
            float px = activeInvaders[i].transform.position.x;
            if (px < minX) minX = px;
            if (px > maxX) maxX = px;
        }

        if (currentDirection > 0 && maxX >= xBoundaryLimit)
        {
            pendingDropAndReverse = true;
        }
        else if (currentDirection < 0 && minX <= -xBoundaryLimit)
        {
            pendingDropAndReverse = true;
        }
    }

    private void CheckBottomLimit()
    {
        for (int i = 0; i < activeInvaders.Count; i++)
        {
            if (activeInvaders[i] == null || !activeInvaders[i].isAlive) continue;
            if (activeInvaders[i].transform.position.z <= bottomLossZ)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.OnInvadersReachedBottom();
                }
                break;
            }
        }
    }

    public void OnBoundaryReached(BoundaryType type)
    {
        if ((type == BoundaryType.Right && currentDirection > 0) ||
            (type == BoundaryType.Left && currentDirection < 0))
        {
            pendingDropAndReverse = true;
        }
    }

    private void AttemptEnemyShoot()
    {
        if (activeInvaders.Count == 0) return;

        // Find columns and bottom-most invader in each column
        Dictionary<int, Invader> bottomInvaders = new Dictionary<int, Invader>();
        for (int i = 0; i < activeInvaders.Count; i++)
        {
            Invader inv = activeInvaders[i];
            if (inv == null || !inv.isAlive) continue;

            if (!bottomInvaders.ContainsKey(inv.colIndex) || inv.rowIndex > bottomInvaders[inv.colIndex].rowIndex)
            {
                bottomInvaders[inv.colIndex] = inv;
            }
        }

        List<Invader> candidateShooters = new List<Invader>(bottomInvaders.Values);
        if (candidateShooters.Count > 0)
        {
            Invader shooter = candidateShooters[Random.Range(0, candidateShooters.Count)];
            if (shooter != null)
            {
                shooter.Shoot();
            }
        }
    }

    public void OnInvaderKilled(Invader invader)
    {
        activeInvaders.Remove(invader);

        // Recalculate step interval to speed up movement
        float remainingRatio = (float)activeInvaders.Count / totalInitialInvaders;
        currentStepInterval = Mathf.Lerp(minStepInterval, initialStepInterval, remainingRatio);

        // Update animation pulse
        float animSpeed = Mathf.Lerp(6.0f, 1.0f, remainingRatio);
        for (int i = 0; i < activeInvaders.Count; i++)
        {
            if (activeInvaders[i] != null)
            {
                InvaderAnimation anim = activeInvaders[i].GetComponent<InvaderAnimation>();
                if (anim != null) anim.SetSpeed(animSpeed);
            }
        }

        if (activeInvaders.Count == 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnWaveCleared();
            }
        }
    }

    public void SetPaused(bool paused)
    {
        isPaused = paused;
    }

    public int GetRemainingCount()
    {
        return activeInvaders.Count;
    }
}
