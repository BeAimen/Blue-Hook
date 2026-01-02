using FishingGameTool.CustomAttribute;

#if UNITY_EDITOR
using FishingGameTool.Debuging;
#endif

using UnityEngine;

namespace FishingGameTool.Example
{
    public class InteractionSystem : MonoBehaviour
    {
        [BetterHeader("Interaction System Settings", 20)]
        public float _interactionRadius = 2f;
        public LayerMask _interactionLayerMask;

        [Space, BetterHeader("Interaction UI Settings", 20)]
        public GameObject _interactionMark;

        #region PRIVATE VARIABLES

        private bool _showInteractionMark = false;

        private CharacterMovement _characterMovement;

        #endregion


        private void Awake()
        {
            _characterMovement = GetComponent<CharacterMovement>();
        }

        private void Update()
        {
            HandleInteractionSystem();
        }

        private void HandleInteractionSystem()
        {
#if UNITY_EDITOR

            DebugUtilities.DrawSphere(transform.position, _interactionRadius, Color.yellow);

#endif

            Collider[] colliders = Physics.OverlapSphere(transform.position, _interactionRadius, _interactionLayerMask);

            if(colliders.Length == 0)
                _interactionMark.SetActive(false);

            for(int i = 0; i < colliders.Length; i++)
            {
                _showInteractionMark = ShowInteractionMark(colliders[i].gameObject);

                if (_showInteractionMark)
                {
                    if (Input.GetKeyDown(KeyCode.E))
                        colliders[i].gameObject.GetComponent<InteractionHandler>().InvokeEvents();

                    HandleInteractionMark(_interactionMark, colliders[i].gameObject.transform.position, _showInteractionMark);
                    break;
                }
                else
                    HandleInteractionMark(_interactionMark, colliders[i].gameObject.transform.position, _showInteractionMark);
            }
        }

        private void HandleInteractionMark(GameObject interactionMark, Vector3 interactionObjectPos, bool showMark)
        {
            if (!showMark)
            {
                interactionMark.SetActive(false);
                return;
            }

            interactionMark.transform.position = interactionObjectPos;
            interactionMark.transform.rotation = Quaternion.LookRotation(interactionObjectPos - _characterMovement.GetCurrentCam().position);

            interactionMark.SetActive(true);
        }

        private bool ShowInteractionMark(GameObject interactionObject)
        {
            float minAngleToShow = 25f;

            Vector3 targetDir = interactionObject.transform.position - _characterMovement.GetCurrentCam().position;
            float angle = Vector3.Angle(targetDir, _characterMovement.GetCurrentCam().forward);

            if (angle < minAngleToShow)
                return true;

            return false;
        }
    }
}
