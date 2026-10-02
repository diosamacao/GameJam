using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    [InitializeOnLoad]
    public static class VerifyPlayableLab
    {
        const string Key="Emergence.MovementSmokeTest";
        static RobotMotor2D motor;
        static Rigidbody2D body;
        static float startTime,startX;
        static int phase;
        static bool walkFrameSeen;
        static readonly List<string> results=new List<string>();
        static VerifyPlayableLab(){EditorApplication.playModeStateChanged+=OnPlayMode;}
        [MenuItem("Tools/Emergence/Verify Player Movement")]
        static void Run(){if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop play mode before running verification.");SessionState.SetBool(Key,true);EditorApplication.isPlaying=true;}
        static void OnPlayMode(PlayModeStateChange state)
        {
            if(state==PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Key,false))
            {
                results.Clear();phase=0;walkFrameSeen=false;
                motor=UnityEngine.Object.FindObjectOfType<RobotMotor2D>();
                if(!motor){Finish("FAIL: Player motor missing");return;}
                motor.GetComponent<RobotKeyboardInput>().enabled=false;body=motor.GetComponent<Rigidbody2D>();
                startTime=Time.time;EditorApplication.update+=Tick;
            }
            if(state==PlayModeStateChange.ExitingPlayMode)EditorApplication.update-=Tick;
        }
        static void Check(bool value,string name){if(!value)throw new InvalidOperationException(name);results.Add("PASS: "+name);}
        static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying || !motor)return;
                if(phase==1 && motor.visual.sprite!=motor.idle)walkFrameSeen=true;
                if(Time.time-startTime < (phase==0?.6f:.5f))return;
                switch(phase)
                {
                    case 0:
                        Check(RobotKeyboardInput.AxisFromKeys(true,false)==-1 && RobotKeyboardInput.AxisFromKeys(false,true)==1 && RobotKeyboardInput.AxisFromKeys(true,true)==0 && RobotKeyboardInput.AxisFromKeys(false,false)==0,"A/D, both held and released mappings");
                        Check(Mathf.Abs(body.position.y-1.5f)<.08f,"Ground contact and gravity");
                        startX=body.position.x;motor.SetMoveInput(RobotKeyboardInput.AxisFromKeys(false,true));break;
                    case 1:
                        Check(body.position.x-startX>1.8f && body.position.x-startX<3.2f,"D moves right at configured speed");
                        Check(!motor.visual.flipX && walkFrameSeen,"Right facing and walking sprite frames");
                        startX=body.position.x;motor.SetMoveInput(RobotKeyboardInput.AxisFromKeys(true,false));break;
                    case 2:
                        Check(startX-body.position.x>1.8f && motor.visual.flipX,"A moves left and flips visual");
                        startX=body.position.x;motor.SetMoveInput(0);break;
                    case 3:
                        Check(Mathf.Abs(body.velocity.x)<.01f && Mathf.Abs(body.position.x-startX)<.2f && motor.visual.sprite==motor.idle,"Release stops horizontal motion and restores idle");
                        body.position=new Vector2(1.8f,1.52f);body.velocity=Vector2.zero;motor.SetMoveInput(-1);break;
                    case 4:
                        Check(body.position.x>=1.34f && body.position.x<1.6f,"Left wall blocks movement");
                        body.position=new Vector2(35.4f,1.52f);body.velocity=Vector2.zero;motor.SetMoveInput(1);break;
                    case 5:
                        Check(body.position.x<=35.92f && body.position.x>35.4f,"Closed door blocks movement");
                        Check(Mathf.Abs(body.position.y-1.5f)<.08f && Mathf.Abs(body.rotation)<.01f,"No floor penetration or body rotation");
                        Check(AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/EmergenceLevel01/Prefabs/Equipment","Assets/EmergenceLevel01/Prefabs/Environment","Assets/EmergenceLevel01/Prefabs/Characters"}).Length==16,"16 reusable base prefabs present");
                        Finish(null);return;
                }
                phase++;startTime=Time.time;
            }
            catch(Exception e){Finish("FAIL: "+e.Message);}
        }
        static void Finish(string failure)
        {
            if(failure!=null)results.Add(failure);
            Directory.CreateDirectory("Logs/Level01Movement");File.WriteAllLines("Logs/Level01Movement/playmode-verification.txt",results);
            SessionState.SetBool(Key,false);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;
            if(failure==null)Debug.Log("MOVEMENT_VERIFICATION_PASSED");else Debug.LogError(failure);
        }
    }
}
