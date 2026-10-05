using UnityEngine;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Emotion Abilities")]
    public sealed class EmotionAbilityConfig : ScriptableObject
    {
        [Min(.1f)] public float holdSeconds=1;
        [Min(0)] public float baseJumpCells=2;
        [Min(0)] public float happyJumpCells=3;
        [Min(.01f)] public float fallbackCellHeight=1;
    }
}
