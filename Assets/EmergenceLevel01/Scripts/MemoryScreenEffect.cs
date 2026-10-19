using UnityEngine;
namespace Emergence.Level01
{
    // Draw behind dialogue and the scene-transition curtain; never captures input.
    [DisallowMultipleComponent]
    public sealed class MemoryScreenEffect : MonoBehaviour
    {
        public bool effectEnabled=true;
        [Range(0,1)] public float entryAlpha=.45f, restingAlpha=.12f;
        [Min(0)] public float fadeInSeconds=.3f, settleSeconds=.8f, fadeOutSeconds=.4f;
        public Color tint=Color.white;
        public int guiDepth=100;
        public float Opacity {get;private set;}
        RobotEmotionGameplay actor;
        bool remembering;
        float elapsed, startAlpha;
        void Awake(){actor=GetComponent<RobotEmotionGameplay>();}
        void Update(){
            bool active=effectEnabled && actor && !actor.isNpc && actor.CurrentEmotion==RobotEmotion.Memory && !actor.IsSwitching;
            if(active!=remembering){remembering=active;elapsed=0;startAlpha=Opacity;}
            elapsed+=Time.unscaledDeltaTime;
            if(remembering){
                if(elapsed<fadeInSeconds)Opacity=Mathf.Lerp(startAlpha,entryAlpha,elapsed/Mathf.Max(.001f,fadeInSeconds));
                else Opacity=Mathf.Lerp(entryAlpha,restingAlpha,(elapsed-fadeInSeconds)/Mathf.Max(.001f,settleSeconds));
            }else Opacity=Mathf.Lerp(startAlpha,0,elapsed/Mathf.Max(.001f,fadeOutSeconds));
        }
        void OnDisable(){Opacity=0;remembering=false;elapsed=0;startAlpha=0;}
        void OnGUI(){
            if(GameMenuUI.IsPaused || Opacity<=0)return;
            var old=GUI.color;int depth=GUI.depth;GUI.depth=guiDepth;
            var color=tint;color.a*=Opacity;GUI.color=color;
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height),Texture2D.whiteTexture);
            GUI.color=old;GUI.depth=depth;
        }
    }
}
