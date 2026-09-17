using UnityEngine;
namespace SlimeRancherVR {
[RequireComponent(typeof(Rigidbody))]
public sealed class RanchItem : MonoBehaviour {
 public RanchItemData data;
 public float hunger;
 public float graceUntil;
 public bool Consumed {get;private set;}
 public Rigidbody Body {get;private set;}
 float lastPull=-10,nextEat;
 Vector3 home;
 void Awake(){Body=GetComponent<Rigidbody>();home=transform.position;}
 public void Place(Vector3 point, Quaternion rotation){transform.SetPositionAndRotation(point,rotation);Body.position=point;Body.rotation=rotation;home=point;var pink=GetComponent<PinkSlime>();if(pink)pink.SetHome(point);var chicken=GetComponent<SlimeRancher.Area1.Area1Chicken>();if(chicken)chicken.SetHome(point);}
 public bool Consume(){if(Consumed||!isActiveAndEnabled)return false;Consumed=true;gameObject.SetActive(false);Destroy(gameObject);return true;}
 public void Pull(Vector3 point,float speed){if(Consumed)return;lastPull=Time.time;var pink=GetComponent<PinkSlime>();if(pink){pink.Pull(point,speed);return;}Body.useGravity=false;Body.linearVelocity=Vector3.MoveTowards(Body.linearVelocity,(point-transform.position).normalized*speed,35*Time.fixedDeltaTime);}
 void Update(){
  if(Consumed||!data)return;
  if(!GetComponent<PinkSlime>()&&Time.time-lastPull>.1f)Body.useGravity=true;
  if(transform.position.y<-6){Place(new Vector3(Mathf.Clamp(home.x,-8,8),1,Mathf.Clamp(home.z,-8,8)),Quaternion.identity);Body.linearVelocity=Vector3.zero;}
  if(data.kind!=RanchItemKind.PinkSlime)return;
  hunger=Mathf.Max(0,hunger-Time.deltaTime);
  if(hunger>0||Time.time<nextEat||Time.time<graceUntil)return;
  nextEat=Time.time+.25f;
  var nearby=Physics.OverlapSphere(transform.position,.8f,~0,QueryTriggerInteraction.Ignore);
  foreach(var col in nearby){var food=col.GetComponentInParent<RanchItem>();if(TryEat(food))break;}
 }
 public bool TryEat(RanchItem food){
  var game=RanchGame.Instance;
  if(!game||!data||data.kind!=RanchItemKind.PinkSlime||hunger>0||Consumed||!food||!food.data||(food.data.kind!=RanchItemKind.Carrot&&food.data.kind!=RanchItemKind.Chicken&&food.data.kind!=RanchItemKind.ElderChicken)||food.Consumed||Vector3.Distance(transform.position,food.transform.position)>.85f)return false;
  var ray=food.transform.position-transform.position;
  foreach(var hit in Physics.RaycastAll(transform.position,ray.normalized,ray.magnitude,~0,QueryTriggerInteraction.Ignore))if(!hit.transform.IsChildOf(transform)&&!hit.transform.IsChildOf(food.transform))return false;
  if(!food.Consume())return false;
  hunger=20;
  var plort=game.Spawn(RanchItemKind.PinkPlort,transform.position+Vector3.up*.65f,Quaternion.identity);
  plort.Body.linearVelocity=Vector3.up*2;plort.graceUntil=Time.time+.5f;
  game.Notify("¡Bien alimentado! Recoge el plort rosa y véndelo.");return true;
 }
}
}
