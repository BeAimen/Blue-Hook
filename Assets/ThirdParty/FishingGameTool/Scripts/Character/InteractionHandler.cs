using FishingGameTool.CustomAttribute;
using UnityEngine;
using UnityEngine.Events;

namespace FishingGameTool.Example
{
    public class InteractionHandler : MonoBehaviour
    {
        public UnityEvent _interactionEvents;

        public void InvokeEvents()
        {
            _interactionEvents.Invoke();
        }
    }
}
