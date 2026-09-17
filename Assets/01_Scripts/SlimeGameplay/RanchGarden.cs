using UnityEngine;
namespace SlimeRancherVR {
public sealed class RanchGarden : MonoBehaviour {
 public float remaining=30;
 public int maximum=6;
 void Update(){remaining-=Time.deltaTime;if(remaining>0||!RanchGame.Instance)return;remaining=30;int count=0;foreach(var item in FindObjectsByType<RanchItem>(FindObjectsSortMode.None))if(item.data&&item.data.kind==RanchItemKind.Carrot&&Vector3.Distance(item.transform.position,transform.position)<2)count++;if(count<maximum)RanchGame.Instance.Spawn(RanchItemKind.Carrot,transform.position+new Vector3(Random.Range(-.8f,.8f),.6f,Random.Range(-.6f,.6f)),Quaternion.identity);}
}
}
