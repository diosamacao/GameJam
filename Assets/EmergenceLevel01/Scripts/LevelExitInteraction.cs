using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01
{
    public sealed class LevelExitInteraction : StoryInteractable
    {
        public LevelFlowConfig flow;
        protected override bool Available=>flow && flow.Find(SceneManager.GetActiveScene().name)!=null;
        protected override string DisplayHint=>EmotionFragment.Remaining(gameObject.scene)>0 ? "情感碎片："+EmotionFragment.Remaining(gameObject.scene) : base.DisplayHint;
        protected override bool Interact(RobotEmotionGameplay player)=>EmotionFragment.Remaining(gameObject.scene)==0 && LevelFlowController.Begin(flow);
        void Reset(){interactionHint="F 进入";}
    }
}
