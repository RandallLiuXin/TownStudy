import renderdoc as rd
import json,os,traceback,struct,hashlib
ROOT=r'E:\others\TownStudy\reconstruction'
SRC=r'E:\others\TownStudy\reconstruction\captures\Townscaper-frame823.rdc'
OUT=ROOT+r'\bindings-001';os.makedirs(OUT,exist_ok=True)
cap=rd.OpenCaptureFile(); replay=None; out={'draws':[],'textures':[]}
def ser(x,depth=0):
 if x is None or isinstance(x,(str,int,float,bool)):return x
 if isinstance(x,(list,tuple)) or hasattr(x,'__iter__') or (hasattr(x,'__len__') and hasattr(x,'__getitem__')):return [ser(v,depth+1) for v in x]
 if depth>4:return str(x)
 if hasattr(x,'Name'):return x.Name()
 if isinstance(x,rd.ResourceId):return str(x)
 attrs=[n for n in dir(x) if not n.startswith('_') and n not in ('this','thisown') and not callable(getattr(x,n))]
 return {n:ser(getattr(x,n),depth+1) for n in attrs} if attrs else str(x)
def savebuf(rid):
 path=OUT+'\\buffer-'+str(rid).split('::')[-1]+'.bin'
 if not os.path.exists(path):
  with open(path,'wb') as f:f.write(bytes(replay.GetBufferData(rid,0,0)))
 return path
try:
 cap.OpenFile(SRC,'rdc',None); result,replay=cap.OpenCapture(rd.ReplayOptions(),None)
 if result.code!=rd.ResultCode.Succeeded:raise RuntimeError(str(result))
 names={str(r.resourceId):r.name for r in replay.GetResources()}
 actions={}
 def walk(xs):
  for a in xs:actions[a.eventId]=a;walk(a.children)
 walk(replay.GetRootActions())
 wanted=set()
 for eid in [142,185,261,284,308,325,375,389,498]:
  replay.SetFrameEvent(eid,True);p=replay.GetPipelineState();d=replay.GetD3D11PipelineState();a=actions[eid]
  row={'event':eid,'count':a.numIndices,'indexOffset':a.indexOffset,'baseVertex':a.baseVertex,'instances':a.numInstances,'topology':str(p.GetPrimitiveTopology()),'layout':ser(p.GetVertexInputs()),'indexBuffer':ser(p.GetIBuffer()),'vertexBuffers':ser(p.GetVBuffers()),'stateNames':{'cull':str(d.rasterizer.state.cullMode),'depth':str(d.outputMerger.depthStencilState.depthFunction)},'raster':ser(d.rasterizer.state),'depthStencil':ser(d.outputMerger.depthStencilState),'blend':ser(d.outputMerger.blendState),'targets':ser(p.GetOutputTargets()),'viewport':ser(p.GetViewport(0)),'stages':{}}
  if eid==185:
   savebuf(p.GetIBuffer().resourceId)
   for v in p.GetVBuffers():
    if v.resourceId!=rd.ResourceId.Null():savebuf(v.resourceId)
  for stage in [rd.ShaderStage.Vertex,rd.ShaderStage.Hull,rd.ShaderStage.Domain,rd.ShaderStage.Geometry,rd.ShaderStage.Pixel]:
   ref=p.GetShaderReflection(stage)
   if not ref:continue
   s={'shader':str(p.GetShader(stage)),'reads':ser(p.GetReadOnlyResources(stage,True)),'samplers':ser(p.GetSamplers(stage)),'constants':[]}
   with open(OUT+'\\event-'+str(eid)+'-'+str(stage).split('.')[-1]+'.txt','w',encoding='utf-8') as f:f.write(replay.DisassembleShader(rd.ResourceId.Null(),ref,''))
   for i,cb in enumerate(ref.constantBlocks):
    b=p.GetConstantBlock(stage,i,0).descriptor
    raw=bytes(replay.GetBufferData(b.resource,b.byteOffset,b.byteSize)) if b.resource!=rd.ResourceId.Null() else b''
    s['constants'].append({'name':cb.name,'slot':cb.fixedBindNumber,'resource':str(b.resource),'offset':b.byteOffset,'size':len(raw),'float4':[list(struct.unpack_from('<4f',raw,j)) for j in range(0,len(raw)-15,16)]})
   row['stages'][str(stage)]=s
   if eid==185:
    for b in p.GetReadOnlyResources(stage,True):wanted.add(str(b.descriptor.resource))
  out['draws'].append(row)
 for t in replay.GetTextures():
  if str(t.resourceId) not in wanted:continue
  rec={'id':str(t.resourceId),'name':names.get(str(t.resourceId)),'width':t.width,'height':t.height,'format':t.format.Name()}
  if t.width<=2048 and t.height<=2048 and not t.creationFlags & rd.TextureCategory.DepthTarget:
   s=rd.TextureSave();s.resourceId=t.resourceId;s.destType=rd.FileType.PNG;s.mip=0;s.slice.sliceIndex=0
   path=OUT+'\\texture-'+str(t.resourceId).split('::')[-1]+'.png';rec['save']=str(replay.SaveTexture(s,path));rec['path']=path
  out['textures'].append(rec)
except:out['error']=traceback.format_exc()
finally:
 if replay:replay.Shutdown()
 cap.Shutdown()
 with open(OUT+r'\evidence.json','w',encoding='utf-8') as f:json.dump(out,f,indent=2,ensure_ascii=False)
os._exit(0)


