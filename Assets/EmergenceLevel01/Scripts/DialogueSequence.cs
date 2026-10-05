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
        [Min(1)] public float charactersPerSecond=30;
        public Line[] lines=new Line[0];
        [TextArea(3,12)] public string stagingNotes;
    }
}
