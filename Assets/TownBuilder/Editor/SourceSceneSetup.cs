using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TownStudy.Builder;
public static class SourceSceneSetup {
 public static void Apply(){
  EditorSceneManager.OpenScene("Assets/Scenes/SideViewBuilder.unity");
  Directory.CreateDirectory("Assets/TownBuilder/Resources/SourceMeshes");AssetDatabase.Refresh();
  AssetDatabase.StartAssetEditing();try{
   foreach(int id in RecoveredCatalog.Instance.candidates.Select(c=>c.id).Concat(RecoveredCatalog.Instance.decorations.Where(c=>c.id>=0).Select(c=>20000+c.id))){
    string path="Assets/TownBuilder/Resources/SourceMeshes/"+id+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);var mesh=RecoveredCatalog.Instance.MeshFor(id,true);
    if(existing==null)AssetDatabase.CreateAsset(mesh,path);else{
     // CopySerialized updates CPU data but can retain the old GPU vertex layout.
     existing.Clear(false);existing.indexFormat=mesh.indexFormat;existing.vertices=mesh.vertices;existing.normals=mesh.normals;existing.uv=mesh.uv;existing.uv2=mesh.uv2;existing.subMeshCount=mesh.subMeshCount;
     for(int sub=0;sub<mesh.subMeshCount;sub++)existing.SetTriangles(mesh.GetTriangles(sub),sub);
     existing.bounds=mesh.bounds;existing.UploadMeshData(false);EditorUtility.SetDirty(existing);UnityEngine.Object.DestroyImmediate(mesh);
    }
   }
  }finally{AssetDatabase.StopAssetEditing();}
  var b=UnityEngine.Object.FindFirstObjectByType<BuilderController>();
  foreach(string name in new[]{"HouseData","TypeData","PaletteData"}){
   string path="Assets/TownBuilder/Art/Capture/"+name+".png";var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   importer.sRGBTexture=false;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Repeat;importer.alphaSource=TextureImporterAlphaSource.FromInput;importer.alphaIsTransparency=false;importer.SaveAndReimport();
  }
  b.sourceHouse=Captured("SourceHouse",false,0);b.sourceFence=Captured("SourceFence",false,15);b.sourceProps=Captured("SourceProps",true,0);
  b.floorMaterial=Mat("Floor","HDRP/Lit",new Color(.36f,.42f,.46f));b.floorMaterial.SetFloat("_Smoothness",.1f);
  b.propMeshes=AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/Art/Meshes"}).Select(g=>AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
  b.Initialize();HdrpSetup.ConfigureScene();
  var old=GameObject.Find("Aniimo Gradient Backdrop");if(old!=null)UnityEngine.Object.DestroyImmediate(old);
  var backdrop=GameObject.CreatePrimitive(PrimitiveType.Quad);backdrop.name="Aniimo Gradient Backdrop";UnityEngine.Object.DestroyImmediate(backdrop.GetComponent<Collider>());
  backdrop.AddComponent<WorkshopBackdrop>().view=b.view;backdrop.GetComponent<Renderer>().sharedMaterial=Mat("AniimoBackdrop","TownStudy/AniimoBackdrop",Color.white);
  EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());AssetDatabase.SaveAssets();
  Debug.Log("SOURCE_SCENE_READY");
 }
 static Material Captured(string name,bool raw,int palette){
  var m=Mat(name,"TownStudy/CapturedHouse",Color.white);
  m.SetTexture("_MainTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TownBuilder/Art/Capture/HouseData.png"));
  m.SetTexture("_TypeTex",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TownBuilder/Art/Capture/TypeData.png"));
  m.SetTexture("_Palette",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TownBuilder/Art/Capture/PaletteData.png"));
  m.SetFloat("_Raw",raw?1:0);m.SetFloat("_PaletteIndex",palette);return m;
 }
 static Material Mat(string name,string shader,Color color){string p="Assets/TownBuilder/Generated/"+name+".mat";var m=AssetDatabase.LoadAssetAtPath<Material>(p);if(m==null){m=new Material(Shader.Find(shader));AssetDatabase.CreateAsset(m,p);}m.shader=Shader.Find(shader);if(m.HasProperty("_BaseColor"))m.SetColor("_BaseColor",color);EditorUtility.SetDirty(m);return m;}
}
