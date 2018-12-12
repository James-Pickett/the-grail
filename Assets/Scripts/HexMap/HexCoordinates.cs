using System;
using System.IO;
using UnityEngine;

namespace HexMap
{
    [Serializable]
    public struct HexCoordinates
    {
        [SerializeField]
        private int x, z;

        public int X
        {
            get { return x; }
        }

        public int Z
        {
            get { return z; }
        }

        public int Y
        {
            get { return -X - Z; }
        }

        public HexCoordinates(int x, int z)
        {
            if (HexMetrics.Wrapping)
            {
                var oX = x + z / 2;
                if (oX < 0)
                {
                    x += HexMetrics.wrapSize;
                }
                else if (oX >= HexMetrics.wrapSize)
                {
                    x -= HexMetrics.wrapSize;
                }
            }

            this.x = x;
            this.z = z;
        }

        public int DistanceTo(HexCoordinates other)
        {
            var xy =
                (x < other.x ? other.x - x : x - other.x) +
                (Y < other.Y ? other.Y - Y : Y - other.Y);

            if (HexMetrics.Wrapping)
            {
                other.x += HexMetrics.wrapSize;
                var xyWrapped =
                    (x < other.x ? other.x - x : x - other.x) +
                    (Y < other.Y ? other.Y - Y : Y - other.Y);
                if (xyWrapped < xy)
                {
                    xy = xyWrapped;
                }
                else
                {
                    other.x -= 2 * HexMetrics.wrapSize;
                    xyWrapped =
                        (x < other.x ? other.x - x : x - other.x) +
                        (Y < other.Y ? other.Y - Y : Y - other.Y);
                    if (xyWrapped < xy)
                    {
                        xy = xyWrapped;
                    }
                }
            }

            return (xy + (z < other.z ? other.z - z : z - other.z)) / 2;
        }

        public static HexCoordinates FromOffsetCoordinates(int x, int z)
        {
            return new HexCoordinates(x: x - z / 2, z: z);
        }

        public static HexCoordinates FromPosition(Vector3 position)
        {
            var x = position.x / HexMetrics.innerDiameter;
            var y = -x;

            var offset = position.z / (HexMetrics.outerRadius * 3f);
            x -= offset;
            y -= offset;

            var iX = Mathf.RoundToInt(f: x);
            var iY = Mathf.RoundToInt(f: y);
            var iZ = Mathf.RoundToInt(f: -x - y);

            if (iX + iY + iZ != 0)
            {
                var dX = Mathf.Abs(f: x - iX);
                var dY = Mathf.Abs(f: y - iY);
                var dZ = Mathf.Abs(f: -x - y - iZ);

                if (dX > dY && dX > dZ)
                {
                    iX = -iY - iZ;
                }
                else if (dZ > dY)
                {
                    iZ = -iX - iY;
                }
            }

            return new HexCoordinates(x: iX, z: iZ);
        }

        public override string ToString()
        {
            return "(" +
                   X + ", " + Y + ", " + Z + ")";
        }

        public string ToStringOnSeparateLines()
        {
            return X + "\n" + Y + "\n" + Z;
        }

        public void Save(BinaryWriter writer)
        {
            writer.Write(value: x);
            writer.Write(value: z);
        }

        public static HexCoordinates Load(BinaryReader reader)
        {
            HexCoordinates c;
            c.x = reader.ReadInt32();
            c.z = reader.ReadInt32();
            return c;
        }
    }
}