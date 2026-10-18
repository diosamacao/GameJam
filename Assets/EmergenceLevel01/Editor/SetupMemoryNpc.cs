using System.IO;
using UnityEditor;
using UnityEngine;
namespace Emergence.Level01.Editor
{
    public static class SetupMemoryNpc
    {
        public const string Path="Assets/EmergenceLevel01/Prefabs/EmotionExpansion/MemoryNPC.prefab";
        [MenuItem("Tools/Emergence/Setup Memory NPC")]
        public static void Build(){
            if(File.Exists(Path))return;
            var npc=PrefabUtility.LoadPrefabContents(SetupEmotionExpansion.Dir+"EmotionNPC.prefab");
            try{npc.name="MemoryNPC";npc.AddComponent<MemoryNpcStateController>();foreach(var sr in npc.GetComponentsInChildren<SpriteRenderer>(true)){var c=sr.color;c.a=.5f;sr.color=c;}PrefabUtility.SaveAsPrefabAsset(npc,Path);}finally{PrefabUtility.UnloadPrefabContents(npc);}
        }
    }
}
