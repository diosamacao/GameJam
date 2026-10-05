using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.IO;
namespace Emergence.Level01.Editor
{
    public static class SetupEmotionGameplay
    {
        const string Root="Assets/EmergenceLevel01/";
        [MenuItem("Tools/Emergence/Setup Emotion Gameplay")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop play first");
            string path=Root+"Generated/EmotionAbilities.asset";
            var config=AssetDatabase.LoadAssetAtPath<EmotionAbilityConfig>(path);
            if(!config){config=ScriptableObject.CreateInstance<EmotionAbilityConfig>();AssetDatabase.CreateAsset(config,path);}
            foreach(string prefab in new[]{Root+"Prefabs/Characters/PlayerRobot.prefab",Root+"Generated/Level01_Laboratory.prefab"}){
                var root=PrefabUtility.LoadPrefabContents(prefab);
                try{foreach(var motor in root.GetComponentsInChildren<RobotMotor2D>(true))Configure(motor,config);PrefabUtility.SaveAsPrefabAsset(root,prefab);}
                finally{PrefabUtility.UnloadPrefabContents(root);}
            }
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"})){
                var scenePath=AssetDatabase.GUIDToAssetPath(guid);var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);bool opened=!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
                bool changed=false;
                foreach(var root in scene.GetRootGameObjects())foreach(var motor in root.GetComponentsInChildren<RobotMotor2D>(true)){
                    Configure(motor,config);var gameplay=motor.GetComponent<RobotEmotionGameplay>();
                    foreach(var r in scene.GetRootGameObjects()){var grid=r.GetComponentInChildren<Grid>();if(grid){gameplay.levelGrid=grid;break;}}
                    PrefabUtility.RecordPrefabInstancePropertyModifications(gameplay);
                    var test=motor.GetComponent<RobotEmotionKeyboardTest>();if(test)PrefabUtility.RecordPrefabInstancePropertyModifications(test);
                    changed=true;
                }
                if(changed){EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}
                if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);
            // Distinct orange border uses existing tile art; no replacement of level terrain.
            string dir=Root+"Prefabs/Gameplay";Directory.CreateDirectory(dir);
            var block=new GameObject("AngerBreakableBlock");
            try{
                var sr=block.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Tiles64/solid_fill.png");sr.color=new Color(1,.5f,.25f);sr.sortingOrder=20;
                sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Generated/LabSpriteUnlit.mat");
                block.AddComponent<BoxCollider2D>().size=Vector2.one;block.AddComponent<EmotionBreakable>();
                var line=block.AddComponent<LineRenderer>();line.useWorldSpace=false;line.loop=true;line.positionCount=4;line.SetPositions(new[]{new Vector3(-.46f,-.46f),new Vector3(-.46f,.46f),new Vector3(.46f,.46f),new Vector3(.46f,-.46f)});line.startWidth=line.endWidth=.04f;line.sharedMaterial=sr.sharedMaterial;line.startColor=line.endColor=new Color(1,.5f,.15f);line.sortingOrder=21;
                PrefabUtility.SaveAsPrefabAsset(block,dir+"/AngerBreakableBlock.prefab");
            }finally{Object.DestroyImmediate(block);}
            string tilePath=Root+"Generated/AngerBreakableTile.asset";
            var tile=AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if(!tile){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,tilePath);}
            tile.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Art/Tiles64/solid_fill.png");tile.color=new Color(1,.5f,.25f);tile.colliderType=Tile.ColliderType.Grid;EditorUtility.SetDirty(tile);
            var gridRoot=new GameObject("AngerBreakableTilemap",typeof(Grid));
            try {
                var mapObject=new GameObject("BreakableTiles",typeof(Tilemap),typeof(TilemapRenderer),typeof(TilemapCollider2D),typeof(EmotionBreakableTilemap));mapObject.transform.SetParent(gridRoot.transform,false);
                var map=mapObject.GetComponent<Tilemap>();map.SetTile(Vector3Int.zero,tile);
                var renderer=mapObject.GetComponent<TilemapRenderer>();renderer.sortingOrder=20;renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Generated/LabSpriteUnlit.mat");
                mapObject.GetComponent<EmotionBreakableTilemap>().breakableTiles=new TileBase[]{tile};
                PrefabUtility.SaveAsPrefabAsset(gridRoot,dir+"/AngerBreakableTilemap.prefab");
            }finally{Object.DestroyImmediate(gridRoot);}
            AssetDatabase.SaveAssets();Debug.Log("EMOTION_GAMEPLAY_READY");
        }
        static void Configure(RobotMotor2D motor,EmotionAbilityConfig config){
            var gameplay=motor.GetComponent<RobotEmotionGameplay>();if(!gameplay)gameplay=motor.gameObject.AddComponent<RobotEmotionGameplay>();gameplay.abilities=config;
            var test=motor.GetComponent<RobotEmotionKeyboardTest>();if(test){test.enableKeyboardTest=false;EditorUtility.SetDirty(test);}
            EditorUtility.SetDirty(gameplay);
        }
    }
}
