#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections;
using UnityEngine;
using UnityEditor;
namespace SlimeRancherVR {
public sealed class VacuumValidation : MonoBehaviour {
 const string Out="C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/";
 SlimeVacuum vacuum;string report="";
 void Check(bool pass,string label){if(!pass)throw new Exception(label);report+="PASS: "+label+"\n";}
 IEnumerator Start(){
  yield return new WaitForSeconds(.5f);
  var routine=Run();
  while(true){object current;try{if(!routine.MoveNext())break;current=routine.Current;}catch(Exception e){File.WriteAllText(Out+"Vacuum_Test.txt",report+"FAIL: "+e);SessionState.SetBool("VacuumValidation",false);EditorApplication.isPlaying=false;yield break;}yield return current;}
  File.WriteAllText(Out+"Vacuum_Test.txt",report+"\nReal Quest hardware not tested.\n");SessionState.SetBool("VacuumValidation",false);EditorApplication.isPlaying=false;
 }
 IEnumerator Run(){
  vacuum=FindAnyObjectByType<SlimeVacuum>();Check(vacuum!=null,"Vacuum in INICIO");
  var body=FindAnyObjectByType<BeatrixVRBody>();Check(body.GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial.GetTexture("_BaseMap")!=null,"Beatrix original color map assigned");
  ScreenCapture.CaptureScreenshot(Out+"INICIO_Aspiradora.png");yield return new WaitForEndOfFrame();
  var preview=FindAnyObjectByType<BeatrixDesktopPreview>();preview.enabled=false;body.enabled=false;
  vacuum.transform.SetParent(null,true);vacuum.transform.SetPositionAndRotation(new Vector3(0,1,0),Quaternion.identity);vacuum.ReadInput=false;
  var data=Instantiate(vacuum.data);vacuum.data=data;foreach(var s in vacuum.demoSlimes){s.data=data;s.gameObject.SetActive(false);}
  var slime=vacuum.demoSlimes[0];slime.gameObject.SetActive(true);slime.Body.useGravity=false;
  slime.transform.position=vacuum.muzzle.position+Vector3.forward*2;Physics.SyncTransforms();
  Check(vacuum.CanPull(slime),"Reachable slime accepted");
  slime.transform.position=vacuum.muzzle.position-Vector3.forward*2;Physics.SyncTransforms();Check(!vacuum.CanPull(slime),"Slime behind tool rejected");
  slime.transform.position=vacuum.muzzle.position+Vector3.forward*8;Physics.SyncTransforms();Check(!vacuum.CanPull(slime),"Slime outside range rejected");
  slime.transform.position=vacuum.muzzle.position+Vector3.forward*2;Physics.SyncTransforms();
  var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=vacuum.muzzle.position+Vector3.forward;wall.transform.localScale=new Vector3(1,1,.2f);Physics.SyncTransforms();Check(!vacuum.CanPull(slime),"Wall blocks suction");Destroy(wall);yield return new WaitForFixedUpdate();
  vacuum.SetSuction(true);
  float previous=Vector3.Distance(slime.transform.position,vacuum.muzzle.position);
  for(int i=0;i<8;i++)yield return new WaitForFixedUpdate();
  Check(Vector3.Distance(slime.transform.position,vacuum.muzzle.position)<previous,"Physics attracts slime toward muzzle");
  vacuum.SetSuction(false);yield return new WaitForSeconds(.18f);Check(slime.Body.useGravity,"Release restores gravity");
  slime.transform.position=vacuum.muzzle.position+Vector3.forward*1.7f;slime.Body.linearVelocity=Vector3.zero;vacuum.SetSuction(true);
  for(int i=0;i<150&&vacuum.Count==0;i++)yield return new WaitForFixedUpdate();
  Check(vacuum.Count==1&&slime.Captured&&!slime.gameObject.activeSelf,"Capture increments inventory and removes slime");
  for(int i=0;i<10;i++)yield return new WaitForFixedUpdate();Check(vacuum.Count==1,"Captured slime cannot count twice");
  data.capacity=1;var second=vacuum.demoSlimes[1];second.gameObject.SetActive(true);second.transform.position=vacuum.muzzle.position+Vector3.forward;Physics.SyncTransforms();Check(!vacuum.CanPull(second),"Full inventory rejects new captures");
  vacuum.ResetDemo();yield return new WaitForFixedUpdate();Check(vacuum.Count==0&&Array.TrueForAll(vacuum.demoSlimes,s=>s.gameObject.activeSelf&&!s.Captured),"Reset restores all eight slimes and clears inventory");
  Destroy(data);
 }
}
}
#endif

