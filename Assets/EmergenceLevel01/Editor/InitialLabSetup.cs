using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Emergence.Level01.Editor
{
    // Runs only when the explicit request file is present in the project root.
    [InitializeOnLoad]
    public static class InitialLabSetup
    {
        const string Request = "EmergenceLevel01.setup-request";
        static InitialLabSetup() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (!File.Exists(Request)) { EditorApplication.update -= Tick; return; }
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
            EditorApplication.update -= Tick;
            File.Move(Request, Request + ".running");
            try
            {
                BuildLabScene.Build();
                var scene = SceneManager.GetActiveScene();
                var camera = GameObject.Find("Level01_Camera").GetComponent<Camera>();
                camera.aspect = 16f / 9f;
                camera.orthographicSize = 20f / camera.aspect;
                for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
                {
                    var other = SceneManager.GetSceneAt(i);
                    if (other != scene && !other.isDirty && other.path == "Assets/Scenes/SampleScene.unity")
                        EditorSceneManager.CloseScene(other, true);
                }
                EditorSceneManager.SaveScene(scene);
                ValidateAndCapture(scene, camera);
                File.Move(Request + ".running", "EmergenceLevel01.setup-complete");
                Debug.Log("EMERGENCE_SETUP_SUCCESS " + scene.path);
            }
            catch (Exception e)
            {
                File.WriteAllText("EmergenceLevel01.setup-error.txt", e.ToString());
                Debug.LogException(e);
            }
        }
        static void ValidateAndCapture(Scene scene, Camera camera)
        {
            var renderers = UnityEngine.Object.FindObjectsOfType<SpriteRenderer>();
            int missing = 0;
            foreach (var r in renderers) if (r.gameObject.scene == scene && (!r.sprite || !r.sharedMaterial || !r.sharedMaterial.shader)) missing++;
            if (missing != 0) throw new InvalidOperationException("Missing sprite/material: " + missing);
            Directory.CreateDirectory("Logs/EmergenceLevel01");
            File.WriteAllText("Logs/EmergenceLevel01/validation.txt",
                "Unity: " + Application.unityVersion + "\nScene: " + scene.path + "\nSprite renderers: " + renderers.Length +
                "\nMissing sprites/materials: " + missing + "\nDoor: " + (UnityEngine.Object.FindObjectOfType<LabDoor>() != null) +
                "\nData paths: " + UnityEngine.Object.FindObjectsOfType<LabDataPacket>().Length + "\nChambers: " + UnityEngine.Object.FindObjectsOfType<LabChamber>().Length);
            var rt = new RenderTexture(1280, 720, 24);
            var previous = RenderTexture.active;
            var previousTarget = camera.targetTexture;
            var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                File.WriteAllBytes("Logs/EmergenceLevel01/unity-preview.png",image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget;RenderTexture.active = previous;
                UnityEngine.Object.DestroyImmediate(image);rt.Release();UnityEngine.Object.DestroyImmediate(rt);
            }
        }
    }
}
