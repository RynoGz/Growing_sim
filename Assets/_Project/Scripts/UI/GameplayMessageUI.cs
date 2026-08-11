using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Small shared notification surface for gameplay actions that cannot complete.
    /// </summary>
    public sealed class GameplayMessageUI : MonoBehaviour
    {
        private static GameplayMessageUI current;

        [SerializeField] private Text messageText;
        [SerializeField] private CanvasGroup messageGroup;
        [SerializeField, Min(0.1f)] private float visibleSeconds = 2.25f;

        private Coroutine hideRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureInstance()
        {
            if (FindFirstObjectByType<GameplayMessageUI>() != null) return;

            GameObject canvasObject = new("Gameplay Messages", typeof(Canvas), typeof(CanvasScaler));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject panel = new("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
            panel.transform.SetParent(canvasObject.transform, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.18f);
            panelRect.anchorMax = new Vector2(0.5f, 0.18f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(640f, 68f);
            panel.GetComponent<Image>().color = new Color(0.025f, 0.045f, 0.03f, 0.94f);

            GameObject textObject = new("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(16f, 6f);
            textRect.offsetMax = new Vector2(-16f, -6f);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 23;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            GameplayMessageUI instance = canvasObject.AddComponent<GameplayMessageUI>();
            instance.messageText = text;
            instance.messageGroup = panel.GetComponent<CanvasGroup>();
            instance.messageGroup.alpha = 0f;
        }

        private void Awake()
        {
            current = this;
            if (messageGroup != null) messageGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (current == this) current = null;
        }

        public static void Show(string message)
        {
            if (current == null || string.IsNullOrWhiteSpace(message)) return;
            current.ShowMessage(message);
        }

        private void ShowMessage(string message)
        {
            if (messageText != null) messageText.text = message;
            if (messageGroup != null) messageGroup.alpha = 1f;
            if (hideRoutine != null) StopCoroutine(hideRoutine);
            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(visibleSeconds);
            if (messageGroup != null) messageGroup.alpha = 0f;
            hideRoutine = null;
        }
    }
}
