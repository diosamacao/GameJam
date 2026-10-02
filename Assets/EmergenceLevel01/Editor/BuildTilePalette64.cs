using System;
using System.IO;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
namespace Emergence.Level01.Editor
{
    public static class BuildTilePalette64
    {
        const string Base="Assets/EmergenceLevel01/";
        const string Dir=Base+"Tilemaps64/";
        static readonly string[] Names={"bg_plain","bg_bolts","bg_vent","bg_light","solid_fill","floor_top","ceiling_bottom","wall_left","wall_right","corner_top_left","corner_top_right","corner_bottom_left","corner_bottom_right","platform_left","platform_middle","platform_right"};
        [MenuItem("Tools/Emergence/Build 64px Tile Palette and Convert Room")]
        public static void Build()
        {
            var scene=SceneManager.GetActiveScene();
            if(Application.isPlaying||scene.path!=Base+"Generated/Level01_InitialLab.unity")throw new InvalidOperationException("Open Level01_InitialLab in edit mode.");
            if(GameObject.Find("LabGrid64")){Debug.Log("64px room already configured; paint the existing Tilemaps.");return;}
            Directory.CreateDirectory("Logs/Tilemap64");EditorSceneManager.SaveScene(scene);
            var stamp=DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");File.Copy(scene.path,"Logs/Tilemap64/scene-before-"+stamp+".unity");
            File.Copy(Base+"Generated/Level01_Laboratory.prefab","Logs/Tilemap64/lab-before-"+stamp+".prefab");
            Directory.CreateDirectory(Dir+"Tiles");Directory.CreateDirectory(Dir+"Palettes");AssetDatabase.Refresh();
            var tiles=new Tile[16];
            for(int i=0;i<Names.Length;i++)
            {
                string png=Base+"Art/Tiles64/"+Names[i]+".png";AssetDatabase.ImportAsset(png,ImportAssetOptions.ForceUpdate);
                var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(png);
                if(!sprite||sprite.rect.width!=64||sprite.rect.height!=64||sprite.pixelsPerUnit!=64)throw new Exception("Invalid 64px sprite: "+png);
                string path=Dir+"Tiles/"+Names[i]+".asset";var tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
                if(!tile){tile=ScriptableObject.CreateInstance<Tile>();AssetDatabase.CreateAsset(tile,path);}
                tile.sprite=sprite;tile.colliderType=i<4?Tile.ColliderType.None:Tile.ColliderType.Grid;tile.color=Color.white;EditorUtility.SetDirty(tile);tiles[i]=tile;
            }
            var material=AssetDatabase.LoadAssetAtPath<Material>(Base+"Generated/LabSpriteUnlit.mat");
            var palette=Palette("Lab_64x64",tiles,0,16,material);
            Palette("Background_64x64",tiles,0,4,material);Palette("Structure_64x64",tiles,4,12,material);
            var root=GameObject.Find("Level01_InitialLaboratory").transform;
            if(PrefabUtility.IsPartOfPrefabInstance(root))PrefabUtility.UnpackPrefabInstance(root.gameObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            var gridObject=new GameObject("LabGrid64");gridObject.transform.SetParent(root,false);gridObject.transform.localPosition=new Vector3(0,.5f,0);var grid=gridObject.AddComponent<Grid>();grid.cellSize=Vector3.one;
            var background=Layer(gridObject.transform,"Background",0,material);var collision=Layer(gridObject.transform,"Collision",20,material);var decoration=Layer(gridObject.transform,"Decoration",1,material);
            for(int y=1;y<=12;y++)for(int x=1;x<=38;x++)background.SetTile(new Vector3Int(x,y,0),tiles[(x%6==0 && y%4==0)?1:0]);
            for(int x=1;x<=38;x++){collision.SetTile(new Vector3Int(x,0,0),tiles[5]);collision.SetTile(new Vector3Int(x,13,0),tiles[6]);}
            for(int y=1;y<=12;y++){collision.SetTile(new Vector3Int(0,y,0),tiles[7]);collision.SetTile(new Vector3Int(39,y,0),tiles[8]);}
            // Interior-facing corners join the floor, ceiling and side strips.
            collision.SetTile(new Vector3Int(0,0,0),tiles[10]);collision.SetTile(new Vector3Int(39,0,0),tiles[9]);
            collision.SetTile(new Vector3Int(0,13,0),tiles[12]);collision.SetTile(new Vector3Int(39,13,0),tiles[11]);
            decoration.SetTile(new Vector3Int(4,10,0),tiles[2]);decoration.SetTile(new Vector3Int(34,10,0),tiles[2]);
            var body=collision.gameObject.AddComponent<Rigidbody2D>();body.bodyType=RigidbodyType2D.Static;
            var composite=collision.gameObject.AddComponent<CompositeCollider2D>();composite.geometryType=CompositeCollider2D.GeometryType.Polygons;
            var collider=collision.gameObject.AddComponent<TilemapCollider2D>();collider.usedByComposite=true;collider.ProcessTilemapChanges();composite.GenerateGeometry();
            var shell=root.Find("LabShell");
            if(shell)
            {
                var props=new GameObject("DecorationProps");props.transform.SetParent(root,false);
                foreach(var sr in shell.GetComponentsInChildren<SpriteRenderer>())if(sr.name=="ceiling_light")
                {var copy=UnityEngine.Object.Instantiate(sr.gameObject,props.transform);copy.name="CeilingLight";copy.transform.position=sr.transform.position;}
                UnityEngine.Object.DestroyImmediate(shell.gameObject);
            }
            var gridPrefab=PrefabUtility.SaveAsPrefabAssetAndConnect(gridObject,Dir+"LabGrid64.prefab",InteractionMode.AutomatedAction);
            // Keep the room editable as ordinary scene Tilemaps; the reusable prefab remains available.
            PrefabUtility.UnpackPrefabInstance(gridObject,PrefabUnpackMode.OutermostRoot,InteractionMode.AutomatedAction);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,Base+"Generated/Level01_Laboratory.prefab");
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            GridPaintingState.palette=palette;GridPaintingState.scenePaintTarget=collision.gameObject;Selection.activeGameObject=collision.gameObject;
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            Validate();Debug.Log("TILEMAP64_READY");
        }
        static Tilemap Layer(Transform parent,string name,int order,Material material)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);var map=go.AddComponent<Tilemap>();var r=go.AddComponent<TilemapRenderer>();r.sharedMaterial=material;r.sortingOrder=order;return map;}
        static GameObject Palette(string name,Tile[] tiles,int first,int count,Material material)
        {
            string path=Dir+"Palettes/"+name+".prefab";var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(!asset)asset=GridPaletteUtility.CreateNewPalette(Dir+"Palettes",name,GridLayout.CellLayout.Rectangle,GridPalette.CellSizing.Manual,Vector3.one,GridLayout.CellSwizzle.XYZ);
            var contents=PrefabUtility.LoadPrefabContents(path);
            try{var map=contents.GetComponentInChildren<Tilemap>();map.ClearAllTiles();map.GetComponent<TilemapRenderer>().sharedMaterial=material;
                for(int i=0;i<count;i++)map.SetTile(new Vector3Int(i%4,-i/4,0),tiles[first+i]);PrefabUtility.SaveAsPrefabAsset(contents,path);}
            finally{PrefabUtility.UnloadPrefabContents(contents);}return AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        [MenuItem("Tools/Emergence/Validate 64px Tilemap")]
        public static void Validate()
        {
            var lines=new System.Collections.Generic.List<string>();
            foreach(var name in Names){var s=AssetDatabase.LoadAssetAtPath<Sprite>(Base+"Art/Tiles64/"+name+".png");if(!s||s.rect.size!=new Vector2(64,64)||s.pixelsPerUnit!=64||s.bounds.size.x!=1)throw new Exception("Tile size mismatch: "+name);}
            lines.Add("PASS: 16 sprites, each 64x64 pixels, PPU64, 1x1 world size");
            foreach(var name in new[]{"Lab_64x64","Background_64x64","Structure_64x64"})
            {string p=Dir+"Palettes/"+name+".prefab";if(!AssetDatabase.LoadAssetAtPath<GameObject>(p)||!AssetDatabase.LoadAssetAtPath<GridPalette>(p))throw new Exception("Missing palette metadata: "+p);}
            lines.Add("PASS: 3 Unity Tile Palettes with GridPalette metadata");
            var grid=GameObject.Find("LabGrid64");if(!grid||grid.GetComponent<Grid>().cellSize!=Vector3.one)throw new Exception("Grid missing or wrong cell size");
            var map=grid.transform.Find("Collision").GetComponent<Tilemap>();var c=map.GetComponent<TilemapCollider2D>();if(!c||!c.usedByComposite)throw new Exception("Collision setup missing");
            foreach(var pos in map.cellBounds.allPositionsWithin)if(map.HasTile(pos)&&map.GetColliderType(pos)!=Tile.ColliderType.Grid)throw new Exception("Non-grid collider in collision layer");
            lines.Add("PASS: tilemap + static rigidbody + composite collider");
            Physics2D.SyncTransforms();var ground=Physics2D.Raycast(new Vector2(22,3),Vector2.down,3);
            if(!ground.collider||Mathf.Abs(ground.point.y-1.5f)>.03f)throw new Exception("Floor height changed");lines.Add("PASS: floor top remains y=1.5");
            if(!UnityEngine.Object.FindObjectOfType<RobotMotor2D>()||!UnityEngine.Object.FindObjectOfType<AssemblyInteraction>())throw new Exception("Gameplay components lost");lines.Add("PASS: player and assembly puzzle preserved");
            Directory.CreateDirectory("Logs/Tilemap64");File.WriteAllLines("Logs/Tilemap64/validation.txt",lines);
        }
    }
}
