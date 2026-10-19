using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emergence.Level01
{
    [AddComponentMenu("Emergence/Game Completion Interaction")]
    public sealed class GameCompletionInteraction : StoryInteractable
    {
        [Header("通关字幕")]
        [TextArea] public string endingText="机械情感\n完";
        public Font endingFont;
        [Min(1)] public int fontSize=48;
        [Min(0)] public float blackFadeSeconds=.8f;
        [Min(0)] public float blackWaitSeconds=1f;
        [Min(0)] public float textFadeInSeconds=1f;
        [Min(0)] public float textHoldSeconds=2f;
        [Header("通关后返回主界面")]
        public string mainMenuScene="MainMenu";
        [Min(0)] public float revealMenuSeconds=.5f;
        bool triggered;
        void Reset(){interactionHint="F 完成游戏";}
        protected override bool Available=>!triggered;
        protected override string DisplayHint=>EmotionFragment.Remaining(gameObject.scene)>0?"收集全部情感碎片后通关":interactionHint;
        protected override bool Interact(RobotEmotionGameplay player){
            if(triggered || EmotionFragment.Remaining(gameObject.scene)>0)return false;
            var flow=ScriptableObject.CreateInstance<LevelFlowConfig>();
            flow.fadeOutSeconds=blackFadeSeconds;flow.fadeInSeconds=revealMenuSeconds;flow.blackHoldSeconds=0;
            flow.introText=endingText;flow.introFont=endingFont?endingFont:GameMenuUI.Instance?GameMenuUI.Instance.DisplayFont:null;
            flow.introFontSize=fontSize;flow.introWaitSeconds=blackWaitSeconds;flow.introFadeInSeconds=textFadeInSeconds;
            flow.introHoldSeconds=textHoldSeconds;flow.introFadeOutSeconds=0;
            flow.routes=new[]{new LevelFlowConfig.Route{fromScene=SceneManager.GetActiveScene().name,nextScene=mainMenuScene}};
            if(!LevelFlowController.Begin(flow)){Destroy(flow);return false;}
            triggered=true;
            // This object is unloaded during the transition; the persistent controller owns cleanup.
            LevelFlowController.DisposeAfterTransition(flow);
            return true;
        }
    }
}
