using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public static class HdrpSetup
{
    const string Root="Assets/TownBuilder/Generated";
    public static void Apply()
    {
        var asset=AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(Root+"/TownHDRP.asset");
        if(asset==null){asset=ScriptableObject.CreateInstance<HDRenderPipelineAsset>();AssetDatabase.CreateAsset(asset,Root+"/TownHDRP.asset");}
        PlayerSettings.colorSpace=ColorSpace.Linear;
        GraphicsSettings.defaultRenderPipeline=asset;
        int level=QualitySettings.GetQualityLevel();
        for(int i=0;i<QualitySettings.names.Length;i++){QualitySettings.SetQualityLevel(i,false);QualitySettings.renderPipeline=asset;}
        QualitySettings.SetQualityLevel(level,false);
        typeof(HDRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings").GetMethod("Ensure",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{true});
        foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Art","Assets/TownBuilder"}))
        {
            var m=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if(m.shader.name!="Standard")continue;
            var color=m.color;var texture=m.mainTexture;
            m.shader=Shader.Find("HDRP/Lit");m.SetColor("_BaseColor",color);m.SetTexture("_BaseColorMap",texture);m.SetFloat("_Smoothness",.2f);m.SetFloat("_Metallic",0);EditorUtility.SetDirty(m);
        }
        EditorSceneManager.OpenScene("Assets/Scenes/SideViewBuilder.unity");
        ConfigureScene();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
        Debug.Log("HDRP_MIGRATION_COMPLETE");
    }
    public static void ConfigureScene()
    {
        var camera=Camera.main;
        var hd=camera.GetComponent<HDAdditionalCameraData>()??camera.gameObject.AddComponent<HDAdditionalCameraData>();
        hd.clearColorMode=HDAdditionalCameraData.ClearColorMode.Color;hd.backgroundColorHDR=new Color(.23f,.25f,.34f);
        hd.antialiasing=HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        foreach(var l in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))Object.DestroyImmediate(l.gameObject);
        Light("AFK inspired warm key",Quaternion.LookRotation(-new Vector3(.219256f,.422618f,-.879387f)),new Color(1,.997f,.96f),1400,true);
        Light("Cool fill",Quaternion.Euler(15,130,0),new Color(.55f,.68f,1),450,false);
        Light("Warm rim",Quaternion.Euler(30,205,0),new Color(1,.75f,.5f),1000,false);
        var old=GameObject.Find("Workshop Volume");if(old!=null)Object.DestroyImmediate(old);
        var volume=new GameObject("Workshop Volume").AddComponent<Volume>();volume.isGlobal=true;volume.priority=10;
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Root+"/WorkshopVolume.asset");
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,Root+"/WorkshopVolume.asset");}
        volume.sharedProfile=profile;
        var exposure=Get<Exposure>(profile);exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(7.5f);
        var tone=Get<Tonemapping>(profile);tone.mode.Override(TonemappingMode.None);
        var ao=Get<ScreenSpaceAmbientOcclusion>(profile);ao.intensity.Override(1);ao.radius.Override(.25f);
        var bloom=Get<Bloom>(profile);bloom.intensity.Override(0);
        var sky=Get<GradientSky>(profile);sky.top.Override(new Color(.25f,.31f,.45f));sky.middle.Override(new Color(.53f,.58f,.65f));sky.bottom.Override(new Color(.3f,.32f,.4f));
        var environment=Get<VisualEnvironment>(profile);environment.skyType.Override((int)SkyType.Gradient);
        foreach(var component in profile.components)if(!AssetDatabase.Contains(component))AssetDatabase.AddObjectToAsset(component,profile);
        EditorUtility.SetDirty(profile);
    }
    static T Get<T>(VolumeProfile p) where T:VolumeComponent {return p.TryGet<T>(out var component)?component:p.Add<T>(true);}
    static void Light(string name,Quaternion rotation,Color color,float intensity,bool shadows)
    {
        var light=new GameObject(name).AddComponent<Light>();light.type=LightType.Directional;
        var data=light.gameObject.AddComponent<HDAdditionalLightData>();light.color=color;light.intensity=intensity;light.shadows=shadows?LightShadows.Soft:LightShadows.None;light.transform.rotation=rotation;
        data.SetShadowResolution(2048);
    }
}
