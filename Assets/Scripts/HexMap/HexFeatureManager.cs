using UnityEngine;

namespace HexMap
{
    public class HexFeatureManager : MonoBehaviour
    {
        private Transform container;

        public Transform[] special;

        public HexFeatureCollection[]
            urbanCollections, farmCollections, plantCollections;

        public HexMesh walls;

        public Transform wallTower, bridge;

        public void Clear()
        {
            if (container)
            {
                Destroy(obj: container.gameObject);
            }

            container = new GameObject(name: "Features Container").transform;
            container.SetParent(parent: transform, worldPositionStays: false);
            walls.Clear();
        }

        public void Apply()
        {
            walls.Apply();
        }

        private Transform PickPrefab(
            HexFeatureCollection[] collection,
            int level, float hash, float choice
        )
        {
            if (level > 0)
            {
                var thresholds = HexMetrics.GetFeatureThresholds(level: level - 1);
                for (var i = 0; i < thresholds.Length; i++)
                {
                    if (hash < thresholds[i])
                    {
                        return collection[i].Pick(choice: choice);
                    }
                }
            }

            return null;
        }

        public void AddBridge(Vector3 roadCenter1, Vector3 roadCenter2)
        {
            roadCenter1 = HexMetrics.Perturb(position: roadCenter1);
            roadCenter2 = HexMetrics.Perturb(position: roadCenter2);
            var instance = Instantiate(original: bridge);
            instance.localPosition = (roadCenter1 + roadCenter2) * 0.5f;
            instance.forward = roadCenter2 - roadCenter1;
            var length = Vector3.Distance(a: roadCenter1, b: roadCenter2);
            instance.localScale = new Vector3(
                x: 1f, y: 1f, z: length * (1f / HexMetrics.bridgeDesignLength)
            );
            instance.SetParent(parent: container, worldPositionStays: false);
        }

        public void AddFeature(HexCell cell, Vector3 position)
        {
            if (cell.IsSpecial)
            {
                return;
            }

            var hash = HexMetrics.SampleHashGrid(position: position);
            var prefab = PickPrefab(
                collection: urbanCollections, level: cell.UrbanLevel, hash: hash.a, choice: hash.d
            );
            var otherPrefab = PickPrefab(
                collection: farmCollections, level: cell.FarmLevel, hash: hash.b, choice: hash.d
            );
            var usedHash = hash.a;
            if (prefab)
            {
                if (otherPrefab && hash.b < hash.a)
                {
                    prefab = otherPrefab;
                    usedHash = hash.b;
                }
            }
            else if (otherPrefab)
            {
                prefab = otherPrefab;
                usedHash = hash.b;
            }

            otherPrefab = PickPrefab(
                collection: plantCollections, level: cell.PlantLevel, hash: hash.c, choice: hash.d
            );
            if (prefab)
            {
                if (otherPrefab && hash.c < usedHash)
                {
                    prefab = otherPrefab;
                }
            }
            else if (otherPrefab)
            {
                prefab = otherPrefab;
            }
            else
            {
                return;
            }

            var instance = Instantiate(original: prefab);
            position.y += instance.localScale.y * 0.5f;
            instance.localPosition = HexMetrics.Perturb(position: position);
            instance.localRotation = Quaternion.Euler(x: 0f, y: 360f * hash.e, z: 0f);
            instance.SetParent(parent: container, worldPositionStays: false);
        }

        public void AddSpecialFeature(HexCell cell, Vector3 position)
        {
            var hash = HexMetrics.SampleHashGrid(position: position);
            var instance = Instantiate(original: special[cell.SpecialIndex - 1]);
            instance.localPosition = HexMetrics.Perturb(position: position);
            instance.localRotation = Quaternion.Euler(x: 0f, y: 360f * hash.e, z: 0f);
            instance.SetParent(parent: container, worldPositionStays: false);
        }

        public void AddWall(
            EdgeVertices near, HexCell nearCell,
            EdgeVertices far, HexCell farCell,
            bool hasRiver, bool hasRoad
        )
        {
            if (
                nearCell.Walled != farCell.Walled &&
                !nearCell.IsUnderwater && !farCell.IsUnderwater &&
                nearCell.GetEdgeType(otherCell: farCell) != HexEdgeType.Cliff
            )
            {
                AddWallSegment(nearLeft: near.v1, farLeft: far.v1, nearRight: near.v2, farRight: far.v2);
                if (hasRiver || hasRoad)
                {
                    AddWallCap(near: near.v2, far: far.v2);
                    AddWallCap(near: far.v4, far: near.v4);
                }
                else
                {
                    AddWallSegment(nearLeft: near.v2, farLeft: far.v2, nearRight: near.v3, farRight: far.v3);
                    AddWallSegment(nearLeft: near.v3, farLeft: far.v3, nearRight: near.v4, farRight: far.v4);
                }

                AddWallSegment(nearLeft: near.v4, farLeft: far.v4, nearRight: near.v5, farRight: far.v5);
            }
        }

        public void AddWall(
            Vector3 c1, HexCell cell1,
            Vector3 c2, HexCell cell2,
            Vector3 c3, HexCell cell3
        )
        {
            if (cell1.Walled)
            {
                if (cell2.Walled)
                {
                    if (!cell3.Walled)
                    {
                        AddWallSegment(pivot: c3, pivotCell: cell3, left: c1, leftCell: cell1, right: c2,
                            rightCell: cell2);
                    }
                }
                else if (cell3.Walled)
                {
                    AddWallSegment(pivot: c2, pivotCell: cell2, left: c3, leftCell: cell3, right: c1, rightCell: cell1);
                }
                else
                {
                    AddWallSegment(pivot: c1, pivotCell: cell1, left: c2, leftCell: cell2, right: c3, rightCell: cell3);
                }
            }
            else if (cell2.Walled)
            {
                if (cell3.Walled)
                {
                    AddWallSegment(pivot: c1, pivotCell: cell1, left: c2, leftCell: cell2, right: c3, rightCell: cell3);
                }
                else
                {
                    AddWallSegment(pivot: c2, pivotCell: cell2, left: c3, leftCell: cell3, right: c1, rightCell: cell1);
                }
            }
            else if (cell3.Walled)
            {
                AddWallSegment(pivot: c3, pivotCell: cell3, left: c1, leftCell: cell1, right: c2, rightCell: cell2);
            }
        }

        private void AddWallSegment(
            Vector3 nearLeft, Vector3 farLeft, Vector3 nearRight, Vector3 farRight,
            bool addTower = false
        )
        {
            nearLeft = HexMetrics.Perturb(position: nearLeft);
            farLeft = HexMetrics.Perturb(position: farLeft);
            nearRight = HexMetrics.Perturb(position: nearRight);
            farRight = HexMetrics.Perturb(position: farRight);

            var left = HexMetrics.WallLerp(near: nearLeft, far: farLeft);
            var right = HexMetrics.WallLerp(near: nearRight, far: farRight);

            var leftThicknessOffset =
                HexMetrics.WallThicknessOffset(near: nearLeft, far: farLeft);
            var rightThicknessOffset =
                HexMetrics.WallThicknessOffset(near: nearRight, far: farRight);

            var leftTop = left.y + HexMetrics.wallHeight;
            var rightTop = right.y + HexMetrics.wallHeight;

            Vector3 v1, v2, v3, v4;
            v1 = v3 = left - leftThicknessOffset;
            v2 = v4 = right - rightThicknessOffset;
            v3.y = leftTop;
            v4.y = rightTop;
            walls.AddQuadUnperturbed(v1: v1, v2: v2, v3: v3, v4: v4);

            Vector3 t1 = v3, t2 = v4;

            v1 = v3 = left + leftThicknessOffset;
            v2 = v4 = right + rightThicknessOffset;
            v3.y = leftTop;
            v4.y = rightTop;
            walls.AddQuadUnperturbed(v1: v2, v2: v1, v3: v4, v4: v3);

            walls.AddQuadUnperturbed(v1: t1, v2: t2, v3: v3, v4: v4);

            if (addTower)
            {
                var towerInstance = Instantiate(original: wallTower);
                towerInstance.transform.localPosition = (left + right) * 0.5f;
                var rightDirection = right - left;
                rightDirection.y = 0f;
                towerInstance.transform.right = rightDirection;
                towerInstance.SetParent(parent: container, worldPositionStays: false);
            }
        }

        private void AddWallSegment(
            Vector3 pivot, HexCell pivotCell,
            Vector3 left, HexCell leftCell,
            Vector3 right, HexCell rightCell
        )
        {
            if (pivotCell.IsUnderwater)
            {
                return;
            }

            var hasLeftWall = !leftCell.IsUnderwater &&
                              pivotCell.GetEdgeType(otherCell: leftCell) != HexEdgeType.Cliff;
            var hasRighWall = !rightCell.IsUnderwater &&
                              pivotCell.GetEdgeType(otherCell: rightCell) != HexEdgeType.Cliff;

            if (hasLeftWall)
            {
                if (hasRighWall)
                {
                    var hasTower = false;
                    if (leftCell.Elevation == rightCell.Elevation)
                    {
                        var hash = HexMetrics.SampleHashGrid(
                            position: (pivot + left + right) * (1f / 3f)
                        );
                        hasTower = hash.e < HexMetrics.wallTowerThreshold;
                    }

                    AddWallSegment(nearLeft: pivot, farLeft: left, nearRight: pivot, farRight: right,
                        addTower: hasTower);
                }
                else if (leftCell.Elevation < rightCell.Elevation)
                {
                    AddWallWedge(near: pivot, far: left, point: right);
                }
                else
                {
                    AddWallCap(near: pivot, far: left);
                }
            }
            else if (hasRighWall)
            {
                if (rightCell.Elevation < leftCell.Elevation)
                {
                    AddWallWedge(near: right, far: pivot, point: left);
                }
                else
                {
                    AddWallCap(near: right, far: pivot);
                }
            }
        }

        private void AddWallCap(Vector3 near, Vector3 far)
        {
            near = HexMetrics.Perturb(position: near);
            far = HexMetrics.Perturb(position: far);

            var center = HexMetrics.WallLerp(near: near, far: far);
            var thickness = HexMetrics.WallThicknessOffset(near: near, far: far);

            Vector3 v1, v2, v3, v4;

            v1 = v3 = center - thickness;
            v2 = v4 = center + thickness;
            v3.y = v4.y = center.y + HexMetrics.wallHeight;
            walls.AddQuadUnperturbed(v1: v1, v2: v2, v3: v3, v4: v4);
        }

        private void AddWallWedge(Vector3 near, Vector3 far, Vector3 point)
        {
            near = HexMetrics.Perturb(position: near);
            far = HexMetrics.Perturb(position: far);
            point = HexMetrics.Perturb(position: point);

            var center = HexMetrics.WallLerp(near: near, far: far);
            var thickness = HexMetrics.WallThicknessOffset(near: near, far: far);

            Vector3 v1, v2, v3, v4;
            var pointTop = point;
            point.y = center.y;

            v1 = v3 = center - thickness;
            v2 = v4 = center + thickness;
            v3.y = v4.y = pointTop.y = center.y + HexMetrics.wallHeight;

            walls.AddQuadUnperturbed(v1: v1, v2: point, v3: v3, v4: pointTop);
            walls.AddQuadUnperturbed(v1: point, v2: v2, v3: pointTop, v4: v4);
            walls.AddTriangleUnperturbed(v1: pointTop, v2: v3, v3: v4);
        }
    }
}