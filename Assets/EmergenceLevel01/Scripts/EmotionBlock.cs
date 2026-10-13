using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    public sealed class EmotionBlock : MonoBehaviour
    {
        public EmotionBlockKind kind;
        public UnityEvent onRemoved=new UnityEvent();
        public bool Removed {get;private set;}
        public bool TryInteract(RobotEmotionGameplay player)
        {
            if(Removed || !isActiveAndEnabled || !player || !player.CanUseEmotionAbilities)return false;
            if(kind==EmotionBlockKind.Electric)return player.HandleElectricContact();
            if(!EmotionBlockRules.CanRemove(kind,player.CurrentEmotion))return false;
            Removed=true;onRemoved.Invoke();gameObject.SetActive(false);return true;
        }
    }
}
