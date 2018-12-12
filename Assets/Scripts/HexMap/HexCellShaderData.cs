using System.Collections.Generic;
using UnityEngine;

namespace HexMap
{
    public class HexCellShaderData : MonoBehaviour
    {
        private const float transitionSpeed = 255f;

        private readonly List<HexCell> transitioningCells = new List<HexCell>();

        private Texture2D cellTexture;
        private Color32[] cellTextureData;

        private bool needsVisibilityReset;

        public HexGrid Grid { get; set; }

        public bool ImmediateMode { get; set; }

        public void Initialize(int x, int z)
        {
            if (cellTexture)
            {
                cellTexture.Resize(width: x, height: z);
            }
            else
            {
                cellTexture = new Texture2D(
                    width: x, height: z, textureFormat: TextureFormat.RGBA32, mipChain: false, linear: true
                );
                cellTexture.filterMode = FilterMode.Point;
                cellTexture.wrapModeU = TextureWrapMode.Repeat;
                cellTexture.wrapModeV = TextureWrapMode.Clamp;
                Shader.SetGlobalTexture(name: "_HexCellData", value: cellTexture);
            }

            Shader.SetGlobalVector(
                name: "_HexCellData_TexelSize",
                value: new Vector4(x: 1f / x, y: 1f / z, z: x, w: z)
            );

            if (cellTextureData == null || cellTextureData.Length != x * z)
            {
                cellTextureData = new Color32[x * z];
            }
            else
            {
                for (var i = 0; i < cellTextureData.Length; i++)
                {
                    cellTextureData[i] = new Color32(r: 0, g: 0, b: 0, a: 0);
                }
            }

            transitioningCells.Clear();
            enabled = true;
        }

        public void RefreshTerrain(HexCell cell)
        {
            cellTextureData[cell.Index].a = (byte) cell.TerrainTypeIndex;
            enabled = true;
        }

        public void RefreshVisibility(HexCell cell)
        {
            var index = cell.Index;
            if (ImmediateMode)
            {
                cellTextureData[index].r = cell.IsVisible ? (byte) 255 : (byte) 0;
                cellTextureData[index].g = cell.IsExplored ? (byte) 255 : (byte) 0;
            }
            else if (cellTextureData[index].b != 255)
            {
                cellTextureData[index].b = 255;
                transitioningCells.Add(item: cell);
            }

            enabled = true;
        }

        public void SetMapData(HexCell cell, float data)
        {
            cellTextureData[cell.Index].b =
                data < 0f ? (byte) 0 : data < 1f ? (byte) (data * 254f) : (byte) 254;
            enabled = true;
        }

        public void ViewElevationChanged()
        {
            needsVisibilityReset = true;
            enabled = true;
        }

        private void LateUpdate()
        {
            if (needsVisibilityReset)
            {
                needsVisibilityReset = false;
                Grid.ResetVisibility();
            }

            var delta = (int) (Time.deltaTime * transitionSpeed);
            if (delta == 0)
            {
                delta = 1;
            }

            for (var i = 0; i < transitioningCells.Count; i++)
            {
                if (!UpdateCellData(cell: transitioningCells[index: i], delta: delta))
                {
                    transitioningCells[index: i--] =
                        transitioningCells[index: transitioningCells.Count - 1];
                    transitioningCells.RemoveAt(index: transitioningCells.Count - 1);
                }
            }

            cellTexture.SetPixels32(colors: cellTextureData);
            cellTexture.Apply();
            enabled = transitioningCells.Count > 0;
        }

        private bool UpdateCellData(HexCell cell, int delta)
        {
            var index = cell.Index;
            var data = cellTextureData[index];
            var stillUpdating = false;

            if (cell.IsExplored && data.g < 255)
            {
                stillUpdating = true;
                var t = data.g + delta;
                data.g = t >= 255 ? (byte) 255 : (byte) t;
            }

            if (cell.IsVisible)
            {
                if (data.r < 255)
                {
                    stillUpdating = true;
                    var t = data.r + delta;
                    data.r = t >= 255 ? (byte) 255 : (byte) t;
                }
            }
            else if (data.r > 0)
            {
                stillUpdating = true;
                var t = data.r - delta;
                data.r = t < 0 ? (byte) 0 : (byte) t;
            }

            if (!stillUpdating)
            {
                data.b = 0;
            }

            cellTextureData[index] = data;
            return stillUpdating;
        }
    }
}