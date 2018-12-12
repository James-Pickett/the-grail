using UnityEngine;
using UnityEngine.EventSystems;

namespace HexMap.UI
{
    public class HexMapEditor : MonoBehaviour
    {
        private int activeElevation;

        private int activeTerrainTypeIndex;

        private int activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;
        private int activeWaterLevel;

        private bool applyElevation = true;

        private bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;
        private bool applyWaterLevel = true;

        private int brushSize;
        private HexDirection dragDirection;

        public HexGrid hexGrid;

        private bool isDrag;
        private HexCell previousCell;

        private OptionalToggle riverMode, roadMode, walledMode;

        public Material terrainMaterial;

        public void SetTerrainTypeIndex(int index)
        {
            activeTerrainTypeIndex = index;
        }

        public void SetApplyElevation(bool toggle)
        {
            applyElevation = toggle;
        }

        public void SetElevation(float elevation)
        {
            activeElevation = (int) elevation;
        }

        public void SetApplyWaterLevel(bool toggle)
        {
            applyWaterLevel = toggle;
        }

        public void SetWaterLevel(float level)
        {
            activeWaterLevel = (int) level;
        }

        public void SetApplyUrbanLevel(bool toggle)
        {
            applyUrbanLevel = toggle;
        }

        public void SetUrbanLevel(float level)
        {
            activeUrbanLevel = (int) level;
        }

        public void SetApplyFarmLevel(bool toggle)
        {
            applyFarmLevel = toggle;
        }

        public void SetFarmLevel(float level)
        {
            activeFarmLevel = (int) level;
        }

        public void SetApplyPlantLevel(bool toggle)
        {
            applyPlantLevel = toggle;
        }

        public void SetPlantLevel(float level)
        {
            activePlantLevel = (int) level;
        }

        public void SetApplySpecialIndex(bool toggle)
        {
            applySpecialIndex = toggle;
        }

        public void SetSpecialIndex(float index)
        {
            activeSpecialIndex = (int) index;
        }

        public void SetBrushSize(float size)
        {
            brushSize = (int) size;
        }

        public void SetRiverMode(int mode)
        {
            riverMode = (OptionalToggle) mode;
        }

        public void SetRoadMode(int mode)
        {
            roadMode = (OptionalToggle) mode;
        }

        public void SetWalledMode(int mode)
        {
            walledMode = (OptionalToggle) mode;
        }

        public void SetEditMode(bool toggle)
        {
            enabled = toggle;
        }

        public void ShowGrid(bool visible)
        {
            if (visible)
            {
                terrainMaterial.EnableKeyword(keyword: "GRID_ON");
            }
            else
            {
                terrainMaterial.DisableKeyword(keyword: "GRID_ON");
            }
        }

        private void Awake()
        {
            terrainMaterial.DisableKeyword(keyword: "GRID_ON");
            Shader.EnableKeyword(keyword: "HEX_MAP_EDIT_MODE");
            SetEditMode(toggle: true);
        }

        private void Update()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButton(button: 0))
                {
                    HandleInput();
                    return;
                }

                if (Input.GetKeyDown(key: KeyCode.U))
                {
                    if (Input.GetKey(key: KeyCode.LeftShift))
                    {
                        DestroyUnit();
                    }
                    else
                    {
                        CreateUnit();
                    }

                    return;
                }
            }

            previousCell = null;
        }

        private HexCell GetCellUnderCursor()
        {
            return
                hexGrid.GetCell(ray: Camera.main.ScreenPointToRay(pos: Input.mousePosition));
        }

        private void CreateUnit()
        {
            var cell = GetCellUnderCursor();
            if (cell && !cell.Unit)
            {
                hexGrid.AddUnit(
                    unit: Instantiate(original: HexUnit.unitPrefab), location: cell,
                    orientation: Random.Range(min: 0f, max: 360f)
                );
            }
        }

        private void DestroyUnit()
        {
            var cell = GetCellUnderCursor();
            if (cell && cell.Unit)
            {
                hexGrid.RemoveUnit(unit: cell.Unit);
            }
        }

        private void HandleInput()
        {
            var currentCell = GetCellUnderCursor();
            if (currentCell)
            {
                if (previousCell && previousCell != currentCell)
                {
                    ValidateDrag(currentCell: currentCell);
                }
                else
                {
                    isDrag = false;
                }

                EditCells(center: currentCell);
                previousCell = currentCell;
            }
            else
            {
                previousCell = null;
            }
        }

        private void ValidateDrag(HexCell currentCell)
        {
            for (
                dragDirection = HexDirection.NE;
                dragDirection <= HexDirection.NW;
                dragDirection++
            )
            {
                if (previousCell.GetNeighbor(direction: dragDirection) == currentCell)
                {
                    isDrag = true;
                    return;
                }
            }

            isDrag = false;
        }

        private void EditCells(HexCell center)
        {
            var centerX = center.coordinates.X;
            var centerZ = center.coordinates.Z;

            for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++)
            {
                for (var x = centerX - r; x <= centerX + brushSize; x++)
                {
                    EditCell(cell: hexGrid.GetCell(coordinates: new HexCoordinates(x: x, z: z)));
                }
            }

            for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++)
            {
                for (var x = centerX - brushSize; x <= centerX + r; x++)
                {
                    EditCell(cell: hexGrid.GetCell(coordinates: new HexCoordinates(x: x, z: z)));
                }
            }
        }

        private void EditCell(HexCell cell)
        {
            if (cell)
            {
                if (activeTerrainTypeIndex >= 0)
                {
                    cell.TerrainTypeIndex = activeTerrainTypeIndex;
                }

                if (applyElevation)
                {
                    cell.Elevation = activeElevation;
                }

                if (applyWaterLevel)
                {
                    cell.WaterLevel = activeWaterLevel;
                }

                if (applySpecialIndex)
                {
                    cell.SpecialIndex = activeSpecialIndex;
                }

                if (applyUrbanLevel)
                {
                    cell.UrbanLevel = activeUrbanLevel;
                }

                if (applyFarmLevel)
                {
                    cell.FarmLevel = activeFarmLevel;
                }

                if (applyPlantLevel)
                {
                    cell.PlantLevel = activePlantLevel;
                }

                if (riverMode == OptionalToggle.No)
                {
                    cell.RemoveRiver();
                }

                if (roadMode == OptionalToggle.No)
                {
                    cell.RemoveRoads();
                }

                if (walledMode != OptionalToggle.Ignore)
                {
                    cell.Walled = walledMode == OptionalToggle.Yes;
                }

                if (isDrag)
                {
                    var otherCell = cell.GetNeighbor(direction: dragDirection.Opposite());
                    if (otherCell)
                    {
                        if (riverMode == OptionalToggle.Yes)
                        {
                            otherCell.SetOutgoingRiver(direction: dragDirection);
                        }

                        if (roadMode == OptionalToggle.Yes)
                        {
                            otherCell.AddRoad(direction: dragDirection);
                        }
                    }
                }
            }
        }

        private enum OptionalToggle
        {
            Ignore,
            Yes,
            No
        }
    }
}