using System.IO;
using UnityEngine;
using UnityEditor;
using SlimeRancherVR;
[InitializeOnLoad]
public static class VacuumTestRunner {
 static VacuumTestRunner(){EditorApplication.update+=Poll;}
 static void Poll(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(!EditorApplication.isPlaying&&File.Exists("Temp/VacuumTest.request")){File.Delete("Temp/VacuumTest.request");SessionState.SetBool("VacuumValidation",true);EditorApplication.isPlaying=true;return;}
  if(EditorApplication.isPlaying&&SessionState.GetBool("VacuumValidation",false)&&!Object.FindAnyObjectByType<VacuumValidation>())new GameObject("Vacuum gameplay validation").AddComponent<VacuumValidation>();
 }
}
