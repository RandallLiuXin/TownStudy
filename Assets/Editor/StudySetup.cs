using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static class StudySetup
{
    const string Source = "E:/others/Townscaper-Recovered/OriginalExport/ExportedProject/Assets";
    static string Match(string text, string pattern) => Regex.Match(text, pattern).Groups[1].Value;
    static string GuidOf(string path) => Match(File.ReadAllText(path + ".meta"), @"guid: (\w+)");
    static float Number(string text, string key, float fallback)
    {
        var value = Match(text, Regex.Escape(key) + @": ([-+\d.eE]+)");
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f) ? f : fallback;
    }

    public static void Build()
    {
        AssetDatabase.Refresh();
        var textures = Directory.GetFiles(Source, "*.png", SearchOption.AllDirectories)
            .ToDictionary(GuidOf, p => "Assets/Art/Textures/" + Path.GetFileName(p));
        var materials = new Dictionary<string, Material>();
        foreach (string path in Directory.GetFiles(Source, "*.mat", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(path);
            var material = new Material(Shader.Find("Standard")) { name = Path.GetFileNameWithoutExtension(path) };
            string color = Match(text, @"_Color: \{([^}]+)\}");
            material.color = new Color(Number(color, "r", 1), Number(color, "g", 1), Number(color, "b", 1), 1);
            material.SetFloat("_Glossiness", 0);
            material.SetFloat("_Metallic", 0);
            string textureGuid = Match(text, @"_MainTex:\s*m_Texture: \{fileID: [^,]+, guid: (\w+)");
            if (textures.TryGetValue(textureGuid, out string texturePath))
                material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            AssetDatabase.CreateAsset(material, "Assets/Art/Materials/" + material.name + ".mat");
            materials.Add(GuidOf(path), material);
        }

        // Recover mesh-to-material slots only from explicit prefab renderer references.
        var meshMaterials = new Dictionary<string, Material[]>();
        foreach (string path in Directory.GetFiles(Source, "*.prefab", SearchOption.AllDirectories))
        {
            string[] blocks = Regex.Split(File.ReadAllText(path), @"(?m)^--- ");
            var renderers = new Dictionary<string, Material[]>();
            foreach (string block in blocks.Where(b => b.StartsWith("!u!23 ")))
            {
                string go = Match(block, @"m_GameObject: \{fileID: (-?\d+)");
                string section = Match(block, @"(?s)m_Materials:\s*(.*?)\n  m_StaticBatchInfo");
                var slots = Regex.Matches(section, @"guid: (\w+)").Cast<Match>()
                    .Select(m => materials.TryGetValue(m.Groups[1].Value, out var mat) ? mat : null).ToArray();
                renderers[go] = slots;
            }
            foreach (string block in blocks.Where(b => b.StartsWith("!u!33 ")))
            {
                string go = Match(block, @"m_GameObject: \{fileID: (-?\d+)");
                string mesh = Match(block, @"m_Mesh: \{fileID: [^,]+, guid: (\w+)");
                if (renderers.TryGetValue(go, out var slots) && slots.Length > 0 && !meshMaterials.ContainsKey(mesh)) meshMaterials.Add(mesh, slots);
            }
        }

        var fallback = new Material(Shader.Find("Standard")) { name = "StudyNeutral", color = new Color(0.7f, 0.76f, 0.78f) };
        fallback.SetFloat("_Glossiness", 0);
        AssetDatabase.CreateAsset(fallback, "Assets/Art/Materials/StudyNeutral.mat");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Recovered Mesh Gallery");
        var report = new List<string> { "Study preview only: Standard materials replace original rendering algorithms." };
        int index = 0;
        foreach (string path in Directory.GetFiles(Source + "/Mesh", "*.asset").OrderBy(p => p, StringComparer.Ordinal))
        {
            string name = Path.GetFileNameWithoutExtension(path);
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Art/Meshes/" + name + ".asset");
            if (!mesh || mesh.vertexCount == 0) throw new Exception("Invalid mesh: " + name);
            var item = new GameObject(name);
            item.transform.SetParent(root.transform);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            bool mapped = meshMaterials.TryGetValue(GuidOf(path), out var slots);
            var renderer = item.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = Enumerable.Range(0, mesh.subMeshCount)
                .Select(i => mapped && i < slots.Length && slots[i] ? slots[i] : fallback).ToArray();
            float size = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z);
            float scale = size > 0.0001f ? 1.4f / size : 1;
            item.transform.localScale = Vector3.one * scale;
            Vector3 cell = new Vector3((index % 12) * 2.4f - 13.2f, 0, (index / 12) * 2.4f - 10.8f);
            item.transform.position = cell - mesh.bounds.center * scale + Vector3.up * mesh.bounds.extents.y * scale;
            var label = new GameObject(name + " Label");
            label.transform.SetParent(root.transform);
            label.transform.position = cell + new Vector3(0, 0.02f, -0.9f);
            label.transform.rotation = Quaternion.Euler(90, 0, 0);
            var text = label.AddComponent<TextMesh>();
            text.text = name; text.anchor = TextAnchor.MiddleCenter; text.characterSize = 0.07f; text.fontSize = 32;
            text.color = Color.white;
            report.Add(name + " | vertices=" + mesh.vertexCount + " | source material mapping=" + mapped);
            index++;
        }
        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.transform.position = new Vector3(0, 29, -24);
        camera.transform.LookAt(Vector3.zero);
        camera.orthographic = true; camera.orthographicSize = 18;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.1f, 0.14f, 0.18f);
        camera.gameObject.AddComponent<AudioListener>();
        var light = new GameObject("Sun").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(45, -30, 0);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
        Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/AssetGallery.unity");
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/AssetGallery.unity", true) };
        AssetDatabase.SaveAssets();
        report.Add("Meshes=" + index + "; original materials=" + materials.Count + "; textures=" + textures.Count);
        Directory.CreateDirectory("Reports");
        File.WriteAllLines("Reports/import-inventory.txt", report);
        var target = new RenderTexture(1600, 1000, 24);
        camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0); image.Apply();
        File.WriteAllBytes("Reports/gallery.png", image.EncodeToPNG());
        camera.targetTexture = null; RenderTexture.active = null;
        UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target);
        Debug.Log("STUDY_SETUP_OK " + index + " meshes");
    }
}
