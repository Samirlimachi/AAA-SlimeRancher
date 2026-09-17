using UnityEngine;
using UnityEngine.InputSystem;
namespace SlimeRancherVR {
public sealed class BeatrixShowcase : MonoBehaviour {
 public Animator animator;
 public Transform character;
 public string current="Reposo";
 public bool bonesVisible;
 LineRenderer[] lines;Transform[] joints;Renderer[] skin;float selectedAt;
 void Start(){
  skin=character.GetComponentsInChildren<SkinnedMeshRenderer>();var list=new System.Collections.Generic.List<Transform>();
  foreach(var t in character.GetComponentsInChildren<Transform>())if(t.parent&&t.parent.name!="Beatrix Body"&&t.name!="Beatrix"&&t.GetComponent<Renderer>()==null&&t.name!="Beatrix_Rig")list.Add(t);
  joints=list.ToArray();lines=new LineRenderer[joints.Length];
  var mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=new Color(.15f,1,.78f);
  for(int i=0;i<lines.Length;i++){var go=new GameObject("Bone guide");go.transform.SetParent(transform);var l=go.AddComponent<LineRenderer>();l.sharedMaterial=mat;l.positionCount=2;l.startWidth=l.endWidth=.007f;l.enabled=false;lines[i]=l;}
  Choose(0);
 }
 public void Choose(int state){
  selectedAt=Time.time;animator.ResetTrigger("Wave");animator.SetFloat("Speed",state==1?1.4f:0);animator.Play(state==2?"Wave":"Locomotion",0,0);current=state==0?"Reposo":state==1?"Caminar en el sitio":"Saludo";
 }
 void Update(){
  var k=Keyboard.current;if(k!=null){if(k.digit1Key.wasPressedThisFrame)Choose(0);if(k.digit2Key.wasPressedThisFrame)Choose(1);if(k.digit3Key.wasPressedThisFrame)Choose(2);if(k.bKey.wasPressedThisFrame)bonesVisible=!bonesVisible;
  float turn=(k.rightArrowKey.isPressed?1:0)-(k.leftArrowKey.isPressed?1:0);character.Rotate(0,turn*65*Time.deltaTime,0);}
  if(Time.time-selectedAt>.3f&&current=="Saludo"&&animator.GetCurrentAnimatorStateInfo(0).IsName("Locomotion"))current="Reposo";
 }
 void LateUpdate(){if(lines==null)return;foreach(var r in skin)r.enabled=!bonesVisible;for(int i=0;i<lines.Length;i++){lines[i].enabled=bonesVisible;if(bonesVisible){lines[i].SetPosition(0,joints[i].position);lines[i].SetPosition(1,joints[i].parent.position);}}}
 void OnGUI(){
  GUI.skin.button.fontSize=18;float sx=Screen.width/1280f,sy=Screen.height/720f;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(sx,sy,1));
  GUI.Box(new Rect(26,28,375,650),GUIContent.none);
  var title=new GUIStyle(GUI.skin.label){fontSize=40,fontStyle=FontStyle.Bold,wordWrap=true};var text=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true};var small=new GUIStyle(GUI.skin.label){fontSize=16,wordWrap=true};
  GUI.Label(new Rect(48,48,330,30),"AVANCE 01 / PERSONAJE",small);
  GUI.Label(new Rect(48,94,330,60),"BEATRIX",title);
  GUI.Label(new Rect(48,168,325,100),"Prototipo académico VR\nInspirado en Slime Rancher",text);
  GUI.Label(new Rect(48,273,320,100),"22 huesos\n3 animaciones base\nIntegración corporal en INICIO",text);
  if(GUI.Button(new Rect(48,397,320,42),"1  Reposo"))Choose(0);
  if(GUI.Button(new Rect(48,447,320,42),"2  Caminar"))Choose(1);
  if(GUI.Button(new Rect(48,497,320,42),"3  Saludar"))Choose(2);
  if(GUI.Button(new Rect(48,547,320,42),bonesVisible?"B  Ocultar huesos":"B  Mostrar huesos"))bonesVisible=!bonesVisible;
  GUI.Label(new Rect(48,613,325,50),"Flechas izquierda / derecha: girar",small);
  GUI.Label(new Rect(455,35,740,45),current,new GUIStyle(text){fontSize=25});
  GUI.Label(new Rect(455,654,765,50),"Versión de avance · Pesos y ropa en ajuste · Prueba en Quest pendiente",small);
 }
}
}

