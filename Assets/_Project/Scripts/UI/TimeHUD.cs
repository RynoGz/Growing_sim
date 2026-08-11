using Growveld.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    public sealed class TimeHUD : MonoBehaviour
    {
        [SerializeField] private GameTimeManager gameTime;
        [SerializeField] private Text timeText;

        private void Update()
        {
            if (gameTime == null || timeText == null) return;
            timeText.text = $"Day {gameTime.Day}  |  {gameTime.FormattedTime}  |  {(gameTime.IsDaylight ? "Daylight" : "Night")}";
        }
    }
}
