using System.IO;
using UnityEngine;
using UnityEditor;
namespace Emergence.Level01.Editor
{
    public static class SetupNpcRoutes
    {
        [MenuItem("Tools/Emergence/Setup NPC Routes And Fragments")]
        public static void Build(){
            string dir=SetupEmotionExpansion.Dir;
            string npcPath=dir+"EmotionNPC.prefab";
            var npc=PrefabUtility.LoadPrefabContents(npcPath);
            try{var body=npc.GetComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Dynamic;body.gravityScale=3;body.constraints=RigidbodyConstraints2D.FreezeRotation;npc.GetComponent<RobotMotor2D>().externallyDriven=false;if(!npc.GetComponent<LocalTimeTarget>())npc.AddComponent<LocalTimeTarget>();PrefabUtility.SaveAsPrefabAsset(npc,npcPath);}finally{PrefabUtility.UnloadPrefabContents(npc);}
            Create(dir+"EmotionFragment.prefab",go=>go.AddComponent<EmotionFragment>());
            Create(dir+"CyclicVanishingBlock.prefab",go=>{var sr=go.AddComponent<SpriteRenderer>();sr.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/EmergenceLevel01/Art/Tiles64/EmotionBlocks/sadness.png");sr.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/EmergenceLevel01/Generated/LabSpriteUnlit.mat");sr.sortingOrder=20;go.AddComponent<BoxCollider2D>();go.AddComponent<EmotionBlock>().kind=EmotionBlockKind.Vanishing;go.AddComponent<CyclicVanishingBlock>();});
            AssetDatabase.SaveAssets();
        }
        static void Create(string path,System.Action<GameObject> setup){if(File.Exists(path))return;var go=new GameObject(Path.GetFileNameWithoutExtension(path));try{setup(go);PrefabUtility.SaveAsPrefabAsset(go,path);}finally{Object.DestroyImmediate(go);}}
    }
}
