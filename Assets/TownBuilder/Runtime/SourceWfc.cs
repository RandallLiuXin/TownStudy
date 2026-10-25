using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace TownStudy.Builder {
// Original corner domains and oriented face profiles, with a chassis boundary at y=-1.
// Empty-corner modules are essential: they carry arches and supports through empty space.
public sealed class SourceWfc {
 public static readonly Vector3Int[] Directions={new Vector3Int(0,0,-1),Vector3Int.right,new Vector3Int(0,0,1),Vector3Int.left,Vector3Int.down,Vector3Int.up};
 public static readonly int[] Opposite={2,3,0,1,5,4};
 public int Propagations,Decisions,Backtracks;
 Vector3Int[] positions;int[,] neighbors;Dictionary<Vector3Int,int> prior;int seed,budget;
 System.Diagnostics.Stopwatch clock;
 Dictionary<int,OriginalCandidate> active;
 readonly RecoveredCatalog catalog=RecoveredCatalog.Instance;
 public static int CaseKey(ISet<Vector2Int> cells,Vector3Int s) {
  int mask=0;if(cells.Contains(new Vector2Int(s.x,s.y)))mask|=1;if(cells.Contains(new Vector2Int(s.x+1,s.y)))mask|=2;if(cells.Contains(new Vector2Int(s.x,s.y+1)))mask|=4;if(cells.Contains(new Vector2Int(s.x+1,s.y+1)))mask|=8;return(s.z==0?16:0)+mask;
 }
 public bool Compatible(int a,int b,int d) { var x=catalog.ById[a];var y=catalog.ById[b];int o=Opposite[d];return x.side0[d]==y.side1[o]&&x.side1[d]==y.side0[o]; }
 public bool Solve(HashSet<Vector2Int> cells,Dictionary<Vector3Int,int> previous,int randomSeed,out Dictionary<Vector3Int,int> result) {
  active=catalog.ById;result=null;prior=previous;seed=randomSeed;budget=1000;clock=System.Diagnostics.Stopwatch.StartNew();Propagations=Decisions=Backtracks=0;
  if(cells.Count==0){result=new Dictionary<Vector3Int,int>();return true;}
  var slots=new HashSet<Vector3Int>();
  for(int x=cells.Min(p=>p.x)-1;x<=cells.Max(p=>p.x);x++)for(int y=-1;y<=cells.Max(p=>p.y);y++)for(int z=-1;z<=0;z++)slots.Add(new Vector3Int(x,y,z));
  positions=slots.OrderBy(p=>p.y).ThenBy(p=>p.x).ThenBy(p=>p.z).ToArray();var index=positions.Select((p,i)=>(p,i)).ToDictionary(t=>t.p,t=>t.i);
  neighbors=new int[positions.Length,6];var domains=new int[positions.Length][];
  for(int i=0;i<positions.Length;i++){
   for(int d=0;d<6;d++)neighbors[i,d]=index.TryGetValue(positions[i]+Directions[d],out int n)?n:-1;
   domains[i]=catalog.Cases[CaseKey(cells,positions[i])].Where(c=>{
    var candidate=catalog.ById[c];
    for(int d=0;d<6;d++)if(neighbors[i,d]<0&&d!=4&&(candidate.side0[d]!=0||candidate.side1[d]!=0))return false;
    return true;
   }).ToArray();
   if(domains[i].Length==0)return false;
  }
  if(!Search(domains,out var solution))return false;
  result=new Dictionary<Vector3Int,int>();for(int i=0;i<positions.Length;i++)result.Add(positions[i],solution[i][0]);return true;
 }
 // Original decor: zero means unconstrained, nonzero faces require neighboring support.
 // Multiple attachments can coexist unless the original overlap table forbids their pair.
 public bool DecorOverlap(int first,int second) {
  int a=(first/8)*263+((first%8)|16)*3329,b=(second/8)*263+((second%8)|16)*3329;
  int key=unchecked(Math.Max(a,b)*6217+Math.Min(a,b)*6221);
  return Array.BinarySearch(catalog.decorOverlaps,key)>=0;
 }
 public Dictionary<Vector3Int,List<int>> SelectDecor(Dictionary<Vector3Int,int> structure,int randomSeed) {
  var domains=structure.ToDictionary(p=>p.Key,p=>catalog.ById[p.Value].decor.ToList());
  Action propagate=()=>{bool changed;do{changed=false;foreach(var p in domains)for(int i=p.Value.Count-1;i>=0;i--){var c=catalog.DecorById[p.Value[i]];for(int d=0;d<6;d++)if(c.side0[d]!=0&&(!domains.TryGetValue(p.Key+Directions[d],out var neighbor)||!neighbor.Any(n=>catalog.DecorById[n].side1[Opposite[d]]==c.side0[d]))){p.Value.RemoveAt(i);changed=true;break;}}}while(changed);};
  propagate();
  var choices=domains.SelectMany(p=>p.Value.Select(id=>(slot:p.Key,id))).OrderBy(p=>catalog.DecorById[p.id].cost).ThenBy(p=>unchecked(p.id*15485863^p.slot.x*73856093^p.slot.y*19349663^randomSeed)).ToArray();
  foreach(var choice in choices){var domain=domains[choice.slot];if(!domain.Contains(choice.id))continue;int removed=domain.RemoveAll(other=>other!=choice.id&&DecorOverlap(choice.id,other));if(removed>0)propagate();}
  return domains;
 }
 uint Hash(int i,int c){unchecked{var p=positions[i];uint h=(uint)(p.x*73856093^p.y*19349663^p.z*1640531513^seed*83492791^c*15485863);h^=h>>16;h*=0x7feb352d;return h^(h>>15);}}
 bool Search(int[][] domains,out int[][] solution,int changed=-1){
  solution=null;if(--budget<0||clock.ElapsedMilliseconds>1500||!Propagate(domains,changed))return false;int pick=-1;double best=double.MaxValue;
  for(int i=0;i<domains.Length;i++){int count=domains[i].Length;if(count<2)continue;double score=Math.Log(count)+(Hash(i,0)%1000)*.0000001;if(score<best){best=score;pick=i;}}
  if(pick<0){solution=domains;return true;}Decisions++;
  int preferred=prior!=null&&prior.TryGetValue(positions[pick],out int old)?old:-1;
  var order=domains[pick].OrderBy(c=>c==preferred?-100000:active[c].cost+(Hash(pick,c)%1000)*.025);
  foreach(int c in order){if(clock.ElapsedMilliseconds>1500)return false;var next=(int[][])domains.Clone();next[pick]=new[]{c};if(Search(next,out solution,pick))return true;Backtracks++;}return false;
 }
 bool Propagate(int[][] domains,int changed){
  var q=new Queue<int>(changed<0?Enumerable.Range(0,domains.Length):new[]{changed});var queued=new bool[domains.Length];foreach(int index in q)queued[index]=true;
  while(q.Count>0){if(clock.ElapsedMilliseconds>1500)return false;int i=q.Dequeue();queued[i]=false;for(int d=0;d<6;d++){
   int n=neighbors[i,d];if(n<0)continue;
   // Pair signatures preserve both directional halves of each source face profile.
   var signatures=new HashSet<ulong>();foreach(int a in domains[i]){var ca=active[a];signatures.Add(((ulong)ca.side0[d]<<32)|(uint)ca.side1[d]);}
   int opposite=Opposite[d];
   var allowed=domains[n].Where(b=>{var cb=active[b];return signatures.Contains(((ulong)cb.side1[opposite]<<32)|(uint)cb.side0[opposite]);}).ToArray();
   if(allowed.Length==domains[n].Length)continue;Propagations++;domains[n]=allowed;if(allowed.Length==0)return false;if(!queued[n]){q.Enqueue(n);queued[n]=true;}
  }}return true;
 }
}
}
