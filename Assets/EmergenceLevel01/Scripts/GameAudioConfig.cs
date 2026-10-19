using System;
using UnityEngine;
namespace Emergence.Level01
{
    public enum GameSound { Footstep, Jump, DoubleJump, Land, SwitchStart, Joy, Anger, Sadness, Courage, Guardian, Memory, Transfer, Resonance, Fragment, Death, BreakFragile, BreakReinforced, Vanish, Restore, DoorOpen, DoorClose, SwitchPress, AssemblyPlace, Checkpoint, Portal, DialogueLine, SceneTransition, MemoryEnter, MemoryReturn }
    [CreateAssetMenu(menuName="Emergence/Audio Config")]
    public sealed class GameAudioConfig : ScriptableObject
    {
        [Range(0,1)] public float masterVolume=.65f;
        [Range(1,32)] public int maxVoices=12;
        public bool npcMovementSounds;
        [Min(.05f)] public float footstepInterval=.28f;
        [Serializable] public sealed class Cue
        {
            public GameSound sound;
            public bool enabled=true;
            public AudioClip[] clips=new AudioClip[0];
            [Range(0,1)] public float volume=.5f;
            public Vector2 pitchRange=new Vector2(.96f,1.04f);
            [Min(0)] public float cooldown=.08f;
            [Tooltip("仅衰减该距离以外的场景音效；0 表示全局音效。")]
            [Min(0)] public float audibleDistance=22;
            [Tooltip("数值越小优先级越高；播放数量满时优先替换低优先级声音。")]
            [Range(0,256)] public int priority=128;
        }
        public Cue[] cues=new Cue[0];
        public Cue Find(GameSound sound){if(cues!=null)foreach(var cue in cues)if(cue!=null && cue.sound==sound)return cue;return null;}
    }
}
