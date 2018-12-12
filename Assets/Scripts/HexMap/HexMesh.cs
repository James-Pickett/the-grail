using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexMap
{
    [RequireComponent(requiredComponent: typeof(MeshFilter), requiredComponent2: typeof(MeshRenderer))]
    public class HexMesh : MonoBehaviour
    {
        [NonSerialized]
        private List<Color> cellWeights;

        private Mesh hexMesh;
        private MeshCollider meshCollider;

        [NonSerialized]
        private List<int> triangles;

        public bool useCollider, useCellData, useUVCoordinates, useUV2Coordinates;

        [NonSerialized]
        private List<Vector2> uvs, uv2s;

        [NonSerialized]
        private List<Vector3> vertices, cellIndices;

        private void Awake()
        {
            GetComponent<MeshFilter>().mesh = hexMesh = new Mesh();
            if (useCollider)
            {
                meshCollider = gameObject.AddComponent<MeshCollider>();
            }

            hexMesh.name = "Hex Mesh";
        }

        public void Clear()
        {
            hexMesh.Clear();
            vertices = ListPool<Vector3>.Get();
            if (useCellData)
            {
                cellWeights = ListPool<Color>.Get();
                cellIndices = ListPool<Vector3>.Get();
            }

            if (useUVCoordinates)
            {
                uvs = ListPool<Vector2>.Get();
            }

            if (useUV2Coordinates)
            {
                uv2s = ListPool<Vector2>.Get();
            }

            triangles = ListPool<int>.Get();
        }

        public void Apply()
        {
            hexMesh.SetVertices(inVertices: vertices);
            ListPool<Vector3>.Add(list: vertices);
            if (useCellData)
            {
                hexMesh.SetColors(inColors: cellWeights);
                ListPool<Color>.Add(list: cellWeights);
                hexMesh.SetUVs(channel: 2, uvs: cellIndices);
                ListPool<Vector3>.Add(list: cellIndices);
            }

            if (useUVCoordinates)
            {
                hexMesh.SetUVs(channel: 0, uvs: uvs);
                ListPool<Vector2>.Add(list: uvs);
            }

            if (useUV2Coordinates)
            {
                hexMesh.SetUVs(channel: 1, uvs: uv2s);
                ListPool<Vector2>.Add(list: uv2s);
            }

            hexMesh.SetTriangles(triangles: triangles, submesh: 0);
            ListPool<int>.Add(list: triangles);
            hexMesh.RecalculateNormals();
            if (useCollider)
            {
                meshCollider.sharedMesh = hexMesh;
            }
        }

        public void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var vertexIndex = vertices.Count;
            vertices.Add(item: HexMetrics.Perturb(position: v1));
            vertices.Add(item: HexMetrics.Perturb(position: v2));
            vertices.Add(item: HexMetrics.Perturb(position: v3));
            triangles.Add(item: vertexIndex);
            triangles.Add(item: vertexIndex + 1);
            triangles.Add(item: vertexIndex + 2);
        }

        public void AddTriangleUnperturbed(Vector3 v1, Vector3 v2, Vector3 v3)
        {
            var vertexIndex = vertices.Count;
            vertices.Add(item: v1);
            vertices.Add(item: v2);
            vertices.Add(item: v3);
            triangles.Add(item: vertexIndex);
            triangles.Add(item: vertexIndex + 1);
            triangles.Add(item: vertexIndex + 2);
        }

        public void AddTriangleUV(Vector2 uv1, Vector2 uv2, Vector3 uv3)
        {
            uvs.Add(item: uv1);
            uvs.Add(item: uv2);
            uvs.Add(item: uv3);
        }

        public void AddTriangleUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3)
        {
            uv2s.Add(item: uv1);
            uv2s.Add(item: uv2);
            uv2s.Add(item: uv3);
        }

        public void AddTriangleCellData(
            Vector3 indices, Color weights1, Color weights2, Color weights3
        )
        {
            cellIndices.Add(item: indices);
            cellIndices.Add(item: indices);
            cellIndices.Add(item: indices);
            cellWeights.Add(item: weights1);
            cellWeights.Add(item: weights2);
            cellWeights.Add(item: weights3);
        }

        public void AddTriangleCellData(Vector3 indices, Color weights)
        {
            AddTriangleCellData(indices: indices, weights1: weights, weights2: weights, weights3: weights);
        }

        public void AddQuad(Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
        {
            var vertexIndex = vertices.Count;
            vertices.Add(item: HexMetrics.Perturb(position: v1));
            vertices.Add(item: HexMetrics.Perturb(position: v2));
            vertices.Add(item: HexMetrics.Perturb(position: v3));
            vertices.Add(item: HexMetrics.Perturb(position: v4));
            triangles.Add(item: vertexIndex);
            triangles.Add(item: vertexIndex + 2);
            triangles.Add(item: vertexIndex + 1);
            triangles.Add(item: vertexIndex + 1);
            triangles.Add(item: vertexIndex + 2);
            triangles.Add(item: vertexIndex + 3);
        }

        public void AddQuadUnperturbed(
            Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4
        )
        {
            var vertexIndex = vertices.Count;
            vertices.Add(item: v1);
            vertices.Add(item: v2);
            vertices.Add(item: v3);
            vertices.Add(item: v4);
            triangles.Add(item: vertexIndex);
            triangles.Add(item: vertexIndex + 2);
            triangles.Add(item: vertexIndex + 1);
            triangles.Add(item: vertexIndex + 1);
            triangles.Add(item: vertexIndex + 2);
            triangles.Add(item: vertexIndex + 3);
        }

        public void AddQuadUV(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
        {
            uvs.Add(item: uv1);
            uvs.Add(item: uv2);
            uvs.Add(item: uv3);
            uvs.Add(item: uv4);
        }

        public void AddQuadUV2(Vector2 uv1, Vector2 uv2, Vector3 uv3, Vector3 uv4)
        {
            uv2s.Add(item: uv1);
            uv2s.Add(item: uv2);
            uv2s.Add(item: uv3);
            uv2s.Add(item: uv4);
        }

        public void AddQuadUV(float uMin, float uMax, float vMin, float vMax)
        {
            uvs.Add(item: new Vector2(x: uMin, y: vMin));
            uvs.Add(item: new Vector2(x: uMax, y: vMin));
            uvs.Add(item: new Vector2(x: uMin, y: vMax));
            uvs.Add(item: new Vector2(x: uMax, y: vMax));
        }

        public void AddQuadUV2(float uMin, float uMax, float vMin, float vMax)
        {
            uv2s.Add(item: new Vector2(x: uMin, y: vMin));
            uv2s.Add(item: new Vector2(x: uMax, y: vMin));
            uv2s.Add(item: new Vector2(x: uMin, y: vMax));
            uv2s.Add(item: new Vector2(x: uMax, y: vMax));
        }

        public void AddQuadCellData(
            Vector3 indices,
            Color weights1, Color weights2, Color weights3, Color weights4
        )
        {
            cellIndices.Add(item: indices);
            cellIndices.Add(item: indices);
            cellIndices.Add(item: indices);
            cellIndices.Add(item: indices);
            cellWeights.Add(item: weights1);
            cellWeights.Add(item: weights2);
            cellWeights.Add(item: weights3);
            cellWeights.Add(item: weights4);
        }

        public void AddQuadCellData(
            Vector3 indices, Color weights1, Color weights2
        )
        {
            AddQuadCellData(indices: indices, weights1: weights1, weights2: weights1, weights3: weights2,
                weights4: weights2);
        }

        public void AddQuadCellData(Vector3 indices, Color weights)
        {
            AddQuadCellData(indices: indices, weights1: weights, weights2: weights, weights3: weights,
                weights4: weights);
        }
    }
}