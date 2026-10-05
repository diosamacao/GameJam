using UnityEngine;
using UnityEngine.Tilemaps;
namespace Emergence.Level01
{
    [RequireComponent(typeof(Tilemap))]
    public sealed class EmotionBreakableTilemap : MonoBehaviour
    {
        [Tooltip("Only enable on a separate map containing exclusively destructible tiles.")]
        public bool allTilesBreakable;
        public TileBase[] breakableTiles=new TileBase[0];
        Tilemap map;
        void Awake(){map=GetComponent<Tilemap>();}
        public bool TryBreakCell(Vector3Int cell,RobotEmotionGameplay source)
        {
            if(!source || !source.CanBreak)return false;
            if(!map)map=GetComponent<Tilemap>();
            var tile=map.GetTile(cell);
            if(!tile || (!allTilesBreakable && System.Array.IndexOf(breakableTiles,tile)<0))return false;
            map.SetTile(cell,null);
            var collider=GetComponent<TilemapCollider2D>();if(collider)collider.ProcessTilemapChanges();
            return true;
        }
        public bool TryBreakContact(Vector2 point,Vector2 normal,RobotEmotionGameplay source)
        {
            if(!map)map=GetComponent<Tilemap>();
            // Contact normal points away from the obstacle toward the robot.
            return TryBreakCell(map.WorldToCell(point-normal*.02f),source);
        }
    }
}
