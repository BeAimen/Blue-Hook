#if UNITY_EDITOR
using FishingGameTool.Debuging;
#endif

using FishingGameTool.Fishing.LootData;
using System.Collections.Generic;
using UnityEngine;

namespace FishingGameTool.Fishing.Float
{
    public class PathData
    {
        public Vector3 _pathPoint;
        public Vector3 _waterNormal;
    }

    public class FishingFloatPathfinder
    {
        private List<PathData> _pathData = new List<PathData>();
        private static int _maxPathPoints = 4;
        private bool _initizlizePath = true;
        private Vector3 _smoothedDirection;
        private static float _smoothedSpeed = 7f;

        ///<summary>
        /// Simulates the behavior of a fishing float, controlling its movement and interaction with objects.
        ///</summary>
        ///<param name="lootData">Data representing the loot object, if present.</param>
        ///<param name="fishingFloatTransform">The transform of the fishing float object.</param>
        ///<param name="transformPosition">The position to which the fishing float is attracted.</param>
        ///<param name="maxLineLength">The maximum length of the fishing line.</param>
        ///<param name="finalSpeed">The final speed of the fishing float's movement.</param>
        ///<param name="attractInput">A boolean flag indicating whether attraction input is enabled.</param>
        ///<param name="fishingLayer">The layer representing a fishing spot.</param>
        ///<returns>None.</returns>
        public void FloatBehavior(FishingLootData lootData, Transform fishingFloatTransform, Vector3 transformPosition, float maxLineLength,  
            float finalSpeed, bool attractInput,LayerMask fishingLayer)
        {
            Vector3 fishingFloatPosition = fishingFloatTransform.position;
            float fishingFloatCheckerRadius = fishingFloatTransform.gameObject.GetComponent<FishingFloat>()._checkerRadius;

            Vector3 attractDirection = Vector3.zero;
            Vector3 lootDirection = Vector3.zero;

            if (lootData != null && lootData._lootType == LootType.Fish)
            {
                // Initialize the path data.
                InitializePath(_pathData, _maxPathPoints, fishingFloatPosition, transformPosition, fishingFloatCheckerRadius, maxLineLength, fishingLayer);

                //If the number of points is too small, abort the code.
                if (_pathData.Count < 1)
                    return;

                float distanceBetweenPoints = Vector3.Distance(_pathData[0]._pathPoint, _pathData[1]._pathPoint);
                float distanceToNextPoint = Vector3.Distance(fishingFloatPosition, _pathData[1]._pathPoint);

                if (distanceToNextPoint < (distanceBetweenPoints / 4f))
                {
                    // Remove the first point and set a new path point.
                    _pathData.RemoveAt(0);
                    SetNewPathPoint(_pathData, _maxPathPoints, transformPosition, fishingFloatCheckerRadius, maxLineLength, fishingLayer);
                }

                lootDirection = (_pathData[1]._pathPoint - fishingFloatPosition).normalized;

#if UNITY_EDITOR
                Debuging();
#endif
            }

            if (attractInput)
            {
                attractDirection = AttractDirection(fishingFloatPosition, transformPosition, CheckWaterNormal(fishingFloatPosition, fishingLayer), fishingLayer);
                Debug.DrawRay(fishingFloatPosition, attractDirection, Color.green);
            }

            float attractionStrength = 1.5f;
            Vector3 finalDirection = (lootDirection + attractDirection * attractionStrength).normalized;

            _smoothedDirection = Vector3.Slerp(_smoothedDirection, finalDirection, _smoothedSpeed * Time.deltaTime);

            fishingFloatTransform.Translate(_smoothedDirection * finalSpeed * Time.deltaTime);
        }

        #region AttractDirectionAndAvoidEdgeCollision

        private Vector3 AttractDirection(Vector3 fishingFloatPosition, Vector3 transformPosition, Vector3 waterNormal, LayerMask fishingLayer)
        {
            // Calculate the direction towards the target position, projecting onto the water surface.
            Vector3 direction = Vector3.ProjectOnPlane(new Vector3(transformPosition.x, fishingFloatPosition.y, transformPosition.z) -
               fishingFloatPosition, waterNormal).normalized;

            float distance = Vector3.Distance(fishingFloatPosition, transformPosition);

            RaycastHit hit;
            Ray ray = new Ray(fishingFloatPosition, direction);

            if (Physics.Raycast(ray, out hit, distance + 1f, ~fishingLayer))
            {
                Vector3 dir = (fishingFloatPosition - hit.point).normalized;

                float offsetDistance = 1f;

                Vector3 offsetPoint = hit.point + dir * offsetDistance;
                Vector3 finalDir = offsetPoint - fishingFloatPosition;

                float minDistance = 3f;

                if (distance >= minDistance)
                    return AvoidEdgeCollision(finalDir, fishingFloatPosition, waterNormal, fishingLayer);
                else
                    return finalDir;
            }

            return direction;
        }

        private Vector3 AvoidEdgeCollision(Vector3 direction, Vector3 fishingFloatPosition, Vector3 waterNormal, LayerMask fishingLayer)
        {
            RaycastHit hit;
            Ray ray = new Ray(fishingFloatPosition, direction);

            float checkingDistance = 1f;

            if (Physics.Raycast(ray, out hit, checkingDistance, ~fishingLayer))
            {
                // If there is a collision, adjust the direction to avoid it.
                Vector3 avoidanceDir = Vector3.Cross(hit.normal, Vector3.up).normalized;
                Vector3 adjustedAvoidanceDir = Vector3.ProjectOnPlane(avoidanceDir, waterNormal).normalized;

                float avoidanceFactor = 0.7f;

                Vector3 finalDir = direction + adjustedAvoidanceDir * avoidanceFactor;
                finalDir = finalDir.normalized;

                return finalDir;
            }

            // If no collision, use the original direction.
            return direction;
        }

        #endregion

        #region InitizlizePathAndSetNewPathPoint

        private void InitializePath(List<PathData> pathData, int maxPathPoints, Vector3 fishingFloatPosition, Vector3 transformPosition, 
            float fishingFloatCheckerRadius, float maxLineLength, LayerMask fishingLayer)
        {
            if (!_initizlizePath)
                return;

            if (pathData.Count == 0)
            {
                // Create the initial path point data.
                PathData newPathPointData = new PathData();
                newPathPointData._pathPoint = fishingFloatPosition;
                newPathPointData._waterNormal = CheckWaterNormal(fishingFloatPosition, fishingLayer);

                pathData.Add(newPathPointData);
            }

            for (int i = 0; i < maxPathPoints; i++)
            {
                // Generate new path points based on previous data.
                PathData newPathPointData = GetPathPoint(pathData[i], i > 0 ? pathData[i - 1] : pathData[i], transformPosition, 
                    fishingFloatCheckerRadius, maxLineLength, fishingLayer);
                pathData.Add(newPathPointData);
            }

            _initizlizePath = false;
        }

        private void SetNewPathPoint(List<PathData> pathData, int maxPathPoints, Vector3 transformPosition, float fishingFloatCheckerRadius, 
            float maxLineLength, LayerMask fishingLayer)
        {
            // Generate a new path point based on previous data.
            PathData newPathPointData = GetPathPoint(pathData[maxPathPoints - 1], pathData[maxPathPoints - 2], transformPosition, 
                fishingFloatCheckerRadius, maxLineLength, fishingLayer);
            pathData.Add(newPathPointData);
        }

        #endregion

        #region GetPathPoint

        private PathData GetPathPoint(PathData currentPathData, PathData previousPathData, Vector3 transformPosition,
            float fishingFloatCheckerRadius, float maxLineLength, LayerMask fishingLayer)
        {
            float range = 15f;

            // Generate a random point within the specified range.
            Vector2 newPathPoint = Random.insideUnitCircle * range;
            Vector3 newPathPointOnPlane = new Vector3(newPathPoint.x, 0f, newPathPoint.y);
            Vector3 newPathPointOnWaterVector = Vector3.ProjectOnPlane(newPathPointOnPlane, currentPathData._waterNormal) + currentPathData._pathPoint;

            // Adjust the generated point to the environment to avoid collisions.
            newPathPointOnWaterVector = AdjustPathPointToEnviorment(currentPathData._pathPoint, newPathPointOnWaterVector, fishingLayer);

            // Calculate the angle between the current and new path points.
            Vector3 angleDir = previousPathData._pathPoint - currentPathData._pathPoint;
            Vector3 targetDir = newPathPointOnWaterVector - currentPathData._pathPoint;

            float angle = Mathf.Acos(Vector3.Dot(angleDir.normalized, targetDir.normalized)) * Mathf.Rad2Deg;
            float minAngleBetweenPathPoints = 70f;

            int itteration = 0;
            int maxWhileItteration = 400;

            while ((!CheckPointVisibility(previousPathData._pathPoint, newPathPointOnWaterVector, fishingLayer) ||
                !CheckNewPathPointCorrectness(currentPathData._pathPoint, newPathPointOnWaterVector, transformPosition, 
                fishingFloatCheckerRadius, maxLineLength, fishingLayer) || angle < minAngleBetweenPathPoints) && itteration <= maxWhileItteration)
            {
                newPathPoint = Random.insideUnitCircle * range;
                newPathPointOnPlane = new Vector3(newPathPoint.x, 0f, newPathPoint.y);
                newPathPointOnWaterVector = Vector3.ProjectOnPlane(newPathPointOnPlane, currentPathData._waterNormal) + currentPathData._pathPoint;

                newPathPointOnWaterVector = AdjustPathPointToEnviorment(currentPathData._pathPoint, newPathPointOnWaterVector, fishingLayer);
                targetDir = newPathPointOnWaterVector - currentPathData._pathPoint;
                angle = Mathf.Acos(Vector3.Dot(angleDir.normalized, targetDir.normalized)) * Mathf.Rad2Deg;

                itteration++;
            }

            // Check the water normal at the new path point.
            Vector3 newWaterNormal = CheckWaterNormal(newPathPointOnWaterVector, fishingLayer);

            // Create the new path point data.
            PathData newPathPointData = new PathData();
            newPathPointData._pathPoint = newPathPointOnWaterVector;
            newPathPointData._waterNormal = newWaterNormal;

            return newPathPointData;
        }

        private Vector3 CheckWaterNormal(Vector3 pathPointPosition, LayerMask fishingLayer)
        {
            float checkingDistance = 0.5f;

            RaycastHit hit;
            Ray ray = new Ray(pathPointPosition, Vector3.down);

            if (Physics.Raycast(ray, out hit, checkingDistance, fishingLayer))
                return hit.normal;

            return Vector3.zero;
        }

        private Vector3 AdjustPathPointToEnviorment(Vector3 currentPathPoint, Vector3 newPathPoint, LayerMask fishingLayer)
        {
            RaycastHit hit;

            if(Physics.Linecast(currentPathPoint, newPathPoint, out hit))
            {
                Vector3 direction = (currentPathPoint - hit.point).normalized;

                //Distance in front of the detected obstacle.
                float offsetDistance = 1.5f;

                if ((fishingLayer & (1 << hit.collider.gameObject.layer)) != 0) 
                {
                    offsetDistance = 1f;
                }

                Vector3 offsetPoint = hit.point + direction * offsetDistance;
                return offsetPoint;
            }

            return newPathPoint;
        }

        private bool CheckPointVisibility(Vector3 previousPathPoint, Vector3 newPathPoint, LayerMask fishingLayer)
        {
            if (Physics.Linecast(previousPathPoint, newPathPoint, ~fishingLayer))
                return false;

            return true;
        }

        private bool CheckNewPathPointCorrectness(Vector3 currentPathPoint, Vector3 newPathPoint, Vector3 transformPosition, 
            float fishingFloatCheckerRadius, float maxLineLength, LayerMask fishingLayer)
        {
            float minDistanceToNewPathPoint = 1f;

            float distanceToNewPathPoint = Vector3.Distance(currentPathPoint, newPathPoint);
            float distanceToLineAttachment = Vector3.Distance(newPathPoint, transformPosition);

            float checkingDistance = fishingFloatCheckerRadius + (fishingFloatCheckerRadius * 0.1f);
            bool correctHeight = true;

            if (Physics.Raycast(newPathPoint, Vector3.down, checkingDistance, fishingLayer))
                correctHeight = false;

            bool checking = true;

           if (distanceToNewPathPoint < minDistanceToNewPathPoint || distanceToLineAttachment > maxLineLength || correctHeight)
                checking = false;

            return checking;
        }

        #endregion

        public void ClearPathData()
        {
            _pathData.Clear();
            _initizlizePath = true;
        }

#if UNITY_EDITOR

        private void Debuging()
        {
            List<Vector3> pathPoints = new List<Vector3>();
            for (int i = 0; i < _pathData.Count; i++)
                pathPoints.Add(_pathData[i]._pathPoint);

            DebugUtilities.DrawPath(pathPoints, Color.red, Color.red, new Vector3(0.2f, 0.2f, 0.2f));
        }

#endif
    }
}
