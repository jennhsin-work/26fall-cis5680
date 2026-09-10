using UnityEngine;

public class ArenaVisuals : MonoBehaviour
{
    [Header("Depth Lane Visuals")]
    public bool createLaneIndicators = true;

    private GameObject backLaneObj;
    private GameObject midLaneObj;
    private GameObject frontLaneObj;

    private void Start()
    {
        SetupArenaVisuals();
    }

    public void SetupArenaVisuals()
    {
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // Clean existing children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            SafeDestroy(transform.GetChild(i).gameObject);
        }

        // 1. Create 3D Floor Cyber Grid
        CreateFloorGrid();

        // 2. Create 3D Depth Lane Stripes
        if (createLaneIndicators)
        {
            CreateDepthLanes();
        }
    }

    private void SafeDestroy(Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) Destroy(obj);
        else DestroyImmediate(obj);
    }

    private void CreateFloorGrid()
    {
        GameObject grid = GameObject.CreatePrimitive(PrimitiveType.Quad);
        grid.name = "CyberGridFloor";
        grid.transform.SetParent(transform, false);
        grid.transform.position = new Vector3(0, -0.05f, 2.5f);
        grid.transform.rotation = Quaternion.Euler(90f, 0, 0);
        grid.transform.localScale = new Vector3(25f, 24f, 1f);

        Collider c = grid.GetComponent<Collider>();
        if (c != null) SafeDestroy(c);

        Material gridMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        Color baseGrid = new Color(0.02f, 0.03f, 0.06f, 1f);
        gridMat.color = baseGrid;
        if (gridMat.HasProperty("_BaseColor")) gridMat.SetColor("_BaseColor", baseGrid);
        if (gridMat.HasProperty("_Smoothness")) gridMat.SetFloat("_Smoothness", 0.35f);
        if (gridMat.HasProperty("_Metallic")) gridMat.SetFloat("_Metallic", 0.05f);

        // Generate procedural clean cyber grid texture
        Texture2D tex = new Texture2D(256, 256);
        Color baseCol = new Color(0.015f, 0.02f, 0.045f);
        Color lineCol = new Color(0.12f, 0.30f, 0.65f);
        Color minorLineCol = new Color(0.04f, 0.12f, 0.28f);

        for (int y = 0; y < 256; y++)
        {
            for (int x = 0; x < 256; x++)
            {
                bool isMajorLine = (x % 32 == 0) || (y % 32 == 0) || (x == 255) || (y == 255);
                bool isMinorLine = (x % 8 == 0) || (y % 8 == 0);
                if (isMajorLine) tex.SetPixel(x, y, lineCol);
                else if (isMinorLine) tex.SetPixel(x, y, minorLineCol);
                else tex.SetPixel(x, y, baseCol);
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Repeat;
        gridMat.mainTexture = tex;
        gridMat.mainTextureScale = new Vector2(10f, 10f);

        MeshRenderer mr = grid.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = gridMat;
            mr.receiveShadows = true;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    private void CreateDepthLanes()
    {
        // Back Lane (Z = -7.5f) - Neon Magenta / Purple (DODGE)
        backLaneObj = CreateLaneStripe("Lane_Back", -7.5f, new Color(0.9f, 0.2f, 0.95f), "BACK LANE (DODGE)");

        // Mid Lane (Z = -6.0f) - Neon Amber / Orange (STANDARD)
        midLaneObj = CreateLaneStripe("Lane_Mid", -6.0f, new Color(1.0f, 0.65f, 0.1f), "MID LANE");

        // Front Lane (Z = -4.8f) - Neon Coral / Red (ATTACK)
        frontLaneObj = CreateLaneStripe("Lane_Front", -4.8f, new Color(1.0f, 0.25f, 0.35f), "FRONT LANE (ATTACK)");
    }

    private GameObject CreateLaneStripe(string name, float zPos, Color glowColor, string label)
    {
        GameObject lane = new GameObject(name);
        lane.transform.SetParent(transform, false);
        lane.transform.position = new Vector3(0f, 0.005f, zPos);

        // Clean glowing line stripe flat on the floor
        GameObject stripe = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stripe.name = "Stripe";
        stripe.transform.SetParent(lane.transform, false);
        stripe.transform.localPosition = Vector3.zero;
        stripe.transform.localScale = new Vector3(22.0f, 0.02f, 0.12f);

        Collider sc = stripe.GetComponent<Collider>();
        if (sc != null) SafeDestroy(sc);

        MeshRenderer mr = stripe.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = ProceduralMeshBuilder.GetGlowMaterial(glowColor, 1.2f, 0.6f, 0.1f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        return lane;
    }
}

