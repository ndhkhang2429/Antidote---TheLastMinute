using UnityEngine;
using System;
using UnityEngine.Rendering.Universal;

namespace BFX
{
    public class BFX_ShaderProperies : IScriptInstance
    {
        public BFX_BloodSettings BloodSettings;

        public AnimationCurve FloatCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        public float GraphTimeMultiplier = 1, GraphIntensityMultiplier = 1;
        public float TimeDelay = 0;

        bool isFrized;
        private float startTime;

        private static readonly int cutoutPropertyID = Shader.PropertyToID("_Cutout");
        private static readonly int forwardDirPropertyID = Shader.PropertyToID("_DecalForwardDir");
        private static readonly int LightIntencity = Shader.PropertyToID("_LightIntencity");

        float _animationTimeLapsed;
        private float _timeLapsedBeforeFadeout;

        private DecalProjector decal;
        private Material decalMat;

        public event Action OnAnimationFinished;

        private void Awake()
        {
            decal = GetComponent<DecalProjector>();

            // [FIXED] Nếu Object không có DecalProjector, dừng chạy ngay để tránh lỗi Null
            if (decal == null) return;

            decalMat = new Material(decal.material);
            decal.material = decalMat;
        }

        internal override void OnEnableExtended()
        {
            // [FIXED] Chặn lỗi nếu decal hoặc decalMat chưa được khởi tạo
            if (decal == null || decalMat == null) return;

            startTime = Time.time + TimeDelay;

            decal.enabled = true;

            var eval = FloatCurve.Evaluate(0) * GraphIntensityMultiplier;
            decalMat.SetFloat(cutoutPropertyID, eval);
            decalMat.SetVector(forwardDirPropertyID, transform.up);
        }

        internal override void OnDisableExtended()
        {
            // [FIXED] Chặn lỗi khi tắt Object
            if (decalMat == null) return;

            var eval = FloatCurve.Evaluate(0) * GraphIntensityMultiplier;
            decalMat.SetFloat(cutoutPropertyID, eval);

            _animationTimeLapsed = 0;
            _timeLapsedBeforeFadeout = 0;
        }

        internal override void ManualUpdate()
        {
            // [FIXED] Chặn lỗi trong vòng lặp Update
            if (decalMat == null) return;

            var deltaTime = BloodSettings == null ? Time.deltaTime : Time.deltaTime * BloodSettings.AnimationSpeed;
            _timeLapsedBeforeFadeout += deltaTime;

            if (BloodSettings == null || _timeLapsedBeforeFadeout > BloodSettings.DecalLifeTimeSeconds - 15f || (_animationTimeLapsed / GraphTimeMultiplier) < 0.3f)
                _animationTimeLapsed += deltaTime;

            var eval = FloatCurve.Evaluate(_animationTimeLapsed / GraphTimeMultiplier) * GraphIntensityMultiplier;
            decalMat.SetFloat(cutoutPropertyID, eval);

            if (BloodSettings != null) decalMat.SetFloat(LightIntencity, Mathf.Clamp(BloodSettings.LightIntensityMultiplier, 0.01f, 1f));

            if (_animationTimeLapsed >= GraphTimeMultiplier)
            {
                CanUpdate = false;
                OnAnimationFinished?.Invoke();
            }

            decalMat.SetVector(forwardDirPropertyID, transform.up);
        }
    }
}