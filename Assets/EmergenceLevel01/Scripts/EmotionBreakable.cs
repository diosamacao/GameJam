using UnityEngine;
using UnityEngine.Events;
namespace Emergence.Level01
{
    public sealed class EmotionBreakable : MonoBehaviour
    {
        public UnityEvent onBroken=new UnityEvent();
        public bool Broken {get;private set;}
        public bool TryBreak(RobotEmotionGameplay source)
        {
            if(Broken || !source || !source.CanBreak)return false;
            Broken=true;onBroken.Invoke();gameObject.SetActive(false);return true;
        }
    }
}
