using System;
using UnityEngine;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Dialogue Sequence")]
    public sealed class DialogueSequence : ScriptableObject
    {
        [Serializable] public sealed class Line
        {
            public string speaker;
            [Tooltip("Inline pauses: first sentence.<pause=1>Next sentence. Seconds use a decimal point.")]
            [TextArea(2,8)] public string text;
            [Tooltip("0 inherits the sequence typing speed.")]
            [Min(0)] public float charactersPerSecond;
            [Min(0)] public float minimumDisplaySeconds;
            [Tooltip("Optional identifier for animation/event listeners.")] public string cue;
        }
        public bool blockPlayerInput=true;
        public bool typewriter=true;
        [Min(1)] public float charactersPerSecond=20;
        [Tooltip("文字显示完毕后自动前进，最后一句结束后自动关闭对话。")]
        public bool autoPlay=true;
        [Tooltip("全文显示完毕后的等待秒数，包含手动补全后的等待。")]
        [Min(0)] public float autoAdvanceDelay=2;
        public Line[] lines=new Line[0];
        [TextArea(3,12)] public string stagingNotes;
    }
}
