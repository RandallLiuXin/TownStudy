import renderdoc as rd
import json,os,traceback
from pathlib import Path
root=Path(r"E:/others/TownStudy/reconstruction/evidence-001");root.mkdir(exist_ok=True)
cap=rd.OpenCaptureFile();replay=None;out={"draws":[]}
try:
 cap.OpenFile(r"E:/others/TownStudy/reconstruction/captures/Townscaper-frame823.rdc","rdc",None)
 status,replay=cap.OpenCapture(rd.ReplayOptions(),None)
 if status.code!=rd.ResultCode.Succeeded:raise RuntimeError(str(status))
 names={str(x.resourceId):x.name for x in replay.GetResources()};seen=set()
 for a in replay.GetRootActions():
  if not a.flags&rd.ActionFlags.Drawcall:continue
  replay.SetFrameEvent(a.eventId,True);pipe=replay.GetPipelineState()
  row={"event":a.eventId,"indices":a.numIndices,"outputs":[str(t.resource) for t in pipe.GetOutputTargets()],"reads":[],"shaders":{}}
  for binding in pipe.GetReadOnlyResources(rd.ShaderStage.Pixel,True):
   rid=str(binding.descriptor.resource);row["reads"].append({"id":rid,"name":names.get(rid),"slot":binding.access.index})
  for stage in (rd.ShaderStage.Vertex,rd.ShaderStage.Pixel):
   shader=pipe.GetShader(stage);refl=pipe.GetShaderReflection(stage)
   row["shaders"][str(stage)]=str(shader)
   if refl and str(shader) not in seen:
    seen.add(str(shader));(root/(str(a.eventId)+"-"+str(stage)+".txt")).write_text(replay.DisassembleShader(rd.ResourceId.Null(),refl,""),encoding="utf-8")
  out["draws"].append(row)
 replay.SetFrameEvent(550,True)
 for tex in replay.GetTextures():
  if str(tex.resourceId) in ["ResourceId::492","ResourceId::497"]:
   save=rd.TextureSave();save.resourceId=tex.resourceId;save.destType=rd.FileType.PNG;save.mip=0;save.slice.sliceIndex=0
   replay.SaveTexture(save,str(root/(str(tex.resourceId).split("::")[-1]+"-final.png")))
 out["status"]="success"
except:out["error"]=traceback.format_exc()
finally:
 (root/"draws.json").write_text(json.dumps(out,indent=2),encoding="utf-8")
 if replay:replay.Shutdown()
 cap.Shutdown()
os._exit(0)
