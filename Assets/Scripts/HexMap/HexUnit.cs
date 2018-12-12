using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HexMap
{
    public class HexUnit : MonoBehaviour
    {
        private const float rotationSpeed = 180f;
        private const float travelSpeed = 4f;

        public static HexUnit unitPrefab;

        private HexCell location, currentTravelLocation;

        private float orientation;

        private List<HexCell> pathToTravel;

        public HexGrid Grid { get; set; }

        public HexCell Location
        {
            get { return location; }
            set
            {
                if (location)
                {
                    Grid.DecreaseVisibility(fromCell: location, range: VisionRange);
                    location.Unit = null;
                }

                location = value;
                value.Unit = this;
                Grid.IncreaseVisibility(fromCell: value, range: VisionRange);
                transform.localPosition = value.Position;
                Grid.MakeChildOfColumn(child: transform, columnIndex: value.ColumnIndex);
            }
        }

        public float Orientation
        {
            get { return orientation; }
            set
            {
                orientation = value;
                transform.localRotation = Quaternion.Euler(x: 0f, y: value, z: 0f);
            }
        }

        public int Speed
        {
            get { return 24; }
        }

        public int VisionRange
        {
            get { return 3; }
        }

        public void ValidateLocation()
        {
            transform.localPosition = location.Position;
        }

        public bool IsValidDestination(HexCell cell)
        {
            return cell.IsExplored && !cell.IsUnderwater && !cell.Unit;
        }

        public void Travel(List<HexCell> path)
        {
            location.Unit = null;
            location = path[index: path.Count - 1];
            location.Unit = this;
            pathToTravel = path;
            StopAllCoroutines();
            StartCoroutine(routine: TravelPath());
        }

        private IEnumerator TravelPath()
        {
            Vector3 a, b, c = pathToTravel[index: 0].Position;
            yield return LookAt(point: pathToTravel[index: 1].Position);

            if (!currentTravelLocation)
            {
                currentTravelLocation = pathToTravel[index: 0];
            }

            Grid.DecreaseVisibility(fromCell: currentTravelLocation, range: VisionRange);
            var currentColumn = currentTravelLocation.ColumnIndex;

            var t = Time.deltaTime * travelSpeed;
            for (var i = 1; i < pathToTravel.Count; i++)
            {
                currentTravelLocation = pathToTravel[index: i];
                a = c;
                b = pathToTravel[index: i - 1].Position;

                var nextColumn = currentTravelLocation.ColumnIndex;
                if (currentColumn != nextColumn)
                {
                    if (nextColumn < currentColumn - 1)
                    {
                        a.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                        b.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                    }
                    else if (nextColumn > currentColumn + 1)
                    {
                        a.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                        b.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                    }

                    Grid.MakeChildOfColumn(child: transform, columnIndex: nextColumn);
                    currentColumn = nextColumn;
                }

                c = (b + currentTravelLocation.Position) * 0.5f;
                Grid.IncreaseVisibility(fromCell: pathToTravel[index: i], range: VisionRange);

                for (; t < 1f; t += Time.deltaTime * travelSpeed)
                {
                    transform.localPosition = Bezier.GetPoint(a: a, b: b, c: c, t: t);
                    var d = Bezier.GetDerivative(a: a, b: b, c: c, t: t);
                    d.y = 0f;
                    transform.localRotation = Quaternion.LookRotation(forward: d);
                    yield return null;
                }

                Grid.DecreaseVisibility(fromCell: pathToTravel[index: i], range: VisionRange);
                t -= 1f;
            }

            currentTravelLocation = null;

            a = c;
            b = location.Position;
            c = b;
            Grid.IncreaseVisibility(fromCell: location, range: VisionRange);
            for (; t < 1f; t += Time.deltaTime * travelSpeed)
            {
                transform.localPosition = Bezier.GetPoint(a: a, b: b, c: c, t: t);
                var d = Bezier.GetDerivative(a: a, b: b, c: c, t: t);
                d.y = 0f;
                transform.localRotation = Quaternion.LookRotation(forward: d);
                yield return null;
            }

            transform.localPosition = location.Position;
            orientation = transform.localRotation.eulerAngles.y;
            ListPool<HexCell>.Add(list: pathToTravel);
            pathToTravel = null;
        }

        private IEnumerator LookAt(Vector3 point)
        {
            if (HexMetrics.Wrapping)
            {
                var xDistance = point.x - transform.localPosition.x;
                if (xDistance < -HexMetrics.innerRadius * HexMetrics.wrapSize)
                {
                    point.x += HexMetrics.innerDiameter * HexMetrics.wrapSize;
                }
                else if (xDistance > HexMetrics.innerRadius * HexMetrics.wrapSize)
                {
                    point.x -= HexMetrics.innerDiameter * HexMetrics.wrapSize;
                }
            }

            point.y = transform.localPosition.y;
            var fromRotation = transform.localRotation;
            var toRotation =
                Quaternion.LookRotation(forward: point - transform.localPosition);
            var angle = Quaternion.Angle(a: fromRotation, b: toRotation);

            if (angle > 0f)
            {
                var speed = rotationSpeed / angle;
                for (
                    var t = Time.deltaTime * speed;
                    t < 1f;
                    t += Time.deltaTime * speed
                )
                {
                    transform.localRotation =
                        Quaternion.Slerp(a: fromRotation, b: toRotation, t: t);
                    yield return null;
                }
            }

            transform.LookAt(worldPosition: point);
            orientation = transform.localRotation.eulerAngles.y;
        }

        public int GetMoveCost(
            HexCell fromCell, HexCell toCell, HexDirection direction)
        {
            if (!IsValidDestination(cell: toCell))
            {
                return -1;
            }

            var edgeType = fromCell.GetEdgeType(otherCell: toCell);
            if (edgeType == HexEdgeType.Cliff)
            {
                return -1;
            }

            int moveCost;
            if (fromCell.HasRoadThroughEdge(direction: direction))
            {
                moveCost = 1;
            }
            else if (fromCell.Walled != toCell.Walled)
            {
                return -1;
            }
            else
            {
                moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
                moveCost +=
                    toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
            }

            return moveCost;
        }

        public void Die()
        {
            if (location)
            {
                Grid.DecreaseVisibility(fromCell: location, range: VisionRange);
            }

            location.Unit = null;
            Destroy(obj: gameObject);
        }

        public void Save(BinaryWriter writer)
        {
            location.coordinates.Save(writer: writer);
            writer.Write(value: orientation);
        }

        public static void Load(BinaryReader reader, HexGrid grid)
        {
            var coordinates = HexCoordinates.Load(reader: reader);
            var orientation = reader.ReadSingle();
            grid.AddUnit(
                unit: Instantiate(original: unitPrefab), location: grid.GetCell(coordinates: coordinates),
                orientation: orientation
            );
        }

        private void OnEnable()
        {
            if (location)
            {
                transform.localPosition = location.Position;
                if (currentTravelLocation)
                {
                    Grid.IncreaseVisibility(fromCell: location, range: VisionRange);
                    Grid.DecreaseVisibility(fromCell: currentTravelLocation, range: VisionRange);
                    currentTravelLocation = null;
                }
            }
        }

//	void OnDrawGizmos () {
//		if (pathToTravel == null || pathToTravel.Count == 0) {
//			return;
//		}
//
//		Vector3 a, b, c = pathToTravel[0].Position;
//
//		for (int i = 1; i < pathToTravel.Count; i++) {
//			a = c;
//			b = pathToTravel[i - 1].Position;
//			c = (b + pathToTravel[i].Position) * 0.5f;
//			for (float t = 0f; t < 1f; t += 0.1f) {
//				Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
//			}
//		}
//
//		a = c;
//		b = pathToTravel[pathToTravel.Count - 1].Position;
//		c = b;
//		for (float t = 0f; t < 1f; t += 0.1f) {
//			Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
//		}
//	}
    }
}