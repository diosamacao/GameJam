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
            [TextArea(2,8)] public string text;
            [Min(0)] public float minimumDisplaySeconds;
            [Tooltip("Optional identifier for animation/event listeners.")] public string cue;
        }
        public bool blockPlayerInput=true;
        public Line[] lines=new Line[0];
        [TextArea(3,12)] public string stagingNotes;
    }
}
