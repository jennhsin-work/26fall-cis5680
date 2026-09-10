using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD Elements")]
    public Text scoreText;
    public Text highScoreText;
    public Text waveText;
    public Text livesText;
    public Text viewModeText;
    public GameObject[] lifeIcons;

    [Header("Cockpit HUD Elements")]
    public Text cockpitTelemetryText;
    public Text warningText;
    public Text focusFeedbackText;

    [Header("Screen Panels")]
    public GameObject startPanel;
    public GameObject gameOverPanel;
    public GameObject victoryPanel;
    public Text gameOverScoreText;
    public Text victoryScoreText;

    private float warningTimer = 0f;
    private float focusFeedbackTimer = 0f;

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

        EnsureHUDOverlay();
    }

    private void Start()
    {
        EnsureHUDOverlay();
    }

    private void EnsureHUDOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

        if (cockpitTelemetryText == null)
        {
            GameObject telemObj = new GameObject("CockpitTelemetryText");
            telemObj.transform.SetParent(canvas.transform, false);
            cockpitTelemetryText = telemObj.AddComponent<Text>();
            cockpitTelemetryText.font = font;
            cockpitTelemetryText.fontSize = 18;
            cockpitTelemetryText.alignment = TextAnchor.LowerCenter;
            cockpitTelemetryText.horizontalOverflow = HorizontalWrapMode.Overflow;
            cockpitTelemetryText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rt = cockpitTelemetryText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 65f);
            rt.sizeDelta = new Vector2(900f, 80f);
        }

        if (warningText == null)
        {
            GameObject warnObj = new GameObject("WarningBannerText");
            warnObj.transform.SetParent(canvas.transform, false);
            warningText = warnObj.AddComponent<Text>();
            warningText.font = font;
            warningText.fontSize = 24;
            warningText.fontStyle = FontStyle.Bold;
            warningText.alignment = TextAnchor.MiddleCenter;
            warningText.horizontalOverflow = HorizontalWrapMode.Overflow;
            warningText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rt = warningText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.72f);
            rt.anchorMax = new Vector2(0.5f, 0.72f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(800f, 60f);
            warningText.gameObject.SetActive(false);
        }

        if (focusFeedbackText == null)
        {
            GameObject focusObj = new GameObject("FocusFeedbackText");
            focusObj.transform.SetParent(canvas.transform, false);
            focusFeedbackText = focusObj.AddComponent<Text>();
            focusFeedbackText.font = font;
            focusFeedbackText.fontSize = 20;
            focusFeedbackText.fontStyle = FontStyle.Bold;
            focusFeedbackText.alignment = TextAnchor.MiddleCenter;
            focusFeedbackText.horizontalOverflow = HorizontalWrapMode.Overflow;
            focusFeedbackText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform rt = focusFeedbackText.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.45f);
            rt.anchorMax = new Vector2(0.5f, 0.45f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(600f, 60f);
            focusFeedbackText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (viewModeText != null)
        {
            string viewName = "3D ARCADE";
            if (CameraController.Instance != null)
            {
                viewName = CameraController.Instance.currentViewMode == CameraViewMode.Arcade3D ? "3D ARCADE" :
                          (CameraController.Instance.currentViewMode == CameraViewMode.ActionFollow3D ? "ACTION FOLLOW" : "2D RETRO");
            }
            viewModeText.text = $"<color=#00FFFF><b>[{viewName}]</b></color>  <b>A/D / ←/→:</b> Move  |  <b>SPACE / L-CLICK:</b> Fire  |  <b>SHIFT / R-CLICK:</b> Shield  |  <b>C/V/TAB:</b> Switch View";
        }

        if (warningTimer > 0f)
        {
            warningTimer -= Time.deltaTime;
            if (warningTimer <= 0f && warningText != null)
            {
                warningText.gameObject.SetActive(false);
            }
        }

        if (focusFeedbackTimer > 0f)
        {
            focusFeedbackTimer -= Time.deltaTime;
            if (focusFeedbackTimer <= 0f && focusFeedbackText != null)
            {
                focusFeedbackText.gameObject.SetActive(false);
            }
        }
    }

    public void SetWarningMessage(string message, float duration = 1.5f, bool isCritical = false)
    {
        warningTimer = duration;
        if (warningText != null)
        {
            warningText.gameObject.SetActive(true);
            warningText.text = isCritical ? $"<color=#FF2244><b>>>> CRITICAL ALERT: {message} <<<</b></color>" : $"<color=#FFAA00><b>>>> ALERT: {message} <<<</b></color>";
        }
    }

    public void ShowFocusHitFeedback(bool isCritical, float distance, float focusDepth)
    {
        focusFeedbackTimer = 1.0f;
        if (focusFeedbackText != null)
        {
            focusFeedbackText.gameObject.SetActive(true);
            if (isCritical)
            {
                focusFeedbackText.text = $"<color=#00FF66><b>★ CRITICAL FOCUS CONVERGENCE! ★</b></color>\nTarget: {distance:F1}m | Full Damage Dealt!";
            }
            else
            {
                float delta = Mathf.Abs(focusDepth - distance);
                focusFeedbackText.text = $"<color=#FFAA00><b>OFF-FOCUS HIT ({distance:F1}m)</b></color>\nFocal Depth: {focusDepth:F1}m (Delta: {delta:F1}m)";
            }
        }
    }

    public void UpdateCockpitStatus(
        float energyRatio,
        float shieldRatio,
        float focalDepth,
        bool isTargetLocked,
        float targetDist,
        bool isTargetInFocus,
        string targetName,
        float nearestInvaderDist)
    {
        if (cockpitTelemetryText != null)
        {
            int energyBars = Mathf.RoundToInt(energyRatio * 10f);
            string energyBarStr = new string('=', Mathf.Clamp(energyBars, 0, 10)).PadRight(10, '-');
            string energyColor = energyRatio < 0.25f ? "#FF3333" : (energyRatio < 0.5f ? "#FFBB00" : "#00FFCC");

            int shieldBars = Mathf.RoundToInt(shieldRatio * 10f);
            string shieldBarStr = new string('=', Mathf.Clamp(shieldBars, 0, 10)).PadRight(10, '-');
            string shieldColor = shieldRatio < 0.3f ? "#FF3333" : "#3399FF";

            string lockInfo;
            if (isTargetLocked)
            {
                string focusStatus = isTargetInFocus ? "<color=#00FF66>[LOCKED - IN FOCUS]</color>" : "<color=#FF9900>[LOCKED - OFF FOCUS]</color>";
                lockInfo = $"TARGET: {targetName} @ {targetDist:F1}m  {focusStatus}";
            }
            else
            {
                lockInfo = "RADAR: SCANNING... [Presets: 1=6.5m, 2=13m, 3=19m | Scroll to Adjust]";
            }

            string threatInfo = nearestInvaderDist > 0.01f ? $"NEAREST THREAT: {nearestInvaderDist:F1}m" : "SECTOR CLEAR";

            cockpitTelemetryText.text = $"<color={shieldColor}><b>SHIELD:</b> [{shieldBarStr}] {Mathf.RoundToInt(shieldRatio * 100)}%</color>    |    <color={energyColor}><b>ENERGY:</b> [{energyBarStr}] {Mathf.RoundToInt(energyRatio * 100)}%</color>\n" +
                                        $"<color=#00FFFF><b>FOCUS DEPTH:</b> {focalDepth:F1}m</color>    |    <color=#FFFF66><b>{threatInfo}</b></color>\n" +
                                        $"<color=#FFFFFF>{lockInfo}</color>";
        }
    }

    public void UpdateScore(int score, int highScore)
    {
        if (scoreText != null) scoreText.text = $"SCORE\n{score:D4}";
        if (highScoreText != null) highScoreText.text = $"HI-SCORE\n{highScore:D4}";
    }

    public void UpdateWave(int wave)
    {
        if (waveText != null) waveText.text = $"WAVE\n{wave:D2}";
    }

    public void UpdateLives(int lives)
    {
        if (livesText != null) livesText.text = $"LIVES: {Mathf.Max(0, lives)}";

        if (lifeIcons != null && lifeIcons.Length > 0)
        {
            for (int i = 0; i < lifeIcons.Length; i++)
            {
                if (lifeIcons[i] != null)
                {
                    lifeIcons[i].SetActive(i < lives);
                }
            }
        }
    }

    public void ShowStartScreen(bool show)
    {
        if (startPanel != null) startPanel.SetActive(show);
    }

    public void ShowGameOverScreen(int finalScore, int highScore)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverScoreText != null)
            {
                gameOverScoreText.text = $"FINAL SCORE: {finalScore:D4}\nHIGH SCORE: {highScore:D4}";
            }
        }
    }

    public void HideGameOverScreen()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
    }

    public void ShowVictoryScreen(int currentScore)
    {
        if (victoryPanel != null)
        {
            victoryPanel.SetActive(true);
            if (victoryScoreText != null)
            {
                victoryScoreText.text = $"WAVE CLEARED!\nSCORE: {currentScore:D4}";
            }
        }
    }

    public void HideVictoryScreen()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
    }
}

