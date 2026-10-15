using System;
using UnityEngine;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Robot Emotion Config")]
    public sealed class RobotEmotionConfig : ScriptableObject
    {
        public Sprite joy, anger, sadness, delight, blank;
        public Sprite strongAnger, courage;
        public Sprite neutral, loading, guardian, memory;
        [Header("新表情屏幕层（固定复用原显示器外壳）")]
        public Sprite neutralScreen,loadingScreen,memoryScreen,guardianScreen,courageScreen;
        public Vector2 screenOffset=new Vector2(0,.3828125f);
        public Sprite GetScreen(RobotEmotion emotion){switch(emotion){case RobotEmotion.Blank:return neutralScreen;case RobotEmotion.Loading:return loadingScreen;case RobotEmotion.Memory:return memoryScreen;case RobotEmotion.Guardian:return guardianScreen;case RobotEmotion.Courage:return courageScreen;default:return null;}}
        [Serializable] public struct FrameAnchor { public Sprite body; public Vector2 neck; }
        public FrameAnchor[] frames;
        public Vector2 fallbackNeck = new Vector2(0, .9f);
        public Sprite GetHead(RobotEmotion emotion)
        {
            switch(emotion) {
                case RobotEmotion.Joy: return joy;
                case RobotEmotion.Anger: return anger;
                case RobotEmotion.Sadness: return sadness;
                case RobotEmotion.Delight: return delight;
                case RobotEmotion.Blank: return neutral ? neutral : blank;
                case RobotEmotion.Loading: return loading ? loading : blank;
                case RobotEmotion.Guardian: return guardian ? guardian : blank;
                case RobotEmotion.Memory: return memory ? memory : blank;
                case RobotEmotion.StrongAnger: return strongAnger ? strongAnger : anger;
                case RobotEmotion.Courage: return courage ? courage : (delight ? delight : joy);
                default: return null;
            }
        }
        public Vector2 GetNeck(Sprite body)
        {
            if(frames != null) foreach(var frame in frames) if(frame.body == body) return frame.neck;
            return fallbackNeck;
        }
    }
}
