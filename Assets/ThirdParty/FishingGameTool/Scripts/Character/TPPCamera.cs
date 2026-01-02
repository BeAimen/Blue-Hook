using UnityEngine;
using FishingGameTool.CustomAttribute;

namespace FishingGameTool.Example
{
    public class TPPCamera : MonoBehaviour
    {
        [BetterHeader("TPP Camera Settings", 20), Space]
        public Transform _target;

        [Space, InfoBox("Correction of the camera focal point position.")]
        public Vector3 _posCorrection = new Vector3 { x = 0f, y = 1.5f, z = 0f };

        [Space, InfoBox("Defining the minimum and maximum allowable vertical rotation angles for the camera.")]
        public Vector2 _verticalRotClamp = new Vector2 { x = -40f, y = 70f };

        public float _rotationSpeed = 3f;
        public float _distance = 3f;

        #region PRIVATE VARIABLES

        private float _currentX = 0f;
        private float _currentY = 0f;

        #endregion

        private void LateUpdate()
        {
            _currentX += Input.GetAxis("Mouse X") * _rotationSpeed;
            _currentY -= Input.GetAxis("Mouse Y") * _rotationSpeed;

            _currentY = Mathf.Clamp(_currentY, _verticalRotClamp.x, _verticalRotClamp.y);

            Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -_distance);
            Vector3 pos = new Vector3(_target.position.x + _posCorrection.x, _target.position.y + _posCorrection.y, _target.position.z + _posCorrection.z);

            Vector3 position = rotation * negDistance + pos;

            transform.rotation = rotation;
            transform.position = position;
        }
    }
}
