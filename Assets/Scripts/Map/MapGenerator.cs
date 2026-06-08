using System.Collections;
using System.Collections.Generic;
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
    public GameObject playerPrefab;

    [Header("Vegetação LowPolyNature")]
    public GameObject[] grassPrefabs;   // grass_1 a grass_5
    public GameObject[] flowerPrefabs;  // flower_1 a flower_5

    [Header("Castelo")]
    public GameObject castlePrefab;
    public float castleFootprintSize = 20f;
    public float castleSafetyMargin = 10f;
    public float castleMaxHeightVariance = 0.5f;

    private Vector3 castlePosition;
    private bool castleSpawned;

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
        MovePlayerToSafeSpot(noiseMap);
        SpawnCastle(noiseMap);
        SpawnTrees(noiseMap);
        SpawnRocks(noiseMap);
        SpawnGroundPickups(noiseMap);
        SpawnVegetation(noiseMap);
    }

    void MovePlayerToSafeSpot(float[,] noiseMap)
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) return;

        LayerMask terrainMask = LayerMask.GetMask("Terrain");
        int cx = mapWidth  / 2;
        int cy = mapHeight / 2;

        // Procurar em espiral a partir do centro até encontrar uma zona de relva
        for (int radius = 0; radius < mapWidth / 2; radius += 4)
        {
            for (int dy = -radius; dy <= radius; dy += 4)
            {
                for (int dx = -radius; dx <= radius; dx += 4)
                {
                    // Só percorrer a borda de cada raio
                    if (Mathf.Abs(dx) != radius && Mathf.Abs(dy) != radius) continue;

                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (nx < 0 || nx >= mapWidth || ny < 0 || ny >= mapHeight) continue;

                    float noise = noiseMap[nx, ny];
                    if (noise <= 0.45f || noise > 0.65f) continue; // só relva

                    float worldX = nx - mapWidth  / 2f;
                    float worldZ = -(ny - mapHeight / 2f);

                    if (!Physics.Raycast(new Vector3(worldX, 100f, worldZ), Vector3.down,
                            out RaycastHit hit, 200f, terrainMask)) continue;

                    playerObj.transform.position = new Vector3(worldX, hit.point.y + 1f, worldZ);

                    Rigidbody rb = playerObj.GetComponent<Rigidbody>();
                    if (rb != null) rb.linearVelocity = Vector3.zero;

                    Debug.Log($"[PlayerSpawn] Jogador movido para relva em {playerObj.transform.position}");
                    return;
                }
            }
        }

        Debug.LogWarning("[PlayerSpawn] Não foi encontrada posição de relva — jogador mantém posição original.");
    }

    void SpawnCastle(float[,] noiseMap)
    {
        GameObject existing = GameObject.Find("Castle");
        if (existing != null) Destroy(existing);

        castleSpawned = false;
        castlePosition = new Vector3(-99999f, 0f, -99999f);

        if (castlePrefab == null)
        {
            Debug.LogWarning("[CastleSpawn] castlePrefab não atribuído.");
            return;
        }

        LayerMask terrainMask = LayerMask.GetMask("Terrain");
        System.Random rng = new System.Random(seed + 10);

        int footprintCells = Mathf.CeilToInt(castleFootprintSize);
        int borderCells = footprintCells + 20;
        int scanStep = 8;

        List<Vector2Int> candidates = new List<Vector2Int>();

        for (int y = borderCells; y < mapHeight - borderCells; y += scanStep)
        {
            for (int x = borderCells; x < mapWidth - borderCells; x += scanStep)
            {
                float nv = noiseMap[x, y];
                if (nv > 0.45f && nv <= 0.65f)
                    candidates.Add(new Vector2Int(x, y));
            }
        }

        if (candidates.Count == 0)
        {
            Debug.LogWarning("[CastleSpawn] Nenhuma posição candidata encontrada.");
            return;
        }

        // Fisher-Yates shuffle com o seed do mapa
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            Vector2Int tmp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = tmp;
        }

        int flatStep = Mathf.Max(4, footprintCells / 3);

        foreach (Vector2Int c in candidates)
        {
            // 1. Verificar no noise map que toda a área do castelo é terreno válido (sem água/areia/rocha)
            bool areaValid = true;
            for (int dy = -footprintCells; dy <= footprintCells && areaValid; dy += 4)
            {
                for (int dx = -footprintCells; dx <= footprintCells && areaValid; dx += 4)
                {
                    int nx = c.x + dx;
                    int ny = c.y + dy;
                    if (nx < 0 || nx >= mapWidth || ny < 0 || ny >= mapHeight)
                    { areaValid = false; break; }
                    float nv = noiseMap[nx, ny];
                    if (nv <= 0.45f || nv > 0.80f)
                    { areaValid = false; break; }
                }
            }
            if (!areaValid) continue;

            // 2. Verificar planura via raycasts
            float worldX = c.x - mapWidth / 2f;
            float worldZ = -(c.y - mapHeight / 2f);

            float minY = float.MaxValue;
            float maxY = float.MinValue;
            int samples = 0;

            for (int dy = -footprintCells; dy <= footprintCells; dy += flatStep)
            {
                for (int dx = -footprintCells; dx <= footprintCells; dx += flatStep)
                {
                    float sx = worldX + dx;
                    float sz = worldZ + dy;
                    if (Physics.Raycast(new Vector3(sx, 100f, sz), Vector3.down, out RaycastHit sh, 200f, terrainMask))
                    {
                        minY = Mathf.Min(minY, sh.point.y);
                        maxY = Mathf.Max(maxY, sh.point.y);
                        samples++;
                    }
                }
            }

            if (samples == 0 || maxY - minY > castleMaxHeightVariance) continue;

            // 3. Obter posição exata do centro
            if (!Physics.Raycast(new Vector3(worldX, 100f, worldZ), Vector3.down, out RaycastHit centerHit, 200f, terrainMask))
                continue;

            Vector3 spawnPos = new Vector3(worldX, centerHit.point.y, worldZ);
            float yRot = (float)(rng.NextDouble() * 360.0);
            GameObject castle = Instantiate(castlePrefab, spawnPos, Quaternion.Euler(0f, yRot, 0f));
            castle.name = "Castle";

            // Corrigir Y: garantir que a base visual do castelo assenta no terreno,
            // independentemente de onde o pivot do prefab está no modelo.
            Renderer[] castleRenderers = castle.GetComponentsInChildren<Renderer>();
            if (castleRenderers.Length > 0)
            {
                Bounds totalBounds = castleRenderers[0].bounds;
                for (int i = 1; i < castleRenderers.Length; i++)
                    totalBounds.Encapsulate(castleRenderers[i].bounds);
                // bottomLift = distância entre o pivot e o ponto mais baixo do modelo
                float bottomLift = castle.transform.position.y - totalBounds.min.y;
                // Encaixar ligeiramente no terreno para parecer mais natural
                castle.transform.position = new Vector3(spawnPos.x, spawnPos.y + bottomLift - 1.5f, spawnPos.z);
                spawnPos = castle.transform.position;
            }

            castlePosition = spawnPos;
            castleSpawned = true;
            Debug.Log($"[CastleSpawn] Castelo colocado em {spawnPos} (bounds ajustados)");
            return;
        }

        Debug.LogWarning("[CastleSpawn] Não foi encontrada posição válida para o castelo.");
    }

    bool IsInCastleExclusionZone(float worldX, float worldZ)
    {
        if (!castleSpawned) return false;
        float totalRadius = castleFootprintSize + castleSafetyMargin;
        float dx = worldX - castlePosition.x;
        float dz = worldZ - castlePosition.z;
        return (dx * dx + dz * dz) < (totalRadius * totalRadius);
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

    if (playerPrefab == null)
    {
        Debug.LogError("Player Prefab não foi atribuído no MapGenerator.");
        return;
    }

    GameObject existing = GameObject.FindWithTag("Player");
    if (existing != null)
        Destroy(existing);

    GameObject player = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
    player.name = "Player";
    player.tag = "Player";
    player.SetActive(true);

    PlayerController pc = player.GetComponent<PlayerController>();
    if (pc != null)
        pc.enabled = true;

    Animator animator = player.GetComponentInChildren<Animator>(true);
    if (animator != null)
    {
        animator.gameObject.SetActive(true);
        animator.enabled = true;
    }

    Camera cam = player.GetComponentInChildren<Camera>(true);
    if (cam != null)
    {
        cam.gameObject.SetActive(true);
        cam.enabled = true;
    }

    AudioListener listener = player.GetComponentInChildren<AudioListener>(true);
    if (listener != null)
        listener.enabled = true;

    Health health = player.GetComponent<Health>();
    if (health == null)
        health = player.AddComponent<Health>();

    health.SetMaxHP(100f, refillHP: true);

    if (player.GetComponent<PlayerRespawn>() == null)
        player.AddComponent<PlayerRespawn>();

    if (player.GetComponent<HungerSystem>() == null)
        player.AddComponent<HungerSystem>();
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

                if (IsInCastleExclusionZone(worldX, worldZ)) continue;

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
                        FixTreeColliders(tree);
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
                        FixTreeColliders(tree2);
                    }
                }
            }
        }
    }

    void FixTreeColliders(GameObject tree)
    {
        
        Transform troncoFolhas = tree.transform.Find("TroncoFolhas");
        if (troncoFolhas != null)
            foreach (Collider col in troncoFolhas.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

        // Cápsula vertical no root — representa apenas o tronco físico
        CapsuleCollider trunk = tree.AddComponent<CapsuleCollider>();
        trunk.direction = 1;                          // eixo Y (vertical)
        trunk.radius    = 0.3f;                       // ~30 cm de raio
        trunk.height    = 4.0f;                       // 4 m de altura de tronco
        trunk.center    = new Vector3(0f, 2f, 0f);    // centrado a 2 m do chão
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

                if (IsInCastleExclusionZone(worldX, worldZ)) continue;

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

                if (IsInCastleExclusionZone(worldX, worldZ)) continue;

                float offsetX = (float)(rng.NextDouble() - 0.5f) * 4f;
                float offsetZ = (float)(rng.NextDouble() - 0.5f) * 4f;

                if (!Physics.Raycast(new Vector3(worldX + offsetX, 100f, worldZ + offsetZ), Vector3.down, out RaycastHit hit, 200f, terrainMask))
                    continue;

                Vector3 pos = new Vector3(worldX + offsetX, hit.point.y + 0.05f, worldZ + offsetZ);

                // Galhos — apenas em zonas de relva e floresta (exclui areia e água)
                if (galhoPrefab != null && noiseValue > 0.45f && noiseValue <= 0.75f)
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

    void SpawnVegetation(float[,] noiseMap)
    {
        GameObject existing = GameObject.Find("Vegetation");
        if (existing != null) Destroy(existing);

        GameObject container = new GameObject("Vegetation");
        System.Random rng = new System.Random(seed + 3);
        LayerMask terrainMask = LayerMask.GetMask("Terrain");

        // Grass: cobre toda a zona verde (Erva + Erva Densa) — noise 0.45 a 0.80
        if (grassPrefabs != null && grassPrefabs.Length > 0)
        {
            for (int y = 0; y < mapHeight; y += 2)
            {
                for (int x = 0; x < mapWidth; x += 2)
                {
                    float noiseValue = noiseMap[x, y];
                    if (noiseValue <= 0.45f || noiseValue > 0.80f) continue;
                    if (rng.NextDouble() > 0.80f) continue;

                    float worldX = x - mapWidth / 2f;
                    float worldZ = -(y - mapHeight / 2f);
                    if (Mathf.Abs(worldX) > mapWidth / 2f - 10f || Mathf.Abs(worldZ) > mapHeight / 2f - 10f) continue;
                    if (IsInCastleExclusionZone(worldX, worldZ)) continue;

                    float offsetX = (float)(rng.NextDouble() - 0.5f) * 1.5f;
                    float offsetZ = (float)(rng.NextDouble() - 0.5f) * 1.5f;
                    if (!Physics.Raycast(new Vector3(worldX + offsetX, 100f, worldZ + offsetZ), Vector3.down, out RaycastHit hit, 200f, terrainMask)) continue;

                    Vector3 pos = new Vector3(worldX + offsetX, hit.point.y + 0.02f, worldZ + offsetZ);
                    GameObject prefab = grassPrefabs[rng.Next(grassPrefabs.Length)];
                    if (prefab == null) continue;

                    GameObject grass = Instantiate(prefab, pos, Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f));
                    grass.transform.SetParent(container.transform);
                    grass.transform.localScale = Vector3.one * (float)(rng.NextDouble() * 0.4f + 0.6f);
                    foreach (Collider c in grass.GetComponentsInChildren<Collider>()) Destroy(c);
                }
            }
        }

        // Flowers: apenas em zonas de relva (Erva) — noise 0.45 a 0.65
        // Excluí: água (≤0.40), areia (0.40-0.45) e erva densa/rocha (>0.65)
        if (flowerPrefabs != null && flowerPrefabs.Length > 0)
        {
            for (int y = 0; y < mapHeight; y += 3)
            {
                for (int x = 0; x < mapWidth; x += 3)
                {
                    float noiseValue = noiseMap[x, y];
                    if (noiseValue <= 0.45f || noiseValue > 0.65f) continue;
                    if (rng.NextDouble() > 0.55f) continue;

                    float worldX = x - mapWidth / 2f;
                    float worldZ = -(y - mapHeight / 2f);
                    if (Mathf.Abs(worldX) > mapWidth / 2f - 10f || Mathf.Abs(worldZ) > mapHeight / 2f - 10f) continue;
                    if (IsInCastleExclusionZone(worldX, worldZ)) continue;

                    float offsetX = (float)(rng.NextDouble() - 0.5f) * 1.5f;
                    float offsetZ = (float)(rng.NextDouble() - 0.5f) * 1.5f;
                    if (!Physics.Raycast(new Vector3(worldX + offsetX, 100f, worldZ + offsetZ), Vector3.down, out RaycastHit hit, 200f, terrainMask)) continue;

                    Vector3 pos = new Vector3(worldX + offsetX, hit.point.y + 0.02f, worldZ + offsetZ);
                    GameObject prefab = flowerPrefabs[rng.Next(flowerPrefabs.Length)];
                    if (prefab == null) continue;

                    GameObject flower = Instantiate(prefab, pos, Quaternion.Euler(0f, (float)(rng.NextDouble() * 360f), 0f));
                    flower.transform.SetParent(container.transform);
                    flower.transform.localScale = Vector3.one * (float)(rng.NextDouble() * 0.3f + 0.7f);
                    foreach (Collider c in flower.GetComponentsInChildren<Collider>()) Destroy(c);
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
        // O mapa só é gerado quando o jogador clica em "Jogar" no menu inicial (ver MainMenuUI).
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