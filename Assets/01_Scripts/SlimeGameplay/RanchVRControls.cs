using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
namespace SlimeRancherVR {
public struct RanchVRHand {
 public bool tracked,grip,primary,secondary,stickClick;
 public float trigger;
 public Vector2 stick;
}
[DefaultExecutionOrder(9500)]
public sealed class RanchVRControls : MonoBehaviour {
 public static RanchVRControls Instance {get;private set;}
 public static bool Active=>Instance&&Instance.isActiveAndEnabled&&Instance.VRActive;
 [Tooltip("Activa los mandos del XR Interaction Simulator sin un visor físico.")]
 public bool useSimulator;
 public float moveSpeed=2.6f,snapAngle=30;
 public bool VRActive {get;private set;}
 public RanchVRHand Left {get;private set;}
 public RanchVRHand Right {get;private set;}
 public RanchGame game;
 public Transform head;
 CharacterController controller;float verticalSpeed,loadHold;bool lastPrimary,lastSecondary,lastJump,lastSave,lastGrip,turnReady=true,loadDone;
 readonly Dictionary<LocomotionProvider,bool> providers=new Dictionary<LocomotionProvider,bool>();
 void Awake(){Instance=this;controller=GetComponent<CharacterController>();}
 void Start(){if(!game)game=RanchGame.Instance;if(!head&&Camera.main)head=Camera.main.transform;ShowControllers();}
 void ShowControllers(){
  foreach(var mode in GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Inputs.XRInputModalityManager>(true)){
   mode.enabled=false;
   if(mode.leftHand)mode.leftHand.SetActive(false);
   if(mode.rightHand)mode.rightHand.SetActive(false);
   if(mode.leftController)mode.leftController.SetActive(true);
   if(mode.rightController)mode.rightController.SetActive(true);
  }
  var body=GetComponentInChildren<BeatrixVRBody>();if(!body)return;
  foreach(var hand in new[]{body.leftTarget,body.rightTarget}){
   if(!hand)continue;hand.gameObject.SetActive(true);
   foreach(var t in hand.GetComponentsInChildren<Transform>(true)){
    if(t.name!="Controller_Base")continue;
    var model=t.parent;
    foreach(var renderer in model.GetComponentsInChildren<MeshRenderer>(true)){
     renderer.enabled=true;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
     for(var parent=renderer.transform;parent&&parent!=hand;parent=parent.parent)parent.gameObject.SetActive(true);
    }
   }
  }
 }
 public static RanchVRHand ReadHand(XRNode node,bool simulator){
  var result=new RanchVRHand();var device=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
  if(!simulator&&device.isValid&&device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked,out bool tracked)&&tracked){
   result.tracked=true;device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxis,out result.stick);device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger,out result.trigger);device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.triggerButton,out bool triggerButton);if(triggerButton)result.trigger=Mathf.Max(result.trigger,1);
   device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton,out result.grip);device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.grip,out float grip);result.grip|=grip>.6f;device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton,out result.primary);device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.secondaryButton,out result.secondary);device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primary2DAxisClick,out result.stickClick);return result;
  }
  foreach(var input in InputSystem.devices){if(!(input is XRController xr))continue;if(!simulator&&input.GetType().Name.Contains("Simulated"))continue;bool matching=false;foreach(var usage in input.usages)if(usage.ToString()==(node==XRNode.LeftHand?"LeftHand":"RightHand"))matching=true;if(!matching||!xr.isTracked.isPressed)continue;
   result.tracked=true;result.stick=input.TryGetChildControl<Vector2Control>("primary2DAxis")?.ReadValue()??Vector2.zero;result.trigger=input.TryGetChildControl<AxisControl>("trigger")?.ReadValue()??0;result.grip=(input.TryGetChildControl<AxisControl>("grip")?.ReadValue()??0)>.6f||Pressed(input,"gripButton");result.primary=Pressed(input,"primaryButton");result.secondary=Pressed(input,"secondaryButton");result.stickClick=Pressed(input,"primary2DAxisClick");return result;
  }
  return result;
 }
 static bool Pressed(UnityEngine.InputSystem.InputDevice d,string name)=>d.TryGetChildControl<ButtonControl>(name)?.isPressed??false;
 void SetLocomotion(bool own){if(own){foreach(var p in GetComponentsInChildren<LocomotionProvider>(true)){if(!providers.ContainsKey(p))providers[p]=p.enabled;p.enabled=false;}}else {foreach(var pair in providers)if(pair.Key)pair.Key.enabled=pair.Value;providers.Clear();}}
 void Update(){
  VRActive=useSimulator||XRSettings.isDeviceActive||BeatrixVRBody.Tracked(XRNode.Head);
  if(!VRActive||!Application.isFocused||!game||!head){Left=Right=default;SetLocomotion(false);ResetButtons();return;}
  SetLocomotion(true);Left=ReadHand(XRNode.LeftHand,useSimulator);Right=ReadHand(XRNode.RightHand,useSimulator);
  if(Right.primary&&!lastPrimary)game.Select(game.selected+1);if(Right.secondary&&!lastSecondary)game.Select(game.selected-1);
  if(Left.grip&&!lastGrip&&game.upgradeStation&&Vector3.Distance(head.position,game.upgradeStation.position)<2.8f)game.BuyUpgrade();
  bool loadChord=Left.secondary&&Left.trigger>.7f;
  if(loadChord){loadHold+=Time.unscaledDeltaTime;if(!loadDone){game.Notify("Cargar partida: mantén Y + gatillo izquierdo (2 s)");if(loadHold>=2){game.LoadGame();verticalSpeed=0;loadDone=true;}}}
  else {loadHold=0;loadDone=false;if(Left.secondary&&!lastSave)game.SaveGame();}
  var axis=Left.stick;if(axis.magnitude<.18f)axis=Vector2.zero;axis=Vector2.ClampMagnitude(axis,1);
  var forward=Vector3.ProjectOnPlane(head.forward,Vector3.up).normalized;if(forward.sqrMagnitude<.01f)forward=transform.forward;
  bool sprint=game.Sprint(Left.stickClick&&axis.sqrMagnitude>0,Time.deltaTime);
  var move=(forward*axis.y+Vector3.Cross(Vector3.up,forward)*axis.x)*moveSpeed*(sprint?1.7f:1);
  if(controller&&controller.enabled){
   // Keep the capsule under the tracked head, including room-scale crouching.
   var local=transform.InverseTransformPoint(head.position);controller.height=Mathf.Clamp(local.y,.65f,2.2f);controller.center=new Vector3(local.x,controller.height*.5f,local.z);
   if(controller.isGrounded){if(verticalSpeed<0)verticalSpeed=-2;if(Left.primary&&!lastJump)verticalSpeed=5;}
   verticalSpeed-=15*Time.deltaTime;controller.Move((move+Vector3.up*verticalSpeed)*Time.deltaTime);
  }
  if(Mathf.Abs(Right.stick.x)<.25f)turnReady=true;
  if(turnReady&&Mathf.Abs(Right.stick.x)>.7f){transform.RotateAround(head.position,Vector3.up,Mathf.Sign(Right.stick.x)*snapAngle);turnReady=false;}
  lastPrimary=Right.primary;lastSecondary=Right.secondary;lastJump=Left.primary;lastSave=Left.secondary;lastGrip=Left.grip;
 }
 void ResetButtons(){lastPrimary=lastSecondary=lastJump=lastSave=lastGrip=false;loadHold=0;loadDone=false;turnReady=true;verticalSpeed=0;}
 void OnDisable(){VRActive=false;Left=Right=default;SetLocomotion(false);ResetButtons();}
 void OnDestroy(){if(Instance==this)Instance=null;}
 public static void Haptic(float amplitude=.25f,float seconds=.06f){if(!Active||Instance.useSimulator)return;var device=UnityEngine.XR.InputDevices.GetDeviceAtXRNode(XRNode.RightHand);if(device.TryGetHapticCapabilities(out var cap)&&cap.supportsImpulse)device.SendHapticImpulse(0,amplitude,seconds);}
}
}

