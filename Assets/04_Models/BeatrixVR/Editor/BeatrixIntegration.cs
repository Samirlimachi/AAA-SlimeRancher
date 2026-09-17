using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;
using SlimeRancherVR;
[InitializeOnLoad]
public static class BeatrixIntegration {
 const string Base="Assets/04_Models/BeatrixVR/";
 const string Request="Temp/BeatrixIntegration.request";
 const string Output="C:/Users/samir/Documents/Codex/2026-09-14/qui-2/outputs/";
 static double next;static int phase,frames;static BeatrixVRBody tested;static Vector3 lastHand;static float maxMovement;
 static BeatrixIntegration(){EditorApplication.update+=Poll;if(SessionState.GetBool("BeatrixTest",false))phase=1;}
 static void Poll(){
  if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
  if(phase>0){TestTick();return;}
  if(EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+1;
  if(!File.Exists(Request))return;
  string cmd=File.ReadAllText(Request).Trim();File.Delete(Request);
  try{if(cmd=="install")Install();else if(cmd=="test"){SessionState.SetBool("BeatrixTest",true);phase=1;frames=0;EditorApplication.isPlaying=true;}}
  catch(Exception e){File.WriteAllText(Output+"Unity_Integration_Error.txt",e.ToString());Debug.LogException(e);}
 }
 [MenuItem("Beatrix/Integrar cuerpo VR")]
 public static void Install(){
  if(EditorApplication.isPlaying)throw new Exception("Sal del modo Play antes de integrar.");
  var scene=SceneManager.GetActiveScene();
  if(scene.path!="Assets/00_Scenes/INICIO.unity")throw new Exception("Abre INICIO para integrar el cuerpo.");
  var existing=UnityEngine.Object.FindFirstObjectByType<BeatrixVRBody>();
  if(existing)throw new Exception("Beatrix ya está integrada; no se creará otro jugador.");
  AssetDatabase.ImportAsset(Base+"Models/Beatrix_Rig.fbx",ImportAssetOptions.ForceSynchronousImport);
  var importer=(ModelImporter)AssetImporter.GetAtPath(Base+"Models/Beatrix_Rig.fbx");
  importer.animationType=ModelImporterAnimationType.Generic;importer.importAnimation=true;importer.isReadable=true;importer.optimizeGameObjects=false;
  importer.animationCompression=ModelImporterAnimationCompression.Off;
  var clips=importer.defaultClipAnimations;
  foreach(var c in clips){c.name=c.name.Contains("Idle")?"Beatrix_Idle":c.name.Contains("Walk")?"Beatrix_Walk_InPlace":"Beatrix_Wave";c.loopTime=!c.name.Contains("Wave");c.loopPose=c.loopTime;}
  importer.clipAnimations=clips;importer.SaveAndReimport();
  var animations=AssetDatabase.LoadAllAssetsAtPath(Base+"Models/Beatrix_Rig.fbx").OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__")).ToArray();
  var idle=animations.Single(c=>c.name=="Beatrix_Idle");var walk=animations.Single(c=>c.name=="Beatrix_Walk_InPlace");var wave=animations.Single(c=>c.name=="Beatrix_Wave");
  var ac=AnimatorController.CreateAnimatorControllerAtPath(Base+"Beatrix.controller");ac.AddParameter("Speed",AnimatorControllerParameterType.Float);ac.AddParameter("Wave",AnimatorControllerParameterType.Trigger);
  var sm=ac.layers[0].stateMachine;var locomotion=sm.AddState("Locomotion");sm.defaultState=locomotion;
  var tree=new BlendTree{name="Idle / Walk",blendType=BlendTreeType.Simple1D,blendParameter="Speed",useAutomaticThresholds=false};AssetDatabase.AddObjectToAsset(tree,ac);tree.AddChild(idle,0);tree.AddChild(walk,1.4f);locomotion.motion=tree;
  var waving=sm.AddState("Wave");waving.motion=wave;var toWave=sm.AddAnyStateTransition(waving);toWave.hasExitTime=false;toWave.duration=.15f;toWave.AddCondition(AnimatorConditionMode.If,0,"Wave");toWave.canTransitionToSelf=false;
  var back=waving.AddTransition(locomotion);back.hasExitTime=true;back.exitTime=1;back.duration=.2f;
  var model=AssetDatabase.LoadAssetAtPath<GameObject>(Base+"Models/Beatrix_Rig.fbx");
  var avatar=(GameObject)PrefabUtility.InstantiatePrefab(model);avatar.name="Beatrix Body";
  PrefabUtility.UnpackPrefabInstance(avatar,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
  var anim=avatar.GetComponent<Animator>();if(!anim)anim=avatar.AddComponent<Animator>();anim.runtimeAnimatorController=ac;anim.applyRootMotion=false;anim.cullingMode=AnimatorCullingMode.AlwaysAnimate;
  var body=avatar.AddComponent<BeatrixVRBody>();body.animator=anim;
  var skin=avatar.GetComponentInChildren<SkinnedMeshRenderer>();skin.quality=SkinQuality.Bone4;skin.updateWhenOffscreen=true;
  var shader=Shader.Find("Universal Render Pipeline/Lit");if(!shader)throw new Exception("No se encontró el shader URP Lit.");
  var materials=skin.sharedMaterials.Select((m,i)=>{var mat=new Material(shader){name="Beatrix Material "+i};mat.SetColor("_BaseColor",new Color(.72f,.65f,.58f));if(m&&m.mainTexture)mat.SetTexture("_BaseMap",m.mainTexture);AssetDatabase.CreateAsset(mat,Base+"BeatrixMaterial"+i+".mat");return mat;}).ToArray();skin.sharedMaterials=materials;
  SplitHead(skin,body);
  // Save a reusable visual prefab; scene references are attached below.
  PrefabUtility.SaveAsPrefabAsset(avatar,Base+"BeatrixBody.prefab");
  var xr=UnityEngine.Object.FindFirstObjectByType<XROrigin>();
  if(!xr){
   var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/XR Interaction Toolkit/3.5.1/Starter Assets/Prefabs/XR Origin (XR Rig).prefab");
   if(!source)throw new Exception("Falta el prefab XR Origin del proyecto.");
   var go=(GameObject)PrefabUtility.InstantiatePrefab(source);go.name="Beatrix XR Player";xr=go.GetComponent<XROrigin>();
  }
  xr.CameraYOffset=1.5f;xr.RequestedTrackingOriginMode=XROrigin.TrackingOriginMode.Floor;
  body.trackingOrigin=xr.transform;body.headTarget=xr.Camera.transform;
  var transforms=xr.GetComponentsInChildren<Transform>(true);
  body.leftTarget=transforms.Single(t=>t.name=="Left Controller");body.rightTarget=transforms.Single(t=>t.name=="Right Controller");
  avatar.transform.SetParent(xr.transform,false);
  body.headTarget.GetComponent<Camera>().nearClipPlane=.03f;
  // A monoscopic camera in INICIO would otherwise remain active beside the XR camera.
  foreach(var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))if(cam!=xr.Camera){cam.enabled=false;var al=cam.GetComponent<AudioListener>();if(al)al.enabled=false;}
  foreach(var target in new[]{body.leftTarget,body.rightTarget})foreach(var r in target.GetComponentsInChildren<Renderer>(true))if(!r.GetComponent<LineRenderer>())r.enabled=false;
  var preview=xr.gameObject.AddComponent<BeatrixDesktopPreview>();preview.body=body;preview.head=body.headTarget;preview.leftHand=body.leftTarget;preview.rightHand=body.rightTarget;
  if(!UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>())new GameObject("XR Interaction Manager").AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
  if(!UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Any(c=>!c.transform.IsChildOf(xr.transform))){
   var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Suelo de prueba VR";floor.transform.position=xr.transform.position;floor.transform.localScale=Vector3.one*2;
   var fm=new Material(shader){name="Suelo VR"};fm.SetColor("_BaseColor",new Color(.21f,.32f,.29f));AssetDatabase.CreateAsset(fm,Base+"Floor.mat");floor.GetComponent<Renderer>().sharedMaterial=fm;
  }
  PrefabUtility.SaveAsPrefabAsset(xr.gameObject,Base+"BeatrixXRPlayer.prefab");
  EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
  var builds=EditorBuildSettings.scenes.ToList();builds.RemoveAll(s=>s.path==scene.path);builds.Insert(0,new EditorBuildSettingsScene(scene.path,true));EditorBuildSettings.scenes=builds.ToArray();
  AssetDatabase.SaveAssets();Selection.activeGameObject=avatar;SceneView.lastActiveSceneView?.FrameSelected();
  File.WriteAllText(Output+"Unity_Integration.txt","INTEGRATED: INICIO\nPrefab: "+Base+"BeatrixXRPlayer.prefab\nClips: "+string.Join(", ",animations.Select(c=>c.name))+"\nBones: "+skin.bones.Length+"\nBounds: "+skin.bounds.size+"\nCamera: "+xr.Camera.name+"\n");
  Debug.Log("BEATRIX: integración completada en INICIO.");
 }
 static void SplitHead(SkinnedMeshRenderer skin,BeatrixVRBody body){
  var original=skin.sharedMesh;var weights=original.boneWeights;var bones=skin.bones;
  bool IsHead(int i){var w=weights[i];float total=0;int[] ids={w.boneIndex0,w.boneIndex1,w.boneIndex2,w.boneIndex3};float[] vs={w.weight0,w.weight1,w.weight2,w.weight3};for(int j=0;j<4;j++)if(ids[j]<bones.Length&&(bones[ids[j]].name=="Head"||bones[ids[j]].name=="Neck"))total+=vs[j];return total>.5f;}
  var bodyMesh=UnityEngine.Object.Instantiate(original);bodyMesh.name="Beatrix First Person Body";var headMesh=UnityEngine.Object.Instantiate(original);headMesh.name="Beatrix Head";
  int headTris=0;
  for(int sub=0;sub<original.subMeshCount;sub++){
   var b=new List<int>();var h=new List<int>();var tri=original.GetTriangles(sub);
   for(int i=0;i<tri.Length;i+=3){var dest=(IsHead(tri[i])||IsHead(tri[i+1])||IsHead(tri[i+2]))?h:b;dest.Add(tri[i]);dest.Add(tri[i+1]);dest.Add(tri[i+2]);}
   bodyMesh.SetTriangles(b,sub);headMesh.SetTriangles(h,sub);headTris+=h.Count/3;
  }
  if(headTris==0)throw new Exception("No se pudieron separar los triángulos de la cabeza.");
  AssetDatabase.CreateAsset(bodyMesh,Base+"BodyMesh.asset");AssetDatabase.CreateAsset(headMesh,Base+"HeadMesh.asset");skin.sharedMesh=bodyMesh;
  var headGO=new GameObject("Beatrix Head (hidden in first person)");headGO.transform.SetParent(skin.transform.parent,false);headGO.transform.localPosition=skin.transform.localPosition;headGO.transform.localRotation=skin.transform.localRotation;headGO.transform.localScale=skin.transform.localScale;
  var hr=headGO.AddComponent<SkinnedMeshRenderer>();hr.sharedMesh=headMesh;hr.bones=bones;hr.rootBone=skin.rootBone;hr.sharedMaterials=skin.sharedMaterials;hr.localBounds=skin.localBounds;hr.updateWhenOffscreen=true;hr.quality=SkinQuality.Bone4;body.headRenderer=hr;
 }
 static void TestTick(){
  try{
   if(!EditorApplication.isPlaying)return;
   frames++;
   if(phase==1&&frames>100){
    tested=UnityEngine.Object.FindFirstObjectByType<BeatrixVRBody>();if(!tested||!tested.enabled)throw new Exception("No hay cuerpo VR activo.");
    if(!tested.animator.isInitialized)throw new Exception("Animator no inicializado.");
    lastHand=tested.Bone("Hand.R").position;maxMovement=0;frames=0;phase=2;
   }else if(phase==2){
    tested.trackingOrigin.position+=tested.trackingOrigin.forward*.01f;
    maxMovement=Mathf.Max(maxMovement,Vector3.Distance(lastHand,tested.Bone("Hand.R").position));
    if(frames>80){
     if(maxMovement<.1f)throw new Exception("El cuerpo no acompaña al jugador.");
     var preview=tested.trackingOrigin.GetComponent<BeatrixDesktopPreview>();preview.enabled=false;tested.previewTracking=true;
     tested.leftTarget.position=tested.Bone("UpperArm.L").position+tested.transform.forward*.3f-tested.transform.right*.12f;
     tested.rightTarget.position=tested.Bone("UpperArm.R").position+tested.transform.forward*.3f+tested.transform.right*.12f;
     tested.ApplyTracking();
     float le=Vector3.Distance(tested.Bone("Hand.L").position,tested.leftTarget.position),re=Vector3.Distance(tested.Bone("Hand.R").position,tested.rightTarget.position);
     if(le>.025f||re>.025f)throw new Exception("Error IK de manos: "+le+", "+re);
     if(tested.headRenderer.shadowCastingMode!=UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly)throw new Exception("La cabeza tapa la cámara.");
     var cams=UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Count(c=>c.enabled);
     File.WriteAllText(Output+"Unity_PlayMode_Test.txt","PASS\nAnimator initialized\nBody follows player: "+maxMovement+" m\nLeft hand IK error: "+le+" m\nRight hand IK error: "+re+" m\nHead hidden: true\nActive cameras: "+cams+"\nSpeed: "+tested.MeasuredSpeed+"\nHardware VR: not tested\n");
     Capture();SessionState.SetBool("BeatrixTest",false);phase=0;EditorApplication.isPlaying=false;
    }
   }
  }catch(Exception e){File.WriteAllText(Output+"Unity_Test_Error.txt",e.ToString());Debug.LogException(e);SessionState.SetBool("BeatrixTest",false);phase=0;EditorApplication.isPlaying=false;}
 }
 static void Capture(){
  tested.headRenderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;
  var go=new GameObject("Verification camera");var cam=go.AddComponent<Camera>();cam.transform.position=tested.transform.position+tested.transform.forward*2.6f+Vector3.up*1.1f;cam.transform.LookAt(tested.transform.position+Vector3.up*.85f);cam.nearClipPlane=.03f;
  var rt=new RenderTexture(900,900,24);cam.targetTexture=rt;cam.Render();var old=RenderTexture.active;RenderTexture.active=rt;var tex=new Texture2D(900,900,TextureFormat.RGB24,false);tex.ReadPixels(new Rect(0,0,900,900),0,0);tex.Apply();File.WriteAllBytes(Output+"Beatrix_Unity.png",tex.EncodeToPNG());RenderTexture.active=old;cam.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(go);UnityEngine.Object.Destroy(tex);UnityEngine.Object.Destroy(rt);
 }
}

