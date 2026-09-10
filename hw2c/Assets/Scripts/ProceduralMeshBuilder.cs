using System.Collections.Generic;
using UnityEngine;

public static class ProceduralMeshBuilder
{
    private static Material dropShadowMaterial;
    private static readonly Dictionary<string, Material> materialCache = new Dictionary<string, Material>();
    private static Mesh unitCubeMesh;

    private static Mesh GetUnitCubeMesh()
    {
        if (unitCubeMesh != null) return unitCubeMesh;
        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        unitCubeMesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
        if (Application.isPlaying) Object.Destroy(tempCube);
        else Object.DestroyImmediate(tempCube);
        return unitCubeMesh;
    }

    public static Material GetDropShadowMaterial()
    {
        if (dropShadowMaterial != null) return dropShadowMaterial;

        Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        float center = 31.5f;
        float maxRadius = 30f;
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                float norm = Mathf.Clamp01(dist / maxRadius);
                // Smooth cosine falloff for soft drop shadow
                float alpha = Mathf.SmoothStep(0.75f, 0.0f, norm);
                tex.SetPixel(x, y, new Color(0.01f, 0.02f, 0.04f, alpha));
            }
        }
        tex.Apply();
        tex.wrapMode = TextureWrapMode.Clamp;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent") ?? Shader.Find("Sprites/Default");
        dropShadowMaterial = new Material(shader);
        dropShadowMaterial.mainTexture = tex;
        if (dropShadowMaterial.HasProperty("_BaseMap")) dropShadowMaterial.SetTexture("_BaseMap", tex);
        if (dropShadowMaterial.HasProperty("_BaseColor")) dropShadowMaterial.SetColor("_BaseColor", Color.white);
        if (dropShadowMaterial.HasProperty("_Surface")) dropShadowMaterial.SetFloat("_Surface", 1); // Transparent
        if (dropShadowMaterial.HasProperty("_Blend")) dropShadowMaterial.SetFloat("_Blend", 0); // Alpha
        dropShadowMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        dropShadowMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        dropShadowMaterial.SetInt("_ZWrite", 0);
        dropShadowMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return dropShadowMaterial;
    }

    public static Material GetGlowMaterial(Color color, float emissionMultiplier = 0.4f, float smoothness = 0.45f, float metallic = 0.1f)
    {
        string key = $"{color.r:F3}_{color.g:F3}_{color.b:F3}_{color.a:F3}_{emissionMultiplier:F2}_{smoothness:F2}_{metallic:F2}";
        if (materialCache.TryGetValue(key, out Material cachedMat) && cachedMat != null)
        {
            return cachedMat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
        Material mat = new Material(shader);
        mat.color = color;
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emissionMultiplier);
        }
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);

        materialCache[key] = mat;
        return mat;
    }

    public static GameObject CreatePlayerMesh(Color bodyColor)
    {
        GameObject root = new GameObject("PlayerCannon_Visual");

        Material hullMat = GetGlowMaterial(bodyColor, 0.45f, 0.5f, 0.15f);
        Material cockpitMat = GetGlowMaterial(new Color(0.1f, 0.95f, 1.0f), 1.4f, 0.8f, 0.1f); // Bright cyan glow
        Material barrelMat = GetGlowMaterial(new Color(0.95f, 0.95f, 1.0f), 1.1f, 0.7f, 0.2f); // Bright silver/white
        Material thrusterMat = GetGlowMaterial(new Color(1.0f, 0.45f, 0.05f), 1.6f, 0.3f, 0f); // Orange glow

        // 3D Voxel layers for Player Tank / Ship (Y = Height, X = Width, Z = Depth)
        string[] layerBase = new string[]
        {
            "...xxxxx...",
            "..xxxxxxx..",
            ".xxxxxxxxx.",
            "xxxxxxxxxxx",
            "xxxxxxxxxxx",
            "xxxxxxxxxxx",
            ".xxxx.xxxx."
        };

        string[] layerMid = new string[]
        {
            "x....x....x",
            "x...xxx...x",
            "x..xxxxx..x",
            "xxxxxxxxxxx",
            ".xxxxxxxxx.",
            "..xxxxxxx..",
            "...tt.tt..."
        };

        string[] layerTop = new string[]
        {
            "b....b....b",
            "b...ccc...b",
            "....ccc....",
            "...ccccc...",
            "....ccc....",
            ".....c.....",
            "..........."
        };

        string[] layerPeak = new string[]
        {
            ".....b.....",
            "....ccc....",
            "....ccc....",
            ".....c.....",
            "...........",
            "...........",
            "..........."
        };

        List<string[]> layers = new List<string[]> { layerBase, layerMid, layerTop, layerPeak };

        Dictionary<char, Material> matMap = new Dictionary<char, Material>
        {
            { 'x', hullMat },
            { 'c', cockpitMat },
            { 'b', barrelMat },
            { 't', thrusterMat }
        };

        BuildVoxelMesh(root, layers, matMap, 0.13f, upright: false);
        AddDropShadow(root, new Vector3(1.8f, 1.5f, 1f), -0.05f);

        return root;
    }

    public static GameObject CreateInvaderMesh(InvaderType type, Color color)
    {
        GameObject root = new GameObject($"Invader_{type}_Visual");

        // Multi-tone shading materials for high visual clarity and depth
        Material bodyMat;
        Material shadeMat;
        Material highlightMat;
        Material eyeMat = GetGlowMaterial(new Color(1.0f, 0.95f, 0.15f), 2.2f, 0.9f, 0f); // Radiant Gold Eyes
        Material coreMat = GetGlowMaterial(Color.white, 1.3f, 0.8f, 0.1f);

        List<string[]> layers = new List<string[]>();
        float voxelSize = 0.11f;

        switch (type)
        {
            case InvaderType.Squid:
                // Squid: Vibrant Hot Pink with Light Pink Crest and Deep Purple-Magenta Shade
                bodyMat = GetGlowMaterial(new Color(1.0f, 0.15f, 0.55f), 0.45f, 0.45f, 0.1f);
                shadeMat = GetGlowMaterial(new Color(0.45f, 0.04f, 0.22f), 0.15f, 0.3f, 0.1f);
                highlightMat = GetGlowMaterial(new Color(1.0f, 0.65f, 0.85f), 0.75f, 0.6f, 0.15f);

                string[] squidFront = new string[]
                {
                    "..hhhh..",
                    ".hhhhhh.",
                    "hhe..ehh",
                    "xxxxxxxx",
                    "..s..s..",
                    ".s.ss.s.",
                    "s.s..s.s"
                };
                string[] squidMid = new string[]
                {
                    ".hhhhhh.",
                    "xxxxxxxx",
                    "xxxxxxxx",
                    "xxxxxxxx",
                    "..xxxx..",
                    "..ssss..",
                    ".s....s."
                };
                string[] squidBack = new string[]
                {
                    "..hhhh..",
                    ".xxxxxx.",
                    "..xxxx..",
                    "..ssss..",
                    "..s..s..",
                    "..s..s..",
                    "........"
                };
                layers.Add(squidBack);
                layers.Add(squidMid);
                layers.Add(squidFront);
                voxelSize = 0.12f;
                break;

            case InvaderType.Crab:
                // Crab: Electric Cyan with Ice-Blue Crest and Deep Azure Shade
                bodyMat = GetGlowMaterial(new Color(0.05f, 0.85f, 1.0f), 0.45f, 0.45f, 0.1f);
                shadeMat = GetGlowMaterial(new Color(0.02f, 0.25f, 0.55f), 0.15f, 0.3f, 0.1f);
                highlightMat = GetGlowMaterial(new Color(0.7f, 0.95f, 1.0f), 0.75f, 0.6f, 0.15f);

                string[] crabFront = new string[]
                {
                    "..h.....h..",
                    "...h...h...",
                    "..hhhhhhh..",
                    ".he.xxx.eh.",
                    "xxxxxxxxxxx",
                    "x.sssssss.x",
                    "s.s.....s.s",
                    "...ss.ss..."
                };
                string[] crabMid = new string[]
                {
                    "h.........h",
                    ".h.......h.",
                    ".xxxxxxxxx.",
                    "xxxxxxxxxxx",
                    "xxxxxxxxxxx",
                    ".sssssssss.",
                    ".s.......s.",
                    "..ss...ss.."
                };
                string[] crabBack = new string[]
                {
                    "...........",
                    "...........",
                    "..hhhhhhh..",
                    ".xxxxxxxxx.",
                    ".sssssssss.",
                    "..sssssss..",
                    "...........",
                    "..........."
                };
                layers.Add(crabBack);
                layers.Add(crabMid);
                layers.Add(crabFront);
                voxelSize = 0.11f;
                break;

            case InvaderType.Octopus:
            default:
                // Octopus: Vivid Lime Green with Lemon Crest and Deep Forest Green Shade
                bodyMat = GetGlowMaterial(new Color(0.25f, 0.98f, 0.25f), 0.45f, 0.45f, 0.1f);
                shadeMat = GetGlowMaterial(new Color(0.05f, 0.38f, 0.12f), 0.15f, 0.3f, 0.1f);
                highlightMat = GetGlowMaterial(new Color(0.8f, 1.0f, 0.5f), 0.75f, 0.6f, 0.15f);

                string[] octoFront = new string[]
                {
                    "....hhhh....",
                    ".hhhhhhhhhh.",
                    "hhxe.xx.exhh",
                    "xxxxxxxxxxxx",
                    "xxxxxxxxxxxx",
                    "..sss..sss..",
                    ".ss..ss..ss.",
                    "ss........ss"
                };
                string[] octoMid = new string[]
                {
                    "...hhhhhh...",
                    "xxxxxxxxxxxx",
                    "xxxxxxxxxxxx",
                    "xxxxxxxxxxxx",
                    ".ssssssssss.",
                    "..ssssssss..",
                    "..ss....ss..",
                    ".ss......ss."
                };
                string[] octoBack = new string[]
                {
                    "....hhhh....",
                    "..xxxxxxxx..",
                    ".xxxxxxxxxx.",
                    ".xxxxxxxxxx.",
                    "..ssssssss..",
                    "...ssssss...",
                    "....s..s....",
                    "............"
                };
                layers.Add(octoBack);
                layers.Add(octoMid);
                layers.Add(octoFront);
                voxelSize = 0.11f;
                break;
        }

        Dictionary<char, Material> matMap = new Dictionary<char, Material>
        {
            { 'x', bodyMat },
            { 's', shadeMat },
            { 'h', highlightMat },
            { 'e', eyeMat },
            { 'c', coreMat }
        };

        BuildVoxelMesh(root, layers, matMap, voxelSize, upright: true);
        AddDropShadow(root, new Vector3(1.6f, 1.3f, 1f), -0.45f);

        return root;
    }

    public static GameObject CreateUFOMesh(Color color)
    {
        GameObject root = new GameObject("UFO_Visual");
        Material bodyMat = GetGlowMaterial(new Color(1.0f, 0.15f, 0.25f), 0.6f, 0.5f, 0.15f); // Ruby Red
        Material domeMat = GetGlowMaterial(new Color(1f, 0.85f, 0.2f), 1.5f, 0.8f, 0.1f); // Golden Cockpit Dome
        Material lightMat = GetGlowMaterial(new Color(0.2f, 1f, 0.5f), 1.8f, 0.7f, 0.05f); // Neon Green/Cyan Rim Lights
        Material emitterMat = GetGlowMaterial(new Color(0.3f, 0.8f, 1f), 1.6f, 0.8f, 0.1f); // Tractor Beam Emitter

        string[] ufoBottom = new string[]
        {
            "..............",
            ".....eeee.....",
            "....eeeeee....",
            "....eeeeee....",
            ".....eeee.....",
            ".............."
        };

        string[] ufoRim = new string[]
        {
            "..l........l..",
            ".xxxxxxxxxxxx.",
            "xxxxxxxxxxxxxx",
            "xxxxxxxxxxxxxx",
            ".xxxxxxxxxxxx.",
            "..l........l.."
        };

        string[] ufoBody = new string[]
        {
            "....xxxxxx....",
            "..xxxxxxxxxx..",
            ".xxxxxxxxxxxx.",
            ".xxxxxxxxxxxx.",
            "..xxxxxxxxxx..",
            "....xxxxxx...."
        };

        string[] ufoDome = new string[]
        {
            "..............",
            ".....dddd.....",
            "....dddddd....",
            "....dddddd....",
            ".....dddd.....",
            ".............."
        };

        List<string[]> layers = new List<string[]> { ufoBottom, ufoRim, ufoBody, ufoDome };

        Dictionary<char, Material> matMap = new Dictionary<char, Material>
        {
            { 'x', bodyMat },
            { 'd', domeMat },
            { 'l', lightMat },
            { 'e', emitterMat }
        };

        BuildVoxelMesh(root, layers, matMap, 0.14f, upright: false);
        AddDropShadow(root, new Vector3(2.4f, 1.8f, 1f), -0.3f);

        return root;
    }

    private static void AddDropShadow(GameObject parent, Vector3 scale, float localY)
    {
        GameObject shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
        shadow.name = "GroundShade";
        shadow.transform.SetParent(parent.transform, false);
        shadow.transform.localPosition = new Vector3(0, localY, 0);
        shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        shadow.transform.localScale = scale;

        Collider sc = shadow.GetComponent<Collider>();
        if (sc != null)
        {
            if (Application.isPlaying) Object.Destroy(sc);
            else Object.DestroyImmediate(sc);
        }

        MeshRenderer smr = shadow.GetComponent<MeshRenderer>();
        if (smr != null)
        {
            smr.sharedMaterial = GetDropShadowMaterial();
            smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            smr.receiveShadows = false;
        }
    }

    private static void BuildVoxelMesh(
        GameObject parent,
        List<string[]> layers,
        Dictionary<char, Material> matMap,
        float voxelSize,
        bool upright = false)
    {
        int numLayers = layers.Count;
        int rows = layers[0].Length;
        int cols = layers[0][0].Length;

        Mesh cubeMesh = GetUnitCubeMesh();

        Dictionary<char, List<CombineInstance>> combineDict = new Dictionary<char, List<CombineInstance>>();
        foreach (char k in matMap.Keys)
        {
            combineDict[k] = new List<CombineInstance>();
        }

        float offsetX = (cols - 1) * 0.5f * voxelSize;
        float offsetY = upright ? (rows - 1) * 0.5f * voxelSize : (numLayers - 1) * 0.5f * voxelSize;
        float offsetZ = upright ? (numLayers - 1) * 0.5f * voxelSize : (rows - 1) * 0.5f * voxelSize;

        for (int l = 0; l < numLayers; l++)
        {
            string[] layer = layers[l];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < layer[r].Length; c++)
                {
                    char ch = layer[r][c];
                    if (ch == '.' || !matMap.ContainsKey(ch)) continue;

                    Vector3 pos;
                    if (upright)
                    {
                        float x = c * voxelSize - offsetX;
                        float y = (rows - 1 - r) * voxelSize - offsetY + 0.5f;
                        float z = l * voxelSize - offsetZ;
                        pos = new Vector3(x, y, z);
                    }
                    else
                    {
                        float x = c * voxelSize - offsetX;
                        float y = l * voxelSize - offsetY + 0.4f;
                        float z = (rows - 1 - r) * voxelSize - offsetZ;
                        pos = new Vector3(x, y, z);
                    }

                    Matrix4x4 matrix = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one * (voxelSize * 0.94f));
                    CombineInstance ci = new CombineInstance
                    {
                        mesh = cubeMesh,
                        transform = matrix
                    };

                    combineDict[ch].Add(ci);
                }
            }
        }

        List<Mesh> subMeshes = new List<Mesh>();
        List<Material> materials = new List<Material>();

        foreach (var kvp in combineDict)
        {
            if (kvp.Value.Count > 0 && matMap.ContainsKey(kvp.Key))
            {
                Mesh m = new Mesh();
                m.CombineMeshes(kvp.Value.ToArray(), true, true);
                subMeshes.Add(m);
                materials.Add(matMap[kvp.Key]);
            }
        }

        CombineInstance[] finalCombines = new CombineInstance[subMeshes.Count];
        for (int i = 0; i < subMeshes.Count; i++)
        {
            finalCombines[i].mesh = subMeshes[i];
            finalCombines[i].transform = Matrix4x4.identity;
        }

        Mesh combinedMesh = new Mesh();
        combinedMesh.name = $"{parent.name}_Mesh";
        combinedMesh.CombineMeshes(finalCombines, false, false);
        combinedMesh.RecalculateNormals();
        combinedMesh.RecalculateBounds();

        MeshFilter mf = parent.AddComponent<MeshFilter>();
        mf.sharedMesh = combinedMesh;

        MeshRenderer mr = parent.AddComponent<MeshRenderer>();
        mr.sharedMaterials = materials.ToArray();
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        mr.receiveShadows = true;
    }
}



