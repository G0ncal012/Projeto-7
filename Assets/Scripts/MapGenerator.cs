using System.Collections;
using UnityEngine;
using UnityEngine.LightTransport;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColourMap, Mesh };
    public DrawMode drawMode;

    public int mapWidth;
    public int mapHeight;
    public float noiseScale;

    public int octaves;
    [Range(0, 1)]
    public float persistance;
    public float lacunarity;

    public int seed;
    public Vector2 offset;

    public bool autoUpdate;

    public float heightMultiplier = 20f;
    public AnimationCurve heightCurve;

    public TerrainType[] regions;

    public float islandFalloff = 2f;

    public GameObject treePrefab;
    public GameObject treePrefab2;
    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

        // ✅ Máscara de ilha com força reduzida para não apagar tudo
        float[,] islandMask = GenerateIslandMask(mapWidth, mapHeight);
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - islandMask[x, y] * 0.5f);
            }
        }

        // DEBUG
        float minH = float.MaxValue, maxH = float.MinValue;
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
            {
                if (noiseMap[x, y] < minH) minH = noiseMap[x, y];
                if (noiseMap[x, y] > maxH) maxH = noiseMap[x, y];
            }
        Debug.Log("noiseMap min=" + minH + " max=" + maxH);
        Debug.Log("heightCurve at 0.5 = " + heightCurve.Evaluate(0.5f));
        Debug.Log("heightCurve at 1.0 = " + heightCurve.Evaluate(1f));

        Color[] colourMap = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float currentHeight = noiseMap[x, y];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (currentHeight <= regions[i].height)
                    {
                        colourMap[y * mapWidth + x] = regions[i].colour;
                        break;
                    }
                }
            }
        }

        MapDisplay display = FindObjectOfType<MapDisplay>();
        if (drawMode == DrawMode.NoiseMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromHeightMap(noiseMap));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            display.DrawTexture(TextureGenerator.TextureFromColourMap(colourMap, mapWidth, mapHeight));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            display.DrawMesh(
    MeshGenerator.GenerateTerrainMesh(noiseMap, heightMultiplier, heightCurve, regions[0].height),
    TextureGenerator.TextureFromColourMap(colourMap, mapWidth, mapHeight)
);

            if (Application.isPlaying)
            {
                SpawnPlayer(noiseMap);
                SpawnWater();
                SpawnMapBorder();
                SpawnTrees(noiseMap);
            }
        }
    }

    float[,] GenerateIslandMask(int width, int height)
    {
        float[,] mask = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float nx = (x / (float)width) * 2f - 1f;
                float ny = (y / (float)height) * 2f - 1f;

                float distance = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));

                mask[x, y] = Mathf.Pow(distance, islandFalloff);
            }
        }

        return mask;
    }

    void SpawnPlayer(float[,] noiseMap)
    {
        float centerHeight = heightCurve.Evaluate(noiseMap[mapWidth / 2, mapHeight / 2]) * heightMultiplier;
        Vector3 spawnPos = new Vector3(0f, centerHeight + 2f, 0f);

        GameObject existing = GameObject.FindWithTag("Player");
        if (existing != null) DestroyImmediate(existing);

        GameObject player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = spawnPos;

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(player.transform);
        body.transform.localPosition = Vector3.zero;
        Destroy(body.GetComponent<CapsuleCollider>());

        CapsuleCollider col = player.AddComponent<CapsuleCollider>();
        col.height = 2f;
        col.radius = 0.5f;

        Rigidbody rb = player.AddComponent<Rigidbody>();
        rb.freezeRotation = true;

        player.AddComponent<PlayerController>();
    }

    void SpawnWater()
{
    GameObject existingWater = GameObject.Find("WaterPlane");
    if (existingWater != null) Destroy(existingWater);

    // ✅ Cria água ligeiramente acima de Y=0 para tapar as bordas do mesh
    GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
    water.name = "WaterPlane";
    water.transform.position = new Vector3(0f, 0.15f, 0f);
    water.transform.localScale = new Vector3(mapWidth * 0.1f, 1f, mapHeight * 0.1f);

    Material waterMat = new Material(Shader.Find("Unlit/Color"));
    waterMat.color = new Color(0.05f, 0.30f, 0.60f, 1f); // azul mais escuro = sensação de profundidade
    water.GetComponent<Renderer>().material = waterMat;

    Destroy(water.GetComponent<Collider>());
}

    void SpawnTrees(float[,] noiseMap)
    {
        GameObject existing = GameObject.Find("Trees");
        if (existing != null) Destroy(existing);

        if (treePrefab == null) return;

        GameObject trees = new GameObject("Trees");
        System.Random rng = new System.Random(seed);

        for (int y = 0; y < mapHeight; y += 8)
        {
            for (int x = 0; x < mapWidth; x += 8)
            {
                float noiseValue = noiseMap[x, y];

                // ✅ Variáveis declaradas fora dos ifs
                float worldX = x - mapWidth / 2f;
                float worldZ = -(y - mapHeight / 2f);
                float offsetX = (float)(rng.NextDouble() - 0.5f) * 3f;
                float offsetZ = (float)(rng.NextDouble() - 0.5f) * 3f;
                float worldY = 0f;
                if (Physics.Raycast(new Vector3(worldX + offsetX, 100f, worldZ + offsetZ), Vector3.down, out RaycastHit hit, 200f))
                    worldY = hit.point.y;

                Vector3 pos = new Vector3(worldX + offsetX, worldY, worldZ + offsetZ);

                if (noiseValue > 0.45f && noiseValue <= 0.55f)
                {
                    if (rng.NextDouble() > 0.7f && treePrefab != null)
                    {
                        GameObject tree = GameObject.Instantiate(treePrefab, pos, Quaternion.identity);
                        tree.transform.SetParent(trees.transform);
                        tree.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f);
                        float scale = (float)(rng.NextDouble() * 0.3f + 0.7f);
                        tree.transform.localScale = Vector3.one * scale;
                    }
                }
                else if (noiseValue > 0.45f && noiseValue <= 0.65f)
                {
                    if (rng.NextDouble() > 0.7f && treePrefab2 != null)
                    {
                        GameObject tree2 = GameObject.Instantiate(treePrefab2, pos, Quaternion.identity);
                        tree2.transform.SetParent(trees.transform);
                        tree2.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f);
                        float scale2 = (float)(rng.NextDouble() * 0.3f + 0.7f);
                        tree2.transform.localScale = Vector3.one * scale2;
                    }
                }
            }
        }
    }

    void SpawnMapBorder()
    {
        GameObject existingBorder = GameObject.Find("MapBorder");
        if (existingBorder != null) Destroy(existingBorder);

        GameObject border = new GameObject("MapBorder");

        float borderHeight = heightMultiplier * 2f;
        float thickness = 2f;
        float halfW = mapWidth / 2f;
        float halfH = mapHeight / 2f;

        CreateWall(border, new Vector3(0f, borderHeight / 2f, halfH), new Vector3(mapWidth, borderHeight, thickness));
        CreateWall(border, new Vector3(0f, borderHeight / 2f, -halfH), new Vector3(mapWidth, borderHeight, thickness));
        CreateWall(border, new Vector3(halfW, borderHeight / 2f, 0f), new Vector3(thickness, borderHeight, mapHeight));
        CreateWall(border, new Vector3(-halfW, borderHeight / 2f, 0f), new Vector3(thickness, borderHeight, mapHeight));
    }

    void CreateWall(GameObject parent, Vector3 position, Vector3 size)
    {
        GameObject wall = new GameObject("Wall");
        wall.transform.SetParent(parent.transform);
        wall.transform.position = position;

        BoxCollider col = wall.AddComponent<BoxCollider>();
        col.size = size;
    }

    void Start()
    {
        mapWidth = 200;
        mapHeight = 200;
        noiseScale = 25f;
        octaves = 1;
        persistance = 0.5f;
        lacunarity = 2f;
        heightMultiplier = 4f;
        islandFalloff = 8f;

        heightCurve = new AnimationCurve(
        new Keyframe(0f, 0f),
        new Keyframe(0.39f, 0f),
        new Keyframe(0.4f, 0f),
        new Keyframe(0.45f, 0.1f), 
        new Keyframe(0.7f, 0.3f), 
        new Keyframe(1f, 0.5f)    
        );

        regions = new TerrainType[]
        {
        new TerrainType { name = "Agua",       height = 0.4f,  colour = new Color(0.13f, 0.46f, 0.70f) },
        new TerrainType { name = "Areia",      height = 0.45f, colour = new Color(0.93f, 0.87f, 0.60f) },
        new TerrainType { name = "Erva",       height = 0.65f, colour = new Color(0.27f, 0.60f, 0.22f) },
        new TerrainType { name = "Erva Densa",   height = 0.80f, colour = new Color(0.13f, 0.37f, 0.13f) },
        new TerrainType { name = "Rocha",      height = 1.00f, colour = new Color(0.50f, 0.45f, 0.40f) },
        };

        heightCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.39f, 0f),
            new Keyframe(0.4f, 0f),
            new Keyframe(0.45f, 0.2f),
            new Keyframe(0.7f, 0.5f),
            new Keyframe(1f, 0.8f)
        );

        seed = Random.Range(0, 100000);
        GenerateMap();
    }

    void OnValidate()
    {
        if (mapWidth < 2) mapWidth = 2;
        if (mapHeight < 2) mapHeight = 2;
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}