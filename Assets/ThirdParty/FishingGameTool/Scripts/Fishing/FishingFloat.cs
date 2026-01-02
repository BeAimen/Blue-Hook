using FishingGameTool.CustomAttribute;
using FishingGameTool.Fishing.Loot;
using FishingGameTool.Fishing.LootData;
using System.Collections.Generic;
using UnityEngine;

namespace FishingGameTool.Fishing.Float
{
    public enum SubstrateType
    {
        Water,
        Land,
        InAir
    };


    [AddComponentMenu("Fishing Game Tool/Fishing Float")]
    [RequireComponent(typeof(Rigidbody))]
    public class FishingFloat : MonoBehaviour
    {
        [System.Serializable]
        public class FishingFloatAnimationSettings
        {
            [InfoBox("Settings for additional float animation in the water, based on an Animation Curve. " +
                "An object representing the float must be placed as a child within the main float object for it to function properly.")]
            public Transform _floatRepresentation;
            public AnimationCurve _floatAnimationCurve;
            public float _animForce = 0.1f;
            public float _animSpeed = 0.3f;
        }

        public LayerMask _fishingFloatLayerMask;
        public float _checkerRadius = 0.05f;

        [Space, AddButton("Enable Float Animations", "_enableFloatAnim")]
        public bool _enableFloatAnim = false;

        [ShowVariable("_enableFloatAnim")]
        public FishingFloatAnimationSettings _fishingFloatAnimationSettings;

        #region PRIVATE VARIABLES

        private GameObject _waterObject;

        #endregion

        private void Update()
        {
            HandleFloatAnim();
        }

        private void HandleFloatAnim()
        {
            if (!_enableFloatAnim || _fishingFloatAnimationSettings._floatRepresentation == null || _waterObject == null)
            {
                if (_fishingFloatAnimationSettings._floatRepresentation == null)
                    Debug.LogError("No float representation!");

                return;
            }

            float yPosAnimEvolve = _fishingFloatAnimationSettings._floatAnimationCurve.Evaluate(Time.time * _fishingFloatAnimationSettings._animSpeed);
            _fishingFloatAnimationSettings._floatRepresentation.localPosition = new Vector3(0f, yPosAnimEvolve * _fishingFloatAnimationSettings._animForce, 0f);
        }


        /// <summary>
        /// Checks the type of ground on which the float is located.
        /// </summary>
        /// <param name="fishingLayer">The layer mask representing the surfaces to check against.</param>
        /// <returns>The type of ground on which the float is located.</returns>
        public SubstrateType CheckSurface(LayerMask fishingLayer)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, _checkerRadius, ~_fishingFloatLayerMask);
            SubstrateType substrateType;

            if (colliders.Length != 0)
            {
                if ((fishingLayer & (1 << colliders[0].gameObject.layer)) != 0)
                {
                    this.gameObject.GetComponent<Rigidbody>().linearVelocity = new Vector3(0f, this.gameObject.GetComponent<Rigidbody>().linearVelocity.y, 0f);
                    _waterObject = colliders[0].gameObject;
                    substrateType = SubstrateType.Water;

                    return substrateType;
                }
                else
                {
                    substrateType = SubstrateType.Land;

                    return substrateType;
                }
            }
            else
            {
                if(CheckInAir(transform, fishingLayer))
                    substrateType = SubstrateType.Land;
                else
                    substrateType = SubstrateType.InAir;
            }   

            return substrateType;
        }

        /// <summary>
        /// This function returns a list of fishing loot data associated with the water object.
        /// </summary>
        /// <returns>List<FishingLootData> - A list containing fishing loot data associated with the water object.</returns>
        public List<FishingLootData> GetLootDataFormWaterObject()
        {
            List<FishingLootData> lootDataList = _waterObject.GetComponent<FishingLoot>().GetFishingLoot();
            return lootDataList;
        }

        private bool CheckInAir(Transform floatTransform, LayerMask fishingLayer)
        {
            //Specify the distance at which the check is to be performed.
            float checkingDistance = 0.3f;

            Vector3 direction = -floatTransform.up;
            Ray ray = new Ray(floatTransform.position, direction);

            if (Physics.Raycast(ray, checkingDistance, ~fishingLayer))
            {
                return true;
            }

            return false;
        }

#if UNITY_EDITOR

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _checkerRadius);
        }

#endif
    }
}
