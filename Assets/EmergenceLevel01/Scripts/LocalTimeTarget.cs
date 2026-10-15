using UnityEngine;
namespace Emergence.Level01
{
    [DefaultExecutionOrder(-30)]
    public sealed class LocalTimeTarget : MonoBehaviour
    {
        [Tooltip("要暂停的移动脚本，不要填自身。恢复时保持原启用状态。")]
        public Behaviour[] pauseBehaviours=new Behaviour[0];
        public Animator[] animators=new Animator[0];
        public Rigidbody2D targetBody;
        public bool Frozen {get;private set;}
        bool[] enabledBefore;float[] animatorSpeeds;Vector2 velocity;float angularVelocity;RigidbodyConstraints2D constraints;
        void FixedUpdate(){SetFrozen(EmotionResonance.InEnhancedRange(transform.position,RobotEmotion.Sadness));}
        public void SetFrozen(bool value){
            if(value==Frozen)return;Frozen=value;
            if(value){
                enabledBefore=new bool[pauseBehaviours.Length];for(int i=0;i<pauseBehaviours.Length;i++){var b=pauseBehaviours[i];if(b && b!=this){enabledBefore[i]=b.enabled;b.enabled=false;}}
                animatorSpeeds=new float[animators.Length];for(int i=0;i<animators.Length;i++)if(animators[i]){animatorSpeeds[i]=animators[i].speed;animators[i].speed=0;}
                if(targetBody){velocity=targetBody.velocity;angularVelocity=targetBody.angularVelocity;constraints=targetBody.constraints;targetBody.velocity=Vector2.zero;targetBody.angularVelocity=0;targetBody.constraints=RigidbodyConstraints2D.FreezeAll;}
            }else{
                for(int i=0;i<pauseBehaviours.Length && i<enabledBefore.Length;i++)if(pauseBehaviours[i] && pauseBehaviours[i]!=this)pauseBehaviours[i].enabled=enabledBefore[i];
                for(int i=0;i<animators.Length && i<animatorSpeeds.Length;i++)if(animators[i])animators[i].speed=animatorSpeeds[i];
                if(targetBody){targetBody.constraints=constraints;targetBody.velocity=velocity;targetBody.angularVelocity=angularVelocity;}
            }
        }
        void OnDisable(){if(Frozen)SetFrozen(false);}
    }
}
