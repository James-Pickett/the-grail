using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace HexMap
{
    public class HexCell : MonoBehaviour
    {
        public HexGridChunk chunk;

        public HexCoordinates coordinates;

        private int elevation = int.MinValue;

        private bool explored;

        [SerializeField]
        private HexCell[] neighbors;

        [SerializeField]
        private bool[] roads;

        private int specialIndex;

        private int terrainTypeIndex;

        public RectTransform uiRect;

        private int urbanLevel, farmLevel, plantLevel;

        private int visibility;

        private bool walled;
        private int waterLevel;

        public int Index { get; set; }

        public int ColumnIndex { get; set; }

        public int Elevation
        {
            get { return elevation; }
            set
            {
                if (elevation == value)
                {
                    return;
                }

                var originalViewElevation = ViewElevation;
                elevation = value;
                if (ViewElevation != originalViewElevation)
                {
                    ShaderData.ViewElevationChanged();
                }

                RefreshPosition();
                ValidateRivers();

                for (var i = 0; i < roads.Length; i++)
                {
                    if (roads[i] && GetElevationDifference(direction: (HexDirection) i) > 1)
                    {
                        SetRoad(index: i, state: false);
                    }
                }

                Refresh();
            }
        }

        public int WaterLevel
        {
            get { return waterLevel; }
            set
            {
                if (waterLevel == value)
                {
                    return;
                }

                var originalViewElevation = ViewElevation;
                waterLevel = value;
                if (ViewElevation != originalViewElevation)
                {
                    ShaderData.ViewElevationChanged();
                }

                ValidateRivers();
                Refresh();
            }
        }

        public int ViewElevation
        {
            get { return elevation >= waterLevel ? elevation : waterLevel; }
        }

        public bool IsUnderwater
        {
            get { return waterLevel > elevation; }
        }

        public bool HasIncomingRiver { get; private set; }

        public bool HasOutgoingRiver { get; private set; }

        public bool HasRiver
        {
            get { return HasIncomingRiver || HasOutgoingRiver; }
        }

        public bool HasRiverBeginOrEnd
        {
            get { return HasIncomingRiver != HasOutgoingRiver; }
        }

        public HexDirection RiverBeginOrEndDirection
        {
            get { return HasIncomingRiver ? IncomingRiver : OutgoingRiver; }
        }

        public bool HasRoads
        {
            get
            {
                for (var i = 0; i < roads.Length; i++)
                {
                    if (roads[i])
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public HexDirection IncomingRiver { get; private set; }

        public HexDirection OutgoingRiver { get; private set; }

        public Vector3 Position
        {
            get { return transform.localPosition; }
        }


        public float StreamBedY
        {
            get
            {
                return
                    (elevation + HexMetrics.streamBedElevationOffset) *
                    HexMetrics.elevationStep;
            }
        }

        public float RiverSurfaceY
        {
            get
            {
                return
                    (elevation + HexMetrics.waterElevationOffset) *
                    HexMetrics.elevationStep;
            }
        }

        public float WaterSurfaceY
        {
            get
            {
                return
                    (waterLevel + HexMetrics.waterElevationOffset) *
                    HexMetrics.elevationStep;
            }
        }

        public int UrbanLevel
        {
            get { return urbanLevel; }
            set
            {
                if (urbanLevel != value)
                {
                    urbanLevel = value;
                    RefreshSelfOnly();
                }
            }
        }

        public int FarmLevel
        {
            get { return farmLevel; }
            set
            {
                if (farmLevel != value)
                {
                    farmLevel = value;
                    RefreshSelfOnly();
                }
            }
        }

        public int PlantLevel
        {
            get { return plantLevel; }
            set
            {
                if (plantLevel != value)
                {
                    plantLevel = value;
                    RefreshSelfOnly();
                }
            }
        }

        public int SpecialIndex
        {
            get { return specialIndex; }
            set
            {
                if (specialIndex != value && !HasRiver)
                {
                    specialIndex = value;
                    RemoveRoads();
                    RefreshSelfOnly();
                }
            }
        }

        public bool IsSpecial
        {
            get { return specialIndex > 0; }
        }

        public bool Walled
        {
            get { return walled; }
            set
            {
                if (walled != value)
                {
                    walled = value;
                    Refresh();
                }
            }
        }

        public int TerrainTypeIndex
        {
            get { return terrainTypeIndex; }
            set
            {
                if (terrainTypeIndex != value)
                {
                    terrainTypeIndex = value;
                    ShaderData.RefreshTerrain(cell: this);
                }
            }
        }

        public bool IsVisible
        {
            get { return visibility > 0 && Explorable; }
        }

        public bool IsExplored
        {
            get { return explored && Explorable; }
            private set { explored = value; }
        }

        public bool Explorable { get; set; }

        public int Distance { get; set; }

        public HexUnit Unit { get; set; }

        public HexCell PathFrom { get; set; }

        public int SearchHeuristic { get; set; }

        public int SearchPriority
        {
            get { return Distance + SearchHeuristic; }
        }

        public int SearchPhase { get; set; }

        public HexCell NextWithSamePriority { get; set; }

        public HexCellShaderData ShaderData { get; set; }

        public void IncreaseVisibility()
        {
            visibility += 1;
            if (visibility == 1)
            {
                IsExplored = true;
                ShaderData.RefreshVisibility(cell: this);
            }
        }

        public void DecreaseVisibility()
        {
            visibility -= 1;
            if (visibility == 0)
            {
                ShaderData.RefreshVisibility(cell: this);
            }
        }

        public void ResetVisibility()
        {
            if (visibility > 0)
            {
                visibility = 0;
                ShaderData.RefreshVisibility(cell: this);
            }
        }

        public HexCell GetNeighbor(HexDirection direction)
        {
            return neighbors[(int) direction];
        }

        public void SetNeighbor(HexDirection direction, HexCell cell)
        {
            neighbors[(int) direction] = cell;
            cell.neighbors[(int) direction.Opposite()] = this;
        }

        public HexEdgeType GetEdgeType(HexDirection direction)
        {
            return HexMetrics.GetEdgeType(
                elevation1: elevation, elevation2: neighbors[(int) direction].elevation
            );
        }

        public HexEdgeType GetEdgeType(HexCell otherCell)
        {
            return HexMetrics.GetEdgeType(
                elevation1: elevation, elevation2: otherCell.elevation
            );
        }

        public bool HasRiverThroughEdge(HexDirection direction)
        {
            return
                HasIncomingRiver && IncomingRiver == direction ||
                HasOutgoingRiver && OutgoingRiver == direction;
        }

        public void RemoveIncomingRiver()
        {
            if (!HasIncomingRiver)
            {
                return;
            }

            HasIncomingRiver = false;
            RefreshSelfOnly();

            var neighbor = GetNeighbor(direction: IncomingRiver);
            neighbor.HasOutgoingRiver = false;
            neighbor.RefreshSelfOnly();
        }

        public void RemoveOutgoingRiver()
        {
            if (!HasOutgoingRiver)
            {
                return;
            }

            HasOutgoingRiver = false;
            RefreshSelfOnly();

            var neighbor = GetNeighbor(direction: OutgoingRiver);
            neighbor.HasIncomingRiver = false;
            neighbor.RefreshSelfOnly();
        }

        public void RemoveRiver()
        {
            RemoveOutgoingRiver();
            RemoveIncomingRiver();
        }

        public void SetOutgoingRiver(HexDirection direction)
        {
            if (HasOutgoingRiver && OutgoingRiver == direction)
            {
                return;
            }

            var neighbor = GetNeighbor(direction: direction);
            if (!IsValidRiverDestination(neighbor: neighbor))
            {
                return;
            }

            RemoveOutgoingRiver();
            if (HasIncomingRiver && IncomingRiver == direction)
            {
                RemoveIncomingRiver();
            }

            HasOutgoingRiver = true;
            OutgoingRiver = direction;
            specialIndex = 0;

            neighbor.RemoveIncomingRiver();
            neighbor.HasIncomingRiver = true;
            neighbor.IncomingRiver = direction.Opposite();
            neighbor.specialIndex = 0;

            SetRoad(index: (int) direction, state: false);
        }

        public bool HasRoadThroughEdge(HexDirection direction)
        {
            return roads[(int) direction];
        }

        public void AddRoad(HexDirection direction)
        {
            if (
                !roads[(int) direction] && !HasRiverThroughEdge(direction: direction) &&
                !IsSpecial && !GetNeighbor(direction: direction).IsSpecial &&
                GetElevationDifference(direction: direction) <= 1
            )
            {
                SetRoad(index: (int) direction, state: true);
            }
        }

        public void RemoveRoads()
        {
            for (var i = 0; i < neighbors.Length; i++)
            {
                if (roads[i])
                {
                    SetRoad(index: i, state: false);
                }
            }
        }

        public int GetElevationDifference(HexDirection direction)
        {
            var difference = elevation - GetNeighbor(direction: direction).elevation;
            return difference >= 0 ? difference : -difference;
        }

        private bool IsValidRiverDestination(HexCell neighbor)
        {
            return neighbor && (
                       elevation >= neighbor.elevation || waterLevel == neighbor.elevation
                   );
        }

        private void ValidateRivers()
        {
            if (
                HasOutgoingRiver &&
                !IsValidRiverDestination(neighbor: GetNeighbor(direction: OutgoingRiver))
            )
            {
                RemoveOutgoingRiver();
            }

            if (
                HasIncomingRiver &&
                !GetNeighbor(direction: IncomingRiver).IsValidRiverDestination(neighbor: this)
            )
            {
                RemoveIncomingRiver();
            }
        }

        private void SetRoad(int index, bool state)
        {
            roads[index] = state;
            neighbors[index].roads[(int) ((HexDirection) index).Opposite()] = state;
            neighbors[index].RefreshSelfOnly();
            RefreshSelfOnly();
        }

        private void RefreshPosition()
        {
            var position = transform.localPosition;
            position.y = elevation * HexMetrics.elevationStep;
            position.y +=
                (HexMetrics.SampleNoise(position: position).y * 2f - 1f) *
                HexMetrics.elevationPerturbStrength;
            transform.localPosition = position;

            var uiPosition = uiRect.localPosition;
            uiPosition.z = -position.y;
            uiRect.localPosition = uiPosition;
        }

        private void Refresh()
        {
            if (chunk)
            {
                chunk.Refresh();
                for (var i = 0; i < neighbors.Length; i++)
                {
                    var neighbor = neighbors[i];
                    if (neighbor != null && neighbor.chunk != chunk)
                    {
                        neighbor.chunk.Refresh();
                    }
                }

                if (Unit)
                {
                    Unit.ValidateLocation();
                }
            }
        }

        private void RefreshSelfOnly()
        {
            chunk.Refresh();
            if (Unit)
            {
                Unit.ValidateLocation();
            }
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(value: (byte) terrainTypeIndex);
            writer.Write(value: (byte) (elevation + 127));
            writer.Write(value: (byte) waterLevel);
            writer.Write(value: (byte) urbanLevel);
            writer.Write(value: (byte) farmLevel);
            writer.Write(value: (byte) plantLevel);
            writer.Write(value: (byte) specialIndex);
            writer.Write(value: walled);

            if (HasIncomingRiver)
            {
                writer.Write(value: (byte) (IncomingRiver + 128));
            }
            else
            {
                writer.Write(value: (byte) 0);
            }

            if (HasOutgoingRiver)
            {
                writer.Write(value: (byte) (OutgoingRiver + 128));
            }
            else
            {
                writer.Write(value: (byte) 0);
            }

            var roadFlags = 0;
            for (var i = 0; i < roads.Length; i++)
            {
                if (roads[i])
                {
                    roadFlags |= 1 << i;
                }
            }

            writer.Write(value: (byte) roadFlags);
            writer.Write(value: IsExplored);
        }

        public void Load(BinaryReader reader, int header)
        {
            terrainTypeIndex = reader.ReadByte();
            ShaderData.RefreshTerrain(cell: this);
            elevation = reader.ReadByte();
            if (header >= 4)
            {
                elevation -= 127;
            }

            RefreshPosition();
            waterLevel = reader.ReadByte();
            urbanLevel = reader.ReadByte();
            farmLevel = reader.ReadByte();
            plantLevel = reader.ReadByte();
            specialIndex = reader.ReadByte();
            walled = reader.ReadBoolean();

            var riverData = reader.ReadByte();
            if (riverData >= 128)
            {
                HasIncomingRiver = true;
                IncomingRiver = (HexDirection) (riverData - 128);
            }
            else
            {
                HasIncomingRiver = false;
            }

            riverData = reader.ReadByte();
            if (riverData >= 128)
            {
                HasOutgoingRiver = true;
                OutgoingRiver = (HexDirection) (riverData - 128);
            }
            else
            {
                HasOutgoingRiver = false;
            }

            int roadFlags = reader.ReadByte();
            for (var i = 0; i < roads.Length; i++)
            {
                roads[i] = (roadFlags & (1 << i)) != 0;
            }

            IsExplored = header >= 3 ? reader.ReadBoolean() : false;
            ShaderData.RefreshVisibility(cell: this);
        }

        public void SetLabel(string text)
        {
            var label = uiRect.GetComponent<Text>();
            label.text = text;
        }

        public void DisableHighlight()
        {
            var highlight = uiRect.GetChild(index: 0).GetComponent<Image>();
            highlight.enabled = false;
        }

        public void EnableHighlight(Color color)
        {
            var highlight = uiRect.GetChild(index: 0).GetComponent<Image>();
            highlight.color = color;
            highlight.enabled = true;
        }

        public void SetMapData(float data)
        {
            ShaderData.SetMapData(cell: this, data: data);
        }
    }
}