using UnityEngine;
using UnityEngine.EventSystems;

namespace HexMap.UI
{
    public class HexGameUI : MonoBehaviour
    {
        private HexCell currentCell;

        public HexGrid grid;

        private HexUnit selectedUnit;

        public void SetEditMode(bool toggle)
        {
            enabled = !toggle;
            grid.ShowUI(visible: !toggle);
            grid.ClearPath();
            if (toggle)
            {
                Shader.EnableKeyword(keyword: "HEX_MAP_EDIT_MODE");
            }
            else
            {
                Shader.DisableKeyword(keyword: "HEX_MAP_EDIT_MODE");
            }
        }

        private void Update()
        {
            if (!EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(button: 0))
                {
                    DoSelection();
                }
                else if (selectedUnit)
                {
                    if (Input.GetMouseButtonDown(button: 1))
                    {
                        DoMove();
                    }
                    else
                    {
                        DoPathfinding();
                    }
                }
            }
        }

        private void DoSelection()
        {
            grid.ClearPath();
            UpdateCurrentCell();
            if (currentCell)
            {
                selectedUnit = currentCell.Unit;
            }
        }

        private void DoPathfinding()
        {
            if (UpdateCurrentCell())
            {
                if (currentCell && selectedUnit.IsValidDestination(cell: currentCell))
                {
                    grid.FindPath(fromCell: selectedUnit.Location, toCell: currentCell, unit: selectedUnit);
                }
                else
                {
                    grid.ClearPath();
                }
            }
        }

        private void DoMove()
        {
            if (grid.HasPath)
            {
                selectedUnit.Travel(path: grid.GetPath());
                grid.ClearPath();
            }
        }

        private bool UpdateCurrentCell()
        {
            var cell =
                grid.GetCell(ray: Camera.main.ScreenPointToRay(pos: Input.mousePosition));
            if (cell != currentCell)
            {
                currentCell = cell;
                return true;
            }

            return false;
        }
    }
}