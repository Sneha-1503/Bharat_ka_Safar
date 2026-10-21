using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Core;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Diary
{
    /// <summary>
    /// Safar Diary (scene BKS_SafarDiary): every state on the left with "found/total", the memory tokens of
    /// the selected state on the right (unlocked when its mission is completed), tap a token for details.
    /// </summary>
    public class BKS_DiaryScreen : MonoBehaviour
    {
        [SerializeField] private BKS_ChapterRegistry registry;
        [SerializeField] private RectTransform chapterList;
        [SerializeField] private BKS_DiaryChapterButton chapterTemplate;
        [SerializeField] private RectTransform tokenGrid;
        [SerializeField] private BKS_DiaryTokenView tokenTemplate;
        [SerializeField] private BKS_DiaryDetailPopup detail;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private TMP_Text chapterTitle;
        [SerializeField] private Button backButton;
        [SerializeField] private string totalFormat = "{0} / {1} memories";
        [Tooltip("Selected state strip (gold) / other strips (parchment) — journal theme")]
        [SerializeField] private Color chapterOn = new Color(1f, 0.78f, 0.30f);
        [SerializeField] private Color chapterOff = new Color(0.97f, 0.90f, 0.71f);
        [Header("Token grid (fits all tokens of a state on the page)")]
        [SerializeField, Min(1)] private int tokenColumns = 3;
        [SerializeField, Min(0f)] private float tokenSpacing = 14f;

        private readonly List<GameObject> spawnedChapters = new List<GameObject>();
        private readonly List<GameObject> spawnedTokens = new List<GameObject>();
        private ChapterData selected;

        private void Start()
        {
            if (registry == null) registry = BKS_ChapterRegistry.Load();
            if (chapterTemplate != null) chapterTemplate.gameObject.SetActive(false);
            if (tokenTemplate != null) tokenTemplate.gameObject.SetActive(false);
            if (backButton != null) backButton.onClick.AddListener(() =>
            {
                if (BKS_SceneLoader.Instance != null) BKS_SceneLoader.Instance.ReturnFromMenu();
                else UnityEngine.SceneManagement.SceneManager.LoadScene(BKS_SceneLoader.SCENE_HOME);
            });
            if (registry == null || registry.chapters.Count == 0) { Debug.LogError("[Diary] No BKS_ChapterRegistry. Run Bharat Ka Safar > Diary > 1."); return; }

            // open on the chapter being played, else the first one
            var active = BKS_SceneLoader.Instance != null ? BKS_SceneLoader.Instance.ActiveChapter : null;
            selected = active != null && registry.chapters.Contains(active) ? active : registry.chapters[0];
            Refresh();
        }

        private static bool Unlocked(MissionData m) => BKS_SaveManager.Instance != null && BKS_SaveManager.Instance.IsMissionCompleted(m.MissionID);

        private void Refresh()
        {
            foreach (var g in spawnedChapters) Destroy(g);
            foreach (var g in spawnedTokens) Destroy(g);
            spawnedChapters.Clear(); spawnedTokens.Clear();

            int found = 0, total = 0;
            foreach (var ch in registry.chapters)
            {
                if (ch == null) continue;
                int f = 0, t = 0;
                foreach (var m in ch.Missions) { if (m == null) continue; t++; if (Unlocked(m)) f++; }
                found += f; total += t;
                var chapter = ch;
                var b = Instantiate(chapterTemplate, chapterList);
                b.gameObject.SetActive(true);
                b.Show(ch.DisplayName, f, t, ch == selected, chapterOn, chapterOff, () => { selected = chapter; Refresh(); });
                spawnedChapters.Add(b.gameObject);
            }
            if (totalText != null) totalText.text = string.Format(totalFormat, found, total);
            if (chapterTitle != null) chapterTitle.text = selected != null ? selected.DisplayName : "";

            if (selected == null) return;
            foreach (var m in selected.Missions)
            {
                if (m == null) continue;
                var t = Instantiate(tokenTemplate, tokenGrid);
                t.gameObject.SetActive(true);
                bool open = Unlocked(m);
                t.Show(m, open, mission =>
                {
                    int stars = BKS_SaveManager.Instance != null ? BKS_SaveManager.Instance.GetMissionBestRating(mission.MissionID) : 0;
                    if (detail != null) detail.Show(mission, open, stars);
                });
                spawnedTokens.Add(t.gameObject);
            }
            FitTokenGrid(spawnedTokens.Count);
        }

        // cell size so every token of the state fits on the right page (square cells)
        private void FitTokenGrid(int count)
        {
            var grid = tokenGrid != null ? tokenGrid.GetComponent<GridLayoutGroup>() : null;
            if (grid == null || count == 0) return;
            Canvas.ForceUpdateCanvases();
            int cols = Mathf.Min(tokenColumns, count), rows = Mathf.CeilToInt(count / (float)cols);
            Rect r = tokenGrid.rect;
            float w = (r.width - grid.padding.horizontal - tokenSpacing * (cols - 1)) / cols;
            float h = (r.height - grid.padding.vertical - tokenSpacing * (rows - 1)) / rows;
            float s = Mathf.Max(40f, Mathf.Min(w, h));
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;
            grid.spacing = new Vector2(tokenSpacing, tokenSpacing);
            grid.cellSize = new Vector2(s, s);
            grid.childAlignment = TextAnchor.MiddleCenter;
        }
    }
}
