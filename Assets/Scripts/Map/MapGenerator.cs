using System.Collections;
using UnityEngine;

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
    public GameObject rocklee;
    public GameObject galhoPrefab;
    public GameObject pedrinhaPrefab;
    public GameObject rockDropPrefab;

    public void GenerateMap()
    {
        float[,] noiseMap = Noise.GenerateNoiseMap(mapWidth, mapHeight, seed, noiseScale, octaves, persistance, lacunarity, offset);

        float[,] islandMask = GenerateIslandMask(mapWidth, mapHeight);
        for (int y = 0; y < mapHeight; y++)
            for (int x = 0; x < mapWidth; x++)
                noiseMap[x, y] = Mathf.Clamp01(noiseMap[x, y] - islandMask[x, y] * 0.1f);

        Color[] colourMap = new Color[mapWidth * mapHeight];
        for (int y = 0; y < mapHeight; y++)
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

        MapDisplay display = FindAnyObjectByType<MapDisplay>();
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
                StartCoroutine(SpawnAfterPhysics(noiseMap));
            }
        }
    }

    IEnumerator SpawnAfterPhysics(float[,] noiseMap)
    {
        yield return null;
        yield return null;
        SpawnTrees(noiseMap);
        SpawnRocks(noiseMap);
        SpawnGroundPickups(noiseMap);
    }

    float[,] GenerateIslandMask(int width, int height)
    {
        float[,] mask = new float[width, height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float nx = (x / (float)width) * 2f - 1f;
                float ny = (y / (float)height) * 2f - 1f;
                float distance = Mathf.Max(Mathf.Abs(nx), Mathf.Abs(ny));
                mask[x, y] = Mathf.Pow(distance, islandFalloff);
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

        Health health = player.AddComponent<Health>();
        health.SetMaxHP(100f, refillHP: true);
        player.AddComponent<PlayerRespawn>();
    }

    void SpawnWater()
    {
        GameObject existingWater = GameObject.Find("WaterPlane");
        if (existingWater != null) Destroy(existingWater);

        GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
        water.name = "WaterPlane";
        water.transform.position = new Vector3(0f, 0.15f, 0f);
        water.transform.localScale = new Vector3(mapWidth * 0.1f, 1f, mapHeight * 0.1f);

        Material waterMat = new Material(Shader.Find("Unlit/Color"));
        waterMat.color = new Color(0.05f, 0.30f, 0.60f, 1f);
        water.GetComponent<Renderer>().material = waterMat;

        Destroy(water.GetComponent<Collider>());
    }

    void SpawnTrees(float[,] noiseMap)
    {
        GameObject existing = GameObject.Find("Trees");
        if (existing != null) Destroy(existing);

        if (treePrefab == null) return;

        LayerMask terrainMask = LayerMask.GetMask("Terrain");
        GameObject trees = new GameObject("Trees");
        System.Random rng = new System.Random(seed);

        for (int y = 0; y < mapHeight; y += 8)
        {
            for (int x = 0; x < mapWidth; x += 8)
            {
                float noiseValue = noiseMap[x, y];
                float worldX = x - mapWidth / 2f;
                float worldZ = -(y - mapHeight / 2f);

                if (Mathf.Abs(worldX) > mapWidth / 2f - 10f || Mathf.Abs(worldZ) > mapHeight / 2f - 10f)
                    continue;

                float offsetX = (float)(rng.NextDouble() - 0.5f) * 3f;
                float offsetZ = (float)(rng.NextDouble() - 0.5f) * 3f;
                float worldY = 0f;

                if (Physics.Raycast(new Vector3(worldX + offsetX, 100f, worldZ + offsetZ), Vector3.down, out RaycastHit hit, 200f, terrainMask))
                    worldY = hit.point.y;
                else
                    continue;

                Vector3 pos = new Vector3(worldX + offsetX, worldY, worldZ + offsetZ);

                if (noiseValue > 0.45f && noiseValue <= 0.55f)
                {
                    if (rng.NextDouble() > 0.4f && treePrefab != null)
                    {
                        GameObject tree = GameObject.Instantiate(treePrefab, pos, Quaternion.identity);
                        tree.transform.SetParent(trees.transform);
                        tree.transform.rotation = Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f);
                        float scale = (float)(rng.NextDouble() * 0.3f + 0.7f);
                        tree.transform.localScale = Vector3.one * scale;
                    }
                }
                else if (noiseValue > 0.55f && noiseValue <= 0.75f)
                {
                    if (rng.NextDouble() > 0.4f && treePrefab2 != null)
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

    void SpawnRocks(float[,] noiseMap)
    {
        GameObject existing = GameObject.Find("Rocks");
        if (existing != null) Destroy(existing);

        if (rocklee == null) return;

        GameObject rocks = new GameObject("Rocks");
        System.Random rng = new System.Random(seed + 1);
        LayerMask terrainMask = LayerMask.GetMask("Terrain");

        for (int y = 0; y < mapHeight; y += 7)
        {
            for (int x = 0; x < mapWidth; x += 7)
            {
                float noiseValue = noiseMap[x, y];
                float worldX = x - mapWidth / 2f;
                float worldZ = -(y - mapHeight / 2f);

                if (Mathf.Abs(worldX) > mapWidth / 2f - 10f || Mathf.Abs(worldZ) > mapHeight / 2f - 10f)
                    continue;

                if (noiseValue > 0.45f && noiseValue <= 1f)
                {
                    if (rng.NextDouble() > 0.8f)
                    {
                        float offsetX = (float)(rng.NextDouble() - 0.5f) * 3f;
                        float offsetZ = (float)(rng.NextDouble() - 0.5f) * 3f;
                        float worldY = 0f;

                        if (Physics.Raycast(new Vector3(worldX + offsetX, 100f, worldZ + offsetZ), Vector3.down, out RaycastHit hit, 200f, terrainMask))
                            worldY = hit.point.y;
                        else
                            continue;

                        Vector3 pos = new Vector3(worldX + offsetX, worldY, worldZ + offsetZ);

                        GameObject rock = GameObject.Instantiate(rocklee, pos, Quaternion.identity);
                        rock.transform.SetParent(rocks.transform);
                        rock.transform.rotation = Quaternion.Euler(
                            (float)(rng.NextDouble() * 30f),
                            (float)(rng.NextDouble() * 360f),
                            (float)(rng.NextDouble() * 30f)
                        );

                        float scale;
                        if (noiseValue > 0.75f)
                            scale = (float)(rng.NextDouble() * 0.5f + 0.3f);
                        else if (noiseValue > 0.65f)
                            scale = (float)(rng.NextDouble() * 0.3f + 0.1f);
                        else
                            scale = (float)(rng.NextDouble() * 0.1f + 0.05f);

                        rock.transform.localScale = Vector3.one * scale;

                        RockBreaking rockBreaking = rock.AddComponent<RockBreaking>();
                        if (rockDropPrefab != null)
                            rockBreaking.Setup(rockDropPrefab);
                    }
                }
            }
        }
    }

    void SpawnGroundPickups(float[,] noiseMap)
    {
        GameObject existing = GameObject.Find("GroundPickups");
        if (existing != null) Destroy(existing);

        GameObject container = new GameObject("GroundPickups");
        System.Random rng = new System.Random(seed + 2);
        LayerMask terrainMask = LayerMask.GetMask("Terrain");

        for (int y = 0; y < mapHeight; y += 5)
        {
            for (int x = 0; x < mapWidth; x += 5)
            {
                float noiseValue = noiseMap[x, y];
                float worldX = x - mapWidth / 2f;
                float worldZ = -(y - mapHeight / 2f);

                if (Mathf.Abs(worldX) > mapWidth / 2f - 10f || Mathf.Abs(worldZ) > mapHeight / 2f - 10f)
                    continue;

                float offsetX = (float)(rng.NextDouble() - 0.5f) * 4f;
                float offsetZ = (float)(rng.NextDouble() - 0.5f) * 4f;

                if (!Physics.Raycast(new Vector3(worldX + offsetX, 100f, worldZ + offsetZ), Vector3.down, out RaycastHit hit, 200f, terrainMask))
                    continue;

                Vector3 pos = new Vector3(worldX + offsetX, hit.point.y + 0.05f, worldZ + offsetZ);

                // Galhos — nas zonas de floresta (mesmo range das árvores)
                if (galhoPrefab != null && noiseValue > 0.40f && noiseValue <= 0.75f)
                {
                    if (rng.NextDouble() > 0.75f)
                    {
                        GameObject galho = Instantiate(galhoPrefab, pos, Quaternion.Euler(90f, (float)(rng.NextDouble() * 360f), 0f));
                        galho.transform.SetParent(container.transform);
                        galho.transform.localScale = Vector3.one * (float)(rng.NextDouble() * 0.05f + 0.05f);
                    }
                }

                // Pedrinhas — nas zonas de rocha e erva densa
                if (pedrinhaPrefab != null && noiseValue > 0.50f && noiseValue <= 0.85f)
                {
                    if (rng.NextDouble() > 0.80f)
                    {
                        Quaternion rot = Quaternion.Euler(
                            (float)(rng.NextDouble() * 30f),
                            (float)(rng.NextDouble() * 360f),
                            (float)(rng.NextDouble() * 30f)
                        );
                        GameObject pedra = Instantiate(pedrinhaPrefab, pos, rot);
                        pedra.transform.SetParent(container.transform);
                        pedra.transform.localScale = Vector3.one * (float)(rng.NextDouble() * 0.05f + 0.03f);
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

        float borderHeight = heightMultiplier * 4f;
        float thickness = 5f;
        float halfW = mapWidth / 2f;
        float halfH = mapHeight / 2f;

        CreateWall(border, new Vector3(0f, borderHeight / 2f, halfH), new Vector3(mapWidth + thickness, borderHeight, thickness));
        CreateWall(border, new Vector3(0f, borderHeight / 2f, -halfH), new Vector3(mapWidth + thickness, borderHeight, thickness));
        CreateWall(border, new Vector3(halfW, borderHeight / 2f, 0f), new Vector3(thickness, borderHeight, mapHeight + thickness));
        CreateWall(border, new Vector3(-halfW, borderHeight / 2f, 0f), new Vector3(thickness, borderHeight, mapHeight + thickness));
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
        mapWidth = 500;
        mapHeight = 500;
        noiseScale = 200f;
        octaves = 1;
        persistance = 0.5f;
        lacunarity = 2f;
        heightMultiplier = 4f;
        islandFalloff = 2f;

        regions = new TerrainType[]
        {
            new TerrainType { name = "Agua",       height = 0.4f,  colour = new Color(0.13f, 0.46f, 0.70f) },
            new TerrainType { name = "Areia",      height = 0.45f, colour = new Color(0.93f, 0.87f, 0.60f) },
            new TerrainType { name = "Erva",       height = 0.65f, colour = new Color(0.27f, 0.60f, 0.22f) },
            new TerrainType { name = "Erva Densa", height = 0.80f, colour = new Color(0.13f, 0.37f, 0.13f) },
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