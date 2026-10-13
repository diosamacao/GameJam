using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01
{
    public enum EmotionBlockKind { Fragile, Vanishing, Reinforced, Electric }
    public static class EmotionBlockRules
    {
        public static bool CanRemove(EmotionBlockKind kind,RobotEmotion emotion)
        {
            switch(kind){
                case EmotionBlockKind.Fragile:return emotion==RobotEmotion.Anger || emotion==RobotEmotion.StrongAnger;
                case EmotionBlockKind.Vanishing:return emotion==RobotEmotion.Sadness;
                case EmotionBlockKind.Reinforced:return emotion==RobotEmotion.StrongAnger;
                default:return false;
            }
        }
    }
    [CreateAssetMenu(menuName="Emergence/Emotion Block Tile")]
    public sealed class EmotionBlockTile : Tile
    {
        public EmotionBlockKind kind;
    }
}
