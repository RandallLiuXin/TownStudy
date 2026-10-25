using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TownStudy.Builder;
public static class BuilderSetup {
 [MenuItem("Town Study/Rebuild Source Module Scene")]public static void Create()=>SourceSceneSetup.Apply();
 public static void Verify(){
  int checks=0,edges=0;Action<bool,string> check=(v,s)=>{checks++;if(!v)throw new Exception("VERIFY_FAIL: "+s);};
  var board=new BuildBoard();check(board.Set(new Vector2Int(0,15),true),"Floating build");string floating=board.Export();check(board.Undo()&&board.Cells.Count==0,"Undo");check(board.Redo()&&board.Export()==floating,"Redo exact");
  check(board.Tiles.Keys.Any(p=>p.y==0),"Empty corner support slots included");
  board.Demo();string saved=board.Export();board.Clear();check(board.Import(saved)&&board.Export()==saved,"Save roundtrip");check(!board.Import("{}")&&board.Export()==saved,"Invalid import atomic");check(!board.Set(new Vector2Int(14,0),true)&&board.Export()==saved,"Bounds atomic");
  var rng=new System.Random(43);var watch=System.Diagnostics.Stopwatch.StartNew();
  for(int t=0;t<20;t++){
   File.AppendAllText("Reports/source-progress.txt","Layout "+t+"\n");
   var cells=new HashSet<Vector2Int>();for(int x=-5;x<=5;x++)for(int y=0;y<8;y++)if(t==0||rng.NextDouble()<.5)cells.Add(new Vector2Int(x,y));
   check(board.Solver.Solve(cells,null,t,out var solution),"Original domain solve "+t);
   foreach(var p in solution){check(RecoveredCatalog.Instance.Cases[SourceWfc.CaseKey(cells,p.Key)].Contains(p.Value),"Original corner membership");for(int d=0;d<6;d++)if(solution.TryGetValue(p.Key+SourceWfc.Directions[d],out int other)){check(board.Solver.Compatible(p.Value,other,d),"Original face profile");edges++;}}
  }
  var editing=new BuildBoard();for(int i=0;i<30;i++){var p=new Vector2Int(rng.Next(-3,4),rng.Next(0,6));string before=editing.Export();check(editing.Set(p,!editing.Cells.Contains(p)),"Random edit");string after=editing.Export();check(editing.Undo()&&editing.Export()==before,"Random undo");check(editing.Redo()&&editing.Export()==after,"Random redo");}
  var full=new HashSet<Vector2Int>();for(int x=BuildBoard.MinX;x<=BuildBoard.MaxX;x++)for(int y=0;y<=15;y++)full.Add(new Vector2Int(x,y));
  check(board.Solver.Solve(full,null,27,out var complete),"Full 448-cell grid");
  board.Demo();var decor=board.Solver.SelectDecor(board.Tiles,27);int attachments=decor.Sum(p=>p.Value.Count);check(attachments>0,"Source decor visible");
  foreach(var slot in decor)foreach(int id in slot.Value){var item=RecoveredCatalog.Instance.DecorById[id];check(RecoveredCatalog.Instance.ById[board.Tiles[slot.Key]].decor.Contains(id),"Source decor association");foreach(int other in slot.Value)if(other!=id)check(!board.Solver.DecorOverlap(id,other),"No source decor overlap");for(int d=0;d<6;d++)if(item.side0[d]!=0)check(decor.TryGetValue(slot.Key+SourceWfc.Directions[d],out var neighbors)&&neighbors.Any(n=>RecoveredCatalog.Instance.DecorById[n].side1[SourceWfc.Opposite[d]]==item.side0[d]),"Decor interface supported");}
  var c=UnityEngine.Object.FindFirstObjectByType<BuilderController>();if(c!=null){if(c.Board==null)c.Initialize();check(!c.view.orthographic&&c.view.transform.forward==Vector3.forward,"Perspective side camera");check(c.ScreenCell(c.view.WorldToScreenPoint(new Vector3(2,3,0)),out var p)&&p==new Vector2Int(2,3),"Screen projection");check(c.sourceHouse!=null&&c.sourceHouse.shader.name=="TownStudy/CapturedHouse","Captured HDRP material");}
  check(UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline is UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset,"HDRP active");
  File.WriteAllText("Reports/source-verification.txt","PASS\nChecks="+checks+"\nOriginal socket edges="+edges+"\nRandom layouts=20\nFull grid=448 cells\nDecor attachments="+attachments+"\nRandom edit/undo/redo=30\nElapsed ms="+watch.ElapsedMilliseconds);
 }
 public static void Screenshot(BuilderController b,string path){var rt=new RenderTexture(1600,900,24);b.view.targetTexture=rt;b.view.aspect=1600f/900;b.Fit();b.view.Render();RenderTexture.active=rt;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());b.view.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(image);}
 public static void Build(){var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{"Assets/Scenes/SideViewBuilder.unity"},locationPathName="Builds/CastleWorkshop/CastleWorkshop.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed");Debug.Log("PLAYER_BUILD_SUCCESS");}
 public static void CreateAndBuild(){Create();Build();}
}


