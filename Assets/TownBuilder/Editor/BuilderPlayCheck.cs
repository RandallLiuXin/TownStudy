using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using TownStudy.Builder;

[InitializeOnLoad]
public static class BuilderPlayCheck
{
    static BuilderPlayCheck()
    {
        if(SessionState.GetBool("BuilderCheck.Active",false)) EditorApplication.update+=Tick;
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/SideViewBuilder.unity");
        SessionState.SetBool("BuilderCheck.Active",true);
        SessionState.SetFloat("BuilderCheck.Deadline",(float)EditorApplication.timeSinceStartup+100);
        EditorApplication.EnterPlaymode();
    }
    static void Tick()
    {
        if(EditorApplication.isPlaying&&UnityEngine.Object.FindFirstObjectByType<BuilderController>()!=null&&UnityEngine.Object.FindFirstObjectByType<BuilderPlayDriver>()==null)
            new GameObject("Verification driver").AddComponent<BuilderPlayDriver>();
        if(EditorApplication.timeSinceStartup>SessionState.GetFloat("BuilderCheck.Deadline",0)) Finish(false,"Play check timed out");
    }
    public static void Finish(bool pass,string report)
    {
        File.WriteAllText("Reports/builder-play-check.txt",(pass?"PASS":"FAIL")+"\n"+report);
        SessionState.SetBool("BuilderCheck.Active",false);EditorApplication.Exit(pass?0:1);
    }
}
public sealed class BuilderPlayDriver : MonoBehaviour
{
    int checks,errors;
    string failure="";
    void Check(bool condition,string name) { checks++;if(!condition)failure+=name+"\n"; }
    void OnEnable(){Application.logMessageReceived+=Log;}
    void OnDisable(){Application.logMessageReceived-=Log;}
    void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception) {errors++;failure+=message+"\n";}}
    IEnumerator Start()
    {
        Application.runInBackground=true;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        yield return new WaitForSeconds(1);
        var b=UnityEngine.Object.FindFirstObjectByType<BuilderController>();
        var mouse=InputSystem.AddDevice<Mouse>();mouse.MakeCurrent();
        Vector2 point=b.view.WorldToScreenPoint(new Vector3(0,5,0));
        var lights=UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        var lightPositions=lights.Select(l=>l.transform.position).ToArray();
        var lightIntensity=lights.Select(l=>l.intensity).ToArray();
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return new WaitForSeconds(.2f);
        var cursor=GameObject.Find("Placement cursor").GetComponent<Canvas>();
        Check(cursor.enabled&&cursor.renderMode==RenderMode.ScreenSpaceOverlay,"Cursor uses screen overlay");
        Check(cursor.GetComponentsInChildren<UnityEngine.UI.Image>().Length==4&&cursor.GetComponentsInChildren<UnityEngine.UI.Image>().All(i=>!i.raycastTarget),"Cursor has four nonblocking edges");
        var edge=cursor.GetComponentsInChildren<UnityEngine.UI.Image>()[0].rectTransform;
        var previous=edge.anchoredPosition;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=b.view.WorldToScreenPoint(new Vector3(1,4,0))});yield return new WaitForSeconds(.2f);
        Check(edge.anchoredPosition!=previous,"Cursor follows mouse");
        Check(lights.Length==UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Length&&lights.Select((l,i)=>l.transform.position==lightPositions[i]&&l.intensity==lightIntensity[i]).All(v=>v),"Mouse leaves scene lights unchanged");
        Check(UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Length==0,"No emissive world cursor");
        Check(b.GetComponentsInChildren<Transform>().Any(t=>t.name=="Floor")&&b.GetComponentsInChildren<Transform>().Any(t=>t.name=="Chassis"),"Floor and chassis present");
        Check(!b.GetComponentsInChildren<Transform>().Any(t=>t.name.StartsWith("Wheel")||t.name.StartsWith("Hub")),"Wheels and hubs removed");
        Check(!b.PointerBlocked(point),"Build point unblocked");
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Left));
        yield return new WaitForSeconds(.2f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return new WaitForSeconds(.1f);
        Check(b.Board.Cells.Contains(new Vector2Int(0,5)),"Real left input builds");
        Check(b.Board.Tiles.Keys.Any(p=>p.y==0)&&b.GetComponentsInChildren<MeshFilter>().Any(m=>m.sharedMesh!=null&&m.sharedMesh.vertexCount>0),"Original support domain and geometry generated");
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point}.WithButton(MouseButton.Right));yield return new WaitForSeconds(.2f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=point});yield return new WaitForSeconds(.1f);
        Check(!b.Board.Cells.Contains(new Vector2Int(0,5)),"Real right input erases");
        b.Action("Undo");Check(b.Board.Cells.Contains(new Vector2Int(0,5)),"Undo restores floating building");b.Action("Redo");
        Check(!b.Board.Cells.Contains(new Vector2Int(0,5)),"Redo removes");
        var button=UnityEngine.Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None).First(x=>x.name=="Erase");
        Vector2 ui=RectTransformUtility.WorldToScreenPoint(null,button.transform.position+new Vector3(30,-15,0));
        Check(b.PointerBlocked(ui),"Toolbar raycast blocks board");
        int count=b.Board.Cells.Count;
        InputSystem.QueueStateEvent(mouse,new MouseState{position=ui}.WithButton(MouseButton.Left));yield return new WaitForSeconds(.2f);
        InputSystem.QueueStateEvent(mouse,new MouseState{position=ui});yield return new WaitForSeconds(.2f);
        Check(b.eraseMode,"Real UI input switches mode");Check(b.Board.Cells.Count==count,"UI click preserves cells");
        b.Action("Build");b.Action("Example");b.Action("Remix");
        yield return null;
        Check(b.transform.Cast<Transform>().Count(t=>t.name=="Generated")==1,"Single generated tree after same-frame actions");
        BuilderSetup.Verify();
        Check(errors==0,"No runtime errors");
        BuilderPlayCheck.Finish(failure.Length==0,"Checks: "+checks+"\nRuntime errors: "+errors+"\n"+failure);
    }
}
