using UnityEngine;

public static class BunkerBuilder
{
    // Classic arch bunker matrix: 6 rows x 8 cols
    private static readonly string[] BunkerPattern = new string[]
    {
        ".xxxxxx.",
        "xxxxxxxx",
        "xxxxxxxx",
        "xxxxxxxx",
        "xxx..xxx",
        "xx....xx"
    };

    public static void CreateBunkers(Transform parent)
    {
        // 4 bunker shelters across the playfield
        float[] bunkerXPositions = new float[] { -6.5f, -2.2f, 2.2f, 6.5f };
        float bunkerZ = -4.2f;
        float cellSize = 0.26f;
        int depthLayers = 2; // 2 layers of 3D depth

        Material healthyMat = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.1f, 0.9f, 0.35f), 1.0f); // Neon green
        Material damagedMat1 = ProceduralMeshBuilder.GetGlowMaterial(new Color(0.9f, 0.8f, 0.1f), 1.2f); // Yellow
        Material damagedMat2 = ProceduralMeshBuilder.GetGlowMaterial(new Color(1.0f, 0.3f, 0.1f), 1.4f); // Orange/Red

        Material[] damageStages = new Material[] { healthyMat, damagedMat1, damagedMat2 };

        for (int b = 0; b < bunkerXPositions.Length; b++)
        {
            GameObject bunkerRoot = new GameObject($"Bunker_{b + 1}");
            bunkerRoot.transform.SetParent(parent, false);
            bunkerRoot.transform.position = new Vector3(bunkerXPositions[b], 0f, bunkerZ);

            int rows = BunkerPattern.Length;
            int cols = BunkerPattern[0].Length;
            float offsetX = (cols - 1) * 0.5f * cellSize;
            float offsetZ = (depthLayers - 1) * 0.5f * cellSize;

            for (int d = 0; d < depthLayers; d++)
            {
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (BunkerPattern[r][c] == 'x')
                        {
                            float x = c * cellSize - offsetX;
                            float y = (rows - 1 - r) * cellSize + 0.15f;
                            float z = d * cellSize - offsetZ;

                            GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                            cell.name = $"Cell_{d}_{r}_{c}";
                            cell.tag = "ShieldCell";
                            cell.transform.SetParent(bunkerRoot.transform, false);
                            cell.transform.localPosition = new Vector3(x, y, z);
                            cell.transform.localScale = Vector3.one * (cellSize * 0.92f);

                            MeshRenderer mr = cell.GetComponent<MeshRenderer>();
                            if (mr != null) mr.sharedMaterial = healthyMat;

                            Collider col = cell.GetComponent<Collider>();
                            if (col != null) col.isTrigger = true;

                            ShieldCell shieldCell = cell.AddComponent<ShieldCell>();
                            shieldCell.maxHitPoints = 2;
                            shieldCell.hitPoints = 2;
                            shieldCell.damageMaterialStages = damageStages;
                        }
                    }
                }
            }
        }
    }
}

