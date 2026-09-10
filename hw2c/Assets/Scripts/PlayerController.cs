using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Ship Movement")]
    public float moveSpeed = 11f;
    public float moveSpeedY = 9f;
    public float boundaryX = 10.5f;
    public float minBoundaryY = 0f;
    public float maxBoundaryY = 5.5f;
    public float defaultZ = -6.5f;

    [Header("Mounted Weapon Aiming")]
    public float aimSensitivityMouse = 1.0f;
    public float aimSensitivityKeyboard = 1.6f;
    public Vector2 reticleScreenPos = new Vector2(0.5f, 0.55f); // Normalized screen space (0 to 1)

    [Header("Adjustable Focus Depth")]
    public float focalDepth = 14.0f;
    public float minFocalDepth = 4.0f;
    public float maxFocalDepth = 24.0f;
    public float focusAdjustSpeed = 12.0f;
    public float focalTolerance = 2.5f;

    [Header("Shared Energy System")]
    public float maxEnergy = 100f;
    public float currentEnergy = 100f;
    public float baseEnergyRechargeRate = 18f;
    public float moveEnergyDrain = 9f;
    public float fireEnergyCost = 12f;
    public float activeShieldEnergyDrain = 26f;
    public float shieldRechargeEnergyDrain = 14f;

    [Header("Deflector Shield")]
    public bool isShieldActive = false;

    [Header("Shooting & Mounted Cannons")]
    public GameObject bulletPrefab;
    public float fireRateLimit = 0.16f;
    public AudioClip shootSound;

    [HideInInspector] public GameObject activeBullet;

    private float horizontalInput = 0f;
    private float verticalInput = 0f;
    private Vector2 keyboardAimInput = Vector2.zero;
    private float depthAdjustInput = 0f;
    private bool firePressed = false;
    private bool shieldHeld = false;
    private float lastFireTime = -10f;
    private bool canControl = true;
    private bool alternateTurret = false;

    // Targeting radar info
    private float currentLockedTargetDistance = -1f;
    private bool isTargetLocked = false;
    private bool isTargetInFocus = false;
    private string lockedTargetName = "";

    private CockpitVisuals cockpitVisuals;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);

        cockpitVisuals = GetComponent<CockpitVisuals>();
        if (cockpitVisuals == null)
        {
            cockpitVisuals = gameObject.AddComponent<CockpitVisuals>();
        }
    }

    private void Start()
    {
        currentEnergy = maxEnergy;
        reticleScreenPos = new Vector2(0.5f, 0.55f);
        Vector3 pos = transform.position;
        pos.z = defaultZ;
        if (pos.y < minBoundaryY || pos.y > maxBoundaryY) pos.y = minBoundaryY;
        transform.position = pos;
    }

    private void Update()
    {
        if (!canControl) return;

        ReadInput();
        HandleMovement();
        HandleAimingAndFocus();
        HandleEnergySystem();
        HandleShield();
        HandleShooting();
        UpdateTargetRadar();
        UpdateCockpitFeedback();
    }

    private float currentRoll = 0f;

    private void ReadInput()
    {
        horizontalInput = 0f;
        verticalInput = 0f;
        keyboardAimInput = Vector2.zero;
        depthAdjustInput = 0f;
        firePressed = false;
        shieldHeld = false;

        if (Keyboard.current != null)
        {
            // Horizontal Ship Movement: A / D or Left / Right Arrows
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontalInput -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontalInput += 1f;

            // Upward / Downward Ship Movement along Y axis: Up / Down Arrows or W / S Keys
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) verticalInput += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) verticalInput -= 1f;

            // Keyboard Aiming: I / J / K / L
            if (Keyboard.current.jKey.isPressed) keyboardAimInput.x -= 1f;
            if (Keyboard.current.lKey.isPressed) keyboardAimInput.x += 1f;
            if (Keyboard.current.iKey.isPressed) keyboardAimInput.y += 1f;
            if (Keyboard.current.kKey.isPressed) keyboardAimInput.y -= 1f;

            // Focus Depth Tuning: Q / E or R / F
            if (Keyboard.current.eKey.isPressed || Keyboard.current.rKey.isPressed) depthAdjustInput += 1f;
            if (Keyboard.current.qKey.isPressed || Keyboard.current.fKey.isPressed) depthAdjustInput -= 1f;

            // Focus Presets: 1 = Near (6.5m), 2 = Mid (13m), 3 = Far (19m)
            if (Keyboard.current.digit1Key.wasPressedThisFrame || Keyboard.current.numpad1Key.wasPressedThisFrame) SetFocusPreset(6.5f);
            if (Keyboard.current.digit2Key.wasPressedThisFrame || Keyboard.current.numpad2Key.wasPressedThisFrame) SetFocusPreset(13.0f);
            if (Keyboard.current.digit3Key.wasPressedThisFrame || Keyboard.current.numpad3Key.wasPressedThisFrame) SetFocusPreset(19.0f);

            // Fire: Space, Enter, or Left Click
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
            {
                firePressed = true;
            }

            // Shield: Left Shift, Right Shift, C, or X
            if (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed || Keyboard.current.cKey.isPressed || Keyboard.current.xKey.isPressed)
            {
                shieldHeld = true;
            }
        }

        // Gamepad Controls
        if (Gamepad.current != null)
        {
            float stickX = Gamepad.current.leftStick.x.ReadValue();
            float dpadX = Gamepad.current.dpad.x.ReadValue();
            if (Mathf.Abs(stickX) > 0.15f) horizontalInput += Mathf.Sign(stickX);
            else if (Mathf.Abs(dpadX) > 0.15f) horizontalInput += Mathf.Sign(dpadX);

            float stickY = Gamepad.current.leftStick.y.ReadValue();
            float dpadY = Gamepad.current.dpad.y.ReadValue();
            if (Mathf.Abs(stickY) > 0.15f) verticalInput += Mathf.Sign(stickY);
            else if (Mathf.Abs(dpadY) > 0.15f) verticalInput += Mathf.Sign(dpadY);

            if (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.rightTrigger.wasPressedThisFrame) firePressed = true;
            if (Gamepad.current.leftTrigger.isPressed || Gamepad.current.buttonEast.isPressed) shieldHeld = true;
        }

        // Mouse Controls
        if (Mouse.current != null)
        {
            // Mouse Delta for aiming
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            if (mouseDelta.sqrMagnitude > 0.01f)
            {
                reticleScreenPos.x += (mouseDelta.x / Screen.width) * aimSensitivityMouse * 1.5f;
                reticleScreenPos.y += (mouseDelta.y / Screen.height) * aimSensitivityMouse * 1.5f;
            }

            // Mouse Scroll Wheel for Focus Depth
            float scrollY = Mouse.current.scroll.y.ReadValue();
            if (Mathf.Abs(scrollY) > 0.1f)
            {
                depthAdjustInput += Mathf.Sign(scrollY);
            }

            // Left Click Fire
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                firePressed = true;
            }

            // Right Click Shield
            if (Mouse.current.rightButton.isPressed)
            {
                shieldHeld = true;
            }
        }

        // Clamp screen space reticle (0.1 to 0.9)
        reticleScreenPos.x = Mathf.Clamp(reticleScreenPos.x, 0.1f, 0.9f);
        reticleScreenPos.y = Mathf.Clamp(reticleScreenPos.y, 0.2f, 0.85f);
    }

    private void SetFocusPreset(float depth)
    {
        focalDepth = Mathf.Clamp(depth, minFocalDepth, maxFocalDepth);
        if (AudioManager.Instance != null && AudioManager.Instance.focusChangeClip != null)
        {
            AudioManager.Instance.PlayEffect(AudioManager.Instance.focusChangeClip, 0.6f);
        }
    }

    private void HandleMovement()
    {
        Vector3 pos = transform.position;
        float speedMult = (currentEnergy > 5f) ? 1.0f : 0.85f;
        bool isMoving = false;

        if (Mathf.Abs(horizontalInput) > 0.01f)
        {
            pos.x += Mathf.Clamp(horizontalInput, -1f, 1f) * moveSpeed * speedMult * Time.deltaTime;
            pos.x = Mathf.Clamp(pos.x, -boundaryX, boundaryX);
            isMoving = true;
        }

        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            pos.y += Mathf.Clamp(verticalInput, -1f, 1f) * moveSpeedY * speedMult * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minBoundaryY, maxBoundaryY);
            isMoving = true;
        }

        if (isMoving)
        {
            // Movement consumes minor energy
            currentEnergy = Mathf.Max(0f, currentEnergy - moveEnergyDrain * Time.deltaTime);
        }

        pos.z = defaultZ;
        transform.position = pos;

        // Smooth bank roll on horizontal steering (no forward/backward pitching)
        float targetRoll = -Mathf.Clamp(horizontalInput, -1f, 1f) * 14.0f;
        currentRoll = Mathf.Lerp(currentRoll, targetRoll, Time.deltaTime * 12.0f);
        transform.rotation = Quaternion.Euler(0f, 0f, currentRoll);

        // Update pilot character and cockpit steering animation
        if (cockpitVisuals != null)
        {
            cockpitVisuals.UpdateSteering(horizontalInput, verticalInput, isMoving);
        }
    }

    private void HandleAimingAndFocus()
    {
        // Keyboard reticle movement
        if (keyboardAimInput.sqrMagnitude > 0.01f)
        {
            reticleScreenPos += keyboardAimInput * aimSensitivityKeyboard * Time.deltaTime;
            reticleScreenPos.x = Mathf.Clamp(reticleScreenPos.x, 0.1f, 0.9f);
            reticleScreenPos.y = Mathf.Clamp(reticleScreenPos.y, 0.2f, 0.85f);
        }

        // Focus depth adjustment
        if (Mathf.Abs(depthAdjustInput) > 0.01f)
        {
            float prevDepth = focalDepth;
            focalDepth += depthAdjustInput * focusAdjustSpeed * Time.deltaTime;
            focalDepth = Mathf.Clamp(focalDepth, minFocalDepth, maxFocalDepth);

            if (Mathf.Abs(focalDepth - prevDepth) > 0.25f && AudioManager.Instance != null && AudioManager.Instance.focusChangeClip != null)
            {
                AudioManager.Instance.PlayEffect(AudioManager.Instance.focusChangeClip, 0.4f);
            }
        }

        // Send aim sway to camera controller
        if (CameraController.Instance != null)
        {
            Vector2 normAim = new Vector2(reticleScreenPos.x * 2f - 1f, reticleScreenPos.y * 2f - 1f);
            CameraController.Instance.SetAimSway(normAim);
        }
    }

    public Vector3 GetAimWorldPoint()
    {
        // Target aim along playfield
        float aimOffsetX = (reticleScreenPos.x - 0.5f) * 14.0f;
        float aimTargetY = transform.position.y + 0.3f;
        float aimTargetZ = transform.position.z + focalDepth;
        return new Vector3(transform.position.x + aimOffsetX, aimTargetY, aimTargetZ);
    }

    private void HandleEnergySystem()
    {
        // Shield HP Regeneration consumes energy if shield is below max
        PlayerHealth ph = GetComponent<PlayerHealth>();
        if (ph != null && ph.currentShield < ph.maxShield && currentEnergy > 5f)
        {
            float regenAmount = 14f * Time.deltaTime;
            ph.HealShield(regenAmount);
            currentEnergy = Mathf.Max(0f, currentEnergy - shieldRechargeEnergyDrain * Time.deltaTime);
        }

        // Natural energy recharge if not heavily draining
        if (!isShieldActive && Mathf.Abs(horizontalInput) < 0.01f && Mathf.Abs(verticalInput) < 0.01f)
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + baseEnergyRechargeRate * Time.deltaTime);
        }
        else
        {
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + (baseEnergyRechargeRate * 0.4f) * Time.deltaTime);
        }
    }

    private void HandleShield()
    {
        if (shieldHeld && currentEnergy > 8f)
        {
            isShieldActive = true;
            currentEnergy = Mathf.Max(0f, currentEnergy - activeShieldEnergyDrain * Time.deltaTime);
            if (cockpitVisuals != null) cockpitVisuals.SetShieldVisual(true);
        }
        else
        {
            isShieldActive = false;
            if (cockpitVisuals != null) cockpitVisuals.SetShieldVisual(false);
        }
    }

    public bool IsShieldActive()
    {
        return isShieldActive;
    }

    private void HandleShooting()
    {
        Vector3 aimPoint = GetAimWorldPoint();
        if (cockpitVisuals != null)
        {
            cockpitVisuals.UpdateTurretAim(aimPoint);
        }

        if (firePressed && Time.time - lastFireTime >= fireRateLimit)
        {
            if (currentEnergy >= fireEnergyCost)
            {
                currentEnergy -= fireEnergyCost;
                FireMountedLaser(aimPoint);
            }
            else
            {
                // Low energy dry fire feedback
                if (AudioManager.Instance != null && AudioManager.Instance.lowEnergyClip != null)
                {
                    AudioManager.Instance.PlayEffect(AudioManager.Instance.lowEnergyClip, 0.7f);
                }
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SetWarningMessage("WEAPON ENERGY DEPLETED!", 1.0f, true);
                }
            }
        }
    }

    public void FireMountedLaser(Vector3 aimWorldPoint)
    {
        lastFireTime = Time.time;
        alternateTurret = !alternateTurret;

        // Choose left or right muzzle spawn point
        Transform muzzle = null;
        if (cockpitVisuals != null)
        {
            muzzle = alternateTurret ? cockpitVisuals.leftMuzzlePoint : cockpitVisuals.rightMuzzlePoint;
        }

        Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position + Vector3.forward * 0.9f + (alternateTurret ? Vector3.left : Vector3.right) * 0.45f;
        Vector3 fireDir = (aimWorldPoint - spawnPos).normalized;

        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bullet.name = "PlayerLaser";
        bullet.tag = "PlayerBullet";
        bullet.transform.position = spawnPos;
        bullet.transform.localScale = new Vector3(0.16f, 0.55f, 0.16f);
        bullet.transform.rotation = Quaternion.LookRotation(fireDir) * Quaternion.Euler(90f, 0f, 0f);

        MeshRenderer mr = bullet.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.material = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.1f, 1f, 0.7f), 2.5f);
        }

        Collider col = bullet.GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        // Point light for bullet
        GameObject lightObj = new GameObject("LaserLight");
        lightObj.transform.SetParent(bullet.transform, false);
        Light l = lightObj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(0.2f, 1f, 0.8f);
        l.range = 3.0f;
        l.intensity = 2.5f;

        PlayerBullet pb = bullet.AddComponent<PlayerBullet>();
        pb.owner = gameObject;
        pb.InitFocus(transform.position, fireDir, focalDepth);
        pb.focalTolerance = focalTolerance;
        activeBullet = bullet;

        // Camera recoil
        if (CameraController.Instance != null)
        {
            CameraController.Instance.AddRecoil(1.0f);
        }

        if (AudioManager.Instance != null)
        {
            AudioClip clip = shootSound != null ? shootSound : AudioManager.Instance.playerShotClip;
            AudioManager.Instance.PlayEffect(clip, 0.75f);
        }
    }

    private void UpdateTargetRadar()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ViewportPointToRay(new Vector3(reticleScreenPos.x, reticleScreenPos.y, 0f));
        RaycastHit hit;

        isTargetLocked = false;
        currentLockedTargetDistance = -1f;
        lockedTargetName = "";

        if (Physics.SphereCast(ray, 0.85f, out hit, 35f, ~0, QueryTriggerInteraction.Collide))
        {
            Invader inv = hit.collider.GetComponent<Invader>() ?? hit.collider.GetComponentInParent<Invader>();
            if (inv != null && inv.isAlive)
            {
                isTargetLocked = true;
                currentLockedTargetDistance = Vector3.Distance(transform.position, inv.transform.position);
                lockedTargetName = inv.invaderType.ToString().ToUpper();
                float diff = Mathf.Abs(focalDepth - currentLockedTargetDistance);
                isTargetInFocus = (diff <= focalTolerance);
                return;
            }

            UFOController ufo = hit.collider.GetComponent<UFOController>() ?? hit.collider.GetComponentInParent<UFOController>();
            if (ufo != null)
            {
                isTargetLocked = true;
                currentLockedTargetDistance = Vector3.Distance(transform.position, ufo.transform.position);
                lockedTargetName = "COMMAND UFO";
                float diff = Mathf.Abs(focalDepth - currentLockedTargetDistance);
                isTargetInFocus = (diff <= focalTolerance);
                return;
            }
        }
    }

    private void UpdateCockpitFeedback()
    {
        // Find nearest invader distance
        float nearestDist = 999f;
        Invader[] invaders = FindObjectsByType<Invader>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        bool hasDepthCharger = false;
        for (int i = 0; i < invaders.Length; i++)
        {
            if (invaders[i] == null || !invaders[i].isAlive) continue;
            float d = Vector3.Distance(transform.position, invaders[i].transform.position);
            if (d < nearestDist) nearestDist = d;
            if (invaders[i].currentState == InvaderState.DepthCharging) hasDepthCharger = true;
        }

        // Check for incoming bullets close to ship
        bool incomingBulletNear = false;
        EnemyBullet[] bullets = FindObjectsByType<EnemyBullet>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < bullets.Length; i++)
        {
            if (bullets[i] == null) continue;
            float dist = Vector3.Distance(transform.position, bullets[i].transform.position);
            if (dist < 4.5f)
            {
                incomingBulletNear = true;
                break;
            }
        }

        bool lowEnergy = (currentEnergy < 25f);
        bool isWarningActive = lowEnergy || incomingBulletNear || hasDepthCharger;
        bool isCritical = (currentEnergy < 15f) || (incomingBulletNear);

        if (cockpitVisuals != null)
        {
            cockpitVisuals.SetWarningLights(isWarningActive, isCritical);
        }

        if (UIManager.Instance != null)
        {
            PlayerHealth ph = GetComponent<PlayerHealth>();
            float shieldRatio = ph != null ? (ph.currentShield / ph.maxShield) : 1f;
            float energyRatio = currentEnergy / maxEnergy;

            UIManager.Instance.UpdateCockpitStatus(
                energyRatio,
                shieldRatio,
                focalDepth,
                isTargetLocked,
                currentLockedTargetDistance,
                isTargetInFocus,
                lockedTargetName,
                nearestDist < 900f ? nearestDist : 0f
            );
        }
    }

    public void SetControlEnabled(bool enabled)
    {
        canControl = enabled;
        if (!enabled)
        {
            horizontalInput = 0f;
            verticalInput = 0f;
            isShieldActive = false;
            if (cockpitVisuals != null) cockpitVisuals.SetShieldVisual(false);
        }
    }

    public void ResetPosition(Vector3 defaultPos)
    {
        transform.position = defaultPos;
        currentEnergy = maxEnergy;
        isShieldActive = false;
        if (cockpitVisuals != null) cockpitVisuals.SetShieldVisual(false);
        if (activeBullet != null)
        {
            Destroy(activeBullet);
            activeBullet = null;
        }
    }
}


