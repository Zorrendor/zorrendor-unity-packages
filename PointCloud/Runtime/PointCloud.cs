using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Zorrendor.PointCloud
{
    public class PointCloud
    {
        public struct Vertex
        {
            public Vector3 position;
            public Color32 color;
        }

        public static readonly VertexAttributeDescriptor[] vertexDescriptor = new VertexAttributeDescriptor[]
        {
            new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3, 0),
            new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.UNorm8, 4, 0)
        };

        public int vertexCount;
        public NativeArray<Vertex> vertices;

        public int indexCount;
        public NativeArray<uint> indices;
    }
}
