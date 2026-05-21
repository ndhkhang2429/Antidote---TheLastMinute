using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;


namespace BFX
{
    public partial class GlobalUpdate : MonoBehaviour
    {
        public static GlobalUpdate          Instance;
        public static HashSet<IScriptInstance> ScriptInstances = new HashSet<IScriptInstance>();



        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RunOnStart()
        {
            Destroy(Instance);
            Instance = null;

            ScriptInstances.Clear();
        }

        public static void CreateInstanceIfRequired()
        {
            if (Instance != null) return;

            var existing = Object.FindObjectsByType<GlobalUpdate>(FindObjectsSortMode.None);
            if (existing.Length > 0)
            {
                Instance = existing[0];

                for (int i = 1; i < existing.Length; i++)
                    Object.Destroy(existing[i].gameObject);
                return;
            }

            var go = new GameObject("BFX_GlobalUpdate")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            Instance = go.AddComponent<GlobalUpdate>();
        }
        
        void Update()
        {
            if (Instance != this) return;

            foreach (var iScriptInstance in ScriptInstances)
            {
                if (iScriptInstance.CanUpdate) iScriptInstance.ManualUpdate();
            }
           
        }

    
        void OnEnable()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Camera.onPreCull    += OnBeforeCameraRendering;
            }
        }

        void OnDisable()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Camera.onPreCull    -= OnBeforeCameraRendering;
            }
           
        }

        private void OnBeforeCameraRendering(Camera cam)
        {
            if (cam.renderingPath == RenderingPath.Forward) cam.depthTextureMode |= DepthTextureMode.Depth;
        }
        
    }
}