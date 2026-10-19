using System.Collections.Generic;
using UnityEngine;
namespace Emergence.Level01
{
    // Session progress survives scene loads, but resets when a new play session begins.
    public static class EmotionProgress
    {
        static readonly HashSet<RobotEmotion> unlocked=new HashSet<RobotEmotion>();
        public static bool EnhancementUnlocked {get;private set;}
        public static void UnlockEnhancement(){EnhancementUnlocked=true;}
        public static void CheckEnhancementScene(string scene,string unlockScene,bool includeLater){
            if(scene==unlockScene){UnlockEnhancement();return;}
            if(!includeLater || string.IsNullOrEmpty(scene) || string.IsNullOrEmpty(unlockScene))return;
            int end=5;while(end<unlockScene.Length && char.IsDigit(unlockScene[end]))end++;
            int actualEnd=5;while(actualEnd<scene.Length && char.IsDigit(scene[actualEnd]))actualEnd++;
            if(scene.StartsWith("Level") && unlockScene.StartsWith("Level") && end>5 && actualEnd>5 && scene.Substring(actualEnd)==unlockScene.Substring(end) && int.TryParse(scene.Substring(5,actualEnd-5),out int level) && int.TryParse(unlockScene.Substring(5,end-5),out int first) && level>=first)UnlockEnhancement();
        }
        public static RobotEmotion Current {get;private set;}=RobotEmotion.Blank;
        public static bool IsPlayable(RobotEmotion emotion)=>emotion==RobotEmotion.Joy || emotion==RobotEmotion.Anger || emotion==RobotEmotion.Sadness || emotion==RobotEmotion.Courage || emotion==RobotEmotion.Guardian || emotion==RobotEmotion.Memory;
        public static bool IsUnlocked(RobotEmotion emotion)=>emotion==RobotEmotion.Blank || unlocked.Contains(emotion);
        public static bool Unlock(RobotEmotion emotion)=>IsPlayable(emotion) && unlocked.Add(emotion);
        public static void Remember(RobotEmotion emotion){if(IsUnlocked(emotion))Current=emotion;}
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] public static void Reset(){unlocked.Clear();Current=RobotEmotion.Blank;EnhancementUnlocked=false;}
    }
}
