using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class SceneCheckpoint : MonoBehaviour
    {
        [Tooltip("出生脚底位置。留空使用本物体位置加偏移。")]
        public Transform spawnPoint;
        public Vector2 spawnOffset;
        public SpriteRenderer indicator;
        public Color inactiveColor=new Color(.35f,.6f,.7f), activeColor=new Color(.4f,1,.8f);
        public UnityEvent onActivated=new UnityEvent();
        public Vector2 SpawnPosition=>(spawnPoint?(Vector2)spawnPoint.position:(Vector2)transform.position)+spawnOffset;
        public bool IsCurrent=>SceneCheckpointState.TryGet(gameObject.scene,out var position) && (position-SpawnPosition).sqrMagnitude<.0001f;
        void Awake(){GetComponent<BoxCollider2D>().isTrigger=true;}
        void Update(){if(indicator)indicator.color=IsCurrent?activeColor:inactiveColor;}
        public bool Activate(RobotEmotionGameplay player){
            if(!player || player.isNpc || !isActiveAndEnabled || player.gameObject.scene!=gameObject.scene || LevelFlowController.IsTransitioning || LocalTimeTarget.IsStopped(this) || IsCurrent)return false;
            SceneCheckpointState.Activate(gameObject.scene,SpawnPosition);onActivated.Invoke();return true;
        }
        void OnTriggerEnter2D(Collider2D other){Activate(other.GetComponentInParent<RobotEmotionGameplay>());}
        void OnTriggerStay2D(Collider2D other){Activate(other.GetComponentInParent<RobotEmotionGameplay>());}
        void Reset(){GetComponent<BoxCollider2D>().isTrigger=true;}
        void OnDrawGizmos(){Gizmos.color=Color.green;Gizmos.DrawWireSphere(SpawnPosition,.15f);Gizmos.DrawLine(SpawnPosition,SpawnPosition+Vector2.up);}
    }
}
