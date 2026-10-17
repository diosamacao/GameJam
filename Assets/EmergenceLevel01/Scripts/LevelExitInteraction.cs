using UnityEngine;
using UnityEngine.SceneManagement;
namespace Emergence.Level01
{
    public sealed class LevelExitInteraction : StoryInteractable
    {
        public LevelFlowConfig flow;
        protected override bool Available=>flow && flow.Find(SceneManager.GetActiveScene().name)!=null;
        protected override bool Interact(RobotEmotionGameplay player)=>LevelFlowController.Begin(flow);
        void Reset(){interactionHint="E 前往下一关";}
    }
}
