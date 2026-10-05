using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyEmotionGameplay
    {
        const string Key="Emergence.EmotionGameplayVerification";
        static RobotMotor2D motor;static RobotEmotionGameplay game;static RobotEmotionController face;static Rigidbody2D body;
        static int phase;static float started,baseline,peak;static bool airborne;static GameObject floor,block;static EmotionBreakable breakable;
        static readonly List<string> results=new List<string>();
        static VerifyEmotionGameplay(){EditorApplication.playModeStateChanged+=Mode;}
        [MenuItem("Tools/Emergence/Verify Emotion Gameplay")]
        public static void Run(){if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop play first");SessionState.SetBool(Key,true);SessionState.SetBool(Key+"Background",Application.runInBackground);EditorApplication.isPlaying=true;}
        static void Mode(PlayModeStateChange state){
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false)){
                Application.runInBackground=true;results.Clear();phase=0;started=Time.time;EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Key,false)){Application.runInBackground=SessionState.GetBool(Key+"Background",false);SessionState.SetBool(Key,false);}
        }
        static void Check(bool ok,string text){if(!ok)throw new Exception(text);results.Add("PASS: "+text);}
        static void Next(){phase++;started=Time.time;}
        static void Tick(){try{
            if(Time.time-started>8)throw new Exception("Timeout at phase "+phase);
            if(phase==0){
                game=UnityEngine.Object.FindObjectOfType<RobotEmotionGameplay>();Check(game && game.abilities,"Player configured");
                motor=game.GetComponent<RobotMotor2D>();face=game.GetComponent<RobotEmotionController>();body=game.GetComponent<Rigidbody2D>();
                motor.GetComponent<RobotKeyboardInput>().enabled=false;game.keyboardInput=false;game.showSwitchProgress=false;
                Check(!game.GetComponent<RobotEmotionKeyboardTest>().enableKeyboardTest,"YUIOP debug bypass disabled");
                game.levelGrid=null;game.abilities=UnityEngine.Object.Instantiate(game.abilities);game.abilities.fallbackCellHeight=1;
                floor=new GameObject("VerificationFloor");floor.transform.position=new Vector3(1000,-.5f);floor.AddComponent<BoxCollider2D>().size=new Vector2(30,1);
                body.position=new Vector2(1000,.1f);body.velocity=Vector2.zero;Physics2D.SyncTransforms();Next();return;
            }
            if(phase==1){if(Time.time-started<.4f)return;
                Check(motor.IsGrounded,"Grounded fixture");game.TrySetEmotion(RobotEmotion.Blank);
                Check(Mathf.Approximately(motor.EffectiveJumpHeight,2),"Base jump is two cells");
                Check(game.BeginSwitch() && face.CurrentEmotion==RobotEmotion.Blank && motor.ControlsLocked,"Q begin blanks head and locks controls");
                motor.SetMoveInput(1);Check(!motor.RequestJump(),"Charge rejects jump");
                Check(!game.ReleaseSwitch() && game.CurrentEmotion==RobotEmotion.Blank && !motor.ControlsLocked,"Early release cancels");
                game.BeginSwitch();Check(!face.SetEmotion(RobotEmotion.Anger),"External switch blocked during charge");Next();return;
            }
            if(phase==2){if(Time.time-started<1.1f)return;
                Check(Mathf.Abs(body.velocity.x)<.01f,"Cannot move while charging");
                Check(!game.IsSwitching && !motor.ControlsLocked && game.CurrentEmotion==RobotEmotion.Joy && face.CurrentEmotion==RobotEmotion.Joy,"One-second hold automatically commits happy and unlocks movement without release");
                Check(Mathf.Approximately(motor.EffectiveJumpHeight,3),"Happy height is three cells");baseline=peak=body.position.y;airborne=false;Check(motor.RequestJump(),"Happy jump accepted");Next();return;
            }
            if(phase==3){peak=Mathf.Max(peak,body.position.y);if(!motor.IsGrounded){airborne=true;CheckOnce(!game.BeginSwitch(),"Cannot initiate switching in air");}
                if(!airborne || !motor.IsGrounded)return;
                Check(Mathf.Abs(peak-baseline-3)<.16f,"Measured happy jump apex is three cells");game.TrySetEmotion(RobotEmotion.Sadness);baseline=peak=body.position.y;airborne=false;Check(motor.RequestJump(),"Sadness retains jump");Next();return;
            }
            if(phase==4){peak=Mathf.Max(peak,body.position.y);if(!motor.IsGrounded)airborne=true;if(!airborne || !motor.IsGrounded)return;
                Check(Mathf.Abs(peak-baseline-2)<.16f,"Measured base jump apex is two cells");
                game.BeginSwitch();game.SendMessage("OnApplicationFocus",false);Check(!game.IsSwitching && game.CurrentEmotion==RobotEmotion.Sadness && !motor.ControlsLocked,"Focus loss cancels and restores previous emotion");
                block=new GameObject("VerificationBreakable");block.transform.position=new Vector3(body.position.x+.85f,.5f);block.AddComponent<BoxCollider2D>().size=Vector2.one;breakable=block.AddComponent<EmotionBreakable>();Physics2D.SyncTransforms();motor.SetMoveInput(1);Next();return;
            }
            if(phase==5){if(Time.time-started<.35f)return;
                Check(block.activeSelf && !breakable.Broken,"Ordinary emotion cannot break touching block");motor.SetMoveInput(0);game.TrySetEmotion(RobotEmotion.Anger);Next();return;
            }
            if(phase==6){if(Time.time-started<.15f)return;
                Check(breakable.Broken && !block.activeSelf,"Switching to anger breaks already touching block");
                var grid=new GameObject("VerificationGrid",typeof(Grid));var mapObject=new GameObject("VerificationTilemap",typeof(Tilemap));mapObject.transform.SetParent(grid.transform);var map=mapObject.GetComponent<Tilemap>();var ability=mapObject.AddComponent<EmotionBreakableTilemap>();var tile=ScriptableObject.CreateInstance<Tile>();
                map.SetTile(Vector3Int.zero,tile);Check(!ability.TryBreakCell(Vector3Int.zero,game),"Unmarked tile protected");ability.breakableTiles=new TileBase[]{tile};map.SetTile(Vector3Int.right,tile);
                Check(ability.TryBreakContact(new Vector2(.001f,.5f),Vector2.left,game) && !map.HasTile(Vector3Int.zero) && map.HasTile(Vector3Int.right),"Tile contact deletes only selected cell");
                Check(floor.activeSelf,"Unmarked ground preserved");
                game.BeginSwitch();Check(!game.CanBreak && !ability.TryBreakCell(Vector3Int.right,game),"Charging suspends anger ability");game.CancelSwitch();
                game.TrySetEmotion(RobotEmotion.Joy);game.BeginSwitch();Next();return;
            }
            if(phase>=7 && phase<=9){
                if(Time.time-started<2.2f)return;
                var expected=phase==7?RobotEmotion.Anger:phase==8?RobotEmotion.Sadness:RobotEmotion.Joy;
                Check(!game.IsSwitching && !motor.ControlsLocked && game.CurrentEmotion==expected,"Automatic cycle advances once and stays unchanged after two seconds: "+expected);
                Check(!game.ReleaseSwitch(),"Repeated release cannot advance cycle phase "+phase);
                game.BeginSwitch();if(phase==9)UnityEngine.Object.Destroy(floor);Next();return;
            }
            if(phase==10){if(Time.time-started<.2f)return;Check(!game.IsSwitching && !motor.ControlsLocked && body.velocity.y<0,"Lost ground cancels switch and continues falling");Finish(null);}
        }catch(Exception e){Finish("FAIL: "+e.Message);}}
        static void CheckOnce(bool ok,string text){if(!results.Contains("PASS: "+text))Check(ok,text);}
        static void Finish(string failure){if(failure!=null)results.Add(failure);Directory.CreateDirectory("Logs/EmotionGameplay");File.WriteAllLines("Logs/EmotionGameplay/verification.txt",results);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;Debug.Log(string.Join("\n",results));}
    }
}
