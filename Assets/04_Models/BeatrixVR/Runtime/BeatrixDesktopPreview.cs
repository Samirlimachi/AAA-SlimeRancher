using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
namespace SlimeRancherVR {
[DefaultExecutionOrder(9000)]
public sealed class BeatrixDesktopPreview : MonoBehaviour {
 public BeatrixVRBody body;
 public Transform head,leftHand,rightHand;
 public float speed=2.6f;
 public static bool InputReady=>Cursor.lockState==CursorLockMode.Locked&&Time.frameCount>lockedFrame&&Application.isFocused;
 static int lockedFrame;
 readonly System.Collections.Generic.Dictionary<Canvas,bool> simulatorUI=new System.Collections.Generic.Dictionary<Canvas,bool>();float nextUI;
 TrackedPoseDriver[] drivers;bool preview;float pitch=12,verticalSpeed;CharacterController controller;
 void Awake(){drivers=GetComponentsInChildren<TrackedPoseDriver>(true);controller=GetComponent<CharacterController>();}
 void Update(){
  bool next=!RanchVRControls.Active&&!UnityEngine.XR.XRSettings.isDeviceActive&&!BeatrixVRBody.Tracked(XRNode.Head);
  if(next!=preview){preview=next;foreach(var d in drivers)d.enabled=!preview;}
  body.previewTracking=preview;if(!preview)return;
  if(Time.unscaledTime>=nextUI){nextUI=Time.unscaledTime+1;foreach(var canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None)){for(var t=canvas.transform;t;t=t.parent)if(t.name.StartsWith("XR Interaction Simulator UI")){if(!simulatorUI.ContainsKey(canvas))simulatorUI.Add(canvas,canvas.enabled);canvas.enabled=false;break;}}}
  if(head.parent)head.parent.localPosition=Vector3.zero;head.localPosition=new Vector3(0,body.standingEyeHeight,0);
  var k=Keyboard.current;var m=Mouse.current;
  if(k!=null&&(k.tabKey.wasPressedThisFrame||k.escapeKey.wasPressedThisFrame)){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
  else if(m!=null&&(m.leftButton.wasPressedThisFrame||m.rightButton.wasPressedThisFrame)&&Cursor.lockState!=CursorLockMode.Locked){Cursor.lockState=CursorLockMode.Locked;Cursor.visible=false;lockedFrame=Time.frameCount;}
  if(InputReady&&m!=null){var delta=m.delta.ReadValue();transform.Rotate(0,delta.x*.12f,0);pitch=Mathf.Clamp(pitch-delta.y*.12f,-80,80);}
  head.localRotation=Quaternion.Euler(pitch,0,0);Vector3 move=Vector3.zero;
  if(k!=null&&InputReady){if(k.wKey.isPressed)move.z++;if(k.sKey.isPressed)move.z--;if(k.dKey.isPressed)move.x++;if(k.aKey.isPressed)move.x--;}
  bool sprint=RanchGame.Instance&&RanchGame.Instance.Sprint(k!=null&&k.leftShiftKey.isPressed&&move.sqrMagnitude>0,Time.deltaTime);
  move=transform.TransformDirection(Vector3.ClampMagnitude(move,1))*speed*(sprint?1.7f:1);
  if(controller&&controller.enabled){if(controller.isGrounded){if(verticalSpeed<0)verticalSpeed=-2;if(k!=null&&InputReady&&k.spaceKey.wasPressedThisFrame)verticalSpeed=5;}verticalSpeed-=15*Time.deltaTime;controller.Move((move+Vector3.up*verticalSpeed)*Time.deltaTime);}else transform.position+=move*Time.deltaTime;
  leftHand.SetPositionAndRotation(head.TransformPoint(new Vector3(-.25f,-.34f,.30f)),head.rotation);rightHand.SetPositionAndRotation(head.TransformPoint(new Vector3(.25f,-.34f,.30f)),head.rotation);
 }
 void OnApplicationFocus(bool focused){if(!focused){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}}
 void OnDisable(){Cursor.lockState=CursorLockMode.None;Cursor.visible=true;foreach(var pair in simulatorUI)if(pair.Key)pair.Key.enabled=pair.Value;if(drivers!=null)foreach(var d in drivers)if(d)d.enabled=true;if(body)body.previewTracking=false;}
}
}

