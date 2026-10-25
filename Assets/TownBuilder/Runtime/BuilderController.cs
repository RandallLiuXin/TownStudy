using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TownStudy.Builder
{
    public sealed class BuilderController : MonoBehaviour
    {
        public Mesh[] propMeshes;
        public Material[] materials;
        public Material sourceHouse, sourceFence, sourceProps, floorMaterial;
        public Camera view;
        public BuildBoard Board { get; private set; }
        public bool eraseMode;
        public string Notice { get; private set; } = "Ready";
        public string SavePath => Path.Combine(Application.persistentDataPath, "castle-layout.json");
        Transform geometry;
        Vector2 lastMouse;
        Vector2Int? lastCell;
        bool stroke;
        Canvas cursor;
        UnityEngine.UI.Image[] cursorEdges;
        void Awake()
        {
            Initialize();
            if(Application.isPlaying)
            {
                gameObject.AddComponent<BuilderHud>();
                cursor=new GameObject("Placement cursor",typeof(Canvas)).GetComponent<Canvas>();
                cursor.renderMode=RenderMode.ScreenSpaceOverlay;cursor.sortingOrder=100;
                cursorEdges=new UnityEngine.UI.Image[4];
                for(int i=0;i<4;i++){var edge=new GameObject("Edge",typeof(RectTransform),typeof(UnityEngine.UI.Image));edge.transform.SetParent(cursor.transform,false);cursorEdges[i]=edge.GetComponent<UnityEngine.UI.Image>();cursorEdges[i].raycastTarget=false;var rect=edge.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=Vector2.zero;rect.pivot=Vector2.zero;}

            }
        }
        public void Initialize()
        {
            Board=new BuildBoard(); Board.Demo(); Rebuild(); Fit();
        }
        public void Action(string action)
        {
            try
            {
                switch(action)
                {
                    case "Build": eraseMode=false; break;
                    case "Erase": eraseMode=true; break;
                    case "Undo": Board.Undo(); break;
                    case "Redo": Board.Redo(); break;
                    case "Example": Board.Demo(); break;
                    case "Clear": Board.Clear(); break;
                    case "Remix": Board.Reroll(); break;
                    case "Fit": Fit(); break;
                    case "Save": File.WriteAllText(SavePath,Board.Export()); Notice="Layout saved"; return;
                    case "Load":
                        if(!File.Exists(SavePath)) { Notice="No saved layout yet"; return; }
                        if(new FileInfo(SavePath).Length>200000 || !Board.Import(File.ReadAllText(SavePath))) { Notice="Invalid layout"; return; }
                        break;
                }
                Rebuild(); Notice=action+" complete";
            }
            catch(Exception e) { Notice=e.Message; Debug.LogException(e); }
        }
        public bool ScreenCell(Vector2 screen, out Vector2Int cell)
        {
            var ray=view.ScreenPointToRay(screen);
            if(new Plane(Vector3.forward,Vector3.zero).Raycast(ray,out float distance))
            { Vector3 p=ray.GetPoint(distance); cell=new Vector2Int(Mathf.RoundToInt(p.x),Mathf.RoundToInt(p.y)); return BuildBoard.InBounds(cell); }
            cell=default; return false;
        }
        public bool PointerBlocked(Vector2 position)
        {
            if(EventSystem.current==null) return false;
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=position},hits);
            return hits.Count>0;
        }
        void Update()
        {
            var mouse=Mouse.current; if(mouse==null) return;
            Vector2 pos=mouse.position.ReadValue(); bool blocked=PointerBlocked(pos);
            if(cursor!=null)
            {
                cursor.enabled=!blocked&&ScreenCell(pos,out _);
                if(cursor.enabled)
                {
                    ScreenCell(pos,out var hover);float x=hover.x,y=hover.y;
                    Vector2 lo=view.WorldToScreenPoint(new Vector3(x-.5f,y-.5f,0)),hi=view.WorldToScreenPoint(new Vector3(x+.5f,y+.5f,0));
                    var color=(eraseMode||mouse.rightButton.isPressed)?new Color(.95f,.3f,.23f):new Color(.85f,.98f,.92f);
                    for(int i=0;i<4;i++){var rect=cursorEdges[i].rectTransform;rect.anchoredPosition=new Vector2(i==1?hi.x-2:lo.x,i==3?hi.y-2:lo.y);rect.sizeDelta=i<2?new Vector2(2,hi.y-lo.y):new Vector2(hi.x-lo.x,2);cursorEdges[i].color=color;}

                }
            }
            if(mouse.leftButton.wasPressedThisFrame||mouse.rightButton.wasPressedThisFrame) { stroke=!blocked; lastCell=null; }
            if(!mouse.leftButton.isPressed&&!mouse.rightButton.isPressed) { stroke=false; lastCell=null; }
            if(stroke&&!blocked&&ScreenCell(pos,out var p)&&lastCell!=p)
            { bool occupied=!(eraseMode||mouse.rightButton.isPressed);if(Board.Cells.Contains(p)!=occupied){if(Board.Set(p,occupied)){Rebuild();Notice="Ready";}else Notice="No compatible solution in time; layout preserved";} lastCell=p; }
            if(!blocked)
            {
                float scroll=mouse.scroll.ReadValue().y;
                if(scroll!=0)
                {
                    float size=Mathf.Clamp(ViewHalfHeight()*(scroll>0?.9f:1.1f),3,18);
                    if(view.orthographic)view.orthographicSize=size;
                    else {var position=view.transform.position;position.z=-size/Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad);view.transform.position=position;}
                }
                if(mouse.middleButton.isPressed&&!mouse.middleButton.wasPressedThisFrame)
                { Vector2 delta=(pos-lastMouse)*(2*ViewHalfHeight()/Screen.height); view.transform.position-=new Vector3(delta.x,delta.y,0); }
            }
            lastMouse=pos;
            var k=Keyboard.current; if(k==null) return;
            if(k.fKey.wasPressedThisFrame) Fit();
            if(k.leftCtrlKey.isPressed||k.rightCtrlKey.isPressed)
            {
                if(k.zKey.wasPressedThisFrame) Action(k.leftShiftKey.isPressed?"Redo":"Undo");
                if(k.yKey.wasPressedThisFrame) Action("Redo");
            }
        }
        float ViewHalfHeight()=>view.orthographic?view.orthographicSize:Mathf.Abs(view.transform.position.z)*Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad);
        public void Fit()
        {
            if(view==null) return;
            float lo=Board.Cells.Count>0?Board.Cells.Min(p=>p.x):-5, hi=Board.Cells.Count>0?Board.Cells.Max(p=>p.x):5;
            float top=Board.Cells.Count>0?Board.Cells.Max(p=>p.y):4;
            float size=Mathf.Max(5,(top+5)*.65f,(hi-lo+6)/Mathf.Max(1,view.aspect)/1.4f);
            view.orthographicSize=size;
            float distance=view.orthographic?25:size/Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad);
            view.transform.position=new Vector3((lo+hi)*.5f,(top-1)*.5f,-distance);
        }
        GameObject Render(string name, Mesh mesh, Vector3 position)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)); go.transform.SetParent(geometry,false); go.transform.localPosition=position;
            go.GetComponent<MeshFilter>().sharedMesh=mesh; go.GetComponent<MeshRenderer>().sharedMaterials=materials; return go;
        }
        GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
        {
            var go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(geometry,false); go.transform.localPosition=position; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;
            if(Application.isPlaying) Destroy(go.GetComponent<Collider>()); else DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        static Vector3 Vec(float[] v)=>new Vector3(v[0],v[1],v[2]);
        void RenderProps(OriginalProp[] props,Vector3Int slot)
        {
            if(props==null||propMeshes==null)return;
            foreach(var prop in props){
                var mesh=Array.Find(propMeshes,m=>m!=null&&m.name==prop.mesh);if(mesh==null)continue;
                var orientation=RecoveredCatalog.Orientation(prop.orientation);
                var root=new GameObject("SourceProp_"+prop.mesh).transform;root.SetParent(geometry,false);root.localPosition=(Vector3)slot+Vector3.one*.5f;
                root.localRotation=Quaternion.LookRotation(orientation.GetColumn(2),Vector3.up);root.localScale=new Vector3(orientation.determinant<0?-1:1,1,1);
                var go=new GameObject(prop.mesh,typeof(MeshFilter),typeof(MeshRenderer));go.transform.SetParent(root,false);go.transform.localPosition=Vec(prop.position);go.transform.localRotation=Quaternion.Euler(Vec(prop.rotation));go.transform.localScale=Vec(prop.scale);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=sourceProps;
                // The original recessed windows need stencil wall cutouts. This HDRP demo
                // brings their facade surface forward instead of importing that shader stack.
                if(prop.mesh.StartsWith("Window")||prop.mesh.StartsWith("Door")){
                    var axis=go.transform.TransformDirection(Vector3.right);if(Mathf.Abs(axis.z)>.8f)go.transform.position+=Vector3.forward*(slot.z<0?-.13f:.13f);
                }
            }
        }
        void SetCornerMask(GameObject go,Vector3Int slot){
            int key=SourceWfc.CaseKey(Board.Cells,slot),mask=0;int[] ids=slot.z<0?new[]{3,2,7,6}:new[]{0,1,4,5};for(int bit=0;bit<4;bit++)if(((key%16)&(1<<bit))!=0)mask|=1<<ids[bit];
            var block=new MaterialPropertyBlock();block.SetFloat("_CornerMask",mask);go.GetComponent<Renderer>().SetPropertyBlock(block);
        }
        public void Rebuild()
        {
            var old=transform.Find("Generated");
            if(old!=null) { old.name="Retired";old.gameObject.SetActive(false); if(Application.isPlaying) Destroy(old.gameObject); else DestroyImmediate(old.gameObject); }
            geometry=new GameObject("Generated").transform; geometry.SetParent(transform,false);
            foreach(var pair in Board.Tiles)
            {
                var mesh=RecoveredCatalog.Instance.MeshFor(pair.Value);
                if(mesh.vertexCount==0)continue;
                var go=Render("SourceModule_"+pair.Value,mesh,(Vector3)pair.Key+Vector3.one*.5f);
                go.GetComponent<Renderer>().sharedMaterials=new[]{sourceHouse,sourceFence};SetCornerMask(go,pair.Key);
            }
            var decorations=Board.Solver.SelectDecor(Board.Tiles,Board.Seed);
            foreach(var pair in Board.Tiles){
                RenderProps(RecoveredCatalog.Instance.ById[pair.Value].props,pair.Key);
                foreach(int id in decorations[pair.Key]){
                    RenderProps(RecoveredCatalog.Instance.DecorById[id].props,pair.Key);
                    var mesh=RecoveredCatalog.Instance.MeshFor(20000+id);if(mesh.vertexCount>0){var go=Render("SourceDecor_"+id,mesh,(Vector3)pair.Key+Vector3.one*.5f);go.GetComponent<Renderer>().sharedMaterials=new[]{sourceHouse,sourceFence};SetCornerMask(go,pair.Key);}
                }
            }
            float min=Board.Cells.Count>0?Board.Cells.Min(p=>p.x)-.8f:-5.8f,max=Board.Cells.Count>0?Board.Cells.Max(p=>p.x)+.8f:5.8f;
            float width=Mathf.Max(3,max-min),center=(min+max)*.5f;
            Primitive("Chassis",PrimitiveType.Cube,new Vector3(center,-1.15f,0),new Vector3(width,.3f,1.8f),materials[1]);
            Primitive("Floor",PrimitiveType.Cube,new Vector3(center,-1.58f,0),new Vector3(width+.35f,.55f,2.1f),floorMaterial!=null?floorMaterial:materials[1]);

        }
    }
}
