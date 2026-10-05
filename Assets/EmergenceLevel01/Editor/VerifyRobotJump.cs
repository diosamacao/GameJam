using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyRobotJump
    {
        const string Key="Emergence.JumpCheck";
        static RobotMotor2D motor;
        static RobotEmotionController emotion;
        static Rigidbody2D body;
        static int phase; static bool previousBackground;
        static float baseline, peak, started, startX;
        static bool sawJump, sawFall, sawLand;
        static List<string> results=new List<string>();
        static VerifyRobotJump(){EditorApplication.playModeStateChanged+=Mode;}
        [MenuItem("Tools/Emergence/Verify Robot Jump")]
        public static void Run(){if(EditorApplication.isPlaying)throw new Exception("Stop play mode");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void Mode(PlayModeStateChange state){
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){previousBackground=Application.runInBackground;Application.runInBackground=true;sawJump=sawFall=sawLand=false;phase=0;started=Time.time;results.Clear();EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
        }
        static void Check(bool value,string text){if(!value)throw new Exception(text);results.Add("PASS: "+text);}
        static void Tick(){
            if(!EditorApplication.isPlaying)return;
            try {
                if(Time.time-started>12)throw new Exception("Timed out waiting for jump/landing");
                if(phase==0){
                    if(Time.time-started<.7f)return;
                    motor=UnityEngine.Object.FindObjectOfType<RobotMotor2D>();body=motor.GetComponent<Rigidbody2D>();emotion=motor.GetComponent<RobotEmotionController>();
                    motor.GetComponent<RobotKeyboardInput>().enabled=false;
                    Check(motor.IsGrounded,"Standing floor detected");
                    emotion.SetEmotion(RobotEmotion.Joy);var gameplay=motor.GetComponent<RobotEmotionGameplay>();if(gameplay){gameplay.keyboardInput=false;gameplay.levelGrid=null;gameplay.abilities=UnityEngine.Object.Instantiate(gameplay.abilities);gameplay.abilities.fallbackCellHeight=1;gameplay.abilities.happyJumpCells=2;}motor.jumpHeight=2;baseline=body.position.y;peak=baseline;startX=body.position.x;
                    Check(motor.RequestJump() && !motor.RequestJump(),"Grounded jump accepted, duplicate request rejected");motor.SetMoveInput(.2f);phase=1;return;
                }
                peak=Mathf.Max(peak,body.position.y);
                sawJump|=motor.visual.sprite==motor.jump;sawFall|=motor.visual.sprite==motor.fall;sawLand|=motor.visual.sprite==motor.land;
                if(phase==1 && body.velocity.y>1){Check(!motor.RequestJump(),"Airborne double jump rejected");phase=2;}
                if(phase==2 && motor.IsGrounded){
                    Check(Mathf.Abs((peak-baseline)-2)<.16f,"2-unit configured jump reaches expected apex");
                    Check(body.position.x>startX+.3f,"Horizontal movement works in air");
                    Check(sawJump && sawFall,"Ascent and descent sprites displayed");
                    Check(emotion.CurrentEmotion==RobotEmotion.Joy && emotion.headRenderer.sprite==emotion.config.joy,"Jump keeps selected expression");
                    motor.SetMoveInput(0);phase=3;started=Time.time;return;
                }
                if(phase==3 && Time.time-started>.25f){
                    Check(sawLand,"Landing sprite displayed");
                    Check(motor.visual.sprite==motor.idle,"Landing returns to idle");
                    motor.jumpHeight=3;var gameplay=motor.GetComponent<RobotEmotionGameplay>();if(gameplay)gameplay.abilities.happyJumpCells=3;peak=baseline=body.position.y;Check(motor.RequestJump(),"Runtime height change accepts next jump");phase=4;return;
                }
                if(phase==4 && body.velocity.y>1)phase=5;
                if(phase==5 && motor.IsGrounded){
                    Check(Mathf.Abs((peak-baseline)-3)<.18f,"Runtime change to 3 units changes measured apex");
                    motor.jumpHeight=0;var gameplay=motor.GetComponent<RobotEmotionGameplay>();if(gameplay)gameplay.abilities.happyJumpCells=0;Check(!motor.RequestJump(),"Zero height disables jumping");Finish(null);
                }
            }catch(Exception e){Finish("FAIL: "+e.Message);}
        }
        static void Finish(string error){if(error!=null)results.Add(error);Directory.CreateDirectory("Logs/RobotJump");File.WriteAllLines("Logs/RobotJump/verification.txt",results);Application.runInBackground=previousBackground;SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;Debug.Log(string.Join("\n",results));}
    }
}
