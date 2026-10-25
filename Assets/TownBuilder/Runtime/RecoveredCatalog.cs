using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TownStudy.Builder {
[Serializable] public class OriginalCase { public int key; public int[] candidates; }
[Serializable] public class OriginalPiece { public int mesh,orientation; }
[Serializable] public class OriginalProp { public string mesh; public int orientation; public float[] position,rotation,scale; }
[Serializable] public class OriginalCandidate { public int id,cost,orientation; public int[] side0,side1; public OriginalPiece[] pieces; public OriginalProp[] props; public int[] decor; }
[Serializable] public class OriginalMesh { public int id,corner,materialType; public int[] outline; public string material; public float[] vertices,normals,uv; public int[] triangles; }
[Serializable] public sealed class RecoveredCatalog {
 public int[] decorOverlaps;
 public OriginalCase[] cases; public OriginalCandidate[] candidates,decorations; public OriginalMesh[] meshes;
 static RecoveredCatalog instance;
 public static RecoveredCatalog Instance { get { if(instance==null) { instance=JsonUtility.FromJson<RecoveredCatalog>(Resources.Load<TextAsset>("RecoveredModules").text); instance.ById=instance.candidates.ToDictionary(c=>c.id);instance.DecorById=instance.decorations.ToDictionary(c=>c.id);instance.Cases=instance.cases.ToDictionary(c=>c.key,c=>c.candidates);instance.meshData=instance.meshes.ToDictionary(m=>m.id); } return instance; } }
 public Dictionary<int,OriginalCandidate> ById,DecorById; public Dictionary<int,int[]> Cases;
 Dictionary<int,OriginalMesh> meshData; readonly Dictionary<int,Mesh> built=new Dictionary<int,Mesh>();
 // Each orientation verified against 690 original modules, with zero conflicting corner permutations.
 static readonly int[,] transforms={{1,0,0,1},{0,-1,1,0},{-1,0,0,-1},{0,1,-1,0},{1,0,0,-1},{0,-1,-1,0},{-1,0,0,1},{0,1,1,0}};
 public static Matrix4x4 Orientation(int i) { var m=Matrix4x4.identity;m.m00=transforms[i,0];m.m02=transforms[i,1];m.m20=transforms[i,2];m.m22=transforms[i,3];return m; }
 public Mesh MeshFor(int id,bool rebuild=false) {
  Mesh result;if(!rebuild&&built.TryGetValue(id,out result)&&result!=null)return result;
  result=rebuild?null:Resources.Load<Mesh>("SourceMeshes/"+id);if(result!=null){built[id]=result;return result;}
  var c=id>=20000?DecorById[id-20000]:ById[id];var v=new List<Vector3>();var n=new List<Vector3>();var uv=new List<Vector2>();var corners=new List<Vector2>();var ts=new[]{new List<int>(),new List<int>()};
  foreach(var p in c.pieces) {
   var s=meshData[p.mesh];var m=Orientation(c.orientation)*Orientation(p.orientation);int start=v.Count;
   int bit=0;for(int value=s.corner;value>1;value>>=1)bit++;
   var cubeCorners=new[]{new Vector3(-1,-1,-1),new Vector3(1,-1,-1),new Vector3(1,-1,1),new Vector3(-1,-1,1),new Vector3(-1,1,-1),new Vector3(1,1,-1),new Vector3(1,1,1),new Vector3(-1,1,1)};
   int actualCorner=Array.IndexOf(cubeCorners,m.MultiplyVector(cubeCorners[bit]));
   for(int i=0;i<s.vertices.Length/3;i++){v.Add(m.MultiplyPoint3x4(new Vector3(s.vertices[i*3],s.vertices[i*3+1],s.vertices[i*3+2])));n.Add(m.MultiplyVector(new Vector3(s.normals[i*3],s.normals[i*3+1],s.normals[i*3+2])).normalized);uv.Add(new Vector2(s.uv[i*2],s.uv[i*2+1]+(s.outline.Length>i?2*s.outline[i]:0)));corners.Add(new Vector2(actualCorner,s.materialType));}
   var t=ts[s.material=="Fence"?1:0];bool flip=m.determinant<0;
   for(int i=0;i<s.triangles.Length;i+=3){t.Add(start+s.triangles[i]);t.Add(start+s.triangles[i+(flip?2:1)]);t.Add(start+s.triangles[i+(flip?1:2)]);}
  }
  result=new Mesh{name="OriginalModule_"+id,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};result.SetVertices(v);result.SetNormals(n);result.SetUVs(0,uv);result.SetUVs(1,corners);result.subMeshCount=2;result.SetTriangles(ts[0],0);result.SetTriangles(ts[1],1);result.RecalculateBounds();built[id]=result;return result;
 }
}
}
