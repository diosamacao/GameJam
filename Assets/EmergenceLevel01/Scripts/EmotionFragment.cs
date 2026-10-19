using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
namespace Emergence.Level01
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class EmotionFragment : MonoBehaviour
    {
        public bool requiredForExit=true;
        public Color color=new Color(.4f,1,1,1);
        [Min(.01f)] public float size=.25f, lineWidth=.06f, pickupRadius=.35f;
        [Min(0)] public float bobAmplitude=.08f, bobSpeed=2;
        public int sortingOrder=60;
        public UnityEvent onCollected=new UnityEvent();
        public bool Collected {get;private set;}
        sealed class Entry {public EmotionFragment item;public int scene;public bool required,collected;}
        static readonly Dictionary<int,Entry> entries=new Dictionary<int,Entry>();
        LineRenderer visual;Material material;float elapsed;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetRegistry(){entries.Clear();SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;SceneManager.sceneUnloaded-=Unloaded;SceneManager.sceneUnloaded+=Unloaded;}
        static void Loaded(Scene scene,LoadSceneMode mode){Scan(scene);}
        static void Unloaded(Scene scene){var remove=new List<int>();foreach(var pair in entries)if(pair.Value.scene==scene.handle)remove.Add(pair.Key);foreach(int id in remove)entries.Remove(id);}
        static void Scan(Scene scene){if(!scene.IsValid() || !scene.isLoaded)return;foreach(var root in scene.GetRootGameObjects())foreach(var fragment in root.GetComponentsInChildren<EmotionFragment>(true))fragment.Register();}
        void Register(){int id=GetInstanceID();if(!entries.ContainsKey(id))entries[id]=new Entry{item=this,scene=gameObject.scene.handle,required=requiredForExit,collected=Collected};}
        public static int Remaining(Scene scene){Scan(scene);int count=0;foreach(var entry in entries.Values){if(entry.item){entry.required=entry.item.requiredForExit;entry.collected=entry.item.Collected;}if(entry.scene==scene.handle && entry.required && !entry.collected)count++;}return count;}
        void Awake(){Register();GetComponent<CircleCollider2D>().isTrigger=true;GetComponent<CircleCollider2D>().radius=pickupRadius;
            var child=new GameObject("FragmentVisual");child.transform.SetParent(transform,false);visual=child.AddComponent<LineRenderer>();visual.useWorldSpace=false;visual.loop=true;visual.positionCount=4;
            material=new Material(Shader.Find("Sprites/Default"));visual.sharedMaterial=material;Draw();}
        void OnEnable(){Register();}
        void Update(){if(LocalTimeTarget.IsStopped(this))return;elapsed+=Time.deltaTime;Draw();}
        void Draw(){if(!visual)return;visual.startColor=visual.endColor=color;visual.startWidth=visual.endWidth=lineWidth;visual.sortingOrder=sortingOrder;float y=Mathf.Sin(elapsed*bobSpeed)*bobAmplitude;visual.SetPositions(new[]{new Vector3(0,y+size),new Vector3(size,y),new Vector3(0,y-size),new Vector3(-size,y)});}
        public bool Collect(RobotEmotionGameplay actor){if(Collected || !actor || actor.isNpc || !isActiveAndEnabled || LocalTimeTarget.IsStopped(this))return false;Collected=true;GameAudio.Play(GameSound.Fragment,transform.position);Register();onCollected.Invoke();gameObject.SetActive(false);return true;}
        void OnTriggerStay2D(Collider2D other){Collect(other.GetComponentInParent<RobotEmotionGameplay>());}
        void OnDrawGizmos(){Gizmos.color=color;var p=transform.position;Gizmos.DrawLine(p+Vector3.up*size,p+Vector3.right*size);Gizmos.DrawLine(p+Vector3.right*size,p+Vector3.down*size);Gizmos.DrawLine(p+Vector3.down*size,p+Vector3.left*size);Gizmos.DrawLine(p+Vector3.left*size,p+Vector3.up*size);}
        void OnDestroy(){if(material)Destroy(material);}
    }
}
