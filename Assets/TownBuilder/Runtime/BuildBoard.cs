using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TownStudy.Builder {
public sealed class BuildBoard {
 public const int MinX=-14,MaxX=13,MinY=0,MaxY=15;
 public HashSet<Vector2Int> Cells {get;private set;}=new HashSet<Vector2Int>();
 public Dictionary<Vector3Int,int> Tiles {get;private set;}=new Dictionary<Vector3Int,int>();
 public int Seed {get;private set;}=27;public SourceWfc Solver {get;}=new SourceWfc();
 readonly Stack<string> undo=new Stack<string>(),redo=new Stack<string>();
 public bool CanUndo=>undo.Count>0;public bool CanRedo=>redo.Count>0;
 public static bool InBounds(Vector2Int p)=>p.x>=MinX&&p.x<=MaxX&&p.y>=0&&p.y<=MaxY;
 [Serializable]public class Placement{public Vector3Int slot;public int candidate;}
 [Serializable]public class SaveData{public int version=2,seed;public Vector2Int[] cells;public Placement[] placements;}
 public string Export()=>JsonUtility.ToJson(new SaveData{seed=Seed,cells=Cells.OrderBy(p=>p.y).ThenBy(p=>p.x).ToArray(),placements=Tiles.OrderBy(p=>p.Key.y).ThenBy(p=>p.Key.x).ThenBy(p=>p.Key.z).Select(p=>new Placement{slot=p.Key,candidate=p.Value}).ToArray()},true);
 bool Apply(HashSet<Vector2Int> cells,int seed,Dictionary<Vector3Int,int> preferred,bool record){
  if(cells.Any(p=>!InBounds(p))||!Solver.Solve(cells,preferred,seed,out var solution))return false;
  if(record){undo.Push(Export());redo.Clear();}Cells=cells;Tiles=solution;Seed=seed;return true;
 }
 public bool Set(Vector2Int p,bool occupied){if(!InBounds(p)||Cells.Contains(p)==occupied)return false;var next=new HashSet<Vector2Int>(Cells);if(occupied)next.Add(p);else next.Remove(p);return Apply(next,Seed,Tiles,true);}
 public bool Clear()=>Cells.Count>0&&Apply(new HashSet<Vector2Int>(),Seed,null,true);
 public bool Reroll()=>Apply(new HashSet<Vector2Int>(Cells),Seed+1,null,true);
 public bool Demo(){var cells=new HashSet<Vector2Int>();for(int x=-5;x<=5;x++)for(int y=0;y<=(Math.Abs(x)>=3?4:2);y++)cells.Add(new Vector2Int(x,y));cells.Remove(new Vector2Int(0,0));cells.Remove(new Vector2Int(0,1));cells.Add(new Vector2Int(8,5));cells.Add(new Vector2Int(8,6));return Apply(cells,27,null,true);}
 public bool Import(string json,bool record=true){try{
  var d=JsonUtility.FromJson<SaveData>(json);if(d==null||(d.version!=1&&d.version!=2)||d.cells==null||d.cells.Length>448||d.cells.Any(p=>!InBounds(p)))return false;
  var cells=new HashSet<Vector2Int>(d.cells);if(cells.Count!=d.cells.Length)return false;
  Dictionary<Vector3Int,int> preferred=null;
  if(d.version==2){
   if(d.placements==null||d.placements.Length>986)return false;preferred=new Dictionary<Vector3Int,int>();
   foreach(var p in d.placements){if(p.slot.x<MinX-1||p.slot.x>MaxX||p.slot.y<-1||p.slot.y>MaxY||p.slot.z< -1||p.slot.z>0||!RecoveredCatalog.Instance.ById.ContainsKey(p.candidate)||preferred.ContainsKey(p.slot))return false;if(!RecoveredCatalog.Instance.Cases[SourceWfc.CaseKey(cells,p.slot)].Contains(p.candidate))return false;preferred.Add(p.slot,p.candidate);}
   int expected=cells.Count==0?0:(cells.Max(p=>p.x)-cells.Min(p=>p.x)+2)*(cells.Max(p=>p.y)+2)*2;
   if(preferred.Count!=expected)return false;
   if(cells.Count>0&&preferred.Keys.Any(p=>p.x<cells.Min(c=>c.x)-1||p.x>cells.Max(c=>c.x)||p.y>cells.Max(c=>c.y)))return false;
   foreach(var p in preferred)for(int dir=0;dir<6;dir++)if(!preferred.ContainsKey(p.Key+SourceWfc.Directions[dir])&&dir!=4){var c=RecoveredCatalog.Instance.ById[p.Value];if(c.side0[dir]!=0||c.side1[dir]!=0)return false;}
   foreach(var p in preferred)for(int dir=0;dir<6;dir++)if(preferred.TryGetValue(p.Key+SourceWfc.Directions[dir],out var neighbor)&&!Solver.Compatible(p.Value,neighbor,dir))return false;
  }
  return Apply(cells,d.seed,preferred,record);
 }catch(Exception){return false;}}
 public bool Undo(){if(!CanUndo)return false;string state=undo.Peek(),current=Export();if(!Import(state,false))return false;undo.Pop();redo.Push(current);return true;}
 public bool Redo(){if(!CanRedo)return false;string state=redo.Peek(),current=Export();if(!Import(state,false))return false;redo.Pop();undo.Push(current);return true;}
}
}
