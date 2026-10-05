using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyRobotEmotions
    {
        const string Key="Emergence.EmotionVerification";
        static readonly List<string> results=new List<string>();
        static RobotEmotionController controller;
        static RobotMotor2D motor;
        static float time;
        static int phase; static bool previousBackground;
        static Sprite firstWalk;
        static VerifyRobotEmotions() { EditorApplication.playModeStateChanged+=Mode; }
        [MenuItem("Tools/Emergence/Verify Robot Emotions")]
        public static void Run() {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play mode first");
            SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;
        }
        static void Mode(PlayModeStateChange state) {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)) {
                previousBackground=Application.runInBackground;Application.runInBackground=true;results.Clear();phase=0;time=Time.time;EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
        }
        static void Check(bool condition,string description) { if(!condition)throw new Exception(description);results.Add("PASS: "+description); }
        static void Tick() {
            if(!EditorApplication.isPlaying || Time.time-time<.65f)return;
            try {
                if(phase==0) {
                    controller=UnityEngine.Object.FindObjectOfType<RobotEmotionController>();
                    Check(controller && controller.config && controller.headRenderer,"Player prefab has configured expression layer");
                    motor=controller.GetComponent<RobotMotor2D>();motor.GetComponent<RobotKeyboardInput>().enabled=false;
                    Check(controller.CurrentEmotion==RobotEmotion.Blank,"Default blank expression");
                    var keys=new[]{KeyCode.Y,KeyCode.U,KeyCode.I,KeyCode.O,KeyCode.P};
                    int events=0;controller.EmotionChanged+=e=>events++;
                    for(int i=0;i<5;i++) {
                        RobotEmotion emotion;Check(RobotEmotionKeyboardTest.TryMapKey(keys[i],out emotion) && (int)emotion==i,"Key "+keys[i]+" maps to "+(RobotEmotion)i);
                        Check(controller.SetEmotion(emotion) && controller.headRenderer.sprite==controller.config.GetHead(emotion),"API displays "+emotion);
                    }
                    Check(events==5,"Change events fire once per transition");controller.SetEmotion(RobotEmotion.Blank);
                    Check(events==5 && !controller.SetEmotion((RobotEmotion)99) && controller.CurrentEmotion==RobotEmotion.Blank,"Repeated or invalid emotion preserves state");
                    foreach(var frame in controller.config.frames) {
                        controller.bodyRenderer.sprite=frame.body;controller.bodyRenderer.flipX=false;controller.RefreshVisual();
                        Check(Vector2.Distance(controller.headRenderer.transform.localPosition,frame.neck)<.001f,"Head follows frame "+frame.body.name);
                    }
                    controller.SetEmotion(RobotEmotion.Anger);motor.SetMoveInput(-1);firstWalk=motor.walk[0];
                } else if(phase==1) {
                    Check(motor.visual.flipX && controller.headRenderer.flipX,"Body and head turn left together");
                    Check(Array.IndexOf(motor.walk,motor.visual.sprite)>=0,"Walking body animation runs");
                    Check(controller.CurrentEmotion==RobotEmotion.Anger && controller.headRenderer.sprite==controller.config.anger,"Walking does not overwrite emotion");
                    firstWalk=motor.visual.sprite;motor.SetMoveInput(1);
                } else if(phase==2) {
                    Check(!motor.visual.flipX && !controller.headRenderer.flipX,"Body and head turn right together");
                    controller.SetEmotion(RobotEmotion.Delight);motor.SetMoveInput(0);
                } else {
                    Check(motor.visual.sprite==motor.idle && controller.headRenderer.sprite==controller.config.delight,"Idle body retains selected emotion");
                    Check(UnityEngine.Object.FindObjectOfType<AssemblyInteraction>(),"Assembly gameplay remains present");Finish(null);return;
                }
                phase++;time=Time.time;
            } catch(Exception e) {Finish("FAIL: "+e.Message);}
        }
        static void Finish(string failure) {
            if(failure!=null)results.Add(failure);Directory.CreateDirectory("Logs/RobotEmotions");File.WriteAllLines("Logs/RobotEmotions/verification.txt",results);
            Application.runInBackground=previousBackground;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
            Debug.Log(string.Join("\n",results));
        }
    }
}
