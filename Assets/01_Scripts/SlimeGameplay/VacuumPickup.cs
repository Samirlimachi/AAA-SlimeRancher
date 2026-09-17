using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
namespace SlimeRancherVR {
[RequireComponent(typeof(Rigidbody),typeof(XRGrabInteractable))]
[DefaultExecutionOrder(10500)]
public sealed class VacuumPickup : MonoBehaviour {
 public Transform leftHand,rightHand;
 public bool IsHeld=>desktopHeld||(grab&&grab.isSelected);
 public bool IsLeftHand=>!desktopHeld&&grab&&grab.isSelected&&leftHand&&grab.interactorsSelecting[0].transform.IsChildOf(leftHand);
 XRGrabInteractable grab;Rigidbody physicsBody;SlimeVacuum vacuum;bool desktopHeld;Vector3 home;
 Collider[] own,playerColliders;
 void Awake(){grab=GetComponent<XRGrabInteractable>();physicsBody=GetComponent<Rigidbody>();vacuum=GetComponent<SlimeVacuum>();home=transform.position;own=GetComponentsInChildren<Collider>();playerColliders=vacuum.playerRoot.GetComponentsInChildren<Collider>();grab.selectEntered.AddListener(OnGrab);grab.selectExited.AddListener(OnRelease);}
 void OnGrab(SelectEnterEventArgs args){IgnorePlayer(true);vacuum.SetSuction(false);RanchGame.Instance?.Notify("Gatillo de esta mano: aspirar. Otro gatillo: lanzar. Suelta el grip para dejarla.");}
 void OnRelease(SelectExitEventArgs args){vacuum.SetSuction(false);IgnorePlayer(false);}
 void IgnorePlayer(bool value){if(own==null||playerColliders==null)return;foreach(var a in own)if(a)foreach(var b in playerColliders)if(b&&a!=b)Physics.IgnoreCollision(a,b,value);}
 public bool PickUpDesktop(){
  if(IsHeld||RanchVRControls.Active||!Camera.main||!rightHand)return false;
  var head=Camera.main.transform;if(Vector3.Distance(head.position,transform.position)>2)return false;
  var delta=transform.position-head.position;foreach(var hit in Physics.RaycastAll(head.position,delta.normalized,delta.magnitude,~0,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(vacuum.playerRoot))return false;
  desktopHeld=true;physicsBody.linearVelocity=Vector3.zero;physicsBody.angularVelocity=Vector3.zero;physicsBody.isKinematic=true;physicsBody.useGravity=false;IgnorePlayer(true);return true;
 }
 public void DropDesktop(){if(!desktopHeld)return;desktopHeld=false;physicsBody.isKinematic=false;physicsBody.useGravity=true;physicsBody.linearVelocity=Vector3.zero;IgnorePlayer(false);vacuum.SetSuction(false);}
 void Update(){
  if(desktopHeld&&RanchVRControls.Active)DropDesktop();
  if(!RanchVRControls.Active&&Keyboard.current!=null&&Keyboard.current.fKey.wasPressedThisFrame){if(desktopHeld)DropDesktop();else PickUpDesktop();}
  if(!IsHeld&&transform.position.y<-6){physicsBody.position=home;physicsBody.linearVelocity=Vector3.zero;physicsBody.angularVelocity=Vector3.zero;}
  if(!IsHeld)vacuum.SetSuction(false);
 }
 void LateUpdate(){if(desktopHeld&&rightHand)transform.SetPositionAndRotation(rightHand.TransformPoint(new Vector3(0,-.035f,.16f)),rightHand.rotation);}
 void OnDisable(){DropDesktop();if(vacuum)vacuum.SetSuction(false);IgnorePlayer(false);}
 void OnDestroy(){if(grab){grab.selectEntered.RemoveListener(OnGrab);grab.selectExited.RemoveListener(OnRelease);}}
 void OnGUI(){if(RanchVRControls.Active||!Camera.main)return;if(!IsHeld&&Vector3.Distance(Camera.main.transform.position,transform.position)<2)GUI.Label(new Rect(Screen.width/2-160,Screen.height/2+50,360,35),"F: recoger aspiradora");else if(desktopHeld)GUI.Label(new Rect(Screen.width-210,35,200,30),"F: soltar aspiradora");}
}
}
