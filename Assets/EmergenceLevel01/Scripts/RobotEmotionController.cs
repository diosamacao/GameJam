using System;
using UnityEngine;
namespace Emergence.Level01
{
    [DisallowMultipleComponent]
    public sealed class RobotEmotionController : MonoBehaviour
    {
        public RobotEmotionConfig config;
        public SpriteRenderer bodyRenderer;
        public SpriteRenderer headRenderer;
        [SerializeField] RobotEmotion currentEmotion = RobotEmotion.Blank;
        public RobotEmotion CurrentEmotion => currentEmotion;
        public event Action<RobotEmotion> EmotionChanged;

        void OnEnable() { RefreshVisual(); }
        void LateUpdate() { RefreshVisual(); }
        public bool SetEmotion(RobotEmotion emotion)
        {
            var gameplay=GetComponent<RobotEmotionGameplay>();
            return gameplay && gameplay.isActiveAndEnabled ? gameplay.TrySetEmotion(emotion) : SetVisualEmotion(emotion);
        }
        internal bool SetVisualEmotion(RobotEmotion emotion)
        {
            if(!config || !config.GetHead(emotion)) return false;
            bool changed = emotion != currentEmotion;
            currentEmotion = emotion;
            RefreshVisual();
            if(changed) EmotionChanged?.Invoke(emotion);
            return true;
        }
        // Inspector UnityEvent / external integer configuration entry point.
        public void SetEmotionByIndex(int index) { SetEmotion((RobotEmotion)index); }
        public void RefreshVisual()
        {
            if(!headRenderer || !bodyRenderer) return;
            var head = config ? config.GetHead(currentEmotion) : null;
            headRenderer.enabled = enabled && bodyRenderer.enabled && head;
            if(!head) return;
            headRenderer.sprite = head;
            var neck = config.GetNeck(bodyRenderer.sprite);
            // Head sprite uses bottom-centre pivot and is a child of the body renderer.
            headRenderer.transform.localPosition = new Vector3(bodyRenderer.flipX ? -neck.x : neck.x,
                bodyRenderer.flipY ? -neck.y : neck.y, 0);
            headRenderer.flipX = bodyRenderer.flipX;
            headRenderer.flipY = bodyRenderer.flipY;
            headRenderer.color = bodyRenderer.color;
            headRenderer.sortingLayerID = bodyRenderer.sortingLayerID;
            headRenderer.sortingOrder = bodyRenderer.sortingOrder + 1;
        }
        void OnDisable() { if(headRenderer) headRenderer.enabled = false; }
    }
}
