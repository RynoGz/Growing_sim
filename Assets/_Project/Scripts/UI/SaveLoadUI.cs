using Growveld.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    public sealed class SaveLoadUI : MonoBehaviour
    {
        [SerializeField] private SaveSystem saveSystem;
        [SerializeField] private Text statusText;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button loadButton;

        private void Awake()
        {
            saveButton?.onClick.AddListener(() => saveSystem?.SaveGame(false));
            loadButton?.onClick.AddListener(() => saveSystem?.LoadGame());
        }

        private void OnEnable()
        {
            if (saveSystem != null) saveSystem.SaveStatusChanged += ShowStatus;
            if (loadButton != null) loadButton.interactable = saveSystem != null && saveSystem.HasSave;
            if (statusText != null) statusText.text = "F5 Save  |  F9 Load  |  Autosave every 2 minutes";
        }

        private void OnDisable()
        {
            if (saveSystem != null) saveSystem.SaveStatusChanged -= ShowStatus;
        }

        private void ShowStatus(string message, bool success)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = success ? new Color(0.55f, 1f, 0.62f) : new Color(1f, 0.48f, 0.4f);
            }
            if (loadButton != null) loadButton.interactable = saveSystem != null && saveSystem.HasSave;
        }
    }
}
