using UnityEngine;

public class CockpitVisuals : MonoBehaviour
{
    public static CockpitVisuals Instance { get; private set; }

    [Header("Articulated Turrets")]
    public Transform leftTurret;
    public Transform rightTurret;
    public Transform leftMuzzlePoint;
    public Transform rightMuzzlePoint;

    [Header("Warning Lights")]
    public MeshRenderer leftWarningBeacon;
    public MeshRenderer rightWarningBeacon;

    [Header("Deflector Shield Visual")]
    public GameObject shieldDome;
    private MeshRenderer shieldDomeRenderer;

    [Header("Thruster Flames")]
    public MeshRenderer leftThrusterFlame;
    public MeshRenderer rightThrusterFlame;

    private Material warningMatOn;
    private Material warningMatOff;
    private Material warningAmberOn;
    private Material thrusterGlowMat;
    private bool warningFlashState = false;
    private float flashTimer = 0f;
    private float steerAngle = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        BuildCockpitInterior();
    }

    private void Start()
    {
        if (shieldDome != null)
        {
            shieldDomeRenderer = shieldDome.GetComponent<MeshRenderer>();
        }
    }

    public void BuildCockpitInterior()
    {
        // Clean existing children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying) Destroy(transform.GetChild(i).gameObject);
            else DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // Palette materials
        Material hullMat = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.12f, 0.18f, 0.28f), 0.35f, 0.6f, 0.3f);
        Material hullAccentMat = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.18f, 0.32f, 0.55f), 0.5f, 0.7f, 0.4f);
        Material frameMat = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.08f, 0.12f, 0.18f), 0.2f, 0.7f, 0.5f);
        Material canopyGlowMat = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.1f, 0.95f, 1.0f), 2.5f, 0.9f, 0.1f);
        Material glowTrimCyan = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.15f, 1.0f, 0.85f), 2.2f, 0.85f, 0.1f);
        Material gunMat = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.35f, 0.40f, 0.48f), 0.45f, 0.8f, 0.7f);
        Material gunGlowMat = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.2f, 1.0f, 0.7f), 2.8f, 0.95f, 0.1f);

        thrusterGlowMat = ProceduralMeshBuilder.GetGlowMaterial(new Color(1.0f, 0.5f, 0.08f), 3.2f, 0.3f, 0.0f);
        warningMatOn = ProceduralMeshBuilder.GetGlowMaterial(new Color(1f, 0.15f, 0.15f), 3.0f, 0.8f, 0.1f);
        warningMatOff = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.25f, 0.03f, 0.03f), 0.1f, 0.3f, 0.1f);
        warningAmberOn = ProceduralMeshBuilder.GetGlowMaterial(new Color(1f, 0.65f, 0.08f), 2.6f, 0.8f, 0.1f);

        GameObject shipRoot = new GameObject("SpaceshipCockpitRoot");
        shipRoot.transform.SetParent(transform, false);
        shipRoot.transform.localPosition = Vector3.zero;

        // ==========================================
        // 1. MAIN FUSELAGE / HULL
        // ==========================================
        GameObject fuselage = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fuselage.name = "Fuselage";
        fuselage.transform.SetParent(shipRoot.transform, false);
        fuselage.transform.localPosition = new Vector3(0f, 0.22f, 0.1f);
        fuselage.transform.localScale = new Vector3(1.6f, 0.35f, 1.8f);
        StripCollider(fuselage);
        fuselage.GetComponent<MeshRenderer>().sharedMaterial = hullMat;

        // Sleek Canopy Dome
        GameObject canopy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        canopy.name = "CockpitCanopy";
        canopy.transform.SetParent(shipRoot.transform, false);
        canopy.transform.localPosition = new Vector3(0f, 0.38f, 0.15f);
        canopy.transform.localScale = new Vector3(0.95f, 0.38f, 1.2f);
        StripCollider(canopy);
        canopy.GetComponent<MeshRenderer>().sharedMaterial = canopyGlowMat;

        // Nose Cone
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "NoseCone";
        nose.transform.SetParent(shipRoot.transform, false);
        nose.transform.localPosition = new Vector3(0f, 0.25f, 1.15f);
        nose.transform.localRotation = Quaternion.Euler(16f, 0f, 0f);
        nose.transform.localScale = new Vector3(1.1f, 0.25f, 0.75f);
        StripCollider(nose);
        nose.GetComponent<MeshRenderer>().sharedMaterial = hullAccentMat;

        // ==========================================
        // 2. SWEPT WINGS WITH GLOW EDGES
        // ==========================================
        // Left Wing
        GameObject lWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lWing.name = "LeftWing";
        lWing.transform.SetParent(shipRoot.transform, false);
        lWing.transform.localPosition = new Vector3(-1.35f, 0.2f, -0.05f);
        lWing.transform.localRotation = Quaternion.Euler(0f, 12f, 6f);
        lWing.transform.localScale = new Vector3(1.2f, 0.12f, 1.4f);
        StripCollider(lWing);
        lWing.GetComponent<MeshRenderer>().sharedMaterial = hullAccentMat;

        GameObject lWingGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lWingGlow.name = "LeftWingGlow";
        lWingGlow.transform.SetParent(lWing.transform, false);
        lWingGlow.transform.localPosition = new Vector3(-0.46f, 0.05f, 0f);
        lWingGlow.transform.localScale = new Vector3(0.08f, 0.3f, 1.0f);
        StripCollider(lWingGlow);
        lWingGlow.GetComponent<MeshRenderer>().sharedMaterial = glowTrimCyan;

        // Right Wing
        GameObject rWing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rWing.name = "RightWing";
        rWing.transform.SetParent(shipRoot.transform, false);
        rWing.transform.localPosition = new Vector3(1.35f, 0.2f, -0.05f);
        rWing.transform.localRotation = Quaternion.Euler(0f, -12f, -6f);
        rWing.transform.localScale = new Vector3(1.2f, 0.12f, 1.4f);
        StripCollider(rWing);
        rWing.GetComponent<MeshRenderer>().sharedMaterial = hullAccentMat;

        GameObject rWingGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rWingGlow.name = "RightWingGlow";
        rWingGlow.transform.SetParent(rWing.transform, false);
        rWingGlow.transform.localPosition = new Vector3(0.46f, 0.05f, 0f);
        rWingGlow.transform.localScale = new Vector3(0.08f, 0.3f, 1.0f);
        StripCollider(rWingGlow);
        rWingGlow.GetComponent<MeshRenderer>().sharedMaterial = glowTrimCyan;

        // ==========================================
        // 3. REAR DUAL THRUSTERS & FLAMES
        // ==========================================
        GameObject lThruster = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lThruster.name = "LeftThruster";
        lThruster.transform.SetParent(shipRoot.transform, false);
        lThruster.transform.localPosition = new Vector3(-0.65f, 0.25f, -0.85f);
        lThruster.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        lThruster.transform.localScale = new Vector3(0.32f, 0.35f, 0.32f);
        StripCollider(lThruster);
        lThruster.GetComponent<MeshRenderer>().sharedMaterial = frameMat;

        GameObject lFlame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        lFlame.name = "LeftThrusterFlame";
        lFlame.transform.SetParent(lThruster.transform, false);
        lFlame.transform.localPosition = new Vector3(0f, -0.85f, 0f);
        lFlame.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
        StripCollider(lFlame);
        leftThrusterFlame = lFlame.GetComponent<MeshRenderer>();
        leftThrusterFlame.sharedMaterial = thrusterGlowMat;

        GameObject rThruster = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rThruster.name = "RightThruster";
        rThruster.transform.SetParent(shipRoot.transform, false);
        rThruster.transform.localPosition = new Vector3(0.65f, 0.25f, -0.85f);
        rThruster.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        rThruster.transform.localScale = new Vector3(0.32f, 0.35f, 0.32f);
        StripCollider(rThruster);
        rThruster.GetComponent<MeshRenderer>().sharedMaterial = frameMat;

        GameObject rFlame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rFlame.name = "RightThrusterFlame";
        rFlame.transform.SetParent(rThruster.transform, false);
        rFlame.transform.localPosition = new Vector3(0f, -0.85f, 0f);
        rFlame.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
        StripCollider(rFlame);
        rightThrusterFlame = rFlame.GetComponent<MeshRenderer>();
        rightThrusterFlame.sharedMaterial = thrusterGlowMat;

        // ==========================================
        // 4. DUAL HEAVY PLASMA CANNONS
        // ==========================================
        leftTurret = CreateTurret("LeftTurret", new Vector3(-0.85f, 0.30f, 0.8f), gunMat, gunGlowMat, out leftMuzzlePoint);
        leftTurret.SetParent(shipRoot.transform, false);

        rightTurret = CreateTurret("RightTurret", new Vector3(0.85f, 0.30f, 0.8f), gunMat, gunGlowMat, out rightMuzzlePoint);
        rightTurret.SetParent(shipRoot.transform, false);

        // Warning Beacons
        GameObject lWarn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lWarn.name = "LeftWarningBeacon";
        lWarn.transform.SetParent(shipRoot.transform, false);
        lWarn.transform.localPosition = new Vector3(-0.6f, 0.42f, 0.4f);
        lWarn.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
        StripCollider(lWarn);
        leftWarningBeacon = lWarn.GetComponent<MeshRenderer>();
        leftWarningBeacon.sharedMaterial = warningMatOff;

        GameObject rWarn = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rWarn.name = "RightWarningBeacon";
        rWarn.transform.SetParent(shipRoot.transform, false);
        rWarn.transform.localPosition = new Vector3(0.6f, 0.42f, 0.4f);
        rWarn.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
        StripCollider(rWarn);
        rightWarningBeacon = rWarn.GetComponent<MeshRenderer>();
        rightWarningBeacon.sharedMaterial = warningMatOff;

        // ==========================================
        // 5. DEFLECTOR SHIELD BUBBLE
        // ==========================================
        shieldDome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldDome.name = "DeflectorShieldDome";
        shieldDome.transform.SetParent(shipRoot.transform, false);
        shieldDome.transform.localPosition = new Vector3(0f, 0.4f, 0.3f);
        shieldDome.transform.localScale = new Vector3(3.2f, 1.8f, 3.0f);
        StripCollider(shieldDome);

        Material shieldMat = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.1f, 0.85f, 1f, 0.35f), 2.5f, 0.95f, 0.1f);
        shieldDomeRenderer = shieldDome.GetComponent<MeshRenderer>();
        shieldDomeRenderer.sharedMaterial = shieldMat;
        shieldDome.SetActive(false);
    }

    private Transform CreateTurret(string name, Vector3 localPos, Material gunMat, Material glowMat, out Transform muzzlePoint)
    {
        GameObject turretBase = new GameObject(name);
        turretBase.transform.localPosition = localPos;

        // Mount
        GameObject mount = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        mount.name = "Mount";
        mount.transform.SetParent(turretBase.transform, false);
        mount.transform.localScale = new Vector3(0.18f, 0.08f, 0.18f);
        mount.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        StripCollider(mount);
        mount.GetComponent<MeshRenderer>().sharedMaterial = gunMat;

        // Barrel
        GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        barrel.name = "Barrel";
        barrel.transform.SetParent(turretBase.transform, false);
        barrel.transform.localPosition = new Vector3(0f, 0f, 0.35f);
        barrel.transform.localScale = new Vector3(0.11f, 0.45f, 0.11f);
        barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        StripCollider(barrel);
        barrel.GetComponent<MeshRenderer>().sharedMaterial = gunMat;

        // Glow Ring
        GameObject glowRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glowRing.name = "GlowRing";
        glowRing.transform.SetParent(barrel.transform, false);
        glowRing.transform.localPosition = new Vector3(0f, 0.85f, 0f);
        glowRing.transform.localScale = new Vector3(1.25f, 0.15f, 1.25f);
        StripCollider(glowRing);
        glowRing.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

        // Muzzle Point
        GameObject muzzle = new GameObject("MuzzlePoint");
        muzzle.transform.SetParent(turretBase.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 0f, 0.85f);
        muzzlePoint = muzzle.transform;

        return turretBase.transform;
    }

    private void StripCollider(GameObject obj)
    {
        Collider c = obj.GetComponent<Collider>();
        if (c != null)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }
    }

    public void UpdateSteering(float horizontalInput, bool isMoving)
    {
        steerAngle = Mathf.Lerp(steerAngle, horizontalInput * 18f, Time.deltaTime * 10f);

        // Thruster flare
        float leftFlare = (horizontalInput > 0.1f) ? 1.6f : ((horizontalInput < -0.1f) ? 0.7f : 1.0f);
        float rightFlare = (horizontalInput < -0.1f) ? 1.6f : ((horizontalInput > 0.1f) ? 0.7f : 1.0f);

        if (leftThrusterFlame != null)
        {
            leftThrusterFlame.transform.localScale = new Vector3(0.7f * leftFlare, 0.6f * leftFlare, 0.7f * leftFlare);
        }
        if (rightThrusterFlame != null)
        {
            rightThrusterFlame.transform.localScale = new Vector3(0.7f * rightFlare, 0.6f * rightFlare, 0.7f * rightFlare);
        }
    }

    public void UpdateTurretAim(Vector3 targetWorldPos)
    {
        if (leftTurret != null)
        {
            Vector3 dirL = (targetWorldPos - leftTurret.position).normalized;
            if (dirL.sqrMagnitude > 0.01f)
            {
                leftTurret.rotation = Quaternion.Slerp(leftTurret.rotation, Quaternion.LookRotation(dirL), Time.deltaTime * 20f);
            }
        }

        if (rightTurret != null)
        {
            Vector3 dirR = (targetWorldPos - rightTurret.position).normalized;
            if (dirR.sqrMagnitude > 0.01f)
            {
                rightTurret.rotation = Quaternion.Slerp(rightTurret.rotation, Quaternion.LookRotation(dirR), Time.deltaTime * 20f);
            }
        }
    }

    public void SetWarningLights(bool isWarningActive, bool isCritical = false)
    {
        if (!isWarningActive)
        {
            if (leftWarningBeacon != null) leftWarningBeacon.sharedMaterial = warningMatOff;
            if (rightWarningBeacon != null) rightWarningBeacon.sharedMaterial = warningMatOff;
            return;
        }

        flashTimer += Time.deltaTime * (isCritical ? 10f : 5f);
        warningFlashState = (Mathf.FloorToInt(flashTimer) % 2) == 0;

        Material activeMat = isCritical ? warningMatOn : warningAmberOn;
        Material curMat = warningFlashState ? activeMat : warningMatOff;

        if (leftWarningBeacon != null) leftWarningBeacon.sharedMaterial = curMat;
        if (rightWarningBeacon != null) rightWarningBeacon.sharedMaterial = curMat;
    }

    public void SetShieldVisual(bool active)
    {
        if (shieldDome != null && shieldDome.activeSelf != active)
        {
            shieldDome.SetActive(active);
        }
    }
}


