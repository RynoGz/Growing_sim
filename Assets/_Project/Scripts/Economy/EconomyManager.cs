using System;
using UnityEngine;

namespace Growveld.Economy
{
    public readonly struct EconomyTransaction
    {
        public EconomyTransaction(float amount, string reason, float balanceAfter)
        {
            Amount = amount;
            Reason = reason;
            BalanceAfter = balanceAfter;
        }

        public float Amount { get; }
        public string Reason { get; }
        public float BalanceAfter { get; }
    }

    /// <summary>
    /// Owns the farm's Rand balance and is the only service that changes money.
    /// </summary>
    public sealed class EconomyManager : MonoBehaviour
    {
        [SerializeField] private float startingBalance = 30000f;
        [SerializeField] private float balance;
        [SerializeField] private bool initialiseOnAwake = true;

        public event Action<float> BalanceChanged;
        public event Action<EconomyTransaction> TransactionCompleted;

        public float Balance => balance;
        public bool CanMakePurchases => balance >= 0f;

        private void Awake()
        {
            if (initialiseOnAwake && Mathf.Approximately(balance, 0f))
            {
                balance = startingBalance;
            }
        }

        private void Start()
        {
            BalanceChanged?.Invoke(balance);
        }

        public bool CanAfford(float amount)
        {
            return amount >= 0f && CanMakePurchases && balance >= amount;
        }

        public bool TrySpend(float amount, string reason)
        {
            if (!CanAfford(amount))
            {
                return false;
            }

            ApplyTransaction(-amount, reason);
            return true;
        }

        public void Credit(float amount, string reason)
        {
            if (amount <= 0f)
            {
                return;
            }

            ApplyTransaction(amount, reason);
        }

        public void DeductBill(float amount, string reason)
        {
            if (amount <= 0f)
            {
                return;
            }

            ApplyTransaction(-amount, reason);
        }

        public void RestoreBalance(float restoredBalance)
        {
            balance = restoredBalance;
            initialiseOnAwake = false;
            BalanceChanged?.Invoke(balance);
        }

        private void ApplyTransaction(float signedAmount, string reason)
        {
            balance += signedAmount;
            BalanceChanged?.Invoke(balance);
            TransactionCompleted?.Invoke(new EconomyTransaction(signedAmount, reason, balance));
        }
    }
}
