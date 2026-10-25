using UnityEngine;
namespace TownStudy.Builder {
[ExecuteAlways] public sealed class WorkshopBackdrop:MonoBehaviour {
 public Camera view;
 void LateUpdate(){if(view==null)return;const float distance=55;float size=view.orthographic?view.orthographicSize:distance*Mathf.Tan(view.fieldOfView*.5f*Mathf.Deg2Rad);transform.position=view.transform.position+view.transform.forward*distance;transform.rotation=view.transform.rotation;transform.localScale=new Vector3(size*view.aspect*2.1f,size*2.1f,1);}
}
}
