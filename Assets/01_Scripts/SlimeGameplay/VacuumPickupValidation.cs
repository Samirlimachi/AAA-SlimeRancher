#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEditor;
namespace SlimeRancherVR {
public sealed class VacuumPickupValidation : MonoBehaviour {
 string report="";SlimeVacuum vac;VacuumPickup pickup;RanchVRControls vr;
 void Check(bool ok,string label){if(!ok)throw new Exception(label);report+="PASS: "+label+"\n";}
 IEnumerator Start(){yield return new WaitForSeconds(.8f);var run=Run();while(true){object current;try{if(!run.MoveNext())break;current=run.Current;}catch(Exception e){Finish("FAIL: "+e);yield break;}yield return current;}Finish("Physical headset not tested.");}
 void Finish(string text){if(vr)vr.useSimulator=false;File.WriteAllText("C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/Pickup_Test.txt",report+text);SessionState.SetBool("PickupValidation",false);EditorApplication.isPlaying=false;}
 XRSimulatedController Device(string usage){var result=InputSystem.devices.OfType<XRSimulatedController>().FirstOrDefault(d=>d.usages.Any(u=>u.ToString()==usage));if(result==null){result=InputSystem.AddDevice<XRSimulatedController>();InputSystem.SetDeviceUsage(result,usage);}return result;}
 IEnumerator Run(){
  vac=FindAnyObjectByType<SlimeVacuum>();pickup=vac.GetComponent<VacuumPickup>();vr=FindAnyObjectByType<RanchVRControls>();var game=RanchGame.Instance;var grab=vac.GetComponent<XRGrabInteractable>();var rb=vac.GetComponent<Rigidbody>();
  Check(pickup&&grab&&rb,"Physics pickup and XR Grab components installed");Check(!game.autoSave&&!game.autoLoad,"Player save isolated from tests");Check(!vac.transform.IsChildOf(game.player)&&!pickup.IsHeld,"Vacuum starts on ground outside player hierarchy");Check(!rb.isKinematic&&rb.useGravity&&vac.transform.position.y<.6f,"Vacuum rests on floor with gravity");
  vac.SetSuction(true);Check(!vac.IsSuctioning&&!vac.Shoot(),"Unheld vacuum cannot aspirate or shoot");
  foreach(var hand in new[]{pickup.leftHand,pickup.rightHand})Check(hand.GetComponentsInChildren<Renderer>(true).Any(r=>!(r is LineRenderer)&&r.enabled&&r.gameObject.activeInHierarchy),hand.name+" has visible controller model");
  Check(pickup.PickUpDesktop()&&pickup.IsHeld,"Desktop can pick up nearby tool");yield return null;Check(rb.isKinematic,"Held desktop tool uses kinematic physics");pickup.DropDesktop();Check(!pickup.IsHeld&&!rb.isKinematic&&rb.useGravity,"Desktop drop restores gravity");yield return new WaitForSeconds(.6f);
  foreach(var mono in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))if(mono.GetType().Name=="XRInteractionSimulator"||mono.GetType().Name=="XRDeviceSimulator")mono.enabled=false;
  FindAnyObjectByType<BeatrixDesktopPreview>().enabled=false;FindAnyObjectByType<BeatrixVRBody>().enabled=false;foreach(var driver in game.player.GetComponentsInChildren<TrackedPoseDriver>())driver.enabled=false;
  var right=Device("RightHand");var left=Device("LeftHand");var rest=new XRSimulatedControllerState{isTracked=true,trackingState=3,deviceRotation=Quaternion.identity};vr.useSimulator=true;InputSystem.QueueStateEvent(right,rest);InputSystem.QueueStateEvent(left,rest);yield return null;yield return null;
  var interactor=pickup.rightHand.GetComponentsInChildren<XRBaseInputInteractor>(true).First(i=>i is IXRSelectInteractor&&i.enabled&&i.gameObject.activeInHierarchy);
  foreach(var other in game.player.GetComponentsInChildren<XRBaseInputInteractor>(true))if(other!=interactor)other.enabled=false;
  interactor.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;interactor.selectInput.QueueManualState(true,1);pickup.rightHand.SetPositionAndRotation(vac.transform.position,Quaternion.identity);yield return null;yield return null;
  var manager=FindAnyObjectByType<XRInteractionManager>();if(!grab.isSelected)manager.SelectEnter((IXRSelectInteractor)interactor,(IXRSelectInteractable)grab);yield return null;yield return null;
  Check(pickup.IsHeld&&!pickup.IsLeftHand,"XRI selection grabs tool with right controller");var previous=vac.transform.position;pickup.rightHand.position+=Vector3.up*.3f;yield return new WaitForSeconds(.1f);Check(Vector3.Distance(previous,vac.transform.position)>.1f,"Held tool follows controller pose");
  var suction=rest;suction.trigger=1;InputSystem.QueueStateEvent(right,suction);yield return null;yield return null;Check(vac.IsSuctioning,"Holding-hand trigger aspirates");InputSystem.QueueStateEvent(right,rest);yield return null;yield return null;
  // Clear test firing space, while keeping the actual XR selection active.
  pickup.rightHand.position=new Vector3(0,1.7f,0);yield return new WaitForSeconds(.1f);foreach(var slot in game.slots)slot.count=0;game.Add(RanchItemKind.Carrot);game.Select(0);var shoot=rest;shoot.trigger=1;InputSystem.QueueStateEvent(left,shoot);yield return null;yield return null;Check(game.slots[0].count==0,"Opposite-hand trigger launches without releasing grip");InputSystem.QueueStateEvent(left,rest);
  interactor.selectInput.QueueManualState(false,0);yield return null;yield return null;yield return new WaitForFixedUpdate();Check(!pickup.IsHeld,"Releasing grip selection drops tool");Check(!rb.isKinematic&&rb.useGravity&&!vac.IsSuctioning,"Drop restores physics and stops suction");float y=vac.transform.position.y;yield return new WaitForSeconds(.2f);Check(vac.transform.position.y<y,"Released tool falls under gravity");
  interactor.selectInput.QueueManualState(true,1);pickup.rightHand.position=vac.transform.position;yield return null;yield return null;if(!grab.isSelected)manager.SelectEnter((IXRSelectInteractor)interactor,(IXRSelectInteractable)grab);yield return null;Check(pickup.IsHeld,"Dropped tool can be grabbed again");
  interactor.selectInput.QueueManualState(false,0);yield return null;yield return null;interactor.enabled=false;
  var leftInteractor=pickup.leftHand.GetComponentsInChildren<XRBaseInputInteractor>(true).First(i=>i.gameObject.activeInHierarchy);leftInteractor.enabled=true;leftInteractor.selectInput.inputSourceMode=XRInputButtonReader.InputSourceMode.ManualValue;leftInteractor.selectInput.QueueManualState(true,1);pickup.leftHand.SetPositionAndRotation(vac.transform.position,Quaternion.identity);yield return null;yield return null;if(!grab.isSelected)manager.SelectEnter((IXRSelectInteractor)leftInteractor,(IXRSelectInteractable)grab);yield return null;
  Check(pickup.IsHeld&&pickup.IsLeftHand,"Left controller can also hold the vacuum");InputSystem.QueueStateEvent(left,suction);yield return null;yield return null;Check(vac.IsSuctioning,"Suction follows left-hand ownership");InputSystem.QueueStateEvent(left,rest);leftInteractor.selectInput.QueueManualState(false,0);yield return null;yield return null;Check(!pickup.IsHeld&&!vac.IsSuctioning,"Left-hand release stops vacuum and drops it");
 }
}
}
#endif
