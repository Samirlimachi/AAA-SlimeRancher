#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEditor;
namespace SlimeRancherVR {
public sealed class RanchVRValidation : MonoBehaviour {
 string report="";RanchVRControls vr;RanchGame game;SlimeVacuum vac;XRSimulatedController left,right;
 XRSimulatedControllerState Rest=>new XRSimulatedControllerState{isTracked=true,trackingState=3,deviceRotation=Quaternion.identity};
 void Check(bool ok,string text){if(!ok)throw new Exception(text);report+="PASS: "+text+"\n";}
 IEnumerator Send(XRSimulatedController device,XRSimulatedControllerState state){InputSystem.QueueStateEvent(device,state);yield return null;yield return null;}
 IEnumerator Start(){yield return new WaitForSeconds(.6f);var run=Run();while(true){object current;try{if(!run.MoveNext())break;current=run.Current;}catch(Exception e){Finish("FAIL: "+e);yield break;}yield return current;}Finish("Hardware Quest not tested; simulated XR controller events passed.");}
 void Finish(string result){if(vr)vr.useSimulator=false;File.WriteAllText("C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/VR_Test.txt",report+result);SessionState.SetBool("VRControlValidation",false);EditorApplication.isPlaying=false;}
 XRSimulatedController Controller(string hand){var result=InputSystem.devices.OfType<XRSimulatedController>().FirstOrDefault(d=>d.usages.Any(u=>u.ToString()==hand));if(result==null){result=InputSystem.AddDevice<XRSimulatedController>();InputSystem.SetDeviceUsage(result,hand);}return result;}
 IEnumerator Run(){
  game=RanchGame.Instance;vr=FindAnyObjectByType<RanchVRControls>();vac=FindAnyObjectByType<SlimeVacuum>();Check(vr&&game&&vac,"VR controls present in INICIO");Check(!game.autoSave&&!game.autoLoad,"Tests cannot overwrite player save");game.saveName="INICIO_VR_validation.json";
  foreach(var script in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(script.GetType().Name=="XRInteractionSimulator"||script.GetType().Name=="XRDeviceSimulator")script.enabled=false;
  FindAnyObjectByType<BeatrixDesktopPreview>().enabled=false;FindAnyObjectByType<BeatrixVRBody>().enabled=false;foreach(var driver in vr.GetComponentsInChildren<TrackedPoseDriver>())driver.enabled=false;
  foreach(var item in FindObjectsByType<RanchItem>(FindObjectsSortMode.None))item.gameObject.SetActive(false);
  left=Controller("LeftHand");right=Controller("RightHand");vr.useSimulator=true;yield return Send(left,Rest);yield return Send(right,Rest);
  Check(RanchVRControls.Active&&vr.Left.tracked&&vr.Right.tracked,"XR left/right controller states detected without keyboard");Check(vr.GetComponentsInChildren<LocomotionProvider>(true).All(p=>!p.enabled),"Duplicate XRI locomotion providers disabled in VR");
  int selected=game.selected;yield return Send(right,Rest.WithButton(ControllerButton.PrimaryButton));Check(game.selected==(selected+1)%4,"Right A selects next inventory slot");yield return null;Check(game.selected==(selected+1)%4,"Holding A does not repeatedly change slots");yield return Send(right,Rest);yield return Send(right,Rest.WithButton(ControllerButton.SecondaryButton));Check(game.selected==selected,"Right B selects previous slot");yield return Send(right,Rest);
  var suction=Rest;suction.trigger=1;yield return Send(right,suction);Check(vac.IsSuctioning,"Right trigger activates vacuum");yield return Send(right,Rest);Check(!vac.IsSuctioning,"Releasing right trigger stops suction");
  vac.transform.SetParent(null,true);vac.transform.SetPositionAndRotation(new Vector3(0,1.6f,0),Quaternion.identity);foreach(var s in game.slots)s.count=0;game.Add(RanchItemKind.Carrot);game.Select(0);var fire=Rest;fire.grip=1;yield return Send(right,fire);Check(game.slots[0].count==0&&FindObjectsByType<RanchItem>(FindObjectsSortMode.None).Any(i=>i.data.kind==RanchItemKind.Carrot),"Right grip ejects actual inventory object");yield return Send(right,Rest);
  yield return Send(right,suction);Check(vac.IsSuctioning,"Vacuum active before disconnect");InputSystem.RemoveDevice(right);yield return null;yield return null;Check(!vac.IsSuctioning&&!vr.Right.tracked,"Controller disconnect stops vacuum");right=Controller("RightHand");yield return Send(right,Rest);
  var start=vr.transform.position;var walk=Rest;walk.primary2DAxis=Vector2.up;yield return Send(left,walk);yield return new WaitForSeconds(.2f);yield return Send(left,Rest);Check(Vector3.Distance(Vector3.ProjectOnPlane(vr.transform.position-start,Vector3.up),Vector3.zero)>.1f,"Left joystick moves player through CharacterController");
  game.energy=80;yield return Send(left,walk.WithButton(ControllerButton.Primary2DAxisClick));yield return new WaitForSeconds(.2f);Check(game.energy<80,"Left joystick click sprints and spends energy");yield return Send(left,Rest);
  float angle=vr.transform.eulerAngles.y;var turn=Rest;turn.primary2DAxis=Vector2.right;yield return Send(right,turn);Check(Mathf.Abs(Mathf.DeltaAngle(angle,vr.transform.eulerAngles.y)-30)<.1f,"Right joystick snap turns 30 degrees");angle=vr.transform.eulerAngles.y;yield return new WaitForSeconds(.15f);Check(Mathf.Abs(Mathf.DeltaAngle(angle,vr.transform.eulerAngles.y))<.1f,"Holding turn does not spin continuously");yield return Send(right,Rest);
  yield return new WaitForSeconds(.3f);float height=vr.transform.position.y;yield return Send(left,Rest.WithButton(ControllerButton.PrimaryButton));yield return new WaitForSeconds(.1f);Check(vr.transform.position.y>height+.1f,"Left X jumps when grounded");yield return Send(left,Rest);
  var originalStation=game.upgradeStation.position;game.upgradeStation.position=vr.head.position;game.coins=250;game.upgrade=0;yield return Send(left,Rest.WithButton(ControllerButton.GripButton));Check(game.coins==100&&game.upgrade==1,"Left grip purchases nearby upgrade");game.upgradeStation.position=originalStation;yield return Send(left,Rest);
  yield return Send(left,Rest.WithButton(ControllerButton.SecondaryButton));Check(File.Exists(game.SavePath),"Left Y saves game to disk");yield return Send(left,Rest);game.coins=999;var load=Rest.WithButton(ControllerButton.SecondaryButton);load.trigger=1;yield return Send(left,load);Check(game.coins==999,"Load chord does not overwrite saved game or load immediately");yield return new WaitForSeconds(2.1f);Check(game.coins==100,"Holding left trigger plus Y loads saved game");yield return Send(left,Rest);
  vr.useSimulator=false;yield return null;yield return null;Check(!RanchVRControls.Active,"No headset returns to desktop mode");
 }
}
}
#endif
