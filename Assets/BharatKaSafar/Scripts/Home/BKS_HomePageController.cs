using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using BharatKaSafar.Core;

namespace BharatKaSafar.Home
{
    /// <summary>
    /// Central coordinator of the Home page (India Map → Chapter Selection).
    ///
    ///   MapStateButton ──click──► SelectState(id) ──► ChapterDetailPanel.SetData(...)
    ///                                              └► RefreshMapStateButtons()
    ///   ChapterDetailPanel ──action──► EnterSelectedChapter() / TryUnlockSelectedState()
    ///
    /// Data:     ChapterData assets (static config: text, images, cost, scene)
    /// Progress: BKS_SaveManager (unlocked / completed / coins) — never stored here
    /// Loading:  BKS_SceneLoader.LoadChapter()
    ///
    /// Everything is event-driven; nothing runs in Update().
    /// </summary>
    public class BKS_HomePageController : MonoBehaviour
    {
        [Header("State Data")]
        [Tooltip("All chapters/states shown on the map (UP, RJ, TN, PB, ...)")]
        [SerializeField] private List<ChapterData> states = new List<ChapterData>();
        [Tooltip("State selected automatically when the Home page opens")]
        [SerializeField] private string defaultStateId = "TN";

        [Header("Map")]
        [Tooltip("Parent of the state buttons. Used to auto-collect MapStateButtons if the list below is empty.")]
        [SerializeField] private Transform mapButtonsRoot;
        [SerializeField] private List<MapStateButton> mapButtons = new List<MapStateButton>();

        [Header("Panel")]
        [SerializeField] private ChapterDetailPanel chapterPanel;

        [Header("Currency HUD")]
        [SerializeField] private TMP_Text coinText;

        [Header("Feedback")]
        [Tooltip("{0} = coins still needed")]
        [SerializeField] private string notEnoughCoinsFormat = "NOT ENOUGH COINS!\nYou need {0} more.";
        [SerializeField] private string sceneMissingMessage  = "This chapter is not ready yet.";
        [SerializeField] private float  feedbackDuration     = 2f;

        [Header("Events (optional hooks for popups, sounds, analytics)")]
        [Tooltip("Arg: coins still needed")]
        public UnityEvent<int>    onNotEnoughCoins = new UnityEvent<int>();
        [Tooltip("Arg: stateId")]
        public UnityEvent<string> onStateUnlocked  = new UnityEvent<string>();

        // ── Runtime ───────────────────────────────────────────────────────────────
        private readonly Dictionary<string, ChapterData>    _dataById   = new Dictionary<string, ChapterData>(StringComparer.OrdinalIgnoreCase);
        // One state can have more than one button on the map (e.g. two J&K labels).
        private readonly Dictionary<string, List<MapStateButton>> _buttonById = new Dictionary<string, List<MapStateButton>>(StringComparer.OrdinalIgnoreCase);
        private BKS_SaveManager _save;
        private string _selectedStateId;

        public string SelectedStateId => _selectedStateId;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            BuildDataLookup();
            CollectAndInitButtons();
        }

        private void Start()
        {
            // BKS_PersistentManagers (in this scene) creates the SaveManager in its Awake.
            _save = BKS_SaveManager.Instance;
            if (_save == null)
            {
                Debug.LogError("[BKS Home] BKS_SaveManager not found. Add [BKS_PersistentManagers] to the Home scene.", this);
            }
            else
            {
                _save.EnsureDefaultUnlockedStates(states); // Tamil Nadu on a new save; never resets progress
                _save.OnCoinsChanged         += HandleCoinsChanged;
                _save.OnStateProgressChanged += HandleStateProgressChanged;
            }

            if (chapterPanel != null)
                chapterPanel.OnActionClicked += HandleActionClicked;
            else
                Debug.LogError("[BKS Home] ChapterDetailPanel is not assigned.", this);

            RefreshCoinHud();
            SelectState(defaultStateId);
        }

        private void OnDestroy()
        {
            if (_save != null)
            {
                _save.OnCoinsChanged         -= HandleCoinsChanged;
                _save.OnStateProgressChanged -= HandleStateProgressChanged;
            }
            if (chapterPanel != null)
                chapterPanel.OnActionClicked -= HandleActionClicked;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            // Coins can change outside this scene (legacy gameplay) — cheap refresh on resume.
            if (hasFocus) RefreshCoinHud();
        }

        // ── Setup ─────────────────────────────────────────────────────────────────

        private void BuildDataLookup()
        {
            _dataById.Clear();
            foreach (var data in states)
            {
                if (data == null) continue;
                string id = data.ChapterID;
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[BKS Home] ChapterData '{data.name}' has no Chapter ID.", data);
                    continue;
                }
                if (_dataById.ContainsKey(id))
                {
                    Debug.LogWarning($"[BKS Home] Duplicate Chapter ID '{id}' ('{data.name}'). Ignoring the duplicate.", data);
                    continue;
                }
                _dataById.Add(id, data);
            }
        }

        private void CollectAndInitButtons()
        {
            // Auto-collect once (not on every click) if the list was left empty.
            if ((mapButtons == null || mapButtons.Count == 0) && mapButtonsRoot != null)
                mapButtons = new List<MapStateButton>(mapButtonsRoot.GetComponentsInChildren<MapStateButton>(true));

            _buttonById.Clear();
            foreach (var btn in mapButtons)
            {
                if (btn == null) continue;
                string id = btn.StateId;
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"[BKS Home] MapStateButton '{btn.name}' has no State ID.", btn);
                    continue;
                }
                if (!_buttonById.TryGetValue(id, out var list))
                {
                    list = new List<MapStateButton>();
                    _buttonById.Add(id, list);
                }
                if (list.Contains(btn)) continue;
                list.Add(btn);
                btn.Initialize(SelectState);
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>Select a state (works for locked and upcoming states too — preview is always allowed).</summary>
        public void SelectState(string stateId)
        {
            if (string.IsNullOrEmpty(stateId)) return;
            stateId = stateId.Trim();

            if (!_dataById.ContainsKey(stateId) && !_buttonById.ContainsKey(stateId))
            {
                Debug.LogWarning($"[BKS Home] Unknown state '{stateId}'. Add its ChapterData to the controller or a MapStateButton with that ID.", this);
                return;
            }

            _selectedStateId = stateId;
            RefreshSelectedStatePanel();
            RefreshMapStateButtons();
            Debug.Log($"[BKS Home] Selected state: {stateId}");
        }

        /// <summary>Redraw the right-side panel for the selected state.</summary>
        public void RefreshSelectedStatePanel()
        {
            if (chapterPanel == null || string.IsNullOrEmpty(_selectedStateId)) return;

            ChapterData data         = FindData(_selectedStateId);
            StateProgressData progress = GetStateProgress(_selectedStateId);
            ChapterStatus status     = ChapterStatusResolver.Resolve(data, progress);

            chapterPanel.SetData(data, progress, status, GetFallbackName(_selectedStateId));
        }

        /// <summary>Redraw only the status + action button of the panel.</summary>
        public void RefreshActionButton()
        {
            if (chapterPanel == null || string.IsNullOrEmpty(_selectedStateId)) return;

            ChapterData data = FindData(_selectedStateId);
            chapterPanel.RefreshStatus(GetStatus(_selectedStateId), data != null ? data.UnlockCost : 0);
        }

        /// <summary>Redraw every pin on the map (status + selection).</summary>
        public void RefreshMapStateButtons()
        {
            foreach (var pair in _buttonById)
            {
                bool selected = string.Equals(pair.Key, _selectedStateId, StringComparison.OrdinalIgnoreCase);
                ChapterStatus status = GetStatus(pair.Key);
                foreach (var btn in pair.Value)
                    if (btn != null) btn.SetVisual(status, selected);
            }
        }

        /// <summary>
        /// Unlock the selected state with coins.
        /// Enough coins → deduct, save, refresh everything (no scene reload).
        /// Not enough → nothing changes, NOT ENOUGH COINS feedback + onNotEnoughCoins event.
        /// </summary>
        public bool TryUnlockSelectedState()
        {
            ChapterData data = FindData(_selectedStateId);
            if (data == null || GetStatus(_selectedStateId) != ChapterStatus.Locked) return false;
            if (_save == null) return false;

            int cost = data.UnlockCost;
            if (_save.TryUnlockState(data.ChapterID, cost))
            {
                Debug.Log($"[BKS Home] {data.DisplayName} unlocked for {cost} coins");
                RefreshAll(); // coin HUD also refreshes via OnCoinsChanged
                onStateUnlocked.Invoke(data.ChapterID);
                return true;
            }

            int needed = Mathf.Max(0, cost - _save.GetCoins());
            Debug.Log($"[BKS Home] Not enough coins for {data.DisplayName}: need {needed} more");
            if (chapterPanel != null) chapterPanel.ShowFeedback(string.Format(notEnoughCoinsFormat, needed.ToString("N0")), feedbackDuration);
            onNotEnoughCoins.Invoke(needed);
            return false;
        }

        /// <summary>Load the selected chapter's scene through BKS_SceneLoader.</summary>
        public void EnterSelectedChapter()
        {
            ChapterData data = FindData(_selectedStateId);
            ChapterStatus status = GetStatus(_selectedStateId);

            if (data == null || (status != ChapterStatus.Unlocked && status != ChapterStatus.Completed))
            {
                Debug.Log($"[BKS Home] Chapter unavailable: {GetFallbackName(_selectedStateId)}");
                return;
            }

            string sceneName = data.ChapterSceneName;
            if (string.IsNullOrEmpty(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[BKS Home] Cannot enter '{data.ChapterID}': scene '{sceneName}' is empty or not in Build Settings.", data);
                if (chapterPanel != null) chapterPanel.ShowFeedback(sceneMissingMessage, feedbackDuration);
                return;
            }

            if (BKS_SceneLoader.Instance != null)
                BKS_SceneLoader.Instance.LoadChapter(data);
            else
                SceneManager.LoadScene(sceneName); // fallback if persistent managers are missing
        }

        /// <summary>Mark a state completed (turns green on the map). Never re-locks.</summary>
        public void CompleteState(string stateId)
        {
            if (_save == null || string.IsNullOrEmpty(stateId)) return;
            _save.CompleteState(stateId.Trim()); // triggers HandleStateProgressChanged → refresh
            Debug.Log($"[BKS Home] State completed: {stateId}");
        }

        /// <summary>Read the saved progress of a state.</summary>
        public StateProgressData GetStateProgress(string stateId)
        {
            ChapterData data = FindData(stateId);
            bool available = data != null && data.ChapterAvailable;

            bool unlocked  = available && _save != null && _save.IsStateUnlocked(stateId);
            bool completed = available && _save != null && _save.IsChapterCompleted(stateId);
            return new StateProgressData(stateId, unlocked || completed, completed);
        }

        public ChapterStatus GetStatus(string stateId)
        {
            return ChapterStatusResolver.Resolve(FindData(stateId), GetStateProgress(stateId));
        }

        // ── Event Handlers ────────────────────────────────────────────────────────

        private void HandleActionClicked()
        {
            switch (GetStatus(_selectedStateId))
            {
                case ChapterStatus.Unlocked:
                case ChapterStatus.Completed:
                    EnterSelectedChapter();
                    break;

                case ChapterStatus.Locked:
                    TryUnlockSelectedState();
                    break;

                default: // Upcoming — button is disabled anyway
                    Debug.Log($"[BKS Home] Chapter unavailable: {GetFallbackName(_selectedStateId)}");
                    break;
            }
        }

        private void HandleCoinsChanged(int _) => RefreshCoinHud();

        private void HandleStateProgressChanged(string _) => RefreshAll();

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void RefreshAll()
        {
            RefreshSelectedStatePanel();
            RefreshMapStateButtons();
            RefreshCoinHud();
        }

        private void RefreshCoinHud()
        {
            if (coinText == null) return;
            int coins = _save != null ? _save.GetCoins() : PlayerPrefs.GetInt("Coins", 0);
            coinText.text = coins.ToString("N0");
        }

        private ChapterData FindData(string stateId)
        {
            if (string.IsNullOrEmpty(stateId)) return null;
            _dataById.TryGetValue(stateId, out var data);
            return data;
        }

        private string GetFallbackName(string stateId)
        {
            var data = FindData(stateId);
            if (data != null) return data.DisplayName;
            if (_buttonById.TryGetValue(stateId ?? string.Empty, out var list) && list.Count > 0 && list[0] != null)
                return list[0].DisplayName;
            return stateId;
        }

        // ── Debug (right-click the component header in Play Mode) ────────────────

        [ContextMenu("Debug/Add 2000 Coins")]
        private void DebugAddCoins() { if (_save != null) _save.AddCoins(2000); }

        [ContextMenu("Debug/Complete Selected State")]
        private void DebugCompleteSelected() => CompleteState(_selectedStateId);

        [ContextMenu("Debug/Reset Progress Of All Listed States")]
        private void DebugResetStates()
        {
            if (_save == null) return;
            foreach (var data in states)
                if (data != null) _save.ResetStateProgress(data.ChapterID);
            _save.EnsureDefaultUnlockedStates(states);
            RefreshAll();
            Debug.Log("[BKS Home] Progress reset for all listed states (defaults re-applied).");
        }
    }
}
