using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using SlimeRancherVR;
namespace SlimeRancher.Area1 {
 public sealed class Area1HandVisual : MonoBehaviour {
  public bool left;
  Transform[] joints; Quaternion[] rest; float curl,index;
  void Awake(){joints=GetComponentsInChildren<Transform>().Where(t=>t.name.Contains("Proximal")||t.name.Contains("Intermediate")||t.name.Contains("Distal")).ToArray();rest=joints.Select(t=>t.localRotation).ToArray();}
  void LateUpdate(){var input=RanchVRControls.ReadHand(left?XRNode.LeftHand:XRNode.RightHand,true);curl=Mathf.MoveTowards(curl,input.grip?1:0,Time.deltaTime*8);index=Mathf.MoveTowards(index,input.trigger,Time.deltaTime*8);
   for(int i=0;i<joints.Length;i++){bool thumb=joints[i].name.Contains("Thumb");float amount=joints[i].name.Contains("Index")?index:curl;float angle=thumb?20:55;joints[i].localRotation=rest[i]*Quaternion.Euler(angle*amount,0,0);}
  }
 }
}
