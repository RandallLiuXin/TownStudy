foreach(var m in UnityEngine.Resources.FindObjectsOfTypeAll<UnityEngine.Mesh>())if(m.name.StartsWith("OriginalModule"))m.UploadMeshData(false);return "Uploaded meshes";
