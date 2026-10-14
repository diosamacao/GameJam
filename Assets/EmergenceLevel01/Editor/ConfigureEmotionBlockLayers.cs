using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.SceneManagement;
namespace Emergence.Level01.Editor
{
    public static class ConfigureEmotionBlockLayers
    {
        static readonly string[] Names={"Anger","Sadness","Reinforced","Electric"};
        static readonly string[] Files={"anger","sadness","reinforced","electric"};
        const string Root="Assets/EmergenceLevel01/";
        [MenuItem("Tools/Emergence/Configure Emotion Block Layers")]
        public static void Build(){
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new System.Exception("Stop play first");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Generated/LabSpriteUnlit.mat");
            for(int i=0;i<4;i++){
                var tile=AssetDatabase.LoadAssetAtPath<EmotionBlockTile>(SetupEmotionBlocks.Dir+"Tiles/"+Files[i]+".asset");
                string name=Names[i]+"_64x64",path=SetupEmotionBlocks.Dir+"Palettes/"+name+".prefab";
                if(!File.Exists(path))GridPaletteUtility.CreateNewPalette(SetupEmotionBlocks.Dir+"Palettes",name,GridLayout.CellLayout.Rectangle,GridPalette.CellSizing.Manual,Vector3.one,GridLayout.CellSwizzle.XYZ);
                var palette=PrefabUtility.LoadPrefabContents(path);try{var map=palette.GetComponentInChildren<Tilemap>();map.ClearAllTiles();map.SetTile(Vector3Int.zero,tile);map.GetComponent<TilemapRenderer>().sharedMaterial=material;PrefabUtility.SaveAsPrefabAsset(palette,path);}finally{PrefabUtility.UnloadPrefabContents(palette);}
            }
            string templatePath=SetupEmotionBlocks.Dir+"Prefabs/EmotionBlocksGrid.prefab";
            var template=PrefabUtility.LoadPrefabContents(templatePath);try{Split(template.GetComponent<Grid>(),material);PrefabUtility.SaveAsPrefabAsset(template,templatePath);}finally{PrefabUtility.UnloadPrefabContents(template);}
            var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            foreach(var guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/Scenes"})){
                string path=AssetDatabase.GUIDToAssetPath(guid);if(path.Contains("/Backup/") || !Path.GetFileName(path).StartsWith("Level"))continue;
                var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                foreach(var root in scene.GetRootGameObjects())foreach(var grid in root.GetComponentsInChildren<Grid>())Split(grid,material);
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(active.IsValid() && active.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(active);AssetDatabase.SaveAssets();Debug.Log("EMOTION_LAYERS_READY");
        }
        static void Split(Grid grid,Material material){
            var targets=new Tilemap[4];
            for(int i=0;i<4;i++){
                var child=grid.transform.Find("Emotion_"+Names[i]);if(!child){var go=new GameObject("Emotion_"+Names[i],typeof(Tilemap),typeof(TilemapRenderer),typeof(TilemapCollider2D),typeof(EmotionBlockMap));child=go.transform;child.SetParent(grid.transform,false);}
                targets[i]=child.GetComponent<Tilemap>();var renderer=child.GetComponent<TilemapRenderer>();renderer.sharedMaterial=material;renderer.sortingOrder=20;
            }
            foreach(var source in grid.GetComponentsInChildren<Tilemap>()){
                if(source.layoutGrid!=grid)continue;
                var cells=new List<Vector3Int>();foreach(var cell in source.cellBounds.allPositionsWithin)if(source.GetTile(cell) is EmotionBlockTile)cells.Add(cell);
                foreach(var cell in cells){
                    var tile=(EmotionBlockTile)source.GetTile(cell);var dest=targets[(int)tile.kind];if(source==dest)continue;
                    var position=source.GetCellCenterWorld(cell);var targetCell=dest.WorldToCell(position);
                    if(Vector3.Distance(position,dest.GetCellCenterWorld(targetCell))>.001f){Debug.LogWarning("Skipped offset tile: "+source.name+cell);continue;}
                    if(dest.HasTile(targetCell))throw new System.Exception("Emotion layer cell already occupied: "+dest.name+targetCell);
                    var flags=source.GetTileFlags(cell);var color=source.GetColor(cell);var matrix=source.GetTransformMatrix(cell);var collider=source.GetColliderType(cell);
                    dest.SetTile(targetCell,tile);dest.SetTileFlags(targetCell,TileFlags.None);dest.SetColor(targetCell,color);dest.SetTransformMatrix(targetCell,matrix);dest.SetColliderType(targetCell,collider);dest.SetTileFlags(targetCell,flags);source.SetTile(cell,null);
                }
                var collision=source.GetComponent<TilemapCollider2D>();if(collision)collision.ProcessTilemapChanges();
            }
        }
    }
}
