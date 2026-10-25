using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace TownStudy.Builder
{
    public sealed class BuilderHud : MonoBehaviour
    {
        BuilderController builder;
        UnityEngine.UI.Text status;
        Font font;
        void Start()
        {
            builder=GetComponent<BuilderController>(); font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasGo=new GameObject("Workshop HUD",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            canvasGo.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=canvasGo.GetComponent<UnityEngine.UI.CanvasScaler>(); scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1600,900); scaler.matchWidthOrHeight=.5f;
            if(EventSystem.current==null) new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
            var top=Panel(canvasGo.transform,"Toolbar",new Vector2(0,1),new Vector2(1,1),new Vector2(0,-94),Vector2.zero);
            Label(top,"CASTLE WORKSHOP",new Vector2(24,-8),new Vector2(420,32),24);
            string[] actions={"Build","Erase","Undo","Redo","Save","Load","Example","Clear","Remix","Fit"};
            for(int i=0;i<actions.Length;i++)
            {
                string action=actions[i]; var go=new GameObject(action,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button)); go.transform.SetParent(top,false);
                Place(go.GetComponent<RectTransform>(),new Vector2(24+i*130,-48),new Vector2(120,34));
                go.GetComponent<UnityEngine.UI.Image>().color=new Color(.24f,.38f,.4f);
                go.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(()=>builder.Action(action));
                var label=Label(go.transform,action,Vector2.zero,new Vector2(120,34),17); label.alignment=TextAnchor.MiddleCenter;
            }
            var bottom=Panel(canvasGo.transform,"Status",Vector2.zero,new Vector2(1,0),Vector2.zero,new Vector2(0,64));
            status=Label(bottom,"",new Vector2(24,-4),new Vector2(1500,25),17);
            Label(bottom,"LMB / drag: build   |   RMB: erase   |   Wheel: zoom   |   MMB: pan   |   F: fit   |   Ctrl+Z / Y: undo / redo",new Vector2(24,-32),new Vector2(1500,24),16);
        }
        void Update() { if(status!=null) status.text=(builder.eraseMode?"ERASE":"BUILD")+"   /   "+builder.Board.Cells.Count+" blocks   /   Floating blocks receive automatic supports   /   "+builder.Notice; }
        RectTransform Panel(Transform parent,string name,Vector2 min,Vector2 max,Vector2 offsetMin,Vector2 offsetMax)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image)); go.transform.SetParent(parent,false);
            var r=go.GetComponent<RectTransform>(); r.anchorMin=min;r.anchorMax=max;r.offsetMin=offsetMin;r.offsetMax=offsetMax;
            go.GetComponent<UnityEngine.UI.Image>().color=new Color(.09f,.16f,.19f,.97f); return r;
        }
        static void Place(RectTransform r,Vector2 position,Vector2 size) { r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=position;r.sizeDelta=size; }
        UnityEngine.UI.Text Label(Transform parent,string text,Vector2 position,Vector2 size,int fontSize)
        {
            var go=new GameObject("Label",typeof(RectTransform),typeof(UnityEngine.UI.Text)); go.transform.SetParent(parent,false);Place(go.GetComponent<RectTransform>(),position,size);
            var label=go.GetComponent<UnityEngine.UI.Text>();label.font=font;label.text=text;label.fontSize=fontSize;label.color=new Color(.93f,.92f,.85f);label.raycastTarget=false;return label;
        }
    }
}
