using System;
using UnityEngine;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Robot Emotion Config")]
    public sealed class RobotEmotionConfig : ScriptableObject
    {
        public Sprite joy, anger, sadness, delight, blank;
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
                case RobotEmotion.Blank: return blank;
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
