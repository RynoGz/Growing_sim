using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Displays the short contextual prompt supplied by the current interactable.
    /// </summary>
    public sealed class InteractionPromptUI : MonoBehaviour
    {
        public const int CarryPriority = 100;
        public const int FarmingPriority = 200;
        public const int ConstructionPriority = 300;
        public const int PlacementPriority = 400;

        [SerializeField] private CanvasGroup promptGroup;
        [SerializeField] private Text promptText;

        private readonly Dictionary<Component, PromptEntry> prompts = new();

        public string CurrentMessage => promptText != null && promptGroup != null && promptGroup.alpha > 0f
            ? promptText.text
            : string.Empty;

        private void Awake()
        {
            prompts.Clear();
            SetVisible(false);
        }

        public void SetPrompt(Component owner, string message, int priority)
        {
            if (owner == null || string.IsNullOrWhiteSpace(message))
            {
                ClearPrompt(owner);
                return;
            }

            prompts[owner] = new PromptEntry(message, priority);
            RefreshVisiblePrompt();
        }

        public void ClearPrompt(Component owner)
        {
            if (owner != null) prompts.Remove(owner);
            RefreshVisiblePrompt();
        }

        private void RefreshVisiblePrompt()
        {
            Component selectedOwner = null;
            PromptEntry selected = default;
            List<Component> staleOwners = null;

            foreach (KeyValuePair<Component, PromptEntry> pair in prompts)
            {
                bool ownerDisabled = pair.Key is Behaviour behaviour && !behaviour.isActiveAndEnabled;
                if (pair.Key == null || !pair.Key.gameObject.activeInHierarchy || ownerDisabled)
                {
                    staleOwners ??= new List<Component>();
                    staleOwners.Add(pair.Key);
                    continue;
                }

                if (selectedOwner == null || pair.Value.Priority > selected.Priority)
                {
                    selectedOwner = pair.Key;
                    selected = pair.Value;
                }
            }

            if (staleOwners != null)
            {
                foreach (Component staleOwner in staleOwners) prompts.Remove(staleOwner);
            }

            if (selectedOwner == null)
            {
                SetVisible(false);
                return;
            }

            if (promptText != null) promptText.text = selected.Message;
            SetVisible(true);
        }

        private void SetVisible(bool isVisible)
        {
            if (promptGroup == null)
            {
                return;
            }

            promptGroup.alpha = isVisible ? 1f : 0f;
            promptGroup.interactable = false;
            promptGroup.blocksRaycasts = false;
        }

        private readonly struct PromptEntry
        {
            public PromptEntry(string message, int priority)
            {
                Message = message;
                Priority = priority;
            }

            public string Message { get; }
            public int Priority { get; }
        }
    }
}
