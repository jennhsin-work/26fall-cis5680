using UnityEngine;
using UnityEngine.InputSystem;

public enum CameraViewMode
{
    Arcade3D,       // Classic angled 3D arcade view (default)
    ActionFollow3D, // Over-the-shoulder follow view
    Retro2D         // Top-down retro arcade view
}

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }

    [Header("Target & View Modes")]
    public Transform targetPlayer;
    public CameraViewMode currentViewMode = CameraViewMode.Arcade3D;

    [Header("Arcade 3D View (Default)")]
    public Vector3 arcade3DPosition = new Vector3(0f, 17.5f, -13.0f);
    public Vector3 arcade3DRotation = new Vector3(52.0f, 0f, 0f);
    public float arcade3DFOV = 52f;

    [Header("Action Follow 3D View")]
    public Vector3 followOffset = new Vector3(0f, 4.2f, -5.5f);
    public Vector3 followRotation = new Vector3(24.0f, 0f, 0f);
    public float followFOV = 62f;

    [Header("Retro 2D View")]
    public Vector3 retro2DPosition = new Vector3(0f, 22.0f, 2.0f);
    public Vector3 retro2DRotation = new Vector3(90.0f, 0f, 0f);
    public float retro2DFOV = 50f;

    [Header("Smoothing")]
    public float positionSmoothSpeed = 12f;
    public float rotationSmoothSpeed = 10f;

    [Header("Camera Recoil")]
    public float recoilPitch = 1.2f;
    public float recoilKickback = 0.12f;
    public float recoilRecoverySpeed = 10f;

    [Header("Impact Trauma / Shake")]
    public float maxShakeTranslation = 0.4f;
    public float maxShakeRotation = 3.5f;
    public float traumaDecaySpeed = 2.2f;

    private float currentTrauma = 0f;
    private Vector3 currentRecoilPos = Vector3.zero;
    private Vector3 currentRecoilRot = Vector3.zero;
    private Vector2 currentAimNormalized = Vector2.zero;
    private float shakeSeed;
    private Camera cam;

    public static bool Is3D => Instance != null && Instance.currentViewMode != CameraViewMode.Retro2D;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        shakeSeed = Random.value * 100f;
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = arcade3DFOV;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 200f;
        }
    }

    private void Start()
    {
        FindPlayerTarget();
        SnapToCurrentView();
    }

    private void Update()
    {
        // View mode toggle with C, V, or Tab
        if (Keyboard.current != null)
        {
            if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.vKey.wasPressedThisFrame || Keyboard.current.tabKey.wasPressedThisFrame)
            {
                CycleViewMode();
            }
        }
    }

    private void LateUpdate()
    {
        if (targetPlayer == null)
        {
            FindPlayerTarget();
        }

        Vector3 targetPos;
        Quaternion targetRot;
        float targetFOV;

        switch (currentViewMode)
        {
            case CameraViewMode.ActionFollow3D:
                Vector3 playerPos = targetPlayer != null ? targetPlayer.position : new Vector3(0f, 0f, -6.5f);
                targetPos = new Vector3(playerPos.x * 0.75f, playerPos.y + followOffset.y, playerPos.z + followOffset.z);
                float swayYaw = currentAimNormalized.x * 4.0f;
                targetRot = Quaternion.Euler(followRotation.x, swayYaw, 0f);
                targetFOV = followFOV;
                break;

            case CameraViewMode.Retro2D:
                targetPos = retro2DPosition;
                targetRot = Quaternion.Euler(retro2DRotation);
                targetFOV = retro2DFOV;
                break;

            case CameraViewMode.Arcade3D:
            default:
                float slightTrackX = targetPlayer != null ? targetPlayer.position.x * 0.18f : 0f;
                targetPos = new Vector3(slightTrackX, arcade3DPosition.y, arcade3DPosition.z);
                targetRot = Quaternion.Euler(arcade3DRotation);
                targetFOV = arcade3DFOV;
                break;
        }

        // Smooth Recoil Decay
        currentRecoilPos = Vector3.Lerp(currentRecoilPos, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);
        currentRecoilRot = Vector3.Lerp(currentRecoilRot, Vector3.zero, Time.deltaTime * recoilRecoverySpeed);

        // Trauma / Shake
        Vector3 shakeOffset = Vector3.zero;
        Vector3 shakeRot = Vector3.zero;
        if (currentTrauma > 0.001f)
        {
            float shake = currentTrauma * currentTrauma;
            float t = Time.time * 26f + shakeSeed;

            float shakeX = (Mathf.PerlinNoise(t, 0f) * 2f - 1f) * maxShakeTranslation * shake;
            float shakeY = (Mathf.PerlinNoise(0f, t) * 2f - 1f) * maxShakeTranslation * shake;
            float shakeZ = (Mathf.PerlinNoise(t, t) * 2f - 1f) * (maxShakeTranslation * 0.5f) * shake;
            shakeOffset = new Vector3(shakeX, shakeY, shakeZ);

            float rotX = (Mathf.PerlinNoise(t + 10f, 0f) * 2f - 1f) * maxShakeRotation * shake;
            float rotY = (Mathf.PerlinNoise(0f, t + 10f) * 2f - 1f) * maxShakeRotation * shake;
            float rotZ = (Mathf.PerlinNoise(t + 20f, t + 20f) * 2f - 1f) * (maxShakeRotation * 1.5f) * shake;
            shakeRot = new Vector3(rotX, rotY, rotZ);

            currentTrauma = Mathf.Max(0f, currentTrauma - Time.deltaTime * traumaDecaySpeed);
        }

        // Apply smooth Position & Rotation
        Vector3 finalPos = targetPos + currentRecoilPos + shakeOffset;
        transform.position = Vector3.Lerp(transform.position, finalPos, Time.deltaTime * positionSmoothSpeed);

        Quaternion recoilRotation = Quaternion.Euler(-currentRecoilRot.x, 0f, 0f);
        Quaternion shakeRotation = Quaternion.Euler(shakeRot);
        Quaternion finalRotation = targetRot * recoilRotation * shakeRotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation, Time.deltaTime * rotationSmoothSpeed);

        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 8f);
        }
    }

    public void CycleViewMode()
    {
        if (currentViewMode == CameraViewMode.Arcade3D)
        {
            SetViewMode(CameraViewMode.ActionFollow3D);
        }
        else if (currentViewMode == CameraViewMode.ActionFollow3D)
        {
            SetViewMode(CameraViewMode.Retro2D);
        }
        else
        {
            SetViewMode(CameraViewMode.Arcade3D);
        }
    }

    public void SetViewMode(CameraViewMode mode)
    {
        currentViewMode = mode;
        if (UIManager.Instance != null)
        {
            string modeName = currentViewMode == CameraViewMode.Arcade3D ? "3D ARCADE" :
                             (currentViewMode == CameraViewMode.ActionFollow3D ? "ACTION FOLLOW" : "2D RETRO");
            UIManager.Instance.SetWarningMessage($"CAMERA VIEW: {modeName}", 1.0f, false);
        }
    }

    public void SnapToCurrentView()
    {
        if (targetPlayer == null) FindPlayerTarget();

        switch (currentViewMode)
        {
            case CameraViewMode.ActionFollow3D:
                Vector3 playerPos = targetPlayer != null ? targetPlayer.position : new Vector3(0f, 0f, -6.5f);
                transform.position = new Vector3(playerPos.x * 0.75f, playerPos.y + followOffset.y, playerPos.z + followOffset.z);
                transform.rotation = Quaternion.Euler(followRotation);
                if (cam != null) cam.fieldOfView = followFOV;
                break;

            case CameraViewMode.Retro2D:
                transform.position = retro2DPosition;
                transform.rotation = Quaternion.Euler(retro2DRotation);
                if (cam != null) cam.fieldOfView = retro2DFOV;
                break;

            case CameraViewMode.Arcade3D:
            default:
                transform.position = arcade3DPosition;
                transform.rotation = Quaternion.Euler(arcade3DRotation);
                if (cam != null) cam.fieldOfView = arcade3DFOV;
                break;
        }
    }

    private void FindPlayerTarget()
    {
        var pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) targetPlayer = pc.transform;
    }

    public void AddRecoil(float multiplier = 1f)
    {
        currentRecoilPos += new Vector3(0f, 0f, -recoilKickback * multiplier);
        currentRecoilRot += new Vector3(recoilPitch * multiplier, 0f, 0f);
    }

    public void AddShake(float trauma)
    {
        currentTrauma = Mathf.Clamp01(currentTrauma + trauma);
    }

    public void SetAimSway(Vector2 normalizedAim)
    {
        currentAimNormalized = normalizedAim;
    }
}


