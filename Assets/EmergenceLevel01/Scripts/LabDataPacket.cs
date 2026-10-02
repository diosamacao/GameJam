using UnityEngine;
namespace Emergence.Level01
{
    public sealed class LabDataPacket : MonoBehaviour
    {
        public Vector3[] points;
        public float speed = 3f;
        public float phase;
        float length;
        void Awake() { for (int i = 1; i < points.Length; i++) length += Vector3.Distance(points[i - 1], points[i]); }
        void Update()
        {
            if (length <= 0 || points.Length < 2) return;
            float d = Mathf.Repeat(Time.time * speed + phase * length, length);
            for (int i = 1; i < points.Length; i++)
            {
                float segment = Vector3.Distance(points[i - 1], points[i]);
                if (d <= segment) { transform.localPosition = Vector3.Lerp(points[i - 1], points[i], segment == 0 ? 0 : d / segment); return; }
                d -= segment;
            }
        }
    }
}
