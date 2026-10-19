using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    public static class SetupGameAudio
    {
        public const string ConfigPath="Assets/EmergenceLevel01/Resources/GameAudioConfig.asset";
        public const string ClipsPath="Assets/EmergenceLevel01/Audio/Clips/";
        [MenuItem("Tools/Emergence/Setup Game Audio")]
        public static void Build(){
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));AssetDatabase.Refresh();
            foreach(var guid in AssetDatabase.FindAssets("t:AudioClip",new[]{ClipsPath.TrimEnd('/')})){
                var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(AudioImporter)AssetImporter.GetAtPath(path);
                importer.forceToMono=true;
                var settings=importer.defaultSampleSettings;settings.preloadAudioData=true;settings.loadType=AudioClipLoadType.DecompressOnLoad;settings.compressionFormat=AudioCompressionFormat.PCM;
                importer.defaultSampleSettings=settings;importer.SaveAndReimport();
            }
            var config=AssetDatabase.LoadAssetAtPath<GameAudioConfig>(ConfigPath);
            if(config)return; // Preserve the designer's replacements and mix on reruns.
            config=ScriptableObject.CreateInstance<GameAudioConfig>();var cues=new List<GameAudioConfig.Cue>();
            foreach(GameSound sound in Enum.GetValues(typeof(GameSound))){
                var cue=new GameAudioConfig.Cue{sound=sound,clips=new[]{AssetDatabase.LoadAssetAtPath<AudioClip>(ClipsPath+sound+".wav")}};
                switch(sound){
                    case GameSound.Footstep:cue.volume=.2f;cue.priority=220;cue.cooldown=.12f;cue.clips=new[]{cue.clips[0],AssetDatabase.LoadAssetAtPath<AudioClip>(ClipsPath+"Footstep_1.wav"),AssetDatabase.LoadAssetAtPath<AudioClip>(ClipsPath+"Footstep_2.wav")};break;
                    case GameSound.Land:cue.volume=.28f;cue.priority=200;cue.cooldown=.15f;break;
                    case GameSound.DialogueLine:cue.volume=.14f;cue.audibleDistance=0;cue.cooldown=.08f;cue.priority=100;break;
                    case GameSound.Death:cue.volume=.6f;cue.priority=10;cue.cooldown=.15f;break;
                    case GameSound.Resonance:cue.volume=.35f;cue.cooldown=.5f;break;
                    case GameSound.BreakFragile:case GameSound.BreakReinforced:cue.volume=.42f;cue.cooldown=.09f;break;
                    case GameSound.Vanish:case GameSound.Restore:cue.volume=.28f;cue.cooldown=.2f;cue.priority=180;break;
                    case GameSound.MemoryEnter:case GameSound.MemoryReturn:case GameSound.SceneTransition:cue.volume=.45f;cue.audibleDistance=0;cue.priority=20;cue.cooldown=.25f;break;
                    case GameSound.Joy:case GameSound.Anger:case GameSound.Sadness:case GameSound.Courage:case GameSound.Guardian:case GameSound.Memory:cue.volume=.38f;cue.cooldown=.2f;break;
                }
                cues.Add(cue);
            }
            config.cues=cues.ToArray();AssetDatabase.CreateAsset(config,ConfigPath);AssetDatabase.SaveAssets();
        }
    }
}
