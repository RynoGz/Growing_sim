namespace Growveld.Interaction
{
    /// <summary>
    /// Marker for interactions that should take priority over dropping a carried object.
    /// </summary>
    public interface IHeldObjectReceiver : IInteractable
    {
    }
}
