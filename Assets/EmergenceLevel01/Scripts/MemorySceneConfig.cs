using UnityEngine;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Memory Scene")]
    public sealed class MemorySceneConfig : ScriptableObject
    {
        [Tooltip("场景名或 Build Settings 中的场景路径。回忆结束后自动返回原场景。")]
        public string memoryScene="Level01_InitialLab";
        public DialogueSequence dialogue;
        [Tooltip("回忆往返的幕布颜色，默认白色；满屏遮挡时始终完全不透明。")]
        public Color transitionColor=Color.white;
        [Min(0)] public float fadeOutSeconds=.35f, fadeInSeconds=.35f, blackHoldSeconds=.1f;
    }
}
