using UnityEngine;

namespace HexMap
{
    public struct EdgeVertices
    {
        public Vector3 v1, v2, v3, v4, v5;

        public EdgeVertices(Vector3 corner1, Vector3 corner2)
        {
            v1 = corner1;
            v2 = Vector3.Lerp(a: corner1, b: corner2, t: 0.25f);
            v3 = Vector3.Lerp(a: corner1, b: corner2, t: 0.5f);
            v4 = Vector3.Lerp(a: corner1, b: corner2, t: 0.75f);
            v5 = corner2;
        }

        public EdgeVertices(Vector3 corner1, Vector3 corner2, float outerStep)
        {
            v1 = corner1;
            v2 = Vector3.Lerp(a: corner1, b: corner2, t: outerStep);
            v3 = Vector3.Lerp(a: corner1, b: corner2, t: 0.5f);
            v4 = Vector3.Lerp(a: corner1, b: corner2, t: 1f - outerStep);
            v5 = corner2;
        }

        public static EdgeVertices TerraceLerp(
            EdgeVertices a, EdgeVertices b, int step)
        {
            EdgeVertices result;
            result.v1 = HexMetrics.TerraceLerp(a: a.v1, b: b.v1, step: step);
            result.v2 = HexMetrics.TerraceLerp(a: a.v2, b: b.v2, step: step);
            result.v3 = HexMetrics.TerraceLerp(a: a.v3, b: b.v3, step: step);
            result.v4 = HexMetrics.TerraceLerp(a: a.v4, b: b.v4, step: step);
            result.v5 = HexMetrics.TerraceLerp(a: a.v5, b: b.v5, step: step);
            return result;
        }
    }
}