using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Zorrendor.PointCloud
{
    [RequireComponent(typeof(PointCloudRenderer))]
    public class PointCloudLoader : MonoBehaviour
    {
        private const string DefaultDirectoryPath = "Assets";
        private const string FolderPathKey = "PointCloudFolderPath";

        public PointCloudRenderer PointCloudRenderer
        {
            get
            {
                if (pointCloudRenderer == null)
                {
                    pointCloudRenderer = this.GetComponent<PointCloudRenderer>();
                }
                return pointCloudRenderer;
            }
        }
        private PointCloudRenderer pointCloudRenderer = null;

        public void LoadFromFile(string path)
        {
            PLYFileReader fileReader = new PLYFileReader();
            PointCloud pointCloud = fileReader.Read(path);
            PointCloudRenderer.Init(pointCloud);
        }

        public void SaveToFile(string path)
        {
            PLYFileWritter fileWritter = new PLYFileWritter();
            fileWritter.Write(PointCloudRenderer.PointCloud, path);
        }

    #if UNITY_EDITOR
        [ContextMenu("Load from file")]
        public void Load()
        {
            string directoryPath = DefaultDirectoryPath;

            if (UnityEditor.EditorPrefs.HasKey(FolderPathKey))
            {
                directoryPath = UnityEditor.EditorPrefs.GetString(FolderPathKey);
            }

            string path = UnityEditor.EditorUtility.OpenFilePanel("Select PLY file", directoryPath, "ply");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            
            UnityEditor.EditorPrefs.SetString(FolderPathKey, Path.GetDirectoryName(path));
         
            this.LoadFromFile(path);
        }

        [ContextMenu("Save to file")]
        public void Save()
        {
            string path = UnityEditor.EditorUtility.SaveFilePanel("Save PLY file", DefaultDirectoryPath, "My PLY File" + ".ply", "ply");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            this.SaveToFile(path);
        }
        
    #endif
    }
}

