using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    public static class SetupRobotJump
    {
        const string Root="Assets/EmergenceLevel01/";
        [MenuItem("Tools/Emergence/Setup Robot Jump")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play mode first.");
            string path=Root+"Prefabs/Characters/PlayerRobot.prefab";
            var prefab=PrefabUtility.LoadPrefabContents(path);
            try {Configure(prefab.GetComponent<RobotMotor2D>());PrefabUtility.SaveAsPrefabAsset(prefab,path);}
            finally {PrefabUtility.UnloadPrefabContents(prefab);}
            foreach(var motor in UnityEngine.Object.FindObjectsOfType<RobotMotor2D>()) {
                Configure(motor);PrefabUtility.RecordPrefabInstancePropertyModifications(motor);
                EditorSceneManager.MarkSceneDirty(motor.gameObject.scene);
            }
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if(scene.path==Root+"Generated/Level01_InitialLab.unity"){
                EditorSceneManager.SaveScene(scene);
                var lab=GameObject.Find("Level01_InitialLaboratory");if(lab)PrefabUtility.SaveAsPrefabAsset(lab,Root+"Generated/Level01_Laboratory.prefab");
            }
            AssetDatabase.SaveAssets();Debug.Log("ROBOT_JUMP_READY");
        }
        static void Configure(RobotMotor2D motor){
            if(!motor)throw new Exception("Motor missing");
            motor.jump=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Characters/RobotModular/body_jump.png");
            motor.fall=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Characters/RobotModular/body_fall.png");
            motor.land=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Characters/RobotModular/body_land.png");
            if(!motor.jump || !motor.fall || !motor.land)throw new Exception("Missing jump animation sprites");
            EditorUtility.SetDirty(motor);
        }
    }
}
