using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using TownStudy.Builder;
public static class SourceAudit {
 public static void Run(){
  var solver=new SourceWfc();var board=new BuildBoard();board.Demo();
  var watch=System.Diagnostics.Stopwatch.StartNew();
  if(!solver.Solve(board.Cells,null,27,out var result))throw new Exception("Source demo solve failed");
  int edges=0;foreach(var pair in result)for(int d=0;d<6;d++)if(result.TryGetValue(pair.Key+SourceWfc.Directions[d],out int other)){if(!solver.Compatible(pair.Value,other,d))throw new Exception("Original profile mismatch");edges++;}
  File.WriteAllText("Reports/source-first-solve.txt","PASS slots="+result.Count+" edges="+edges+" ms="+watch.ElapsedMilliseconds+" decisions="+solver.Decisions+" backtracks="+solver.Backtracks);
  Debug.Log("SOURCE_SOLVE_PASS");
 }
}
