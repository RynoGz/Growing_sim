using Growveld.Interaction;
using UnityEngine;

namespace Growveld.Environment
{
    /// <summary>
    /// Physical room sensor that reveals the exact shared humidity value.
    /// </summary>
    public sealed class HumiditySensor : MonoBehaviour, IInteractable, IContextualInfoProvider
    {
        [SerializeField] private GrowRoomEnvironment room;

        public string InteractionPrompt => "Read humidity sensor";
        public string ContextualInfo => room == null
            ? "Humidity sensor not connected"
            : $"{room.DisplayName}\nHumidity: {room.Humidity:0.0}%\nStatus: {room.HumidityStatus}";

        public bool CanInteract(GameObject interactor)
        {
            return room != null;
        }

        public void Interact(GameObject interactor)
        {
            // Looking at the sensor displays its exact reading in the contextual HUD.
        }
    }
}
