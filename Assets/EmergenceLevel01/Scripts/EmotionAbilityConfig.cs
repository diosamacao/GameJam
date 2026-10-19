using UnityEngine;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Emotion Abilities")]
    public sealed class EmotionAbilityConfig : ScriptableObject
    {
        [Header("增强情感解锁（本次游玩跨关卡保留）")]
        public string enhancementUnlockScene="Level03_InitialLab";
        public bool unlockInLaterNumberedLevels=true;
        [Tooltip("跳跃高度最终倍率，默认保留现有1.2倍。")][Min(0)] public float jumpHeightMultiplier=1.2f;
        [Min(.1f)] public float holdSeconds=.5f;
        [Min(0)] public float baseJumpCells=2;
        [Min(0)] public float happyJumpCells=3;
        [Header("短按跳跃最低格数（最终倍率同样生效）")]
        [Min(0)] public float minimumJumpCells=1;
        [Min(0)] public float happyMinimumJumpCells=1.5f;
        [Min(.01f)] public float fallbackCellHeight=1;
    }
}
