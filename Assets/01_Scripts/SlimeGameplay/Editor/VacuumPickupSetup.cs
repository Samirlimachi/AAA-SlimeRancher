using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using SlimeRancherVR;
[InitializeOnLoad]
public static class VacuumPickupSetup {
 const string Out="C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/";
 static VacuumPickupSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/VacuumPickup.request")){string command=File.ReadAllText("Temp/VacuumPickup.request").Trim();File.Delete("Temp/VacuumPickup.request");try{Install();if(command=="test"){SessionState.SetBool("PickupValidation",true);EditorApplication.isPlaying=true;}}catch(Exception e){File.WriteAllText(Out+"Pickup_Setup.txt",e.ToString());Debug.LogException(e);}}
  if(EditorApplication.isPlaying&&SessionState.GetBool("PickupValidation",false)&&!UnityEngine.Object.FindAnyObjectByType<VacuumPickupValidation>())new GameObject("Pickup validation").AddComponent<VacuumPickupValidation>();
 }
 static void Install(){var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/00_Scenes/INICIO.unity"){if(scene.isDirty)throw new Exception("Escena actual sin guardar");scene=EditorSceneManager.OpenScene("Assets/00_Scenes/INICIO.unity");}
  var vac=UnityEngine.Object.FindAnyObjectByType<SlimeVacuum>();var body=UnityEngine.Object.FindAnyObjectByType<BeatrixVRBody>();if(!vac||!body)throw new Exception("Falta aspiradora o jugador");
  if(!vac.GetComponent<VacuumPickup>()){
   if(PrefabUtility.IsPartOfPrefabInstance(vac.gameObject)){var outer=PrefabUtility.GetOutermostPrefabInstanceRoot(vac.gameObject);PrefabUtility.UnpackPrefabInstance(outer,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);}
   vac.transform.SetParent(null,true);vac.transform.SetPositionAndRotation(body.trackingOrigin.position+new Vector3(.55f,.32f,1.1f),Quaternion.Euler(0,-30,0));
   var rb=vac.gameObject.AddComponent<Rigidbody>();rb.mass=1;rb.interpolation=RigidbodyInterpolation.Interpolate;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;
   var col=vac.gameObject.AddComponent<BoxCollider>();col.center=new Vector3(0,.075f,.05f);col.size=new Vector3(.35f,.29f,.6f);
   var attach=new GameObject("Empuñadura").transform;attach.SetParent(vac.transform,false);attach.localPosition=new Vector3(0,.035f,-.16f);
   var grab=vac.gameObject.AddComponent<XRGrabInteractable>();grab.attachTransform=attach;grab.useDynamicAttach=false;grab.movementType=XRBaseInteractable.MovementType.Kinematic;grab.throwOnDetach=true;grab.throwVelocityScale=1;grab.selectMode=InteractableSelectMode.Single;grab.colliders.Clear();grab.colliders.Add(col);
   var pickup=vac.gameObject.AddComponent<VacuumPickup>();pickup.leftHand=body.leftTarget;pickup.rightHand=body.rightTarget;
  }
  string report="Aspiradora física en el suelo; agarre XR con grip; F para recoger/soltar en PC.\n";
  foreach(var hand in new[]{body.leftTarget,body.rightTarget}){
   foreach(var renderer in hand.GetComponentsInChildren<Renderer>(true)){if(renderer is LineRenderer||renderer is TrailRenderer||renderer is ParticleSystemRenderer)continue;renderer.enabled=true;for(var t=renderer.transform;t&&t!=hand;t=t.parent)t.gameObject.SetActive(true);report+="Visible: "+hand.name+"/"+renderer.name+"\n";}
   foreach(var input in hand.GetComponentsInChildren<XRBaseInputInteractor>(true))input.selectActionTrigger=XRBaseInputInteractor.InputTriggerType.StateChange;
  }
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Out+"Pickup_Setup.txt",report);
 }
}
