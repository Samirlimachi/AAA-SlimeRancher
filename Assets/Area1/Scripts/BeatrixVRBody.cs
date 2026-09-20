using System;
using System.Linq;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Rendering;
namespace SlimeRancherVR {
[DefaultExecutionOrder(10000)]
public sealed class BeatrixVRBody : MonoBehaviour {
 public Transform trackingOrigin, headTarget, leftTarget, rightTarget;
 public Animator animator;
 public SkinnedMeshRenderer headRenderer;
 public float standingEyeHeight=1.5f;
 public bool previewTracking;
 public Vector3 leftHandRotationOffset = new Vector3(0,0,90), rightHandRotationOffset = new Vector3(0,0,-90);
 Transform head,hips,lu,ll,lh,ru,rl,rh;
 Quaternion headOffset;
 Vector3 previousHead;
 bool initialized;
 public float MeasuredSpeed {get; private set;}
 public static bool Tracked(XRNode node) {
  if(RanchVRControls.Instance&&RanchVRControls.Instance.useSimulator){
   foreach(var device in UnityEngine.InputSystem.InputSystem.devices){
    if(node==XRNode.Head&&device is UnityEngine.InputSystem.XR.XRHMD hmd&&hmd.isTracked.isPressed)return true;
    if(device is UnityEngine.InputSystem.XR.XRController hand&&hand.isTracked.isPressed)foreach(var usage in hand.usages)if((node==XRNode.LeftHand&&usage.ToString()=="LeftHand")||(node==XRNode.RightHand&&usage.ToString()=="RightHand"))return true;
   }
  }
  var d=InputDevices.GetDeviceAtXRNode(node);
  return d.isValid && d.TryGetFeatureValue(CommonUsages.isTracked,out bool tracked) && tracked;
 }
 public Transform Bone(string name) => GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name);
 void Awake() {
  head=Bone("Head");hips=Bone("Hips");lu=Bone("UpperArm.L");ll=Bone("LowerArm.L");lh=Bone("Hand.L");ru=Bone("UpperArm.R");rl=Bone("LowerArm.R");rh=Bone("Hand.R");
  initialized=headTarget&&trackingOrigin&&animator&&head&&hips&&lu&&ll&&lh&&ru&&rl&&rh;
  if(!initialized) { Debug.LogError("Beatrix: faltan referencias del esqueleto o del equipo XR.",this);enabled=false;return; }
  headOffset=Quaternion.Inverse(transform.rotation)*head.rotation;
  previousHead=headTarget.position;
  animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  if(headRenderer)headRenderer.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
 }
 void Update() {
  if(!initialized)return;
  Vector3 delta=headTarget.position-previousHead;delta.y=0;
  // Teleportation must not produce a burst of walking.
  MeasuredSpeed=delta.magnitude>.5f?0:delta.magnitude/Mathf.Max(Time.deltaTime,.001f);
  previousHead=headTarget.position;
  animator.SetFloat("Speed",MeasuredSpeed,.12f,Time.deltaTime);
 }
 void LateUpdate() { ApplyTracking(); }
 public void ApplyTracking() {
  if(!initialized)return;
  Vector3 forward=Vector3.ProjectOnPlane(headTarget.forward,Vector3.up);
  if(forward.sqrMagnitude>.02f)transform.rotation=Quaternion.LookRotation(forward,Vector3.up);
  float crouch=Mathf.Clamp(headTarget.position.y-trackingOrigin.position.y-standingEyeHeight,-.65f,.3f);
  transform.position=new Vector3(headTarget.position.x,trackingOrigin.position.y+crouch,headTarget.position.z)-transform.forward*.07f;
  head.rotation=headTarget.rotation*headOffset;
  if(previewTracking||Tracked(XRNode.LeftHand)) SolveArm(lu,ll,lh,leftTarget, -1,leftHandRotationOffset);
  if(previewTracking||Tracked(XRNode.RightHand)) SolveArm(ru,rl,rh,rightTarget,1,rightHandRotationOffset);
 }
 void SolveArm(Transform upper,Transform lower,Transform hand,Transform target,float side,Vector3 rotationOffset) {
  if(!target)return;
  Vector3 start=upper.position, end=target.position;
  float a=Vector3.Distance(start,lower.position),b=Vector3.Distance(lower.position,hand.position);
  Vector3 direction=end-start;float distance=direction.magnitude;
  if(a<.001f||b<.001f||distance<.001f)return;
  direction/=distance;distance=Mathf.Clamp(distance,Mathf.Abs(a-b)+.001f,a+b-.001f);
  Vector3 pole=transform.right*side*.6f-transform.up*.7f-transform.forward*.25f;
  Vector3 bend=Vector3.ProjectOnPlane(pole,direction).normalized;
  if(bend.sqrMagnitude<.01f)bend=Vector3.ProjectOnPlane(transform.forward,direction).normalized;
  float along=(a*a-b*b+distance*distance)/(2*distance);
  Vector3 elbow=start+direction*along+bend*Mathf.Sqrt(Mathf.Max(0,a*a-along*along));
  upper.rotation=Quaternion.FromToRotation(lower.position-start,elbow-start)*upper.rotation;
  Vector3 reachable=start+direction*distance;
  lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,reachable-lower.position)*lower.rotation;
  hand.rotation=target.rotation*Quaternion.Euler(rotationOffset);
 }
}
}