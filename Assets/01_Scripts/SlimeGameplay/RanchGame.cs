using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace SlimeRancherVR {
[Serializable] public sealed class RanchSlot { public RanchItemKind kind; public int count; }
[Serializable] public sealed class RanchWorldSave { public RanchItemKind kind; public Vector3 position; public Quaternion rotation; public Vector3 velocity; public float hunger; }
[Serializable] public sealed class RanchSave { public int version=1,coins,selected,upgrade,water; public float clock,health,energy,garden; public Vector3 player; public float yaw; public RanchSlot[] slots; public List<RanchWorldSave> items=new List<RanchWorldSave>(); }
[DefaultExecutionOrder(-100)]
public sealed class RanchGame : MonoBehaviour {
 public static RanchGame Instance {get;private set;}
 public RanchItemData[] catalog;
 public Transform player,worldRoot,upgradeStation;
 public Light sun;
 public RanchGarden garden;
 public RanchSlot[] slots={new RanchSlot(),new RanchSlot(),new RanchSlot(),new RanchSlot()};
 public int selected,coins=250,upgrade,water;
 public float clock=8*60,health=100,energy=100;
 public bool autoSave=true,autoLoad=true;
 public bool installLegacyVRControls=true, showDesktopHUD=true;
 public string saveName="INICIO_rancho_v1.json";
 public string Message {get;private set;}="Recoge zanahorias, alimenta slimes y vende sus plorts.";
 public float MessageUntil {get;private set;}=20;
 public string SavePath=>Path.Combine(Application.persistentDataPath,saveName);
 float nextSave=60,nextDamage; bool sprinting,lastInteract;
 Vector3 start;
 void Awake(){Instance=this;start=player?player.position:Vector3.zero;
 #if UNITY_EDITOR
 if((UnityEditor.SessionState.GetBool("RanchValidation",false)||UnityEditor.SessionState.GetBool("VRControlValidation",false)||UnityEditor.SessionState.GetBool("PickupValidation",false))){autoSave=false;autoLoad=false;saveName="INICIO_validation_only.json";}
 #endif
 }
 void Start(){if(installLegacyVRControls&&player&&!player.GetComponent<RanchVRControls>()){var vr=player.gameObject.AddComponent<RanchVRControls>();vr.game=this;}if(autoLoad&&File.Exists(SavePath))LoadGame();}
 public RanchItemData Data(RanchItemKind kind){foreach(var d in catalog)if(d&&d.kind==kind)return d;return null;}
 public int Limit(RanchItemKind kind)=>Data(kind).stackLimit+(upgrade>0?10:0);
 public bool CanStore(RanchItemKind kind){foreach(var s in slots)if(s.count==0||(s.kind==kind&&s.count<Limit(kind)))return true;return false;}
 public bool Add(RanchItemKind kind){if(!Data(kind))return false;foreach(var s in slots)if(s.count>0&&s.kind==kind&&s.count<Limit(kind)){s.count++;return true;}foreach(var s in slots)if(s.count==0){s.kind=kind;s.count=1;return true;}Notify("Inventario lleno: lanza o vende algún objeto.");return false;}
 public bool Collect(RanchItem item){if(!item||item.Consumed||!item.data||!CanStore(item.data.kind))return false;var kind=item.data.kind;if(!item.Consume())return false;Add(kind);Notify(Data(kind).itemName+" recogido");return true;}
 public RanchItem Spawn(RanchItemKind kind,Vector3 point,Quaternion rotation){var d=Data(kind);if(!d||!d.prefab)throw new InvalidOperationException("Falta prefab: "+kind);var go=Instantiate(d.prefab,point,rotation,worldRoot);go.name=d.itemName;var item=go.GetComponent<RanchItem>();item.data=d;item.Place(point,rotation);return item;}
 public void Select(int index){selected=(index+slots.Length)%slots.Length;}
 public void Notify(string value){Message=value;MessageUntil=Time.time+5;}
 public bool Sprint(bool wanted,float dt){sprinting=wanted&&energy>1;if(sprinting)energy=Mathf.Max(0,energy-18*dt);return sprinting;}
 public void Damage(float amount){if(Time.time<nextDamage)return;nextDamage=Time.time+1;health=Mathf.Max(0,health-amount);Notify("¡Cuidado con el agua profunda!");if(health<=0)Respawn();}
 public void Respawn(){MovePlayer(start,0);health=100;energy=100;clock+=60;Notify("De vuelta en el rancho. Ha pasado una hora.");}
 void MovePlayer(Vector3 point,float yaw){if(!player)return;var cc=player.GetComponent<CharacterController>();bool enabled=cc&&cc.enabled;if(cc)cc.enabled=false;player.SetPositionAndRotation(point,Quaternion.Euler(0,yaw,0));if(cc)cc.enabled=enabled;}
 public bool BuyUpgrade(){if(upgrade>0){Notify("Depósitos mejorados: 30 objetos por ranura.");return false;}if(coins<150){Notify("Necesitas 150 monedas para mejorar los depósitos.");return false;}coins-=150;upgrade=1;Notify("¡Mejora comprada! 30 objetos por ranura.");return true;}
 void Update(){
  clock+=Time.deltaTime; // One real second is one game minute.
  if(!sprinting)energy=Mathf.Min(100,energy+12*Time.deltaTime);sprinting=false;
  if(player&&player.position.y<-4)Respawn();
  if(sun){float hour=(clock%1440)/60;sun.transform.rotation=Quaternion.Euler(hour*15-90,-30,0);sun.intensity=Mathf.Lerp(.12f,1.1f,Mathf.Clamp01(Mathf.Sin((hour-6)/12*Mathf.PI)));RenderSettings.ambientLight=Color.Lerp(new Color(.18f,.23f,.35f),new Color(.5f,.52f,.55f),sun.intensity/1.1f);}
  bool desktop=!RanchVRControls.Active&&!BeatrixVRBody.Tracked(XRNode.Head);var k=Keyboard.current;
  if(desktop&&k!=null){if(k.digit1Key.wasPressedThisFrame)Select(0);if(k.digit2Key.wasPressedThisFrame)Select(1);if(k.digit3Key.wasPressedThisFrame)Select(2);if(k.digit4Key.wasPressedThisFrame)Select(3);if(k.f5Key.wasPressedThisFrame)SaveGame();if(k.f9Key.wasPressedThisFrame)LoadGame();var m=Mouse.current;if(m!=null&&Mathf.Abs(m.scroll.ReadValue().y)>.1f)Select(selected+(m.scroll.ReadValue().y>0?-1:1));}
  var left=InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);left.TryGetFeatureValue(UnityEngine.XR.CommonUsages.gripButton,out bool grip);
  bool interact=desktop&&k!=null&&k.eKey.isPressed;
  if(interact&&!lastInteract&&upgradeStation&&player&&Vector3.Distance(player.position,upgradeStation.position)<2.5f)BuyUpgrade();lastInteract=interact;
  if(autoSave&&Time.unscaledTime>nextSave){SaveGame(false);nextSave=Time.unscaledTime+60;}
 }
 public bool SaveGame(bool announce=true){try{
  var s=new RanchSave{water=water,coins=coins,selected=selected,upgrade=upgrade,clock=clock,health=health,energy=energy,player=player.position,yaw=player.eulerAngles.y,slots=slots,garden=garden?garden.remaining:30};
  foreach(var item in FindObjectsByType<RanchItem>(FindObjectsSortMode.None))if(!item.Consumed&&item.data)s.items.Add(new RanchWorldSave{kind=item.data.kind,position=item.transform.position,rotation=item.transform.rotation,velocity=item.Body.linearVelocity,hunger=item.hunger});
  Directory.CreateDirectory(Application.persistentDataPath);string temp=SavePath+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(s,true));if(File.Exists(SavePath))File.Replace(temp,SavePath,SavePath+".bak");else File.Move(temp,SavePath);if(announce)Notify("Partida guardada");return true;
 }catch(Exception e){Notify("No se pudo guardar la partida");Debug.LogWarning(e.Message);return false;}}
 static bool Finite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
 static bool Valid(Vector3 v)=>Finite(v.x)&&Finite(v.y)&&Finite(v.z)&&v.sqrMagnitude<1000000;
 public bool LoadGame(){try{
  if(!File.Exists(SavePath)){Notify("Todavía no hay una partida guardada");return false;}
  var s=JsonUtility.FromJson<RanchSave>(File.ReadAllText(SavePath));
  if(s==null||s.version!=1||s.slots==null||s.slots.Length!=4||s.items==null||s.items.Count>1000||s.coins<0||s.water<0||s.water>30||s.upgrade<0||s.upgrade>1||s.selected<0||s.selected>3||!Valid(s.player)||!Finite(s.clock)||s.clock<0||!Finite(s.yaw)||!Finite(s.health)||s.health<0||s.health>100||!Finite(s.energy)||s.energy<0||s.energy>100||!Finite(s.garden))throw new InvalidDataException("Guardado no válido");
  foreach(var slot in s.slots)if(slot==null||!Data(slot.kind)||slot.count<0||slot.count>Data(slot.kind).stackLimit+s.upgrade*10)throw new InvalidDataException("Inventario no válido");
  foreach(var item in s.items)if(item==null||!Data(item.kind)||!Data(item.kind).prefab||!Valid(item.position)||!Valid(item.velocity)||!Finite(item.hunger)||item.hunger<0||!Finite(item.rotation.x)||!Finite(item.rotation.y)||!Finite(item.rotation.z)||!Finite(item.rotation.w))throw new InvalidDataException("Objetos no válidos");
  foreach(var item in FindObjectsByType<RanchItem>(FindObjectsSortMode.None)){item.gameObject.SetActive(false);Destroy(item.gameObject);}
  water=s.water;slots=s.slots;coins=s.coins;selected=s.selected;upgrade=s.upgrade;clock=s.clock;health=s.health;energy=s.energy;MovePlayer(s.player,s.yaw);if(garden)garden.remaining=s.garden;
  foreach(var state in s.items){var item=Spawn(state.kind,state.position,state.rotation);item.hunger=state.hunger;item.Body.linearVelocity=state.velocity;item.graceUntil=Time.time+.5f;}
  Physics.SyncTransforms();Notify("Partida cargada");return true;
 }catch(Exception e){Notify("No se pudo cargar: el estado actual se conserva");Debug.LogWarning(e.Message);return false;}}
 void OnApplicationPause(bool paused){if(paused&&autoSave)SaveGame(false);}
 void OnApplicationQuit(){if(autoSave)SaveGame(false);}
 void OnDestroy(){if(Instance==this)Instance=null;}
 void OnGUI(){
  if(!showDesktopHUD||RanchVRControls.Active||BeatrixVRBody.Tracked(XRNode.Head))return;
  var old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(Vector3.zero,Quaternion.identity,new Vector3(Screen.width/1280f,Screen.height/720f,1));
  var label=new GUIStyle(GUI.skin.label){fontSize=19,normal={textColor=Color.white}};var big=new GUIStyle(label){fontSize=27,fontStyle=FontStyle.Bold};var center=new GUIStyle(label){alignment=TextAnchor.MiddleCenter,wordWrap=true};var small=new GUIStyle(label){fontSize=15};
  Panel(new Rect(18,18,200,89),new Color(.08f,.12f,.18f,.8f));GUI.Label(new Rect(30,24,180,36),"Día "+(1+Mathf.FloorToInt(clock/1440)),big);int mins=Mathf.FloorToInt(clock%1440);GUI.Label(new Rect(30,61,180,32),(mins/60).ToString("00")+":"+(mins%60).ToString("00"),label);
  GUI.Label(new Rect(24,552,220,36),"MONEDAS  "+coins,big);Bar(new Rect(24,595,220,32),health,new Color(.95f,.17f,.3f),"VIDA",small);Bar(new Rect(24,635,220,32),energy,new Color(.05f,.7f,.95f),"ENERGÍA",small);
  for(int i=0;i<4;i++){var slot=slots[i];var r=new Rect(361+i*142,582,132,116);Panel(new Rect(r.x-3,r.y-3,r.width+6,r.height+6),i==selected?new Color(1,.78f,.2f):new Color(.35f,.42f,.49f));Panel(r,new Color(.07f,.11f,.17f,.96f));GUI.Label(new Rect(r.x+7,r.y+4,30,22),(i+1).ToString(),small);if(slot.count>0){var d=Data(slot.kind);Panel(new Rect(r.x+51,r.y+12,30,24),d.color);GUI.Label(new Rect(r.x+4,r.y+38,124,48),d.itemName,center);GUI.Label(new Rect(r.x,r.y+86,132,27),slot.count+" / "+Limit(slot.kind),center);}else GUI.Label(new Rect(r.x,r.y+40,132,45),"Vacío",center);}
  GUI.Label(new Rect(624,342,32,32),"+",center);
  Panel(new Rect(256,18,780,47),new Color(.06f,.1f,.16f,.7f));GUI.Label(new Rect(267,21,760,42),Time.time<MessageUntil?Message:"Zanahorias → slimes rosados → plorts → mercado",center);
  GUI.Label(new Rect(270,548,930,26),"Derecho: aspirar   Izquierdo: lanzar   1–4 / rueda: ranura",small);
  GUI.Label(new Rect(20,690,1240,28),"WASD: caminar   Shift: correr   Espacio: saltar   Tab: liberar ratón   F5: guardar   F9: cargar",small);
  if(upgradeStation&&Vector3.Distance(player.position,upgradeStation.position)<2.5f)GUI.Label(new Rect(410,460,500,60),upgrade>0?"Depósitos mejorados: 30 por ranura":"E: mejorar depósitos · 150 monedas",center);
  if(Cursor.lockState!=CursorLockMode.Locked)GUI.Label(new Rect(325,500,700,35),"Haz clic dentro del juego para controlar la cámara",center);
  GUI.matrix=old;
 }
 static void Panel(Rect r,Color color){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
 static void Bar(Rect r,float value,Color color,string title,GUIStyle style){Panel(r,new Color(.06f,.1f,.16f,.9f));Panel(new Rect(r.x+2,r.y+2,(r.width-4)*Mathf.Clamp01(value/100),r.height-4),color);GUI.Label(new Rect(r.x+9,r.y+4,r.width-10,r.height),title+"   "+Mathf.CeilToInt(value),style);}
}
}




