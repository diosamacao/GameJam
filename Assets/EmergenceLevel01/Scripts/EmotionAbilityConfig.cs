using UnityEngine;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Emotion Abilities")]
    public sealed class EmotionAbilityConfig : ScriptableObject
    {
        [Tooltip("跳跃高度最终倍率，默认保留现有1.2倍。")][Min(0)] public float jumpHeightMultiplier=1.2f;
        [Min(.1f)] public float holdSeconds=1;
        [Min(0)] public float baseJumpCells=2;
        [Min(0)] public float happyJumpCells=3;
        [Min(.01f)] public float fallbackCellHeight=1;
    }
}
