using System.Collections;
using Growveld.Economy;
using UnityEngine;
using UnityEngine.UI;

namespace Growveld.UI
{
    /// <summary>
    /// Shows the current Rand balance and short transaction messages.
    /// </summary>
    public sealed class EconomyHUD : MonoBehaviour
    {
        [SerializeField] private EconomyManager economy;
        [SerializeField] private Text moneyText;
        [SerializeField] private Text transactionText;
        [SerializeField, Min(0.1f)] private float messageDuration = 3f;

        private Coroutine hideMessageRoutine;

        private void OnEnable()
        {
            if (economy != null)
            {
                economy.BalanceChanged += UpdateBalance;
                economy.TransactionCompleted += ShowTransaction;
                UpdateBalance(economy.Balance);
            }

            if (transactionText != null)
            {
                transactionText.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (economy != null)
            {
                economy.BalanceChanged -= UpdateBalance;
                economy.TransactionCompleted -= ShowTransaction;
            }
        }

        private void UpdateBalance(float balance)
        {
            if (moneyText == null)
            {
                return;
            }

            moneyText.text = $"R {balance:N0}";
            moneyText.color = balance < 0f
                ? new Color(1f, 0.38f, 0.3f)
                : Color.white;
        }

        private void ShowTransaction(EconomyTransaction transaction)
        {
            if (transactionText == null)
            {
                return;
            }

            string sign = transaction.Amount >= 0f ? "+" : "-";
            transactionText.text = $"{transaction.Reason}\n{sign}R {Mathf.Abs(transaction.Amount):N0}";
            transactionText.color = transaction.Amount >= 0f
                ? new Color(0.45f, 0.9f, 0.5f)
                : new Color(1f, 0.68f, 0.32f);
            transactionText.gameObject.SetActive(true);

            if (hideMessageRoutine != null)
            {
                StopCoroutine(hideMessageRoutine);
            }

            hideMessageRoutine = StartCoroutine(HideMessageAfterDelay());
        }

        private IEnumerator HideMessageAfterDelay()
        {
            yield return new WaitForSeconds(messageDuration);
            if (transactionText != null)
            {
                transactionText.gameObject.SetActive(false);
            }
        }
    }
}
