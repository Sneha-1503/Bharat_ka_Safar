using UnityEngine;
using System.Collections;

namespace BharatKaSafar.Rewards
{
    /// <summary>
    /// Centralized Reward System for Coins, Score, and Multipliers.
    /// Place one instance in the gameplay scene.
    /// </summary>
    public class BKS_RewardManager : MonoBehaviour
    {
        public static BKS_RewardManager Instance { get; private set; }

        [Header("Current Session Data")]
        public int currentCoins = 0;
        public int currentScore = 0;

        [Header("Multiplier State")]
        public bool isMultiplierActive = false;
        public int currentMultiplier = 1;
        public float MultiplierTimeLeft { get; private set; }

        // Event for UI to update
        public event System.Action OnRewardsUpdated;
        public event System.Action<bool, float> OnMultiplierChanged; // isActive, duration left

        private Coroutine multiplierCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                currentCoins = 0;
                currentScore = 0;
                currentMultiplier = 1;
                isMultiplierActive = false;
            }
            else
                Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void AddCoin(int amount)
        {
            currentCoins += (amount * currentMultiplier);
            Challenges.BKS_ChallengeStats.Add(Challenges.BKS_Stat.CoinsCollected, amount * currentMultiplier);
            AddScore(amount * 10); // Coins also give score
            OnRewardsUpdated?.Invoke();
        }

        public void AddScore(int amount)
        {
            currentScore += (amount * currentMultiplier);
            OnRewardsUpdated?.Invoke();
        }

        /// <summary>
        /// Start 2X (button or pickup). There is one shared timer: activating while 2X is already
        /// running adds the new duration to the time left instead of starting a second timer.
        /// </summary>
        public void ActivateMultiplier(int multiplierValue, float duration)
        {
            if (isMultiplierActive && multiplierCoroutine != null)
            {
                MultiplierTimeLeft += duration;
                currentMultiplier = Mathf.Max(currentMultiplier, multiplierValue);
                return;
            }
            multiplierCoroutine = StartCoroutine(MultiplierRoutine(multiplierValue, duration));
        }

        private IEnumerator MultiplierRoutine(int value, float duration)
        {
            isMultiplierActive = true;
            currentMultiplier = value;

            MultiplierTimeLeft = duration;
            while (MultiplierTimeLeft > 0)
            {
                OnMultiplierChanged?.Invoke(true, MultiplierTimeLeft);
                yield return null;
                MultiplierTimeLeft -= Time.deltaTime;
            }

            multiplierCoroutine = null;
            isMultiplierActive = false;
            currentMultiplier = 1;
            MultiplierTimeLeft = 0f;
            OnMultiplierChanged?.Invoke(false, 0f);
        }

        // For Checkpoint System
        public void SetState(int savedCoins, int savedScore)
        {
            currentCoins = savedCoins;
            currentScore = savedScore;
            isMultiplierActive = false;
            currentMultiplier = 1;
            if (multiplierCoroutine != null) StopCoroutine(multiplierCoroutine);
            multiplierCoroutine = null;
            MultiplierTimeLeft = 0f;
            OnRewardsUpdated?.Invoke();
            OnMultiplierChanged?.Invoke(false, 0f);
        }
    }
}
