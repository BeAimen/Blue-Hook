using UnityEngine;

namespace FishingGameTool.Example
{
    public class ItemInfo : MonoBehaviour
    {
        public CharacterMovement _characterMovement;

        private void Update()
        {
            transform.rotation = Quaternion.LookRotation(transform.position - _characterMovement.GetCurrentCam().position);
        }
    }
}
