using UnityEngine;
using UnityEngine.InputSystem;

namespace ChulaEarthquakeVR
{
    public sealed class EmergencyStopInput : MonoBehaviour
    {
        [SerializeField] private GameFlowController flow;
        private void Update()
        {
            if (Keyboard.current != null &&
                (Keyboard.current.f12Key.wasPressedThisFrame || Keyboard.current.backspaceKey.wasPressedThisFrame))
                flow?.AbortTutorial("desktop_emergency_stop");
        }
        public void Configure(GameFlowController controller) => flow = controller;
    }
}
