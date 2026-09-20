using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
namespace SlimeRancherVR {
[DefaultExecutionOrder(11000)]
public sealed class SlimeVacuum : MonoBehaviour {
 public SlimeData data;
 public Transform muzzle,playerRoot;
 public LineRenderer stream;
 public TextMesh inventoryText;
 public PinkSlime[] demoSlimes;
 public bool ReadInput=true;
 public int Count {get {int count=0;if(RanchGame.Instance)foreach(var s in RanchGame.Instance.slots)if(s.kind==RanchItemKind.PinkSlime)count+=s.count;return count;}}
 public bool IsSuctioning {get;private set;}
 public PinkSlime CurrentTarget {get;private set;}
 public RanchItem Target {get;private set;}
 readonly Collider[] candidates=new Collider[128];readonly RaycastHit[] hits=new RaycastHit[128];
 AudioSource hum;AudioClip captureClip,shotClip;
 bool requested;float nextShot;VacuumPickup pickup;
 public bool CanUse=>!pickup||pickup.IsHeld;
 RanchGame Game=>RanchGame.Instance;
 void Awake(){pickup=GetComponent<VacuumPickup>();hum=gameObject.AddComponent<AudioSource>();hum.spatialBlend=.4f;hum.volume=.12f;hum.loop=true;hum.playOnAwake=false;hum.clip=Tone(85,.5f);captureClip=Tone(660,.12f);shotClip=Tone(220,.1f);}
 AudioClip Tone(float hz,float seconds){int n=Mathf.RoundToInt(22050*seconds);var clip=AudioClip.Create("Vacuum feedback",n,1,22050,false);var samples=new float[n];for(int i=0;i<n;i++)samples[i]=Mathf.Sin(2*Mathf.PI*hz*i/22050)*.25f*Mathf.Min(1,i/220f)*Mathf.Min(1,(n-i)/220f);clip.SetData(samples,0);return clip;}
 void Update(){
  if(!Game)return;
  if(ReadInput){
   if(!RanchVRControls.Active&&!BeatrixVRBody.Tracked(XRNode.Head)&&Camera.main){var camera=Camera.main.transform;Vector3 aim=camera.position+camera.forward*30;float nearest=30;int count=Physics.RaycastNonAlloc(camera.position,camera.forward,hits,30,~0,QueryTriggerInteraction.Ignore);for(int i=0;i<count;i++)if(!hits[i].transform.IsChildOf(playerRoot)&&hits[i].distance<nearest){nearest=hits[i].distance;aim=hits[i].point;}if(Vector3.Dot(aim-muzzle.position,camera.forward)>.1f)muzzle.rotation=Quaternion.LookRotation(aim-muzzle.position);}
   bool desktop=!RanchVRControls.Active&&!BeatrixVRBody.Tracked(XRNode.Head);var mouse=Mouse.current;bool mouseReady=desktop&&BeatrixDesktopPreview.InputReady;
   var vr=RanchVRControls.Instance;var hand=RanchVRControls.Active?(pickup&&pickup.IsLeftHand?vr.Left:vr.Right):default;
   var other=RanchVRControls.Active?(pickup&&pickup.IsLeftHand?vr.Right:vr.Left):default;
   if(!desktop)muzzle.localRotation=Quaternion.identity;
   requested=CanUse&&(desktop?(mouseReady&&mouse!=null&&mouse.rightButton.isPressed):(hand.tracked&&hand.trigger>.15f));
   bool shoot=CanUse&&(desktop?(mouseReady&&mouse!=null&&mouse.leftButton.isPressed):(other.tracked&&other.trigger>.15f&&!vr.Left.secondary));
   if(shoot&&!requested&&Time.time>=nextShot){Shoot();nextShot=Time.time+.3f;}
  }
  IsSuctioning=requested&&CanUse;
  if(IsSuctioning&&!hum.isPlaying)hum.Play();else if(!IsSuctioning&&hum.isPlaying)hum.Stop();
  if(inventoryText){var slot=Game.slots[Game.selected];inventoryText.text=(Game.selected+1)+": "+(slot.count>0?Game.Data(slot.kind).itemName+" "+slot.count+"/"+Game.Limit(slot.kind):"Vacío")+"\nMonedas: "+Game.coins+" | Vida: "+Mathf.CeilToInt(Game.health)+"\n"+(Time.time<Game.MessageUntil?Game.Message:"Grip: agarrar/soltar | A-B: ranuras\nGatillo de esta mano: aspirar / otro: lanzar\nIzq: stick mueve / X salta / Y guarda\nGrip: comprar | Gatillo + Y (2 s): cargar");}
 }
 public void SetSuction(bool active){requested=active&&CanUse;IsSuctioning=requested;}
 bool ClearPath(Vector3 from,Vector3 to,Transform target=null){var delta=to-from;int n=Physics.RaycastNonAlloc(from,delta.normalized,hits,delta.magnitude,~0,QueryTriggerInteraction.Ignore);if(n==hits.Length)return false;for(int i=0;i<n;i++){var t=hits[i].collider.transform;if(t.IsChildOf(transform)||(playerRoot&&t.IsChildOf(playerRoot))||(target&&t.IsChildOf(target)))continue;return false;}return true;}
 public bool CanPull(PinkSlime slime)=>slime&&CanPull(slime.GetComponent<RanchItem>());
 public bool CanPull(RanchItem item){
  if(!Game||!item||item.Consumed||!item.isActiveAndEnabled||!item.data||Time.time<item.graceUntil||!Game.CanStore(item.data.kind))return false;
  var held=item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>(); if(held&&held.isSelected)return false;
  Vector3 delta=item.transform.position-muzzle.position;float distance=delta.magnitude;
  if(distance>data.suctionRange)return false;
  if(distance>.5f&&Vector3.Dot(muzzle.forward,delta/distance)<Mathf.Cos(data.suctionHalfAngle*Mathf.Deg2Rad))return false;
  var head=Camera.main;if(head&&!ClearPath(head.transform.position,muzzle.position))return false;
  return ClearPath(muzzle.position,item.transform.position,item.transform);
 }
 public bool Shoot(){
  if(!Game||!muzzle||!CanUse)return false;var slot=Game.slots[Game.selected];if(slot.count==0){Game.Notify("Ranura vacía: cambia con A/B en VR o 1–4 en PC.");return false;}
  var d=Game.Data(slot.kind);var point=muzzle.position+muzzle.forward*(d.radius+.12f);var head=Camera.main;
  if((head&&!ClearPath(head.transform.position,point))||!ClearPath(muzzle.position,point)){Game.Notify("Aléjate de la pared para lanzar.");return false;}
  int n=Physics.OverlapSphereNonAlloc(point,d.radius,candidates,~0,QueryTriggerInteraction.Ignore);
  if(n==candidates.Length)return false;
  for(int i=0;i<n;i++)if(!candidates[i].transform.IsChildOf(playerRoot)&&!candidates[i].transform.IsChildOf(transform)){Game.Notify("La boquilla está bloqueada.");return false;}
  var item=Game.Spawn(slot.kind,point,Quaternion.Euler(0,90,0));item.graceUntil=Time.time+.65f;item.Body.linearVelocity=muzzle.forward*9+Vector3.up*.8f;
  foreach(var col in item.GetComponentsInChildren<Collider>())foreach(var own in playerRoot.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(col,own);
  // Restore collisions after the projectile has cleared the player's body.
  StartCoroutine(RestorePlayerCollision(item));
  slot.count--;hum.PlayOneShot(shotClip,.9f);RanchVRControls.Haptic(.4f);return true;
 }
 System.Collections.IEnumerator RestorePlayerCollision(RanchItem item){yield return new WaitForSeconds(.5f);if(item)foreach(var col in item.GetComponentsInChildren<Collider>())foreach(var own in playerRoot.GetComponentsInChildren<Collider>())Physics.IgnoreCollision(col,own,false);}
 void FixedUpdate(){
  Target=null;CurrentTarget=null;if(!IsSuctioning||!Game)return;
  int n=Physics.OverlapSphereNonAlloc(muzzle.position,data.suctionRange,candidates,~0,QueryTriggerInteraction.Ignore);float best=float.MaxValue;
  for(int i=0;i<n;i++){var item=candidates[i].GetComponentInParent<RanchItem>();if(!CanPull(item))continue;float score=Vector3.Distance(muzzle.position,item.transform.position);if(score<best){best=score;Target=item;}}
  if(!Target)return;CurrentTarget=Target.GetComponent<PinkSlime>();
  if(best<.48f){if(Game.Collect(Target)){hum.PlayOneShot(captureClip,.9f);RanchVRControls.Haptic();}Target=null;CurrentTarget=null;}else Target.Pull(muzzle.position,data.pullSpeed);
 }
 void LateUpdate(){if(inventoryText){inventoryText.GetComponent<Renderer>().enabled=CanUse&&(RanchVRControls.Active||BeatrixVRBody.Tracked(XRNode.Head));inventoryText.transform.localScale=Vector3.one*.0022f;var camera=Camera.main;if(camera)inventoryText.transform.rotation=camera.transform.rotation;}
  if(!stream)return;stream.enabled=IsSuctioning;if(stream.enabled){stream.SetPosition(0,muzzle.position);stream.SetPosition(1,Target?Target.transform.position:muzzle.position+muzzle.forward);stream.startWidth=.025f;stream.endWidth=.09f;}}
 public void ResetDemo(){SetSuction(false);if(Game)foreach(var s in Game.slots)s.count=0;if(demoSlimes!=null)foreach(var s in demoSlimes)if(s)s.ResetSlime();}
 void OnDisable(){SetSuction(false);if(stream)stream.enabled=false;if(hum)hum.Stop();}
 void OnDestroy(){if(hum&&hum.clip)Destroy(hum.clip);if(captureClip)Destroy(captureClip);if(shotClip)Destroy(shotClip);}
}
}



