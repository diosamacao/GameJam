using System;
using UnityEngine;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Level Flow")]
    public sealed class LevelFlowConfig : ScriptableObject
    {
        [HideInInspector] public string laboratoryScene="Level01_InitialLab";
        [Header("换关黑幕（秒，使用真实时间）")]
        [Min(0)] public float fadeOutSeconds=.35f;
        [Min(0)] public float fadeInSeconds=.35f;
        [Min(0)] public float blackHoldSeconds=.1f;
        [Serializable] public sealed class Route
        {
            public string fromScene;
            public string nextScene;
            // Legacy serialized reference retained for compatibility; direct transitions do not use it.
            [HideInInspector]
            public DialogueSequence laboratoryDialogue;
        }
        public Route[] routes=new Route[0];
        public Route Find(string scene){if(routes!=null)foreach(var route in routes)if(route!=null && route.fromScene==scene)return route;return null;}
    }
}
