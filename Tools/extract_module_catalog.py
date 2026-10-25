"""Extract the user's local serialized Townscaper catalog without running game code."""
import json, collections, re
from pathlib import Path
import yaml

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(r'E:\others\Townscaper-Recovered\OriginalExport\ExportedProject\Assets')
raw = (SOURCE / 'MonoBehaviour/ModuleLibrary.asset').read_text(encoding='utf-8')
# BaseLoader preserves leading zeroes in Unity's hex-encoded byte arrays.
d = yaml.load(raw[raw.index('MonoBehaviour:'):], Loader=yaml.CBaseLoader)['MonoBehaviour']
corners = [(-1,-1,-1),(1,-1,-1),(1,-1,1),(-1,-1,1),(-1,1,-1),(1,1,-1),(1,1,1),(-1,1,1)]
matrices = [(1,0,0,1),(0,-1,1,0),(-1,0,0,-1),(0,1,-1,0),(1,0,0,-1),(0,-1,-1,0),(-1,0,0,1),(0,1,1,0)]
lookup = {tuple(int(c['values'][f'v{i}']) for i in range(8)):c['orientedModules'] for c in d['cornerLookups']}
def integers(s, size=1):
    b=bytes.fromhex(s)
    return [int.from_bytes(b[i:i+size],'little') for i in range(0,len(b),size)]
cases=[]; used=set()
for layer,indices in enumerate(([3,2,7,6],[0,1,4,5])):
    for mask in range(16):
        values=[18]*8
        for bit,index in enumerate(indices):
            if mask & (1<<bit):values[index]=0
        candidates=[int(m['moduleIndex'])*8+(int(m['flags'])&7) for m in lookup[tuple(values)]]
        used.update(candidates);cases.append(dict(key=layer*16+mask,candidates=candidates))
guids={}
for meta in (SOURCE/'Material').glob('*.mat.meta'):
    for line in meta.read_text().splitlines():
        if line.startswith('guid:'):guids[line.split(':',1)[1].strip()]=meta.name[:-9]
mesh_ids=set();candidates=[]; decor_ids=set()
mesh_names={}
for meta in (SOURCE/'Mesh').glob('*.asset.meta'):
    match=re.search(r'^guid: (\w+)',meta.read_text(),re.M)
    if match:mesh_names[match[1]]=meta.name[:-11]
prefab_mesh={}
for meta in (SOURCE/'GameObject').glob('*.prefab.meta'):
    guid=re.search(r'^guid: (\w+)',meta.read_text(),re.M)
    prefab=meta.with_suffix('').read_text(encoding='utf-8')
    mesh=re.search(r'm_Mesh: \{fileID: \d+, guid: (\w+)',prefab)
    if guid and mesh and mesh[1] in mesh_names:prefab_mesh[guid[1]]=mesh_names[mesh[1]]
def compose(a,b):
    a,b=matrices[a],matrices[b]
    return matrices.index((a[0]*b[0]+a[1]*b[2],a[0]*b[1]+a[1]*b[3],a[2]*b[0]+a[3]*b[2],a[2]*b[1]+a[3]*b[3]))
def props_for(m,orientation=0):
    props=[]
    for p in m['propPlacements']:
        name=prefab_mesh.get(p['prop'].get('guid'))
        if name:
            vec=lambda key,scale:[int(p[key][a])/scale for a in 'xyz']
            props.append(dict(mesh=name,orientation=orientation,position=vec('pos',254),rotation=vec('rotationByte',256/360),scale=vec('scale',128)))
    return props
for identifier in sorted(used):
    m=d['modules'][identifier//8];orientation=identifier%8
    profile=d['sideLookups'][integers(m['profileIndexes'],2)[orientation]]
    side0=[int(profile[f'v{i}0']) for i in range(4)]+[int(profile['v4']),int(profile['v5'])]
    side1=[int(profile[f'v{i}1']) for i in range(4)]+[int(profile['v4']),int(profile['v5'])]
    pieces=[dict(mesh=int(p['moduleIndex']),orientation=int(p['flags'])&7) for p in m['orientedModuleMeshes']]
    mesh_ids.update(p['mesh'] for p in pieces)
    decor=[int(p['moduleIndex'])*8+compose(orientation,int(p['flags'])&7) for p in m['decorModules']]
    decor_ids.update(decor)
    candidates.append(dict(id=identifier,cost=int(m['cost']),orientation=orientation,side0=side0,side1=side1,pieces=pieces,props=props_for(m,orientation),decor=decor))
decorations=[dict(id=-1,cost=0,side0=[0]*6,side1=[0]*6,props=[])]
for identifier in sorted(decor_ids):
    m=d['decorModules'][identifier//8];orientation=identifier%8
    profile=d['sideLookups'][integers(m['profileIndexes'],2)[orientation]]
    side0=[int(profile[f'v{i}0']) for i in range(4)]+[int(profile['v4']),int(profile['v5'])]
    side1=[int(profile[f'v{i}1']) for i in range(4)]+[int(profile['v4']),int(profile['v5'])]
    pieces=[dict(mesh=int(p['moduleIndex']),orientation=int(p['flags'])&7) for p in m['orientedModuleMeshes']]
    mesh_ids.update(p['mesh'] for p in pieces)
    decorations.append(dict(pieces=pieces,id=identifier,cost=int(m['cost']),orientation=orientation,side0=side0,side1=side1,props=props_for(m,orientation)))
meshes=[]
for identifier in sorted(mesh_ids):
    m=d['moduleMeshes'][identifier]
    vec=lambda key,axes,scale:[int(v[a])/scale for v in m[key] for a in axes]
    meshes.append(dict(id=identifier,material=guids.get(m['material']['guid'],m['material']['guid']),outline=[int(v["b"]) for v in m["outlineUvs"]],corner=int(m["corner"]),materialType=int(m["materialType"]),vertices=vec('verts','xyz',254),normals=vec('normals','xyz',127),uv=vec('uvs','xy',256),triangles=integers(m['tris'])))
catalog=dict(cases=cases,candidates=candidates,meshes=meshes,decorations=decorations,decorOverlaps=sorted(set(v if v<2147483648 else v-4294967296 for v in integers(d["decorOverlaps"],4))))
(ROOT/'Assets/TownBuilder/Resources/RecoveredModules.json').write_text(json.dumps(catalog,separators=(',',':')),encoding='utf-8')
report=dict(source=str(SOURCE/'MonoBehaviour/ModuleLibrary.asset'),sourceCounts={k:len(d[k]) for k in ['modules','decorModules','cornerLookups','sideLookups','moduleMeshes']},sideViewCases=len(cases),candidateCount=len(candidates),meshCount=len(meshes),orientedDecorCandidates=len(decorations)-1,decorOverlapHashes=len(catalog["decorOverlaps"]),propPlacements=sum(len(c["props"]) for c in candidates+decorations),materials=sorted(set(m['material'] for m in meshes)),coordinateScale=254,uvScale=256)
(ROOT/'Reports/module-extraction.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
