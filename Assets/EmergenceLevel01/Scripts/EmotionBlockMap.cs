using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Tilemap))]
    public sealed class EmotionBlockMap : MonoBehaviour
    {
        [Tooltip("接触点向方块内部偏移的世界距离。")][Min(0)] public float contactInset=.02f;
        Tilemap map;
        struct HiddenCell {public TileBase tile;public Color color;public TileFlags flags;public Tile.ColliderType collider;}
        readonly System.Collections.Generic.Dictionary<Vector3Int,HiddenCell> hidden=new System.Collections.Generic.Dictionary<Vector3Int,HiddenCell>();
        public bool IsVanishing(Vector3Int cell)=>hidden.ContainsKey(cell);
        public bool IsFrozenCell(Vector3Int cell){if(!map)map=GetComponent<Tilemap>();return EmotionResonance.InEnhancedRange(map.GetCellCenterWorld(cell),RobotEmotion.Sadness);}
        public bool BeginVanish(Vector3Int cell){if(!map)map=GetComponent<Tilemap>();var tile=map.GetTile(cell);if(!tile || !isActiveAndEnabled || hidden.ContainsKey(cell) || IsFrozenCell(cell))return false;StartCoroutine(Vanish(cell,tile));return true;}
        public bool TryInteractCell(Vector3Int cell,RobotEmotionGameplay player,out bool respawned)
        {
            respawned=false;
            if(!isActiveAndEnabled || !player || !player.CanUseEmotionAbilities || IsFrozenCell(cell))return false;
            if(!map)map=GetComponent<Tilemap>();
            var tile=map.GetTile(cell) as EmotionBlockTile;if(!tile)return false;
            if(hidden.ContainsKey(cell))return false;
            if(tile.kind==EmotionBlockKind.Electric){respawned=player.HandleElectricContact();return respawned;}
            if(!EmotionBlockRules.CanRemove(tile.kind,player.EffectiveEmotion))return false;
            if(tile.kind==EmotionBlockKind.Vanishing)return BeginVanish(cell);
            var renderer=GetComponent<TilemapRenderer>();
            if(renderer)BlockShatterEffect.Spawn(tile.sprite,map.transform.localToWorldMatrix*Matrix4x4.Translate(map.CellToLocalInterpolated((Vector3)cell+map.tileAnchor))*map.GetTransformMatrix(cell),renderer.sharedMaterial,renderer.sortingLayerID,renderer.sortingOrder,map.GetColor(cell)*map.color,shatter);
            GameAudio.Play(tile.kind==EmotionBlockKind.Reinforced?GameSound.BreakReinforced:GameSound.BreakFragile,map.GetCellCenterWorld(cell));map.SetTile(cell,null);RefreshCollision();
            return true;
        }
        [Header("悲伤方块")]
        [Tooltip("触碰后渐隐时长（秒），期间保留碰撞。")][Min(0)] public float fadeSeconds=.5f;
        [Tooltip("渐隐后的不透明度。")][Range(0,1)] public float fadedAlpha=.3f;
        [Tooltip("关闭碰撞后到恢复的时长（秒）。")][Min(0)] public float restoreSeconds=3;
        [Header("破坏表现")]
        public BlockShatterSettings shatter=new BlockShatterSettings();
        System.Collections.IEnumerator Vanish(Vector3Int cell,TileBase tile){
            GameAudio.Play(GameSound.Vanish,map.GetCellCenterWorld(cell));var state=new HiddenCell{tile=tile,color=map.GetColor(cell),flags=map.GetTileFlags(cell),collider=map.GetColliderType(cell)};hidden.Add(cell,state);
            map.SetTileFlags(cell,state.flags & ~TileFlags.LockColor);
            float elapsed=0;var color=state.color;
            while(elapsed<fadeSeconds || IsFrozenCell(cell)){
                if(map.GetTile(cell)!=tile){hidden.Remove(cell);yield break;}
                if(!IsFrozenCell(cell)){elapsed+=Time.deltaTime;color.a=Mathf.Lerp(state.color.a,fadedAlpha,elapsed/Mathf.Max(.001f,fadeSeconds));map.SetColor(cell,color);}yield return null;
            }
            if(map.GetTile(cell)!=tile){hidden.Remove(cell);yield break;}
            color.a=fadedAlpha;map.SetColor(cell,color);map.SetColliderType(cell,Tile.ColliderType.None);RefreshCollision();
            elapsed=0;while(elapsed<restoreSeconds || IsFrozenCell(cell)){if(!IsFrozenCell(cell))elapsed+=Time.deltaTime;yield return null;}if(map.GetTile(cell)==tile)GameAudio.Play(GameSound.Restore,map.GetCellCenterWorld(cell));Restore(cell);
        }
        void Restore(Vector3Int cell){HiddenCell state;if(!hidden.TryGetValue(cell,out state))return;
            if(map && map.GetTile(cell)==state.tile){map.SetTileFlags(cell,TileFlags.None);map.SetColor(cell,state.color);map.SetColliderType(cell,state.collider);map.SetTileFlags(cell,state.flags);RefreshCollision();}hidden.Remove(cell);
        }
        void RefreshCollision(){var collider=GetComponent<TilemapCollider2D>();if(collider)collider.ProcessTilemapChanges();}
        void OnDisable(){StopAllCoroutines();foreach(var cell in new System.Collections.Generic.List<Vector3Int>(hidden.Keys))Restore(cell);}
        public bool TryInteractContact(Vector2 point,Vector2 normal,RobotEmotionGameplay player,out bool respawned)
        {
            if(!map)map=GetComponent<Tilemap>();
            return TryInteractCell(map.WorldToCell(point-normal*contactInset),player,out respawned);
        }
    }
}
