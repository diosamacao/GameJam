using System.Collections.Generic;
using UnityEngine;
namespace Emergence.Level01
{
    public abstract class StoryInteractable : MonoBehaviour
    {
        static readonly List<StoryInteractable> targets=new List<StoryInteractable>();
        [Min(0)] public float interactionRadius=2;
        public string interactionHint="E 交互";
        public bool showHint=true;
        public Vector2 hintSize=new Vector2(220,30);
        public float hintBottomOffset=120;
        protected virtual bool Available=>true;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetRegistry(){targets.Clear();}
        protected virtual void OnEnable(){targets.Add(this);}
        protected virtual void OnDisable(){targets.Remove(this);}
        public bool TryInteract(RobotEmotionGameplay player){return player && !player.isNpc && player.CanUseEmotionAbilities && !LevelFlowController.IsTransitioning && isActiveAndEnabled && Available && Vector2.Distance(transform.position,player.transform.position)<=interactionRadius && Interact(player);}
        protected abstract bool Interact(RobotEmotionGameplay player);
        static StoryInteractable Nearest(RobotEmotionGameplay player){StoryInteractable result=null;float nearest=float.PositiveInfinity;foreach(var item in targets){if(!item || !item.isActiveAndEnabled || !item.Available)continue;float distance=Vector2.Distance(player.transform.position,item.transform.position);if(distance<=item.interactionRadius && distance<nearest){result=item;nearest=distance;}}return result;}
        public static bool TryNearest(RobotEmotionGameplay player){var item=Nearest(player);if(!item)return false;item.TryInteract(player);return true;}
        void OnGUI(){if(!showHint || LevelFlowController.IsTransitioning || DialogueDirector.IsBlockingInput)return;foreach(var actor in EmotionResonance.Actors){if(!actor || actor.Actor.isNpc || !actor.Actor.CanUseEmotionAbilities || Nearest(actor.Actor)!=this)continue;var transfer=actor.GetComponent<EmotionTransfer>();string hint=interactionHint.Replace("E",transfer?transfer.interactKey.ToString():"E");GUI.Box(new Rect((Screen.width-hintSize.x)/2,Screen.height-hintBottomOffset,hintSize.x,hintSize.y),hint);break;}}
        void OnDrawGizmosSelected(){Gizmos.color=Color.yellow;Gizmos.DrawWireSphere(transform.position,interactionRadius);}
    }
}
