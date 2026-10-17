using System.Collections.Generic;
using UnityEngine;
namespace Emergence.Level01
{
    // Session progress survives scene loads, but resets when a new play session begins.
    public static class EmotionProgress
    {
        static readonly HashSet<RobotEmotion> unlocked=new HashSet<RobotEmotion>();
        public static RobotEmotion Current {get;private set;}=RobotEmotion.Blank;
        public static bool IsPlayable(RobotEmotion emotion)=>emotion==RobotEmotion.Joy || emotion==RobotEmotion.Anger || emotion==RobotEmotion.Sadness || emotion==RobotEmotion.Courage || emotion==RobotEmotion.Guardian || emotion==RobotEmotion.Memory;
        public static bool IsUnlocked(RobotEmotion emotion)=>emotion==RobotEmotion.Blank || unlocked.Contains(emotion);
        public static bool Unlock(RobotEmotion emotion)=>IsPlayable(emotion) && unlocked.Add(emotion);
        public static void Remember(RobotEmotion emotion){if(IsUnlocked(emotion))Current=emotion;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] public static void Reset(){unlocked.Clear();Current=RobotEmotion.Blank;}
    }
}
