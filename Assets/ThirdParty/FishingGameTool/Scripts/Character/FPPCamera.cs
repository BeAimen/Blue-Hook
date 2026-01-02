using FishingGameTool.CustomAttribute;
using UnityEngine;

namespace FishingGameTool.Example
{
    public class FPPCameraSystem : MonoBehaviour
    {
        [BetterHeader("Camera Settings", 20)]
        public Transform _character;
        public Vector2 _sens;
        
        [Space, AddButton("Enable Look Inertia", "_enableLookInertia")]
        public bool _enableLookInertia = false;
        [ShowVariable("_enableLookInertia")] 
        public float _lookInertia = 20f;

        public float _verticalRotationLimit = 85f;

        #region PRIVATE VARIABLES

        private Transform _mainCamera;

        private Vector2 _lookInput;
        private Vector2 _finalCameraRotation;

        private float _verticalCameraRotation;

        #endregion

        private void Awake()
        {
            _mainCamera = transform;
        }

        private void Update()
        {
            HandleCamera();
            HandleInput();
        }

        private void HandleCamera()
        {
            _verticalCameraRotation -= _lookInput.y * _sens.y;
            _verticalCameraRotation = Mathf.Clamp(_verticalCameraRotation, -_verticalRotationLimit, _verticalRotationLimit);

            float horizontalCameraRotation = _lookInput.x * _sens.x;

            if (_enableLookInertia)
                _finalCameraRotation = Vector2.Lerp(_finalCameraRotation, new Vector2(horizontalCameraRotation, _verticalCameraRotation), _lookInertia * Time.deltaTime);
            else
                _finalCameraRotation = new Vector2(horizontalCameraRotation, _verticalCameraRotation);

            _mainCamera.localRotation = Quaternion.Euler(_finalCameraRotation.y, 0f, 0f);
            _character.Rotate(Vector2.up * _finalCameraRotation.x);
        }

        private void HandleInput()
        {
            _lookInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }
    }
}
