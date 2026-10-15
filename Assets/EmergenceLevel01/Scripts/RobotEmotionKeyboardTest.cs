using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(RobotEmotionController))]
    public sealed class RobotEmotionKeyboardTest : MonoBehaviour
    {
        [Tooltip("Disable this component when gameplay takes ownership of emotions.")]
        public bool enableKeyboardTest = false;
        public KeyCode joyKey=KeyCode.Y, angerKey=KeyCode.U, sadnessKey=KeyCode.I, delightKey=KeyCode.O, blankKey=KeyCode.P;
        RobotEmotionController target;
        void Awake() { target = GetComponent<RobotEmotionController>(); }
        public static bool TryMapKey(KeyCode key, out RobotEmotion emotion)
        {
            switch(key) {
                case KeyCode.Y: emotion=RobotEmotion.Joy; return true;
                case KeyCode.U: emotion=RobotEmotion.Anger; return true;
                case KeyCode.I: emotion=RobotEmotion.Sadness; return true;
                case KeyCode.O: emotion=RobotEmotion.Delight; return true;
                case KeyCode.P: emotion=RobotEmotion.Blank; return true;
                default: emotion=RobotEmotion.Blank; return false;
            }
        }
        void Update()
        {
            if(DialogueDirector.IsBlockingInput || !enableKeyboardTest || !target) return;
            if(Input.GetKeyDown(joyKey)) target.SetEmotion(RobotEmotion.Joy);
            else if(Input.GetKeyDown(angerKey)) target.SetEmotion(RobotEmotion.Anger);
            else if(Input.GetKeyDown(sadnessKey)) target.SetEmotion(RobotEmotion.Sadness);
            else if(Input.GetKeyDown(delightKey)) target.SetEmotion(RobotEmotion.Delight);
            else if(Input.GetKeyDown(blankKey)) target.SetEmotion(RobotEmotion.Blank);
        }
    }
}
