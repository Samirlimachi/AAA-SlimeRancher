using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using System.Collections.Generic;
namespace SlimeRancherVR {
[RequireComponent(typeof(Rigidbody),typeof(XRGrabInteractable))]
[DefaultExecutionOrder(10500)]
public sealed class VacuumPickup : MonoBehaviour {
 public Transform leftHand,rightHand;
 public bool IsHeld=>grab&&grab.isSelected;
 public bool IsLeftHand=>grab&&grab.isSelected&&leftHand&&grab.interactorsSelecting[0].transform.IsChildOf(leftHand);
 XRGrabInteractable grab;Rigidbody physicsBody;SlimeVacuum vacuum;Vector3 home;
 Collider[] own,playerColliders;
 // VR grab is a toggle: press grip to take the vacuum, press it again to drop it.
 readonly Dictionary<XRBaseInputInteractor,XRBaseInputInteractor.InputTriggerType> originalTriggers=new();
 readonly List<XRBaseInputInteractor> pendingRestore=new();
 bool promptShown;
 void Awake(){grab=GetComponent<XRGrabInteractable>();physicsBody=GetComponent<Rigidbody>();vacuum=GetComponent<SlimeVacuum>();home=transform.position;own=GetComponentsInChildren<Collider>();playerColliders=vacuum.playerRoot.GetComponentsInChildren<Collider>();grab.selectEntered.AddListener(OnGrab);grab.selectExited.AddListener(OnRelease);}
 void OnGrab(SelectEnterEventArgs args){
  if(args.interactorObject is XRBaseInputInteractor input){pendingRestore.Remove(input);if(!originalTriggers.ContainsKey(input))originalTriggers[input]=input.selectActionTrigger;input.selectActionTrigger=XRBaseInputInteractor.InputTriggerType.Toggle;}
  IgnorePlayer(true);vacuum.SetSuction(false);RanchGame.Instance?.Notify("Gatillo de esta mano: aspirar. Otro gatillo: lanzar. Grip otra vez: soltar.");}
 void OnRelease(SelectExitEventArgs args){if(args.interactorObject is XRBaseInputInteractor input&&originalTriggers.ContainsKey(input)&&!pendingRestore.Contains(input))pendingRestore.Add(input);vacuum.SetSuction(false);IgnorePlayer(false);}
 // Restore the hand's normal grab mode only once the grip is let go, so the drop press cannot re-grab anything.
 void RestoreTriggers(bool force){for(int i=pendingRestore.Count-1;i>=0;i--){var input=pendingRestore[i];if(input&&!force&&input.selectInput.ReadIsPerformed())continue;if(input)input.selectActionTrigger=originalTriggers[input];originalTriggers.Remove(input);pendingRestore.RemoveAt(i);}}
 void ShowGrabPromptOnce(){
  if(promptShown||IsHeld||!Camera.main)return;
  if(Vector3.Distance(Camera.main.transform.position,transform.position)>1.5f)return;
  promptShown=true;RanchGame.Instance?.Notify("Presiona el GRIP (botón lateral) para agarrar la aspiradora. Presiónalo otra vez para soltarla.");}
 void IgnorePlayer(bool value){if(own==null||playerColliders==null)return;foreach(var a in own)if(a)foreach(var b in playerColliders)if(b&&a!=b)Physics.IgnoreCollision(a,b,value);}
 void Update(){
  RestoreTriggers(false);ShowGrabPromptOnce();
  if(!IsHeld&&transform.position.y<-6){physicsBody.position=home;physicsBody.linearVelocity=Vector3.zero;physicsBody.angularVelocity=Vector3.zero;}
  if(!IsHeld)vacuum.SetSuction(false);
 }
 void OnDisable(){pendingRestore.Clear();foreach(var input in originalTriggers.Keys)pendingRestore.Add(input);RestoreTriggers(true);if(vacuum)vacuum.SetSuction(false);IgnorePlayer(false);}
 void OnDestroy(){if(grab){grab.selectEntered.RemoveListener(OnGrab);grab.selectExited.RemoveListener(OnRelease);}}
}
}
