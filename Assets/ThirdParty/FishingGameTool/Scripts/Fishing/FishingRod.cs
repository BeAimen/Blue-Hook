using UnityEngine;
using System;
using FishingGameTool.Fishing.Line;
using FishingGameTool.CustomAttribute;

namespace FishingGameTool.Fishing.Rod
{
    [AddComponentMenu("Fishing Game Tool/Fishing Rod")]
    [RequireComponent(typeof(Animator), typeof(LineRenderer))]
    public class FishingRod : MonoBehaviour
    {
        [Serializable]
        public class FishingLineSettings
        {
            public Transform _lineAttachment;
            [InfoBox("This is the range of the number of points for the Line Renderer. It is adjusted based on the distance between the line attachment and the float.")]
            public Vector2 _resolutionRange = new Vector2 { x = 40, y = 10 };
            [Range(-2f, 0f)]
            public float _simulateGravity = -1f;
            [Space]
            public Color _color = new Color32(0, 0, 0, 255);
            public float _width = 0.005f;
        }

        [BetterHeader("Fishing Line Settings", 20)]
        public FishingLineSettings _line;

        [Space, BetterHeader("Fishing Line Status", 20)]
        public FishingLineStatus _lineStatus;
        public bool _isLineBreakable = true;

        [Space, BetterHeader("Fishing Rod Settings", 20)]
        public float _baseAttractSpeed = 5f;
        [InfoBox("Determines the allowable range of bending angles for the fishing rod. It is used to adjust the bending of the rod based on the calculated angles. " +
            "The x-component represents the minimum angle, while the y-component represents the maximum angle.")]
        public Vector2 _angleRange = new Vector2 { x = -110f, y = 110 };

        [Space, AddButton("Show Debug Options", "_showDebugOption")]
        public bool _showDebugOption = false;

        [ShowVariable("_showDebugOption")]
        [Space, BetterHeader("For Debug", 20), InfoBox("The variables below allow you to test the fishing rod during configuration. These variables are modified by the main Fishing System script.")]
        public Transform _fishingFloat;
        [ShowVariable("_showDebugOption")]
        public bool _lootCaught = false;

        #region PRIVATE VARIABLES

        private Animator _animator;
        private LineRenderer _fishingLineRenderer;

        private float _smoothedSimGravity;

        private Vector2 _smoothedBend;

        #endregion

        private void Awake()
        {
            if(_line._lineAttachment == null)
            {
                Debug.LogError("Please add a fishing line attachment!");
                this.enabled = false;
            }

            _animator = GetComponent<Animator>();
            _fishingLineRenderer = GetComponent<LineRenderer>();

            _fishingLineRenderer.startColor = _line._color;
            _fishingLineRenderer.endColor = _line._color;
            _fishingLineRenderer.startWidth = _line._width;
            _fishingLineRenderer.endWidth = _line._width;
        }

        private void Update()
        {
            CalculateBend();
            FishingLine();
        }

        #region CalculateBend

        private void CalculateBend()
        {
            Vector2 bend = Vector2.zero;

            if (_lineStatus._isLineBroken || _fishingFloat == null || !_lootCaught)
                bend = Vector2.zero;
            else
                bend = RemapAngleToBend(CalculateAngles(_fishingFloat.position, transform.position), _angleRange);

            float bendingSpeed = 14f;
            _smoothedBend = Vector2.Lerp(_smoothedBend, bend, Time.deltaTime * bendingSpeed);

            _animator.SetFloat("HorizontalBend", _smoothedBend.x);
            _animator.SetFloat("VerticalBend", _smoothedBend.y);
        }

        private static Vector2 RemapAngleToBend(Vector2 angle, Vector2 angleRange)
        {
            float x = Mathf.InverseLerp(angleRange.x, angleRange.y, angle.x);
            float y = Mathf.InverseLerp(angleRange.x, angleRange.y, angle.y);

            float valueX = Mathf.Lerp(-1f, 1f, x);
            float valueY = Mathf.Lerp(-1f, 1f, y);

            Vector2 bend = new Vector2(-valueX, -valueY);

            return bend;
        }

        private Vector2 CalculateAngles(Vector3 floatPosition, Vector3 position)
        {
            Vector3 dir = floatPosition - position;

            float angleCorrection = 90;
            float angleX = Vector3.Angle(transform.right, dir) - angleCorrection;
            float angleY = Vector3.Angle(transform.up, dir) - angleCorrection;

            Vector2 angel = new Vector2 (angleX, angleY);

            return angel;
        }

        #endregion

        #region FishingLine

        private void FishingLine()
        {
            if (_lineStatus._isLineBroken || _fishingFloat == null)
            {
                _fishingLineRenderer.positionCount = 0;
                return;
            }

            float distance = Vector3.Distance(_line._lineAttachment.position, _fishingFloat.position);
            int resolution = CalculateLineResolution(distance, _line._resolutionRange);

            _lineStatus._currentLineLength = distance;
            _fishingLineRenderer.positionCount = resolution;

            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)resolution;
                Vector3 position = CalculatePointOnCurve(t, _line._lineAttachment.position, _fishingFloat.position, _lootCaught, _line._simulateGravity);
                _fishingLineRenderer.SetPosition(i, position);
            }
        }

        private static int CalculateLineResolution(float distance, Vector2 resolutionRange)
        {
            float minDis = 1f;
            float maxDis = 20f;

            float x = Mathf.InverseLerp(minDis, maxDis, distance);
            float value = Mathf.Lerp(resolutionRange.x, resolutionRange.y, x);

            return (int)value;
        }

        private Vector3 CalculatePointOnCurve(float t, Vector3 attachmentPosition, Vector3 floatPosition, bool lootCaught, float simulateGravity)
        {
            Vector3 pointA = attachmentPosition;
            Vector3 pointB = floatPosition;

            float lineTensionSpeed = 2f;
            _smoothedSimGravity = Mathf.Lerp(_smoothedSimGravity, lootCaught == true ? 0f : simulateGravity, Time.deltaTime * lineTensionSpeed);

            Vector3 controlPoint = Vector3.Lerp(pointA, pointB, 0.5f) + Vector3.up * _smoothedSimGravity;
            Vector3 pointOnCurve = CalculateBezier(pointA, controlPoint, pointB, t, floatPosition);

            return pointOnCurve;
        }

        private Vector3 CalculateBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t, Vector3 floatPosition)
        {
            float u = 1 - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 point = uuu * p0;
            point += 3 * uu * t * p1;
            point += 3 * u * tt * p2;
            point += ttt * floatPosition;

            return point;
        }

        #endregion

        /// <summary>
        /// Calculates the load status of the fishing line.
        /// </summary>
        /// <param name="attractInput">Determines if the fishing line is being attracted.</param>
        /// <param name="lootWeight">It uses the loot weight to calculate the load on the line.</param>
        /// <returns>A FishingLineStatus object representing the current status of the fishing line.</returns>
        public FishingLineStatus CalculateLineLoad(bool attractInput, float lootWeight, int lootTier)
        {
            Vector3 dir = _fishingFloat.position - transform.position;
            float angle = Vector3.Angle(transform.forward, dir);

            angle = angle > _angleRange.y ? _angleRange.y : angle;

            if (attractInput) 
            {
                float loadDecreaseFactor = 4f;
                float calculatedLootWeight = (lootWeight - (lootWeight / lootTier)) <= 0f ? 1f : (lootWeight - (lootWeight / lootTier));

                _lineStatus._currentLineLoad += ((angle * calculatedLootWeight) * Time.deltaTime) / loadDecreaseFactor;
                _lineStatus._currentLineLoad = _lineStatus._currentLineLoad > _lineStatus._maxLineLoad ? _lineStatus._maxLineLoad : _lineStatus._currentLineLoad;
            }
            else
            {
                _lineStatus._currentOverLoad = 0f;
                float loadReduction = 5f;
                _lineStatus._currentLineLoad -= loadReduction * Time.deltaTime;
                _lineStatus._currentLineLoad = _lineStatus._currentLineLoad < 0f ? 0f : _lineStatus._currentLineLoad;
            }

            if (_lineStatus._currentLineLoad == _lineStatus._maxLineLoad)
            {
                _lineStatus._currentOverLoad += Time.deltaTime;

                if (_lineStatus._currentOverLoad >= _lineStatus._overLoadDuration)
                {
                    if(_isLineBreakable)
                        _lineStatus._isLineBroken = true;

                    FishingSystem[] fishingSystem = FindObjectsByType<FishingSystem>(FindObjectsSortMode.None);

                    if (fishingSystem.Length > 1)
                        Debug.LogWarning("There is more than one object on the scene containing the Fishing System component. " +
                            "Please remove the other components containing Fishing System!");
                    else
                        fishingSystem[0].ForceStopFishing();
                }
            }

            _lineStatus._attractFloatSpeed = CalculateAttractSpeed(angle, _angleRange, _lineStatus._currentLineLoad, _lineStatus._maxLineLoad, _baseAttractSpeed, lootTier);

            return _lineStatus;
        }

        private float CalculateAttractSpeed(float angle, Vector2 angleRange, float currentLineLoad, float maxLineLoad, float baseAttractSpeed, int lootTier)
        {
            float normalizeAngle = angle / angleRange.y;
            float attractBonus = CalculateAttractBonus(currentLineLoad, maxLineLoad, lootTier);
            float speedBonus = normalizeAngle * attractBonus;
            float attractSpeed = baseAttractSpeed + speedBonus;

            return attractSpeed;
        }

        private static float CalculateAttractBonus(float currentLineLoad, float maxLineLoad, int lootTier)
        {
            float[] attractBonusMultiplier = { 0.2f, 0.4f, 0.6f, 0.8f, 1.0f };

            float x = Mathf.InverseLerp(0f, maxLineLoad, currentLineLoad);
            float value = Mathf.Lerp(1f, currentLineLoad * attractBonusMultiplier[lootTier], x);

            return value;
        }

        public void LootCaught(bool value)
        {
            _lootCaught = value;
        }

        public void FinishFishing()
        {
            _lineStatus._attractFloatSpeed = 0f;
            _lineStatus._currentLineLoad = 0f;
            _lineStatus._currentOverLoad = 0f;
            _lootCaught = false;
        }
    }
}
