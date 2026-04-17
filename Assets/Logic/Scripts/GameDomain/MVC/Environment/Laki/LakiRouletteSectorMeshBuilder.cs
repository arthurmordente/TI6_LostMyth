using UnityEngine;

namespace Logic.Scripts.GameDomain.MVC.Environment.Laki
{
    /// <summary>Shared ring-sector mesh generation for roulette tiles (visual catalog prefabs + runtime arena).</summary>
    public static class LakiRouletteSectorMeshBuilder
    {
        /// <param name="pivotOffset">Subtracted from every vertex so local origin is the tile geometric centre.</param>
        public static Mesh BuildRingSectorMesh(float innerR, float outerR, float degStart, float degEnd, int arcSegments, Vector3 pivotOffset = default)
        {
            arcSegments = Mathf.Max(1, arcSegments);
            int vertsPerRing = arcSegments + 1;
            int vertexCount = vertsPerRing * 2;
            int triCount = arcSegments * 2;

            var verts = new Vector3[vertexCount];
            var tris = new int[triCount * 3];
            var uvs = new Vector2[vertexCount];

            float a0 = degStart * Mathf.Deg2Rad;
            float a1 = degEnd * Mathf.Deg2Rad;
            float da = (a1 - a0) / arcSegments;

            int vi = 0;
            for (int i = 0; i < vertsPerRing; i++)
            {
                float a = a0 + da * i;
                float ca = Mathf.Cos(a);
                float sa = Mathf.Sin(a);
                verts[vi + 0] = new Vector3(ca * innerR, 0f, sa * innerR) - pivotOffset;
                verts[vi + 1] = new Vector3(ca * outerR, 0f, sa * outerR) - pivotOffset;
                uvs[vi + 0] = new Vector2((float)i / arcSegments, 0f);
                uvs[vi + 1] = new Vector2((float)i / arcSegments, 1f);
                vi += 2;
            }

            int ti = 0;
            for (int i = 0; i < arcSegments; i++)
            {
                int i0 = i * 2;
                int i1 = i0 + 1;
                int i2 = i0 + 2;
                int i3 = i0 + 3;
                tris[ti++] = i0; tris[ti++] = i3; tris[ti++] = i1;
                tris[ti++] = i0; tris[ti++] = i2; tris[ti++] = i3;
            }

            var mesh = new Mesh
            {
                name = "RingSector",
                indexFormat = vertexCount > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16
            };
            mesh.SetVertices(verts);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(tris, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
            return mesh;
        }
    }
}
