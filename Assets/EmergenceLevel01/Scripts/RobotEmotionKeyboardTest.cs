using UnityEngine;
namespace Emergence.Level01
{
    [RequireComponent(typeof(RobotEmotionController))]
    public sealed class RobotEmotionKeyboardTest : MonoBehaviour
    {
        [Tooltip("Disable this component when gameplay takes ownership of emotions.")]
        public bool enableKeyboardTest = false;
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
            if(!enableKeyboardTest || !target) return;
            if(Input.GetKeyDown(KeyCode.Y)) target.SetEmotion(RobotEmotion.Joy);
            else if(Input.GetKeyDown(KeyCode.U)) target.SetEmotion(RobotEmotion.Anger);
            else if(Input.GetKeyDown(KeyCode.I)) target.SetEmotion(RobotEmotion.Sadness);
            else if(Input.GetKeyDown(KeyCode.O)) target.SetEmotion(RobotEmotion.Delight);
            else if(Input.GetKeyDown(KeyCode.P)) target.SetEmotion(RobotEmotion.Blank);
        }
    }
}
