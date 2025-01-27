using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Zorrendor.PointCloud
{
    public class PLYFileWritter
    {
        private const int BufferSize = 65536;

        private const string HeaderTop = "ply\nformat binary_little_endian 1.0\ncomment author: Point Cloud\ncomment object: another cube\nelement vertex ";
        private const string HeaderBottom = "\nproperty float x\nproperty float y\nproperty float z\nproperty uchar red\nproperty uchar green\nproperty uchar blue\nend_header\n";

        public void Write(PointCloud pointCloud, string filename)
        {
            const int vertexByteSize = sizeof(float) * 3 + sizeof(byte) * 3;
            ByteConverter offsets = new ByteConverter();
            offsets.Bytes = new byte[vertexByteSize];

            using (FileStream fs = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (BufferedStream bs = new BufferedStream(fs, BufferSize))
            using (BinaryWriter bw = new BinaryWriter(bs))
            {
                bw.Write((HeaderTop + pointCloud.vertexCount.ToString() + HeaderBottom).ToCharArray());
                for (int i = 0; i < pointCloud.vertexCount; i++)
                {
                    PointCloud.Vertex ver = pointCloud.vertices[i];
                    offsets.Floats[0] = ver.position.x;
                    offsets.Floats[1] = ver.position.z;
                    offsets.Floats[2] = ver.position.y;
                    offsets.Bytes[12] = ver.color.r;
                    offsets.Bytes[13] = ver.color.g;
                    offsets.Bytes[14] = ver.color.b;
                    bw.Write(offsets.Bytes, 0, vertexByteSize);
                }
            
                // unsafe
                // {
                //     void* vertexPtr = NativeArrayUnsafeUtility.GetUnsafePtr(pointCloud.vertices);
                //     byte* bytePtr = (byte*)vertexPtr;
                //     bw.Write(bytePtr, pointCloud.vertexCount * vertexByteSize);
                // }
            }
            Debug.Log("Save to file " + filename);
        }
    }

}

