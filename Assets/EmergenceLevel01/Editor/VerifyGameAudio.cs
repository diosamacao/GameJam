using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyGameAudio
    {
        const string Key="Emergence.GameAudioTest";
        static readonly List<string> results=new List<string>();
        static readonly Stack<IEnumerator> stack=new Stack<IEnumerator>();
        static double deadline;
        static VerifyGameAudio(){EditorApplication.playModeStateChanged+=Mode;}
        public static void Batch(){SetupGameAudio.Build();SessionState.SetBool(Key,true);EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);EditorApplication.isPlaying=true;}
        static void Mode(PlayModeStateChange state){
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode){Application.runInBackground=true;results.Clear();stack.Push(Tests());deadline=EditorApplication.timeSinceStartup+120;EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetBool(Key,false);EditorApplication.Exit(SessionState.GetBool(Key+"Success",false)?0:1);}
        }
        static void Tick(){try{
            if(EditorApplication.timeSinceStartup>deadline)throw new Exception("Test timeout");
            if(stack.Count==0){Finish(true);return;}
            var current=stack.Peek();if(!current.MoveNext()){stack.Pop();return;}if(current.Current is IEnumerator nested)stack.Push(nested);
        }catch(Exception ex){results.Add("FAIL: "+ex);Finish(false);}}
        static void Finish(bool success){EditorApplication.update-=Tick;Directory.CreateDirectory("Logs/GameAudio");File.WriteAllLines("Logs/GameAudio/verification.txt",results);SessionState.SetBool(Key+"Success",success);EditorApplication.isPlaying=false;}
        static void Check(bool value,string label){results.Add((value?"PASS: ":"FAIL: ")+label);if(!value)throw new Exception(label);}
        static IEnumerator Wait(float seconds){float end=Time.time+seconds;while(Time.time<end)yield return null;}

        static RobotEmotionGameplay Spawn(bool npc,Vector2 position){
            var actor=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EmergenceLevel01/Prefabs/Characters/PlayerRobot.prefab"),position,Quaternion.identity).GetComponent<RobotEmotionGameplay>();actor.isNpc=npc;actor.requireEmotionUnlocks=false;actor.keyboardInput=false;actor.GetComponent<RobotKeyboardInput>().enabled=false;actor.GetComponent<Rigidbody2D>().simulated=false;return actor;
        }
        static IEnumerator Tests(){
            yield return Wait(.2f);
            var audio=GameAudio.Instance;Check(audio && audio.config,"Persistent audio service loads default configuration");
            var config=UnityEngine.Object.Instantiate(audio.config);audio.config=config;audio.StopAll();
            Check(config.cues.Length==System.Enum.GetValues(typeof(GameSound)).Length,"Every sound event has a configurable entry");
            foreach(var cue in config.cues)foreach(var clip in cue.clips)Check(clip && clip.samples>0 && clip.channels==1,"Imported mono clip: "+cue.sound);
            Check(UnityEngine.Object.FindObjectsOfType<AudioListener>().Length==1,"Missing scene listener automatically supplied");
            var other=new GameObject("External Listener").AddComponent<AudioListener>();audio.RefreshListeners();Check(UnityEngine.Object.FindObjectsOfType<AudioListener>().Length==2 && !audio.GetComponent<AudioListener>().enabled,"Existing listener disables fallback");
            UnityEngine.Object.Destroy(other.gameObject);yield return null;yield return null;Check(audio.GetComponent<AudioListener>().enabled,"Fallback returns after external listener removed");
            var step=config.Find(GameSound.Footstep);step.cooldown=1;
            Check(GameAudio.Play(GameSound.Footstep),"Configured clip accepts playback");Check(!GameAudio.Play(GameSound.Footstep),"Repeated contacts respect cooldown");
            audio.muted=true;Check(!GameAudio.Play(GameSound.Jump),"Global mute rejects new sounds");audio.muted=false;
            var jump=config.Find(GameSound.Jump);jump.enabled=false;Check(!GameAudio.Play(GameSound.Jump),"Individual event can be disabled");jump.enabled=true;
            config.masterVolume=0;Check(!GameAudio.Play(GameSound.Jump),"Master volume zero silences playback");config.masterVolume=.65f;
            var death=config.Find(GameSound.Death);death.cooldown=0;config.maxVoices=3;audio.StopAll();
            for(int i=0;i<3;i++)Check(GameAudio.Play(GameSound.Death),"High priority pooled voice "+i);
            Check(audio.PoolSize==3,"Voice count follows configured hard limit");step.cooldown=0;Check(!GameAudio.Play(GameSound.Footstep),"Footsteps cannot steal active death sounds");
            audio.StopAll();config.maxVoices=12;
            var floor=new GameObject("Floor",typeof(BoxCollider2D));floor.transform.position=new Vector3(1000,-.5f);floor.GetComponent<BoxCollider2D>().size=new Vector2(20,1);
            var player=Spawn(false,new Vector2(1000,0));player.GetComponent<Rigidbody2D>().simulated=true;var motor=player.GetComponent<RobotMotor2D>();
            float timeout=Time.time+3;while(!motor.IsGrounded && Time.time<timeout)yield return null;Check(motor.IsGrounded,"Movement fixture grounded");
            audio.StopAll();int before=audio.PlayCount;Check(motor.RequestJump(),"Grounded jump accepted");yield return Wait(.08f);Check(audio.PlayCount>before && audio.LastSound==GameSound.Jump,"Successful physics jump plays jump event");
            before=audio.PlayCount;Check(!motor.RequestJump(),"Invalid airborne jump rejected");Check(audio.PlayCount==before,"Rejected jump remains silent");
            timeout=Time.time+4;while(!motor.IsGrounded && Time.time<timeout)yield return null;Check(motor.IsGrounded && audio.LastSound==GameSound.Land,"Landing produces impact sound");
            before=audio.PlayCount;motor.SetMoveInput(1);yield return Wait(.65f);motor.SetMoveInput(0);Check(audio.PlayCount>=before+2,"Grounded movement emits paced footsteps");
            yield return Wait(.2f);player.TrySetEmotion(RobotEmotion.Memory);Check(audio.LastSound==GameSound.Memory,"Changing emotion produces distinct memory cue");
            var door=new GameObject("Door").AddComponent<LabDoor>();door.SetOpen(true);Check(audio.LastSound==GameSound.DoorOpen,"Door transition plays open cue");before=audio.PlayCount;door.SetOpen(true);Check(audio.PlayCount==before,"Repeated same door state remains silent");
            var fragment=new GameObject("Fragment").AddComponent<EmotionFragment>();Check(fragment.Collect(player) && audio.LastSound==GameSound.Fragment,"Collectible plays its cue once");before=audio.PlayCount;Check(!fragment.Collect(player) && audio.PlayCount==before,"Already collected fragment remains silent");
            var sequence=ScriptableObject.CreateInstance<DialogueSequence>();sequence.typewriter=false;sequence.lines=new[]{new DialogueSequence.Line{text="Audio check"}};var director=new GameObject("Director").AddComponent<DialogueDirector>();director.TryPlay(sequence);Check(audio.LastSound==GameSound.DialogueLine,"Dialogue line emits quiet interface cue");director.Stop();
            Check(GameAudio.Instance==audio,"All hooks reuse one persistent audio service");
        }
    }
}
