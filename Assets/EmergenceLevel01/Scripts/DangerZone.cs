using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DangerZone : MonoBehaviour
    {
        public bool enhancedCourageIsSafe=true;
        public bool TryContact(RobotEmotionGameplay actor){if(LocalTimeTarget.IsStopped(this) || !actor || actor.isNpc)return false;var resonance=actor.GetComponent<EmotionResonance>();if(enhancedCourageIsSafe && resonance){resonance.RefreshState();if(resonance.Enhanced && resonance.Emotion==RobotEmotion.Courage)return false;}return actor.DieAndRespawn();}
        void OnTriggerStay2D(Collider2D other){TryContact(other.GetComponentInParent<RobotEmotionGameplay>());}
        void Reset(){GetComponent<Collider2D>().isTrigger=true;}
    }
}
