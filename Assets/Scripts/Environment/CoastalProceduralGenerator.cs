using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace DyingHorizon
{
    /// <summary>
    /// Gerador procedural da Zona Litorânea de Dying Horizon.
    /// Esculpe costa orgânica via Perlin Noise, pinta camadas com Wang Autotiling / RuleTiles,
    /// aplica AnimatedTile nas ondas do oceano e espuma costeira, e distribui props, suprimentos e inimigos.
    /// </summary>
    public class CoastalProceduralGenerator : MonoBehaviour
    {
        [Header("Dimensões e Seed")]
        [SerializeField] private int minX = -28;
        [SerializeField] private int maxX = 22;
        [SerializeField] private int minY = -16;
        [SerializeField] private int maxY = 15;

        public int seed = 2026;
        public bool useRandomSeed = false;
        public bool generateOnAwake = true;

        [Header("Parâmetros de Ruído (Perlin Noise)")]
        [SerializeField] private float shoreFrequency = 0.14f;
        [SerializeField] private float shoreAmplitude = 2.6f;
        [SerializeField] private float shoreBaseX = 4.6f;
        [SerializeField] private float grassFrequency = 0.16f;
        [SerializeField] private float grassAmplitude = 2.2f;
        [SerializeField] private float grassBaseX = -2.2f;
        [SerializeField] private float trailFrequency = 0.18f;
        [SerializeField] private float trailWidth = 1.25f;

        [Header("Referências de Tilemaps")]
        public Tilemap groundTilemap;
        public Tilemap oceanTilemap;
        public Tilemap waveFoamTilemap;
        public Tilemap decoTilemap;
        public Transform propsContainer;

        [Header("Tiles Animados (Ondas 8-Bits) e Colisão")]
        public TileBase oceanAnimatedTile;
        public TileBase oceanFoamAnimatedTile;
        public TileBase oceanBarrierTile;

        [Header("Wang Tilesets Encadeados (16 tiles cada: w00..w15)")]
        public TileBase[] oceanBeachTiles = new TileBase[16];
        public TileBase[] beachGrassTiles = new TileBase[16];
        public TileBase[] dirtGrassTiles = new TileBase[16];

        [Header("RuleTiles de Referência (Zona Litoral)")]
        public TileBase rulePraiaAreia;
        public TileBase ruleVegetacaoCosteira;
        public TileBase ruleTrilhaTerra;

        [Header("Tiles de Detalhes")]
        public TileBase driftwoodTile;
        public TileBase spikeTrapTile;

        [Header("Sprites de Props e Estruturas")]
        public Sprite campfireSprite;
        public Sprite wreckedBoatSprite;
        public Sprite abandonedShackSprite;
        public Sprite rustedCarSprite;
        public Sprite coastalPalmSprite;
        public Sprite deadTreeSprite;
        public Sprite coastalPineSprite;
        public Sprite coastalRockSprite;
        public Sprite seaRockSprite;
        public Sprite rustedBarrelSprite;
        public Sprite supplyCrateSprite;

        [Header("Sprites das 3 Primeiras Armas e Projéteis (8-Bits)")]
        public Sprite pistolSprite;
        public Sprite shotgunSprite;
        public Sprite rifleSprite;
        public Sprite bulletSprite;
        public Sprite muzzleFlashSprite;

        [Header("Sprites e Escalas dos Personagens (Proporção 8-Bits)")]
        public float playerScale = 0.85f;
        public Vector2 playerColliderSize = new Vector2(0.55f, 0.65f);
        public float playerShadowScale = 0.55f;

        public Sprite krabySprite;
        public float krabyScale = 1.50f;
        public float krabyColliderRadius = 0.50f;

        public Sprite zombieSprite;
        public Sprite[] zombieWalkSprites;
        public float zombieScale = 1.30f;
        public float zombieColliderRadius = 0.45f;

        public Sprite birdieSprite;
        public float birdieScale = 1.20f;
        public float birdieColliderRadius = 0.40f;

        [Header("Densidade Procedural")]
        [SerializeField] private int wreckedBoatCount = 2;
        [SerializeField] private int shackCount = 2;
        [SerializeField] private int rustedCarCount = 2;
        [SerializeField] private int coastalPalmCount = 8;
        [SerializeField] private int deadTreeCount = 6;
        [SerializeField] private int coastalRockCount = 6;
        [SerializeField] private int barrelCount = 3;
        [SerializeField] private int driftwoodCount = 8;
        [SerializeField] private int spikeTrapCount = 4;
        [SerializeField] private int supplyNodeCount = 5;

        public int ActiveSeed { get; private set; }

        private int[,] terrainGrid;
        private int gridW;
        private int gridH;

        private void Awake()
        {
            if (generateOnAwake)
            {
                GenerateMap();
            }
        }

        [ContextMenu("Gerar Mapa Procedural")]
        public void GenerateMap()
        {
            ActiveSeed = useRandomSeed
                ? Environment.TickCount ^ UnityEngine.Random.Range(int.MinValue, int.MaxValue)
                : seed;

            var rng = new System.Random(ActiveSeed);
            float offsetShoreX = (float)(rng.NextDouble() * 1000.0);
            float offsetGrassX = (float)(rng.NextDouble() * 1000.0);
            float offsetRimX   = (float)(rng.NextDouble() * 1000.0);
            float offsetTrailX = (float)(rng.NextDouble() * 1000.0);
            float trailPhase   = (float)(rng.NextDouble() * Math.PI * 2.0);

            if (groundTilemap == null || oceanTilemap == null || decoTilemap == null)
            {
                Debug.LogWarning("[CoastalProceduralGenerator] Tilemaps não atribuídos!");
                return;
            }

            ClearExistingMap();

            gridW = (maxX - minX) + 2;
            gridH = (maxY - minY) + 2;
            terrainGrid = new int[gridW, gridH];

            // 1. Esculpir biomas via Perlin Noise (0=Oceano, 1=Areia, 2=Vegetação Costeira, 3=Trilha de Terra)
            for (int ix = 0; ix < gridW; ix++)
            {
                for (int iy = 0; iy < gridH; iy++)
                {
                    int vx = minX + ix;
                    int vy = minY + iy;

                    // Fronteira externa de segurança (sempre oceano profundo para margem da câmera)
                    if (vx <= minX + 3 || vx >= maxX - 5 || vy <= minY + 2 || vy >= maxY - 2)
                    {
                        terrainGrid[ix, iy] = 0;
                        continue;
                    }

                    float nShore = Mathf.PerlinNoise(vy * shoreFrequency + offsetShoreX, 0.37f) * 2f - 1f;
                    float nShoreFine = Mathf.PerlinNoise(vy * shoreFrequency * 2.3f + offsetShoreX, 3.11f) * 2f - 1f;
                    float shoreEdge = shoreBaseX + nShore * shoreAmplitude + nShoreFine * 0.65f;

                    float nWest = Mathf.PerlinNoise(vy * 0.14f + offsetRimX, 13.7f) * 2f - 1f;
                    float nWestFine = Mathf.PerlinNoise(vy * 0.31f + offsetRimX, 17.3f) * 2f - 1f;
                    float westEdge = (minX + 8.5f) + nWest * 2.4f + nWestFine * 0.7f;

                    float nGrass = Mathf.PerlinNoise(vy * grassFrequency + offsetGrassX, 1.73f) * 2f - 1f;
                    float grassEdge = grassBaseX + nGrass * grassAmplitude;
                    // Garante faixa mínima de areia transitável perto do centro
                    grassEdge = Mathf.Min(grassEdge, shoreEdge - 4.2f);

                    float nNorth = Mathf.PerlinNoise(vx * 0.14f + offsetRimX, 5.2f) * 2f - 1f;
                    float nNorthFine = Mathf.PerlinNoise(vx * 0.29f + offsetRimX, 7.1f) * 2f - 1f;
                    float nSouth = Mathf.PerlinNoise(vx * 0.14f + offsetRimX, 9.8f) * 2f - 1f;
                    float nSouthFine = Mathf.PerlinNoise(vx * 0.29f + offsetRimX, 11.4f) * 2f - 1f;
                    float northRim = (maxY - 5.2f) + nNorth * 2.1f + nNorthFine * 0.6f;
                    float southRim = (minY + 6.2f) + nSouth * 2.1f + nSouthFine * 0.6f;

                    // Arredondamento orgânico dos cantos da ilha costeira
                    float normX = (vx - ((westEdge + shoreEdge) * 0.5f)) / Mathf.Max(1f, (shoreEdge - westEdge) * 0.5f);
                    float normY = (vy - ((southRim + northRim) * 0.5f)) / Mathf.Max(1f, (northRim - southRim) * 0.5f);
                    float cornerDist = normX * normX * normX * normX + normY * normY * normY * normY;

                    if (vx > shoreEdge || vx < westEdge || vy > northRim || vy < southRim || cornerDist > 1.35f)
                    {
                        terrainGrid[ix, iy] = 0;
                    }
                    else if (vx > grassEdge || vy > northRim - 2.2f || vy < southRim + 2.2f || vx < westEdge + 2.4f || cornerDist > 0.92f)
                    {
                        terrainGrid[ix, iy] = 1;
                    }
                    else
                    {
                        float nTrail = Mathf.PerlinNoise(vx * trailFrequency + offsetTrailX, 2.4f) * 2f - 1f;
                        float roadCenterY = Mathf.Sin((vx + 4f) * 0.28f + trailPhase) * 2.4f + nTrail * 1.4f;
                        bool onMainTrail = vx <= grassEdge - 1.5f && vx >= minX + 11 && Mathf.Abs(vy - roadCenterY) <= trailWidth;

                        float branchNoise = Mathf.PerlinNoise((vx + 20) * 0.22f + offsetTrailX, (vy + 20) * 0.22f + offsetGrassX);
                        bool onClearing = vx <= grassEdge - 3.0f && vx >= minX + 12 && branchNoise > 0.69f;

                        terrainGrid[ix, iy] = (onMainTrail || onClearing) ? 3 : 2;
                    }
                }
            }

            // 2. Suavização de gradiente (diferença máxima de 1 tier entre vizinhos para Wang Tileset perfeito)
            SmoothTerrainTiers();

            // 3. Pintura dos Tilemaps (Terreno, Oceano Animado, Espuma Costeira Animada)
            var sandCells = new List<Vector3Int>();
            var shoreBeachCells = new List<Vector3Int>();
            var grassCells = new List<Vector3Int>();
            var dirtCells = new List<Vector3Int>();
            var oceanEdgeCells = new List<Vector3Int>();

            TileBase deepOceanTile = oceanAnimatedTile != null ? oceanAnimatedTile : (oceanBeachTiles != null && oceanBeachTiles.Length > 0 ? oceanBeachTiles[0] : null);

            for (int cx = minX; cx <= maxX; cx++)
            {
                for (int cy = minY; cy <= maxY; cy++)
                {
                    int ix = cx - minX;
                    int iy = cy - minY;
                    int sw = terrainGrid[ix, iy];
                    int se = terrainGrid[ix + 1, iy];
                    int nw = terrainGrid[ix, iy + 1];
                    int ne = terrainGrid[ix + 1, iy + 1];

                    int lo = Mathf.Min(Mathf.Min(sw, se), Mathf.Min(nw, ne));
                    int hi = Mathf.Max(Mathf.Max(sw, se), Mathf.Max(nw, ne));
                    var pos = new Vector3Int(cx, cy, 0);

                    if (hi <= 1)
                    {
                        int mask = ((nw == 1 ? 1 : 0) << 3) |
                                   ((ne == 1 ? 1 : 0) << 2) |
                                   ((sw == 1 ? 1 : 0) << 1) |
                                   (se == 1 ? 1 : 0);

                        if (mask == 0)
                        {
                            oceanTilemap.SetTile(pos, deepOceanTile);
                            // Verifica se esta célula oceânica encosta em areia para receber espuma animada
                            if (IsAdjacentToTier(ix, iy, 1))
                            {
                                oceanEdgeCells.Add(pos);
                            }
                        }
                        else
                        {
                            if (oceanBeachTiles != null && oceanBeachTiles.Length > mask && oceanBeachTiles[mask] != null)
                                groundTilemap.SetTile(pos, oceanBeachTiles[mask]);
                            else if (rulePraiaAreia != null)
                                groundTilemap.SetTile(pos, rulePraiaAreia);

                            int sandCorners = (nw == 1 ? 1 : 0) + (ne == 1 ? 1 : 0) + (sw == 1 ? 1 : 0) + (se == 1 ? 1 : 0);
                            if (sandCorners <= 2)
                            {
                                if (oceanBarrierTile != null)
                                    oceanTilemap.SetTile(pos, oceanBarrierTile);
                            }
                            else if (mask == 15)
                            {
                                sandCells.Add(pos);
                                if (IsNearTier(ix, iy, 0, 2))
                                    shoreBeachCells.Add(pos);
                            }
                        }
                    }
                    else if (hi == 2 && lo >= 1)
                    {
                        int mask = ((nw == 2 ? 1 : 0) << 3) |
                                   ((ne == 2 ? 1 : 0) << 2) |
                                   ((sw == 2 ? 1 : 0) << 1) |
                                   (se == 2 ? 1 : 0);

                        if (beachGrassTiles != null && beachGrassTiles.Length > mask && beachGrassTiles[mask] != null)
                            groundTilemap.SetTile(pos, beachGrassTiles[mask]);
                        else if (ruleVegetacaoCosteira != null)
                            groundTilemap.SetTile(pos, ruleVegetacaoCosteira);

                        if (mask == 15) grassCells.Add(pos);
                    }
                    else
                    {
                        int mask = ((nw == 2 ? 1 : 0) << 3) |
                                   ((ne == 2 ? 1 : 0) << 2) |
                                   ((sw == 2 ? 1 : 0) << 1) |
                                   (se == 2 ? 1 : 0);

                        if (dirtGrassTiles != null && dirtGrassTiles.Length > mask && dirtGrassTiles[mask] != null)
                            groundTilemap.SetTile(pos, dirtGrassTiles[mask]);
                        else if (ruleTrilhaTerra != null)
                            groundTilemap.SetTile(pos, ruleTrilhaTerra);

                        if (mask == 0) dirtCells.Add(pos);
                    }
                }
            }

            // 4. Pintar ondas de espuma animadas ao longo da linha costeira
            if (waveFoamTilemap != null && oceanFoamAnimatedTile != null)
            {
                foreach (var cell in oceanEdgeCells)
                {
                    waveFoamTilemap.SetTile(cell, oceanFoamAnimatedTile);
                }
            }

            // 5. Determinar posição segura do Sobrevivente na praia central
            Vector2 playerSpawn = FindBestPlayerSpawn(sandCells);
            var occupiedSpots = new List<Vector2> { playerSpawn };

            // 6. Dispersão Procedural de Detalhes (Driftwood na areia, Spike Traps na borda da grama)
            PlaceDecoTiles(rng, shoreBeachCells, driftwoodTile, driftwoodCount, occupiedSpots, 1.8f);
            PlaceDecoTiles(rng, grassCells, spikeTrapTile, spikeTrapCount, occupiedSpots, 2.5f);

            // 7. Dispersão Procedural de Props e Estruturas
            EnsurePropsContainer();

            // Fogueira de abrigo próxima ao jogador
            Vector2 campfirePos = FindSpotNear(rng, sandCells, playerSpawn, 1.3f, 2.6f, occupiedSpots, 1.1f);
            if (campfireSprite != null)
            {
                SpawnStructure("Shelter_Campfire", campfireSprite, campfirePos, 2, false, Vector2.zero, 1f);
                occupiedSpots.Add(campfirePos);
            }

            // Barcos naufragados na faixa litorânea (perto do mar)
            SpawnPropsBatch(rng, "Wrecked_Boat", wreckedBoatSprite, shoreBeachCells, wreckedBoatCount, 3, true, new Vector2(1.7f, 0.9f), 1f, occupiedSpots, 4.0f, playerSpawn, 3.0f);

            // Casebres abandonados na vegetação / trilhas
            var inlandCells = new List<Vector3Int>(grassCells);
            inlandCells.AddRange(dirtCells);
            SpawnPropsBatch(rng, "Abandoned_Shack", abandonedShackSprite, inlandCells, shackCount, 4, true, new Vector2(1.8f, 1.7f), 1f, occupiedSpots, 5.5f, playerSpawn, 5.0f);

            // Carros enferrujados em clareiras / grama
            SpawnPropsBatch(rng, "Rusted_Car", rustedCarSprite, inlandCells, rustedCarCount, 3, true, new Vector2(1.0f, 1.8f), 1f, occupiedSpots, 4.5f, playerSpawn, 4.5f);

            // Palmeiras costeiras, árvores secas retorcidas e pinheiros costeiros (8-bits aprimorados)
            SpawnPropsBatch(rng, "Coastal_Palm", coastalPalmSprite, grassCells, coastalPalmCount, 5, true, new Vector2(0.55f, 0.45f), 1f, occupiedSpots, 2.8f, playerSpawn, 3.0f);
            SpawnPropsBatch(rng, "Dead_Tree", deadTreeSprite, grassCells, deadTreeCount, 4, true, new Vector2(0.55f, 0.45f), 1f, occupiedSpots, 2.6f, playerSpawn, 3.0f);
            if (coastalPineSprite != null)
            {
                SpawnPropsBatch(rng, "Coastal_Pine", coastalPineSprite, grassCells, 5, 5, true, new Vector2(0.50f, 0.45f), 1f, occupiedSpots, 2.6f, playerSpawn, 3.0f);
            }

            // Rochas costeiras e barris enferrujados
            for (int i = 0; i < coastalRockCount; i++)
            {
                Sprite rockSp = (i % 2 == 0 && seaRockSprite != null) ? seaRockSprite : coastalRockSprite;
                SpawnPropsBatch(rng, $"Coastal_Rock_{i + 1}", rockSp, shoreBeachCells.Count > 0 ? shoreBeachCells : sandCells, 1, 2, true, new Vector2(0.8f, 0.8f), 1f, occupiedSpots, 2.6f, playerSpawn, 2.6f, false);
            }
            SpawnPropsBatch(rng, "Rusted_Barrel", rustedBarrelSprite, inlandCells.Count > 0 ? inlandCells : sandCells, barrelCount, 3, true, new Vector2(0.8f, 0.8f), 1f, occupiedSpots, 3.0f, playerSpawn, 3.0f);

            // 8. Reconstruir Bounds e Collider Composto do Oceano
            groundTilemap.CompressBounds();
            oceanTilemap.CompressBounds();
            decoTilemap.CompressBounds();
            if (waveFoamTilemap != null) waveFoamTilemap.CompressBounds();

            var oceanTmCol = oceanTilemap.GetComponent<TilemapCollider2D>();
            if (oceanTmCol != null) oceanTmCol.ProcessTilemapChanges();
            var oceanCompCol = oceanTilemap.GetComponent<CompositeCollider2D>();
            if (oceanCompCol != null) oceanCompCol.GenerateGeometry();

            // 9. Posicionar/Calibrar Player, Recursos, Armas e Inimigos com Proporções Corretas
            ConfigurePlayerAndCamera(playerSpawn);
            var resourceWaypoints = PlaceProceduralResources(rng, sandCells, grassCells, playerSpawn, occupiedSpots);
            PlaceProceduralWeapons(rng, sandCells, inlandCells, playerSpawn, occupiedSpots);
            PlaceProceduralEnemies(rng, shoreBeachCells, sandCells, inlandCells, playerSpawn, occupiedSpots);

            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null && playerGO.TryGetComponent<PlayerController2D>(out var pc) && resourceWaypoints.Count > 0)
            {
                resourceWaypoints.Add(playerSpawn);
                pc.SetDemoWaypoints(resourceWaypoints.ToArray());
            }

            Debug.Log($"[CoastalProceduralGenerator] Zona Litorânea gerada com sucesso! Seed={ActiveSeed} | Areia={sandCells.Count} | Grama={grassCells.Count} | Trilha={dirtCells.Count} | Espuma={oceanEdgeCells.Count}");
        }

        [ContextMenu("Gerar Mapa com Nova Seed")]
        public void GenerateWithNewRandomSeed()
        {
            seed = UnityEngine.Random.Range(1, 999999);
            useRandomSeed = false;
            GenerateMap();
        }

        private void ClearExistingMap()
        {
            groundTilemap.ClearAllTiles();
            oceanTilemap.ClearAllTiles();
            decoTilemap.ClearAllTiles();
            if (waveFoamTilemap != null) waveFoamTilemap.ClearAllTiles();

            EnsurePropsContainer();
            for (int i = propsContainer.childCount - 1; i >= 0; i--)
            {
                var child = propsContainer.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(child);
                else DestroyImmediate(child);
            }
        }

        private void EnsurePropsContainer()
        {
            if (propsContainer == null)
            {
                var existing = transform.Find("Structures_And_Props");
                if (existing != null)
                {
                    propsContainer = existing;
                }
                else
                {
                    var go = new GameObject("Structures_And_Props");
                    go.transform.SetParent(transform, false);
                    propsContainer = go.transform;
                }
            }
        }

        private void SmoothTerrainTiers()
        {
            bool changed = true;
            int guard = 0;
            while (changed && guard++ < 16)
            {
                changed = false;
                for (int ix = 0; ix < gridW; ix++)
                {
                    for (int iy = 0; iy < gridH; iy++)
                    {
                        int cur = terrainGrid[ix, iy];
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                if (dx == 0 && dy == 0) continue;
                                int nx = ix + dx, ny = iy + dy;
                                if (nx < 0 || nx >= gridW || ny < 0 || ny >= gridH) continue;
                                int nb = terrainGrid[nx, ny];
                                if (cur - nb > 1)
                                {
                                    terrainGrid[ix, iy] = nb + 1;
                                    cur = terrainGrid[ix, iy];
                                    changed = true;
                                }
                            }
                        }
                    }
                }
            }
        }

        private bool IsAdjacentToTier(int ix, int iy, int targetTier)
        {
            return IsNearTier(ix, iy, targetTier, 1);
        }

        private bool IsNearTier(int ix, int iy, int targetTier, int radius)
        {
            for (int dx = -radius; dx <= radius + 1; dx++)
            {
                for (int dy = -radius; dy <= radius + 1; dy++)
                {
                    int nx = ix + dx;
                    int ny = iy + dy;
                    if (nx >= 0 && nx < gridW && ny >= 0 && ny < gridH)
                    {
                        if (terrainGrid[nx, ny] == targetTier) return true;
                    }
                }
            }
            return false;
        }

        private Vector2 FindBestPlayerSpawn(List<Vector3Int> sandCells)
        {
            Vector2 best = Vector2.zero;
            float bestDist = float.MaxValue;
            foreach (var c in sandCells)
            {
                int ix = c.x - minX;
                int iy = c.y - minY;
                // Evita células imediatamente encostadas no mar
                if (IsNearTier(ix, iy, 0, 1)) continue;
                Vector2 worldPos = new Vector2(c.x + 0.5f, c.y + 0.5f);
                float d = worldPos.sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = worldPos;
                }
            }
            return best;
        }

        private Vector2 FindSpotNear(System.Random rng, List<Vector3Int> pool, Vector2 anchor, float minDist, float maxDist, List<Vector2> occupied, float minClearance)
        {
            var candidates = new List<Vector2>();
            foreach (var c in pool)
            {
                Vector2 p = new Vector2(c.x + 0.5f, c.y + 0.5f);
                float d = Vector2.Distance(p, anchor);
                if (d >= minDist && d <= maxDist && HasClearance(p, occupied, minClearance))
                {
                    candidates.Add(p);
                }
            }
            if (candidates.Count > 0)
            {
                return candidates[rng.Next(candidates.Count)];
            }
            return anchor + new Vector2(-1.4f, 0.8f);
        }

        private bool HasClearance(Vector2 pos, List<Vector2> occupied, float minDist)
        {
            float sqr = minDist * minDist;
            for (int i = 0; i < occupied.Count; i++)
            {
                if ((occupied[i] - pos).sqrMagnitude < sqr) return false;
            }
            return true;
        }

        private void PlaceDecoTiles(System.Random rng, List<Vector3Int> pool, TileBase tile, int count, List<Vector2> occupied, float clearance)
        {
            if (tile == null || pool == null || pool.Count == 0) return;
            int placed = 0;
            int attempts = count * 12;
            while (placed < count && attempts-- > 0)
            {
                var cell = pool[rng.Next(pool.Count)];
                Vector2 p = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                if (!HasClearance(p, occupied, clearance)) continue;
                decoTilemap.SetTile(cell, tile);
                occupied.Add(p);
                placed++;
            }
        }

        private void SpawnPropsBatch(
            System.Random rng,
            string baseName,
            Sprite sp,
            List<Vector3Int> pool,
            int count,
            int sortOrder,
            bool solidCollider,
            Vector2 colSize,
            float scale,
            List<Vector2> occupied,
            float clearance,
            Vector2 playerSpawn,
            float minPlayerDist,
            bool appendIndex = true)
        {
            if (sp == null || pool == null || pool.Count == 0) return;
            int placed = 0;
            int attempts = count * 20;
            while (placed < count && attempts-- > 0)
            {
                var cell = pool[rng.Next(pool.Count)];
                Vector2 p = new Vector2(cell.x + 0.5f, cell.y + 0.5f);
                if (Vector2.Distance(p, playerSpawn) < minPlayerDist) continue;
                if (!HasClearance(p, occupied, clearance)) continue;

                string objName = appendIndex ? $"{baseName}_{placed + 1}" : baseName;
                SpawnStructure(objName, sp, p, sortOrder, solidCollider, colSize, scale);
                occupied.Add(p);
                placed++;
            }
        }

        private void SpawnStructure(string objName, Sprite sp, Vector2 pos, int sortOrder, bool solidCollider, Vector2 colSize, float scale = 1f)
        {
            var go = new GameObject(objName);
            go.transform.SetParent(propsContainer, false);
            go.transform.position = new Vector3(pos.x, pos.y, 0f);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = sortOrder;

            bool isTree = objName.StartsWith("Coastal_Palm") || objName.StartsWith("Dead_Tree") || objName.StartsWith("Coastal_Pine");

            // Dynamic Y-Sorting: ordena pela base do objeto
            var ysort = go.AddComponent<YSort2D>();
            ysort.isStatic = true;
            if (isTree)
            {
                ysort.pivotOffsetY = -0.75f;
            }
            else
            {
                ysort.pivotOffsetY = -colSize.y * 0.45f;
            }

            if (solidCollider)
            {
                var bc = go.AddComponent<BoxCollider2D>();
                if (isTree)
                {
                    // A base do tronco tem colisor menor e baixo para permitir que o player ande atrás da copa
                    bc.size = new Vector2(0.40f, 0.30f);
                    bc.offset = new Vector2(0f, -0.65f);
                }
                else
                {
                    bc.size = colSize;
                }
            }

            // Para árvores: adiciona sensor da copa com efeito suave de semi-transparência ao passar por baixo/atrás das folhas
            if (isTree)
            {
                var canopyCol = go.AddComponent<CircleCollider2D>();
                canopyCol.isTrigger = true;
                canopyCol.offset = new Vector2(0f, 0.35f);
                canopyCol.radius = 0.65f;
                go.AddComponent<TreeCanopyFader2D>();
            }
        }

        private void ConfigurePlayerAndCamera(Vector2 playerSpawn)
        {
            var playerGO = GameObject.FindWithTag("Player");
            if (playerGO != null)
            {
                playerGO.transform.position = new Vector3(playerSpawn.x, playerSpawn.y, 0f);
                playerGO.transform.localScale = new Vector3(playerScale, playerScale, 1f);

                var ysort = playerGO.GetComponent<YSort2D>();
                if (ysort == null) ysort = playerGO.AddComponent<YSort2D>();
                ysort.pivotOffsetY = -0.36f;
                ysort.isStatic = false;

                var col = playerGO.GetComponent<BoxCollider2D>();
                if (col != null) col.size = playerColliderSize;

                var shadow = playerGO.transform.Find("Shadow");
                if (shadow != null)
                {
                    shadow.localScale = new Vector3(playerShadowScale, playerShadowScale, 1f);
                    shadow.localPosition = new Vector3(0f, -0.36f, 0f);
                }

                var cam = Camera.main;
                if (cam != null)
                {
                    cam.transform.position = new Vector3(playerSpawn.x, playerSpawn.y, -10f);
                }

                // Configurar Controlador das 3 Primeiras Armas (Pistola, Escopeta, Fuzil)
                var wc = playerGO.GetComponent<WeaponController2D>();
                if (wc == null) wc = playerGO.AddComponent<WeaponController2D>();
                wc.bulletSprite = bulletSprite;
                wc.muzzleFlashSprite = muzzleFlashSprite;
                wc.weapons = new WeaponSpec[]
                {
                    new WeaponSpec
                    {
                        name = "Pistola M1911",
                        sprite = pistolSprite,
                        damage = 25f,
                        fireRate = 4.0f,
                        projectileSpeed = 16f,
                        pelletsPerShot = 1,
                        spreadAngle = 2f,
                        magSize = 12,
                        currentAmmo = 12,
                        reserveAmmo = 48,
                        automatic = false,
                        holdRadius = 0.48f,
                        spriteScale = 0.42f
                    },
                    new WeaponSpec
                    {
                        name = "Escopeta Costeira",
                        sprite = shotgunSprite,
                        damage = 18f,
                        fireRate = 1.3f,
                        projectileSpeed = 15f,
                        pelletsPerShot = 4,
                        spreadAngle = 20f,
                        magSize = 6,
                        currentAmmo = 6,
                        reserveAmmo = 24,
                        automatic = false,
                        holdRadius = 0.52f,
                        spriteScale = 0.55f
                    },
                    new WeaponSpec
                    {
                        name = "Fuzil de Assalto",
                        sprite = rifleSprite,
                        damage = 30f,
                        fireRate = 8.0f,
                        projectileSpeed = 20f,
                        pelletsPerShot = 1,
                        spreadAngle = 3.5f,
                        magSize = 24,
                        currentAmmo = 24,
                        reserveAmmo = 96,
                        automatic = true,
                        holdRadius = 0.54f,
                        spriteScale = 0.60f
                    }
                };
                wc.EquipWeapon(wc.currentWeaponIndex < 0 ? 0 : wc.currentWeaponIndex);
            }
        }

        private void PlaceProceduralWeapons(System.Random rng, List<Vector3Int> sandCells, List<Vector3Int> inlandCells, Vector2 playerSpawn, List<Vector2> occupied)
        {
            var pool = new List<Vector3Int>(sandCells);
            if (inlandCells != null && inlandCells.Count > 0) pool.AddRange(inlandCells);

            string[] pickupNames = { "Pickup_Pistola_M1911", "Pickup_Escopeta_Costeira", "Pickup_Fuzil_Assalto" };
            string[] labels = { "Pistola M1911", "Escopeta Costeira", "Fuzil de Assalto" };
            Sprite[] sprites = { pistolSprite, shotgunSprite, rifleSprite };

            for (int i = 0; i < 3; i++)
            {
                var pGO = GameObject.Find(pickupNames[i]);
                if (pGO == null)
                {
                    pGO = new GameObject(pickupNames[i]);
                    var sr = pGO.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 4;
                    var col = pGO.AddComponent<CircleCollider2D>();
                    col.isTrigger = true;
                    col.radius = 0.55f;
                    pGO.AddComponent<WeaponPickup2D>();
                    var ysort = pGO.AddComponent<YSort2D>();
                    ysort.pivotOffsetY = -0.2f;
                    ysort.isStatic = true;
                }

                var pSr = pGO.GetComponent<SpriteRenderer>();
                if (pSr != null && sprites[i] != null) pSr.sprite = sprites[i];
                pGO.transform.localScale = new Vector3(0.65f, 0.65f, 1f);

                Vector2 pos = FindSpotNear(rng, pool, playerSpawn, 1.8f, 3.8f, occupied, 1.4f);
                occupied.Add(pos);

                var pickup = pGO.GetComponent<WeaponPickup2D>();
                if (pickup != null)
                {
                    pickup.weaponIndex = i;
                    pickup.weaponLabel = labels[i];
                    pickup.SetBasePosition(new Vector3(pos.x, pos.y, 0f));
                }
                else
                {
                    pGO.transform.position = new Vector3(pos.x, pos.y, 0f);
                }
            }
        }

        private List<Vector2> PlaceProceduralResources(System.Random rng, List<Vector3Int> sandCells, List<Vector3Int> grassCells, Vector2 playerSpawn, List<Vector2> occupied)
        {
            var placedPositions = new List<Vector2>();
            var pool = new List<Vector3Int>(sandCells);
            pool.AddRange(grassCells);

            for (int i = 0; i < supplyNodeCount; i++)
            {
                string rName = $"Resource_Node_{i + 1}";
                var resGO = GameObject.Find(rName);
                if (resGO == null)
                {
                    resGO = new GameObject(rName);
                    var rSr = resGO.AddComponent<SpriteRenderer>();
                    rSr.color = Color.white;
                    if (supplyCrateSprite != null) rSr.sprite = supplyCrateSprite;
                    rSr.sortingOrder = 4;

                    var rCol = resGO.AddComponent<CircleCollider2D>();
                    rCol.isTrigger = true;
                    rCol.radius = 0.5f;

                    var rItem = resGO.AddComponent<ResourceItem2D>();
                    rItem.amount = 1;
                    rItem.resourceName = $"Suprimento #{i + 1}";

                    var ysort = resGO.AddComponent<YSort2D>();
                    ysort.pivotOffsetY = -0.25f;
                    ysort.isStatic = true;
                }

                Vector2 pos = FindSpotNear(rng, pool, playerSpawn, 2.0f, 7.5f, occupied, 1.8f);
                resGO.transform.position = new Vector3(pos.x, pos.y, 0f);
                resGO.transform.localScale = new Vector3(0.80f, 0.80f, 1f);
                occupied.Add(pos);
                placedPositions.Add(pos);
            }

            return placedPositions;
        }

        private void PlaceProceduralEnemies(System.Random rng, List<Vector3Int> shoreCells, List<Vector3Int> sandCells, List<Vector3Int> inlandCells, Vector2 playerSpawn, List<Vector2> occupied)
        {
            var playerGO = GameObject.FindWithTag("Player");
            Transform playerTr = playerGO != null ? playerGO.transform : null;

            // Kraby: próximo à areia/orla (grande caranguejo mutante: 1.50x)
            Vector2 krabyPos = FindSpotNear(rng, shoreCells.Count > 0 ? shoreCells : sandCells, playerSpawn, 2.4f, 4.8f, occupied, 1.8f);
            ConfigureEnemy("Mutante_Kraby", krabySprite, krabyPos, krabyScale, krabyColliderRadius, playerTr, 15f, 1.3f, 4.2f);
            occupied.Add(krabyPos);

            // Zombie: na vegetação / próximo a trilhas e casebres (1.30x)
            Vector2 zombiePos = FindSpotNear(rng, inlandCells.Count > 0 ? inlandCells : sandCells, playerSpawn, 3.0f, 6.2f, occupied, 2.0f);
            ConfigureEnemy("Mutante_Zombie", zombieSprite, zombiePos, zombieScale, zombieColliderRadius, playerTr, 18f, 1.45f, 7.5f, zombieWalkSprites);
            occupied.Add(zombiePos);

            // Birdie: patrulhando a areia/orla sul ou norte (1.20x)
            Vector2 birdiePos = FindSpotNear(rng, sandCells, playerSpawn, 2.8f, 5.8f, occupied, 2.0f);
            ConfigureEnemy("Mutante_Birdie", birdieSprite, birdiePos, birdieScale, birdieColliderRadius, playerTr, 14f, 1.55f, 4.8f);
            occupied.Add(birdiePos);
        }

        private void ConfigureEnemy(string eName, Sprite sprite, Vector2 pos, float scale, float colRadius, Transform targetPlayer, float damage, float speed, float detectRadius, Sprite[] walkSprites = null)
        {
            var eGO = GameObject.Find(eName);
            if (eGO == null) eGO = new GameObject(eName);
            eGO.transform.position = new Vector3(pos.x, pos.y, 0f);
            eGO.transform.localScale = new Vector3(scale, scale, 1f);

            var eSr = eGO.GetComponent<SpriteRenderer>();
            if (eSr == null) eSr = eGO.AddComponent<SpriteRenderer>();
            eSr.color = Color.white;
            if (sprite != null) eSr.sprite = sprite;
            eSr.sortingOrder = 6;

            var ysort = eGO.GetComponent<YSort2D>();
            if (ysort == null) ysort = eGO.AddComponent<YSort2D>();
            ysort.pivotOffsetY = -colRadius * 0.5f;
            ysort.isStatic = false;

            var eCol = eGO.GetComponent<CircleCollider2D>();
            if (eCol == null) eCol = eGO.AddComponent<CircleCollider2D>();
            eCol.radius = colRadius;

            var eRb = eGO.GetComponent<Rigidbody2D>();
            if (eRb == null) eRb = eGO.AddComponent<Rigidbody2D>();
            eRb.gravityScale = 0f;
            eRb.freezeRotation = true;

            var eMut = eGO.GetComponent<MutantEnemy2D>();
            if (eMut == null) eMut = eGO.AddComponent<MutantEnemy2D>();
            eMut.mutantName = eName;
            eMut.targetPlayer = targetPlayer;
            eMut.damage = damage;
            eMut.moveSpeed = speed;
            eMut.detectionRadius = detectRadius;
            eMut.walkSprites = walkSprites;
            eMut.SetOrigin(pos);
        }
    }
}
