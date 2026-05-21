using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace BFX
{
    //[ExecuteAlways]
    public class BFX_DecalSettings : IScriptInstance
    {
        public BFX_BloodSettings BloodSettings;
        public Transform parent;
        public float TimeHeightMax = 3.1f;
        public float TimeHeightMin = -0.1f;
        [Space] public Vector3 TimeScaleMax = Vector3.one;
        public Vector3 TimeScaleMin = Vector3.one;
        [Space] public Vector3 TimeOffsetMax = Vector3.zero;
        public Vector3 TimeOffsetMin = Vector3.zero;
        [Space] public AnimationCurve TimeByHeight = AnimationCurve.Linear(0, 0, 1, 1);

        private Vector3 startOffset;
        private Vector3 startScale;
        private float timeDelay;

        Transform t, tParent;
        BFX_ShaderProperies shaderProperies;

        Vector3 averageRay;
        bool isPositionInitialized;
        private Vector3 initializedPosition;
        private DecalProjector decal;

        private void Awake()
        {
            decal = GetComponent<DecalProjector>();
            shaderProperies = GetComponent<BFX_ShaderProperies>();

            // [FIXED] Chặn lỗi nếu Object không có đủ component hoặc chưa gán parent
            if (decal == null || shaderProperies == null || parent == null) return;

            startOffset = transform.localPosition;
            startScale = transform.localScale;
            t = transform;
            tParent = parent.transform;

            shaderProperies.OnAnimationFinished += ShaderCurve_OnAnimationFinished;
        }

        private void ShaderCurve_OnAnimationFinished()
        {
            // [FIXED] Kiểm tra null trước khi tắt
            if (decal != null) decal.enabled = false;
        }

        internal override void ManualUpdate()
        {
            // [FIXED] Dừng chạy vòng lặp nếu thiếu component
            if (decal == null || shaderProperies == null || parent == null) return;

            if (!isPositionInitialized) InitializePosition();
            if (shaderProperies.CanUpdate && initializedPosition.x < float.PositiveInfinity) transform.position = initializedPosition;
        }

        void InitializePosition()
        {
            // [FIXED] Chặn lỗi phát sinh do BloodSettings bị trống
            if (decal == null || parent == null || BloodSettings == null || shaderProperies == null) return;

            decal.enabled = false;

            var currentHeight = parent.position.y;
            float ground = currentHeight;
            if (BloodSettings.AutomaticGroundHeightDetection)
            {
                var raycasts = Physics.RaycastAll(parent.position, Vector3.down, 5);
                foreach (var raycastHit in raycasts)
                {
                    if (raycastHit.point.y < ground) ground = raycastHit.point.y;
                }
            }
            else
            {
                ground = BloodSettings.GroundHeight;
            }

            var currentScale = parent.localScale.y;
            var scaledTimeHeightMax = TimeHeightMax * currentScale;
            var scaledTimeHeightMin = TimeHeightMin * currentScale;

            if (currentHeight - ground >= scaledTimeHeightMax || currentHeight - ground <= scaledTimeHeightMin)
            {
                decal.enabled = false;
            }
            else
            {
                decal.enabled = true;
            }

            float diff = (tParent.position.y - ground) / scaledTimeHeightMax;
            diff = Mathf.Abs(diff);

            var scaleMul = Vector3.Lerp(TimeScaleMin, TimeScaleMax, diff);
            decal.size = new Vector3(scaleMul.x * startScale.x, scaleMul.z * startScale.z, startScale.y);

            var lastOffset = Vector3.Lerp(TimeOffsetMin, TimeOffsetMax, diff);
            t.localPosition = startOffset + lastOffset;
            t.position = new Vector3(t.position.x, ground + 0.05f, t.position.z);


            timeDelay = TimeByHeight.Evaluate(diff);

            shaderProperies.CanUpdate = false;
            Invoke(nameof(EnableDecalAnimation), Mathf.Max(0, timeDelay / BloodSettings.AnimationSpeed));

            isPositionInitialized = true;
        }

        internal override void OnDisableExtended()
        {
            isPositionInitialized = false;
            initializedPosition = Vector3.positiveInfinity;
        }

        internal override void OnEnableExtended()
        {

        }

        void EnableDecalAnimation()
        {
            // [FIXED] Chặn lỗi nếu Object bị hủy trước khi Invoke chạy
            if (shaderProperies != null) shaderProperies.CanUpdate = true;
            initializedPosition = transform.position;
        }

        private void OnDrawGizmosSelected()
        {
            if (t == null) t = transform;
            Gizmos.color = new Color(49 / 255.0f, 136 / 255.0f, 1, 0.03f);
            Gizmos.matrix = Matrix4x4.TRS(t.position, t.rotation, t.lossyScale);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);

            Gizmos.color = new Color(49 / 255.0f, 136 / 255.0f, 1, 0.85f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}