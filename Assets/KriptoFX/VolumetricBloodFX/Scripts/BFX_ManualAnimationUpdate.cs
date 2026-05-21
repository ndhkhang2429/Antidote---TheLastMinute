using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace BFX
{
    [ExecuteAlways]
    public class BFX_ManualAnimationUpdate : IScriptInstance
    {

        public                  BFX_BloodSettings BloodSettings;
        public                  AnimationCurve    AnimationSpeed = AnimationCurve.Linear(0, 0, 1, 1);
        public                  float             FramesCount    = 99;
        public                  float             TimeLimit      = 3;
        public                  float             OffsetFrames   = 0;

        private                 float currentTime;
        
        private static readonly int   UseCustomTime  = Shader.PropertyToID("_UseCustomTime");
        private static readonly int   TimeInFrames   = Shader.PropertyToID("_TimeInFrames");
        private static readonly int   LightIntencity = Shader.PropertyToID("_LightIntencity");
        
        Renderer                      rend;
        private MaterialPropertyBlock propertyBlock;


        internal override void OnEnableExtended()
        {
            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();

                rend = GetComponent<Renderer>();
            }
            

            rend.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(UseCustomTime, 1.0f);
            propertyBlock.SetFloat(TimeInFrames,  0.0f);
            rend.SetPropertyBlock(propertyBlock);

            currentTime = 0;
        }


        internal override void OnDisableExtended()
        {
            
        }

        internal override void ManualUpdate()
        {
            if (!Application.isPlaying) currentTime = BloodSettings.DebugAnimationTime;
            else
            {
                currentTime += Time.deltaTime * BloodSettings.AnimationSpeed;

                if (currentTime / TimeLimit > 1.0)
                {
                    if (rend.enabled) rend.enabled = false;
                    CanUpdate = false;
                    return;
                }
            }

            var currentFrameTime = AnimationSpeed.Evaluate(currentTime / TimeLimit);
            currentFrameTime = currentFrameTime * FramesCount + OffsetFrames + 1.1f;
            float timeInFrames = (Mathf.Ceil(-currentFrameTime) / (FramesCount + 1)) + (1.0f / (FramesCount + 1));

            rend.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(LightIntencity, Mathf.Clamp(BloodSettings.LightIntensityMultiplier, 0.01f, 1f));
            propertyBlock.SetFloat(TimeInFrames,   timeInFrames);
            rend.SetPropertyBlock(propertyBlock);
        }
    }
}