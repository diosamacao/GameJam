using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class MemorySceneTrigger : MonoBehaviour
    {
        public MemorySceneConfig memory;
        public bool triggerOnEnter=true, playOnce=true;
        public bool HasPlayed {get;private set;}
        public bool TryTrigger(){
            if(!isActiveAndEnabled || (playOnce && HasPlayed))return false;
            if(!LevelFlowController.BeginMemory(memory))return false;
            HasPlayed=true;return true;
        }
        public void Trigger(){TryTrigger();}
        public void ResetTrigger(){HasPlayed=false;}
        public bool TryContact(Collider2D other){
            if(!triggerOnEnter || !other)return false;
            var actor=other.GetComponentInParent<RobotEmotionGameplay>();
            return actor && !actor.isNpc && actor.CanUseEmotionAbilities && TryTrigger();
        }
        void OnTriggerEnter2D(Collider2D other){TryContact(other);}
        void Reset(){GetComponent<BoxCollider2D>().isTrigger=true;}
    }
}
