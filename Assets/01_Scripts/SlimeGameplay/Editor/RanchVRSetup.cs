using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SlimeRancherVR;
[InitializeOnLoad]
public static class RanchVRSetup {
 const string Out="C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/";
 static RanchVRSetup(){EditorApplication.update+=Poll;}
 static void Poll(){if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(!EditorApplication.isPlayingOrWillChangePlaymode&&File.Exists("Temp/RanchVR.request")){string command=File.ReadAllText("Temp/RanchVR.request").Trim();File.Delete("Temp/RanchVR.request");try{
   var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/00_Scenes/INICIO.unity"){if(scene.isDirty)throw new Exception("La escena actual tiene cambios sin guardar.");scene=EditorSceneManager.OpenScene("Assets/00_Scenes/INICIO.unity");}
   var game=UnityEngine.Object.FindAnyObjectByType<RanchGame>();var body=UnityEngine.Object.FindAnyObjectByType<BeatrixVRBody>();if(!game||!body)throw new Exception("Falta el rancho o Beatrix");
   var vr=game.player.GetComponent<RanchVRControls>();if(!vr)vr=game.player.gameObject.AddComponent<RanchVRControls>();vr.game=game;vr.head=body.headTarget;vr.useSimulator=false;
   EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);File.WriteAllText(Out+"VR_Setup.txt","INICIO: VR controls installed. PC input retained.\n");
   if(command=="test"){SessionState.SetBool("VRControlValidation",true);EditorApplication.isPlaying=true;}
  }catch(Exception e){File.WriteAllText(Out+"VR_Setup.txt",e.ToString());}}
  if(EditorApplication.isPlaying&&SessionState.GetBool("VRControlValidation",false)&&!UnityEngine.Object.FindAnyObjectByType<RanchVRValidation>())new GameObject("VR input validation").AddComponent<RanchVRValidation>();
 }
}
