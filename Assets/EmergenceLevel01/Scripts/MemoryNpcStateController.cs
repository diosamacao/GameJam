using UnityEngine;
namespace Emergence.Level01
{
    public enum MemoryNpcState { Normal, Memory }
    [DefaultExecutionOrder(-45)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EmotionNpc))]
    public sealed class MemoryNpcStateController : MonoBehaviour
    {
        [SerializeField] MemoryNpcState state=MemoryNpcState.Normal;
        [Range(0,1)] public float normalAlpha=.5f, memoryAlpha=.9f;
        public MemoryNpcState State=>state;
        public bool EmotionEnabled=>state==MemoryNpcState.Memory;
        SpriteRenderer[] visuals;
        public static bool AllowsEmotion(Component target){var gate=target.GetComponentInParent<MemoryNpcStateController>();return !gate || !gate.isActiveAndEnabled || gate.EmotionEnabled;}
        void Awake(){state=MemoryNpcState.Normal;visuals=GetComponentsInChildren<SpriteRenderer>(true);ApplyAlpha();}
        void Update(){RefreshState();}
        public void RefreshState(){
            // Time stop takes precedence over range changes, including the loss of memory resonance.
            if(LocalTimeTarget.IsStopped(this))return;
            bool inMemory=false;
            var own=GetComponent<EmotionResonance>();
            foreach(var source in EmotionResonance.Actors){
                // The NPC cannot keep itself visible merely by standing inside its own aura.
                if(!source || source==own || source.Emotion!=RobotEmotion.Memory || ((Vector2)(source.transform.position-transform.position)).sqrMagnitude>source.radius*source.radius)continue;
                source.EnsureState();
                if(source.Enhanced && source.Emotion==RobotEmotion.Memory && Vector2.Distance(source.transform.position,transform.position)<=source.radius){inMemory=true;break;}
            }
            var next=inMemory?MemoryNpcState.Memory:MemoryNpcState.Normal;
            if(next!=state){state=next;EmotionResonance.InvalidateStates();}
            ApplyAlpha();
        }
        void LateUpdate(){ApplyAlpha();}
        void ApplyAlpha(){
            float alpha=EmotionEnabled?memoryAlpha:normalAlpha;
            if(visuals!=null)foreach(var visual in visuals)if(visual){var c=visual.color;c.a=alpha;visual.color=c;}
            // The face component copies the body's tint to its dynamically created screen.
            var face=GetComponent<RobotEmotionController>();if(face)face.RefreshVisual();
        }
        void OnEnable(){EmotionResonance.InvalidateStates();}
        void OnDisable(){EmotionResonance.InvalidateStates();if(visuals!=null)foreach(var visual in visuals)if(visual){var c=visual.color;c.a=1;visual.color=c;}}
    }
}
