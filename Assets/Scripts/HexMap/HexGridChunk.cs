using UnityEngine;

namespace HexMap
{
    public class HexGridChunk : MonoBehaviour
    {
        private static readonly Color weights1 = new Color(r: 1f, g: 0f, b: 0f);
        private static readonly Color weights2 = new Color(r: 0f, g: 1f, b: 0f);
        private static readonly Color weights3 = new Color(r: 0f, g: 0f, b: 1f);

        private HexCell[] cells;

        public HexFeatureManager features;

        private Canvas gridCanvas;

        public HexMesh terrain, rivers, roads, water, waterShore, estuaries;

        private void Awake()
        {
            gridCanvas = GetComponentInChildren<Canvas>();

            cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        }

        public void AddCell(int index, HexCell cell)
        {
            cells[index] = cell;
            cell.chunk = this;
            cell.transform.SetParent(parent: transform, worldPositionStays: false);
            cell.uiRect.SetParent(parent: gridCanvas.transform, worldPositionStays: false);
        }

        public void Refresh()
        {
            enabled = true;
        }

        public void ShowUI(bool visible)
        {
            gridCanvas.gameObject.SetActive(value: visible);
        }

        private void LateUpdate()
        {
            Triangulate();
            enabled = false;
        }

        public void Triangulate()
        {
            terrain.Clear();
            rivers.Clear();
            roads.Clear();
            water.Clear();
            waterShore.Clear();
            estuaries.Clear();
            features.Clear();
            for (var i = 0; i < cells.Length; i++)
            {
                Triangulate(cell: cells[i]);
            }

            terrain.Apply();
            rivers.Apply();
            roads.Apply();
            water.Apply();
            waterShore.Apply();
            estuaries.Apply();
            features.Apply();
        }

        private void Triangulate(HexCell cell)
        {
            for (var d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                Triangulate(direction: d, cell: cell);
            }

            if (!cell.IsUnderwater)
            {
                if (!cell.HasRiver && !cell.HasRoads)
                {
                    features.AddFeature(cell: cell, position: cell.Position);
                }

                if (cell.IsSpecial)
                {
                    features.AddSpecialFeature(cell: cell, position: cell.Position);
                }
            }
        }

        private void Triangulate(HexDirection direction, HexCell cell)
        {
            var center = cell.Position;
            var e = new EdgeVertices(
                corner1: center + HexMetrics.GetFirstSolidCorner(direction: direction),
                corner2: center + HexMetrics.GetSecondSolidCorner(direction: direction)
            );

            if (cell.HasRiver)
            {
                if (cell.HasRiverThroughEdge(direction: direction))
                {
                    e.v3.y = cell.StreamBedY;
                    if (cell.HasRiverBeginOrEnd)
                    {
                        TriangulateWithRiverBeginOrEnd(direction: direction, cell: cell, center: center, e: e);
                    }
                    else
                    {
                        TriangulateWithRiver(direction: direction, cell: cell, center: center, e: e);
                    }
                }
                else
                {
                    TriangulateAdjacentToRiver(direction: direction, cell: cell, center: center, e: e);
                }
            }
            else
            {
                TriangulateWithoutRiver(direction: direction, cell: cell, center: center, e: e);

                if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction: direction))
                {
                    features.AddFeature(cell: cell, position: (center + e.v1 + e.v5) * (1f / 3f));
                }
            }

            if (direction <= HexDirection.SE)
            {
                TriangulateConnection(direction: direction, cell: cell, e1: e);
            }

            if (cell.IsUnderwater)
            {
                TriangulateWater(direction: direction, cell: cell, center: center);
            }
        }

        private void TriangulateWater(
            HexDirection direction, HexCell cell, Vector3 center
        )
        {
            center.y = cell.WaterSurfaceY;

            var neighbor = cell.GetNeighbor(direction: direction);
            if (neighbor != null && !neighbor.IsUnderwater)
            {
                TriangulateWaterShore(direction: direction, cell: cell, neighbor: neighbor, center: center);
            }
            else
            {
                TriangulateOpenWater(direction: direction, cell: cell, neighbor: neighbor, center: center);
            }
        }

        private void TriangulateOpenWater(
            HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center
        )
        {
            var c1 = center + HexMetrics.GetFirstWaterCorner(direction: direction);
            var c2 = center + HexMetrics.GetSecondWaterCorner(direction: direction);

            water.AddTriangle(v1: center, v2: c1, v3: c2);
            Vector3 indices;
            indices.x = indices.y = indices.z = cell.Index;
            water.AddTriangleCellData(indices: indices, weights: weights1);

            if (direction <= HexDirection.SE && neighbor != null)
            {
                var bridge = HexMetrics.GetWaterBridge(direction: direction);
                var e1 = c1 + bridge;
                var e2 = c2 + bridge;

                water.AddQuad(v1: c1, v2: c2, v3: e1, v4: e2);
                indices.y = neighbor.Index;
                water.AddQuadCellData(indices: indices, weights1: weights1, weights2: weights2);

                if (direction <= HexDirection.E)
                {
                    var nextNeighbor = cell.GetNeighbor(direction: direction.Next());
                    if (nextNeighbor == null || !nextNeighbor.IsUnderwater)
                    {
                        return;
                    }

                    water.AddTriangle(
                        v1: c2, v2: e2, v3: c2 + HexMetrics.GetWaterBridge(direction: direction.Next())
                    );
                    indices.z = nextNeighbor.Index;
                    water.AddTriangleCellData(
                        indices: indices, weights1: weights1, weights2: weights2, weights3: weights3
                    );
                }
            }
        }

        private void TriangulateWaterShore(
            HexDirection direction, HexCell cell, HexCell neighbor, Vector3 center
        )
        {
            var e1 = new EdgeVertices(
                corner1: center + HexMetrics.GetFirstWaterCorner(direction: direction),
                corner2: center + HexMetrics.GetSecondWaterCorner(direction: direction)
            );
            water.AddTriangle(v1: center, v2: e1.v1, v3: e1.v2);
            water.AddTriangle(v1: center, v2: e1.v2, v3: e1.v3);
            water.AddTriangle(v1: center, v2: e1.v3, v3: e1.v4);
            water.AddTriangle(v1: center, v2: e1.v4, v3: e1.v5);
            Vector3 indices;
            indices.x = indices.z = cell.Index;
            indices.y = neighbor.Index;
            water.AddTriangleCellData(indices: indices, weights: weights1);
            water.AddTriangleCellData(indices: indices, weights: weights1);
            water.AddTriangleCellData(indices: indices, weights: weights1);
            water.AddTriangleCellData(indices: indices, weights: weights1);

            var center2 = neighbor.Position;
            if (neighbor.ColumnIndex < cell.ColumnIndex - 1)
            {
                center2.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
            }
            else if (neighbor.ColumnIndex > cell.ColumnIndex + 1)
            {
                center2.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
            }

            center2.y = center.y;
            var e2 = new EdgeVertices(
                corner1: center2 + HexMetrics.GetSecondSolidCorner(direction: direction.Opposite()),
                corner2: center2 + HexMetrics.GetFirstSolidCorner(direction: direction.Opposite())
            );

            if (cell.HasRiverThroughEdge(direction: direction))
            {
                TriangulateEstuary(
                    e1: e1, e2: e2,
                    incomingRiver: cell.HasIncomingRiver && cell.IncomingRiver == direction, indices: indices
                );
            }
            else
            {
                waterShore.AddQuad(v1: e1.v1, v2: e1.v2, v3: e2.v1, v4: e2.v2);
                waterShore.AddQuad(v1: e1.v2, v2: e1.v3, v3: e2.v2, v4: e2.v3);
                waterShore.AddQuad(v1: e1.v3, v2: e1.v4, v3: e2.v3, v4: e2.v4);
                waterShore.AddQuad(v1: e1.v4, v2: e1.v5, v3: e2.v4, v4: e2.v5);
                waterShore.AddQuadUV(uMin: 0f, uMax: 0f, vMin: 0f, vMax: 1f);
                waterShore.AddQuadUV(uMin: 0f, uMax: 0f, vMin: 0f, vMax: 1f);
                waterShore.AddQuadUV(uMin: 0f, uMax: 0f, vMin: 0f, vMax: 1f);
                waterShore.AddQuadUV(uMin: 0f, uMax: 0f, vMin: 0f, vMax: 1f);
                waterShore.AddQuadCellData(indices: indices, weights1: weights1, weights2: weights2);
                waterShore.AddQuadCellData(indices: indices, weights1: weights1, weights2: weights2);
                waterShore.AddQuadCellData(indices: indices, weights1: weights1, weights2: weights2);
                waterShore.AddQuadCellData(indices: indices, weights1: weights1, weights2: weights2);
            }

            var nextNeighbor = cell.GetNeighbor(direction: direction.Next());
            if (nextNeighbor != null)
            {
                var center3 = nextNeighbor.Position;
                if (nextNeighbor.ColumnIndex < cell.ColumnIndex - 1)
                {
                    center3.x += HexMetrics.wrapSize * HexMetrics.innerDiameter;
                }
                else if (nextNeighbor.ColumnIndex > cell.ColumnIndex + 1)
                {
                    center3.x -= HexMetrics.wrapSize * HexMetrics.innerDiameter;
                }

                var v3 = center3 + (nextNeighbor.IsUnderwater
                             ? HexMetrics.GetFirstWaterCorner(direction: direction.Previous())
                             : HexMetrics.GetFirstSolidCorner(direction: direction.Previous()));
                v3.y = center.y;
                waterShore.AddTriangle(v1: e1.v5, v2: e2.v5, v3: v3);
                waterShore.AddTriangleUV(
                    uv1: new Vector2(x: 0f, y: 0f),
                    uv2: new Vector2(x: 0f, y: 1f),
                    uv3: new Vector2(x: 0f, y: nextNeighbor.IsUnderwater ? 0f : 1f)
                );
                indices.z = nextNeighbor.Index;
                waterShore.AddTriangleCellData(
                    indices: indices, weights1: weights1, weights2: weights2, weights3: weights3
                );
            }
        }

        private void TriangulateEstuary(
            EdgeVertices e1, EdgeVertices e2, bool incomingRiver, Vector3 indices
        )
        {
            waterShore.AddTriangle(v1: e2.v1, v2: e1.v2, v3: e1.v1);
            waterShore.AddTriangle(v1: e2.v5, v2: e1.v5, v3: e1.v4);
            waterShore.AddTriangleUV(
                uv1: new Vector2(x: 0f, y: 1f), uv2: new Vector2(x: 0f, y: 0f), uv3: new Vector2(x: 0f, y: 0f)
            );
            waterShore.AddTriangleUV(
                uv1: new Vector2(x: 0f, y: 1f), uv2: new Vector2(x: 0f, y: 0f), uv3: new Vector2(x: 0f, y: 0f)
            );
            waterShore.AddTriangleCellData(indices: indices, weights1: weights2, weights2: weights1,
                weights3: weights1);
            waterShore.AddTriangleCellData(indices: indices, weights1: weights2, weights2: weights1,
                weights3: weights1);

            estuaries.AddQuad(v1: e2.v1, v2: e1.v2, v3: e2.v2, v4: e1.v3);
            estuaries.AddTriangle(v1: e1.v3, v2: e2.v2, v3: e2.v4);
            estuaries.AddQuad(v1: e1.v3, v2: e1.v4, v3: e2.v4, v4: e2.v5);

            estuaries.AddQuadUV(
                uv1: new Vector2(x: 0f, y: 1f), uv2: new Vector2(x: 0f, y: 0f),
                uv3: new Vector2(x: 1f, y: 1f), uv4: new Vector2(x: 0f, y: 0f)
            );
            estuaries.AddTriangleUV(
                uv1: new Vector2(x: 0f, y: 0f), uv2: new Vector2(x: 1f, y: 1f), uv3: new Vector2(x: 1f, y: 1f)
            );
            estuaries.AddQuadUV(
                uv1: new Vector2(x: 0f, y: 0f), uv2: new Vector2(x: 0f, y: 0f),
                uv3: new Vector2(x: 1f, y: 1f), uv4: new Vector2(x: 0f, y: 1f)
            );
            estuaries.AddQuadCellData(
                indices: indices, weights1: weights2, weights2: weights1, weights3: weights2, weights4: weights1
            );
            estuaries.AddTriangleCellData(indices: indices, weights1: weights1, weights2: weights2, weights3: weights2);
            estuaries.AddQuadCellData(indices: indices, weights1: weights1, weights2: weights2);

            if (incomingRiver)
            {
                estuaries.AddQuadUV2(
                    uv1: new Vector2(x: 1.5f, y: 1f), uv2: new Vector2(x: 0.7f, y: 1.15f),
                    uv3: new Vector2(x: 1f, y: 0.8f), uv4: new Vector2(x: 0.5f, y: 1.1f)
                );
                estuaries.AddTriangleUV2(
                    uv1: new Vector2(x: 0.5f, y: 1.1f),
                    uv2: new Vector2(x: 1f, y: 0.8f),
                    uv3: new Vector2(x: 0f, y: 0.8f)
                );
                estuaries.AddQuadUV2(
                    uv1: new Vector2(x: 0.5f, y: 1.1f), uv2: new Vector2(x: 0.3f, y: 1.15f),
                    uv3: new Vector2(x: 0f, y: 0.8f), uv4: new Vector2(x: -0.5f, y: 1f)
                );
            }
            else
            {
                estuaries.AddQuadUV2(
                    uv1: new Vector2(x: -0.5f, y: -0.2f), uv2: new Vector2(x: 0.3f, y: -0.35f),
                    uv3: new Vector2(x: 0f, y: 0f), uv4: new Vector2(x: 0.5f, y: -0.3f)
                );
                estuaries.AddTriangleUV2(
                    uv1: new Vector2(x: 0.5f, y: -0.3f),
                    uv2: new Vector2(x: 0f, y: 0f),
                    uv3: new Vector2(x: 1f, y: 0f)
                );
                estuaries.AddQuadUV2(
                    uv1: new Vector2(x: 0.5f, y: -0.3f), uv2: new Vector2(x: 0.7f, y: -0.35f),
                    uv3: new Vector2(x: 1f, y: 0f), uv4: new Vector2(x: 1.5f, y: -0.2f)
                );
            }
        }

        private void TriangulateWithoutRiver(
            HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
        )
        {
            TriangulateEdgeFan(center: center, edge: e, index: cell.Index);

            if (cell.HasRoads)
            {
                var interpolators = GetRoadInterpolators(direction: direction, cell: cell);
                TriangulateRoad(
                    center: center,
                    mL: Vector3.Lerp(a: center, b: e.v1, t: interpolators.x),
                    mR: Vector3.Lerp(a: center, b: e.v5, t: interpolators.y),
                    e: e, hasRoadThroughCellEdge: cell.HasRoadThroughEdge(direction: direction), index: cell.Index
                );
            }
        }

        private Vector2 GetRoadInterpolators(HexDirection direction, HexCell cell)
        {
            Vector2 interpolators;
            if (cell.HasRoadThroughEdge(direction: direction))
            {
                interpolators.x = interpolators.y = 0.5f;
            }
            else
            {
                interpolators.x =
                    cell.HasRoadThroughEdge(direction: direction.Previous()) ? 0.5f : 0.25f;
                interpolators.y =
                    cell.HasRoadThroughEdge(direction: direction.Next()) ? 0.5f : 0.25f;
            }

            return interpolators;
        }

        private void TriangulateAdjacentToRiver(
            HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
        )
        {
            if (cell.HasRoads)
            {
                TriangulateRoadAdjacentToRiver(direction: direction, cell: cell, center: center, e: e);
            }

            if (cell.HasRiverThroughEdge(direction: direction.Next()))
            {
                if (cell.HasRiverThroughEdge(direction: direction.Previous()))
                {
                    center += HexMetrics.GetSolidEdgeMiddle(direction: direction) *
                              (HexMetrics.innerToOuter * 0.5f);
                }
                else if (
                    cell.HasRiverThroughEdge(direction: direction.Previous2())
                )
                {
                    center += HexMetrics.GetFirstSolidCorner(direction: direction) * 0.25f;
                }
            }
            else if (
                cell.HasRiverThroughEdge(direction: direction.Previous()) &&
                cell.HasRiverThroughEdge(direction: direction.Next2())
            )
            {
                center += HexMetrics.GetSecondSolidCorner(direction: direction) * 0.25f;
            }

            var m = new EdgeVertices(
                corner1: Vector3.Lerp(a: center, b: e.v1, t: 0.5f),
                corner2: Vector3.Lerp(a: center, b: e.v5, t: 0.5f)
            );

            TriangulateEdgeStrip(
                e1: m, w1: weights1, index1: cell.Index,
                e2: e, w2: weights1, index2: cell.Index
            );
            TriangulateEdgeFan(center: center, edge: m, index: cell.Index);

            if (!cell.IsUnderwater && !cell.HasRoadThroughEdge(direction: direction))
            {
                features.AddFeature(cell: cell, position: (center + e.v1 + e.v5) * (1f / 3f));
            }
        }

        private void TriangulateRoadAdjacentToRiver(
            HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
        )
        {
            var hasRoadThroughEdge = cell.HasRoadThroughEdge(direction: direction);
            var previousHasRiver = cell.HasRiverThroughEdge(direction: direction.Previous());
            var nextHasRiver = cell.HasRiverThroughEdge(direction: direction.Next());
            var interpolators = GetRoadInterpolators(direction: direction, cell: cell);
            var roadCenter = center;

            if (cell.HasRiverBeginOrEnd)
            {
                roadCenter += HexMetrics.GetSolidEdgeMiddle(
                                  direction: cell.RiverBeginOrEndDirection.Opposite()
                              ) * (1f / 3f);
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Opposite())
            {
                Vector3 corner;
                if (previousHasRiver)
                {
                    if (
                        !hasRoadThroughEdge &&
                        !cell.HasRoadThroughEdge(direction: direction.Next())
                    )
                    {
                        return;
                    }

                    corner = HexMetrics.GetSecondSolidCorner(direction: direction);
                }
                else
                {
                    if (
                        !hasRoadThroughEdge &&
                        !cell.HasRoadThroughEdge(direction: direction.Previous())
                    )
                    {
                        return;
                    }

                    corner = HexMetrics.GetFirstSolidCorner(direction: direction);
                }

                roadCenter += corner * 0.5f;
                if (cell.IncomingRiver == direction.Next() && (
                        cell.HasRoadThroughEdge(direction: direction.Next2()) ||
                        cell.HasRoadThroughEdge(direction: direction.Opposite())
                    ))
                {
                    features.AddBridge(roadCenter1: roadCenter, roadCenter2: center - corner * 0.5f);
                }

                center += corner * 0.25f;
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Previous())
            {
                roadCenter -= HexMetrics.GetSecondCorner(direction: cell.IncomingRiver) * 0.2f;
            }
            else if (cell.IncomingRiver == cell.OutgoingRiver.Next())
            {
                roadCenter -= HexMetrics.GetFirstCorner(direction: cell.IncomingRiver) * 0.2f;
            }
            else if (previousHasRiver && nextHasRiver)
            {
                if (!hasRoadThroughEdge)
                {
                    return;
                }

                var offset = HexMetrics.GetSolidEdgeMiddle(direction: direction) *
                             HexMetrics.innerToOuter;
                roadCenter += offset * 0.7f;
                center += offset * 0.5f;
            }
            else
            {
                HexDirection middle;
                if (previousHasRiver)
                {
                    middle = direction.Next();
                }
                else if (nextHasRiver)
                {
                    middle = direction.Previous();
                }
                else
                {
                    middle = direction;
                }

                if (
                    !cell.HasRoadThroughEdge(direction: middle) &&
                    !cell.HasRoadThroughEdge(direction: middle.Previous()) &&
                    !cell.HasRoadThroughEdge(direction: middle.Next())
                )
                {
                    return;
                }

                var offset = HexMetrics.GetSolidEdgeMiddle(direction: middle);
                roadCenter += offset * 0.25f;
                if (
                    direction == middle &&
                    cell.HasRoadThroughEdge(direction: direction.Opposite())
                )
                {
                    features.AddBridge(
                        roadCenter1: roadCenter,
                        roadCenter2: center - offset * (HexMetrics.innerToOuter * 0.7f)
                    );
                }
            }

            var mL = Vector3.Lerp(a: roadCenter, b: e.v1, t: interpolators.x);
            var mR = Vector3.Lerp(a: roadCenter, b: e.v5, t: interpolators.y);
            TriangulateRoad(center: roadCenter, mL: mL, mR: mR, e: e, hasRoadThroughCellEdge: hasRoadThroughEdge,
                index: cell.Index);
            if (previousHasRiver)
            {
                TriangulateRoadEdge(center: roadCenter, mL: center, mR: mL, index: cell.Index);
            }

            if (nextHasRiver)
            {
                TriangulateRoadEdge(center: roadCenter, mL: mR, mR: center, index: cell.Index);
            }
        }

        private void TriangulateWithRiverBeginOrEnd(
            HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
        )
        {
            var m = new EdgeVertices(
                corner1: Vector3.Lerp(a: center, b: e.v1, t: 0.5f),
                corner2: Vector3.Lerp(a: center, b: e.v5, t: 0.5f)
            );
            m.v3.y = e.v3.y;

            TriangulateEdgeStrip(
                e1: m, w1: weights1, index1: cell.Index,
                e2: e, w2: weights1, index2: cell.Index
            );
            TriangulateEdgeFan(center: center, edge: m, index: cell.Index);

            if (!cell.IsUnderwater)
            {
                var reversed = cell.HasIncomingRiver;
                Vector3 indices;
                indices.x = indices.y = indices.z = cell.Index;
                TriangulateRiverQuad(
                    v1: m.v2, v2: m.v4, v3: e.v2, v4: e.v4,
                    y: cell.RiverSurfaceY, v: 0.6f, reversed: reversed, indices: indices
                );
                center.y = m.v2.y = m.v4.y = cell.RiverSurfaceY;
                rivers.AddTriangle(v1: center, v2: m.v2, v3: m.v4);
                if (reversed)
                {
                    rivers.AddTriangleUV(
                        uv1: new Vector2(x: 0.5f, y: 0.4f),
                        uv2: new Vector2(x: 1f, y: 0.2f), uv3: new Vector2(x: 0f, y: 0.2f)
                    );
                }
                else
                {
                    rivers.AddTriangleUV(
                        uv1: new Vector2(x: 0.5f, y: 0.4f),
                        uv2: new Vector2(x: 0f, y: 0.6f), uv3: new Vector2(x: 1f, y: 0.6f)
                    );
                }

                rivers.AddTriangleCellData(indices: indices, weights: weights1);
            }
        }

        private void TriangulateWithRiver(
            HexDirection direction, HexCell cell, Vector3 center, EdgeVertices e
        )
        {
            Vector3 centerL, centerR;
            if (cell.HasRiverThroughEdge(direction: direction.Opposite()))
            {
                centerL = center +
                          HexMetrics.GetFirstSolidCorner(direction: direction.Previous()) * 0.25f;
                centerR = center +
                          HexMetrics.GetSecondSolidCorner(direction: direction.Next()) * 0.25f;
            }
            else if (cell.HasRiverThroughEdge(direction: direction.Next()))
            {
                centerL = center;
                centerR = Vector3.Lerp(a: center, b: e.v5, t: 2f / 3f);
            }
            else if (cell.HasRiverThroughEdge(direction: direction.Previous()))
            {
                centerL = Vector3.Lerp(a: center, b: e.v1, t: 2f / 3f);
                centerR = center;
            }
            else if (cell.HasRiverThroughEdge(direction: direction.Next2()))
            {
                centerL = center;
                centerR = center +
                          HexMetrics.GetSolidEdgeMiddle(direction: direction.Next()) *
                          (0.5f * HexMetrics.innerToOuter);
            }
            else
            {
                centerL = center +
                          HexMetrics.GetSolidEdgeMiddle(direction: direction.Previous()) *
                          (0.5f * HexMetrics.innerToOuter);
                centerR = center;
            }

            center = Vector3.Lerp(a: centerL, b: centerR, t: 0.5f);

            var m = new EdgeVertices(
                corner1: Vector3.Lerp(a: centerL, b: e.v1, t: 0.5f),
                corner2: Vector3.Lerp(a: centerR, b: e.v5, t: 0.5f),
                outerStep: 1f / 6f
            );
            m.v3.y = center.y = e.v3.y;

            TriangulateEdgeStrip(
                e1: m, w1: weights1, index1: cell.Index,
                e2: e, w2: weights1, index2: cell.Index
            );

            terrain.AddTriangle(v1: centerL, v2: m.v1, v3: m.v2);
            terrain.AddQuad(v1: centerL, v2: center, v3: m.v2, v4: m.v3);
            terrain.AddQuad(v1: center, v2: centerR, v3: m.v3, v4: m.v4);
            terrain.AddTriangle(v1: centerR, v2: m.v4, v3: m.v5);

            Vector3 indices;
            indices.x = indices.y = indices.z = cell.Index;
            terrain.AddTriangleCellData(indices: indices, weights: weights1);
            terrain.AddQuadCellData(indices: indices, weights: weights1);
            terrain.AddQuadCellData(indices: indices, weights: weights1);
            terrain.AddTriangleCellData(indices: indices, weights: weights1);

            if (!cell.IsUnderwater)
            {
                var reversed = cell.IncomingRiver == direction;
                TriangulateRiverQuad(
                    v1: centerL, v2: centerR, v3: m.v2, v4: m.v4,
                    y: cell.RiverSurfaceY, v: 0.4f, reversed: reversed, indices: indices
                );
                TriangulateRiverQuad(
                    v1: m.v2, v2: m.v4, v3: e.v2, v4: e.v4,
                    y: cell.RiverSurfaceY, v: 0.6f, reversed: reversed, indices: indices
                );
            }
        }

        private void TriangulateConnection(
            HexDirection direction, HexCell cell, EdgeVertices e1
        )
        {
            var neighbor = cell.GetNeighbor(direction: direction);
            if (neighbor == null)
            {
                return;
            }

            var bridge = HexMetrics.GetBridge(direction: direction);
            bridge.y = neighbor.Position.y - cell.Position.y;
            var e2 = new EdgeVertices(
                corner1: e1.v1 + bridge,
                corner2: e1.v5 + bridge
            );

            var hasRiver = cell.HasRiverThroughEdge(direction: direction);
            var hasRoad = cell.HasRoadThroughEdge(direction: direction);

            if (hasRiver)
            {
                e2.v3.y = neighbor.StreamBedY;
                Vector3 indices;
                indices.x = indices.z = cell.Index;
                indices.y = neighbor.Index;

                if (!cell.IsUnderwater)
                {
                    if (!neighbor.IsUnderwater)
                    {
                        TriangulateRiverQuad(
                            v1: e1.v2, v2: e1.v4, v3: e2.v2, v4: e2.v4,
                            y1: cell.RiverSurfaceY, y2: neighbor.RiverSurfaceY, v: 0.8f,
                            reversed: cell.HasIncomingRiver && cell.IncomingRiver == direction,
                            indices: indices
                        );
                    }
                    else if (cell.Elevation > neighbor.WaterLevel)
                    {
                        TriangulateWaterfallInWater(
                            v1: e1.v2, v2: e1.v4, v3: e2.v2, v4: e2.v4,
                            y1: cell.RiverSurfaceY, y2: neighbor.RiverSurfaceY,
                            waterY: neighbor.WaterSurfaceY, indices: indices
                        );
                    }
                }
                else if (
                    !neighbor.IsUnderwater &&
                    neighbor.Elevation > cell.WaterLevel
                )
                {
                    TriangulateWaterfallInWater(
                        v1: e2.v4, v2: e2.v2, v3: e1.v4, v4: e1.v2,
                        y1: neighbor.RiverSurfaceY, y2: cell.RiverSurfaceY,
                        waterY: cell.WaterSurfaceY, indices: indices
                    );
                }
            }

            if (cell.GetEdgeType(direction: direction) == HexEdgeType.Slope)
            {
                TriangulateEdgeTerraces(begin: e1, beginCell: cell, end: e2, endCell: neighbor, hasRoad: hasRoad);
            }
            else
            {
                TriangulateEdgeStrip(
                    e1: e1, w1: weights1, index1: cell.Index,
                    e2: e2, w2: weights2, index2: neighbor.Index, hasRoad: hasRoad
                );
            }

            features.AddWall(near: e1, nearCell: cell, far: e2, farCell: neighbor, hasRiver: hasRiver,
                hasRoad: hasRoad);

            var nextNeighbor = cell.GetNeighbor(direction: direction.Next());
            if (direction <= HexDirection.E && nextNeighbor != null)
            {
                var v5 = e1.v5 + HexMetrics.GetBridge(direction: direction.Next());
                v5.y = nextNeighbor.Position.y;

                if (cell.Elevation <= neighbor.Elevation)
                {
                    if (cell.Elevation <= nextNeighbor.Elevation)
                    {
                        TriangulateCorner(
                            bottom: e1.v5, bottomCell: cell, left: e2.v5, leftCell: neighbor, right: v5,
                            rightCell: nextNeighbor
                        );
                    }
                    else
                    {
                        TriangulateCorner(
                            bottom: v5, bottomCell: nextNeighbor, left: e1.v5, leftCell: cell, right: e2.v5,
                            rightCell: neighbor
                        );
                    }
                }
                else if (neighbor.Elevation <= nextNeighbor.Elevation)
                {
                    TriangulateCorner(
                        bottom: e2.v5, bottomCell: neighbor, left: v5, leftCell: nextNeighbor, right: e1.v5,
                        rightCell: cell
                    );
                }
                else
                {
                    TriangulateCorner(
                        bottom: v5, bottomCell: nextNeighbor, left: e1.v5, leftCell: cell, right: e2.v5,
                        rightCell: neighbor
                    );
                }
            }
        }

        private void TriangulateWaterfallInWater(
            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
            float y1, float y2, float waterY, Vector3 indices
        )
        {
            v1.y = v2.y = y1;
            v3.y = v4.y = y2;
            v1 = HexMetrics.Perturb(position: v1);
            v2 = HexMetrics.Perturb(position: v2);
            v3 = HexMetrics.Perturb(position: v3);
            v4 = HexMetrics.Perturb(position: v4);
            var t = (waterY - y2) / (y1 - y2);
            v3 = Vector3.Lerp(a: v3, b: v1, t: t);
            v4 = Vector3.Lerp(a: v4, b: v2, t: t);
            rivers.AddQuadUnperturbed(v1: v1, v2: v2, v3: v3, v4: v4);
            rivers.AddQuadUV(uMin: 0f, uMax: 1f, vMin: 0.8f, vMax: 1f);
            rivers.AddQuadCellData(indices: indices, weights1: weights1, weights2: weights2);
        }

        private void TriangulateCorner(
            Vector3 bottom, HexCell bottomCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
        {
            var leftEdgeType = bottomCell.GetEdgeType(otherCell: leftCell);
            var rightEdgeType = bottomCell.GetEdgeType(otherCell: rightCell);

            if (leftEdgeType == HexEdgeType.Slope)
            {
                if (rightEdgeType == HexEdgeType.Slope)
                {
                    TriangulateCornerTerraces(
                        begin: bottom, beginCell: bottomCell, left: left, leftCell: leftCell, right: right,
                        rightCell: rightCell
                    );
                }
                else if (rightEdgeType == HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(
                        begin: left, beginCell: leftCell, left: right, leftCell: rightCell, right: bottom,
                        rightCell: bottomCell
                    );
                }
                else
                {
                    TriangulateCornerTerracesCliff(
                        begin: bottom, beginCell: bottomCell, left: left, leftCell: leftCell, right: right,
                        rightCell: rightCell
                    );
                }
            }
            else if (rightEdgeType == HexEdgeType.Slope)
            {
                if (leftEdgeType == HexEdgeType.Flat)
                {
                    TriangulateCornerTerraces(
                        begin: right, beginCell: rightCell, left: bottom, leftCell: bottomCell, right: left,
                        rightCell: leftCell
                    );
                }
                else
                {
                    TriangulateCornerCliffTerraces(
                        begin: bottom, beginCell: bottomCell, left: left, leftCell: leftCell, right: right,
                        rightCell: rightCell
                    );
                }
            }
            else if (leftCell.GetEdgeType(otherCell: rightCell) == HexEdgeType.Slope)
            {
                if (leftCell.Elevation < rightCell.Elevation)
                {
                    TriangulateCornerCliffTerraces(
                        begin: right, beginCell: rightCell, left: bottom, leftCell: bottomCell, right: left,
                        rightCell: leftCell
                    );
                }
                else
                {
                    TriangulateCornerTerracesCliff(
                        begin: left, beginCell: leftCell, left: right, leftCell: rightCell, right: bottom,
                        rightCell: bottomCell
                    );
                }
            }
            else
            {
                terrain.AddTriangle(v1: bottom, v2: left, v3: right);
                Vector3 indices;
                indices.x = bottomCell.Index;
                indices.y = leftCell.Index;
                indices.z = rightCell.Index;
                terrain.AddTriangleCellData(indices: indices, weights1: weights1, weights2: weights2,
                    weights3: weights3);
            }

            features.AddWall(c1: bottom, cell1: bottomCell, c2: left, cell2: leftCell, c3: right, cell3: rightCell);
        }

        private void TriangulateEdgeTerraces(
            EdgeVertices begin, HexCell beginCell,
            EdgeVertices end, HexCell endCell,
            bool hasRoad
        )
        {
            var e2 = EdgeVertices.TerraceLerp(a: begin, b: end, step: 1);
            var w2 = HexMetrics.TerraceLerp(a: weights1, b: weights2, step: 1);
            float i1 = beginCell.Index;
            float i2 = endCell.Index;

            TriangulateEdgeStrip(e1: begin, w1: weights1, index1: i1, e2: e2, w2: w2, index2: i2, hasRoad: hasRoad);

            for (var i = 2; i < HexMetrics.terraceSteps; i++)
            {
                var e1 = e2;
                var w1 = w2;
                e2 = EdgeVertices.TerraceLerp(a: begin, b: end, step: i);
                w2 = HexMetrics.TerraceLerp(a: weights1, b: weights2, step: i);
                TriangulateEdgeStrip(e1: e1, w1: w1, index1: i1, e2: e2, w2: w2, index2: i2, hasRoad: hasRoad);
            }

            TriangulateEdgeStrip(e1: e2, w1: w2, index1: i1, e2: end, w2: weights2, index2: i2, hasRoad: hasRoad);
        }

        private void TriangulateCornerTerraces(
            Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
        {
            var v3 = HexMetrics.TerraceLerp(a: begin, b: left, step: 1);
            var v4 = HexMetrics.TerraceLerp(a: begin, b: right, step: 1);
            var w3 = HexMetrics.TerraceLerp(a: weights1, b: weights2, step: 1);
            var w4 = HexMetrics.TerraceLerp(a: weights1, b: weights3, step: 1);
            Vector3 indices;
            indices.x = beginCell.Index;
            indices.y = leftCell.Index;
            indices.z = rightCell.Index;

            terrain.AddTriangle(v1: begin, v2: v3, v3: v4);
            terrain.AddTriangleCellData(indices: indices, weights1: weights1, weights2: w3, weights3: w4);

            for (var i = 2; i < HexMetrics.terraceSteps; i++)
            {
                var v1 = v3;
                var v2 = v4;
                var w1 = w3;
                var w2 = w4;
                v3 = HexMetrics.TerraceLerp(a: begin, b: left, step: i);
                v4 = HexMetrics.TerraceLerp(a: begin, b: right, step: i);
                w3 = HexMetrics.TerraceLerp(a: weights1, b: weights2, step: i);
                w4 = HexMetrics.TerraceLerp(a: weights1, b: weights3, step: i);
                terrain.AddQuad(v1: v1, v2: v2, v3: v3, v4: v4);
                terrain.AddQuadCellData(indices: indices, weights1: w1, weights2: w2, weights3: w3, weights4: w4);
            }

            terrain.AddQuad(v1: v3, v2: v4, v3: left, v4: right);
            terrain.AddQuadCellData(indices: indices, weights1: w3, weights2: w4, weights3: weights2,
                weights4: weights3);
        }

        private void TriangulateCornerTerracesCliff(
            Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
        {
            var b = 1f / (rightCell.Elevation - beginCell.Elevation);
            if (b < 0)
            {
                b = -b;
            }

            var boundary = Vector3.Lerp(
                a: HexMetrics.Perturb(position: begin), b: HexMetrics.Perturb(position: right), t: b
            );
            var boundaryWeights = Color.Lerp(a: weights1, b: weights3, t: b);
            Vector3 indices;
            indices.x = beginCell.Index;
            indices.y = leftCell.Index;
            indices.z = rightCell.Index;

            TriangulateBoundaryTriangle(
                begin: begin, beginWeights: weights1, left: left, leftWeights: weights2, boundary: boundary,
                boundaryWeights: boundaryWeights, indices: indices
            );

            if (leftCell.GetEdgeType(otherCell: rightCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(
                    begin: left, beginWeights: weights2, left: right, leftWeights: weights3,
                    boundary: boundary, boundaryWeights: boundaryWeights, indices: indices
                );
            }
            else
            {
                terrain.AddTriangleUnperturbed(
                    v1: HexMetrics.Perturb(position: left), v2: HexMetrics.Perturb(position: right), v3: boundary
                );
                terrain.AddTriangleCellData(
                    indices: indices, weights1: weights2, weights2: weights3, weights3: boundaryWeights
                );
            }
        }

        private void TriangulateCornerCliffTerraces(
            Vector3 begin, HexCell beginCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
        {
            var b = 1f / (leftCell.Elevation - beginCell.Elevation);
            if (b < 0)
            {
                b = -b;
            }

            var boundary = Vector3.Lerp(
                a: HexMetrics.Perturb(position: begin), b: HexMetrics.Perturb(position: left), t: b
            );
            var boundaryWeights = Color.Lerp(a: weights1, b: weights2, t: b);
            Vector3 indices;
            indices.x = beginCell.Index;
            indices.y = leftCell.Index;
            indices.z = rightCell.Index;

            TriangulateBoundaryTriangle(
                begin: right, beginWeights: weights3, left: begin, leftWeights: weights1, boundary: boundary,
                boundaryWeights: boundaryWeights, indices: indices
            );

            if (leftCell.GetEdgeType(otherCell: rightCell) == HexEdgeType.Slope)
            {
                TriangulateBoundaryTriangle(
                    begin: left, beginWeights: weights2, left: right, leftWeights: weights3,
                    boundary: boundary, boundaryWeights: boundaryWeights, indices: indices
                );
            }
            else
            {
                terrain.AddTriangleUnperturbed(
                    v1: HexMetrics.Perturb(position: left), v2: HexMetrics.Perturb(position: right), v3: boundary
                );
                terrain.AddTriangleCellData(
                    indices: indices, weights1: weights2, weights2: weights3, weights3: boundaryWeights
                );
            }
        }

        private void TriangulateBoundaryTriangle(
            Vector3 begin, Color beginWeights,
            Vector3 left, Color leftWeights,
            Vector3 boundary, Color boundaryWeights, Vector3 indices
        )
        {
            var v2 = HexMetrics.Perturb(position: HexMetrics.TerraceLerp(a: begin, b: left, step: 1));
            var w2 = HexMetrics.TerraceLerp(a: beginWeights, b: leftWeights, step: 1);

            terrain.AddTriangleUnperturbed(v1: HexMetrics.Perturb(position: begin), v2: v2, v3: boundary);
            terrain.AddTriangleCellData(indices: indices, weights1: beginWeights, weights2: w2,
                weights3: boundaryWeights);

            for (var i = 2; i < HexMetrics.terraceSteps; i++)
            {
                var v1 = v2;
                var w1 = w2;
                v2 = HexMetrics.Perturb(position: HexMetrics.TerraceLerp(a: begin, b: left, step: i));
                w2 = HexMetrics.TerraceLerp(a: beginWeights, b: leftWeights, step: i);
                terrain.AddTriangleUnperturbed(v1: v1, v2: v2, v3: boundary);
                terrain.AddTriangleCellData(indices: indices, weights1: w1, weights2: w2, weights3: boundaryWeights);
            }

            terrain.AddTriangleUnperturbed(v1: v2, v2: HexMetrics.Perturb(position: left), v3: boundary);
            terrain.AddTriangleCellData(indices: indices, weights1: w2, weights2: leftWeights,
                weights3: boundaryWeights);
        }

        private void TriangulateEdgeFan(Vector3 center, EdgeVertices edge, float index)
        {
            terrain.AddTriangle(v1: center, v2: edge.v1, v3: edge.v2);
            terrain.AddTriangle(v1: center, v2: edge.v2, v3: edge.v3);
            terrain.AddTriangle(v1: center, v2: edge.v3, v3: edge.v4);
            terrain.AddTriangle(v1: center, v2: edge.v4, v3: edge.v5);

            Vector3 indices;
            indices.x = indices.y = indices.z = index;
            terrain.AddTriangleCellData(indices: indices, weights: weights1);
            terrain.AddTriangleCellData(indices: indices, weights: weights1);
            terrain.AddTriangleCellData(indices: indices, weights: weights1);
            terrain.AddTriangleCellData(indices: indices, weights: weights1);
        }

        private void TriangulateEdgeStrip(
            EdgeVertices e1, Color w1, float index1,
            EdgeVertices e2, Color w2, float index2,
            bool hasRoad = false
        )
        {
            terrain.AddQuad(v1: e1.v1, v2: e1.v2, v3: e2.v1, v4: e2.v2);
            terrain.AddQuad(v1: e1.v2, v2: e1.v3, v3: e2.v2, v4: e2.v3);
            terrain.AddQuad(v1: e1.v3, v2: e1.v4, v3: e2.v3, v4: e2.v4);
            terrain.AddQuad(v1: e1.v4, v2: e1.v5, v3: e2.v4, v4: e2.v5);

            Vector3 indices;
            indices.x = indices.z = index1;
            indices.y = index2;
            terrain.AddQuadCellData(indices: indices, weights1: w1, weights2: w2);
            terrain.AddQuadCellData(indices: indices, weights1: w1, weights2: w2);
            terrain.AddQuadCellData(indices: indices, weights1: w1, weights2: w2);
            terrain.AddQuadCellData(indices: indices, weights1: w1, weights2: w2);

            if (hasRoad)
            {
                TriangulateRoadSegment(
                    v1: e1.v2, v2: e1.v3, v3: e1.v4, v4: e2.v2, v5: e2.v3, v6: e2.v4, w1: w1, w2: w2, indices: indices
                );
            }
        }

        private void TriangulateRiverQuad(
            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
            float y, float v, bool reversed, Vector3 indices
        )
        {
            TriangulateRiverQuad(v1: v1, v2: v2, v3: v3, v4: v4, y1: y, y2: y, v: v, reversed: reversed,
                indices: indices);
        }

        private void TriangulateRiverQuad(
            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4,
            float y1, float y2, float v, bool reversed, Vector3 indices
        )
        {
            v1.y = v2.y = y1;
            v3.y = v4.y = y2;
            rivers.AddQuad(v1: v1, v2: v2, v3: v3, v4: v4);
            if (reversed)
            {
                rivers.AddQuadUV(uMin: 1f, uMax: 0f, vMin: 0.8f - v, vMax: 0.6f - v);
            }
            else
            {
                rivers.AddQuadUV(uMin: 0f, uMax: 1f, vMin: v, vMax: v + 0.2f);
            }

            rivers.AddQuadCellData(indices: indices, weights1: weights1, weights2: weights2);
        }

        private void TriangulateRoad(
            Vector3 center, Vector3 mL, Vector3 mR,
            EdgeVertices e, bool hasRoadThroughCellEdge, float index
        )
        {
            if (hasRoadThroughCellEdge)
            {
                Vector3 indices;
                indices.x = indices.y = indices.z = index;
                var mC = Vector3.Lerp(a: mL, b: mR, t: 0.5f);
                TriangulateRoadSegment(
                    v1: mL, v2: mC, v3: mR, v4: e.v2, v5: e.v3, v6: e.v4,
                    w1: weights1, w2: weights1, indices: indices
                );
                roads.AddTriangle(v1: center, v2: mL, v3: mC);
                roads.AddTriangle(v1: center, v2: mC, v3: mR);
                roads.AddTriangleUV(
                    uv1: new Vector2(x: 1f, y: 0f), uv2: new Vector2(x: 0f, y: 0f), uv3: new Vector2(x: 1f, y: 0f)
                );
                roads.AddTriangleUV(
                    uv1: new Vector2(x: 1f, y: 0f), uv2: new Vector2(x: 1f, y: 0f), uv3: new Vector2(x: 0f, y: 0f)
                );
                roads.AddTriangleCellData(indices: indices, weights: weights1);
                roads.AddTriangleCellData(indices: indices, weights: weights1);
            }
            else
            {
                TriangulateRoadEdge(center: center, mL: mL, mR: mR, index: index);
            }
        }

        private void TriangulateRoadEdge(
            Vector3 center, Vector3 mL, Vector3 mR, float index
        )
        {
            roads.AddTriangle(v1: center, v2: mL, v3: mR);
            roads.AddTriangleUV(
                uv1: new Vector2(x: 1f, y: 0f), uv2: new Vector2(x: 0f, y: 0f), uv3: new Vector2(x: 0f, y: 0f)
            );
            Vector3 indices;
            indices.x = indices.y = indices.z = index;
            roads.AddTriangleCellData(indices: indices, weights: weights1);
        }

        private void TriangulateRoadSegment(
            Vector3 v1, Vector3 v2, Vector3 v3,
            Vector3 v4, Vector3 v5, Vector3 v6,
            Color w1, Color w2, Vector3 indices
        )
        {
            roads.AddQuad(v1: v1, v2: v2, v3: v4, v4: v5);
            roads.AddQuad(v1: v2, v2: v3, v3: v5, v4: v6);
            roads.AddQuadUV(uMin: 0f, uMax: 1f, vMin: 0f, vMax: 0f);
            roads.AddQuadUV(uMin: 1f, uMax: 0f, vMin: 0f, vMax: 0f);
            roads.AddQuadCellData(indices: indices, weights1: w1, weights2: w2);
            roads.AddQuadCellData(indices: indices, weights1: w1, weights2: w2);
        }
    }
}