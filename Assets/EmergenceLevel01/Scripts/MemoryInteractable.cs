using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    public sealed class MemoryInteractable : MonoBehaviour
    {
        static readonly List<MemoryInteractable> Items=new List<MemoryInteractable>();
        [Min(0)] public float interactionRadius=2;
        public bool state, toggle=true;
        public GameObject stateOffVisual,stateOnVisual;
        public UnityEvent onRemembered=new UnityEvent(), onForgotten=new UnityEvent();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetRegistry(){Items.Clear();}
        void OnEnable(){if(!Items.Contains(this))Items.Add(this);Apply();}
        void OnDisable(){Items.Remove(this);}
        void Apply(){if(stateOffVisual && stateOffVisual!=gameObject)stateOffVisual.SetActive(!state);if(stateOnVisual && stateOnVisual!=gameObject)stateOnVisual.SetActive(state);}
        public bool Interact(RobotEmotionGameplay player){if(LocalTimeTarget.IsStopped(this) || !player || player.isNpc || !player.CanUseEmotionAbilities || player.CurrentEmotion!=RobotEmotion.Memory || Vector2.Distance(transform.position,player.transform.position)>interactionRadius)return false;state=toggle?!state:true;Apply();if(state)onRemembered.Invoke();else onForgotten.Invoke();return true;}
        public static bool TryNearest(EmotionResonance source){MemoryInteractable nearest=null;float best=float.MaxValue;foreach(var item in Items){float d=Vector2.Distance(item.transform.position,source.transform.position);if(d<=item.interactionRadius && d<best){best=d;nearest=item;}}return nearest && nearest.Interact(source.Actor);}
    }
}
