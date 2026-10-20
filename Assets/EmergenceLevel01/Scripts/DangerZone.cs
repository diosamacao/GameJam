using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DangerZone : MonoBehaviour
    {
        public bool enhancedCourageIsSafe=true;
        Collider2D area;
        readonly System.Collections.Generic.List<Collider2D> overlaps=new System.Collections.Generic.List<Collider2D>();
        void Awake(){area=GetComponent<Collider2D>();}
        void FixedUpdate(){
            if(!area || !area.enabled || GameMenuUI.BlocksGameplay || LevelFlowController.IsTransitioning || LocalTimeTarget.IsStopped(this))return;
            // Sleeping rigidbodies do not reliably produce Stay messages.
            var filter=new ContactFilter2D();filter.NoFilter();
            area.OverlapCollider(filter,overlaps);
            foreach(var other in overlaps)if(other)TryContact(other.GetComponentInParent<RobotEmotionGameplay>());
        }
        public bool TryContact(RobotEmotionGameplay actor){if(LocalTimeTarget.IsStopped(this) || !actor || LocalTimeTarget.IsStopped(actor))return false;var resonance=actor.GetComponent<EmotionResonance>();if(enhancedCourageIsSafe && resonance){resonance.RefreshState();if(resonance.Enhanced && resonance.Emotion==RobotEmotion.Courage)return false;}return actor.DieAndRespawn();}
        void OnTriggerStay2D(Collider2D other){TryContact(other.GetComponentInParent<RobotEmotionGameplay>());}
        void Reset(){GetComponent<Collider2D>().isTrigger=true;}
    }
}
