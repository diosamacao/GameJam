using System;
using UnityEngine;
namespace Emergence.Level01
{
    [CreateAssetMenu(menuName="Emergence/Level Flow")]
    public sealed class LevelFlowConfig : ScriptableObject
    {
        public string laboratoryScene="Level01_InitialLab";
        [Header("换关黑幕（秒，使用真实时间）")]
        [Min(0)] public float fadeOutSeconds=.35f;
        [Min(0)] public float fadeInSeconds=.35f;
        [Min(0)] public float blackHoldSeconds=.1f;
        [Serializable] public sealed class Route
        {
            public string fromScene;
            public string nextScene;
            [Tooltip("返回实验室时的剧情；留空则不播放。第1关直接进入第2关。")]
            public DialogueSequence laboratoryDialogue;
        }
        public Route[] routes=new Route[0];
        public Route Find(string scene){if(routes!=null)foreach(var route in routes)if(route!=null && route.fromScene==scene)return route;return null;}
    }
}
