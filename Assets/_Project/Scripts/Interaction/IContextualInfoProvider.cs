namespace Growveld.Interaction
{
    /// <summary>
    /// Optional detailed HUD information for the object under the interaction ray.
    /// </summary>
    public interface IContextualInfoProvider
    {
        string ContextualInfo { get; }
    }
}
