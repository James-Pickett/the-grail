using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace HexMap
{
    public class HexGrid : MonoBehaviour
    {
        private readonly List<HexUnit> units = new List<HexUnit>();
        public int cellCountX = 20, cellCountZ = 15;
        public Text cellLabelPrefab;

        public HexCell cellPrefab;
        private HexCell[] cells;

        private HexCellShaderData cellShaderData;

        private int chunkCountX, chunkCountZ;
        public HexGridChunk chunkPrefab;
        private HexGridChunk[] chunks;

        private Transform[] columns;

        private int currentCenterColumnIndex = -1;

        private HexCell currentPathFrom, currentPathTo;

        public Texture2D noiseSource;

        private HexCellPriorityQueue searchFrontier;

        private int searchFrontierPhase;

        public int seed;
        public HexUnit unitPrefab;

        public bool wrapping;

        public bool HasPath { get; private set; }

        private void Awake()
        {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed: seed);
            HexUnit.unitPrefab = unitPrefab;
            cellShaderData = gameObject.AddComponent<HexCellShaderData>();
            cellShaderData.Grid = this;
            CreateMap(x: cellCountX, z: cellCountZ, wrapping: wrapping);
        }

        public void AddUnit(HexUnit unit, HexCell location, float orientation)
        {
            units.Add(item: unit);
            unit.Grid = this;
            unit.Location = location;
            unit.Orientation = orientation;
        }

        public void RemoveUnit(HexUnit unit)
        {
            units.Remove(item: unit);
            unit.Die();
        }

        public void MakeChildOfColumn(Transform child, int columnIndex)
        {
            child.SetParent(parent: columns[columnIndex], worldPositionStays: false);
        }

        public bool CreateMap(int x, int z, bool wrapping)
        {
            if (
                x <= 0 || x % HexMetrics.chunkSizeX != 0 ||
                z <= 0 || z % HexMetrics.chunkSizeZ != 0
            )
            {
                Debug.LogError(message: "Unsupported map size.");
                return false;
            }

            ClearPath();
            ClearUnits();
            if (columns != null)
            {
                for (var i = 0; i < columns.Length; i++)
                {
                    Destroy(obj: columns[i].gameObject);
                }
            }

            cellCountX = x;
            cellCountZ = z;
            this.wrapping = wrapping;
            currentCenterColumnIndex = -1;
            HexMetrics.wrapSize = wrapping ? cellCountX : 0;
            chunkCountX = cellCountX / HexMetrics.chunkSizeX;
            chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;
            cellShaderData.Initialize(x: cellCountX, z: cellCountZ);
            CreateChunks();
            CreateCells();
            return true;
        }

        private void CreateChunks()
        {
            columns = new Transform[chunkCountX];
            for (var x = 0; x < chunkCountX; x++)
            {
                columns[x] = new GameObject(name: "Column").transform;
                columns[x].SetParent(parent: transform, worldPositionStays: false);
            }

            chunks = new HexGridChunk[chunkCountX * chunkCountZ];
            for (int z = 0, i = 0; z < chunkCountZ; z++)
            {
                for (var x = 0; x < chunkCountX; x++)
                {
                    var chunk = chunks[i++] = Instantiate(original: chunkPrefab);
                    chunk.transform.SetParent(parent: columns[x], worldPositionStays: false);
                }
            }
        }

        private void CreateCells()
        {
            cells = new HexCell[cellCountZ * cellCountX];

            for (int z = 0, i = 0; z < cellCountZ; z++)
            {
                for (var x = 0; x < cellCountX; x++)
                {
                    CreateCell(x: x, z: z, i: i++);
                }
            }
        }

        private void ClearUnits()
        {
            for (var i = 0; i < units.Count; i++)
            {
                units[index: i].Die();
            }

            units.Clear();
        }

        private void OnEnable()
        {
            if (!HexMetrics.noiseSource)
            {
                HexMetrics.noiseSource = noiseSource;
                HexMetrics.InitializeHashGrid(seed: seed);
                HexUnit.unitPrefab = unitPrefab;
                HexMetrics.wrapSize = wrapping ? cellCountX : 0;
                ResetVisibility();
            }
        }

        public HexCell GetCell(Ray ray)
        {
            RaycastHit hit;
            if (Physics.Raycast(ray: ray, hitInfo: out hit))
            {
                return GetCell(position: hit.point);
            }

            return null;
        }

        public HexCell GetCell(Vector3 position)
        {
            position = transform.InverseTransformPoint(position: position);
            var coordinates = HexCoordinates.FromPosition(position: position);
            return GetCell(coordinates: coordinates);
        }

        public HexCell GetCell(HexCoordinates coordinates)
        {
            var z = coordinates.Z;
            if (z < 0 || z >= cellCountZ)
            {
                return null;
            }

            var x = coordinates.X + z / 2;
            if (x < 0 || x >= cellCountX)
            {
                return null;
            }

            return cells[x + z * cellCountX];
        }

        public HexCell GetCell(int xOffset, int zOffset)
        {
            return cells[xOffset + zOffset * cellCountX];
        }

        public HexCell GetCell(int cellIndex)
        {
            return cells[cellIndex];
        }

        public void ShowUI(bool visible)
        {
            for (var i = 0; i < chunks.Length; i++)
            {
                chunks[i].ShowUI(visible: visible);
            }
        }

        private void CreateCell(int x, int z, int i)
        {
            Vector3 position;
            position.x = (x + z * 0.5f - z / 2) * HexMetrics.innerDiameter;
            position.y = 0f;
            position.z = z * (HexMetrics.outerRadius * 1.5f);

            var cell = cells[i] = Instantiate(original: cellPrefab);
            cell.transform.localPosition = position;
            cell.coordinates = HexCoordinates.FromOffsetCoordinates(x: x, z: z);
            cell.Index = i;
            cell.ColumnIndex = x / HexMetrics.chunkSizeX;
            cell.ShaderData = cellShaderData;

            if (wrapping)
            {
                cell.Explorable = z > 0 && z < cellCountZ - 1;
            }
            else
            {
                cell.Explorable =
                    x > 0 && z > 0 && x < cellCountX - 1 && z < cellCountZ - 1;
            }

            if (x > 0)
            {
                cell.SetNeighbor(direction: HexDirection.W, cell: cells[i - 1]);
                if (wrapping && x == cellCountX - 1)
                {
                    cell.SetNeighbor(direction: HexDirection.E, cell: cells[i - x]);
                }
            }

            if (z > 0)
            {
                if ((z & 1) == 0)
                {
                    cell.SetNeighbor(direction: HexDirection.SE, cell: cells[i - cellCountX]);
                    if (x > 0)
                    {
                        cell.SetNeighbor(direction: HexDirection.SW, cell: cells[i - cellCountX - 1]);
                    }
                    else if (wrapping)
                    {
                        cell.SetNeighbor(direction: HexDirection.SW, cell: cells[i - 1]);
                    }
                }
                else
                {
                    cell.SetNeighbor(direction: HexDirection.SW, cell: cells[i - cellCountX]);
                    if (x < cellCountX - 1)
                    {
                        cell.SetNeighbor(direction: HexDirection.SE, cell: cells[i - cellCountX + 1]);
                    }
                    else if (wrapping)
                    {
                        cell.SetNeighbor(
                            direction: HexDirection.SE, cell: cells[i - cellCountX * 2 + 1]
                        );
                    }
                }
            }

            var label = Instantiate(original: cellLabelPrefab);
            label.rectTransform.anchoredPosition =
                new Vector2(x: position.x, y: position.z);
            cell.uiRect = label.rectTransform;

            cell.Elevation = 0;

            AddCellToChunk(x: x, z: z, cell: cell);
        }

        private void AddCellToChunk(int x, int z, HexCell cell)
        {
            var chunkX = x / HexMetrics.chunkSizeX;
            var chunkZ = z / HexMetrics.chunkSizeZ;
            var chunk = chunks[chunkX + chunkZ * chunkCountX];

            var localX = x - chunkX * HexMetrics.chunkSizeX;
            var localZ = z - chunkZ * HexMetrics.chunkSizeZ;
            chunk.AddCell(index: localX + localZ * HexMetrics.chunkSizeX, cell: cell);
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(value: cellCountX);
            writer.Write(value: cellCountZ);
            writer.Write(value: wrapping);

            for (var i = 0; i < cells.Length; i++)
            {
                cells[i].Save(writer: writer);
            }

            writer.Write(value: units.Count);
            for (var i = 0; i < units.Count; i++)
            {
                units[index: i].Save(writer: writer);
            }
        }

        public void Load(BinaryReader reader, int header)
        {
            ClearPath();
            ClearUnits();
            int x = 20, z = 15;
            if (header >= 1)
            {
                x = reader.ReadInt32();
                z = reader.ReadInt32();
            }

            var wrapping = header >= 5 ? reader.ReadBoolean() : false;
            if (x != cellCountX || z != cellCountZ || this.wrapping != wrapping)
            {
                if (!CreateMap(x: x, z: z, wrapping: wrapping))
                {
                    return;
                }
            }

            var originalImmediateMode = cellShaderData.ImmediateMode;
            cellShaderData.ImmediateMode = true;

            for (var i = 0; i < cells.Length; i++)
            {
                cells[i].Load(reader: reader, header: header);
            }

            for (var i = 0; i < chunks.Length; i++)
            {
                chunks[i].Refresh();
            }

            if (header >= 2)
            {
                var unitCount = reader.ReadInt32();
                for (var i = 0; i < unitCount; i++)
                {
                    HexUnit.Load(reader: reader, grid: this);
                }
            }

            cellShaderData.ImmediateMode = originalImmediateMode;
        }

        public List<HexCell> GetPath()
        {
            if (!HasPath)
            {
                return null;
            }

            var path = ListPool<HexCell>.Get();
            for (var c = currentPathTo; c != currentPathFrom; c = c.PathFrom)
            {
                path.Add(item: c);
            }

            path.Add(item: currentPathFrom);
            path.Reverse();
            return path;
        }

        public void ClearPath()
        {
            if (HasPath)
            {
                var current = currentPathTo;
                while (current != currentPathFrom)
                {
                    current.SetLabel(text: null);
                    current.DisableHighlight();
                    current = current.PathFrom;
                }

                current.DisableHighlight();
                HasPath = false;
            }
            else if (currentPathFrom)
            {
                currentPathFrom.DisableHighlight();
                currentPathTo.DisableHighlight();
            }

            currentPathFrom = currentPathTo = null;
        }

        private void ShowPath(int speed)
        {
            if (HasPath)
            {
                var current = currentPathTo;
                while (current != currentPathFrom)
                {
                    var turn = (current.Distance - 1) / speed;
                    current.SetLabel(text: turn.ToString());
                    current.EnableHighlight(color: Color.white);
                    current = current.PathFrom;
                }
            }

            currentPathFrom.EnableHighlight(color: Color.blue);
            currentPathTo.EnableHighlight(color: Color.red);
        }

        public void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
        {
            ClearPath();
            currentPathFrom = fromCell;
            currentPathTo = toCell;
            HasPath = Search(fromCell: fromCell, toCell: toCell, unit: unit);
            ShowPath(speed: unit.Speed);
        }

        private bool Search(HexCell fromCell, HexCell toCell, HexUnit unit)
        {
            var speed = unit.Speed;
            searchFrontierPhase += 2;
            if (searchFrontier == null)
            {
                searchFrontier = new HexCellPriorityQueue();
            }
            else
            {
                searchFrontier.Clear();
            }

            fromCell.SearchPhase = searchFrontierPhase;
            fromCell.Distance = 0;
            searchFrontier.Enqueue(cell: fromCell);
            while (searchFrontier.Count > 0)
            {
                var current = searchFrontier.Dequeue();
                current.SearchPhase += 1;

                if (current == toCell)
                {
                    return true;
                }

                var currentTurn = (current.Distance - 1) / speed;

                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbor = current.GetNeighbor(direction: d);
                    if (
                        neighbor == null ||
                        neighbor.SearchPhase > searchFrontierPhase
                    )
                    {
                        continue;
                    }

                    if (!unit.IsValidDestination(cell: neighbor))
                    {
                        continue;
                    }

                    var moveCost = unit.GetMoveCost(fromCell: current, toCell: neighbor, direction: d);
                    if (moveCost < 0)
                    {
                        continue;
                    }

                    var distance = current.Distance + moveCost;
                    var turn = (distance - 1) / speed;
                    if (turn > currentTurn)
                    {
                        distance = turn * speed + moveCost;
                    }

                    if (neighbor.SearchPhase < searchFrontierPhase)
                    {
                        neighbor.SearchPhase = searchFrontierPhase;
                        neighbor.Distance = distance;
                        neighbor.PathFrom = current;
                        neighbor.SearchHeuristic =
                            neighbor.coordinates.DistanceTo(other: toCell.coordinates);
                        searchFrontier.Enqueue(cell: neighbor);
                    }
                    else if (distance < neighbor.Distance)
                    {
                        var oldPriority = neighbor.SearchPriority;
                        neighbor.Distance = distance;
                        neighbor.PathFrom = current;
                        searchFrontier.Change(cell: neighbor, oldPriority: oldPriority);
                    }
                }
            }

            return false;
        }

        public void IncreaseVisibility(HexCell fromCell, int range)
        {
            var cells = GetVisibleCells(fromCell: fromCell, range: range);
            for (var i = 0; i < cells.Count; i++)
            {
                cells[index: i].IncreaseVisibility();
            }

            ListPool<HexCell>.Add(list: cells);
        }

        public void DecreaseVisibility(HexCell fromCell, int range)
        {
            var cells = GetVisibleCells(fromCell: fromCell, range: range);
            for (var i = 0; i < cells.Count; i++)
            {
                cells[index: i].DecreaseVisibility();
            }

            ListPool<HexCell>.Add(list: cells);
        }

        public void ResetVisibility()
        {
            for (var i = 0; i < cells.Length; i++)
            {
                cells[i].ResetVisibility();
            }

            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[index: i];
                IncreaseVisibility(fromCell: unit.Location, range: unit.VisionRange);
            }
        }

        private List<HexCell> GetVisibleCells(HexCell fromCell, int range)
        {
            var visibleCells = ListPool<HexCell>.Get();

            searchFrontierPhase += 2;
            if (searchFrontier == null)
            {
                searchFrontier = new HexCellPriorityQueue();
            }
            else
            {
                searchFrontier.Clear();
            }

            range += fromCell.ViewElevation;
            fromCell.SearchPhase = searchFrontierPhase;
            fromCell.Distance = 0;
            searchFrontier.Enqueue(cell: fromCell);
            var fromCoordinates = fromCell.coordinates;
            while (searchFrontier.Count > 0)
            {
                var current = searchFrontier.Dequeue();
                current.SearchPhase += 1;
                visibleCells.Add(item: current);

                for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
                {
                    var neighbor = current.GetNeighbor(direction: d);
                    if (
                        neighbor == null ||
                        neighbor.SearchPhase > searchFrontierPhase ||
                        !neighbor.Explorable
                    )
                    {
                        continue;
                    }

                    var distance = current.Distance + 1;
                    if (distance + neighbor.ViewElevation > range ||
                        distance > fromCoordinates.DistanceTo(other: neighbor.coordinates)
                    )
                    {
                        continue;
                    }

                    if (neighbor.SearchPhase < searchFrontierPhase)
                    {
                        neighbor.SearchPhase = searchFrontierPhase;
                        neighbor.Distance = distance;
                        neighbor.SearchHeuristic = 0;
                        searchFrontier.Enqueue(cell: neighbor);
                    }
                    else if (distance < neighbor.Distance)
                    {
                        var oldPriority = neighbor.SearchPriority;
                        neighbor.Distance = distance;
                        searchFrontier.Change(cell: neighbor, oldPriority: oldPriority);
                    }
                }
            }

            return visibleCells;
        }

        public void CenterMap(float xPosition)
        {
            var centerColumnIndex = (int)
                (xPosition / (HexMetrics.innerDiameter * HexMetrics.chunkSizeX));

            if (centerColumnIndex == currentCenterColumnIndex)
            {
                return;
            }

            currentCenterColumnIndex = centerColumnIndex;

            var minColumnIndex = centerColumnIndex - chunkCountX / 2;
            var maxColumnIndex = centerColumnIndex + chunkCountX / 2;

            Vector3 position;
            position.y = position.z = 0f;
            for (var i = 0; i < columns.Length; i++)
            {
                if (i < minColumnIndex)
                {
                    position.x = chunkCountX *
                                 (HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
                }
                else if (i > maxColumnIndex)
                {
                    position.x = chunkCountX *
                                 -(HexMetrics.innerDiameter * HexMetrics.chunkSizeX);
                }
                else
                {
                    position.x = 0f;
                }

                columns[i].localPosition = position;
            }
        }
    }
}