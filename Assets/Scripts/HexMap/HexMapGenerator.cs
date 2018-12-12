using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HexMap
{
    public class HexMapGenerator : MonoBehaviour
    {
        public enum HemisphereMode
        {
            Both,
            North,
            South
        }

        private static readonly float[] temperatureBands = {0.1f, 0.3f, 0.6f};

        private static readonly float[] moistureBands = {0.12f, 0.28f, 0.85f};

        private static readonly Biome[] biomes =
        {
            new Biome(terrain: 0, plant: 0), new Biome(terrain: 4, plant: 0), new Biome(terrain: 4, plant: 0),
            new Biome(terrain: 4, plant: 0),
            new Biome(terrain: 0, plant: 0), new Biome(terrain: 2, plant: 0), new Biome(terrain: 2, plant: 1),
            new Biome(terrain: 2, plant: 2),
            new Biome(terrain: 0, plant: 0), new Biome(terrain: 1, plant: 0), new Biome(terrain: 1, plant: 1),
            new Biome(terrain: 1, plant: 2),
            new Biome(terrain: 0, plant: 0), new Biome(terrain: 1, plant: 1), new Biome(terrain: 1, plant: 2),
            new Biome(terrain: 1, plant: 3)
        };

        private readonly List<HexDirection> flowDirections = new List<HexDirection>();

        private int cellCount, landCells;

        [Range(min: 20, max: 200)]
        public int chunkSizeMax = 100;

        [Range(min: 20, max: 200)]
        public int chunkSizeMin = 30;

        private List<ClimateData> climate = new List<ClimateData>();

        [Range(min: 6, max: 10)]
        public int elevationMaximum = 8;

        [Range(min: -4, max: 0)]
        public int elevationMinimum = -2;

        [Range(min: 0, max: 100)]
        public int erosionPercentage = 50;

        [Range(min: 0f, max: 1f)]
        public float evaporationFactor = 0.5f;

        [Range(min: 0f, max: 1f)]
        public float extraLakeProbability = 0.25f;

        public HexGrid grid;

        public HemisphereMode hemisphere;

        [Range(min: 0f, max: 1f)]
        public float highRiseProbability = 0.25f;

        [Range(min: 0f, max: 1f)]
        public float highTemperature = 1f;

        [Range(min: 0f, max: 0.5f)]
        public float jitterProbability = 0.25f;

        [Range(min: 5, max: 95)]
        public int landPercentage = 50;

        [Range(min: 0f, max: 1f)]
        public float lowTemperature;

        [Range(min: 0, max: 10)]
        public int mapBorderX = 5;

        [Range(min: 0, max: 10)]
        public int mapBorderZ = 5;

        private List<ClimateData> nextClimate = new List<ClimateData>();

        [Range(min: 0f, max: 1f)]
        public float precipitationFactor = 0.25f;

        [Range(min: 0, max: 10)]
        public int regionBorder = 5;

        [Range(min: 1, max: 4)]
        public int regionCount = 1;

        private List<MapRegion> regions;

        [Range(min: 0, max: 20)]
        public int riverPercentage = 10;

        [Range(min: 0f, max: 1f)]
        public float runoffFactor = 0.25f;

        private HexCellPriorityQueue searchFrontier;

        private int searchFrontierPhase;

        public int seed;

        [Range(min: 0f, max: 1f)]
        public float seepageFactor = 0.125f;

        [Range(min: 0f, max: 0.4f)]
        public float sinkProbability = 0.2f;

        [Range(min: 0f, max: 1f)]
        public float startingMoisture = 0.1f;

        [Range(min: 0f, max: 1f)]
        public float temperatureJitter = 0.1f;

        private int temperatureJitterChannel;

        public bool useFixedSeed;

        [Range(min: 1, max: 5)]
        public int waterLevel = 3;

        public HexDirection windDirection = HexDirection.NW;

        [Range(min: 1f, max: 10f)]
        public float windStrength = 4f;

        public void GenerateMap(int x, int z, bool wrapping)
        {
            var originalRandomState = Random.state;
            if (!useFixedSeed)
            {
                seed = Random.Range(min: 0, max: int.MaxValue);
                seed ^= (int) DateTime.Now.Ticks;
                seed ^= (int) Time.unscaledTime;
                seed &= int.MaxValue;
            }

            Random.InitState(seed: seed);

            cellCount = x * z;
            grid.CreateMap(x: x, z: z, wrapping: wrapping);
            if (searchFrontier == null)
            {
                searchFrontier = new HexCellPriorityQueue();
            }

            for (var i = 0; i < cellCount; i++)
            {
                grid.GetCell(cellIndex: i).WaterLevel = waterLevel;
            }

            CreateRegions();
            CreateLand();
            ErodeLand();
            CreateClimate();
            CreateRivers();
            SetTerrainType();
            for (var i = 0; i < cellCount; i++)
            {
                grid.GetCell(cellIndex: i).SearchPhase = 0;
            }

            Random.state = originalRandomState;
        }

        private void CreateRegions()
        {
            if (regions == null)
            {
                regions = new List<MapRegion>();
            }
            else
            {
                regions.Clear();
            }

            var borderX = grid.wrapping ? regionBorder : mapBorderX;
            MapRegion region;
            switch (regionCount)
            {
                default:
                    if (grid.wrapping)
                    {
                        borderX = 0;
                    }

                    region.xMin = borderX;
                    region.xMax = grid.cellCountX - borderX;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ - mapBorderZ;
                    regions.Add(item: region);
                    break;
                case 2:
                    if (Random.value < 0.5f)
                    {
                        region.xMin = borderX;
                        region.xMax = grid.cellCountX / 2 - regionBorder;
                        region.zMin = mapBorderZ;
                        region.zMax = grid.cellCountZ - mapBorderZ;
                        regions.Add(item: region);
                        region.xMin = grid.cellCountX / 2 + regionBorder;
                        region.xMax = grid.cellCountX - borderX;
                        regions.Add(item: region);
                    }
                    else
                    {
                        if (grid.wrapping)
                        {
                            borderX = 0;
                        }

                        region.xMin = borderX;
                        region.xMax = grid.cellCountX - borderX;
                        region.zMin = mapBorderZ;
                        region.zMax = grid.cellCountZ / 2 - regionBorder;
                        regions.Add(item: region);
                        region.zMin = grid.cellCountZ / 2 + regionBorder;
                        region.zMax = grid.cellCountZ - mapBorderZ;
                        regions.Add(item: region);
                    }

                    break;
                case 3:
                    region.xMin = borderX;
                    region.xMax = grid.cellCountX / 3 - regionBorder;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ - mapBorderZ;
                    regions.Add(item: region);
                    region.xMin = grid.cellCountX / 3 + regionBorder;
                    region.xMax = grid.cellCountX * 2 / 3 - regionBorder;
                    regions.Add(item: region);
                    region.xMin = grid.cellCountX * 2 / 3 + regionBorder;
                    region.xMax = grid.cellCountX - borderX;
                    regions.Add(item: region);
                    break;
                case 4:
                    region.xMin = borderX;
                    region.xMax = grid.cellCountX / 2 - regionBorder;
                    region.zMin = mapBorderZ;
                    region.zMax = grid.cellCountZ / 2 - regionBorder;
                    regions.Add(item: region);
                    region.xMin = grid.cellCountX / 2 + regionBorder;
                    region.xMax = grid.cellCountX - borderX;
                    regions.Add(item: region);
                    region.zMin = grid.cellCountZ / 2 + regionBorder;
                    region.zMax = grid.cellCountZ - mapBorderZ;
                    regions.Add(item: region);
                    region.xMin = borderX;
                    region.xMax = grid.cellCountX / 2 - regionBorder;
                    regions.Add(item: region);
                    break;
            }
        }

        private void CreateLand()
        {
            var landBudget = Mathf.RoundToInt(f: cellCount * landPercentage * 0.01f);
            landCells = landBudget;
            for (var guard = 0; guard < 10000; guard++)
            {
                var sink = Random.value < sinkProbability;
                for (var i = 0; i < regions.Count; i++)
                {
                    var region = regions[index: i];
                    var chunkSize = Random.Range(min: chunkSizeMin, max: chunkSizeMax - 1);
                    if (sink)
                    {
                        landBudget = SinkTerrain(chunkSize: chunkSize, budget: landBudget, region: region);
                    }
                    else
                    {
                        landBudget = RaiseTerrain(chunkSize: chunkSize, budget: landBudget, region: region);
                        if (landBudget == 0)
                        {
                            return;
                        }
                    }
                }
            }

            if (landBudget > 0)
            {
                Debug.LogWarning(message: "Failed to use up " + landBudget + " land budget.");
                landCells -= landBudget;
            }
        }

        private int RaiseTerrain(int chunkSize, int budget, MapRegion region)
        {
            searchFrontierPhase += 1;
            var firstCell = GetRandomCell(region: region);
            firstCell.SearchPhase = searchFrontierPhase;
            firstCell.Distance = 0;
            firstCell.SearchHeuristic = 0;
            searchFrontier.Enqueue(cell: firstCell);
            var center = firstCell.coordinates;

            var rise = Random.value < highRiseProbability ? 2 : 1;
            var size = 0;
            while (size < chunkSize && searchFrontier.Count > 0)
            {
                var current = searchFrontier.Dequeue();
                var originalElevation = current.Elevation;
                var newElevation = originalElevation + rise;
                if (newElevation > elevationMaximum)
                {
                    continue;
                }

                current.Elevation = newElevation;
                if (
                    originalElevation < waterLevel &&
                    newElevation >= waterLevel && --budget == 0
                )
                {
                    break;
                }

                size += 1;

                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbor = current.GetNeighbor(direction: d);
                    if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                    {
                        neighbor.SearchPhase = searchFrontierPhase;
                        neighbor.Distance = neighbor.coordinates.DistanceTo(other: center);
                        neighbor.SearchHeuristic =
                            Random.value < jitterProbability ? 1 : 0;
                        searchFrontier.Enqueue(cell: neighbor);
                    }
                }
            }

            searchFrontier.Clear();
            return budget;
        }

        private int SinkTerrain(int chunkSize, int budget, MapRegion region)
        {
            searchFrontierPhase += 1;
            var firstCell = GetRandomCell(region: region);
            firstCell.SearchPhase = searchFrontierPhase;
            firstCell.Distance = 0;
            firstCell.SearchHeuristic = 0;
            searchFrontier.Enqueue(cell: firstCell);
            var center = firstCell.coordinates;

            var sink = Random.value < highRiseProbability ? 2 : 1;
            var size = 0;
            while (size < chunkSize && searchFrontier.Count > 0)
            {
                var current = searchFrontier.Dequeue();
                var originalElevation = current.Elevation;
                var newElevation = current.Elevation - sink;
                if (newElevation < elevationMinimum)
                {
                    continue;
                }

                current.Elevation = newElevation;
                if (
                    originalElevation >= waterLevel &&
                    newElevation < waterLevel
                )
                {
                    budget += 1;
                }

                size += 1;

                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbor = current.GetNeighbor(direction: d);
                    if (neighbor && neighbor.SearchPhase < searchFrontierPhase)
                    {
                        neighbor.SearchPhase = searchFrontierPhase;
                        neighbor.Distance = neighbor.coordinates.DistanceTo(other: center);
                        neighbor.SearchHeuristic =
                            Random.value < jitterProbability ? 1 : 0;
                        searchFrontier.Enqueue(cell: neighbor);
                    }
                }
            }

            searchFrontier.Clear();
            return budget;
        }

        private void ErodeLand()
        {
            var erodibleCells = ListPool<HexCell>.Get();
            for (var i = 0; i < cellCount; i++)
            {
                var cell = grid.GetCell(cellIndex: i);
                if (IsErodible(cell: cell))
                {
                    erodibleCells.Add(item: cell);
                }
            }

            var targetErodibleCount =
                (int) (erodibleCells.Count * (100 - erosionPercentage) * 0.01f);

            while (erodibleCells.Count > targetErodibleCount)
            {
                var index = Random.Range(min: 0, max: erodibleCells.Count);
                var cell = erodibleCells[index: index];
                var targetCell = GetErosionTarget(cell: cell);

                cell.Elevation -= 1;
                targetCell.Elevation += 1;

                if (!IsErodible(cell: cell))
                {
                    erodibleCells[index: index] = erodibleCells[index: erodibleCells.Count - 1];
                    erodibleCells.RemoveAt(index: erodibleCells.Count - 1);
                }

                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbor = cell.GetNeighbor(direction: d);
                    if (
                        neighbor && neighbor.Elevation == cell.Elevation + 2 &&
                        !erodibleCells.Contains(item: neighbor)
                    )
                    {
                        erodibleCells.Add(item: neighbor);
                    }
                }

                if (IsErodible(cell: targetCell) && !erodibleCells.Contains(item: targetCell))
                {
                    erodibleCells.Add(item: targetCell);
                }

                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbor = targetCell.GetNeighbor(direction: d);
                    if (
                        neighbor && neighbor != cell &&
                        neighbor.Elevation == targetCell.Elevation + 1 &&
                        !IsErodible(cell: neighbor)
                    )
                    {
                        erodibleCells.Remove(item: neighbor);
                    }
                }
            }

            ListPool<HexCell>.Add(list: erodibleCells);
        }

        private bool IsErodible(HexCell cell)
        {
            var erodibleElevation = cell.Elevation - 2;
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = cell.GetNeighbor(direction: d);
                if (neighbor && neighbor.Elevation <= erodibleElevation)
                {
                    return true;
                }
            }

            return false;
        }

        private HexCell GetErosionTarget(HexCell cell)
        {
            var candidates = ListPool<HexCell>.Get();
            var erodibleElevation = cell.Elevation - 2;
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = cell.GetNeighbor(direction: d);
                if (neighbor && neighbor.Elevation <= erodibleElevation)
                {
                    candidates.Add(item: neighbor);
                }
            }

            var target = candidates[index: Random.Range(min: 0, max: candidates.Count)];
            ListPool<HexCell>.Add(list: candidates);
            return target;
        }

        private void CreateClimate()
        {
            climate.Clear();
            nextClimate.Clear();
            var initialData = new ClimateData();
            initialData.moisture = startingMoisture;
            var clearData = new ClimateData();
            for (var i = 0; i < cellCount; i++)
            {
                climate.Add(item: initialData);
                nextClimate.Add(item: clearData);
            }

            for (var cycle = 0; cycle < 40; cycle++)
            {
                for (var i = 0; i < cellCount; i++)
                {
                    EvolveClimate(cellIndex: i);
                }

                var swap = climate;
                climate = nextClimate;
                nextClimate = swap;
            }
        }

        private void EvolveClimate(int cellIndex)
        {
            var cell = grid.GetCell(cellIndex: cellIndex);
            var cellClimate = climate[index: cellIndex];

            if (cell.IsUnderwater)
            {
                cellClimate.moisture = 1f;
                cellClimate.clouds += evaporationFactor;
            }
            else
            {
                var evaporation = cellClimate.moisture * evaporationFactor;
                cellClimate.moisture -= evaporation;
                cellClimate.clouds += evaporation;
            }

            var precipitation = cellClimate.clouds * precipitationFactor;
            cellClimate.clouds -= precipitation;
            cellClimate.moisture += precipitation;

            var cloudMaximum = 1f - cell.ViewElevation / (elevationMaximum + 1f);
            if (cellClimate.clouds > cloudMaximum)
            {
                cellClimate.moisture += cellClimate.clouds - cloudMaximum;
                cellClimate.clouds = cloudMaximum;
            }

            var mainDispersalDirection = windDirection.Opposite();
            var cloudDispersal = cellClimate.clouds * (1f / (5f + windStrength));
            var runoff = cellClimate.moisture * runoffFactor * (1f / 6f);
            var seepage = cellClimate.moisture * seepageFactor * (1f / 6f);
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                var neighbor = cell.GetNeighbor(direction: d);
                if (!neighbor)
                {
                    continue;
                }

                var neighborClimate = nextClimate[index: neighbor.Index];
                if (d == mainDispersalDirection)
                {
                    neighborClimate.clouds += cloudDispersal * windStrength;
                }
                else
                {
                    neighborClimate.clouds += cloudDispersal;
                }

                var elevationDelta = neighbor.ViewElevation - cell.ViewElevation;
                if (elevationDelta < 0)
                {
                    cellClimate.moisture -= runoff;
                    neighborClimate.moisture += runoff;
                }
                else if (elevationDelta == 0)
                {
                    cellClimate.moisture -= seepage;
                    neighborClimate.moisture += seepage;
                }

                nextClimate[index: neighbor.Index] = neighborClimate;
            }

            var nextCellClimate = nextClimate[index: cellIndex];
            nextCellClimate.moisture += cellClimate.moisture;
            if (nextCellClimate.moisture > 1f)
            {
                nextCellClimate.moisture = 1f;
            }

            nextClimate[index: cellIndex] = nextCellClimate;
            climate[index: cellIndex] = new ClimateData();
        }

        private void CreateRivers()
        {
            var riverOrigins = ListPool<HexCell>.Get();
            for (var i = 0; i < cellCount; i++)
            {
                var cell = grid.GetCell(cellIndex: i);
                if (cell.IsUnderwater)
                {
                    continue;
                }

                var data = climate[index: i];
                var weight =
                    data.moisture * (cell.Elevation - waterLevel) /
                    (elevationMaximum - waterLevel);
                if (weight > 0.75f)
                {
                    riverOrigins.Add(item: cell);
                    riverOrigins.Add(item: cell);
                }

                if (weight > 0.5f)
                {
                    riverOrigins.Add(item: cell);
                }

                if (weight > 0.25f)
                {
                    riverOrigins.Add(item: cell);
                }
            }

            var riverBudget = Mathf.RoundToInt(f: landCells * riverPercentage * 0.01f);
            while (riverBudget > 0 && riverOrigins.Count > 0)
            {
                var index = Random.Range(min: 0, max: riverOrigins.Count);
                var lastIndex = riverOrigins.Count - 1;
                var origin = riverOrigins[index: index];
                riverOrigins[index: index] = riverOrigins[index: lastIndex];
                riverOrigins.RemoveAt(index: lastIndex);

                if (!origin.HasRiver)
                {
                    var isValidOrigin = true;
                    for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                    {
                        var neighbor = origin.GetNeighbor(direction: d);
                        if (neighbor && (neighbor.HasRiver || neighbor.IsUnderwater))
                        {
                            isValidOrigin = false;
                            break;
                        }
                    }

                    if (isValidOrigin)
                    {
                        riverBudget -= CreateRiver(origin: origin);
                    }
                }
            }

            if (riverBudget > 0)
            {
                Debug.LogWarning(message: "Failed to use up river budget.");
            }

            ListPool<HexCell>.Add(list: riverOrigins);
        }

        private int CreateRiver(HexCell origin)
        {
            var length = 1;
            var cell = origin;
            var direction = HexDirection.NE;
            while (!cell.IsUnderwater)
            {
                var minNeighborElevation = int.MaxValue;
                flowDirections.Clear();
                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbor = cell.GetNeighbor(direction: d);
                    if (!neighbor)
                    {
                        continue;
                    }

                    if (neighbor.Elevation < minNeighborElevation)
                    {
                        minNeighborElevation = neighbor.Elevation;
                    }

                    if (neighbor == origin || neighbor.HasIncomingRiver)
                    {
                        continue;
                    }

                    var delta = neighbor.Elevation - cell.Elevation;
                    if (delta > 0)
                    {
                        continue;
                    }

                    if (neighbor.HasOutgoingRiver)
                    {
                        cell.SetOutgoingRiver(direction: d);
                        return length;
                    }

                    if (delta < 0)
                    {
                        flowDirections.Add(item: d);
                        flowDirections.Add(item: d);
                        flowDirections.Add(item: d);
                    }

                    if (
                        length == 1 ||
                        d != direction.Next2() && d != direction.Previous2()
                    )
                    {
                        flowDirections.Add(item: d);
                    }

                    flowDirections.Add(item: d);
                }

                if (flowDirections.Count == 0)
                {
                    if (length == 1)
                    {
                        return 0;
                    }

                    if (minNeighborElevation >= cell.Elevation)
                    {
                        cell.WaterLevel = minNeighborElevation;
                        if (minNeighborElevation == cell.Elevation)
                        {
                            cell.Elevation = minNeighborElevation - 1;
                        }
                    }

                    break;
                }

                direction = flowDirections[index: Random.Range(min: 0, max: flowDirections.Count)];
                cell.SetOutgoingRiver(direction: direction);
                length += 1;

                if (
                    minNeighborElevation >= cell.Elevation &&
                    Random.value < extraLakeProbability
                )
                {
                    cell.WaterLevel = cell.Elevation;
                    cell.Elevation -= 1;
                }

                cell = cell.GetNeighbor(direction: direction);
            }

            return length;
        }

        private void SetTerrainType()
        {
            temperatureJitterChannel = Random.Range(min: 0, max: 4);
            var rockDesertElevation =
                elevationMaximum - (elevationMaximum - waterLevel) / 2;

            for (var i = 0; i < cellCount; i++)
            {
                var cell = grid.GetCell(cellIndex: i);
                var temperature = DetermineTemperature(cell: cell);
                var moisture = climate[index: i].moisture;
                if (!cell.IsUnderwater)
                {
                    var t = 0;
                    for (; t < temperatureBands.Length; t++)
                    {
                        if (temperature < temperatureBands[t])
                        {
                            break;
                        }
                    }

                    var m = 0;
                    for (; m < moistureBands.Length; m++)
                    {
                        if (moisture < moistureBands[m])
                        {
                            break;
                        }
                    }

                    var cellBiome = biomes[t * 4 + m];

                    if (cellBiome.terrain == 0)
                    {
                        if (cell.Elevation >= rockDesertElevation)
                        {
                            cellBiome.terrain = 3;
                        }
                    }
                    else if (cell.Elevation == elevationMaximum)
                    {
                        cellBiome.terrain = 4;
                    }

                    if (cellBiome.terrain == 4)
                    {
                        cellBiome.plant = 0;
                    }
                    else if (cellBiome.plant < 3 && cell.HasRiver)
                    {
                        cellBiome.plant += 1;
                    }

                    cell.TerrainTypeIndex = cellBiome.terrain;
                    cell.PlantLevel = cellBiome.plant;
                }
                else
                {
                    int terrain;
                    if (cell.Elevation == waterLevel - 1)
                    {
                        int cliffs = 0, slopes = 0;
                        for (
                            var d = HexDirection.NE;
                            d <= HexDirection.NW;
                            d++
                        )
                        {
                            var neighbor = cell.GetNeighbor(direction: d);
                            if (!neighbor)
                            {
                                continue;
                            }

                            var delta = neighbor.Elevation - cell.WaterLevel;
                            if (delta == 0)
                            {
                                slopes += 1;
                            }
                            else if (delta > 0)
                            {
                                cliffs += 1;
                            }
                        }

                        if (cliffs + slopes > 3)
                        {
                            terrain = 1;
                        }
                        else if (cliffs > 0)
                        {
                            terrain = 3;
                        }
                        else if (slopes > 0)
                        {
                            terrain = 0;
                        }
                        else
                        {
                            terrain = 1;
                        }
                    }
                    else if (cell.Elevation >= waterLevel)
                    {
                        terrain = 1;
                    }
                    else if (cell.Elevation < 0)
                    {
                        terrain = 3;
                    }
                    else
                    {
                        terrain = 2;
                    }

                    if (terrain == 1 && temperature < temperatureBands[0])
                    {
                        terrain = 2;
                    }

                    cell.TerrainTypeIndex = terrain;
                }
            }
        }

        private float DetermineTemperature(HexCell cell)
        {
            var latitude = (float) cell.coordinates.Z / grid.cellCountZ;
            if (hemisphere == HemisphereMode.Both)
            {
                latitude *= 2f;
                if (latitude > 1f)
                {
                    latitude = 2f - latitude;
                }
            }
            else if (hemisphere == HemisphereMode.North)
            {
                latitude = 1f - latitude;
            }

            var temperature =
                Mathf.LerpUnclamped(a: lowTemperature, b: highTemperature, t: latitude);

            temperature *= 1f - (cell.ViewElevation - waterLevel) /
                           (elevationMaximum - waterLevel + 1f);

            var jitter =
                HexMetrics.SampleNoise(position: cell.Position * 0.1f)[index: temperatureJitterChannel];

            temperature += (jitter * 2f - 1f) * temperatureJitter;

            return temperature;
        }

        private HexCell GetRandomCell(MapRegion region)
        {
            return grid.GetCell(
                xOffset: Random.Range(min: region.xMin, max: region.xMax),
                zOffset: Random.Range(min: region.zMin, max: region.zMax)
            );
        }

        private struct MapRegion
        {
            public int xMin, xMax, zMin, zMax;
        }

        private struct ClimateData
        {
            public float clouds, moisture;
        }

        private struct Biome
        {
            public int terrain, plant;

            public Biome(int terrain, int plant)
            {
                this.terrain = terrain;
                this.plant = plant;
            }
        }
    }
}