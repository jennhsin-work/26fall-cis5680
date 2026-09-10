using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public enum GameState
{
    StartMenu,
    Playing,
    WaveTransition,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Configuration")]
    public int startingLives = 3;
    public float waveDelay = 2.0f;

    [Header("References")]
    public FormationController formationController;
    public PlayerController playerController;
    public PlayerHealth playerHealth;
    public UFOController ufoController;
    public Transform bunkerParent;

    [Header("Runtime State")]
    public GameState currentState = GameState.StartMenu;
    public int score = 0;
    public int highScore = 0;
    public int currentWave = 1;

    private const string HighScoreKey = "SpaceInvaders_HighScore";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
    }

    private void Start()
    {
        if (formationController == null) formationController = FindFirstObjectByType<FormationController>();
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (ufoController == null) ufoController = FindFirstObjectByType<UFOController>();

        ShowStartMenu();
    }

    private void Update()
    {
        if (currentState == GameState.StartMenu)
        {
            if (WasAnyKeyPressed())
            {
                StartNewGame();
            }
        }
        else if (currentState == GameState.GameOver)
        {
            if (WasAnyKeyPressed())
            {
                StartNewGame();
            }
        }
    }

    private bool WasAnyKeyPressed()
    {
        if (Keyboard.current != null && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.anyKey.wasPressedThisFrame))
        {
            return true;
        }
        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
        {
            return true;
        }
        if (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame))
        {
            return true;
        }
        return false;
    }

    public bool IsPlaying()
    {
        return currentState == GameState.Playing;
    }

    public void ShowStartMenu()
    {
        currentState = GameState.StartMenu;
        if (playerController != null) playerController.SetControlEnabled(false);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(score, highScore);
            UIManager.Instance.ShowStartScreen(true);
            UIManager.Instance.HideGameOverScreen();
            UIManager.Instance.HideVictoryScreen();
        }
    }

    public void StartNewGame()
    {
        score = 0;
        currentWave = 1;
        currentState = GameState.Playing;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowStartScreen(false);
            UIManager.Instance.HideGameOverScreen();
            UIManager.Instance.HideVictoryScreen();
            UIManager.Instance.UpdateScore(score, highScore);
            UIManager.Instance.UpdateWave(currentWave);
            UIManager.Instance.UpdateLives(startingLives);
        }

        if (playerHealth != null)
        {
            playerHealth.Init(startingLives);
        }
        if (playerController != null)
        {
            playerController.SetControlEnabled(true);
        }

        RebuildBunkers();

        if (formationController != null)
        {
            formationController.SpawnFormation(currentWave);
        }
    }

    public void AddScore(int points)
    {
        score += points;
        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateScore(score, highScore);
        }
    }

    public void OnPlayerHit(int remainingLives)
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateLives(remainingLives);
        }
    }

    public void OnWaveCleared()
    {
        if (currentState != GameState.Playing) return;
        StartCoroutine(WaveClearRoutine());
    }

    private IEnumerator WaveClearRoutine()
    {
        currentState = GameState.WaveTransition;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect(AudioManager.Instance.levelWinClip);
        }

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictoryScreen(score);
        }

        // Clear remaining bullets
        ClearProjectiles();

        yield return new WaitForSeconds(waveDelay);

        currentWave++;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideVictoryScreen();
            UIManager.Instance.UpdateWave(currentWave);
        }

        currentState = GameState.Playing;
        if (formationController != null)
        {
            formationController.SpawnFormation(currentWave);
        }
    }

    public void OnInvadersReachedBottom()
    {
        if (currentState != GameState.Playing) return;
        OnGameOver();
    }

    public void OnGameOver()
    {
        if (currentState == GameState.GameOver) return;
        currentState = GameState.GameOver;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayEffect(AudioManager.Instance.gameOverClip);
        }

        if (playerController != null)
        {
            playerController.SetControlEnabled(false);
        }

        if (formationController != null)
        {
            formationController.SetPaused(true);
        }

        ClearProjectiles();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverScreen(score, highScore);
        }
    }

    private void ClearProjectiles()
    {
        GameObject[] playerBullets = GameObject.FindGameObjectsWithTag("PlayerBullet");
        foreach (var b in playerBullets) Destroy(b);

        GameObject[] enemyBullets = GameObject.FindGameObjectsWithTag("EnemyBullet");
        foreach (var b in enemyBullets) Destroy(b);
    }

    public void RebuildBunkers()
    {
        if (bunkerParent != null)
        {
            // Re-instantiate bunker cells if destroyed
            for (int i = bunkerParent.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(bunkerParent.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(bunkerParent.GetChild(i).gameObject);
                }
            }

            BunkerBuilder.CreateBunkers(bunkerParent);
        }
    }
}
