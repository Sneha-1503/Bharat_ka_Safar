using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using BharatKaSafar.Missions;
using BharatKaSafar.Vehicle;

namespace BharatKaSafar.UI
{
    /// <summary>One-time control practice in the actual HUD. Physics, fuel and mission time stay paused.</summary>
    public sealed class BKS_DrivingTutorial : MonoBehaviour
    {
        private const string SaveKey = "BKS_DrivingTutorial_v1"; // 1 completed, 2 explicitly skipped
        private static BKS_DrivingTutorial active;
        private static bool? replayRequested;
        public static bool IsShowing => active != null;
        public static bool NeedsIntroduction => PlayerPrefs.GetInt(SaveKey, 0) == 0;
        public static void RequestReplay(bool value) => replayRequested = value;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { active = null; replayRequested = null; }

        public static void TryBegin(MissionManager mission)
        {
            bool wanted = replayRequested ?? NeedsIntroduction;
            replayRequested = null;
            if (!wanted || active != null || mission == null) return;
            var tutorial = mission.gameObject.AddComponent<BKS_DrivingTutorial>();
            tutorial.mission = mission;
            active = tutorial;
            tutorial.previousTimeScale = Time.timeScale;
            tutorial.ownsPause = true;
            Time.timeScale = 0f;
        }

        private MissionManager mission;
        private BKS_GameplayHUD hud;
        private CarController car;
        private BKS_HornController horn;
        private GameObject overlay;
        private RectTransform canvasRect, safe, focus;
        private readonly Image[] shades = new Image[4];
        private readonly Image[] edges = new Image[4];
        private readonly Vector3[] corners = new Vector3[4];
        private TMP_Text title, description, feedback, nextLabel;
        private Button next;
        private Image progress;
        private int step;
        private float held, previousTimeScale;
        private bool balanceGasDone, satisfied, ownsPause;

        private IEnumerator Start()
        {
            // StartPoint and the HUD bind their controls in Start; allow them to finish first.
            float deadline = Time.realtimeSinceStartup + 5f;
            while (Time.realtimeSinceStartup < deadline)
            {
                hud = FindFirstObjectByType<BKS_GameplayHUD>();
                car = FindFirstObjectByType<CarController>();
                horn = car != null ? car.GetComponentInChildren<BKS_HornController>() : null;
                if (hud != null && car != null && horn != null && hud.AccelerateControl != null &&
                    hud.BrakeControl != null && hud.HornControl != null) break;
                yield return null;
            }
            if (hud == null || car == null || horn == null || hud.AccelerateControl == null ||
                hud.BrakeControl == null || hud.HornControl == null)
            {
                Debug.LogWarning("[Driving tutorial] Controls unavailable; leaving introduction pending for the next mission.");
                Close();
                yield break;
            }
            yield return null;
            horn.OnHornPressed += OnHorn;
            BuildUI();
            ShowStep();
        }

        private void Update()
        {
            if (overlay == null) return;
            if (mission == null || mission.CurrentState != MissionState.Active) { Close(); return; }
            FitSafeArea();
            UpdateSpotlight();
            if (Input.GetKeyDown(KeyCode.Escape)) { Finish(false); return; }
            if (satisfied || step >= 3) return;
            float input = car != null ? car.DriveInput : 0f;
            bool wantsGas = step == 0 || (step == 2 && !balanceGasDone);
            bool correct = wantsGas ? input > 0.1f : input < -0.1f;
            held = correct ? held + Mathf.Min(Time.unscaledDeltaTime, 0.1f) : 0f;
            SetProgress(Mathf.Clamp01(held / 0.6f));
            if (held < 0.6f) return;
            if (step == 2 && !balanceGasDone)
            {
                balanceGasDone = true;
                held = 0f;
                focus = hud.BrakeControl;
                feedback.text = "Nose up! Now hold BRAKE to practise lowering it.";
                SetProgress(0f);
            }
            else CompleteStep();
        }

        private void OnHorn() { if (step == 3) CompleteStep(); }

        private void CompleteStep()
        {
            satisfied = true;
            SetProgress(1f);
            next.interactable = true;
            feedback.text = step == 3 ? "Good! Use the horn before you reach an animal or cart." : "Got it! Release the pedal, then continue.";
        }

        private void Advance()
        {
            if (!satisfied) return;
            if (step == 4) { Finish(true); return; }
            ReleaseControls();
            step++;
            ShowStep();
        }

        private void ShowStep()
        {
            held = 0f;
            balanceGasDone = false;
            satisfied = step == 4;
            next.interactable = satisfied;
            nextLabel.text = step == 4 ? "START DRIVING" : "CONTINUE";
            SetProgress(satisfied ? 1f : 0f);
            switch (step)
            {
                case 0:
                    title.text = "1 / 5   GET MOVING";
                    description.text = "Hold the right pedal to accelerate. Release it to coast.\nTry holding the highlighted GAS pedal.";
                    focus = hud.AccelerateControl;
                    feedback.text = "Hold GAS until the bar fills.";
                    break;
                case 1:
                    title.text = "2 / 5   SLOW DOWN";
                    description.text = "The left pedal brakes while you move forward.\nKeep holding it after stopping to reverse.";
                    focus = hud.BrakeControl;
                    feedback.text = "Hold the highlighted BRAKE pedal.";
                    break;
                case 2:
                    title.text = "3 / 5   BALANCE IN THE AIR";
                    description.text = "When airborne, GAS lifts the nose and BRAKE lowers it.\nUse short taps to land on both wheels. Practise GAS, then BRAKE.";
                    focus = hud.AccelerateControl;
                    feedback.text = "First, hold GAS to practise lifting the nose.";
                    break;
                case 3:
                    title.text = "4 / 5   MAKE WAY";
                    description.text = "Tap the horn near animals and carts ahead to clear the road.\nIt will not remove every obstacle: slow down for rocks and rough ground.";
                    focus = hud.HornControl;
                    feedback.text = "Tap the highlighted HORN button.";
                    break;
                default:
                    title.text = "5 / 5   WATCH YOUR FUEL";
                    description.text = "Watch the fuel gauge at the top left. Collect red fuel cans before it empties.\nReach the finish and complete the briefing goals to earn more stars.";
                    focus = null;
                    feedback.text = "Ready! You can practise again from any mission briefing.";
                    break;
            }
            UpdateSpotlight();
        }

        private void Finish(bool completed)
        {
            // Replaying then skipping must not downgrade a completed introduction.
            if (completed || NeedsIntroduction) PlayerPrefs.SetInt(SaveKey, completed ? 1 : 2);
            PlayerPrefs.Save();
            Close();
        }

        private void ReleaseControls()
        {
            foreach (var pedals in FindObjectsByType<BKS_PedalFix>(FindObjectsSortMode.None)) pedals.ReleaseAll();
            if (car != null) car.GasBrakeRelease();
        }

        private void Close()
        {
            ReleaseControls();
            if (horn != null) horn.OnHornPressed -= OnHorn;
            if (overlay != null) { overlay.SetActive(false); Destroy(overlay); }
            if (active == this) active = null;
            if (ownsPause)
            {
                ownsPause = false;
                // A terminal result owns its pause; never resume a completed/failed mission.
                if (mission != null && mission.CurrentState == MissionState.Active &&
                    (BKS_PausePanel.Instance == null || !BKS_PausePanel.Instance.IsPaused))
                    Time.timeScale = previousTimeScale;
            }
            Destroy(this);
        }

        private void OnDisable()
        {
            if (active == this || ownsPause) Close();
        }

        private void OnApplicationFocus(bool focused) { if (!focused) { held = 0f; ReleaseControls(); } }

        private void BuildUI()
        {
            overlay = new GameObject("Driving tutorial", typeof(RectTransform));
            BKS_UIStyle.MakeOverlay(overlay, 32500);
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.matchWidthOrHeight = 0.5f;
            canvasRect = (RectTransform)overlay.transform;
            for (int i = 0; i < 4; i++) shades[i] = Box("Shade", canvasRect, new Color(0f, 0.06f, 0.08f, 0.62f), true);
            for (int i = 0; i < 4; i++) edges[i] = Box("Control highlight", canvasRect, BKS_UIStyle.Gold, false);
            safe = Rect("Safe area", canvasRect, Vector2.zero, Vector2.one);
            var card = Box("Lesson card", safe, BKS_UIStyle.TealDark, true);
            Place(card.rectTransform, new Vector2(0.21f, 0.43f), new Vector2(0.79f, 0.9f));
            var border = card.gameObject.AddComponent<Outline>();
            border.effectColor = BKS_UIStyle.Gold; border.effectDistance = new Vector2(3f, -3f);
            Label("Practice note", card.transform, "CONTROL PRACTICE  /  CAR & FUEL PAUSED", 22, BKS_UIStyle.Gold, .05f, .86f, .95f, .95f);
            title = Label("Title", card.transform, "", 38, Color.white, .05f, .7f, .95f, .86f);
            description = Label("Instructions", card.transform, "", 29, BKS_UIStyle.Parchment, .05f, .38f, .95f, .69f);
            feedback = Label("Feedback", card.transform, "", 24, BKS_UIStyle.Gold, .05f, .25f, .95f, .37f);
            var track = Box("Hold progress track", card.transform, new Color(1f, 1f, 1f, .15f), false);
            Place(track.rectTransform, new Vector2(.05f, .21f), new Vector2(.95f, .23f));
            progress = Box("Hold progress", track.transform, BKS_UIStyle.Gold, false);
            next = MakeButton("Continue", card.transform, "CONTINUE", new Vector2(.51f, .04f), new Vector2(.95f, .18f), Advance);
            nextLabel = next.GetComponentInChildren<TMP_Text>();
            MakeButton("Skip", card.transform, "SKIP TUTORIAL", new Vector2(.05f, .04f), new Vector2(.45f, .18f), () => Finish(false));
            FitSafeArea();
        }

        private void SetProgress(float value)
        {
            if (progress != null) progress.rectTransform.anchorMax = new Vector2(value, 1f);
        }

        private void FitSafeArea()
        {
            Rect r = Screen.safeArea;
            safe.anchorMin = new Vector2(r.xMin / Screen.width, r.yMin / Screen.height);
            safe.anchorMax = new Vector2(r.xMax / Screen.width, r.yMax / Screen.height);
        }

        private void UpdateSpotlight()
        {
            Vector2 min = new Vector2(.01f, .87f), max = new Vector2(.25f, .99f); // fuel gauge
            if (focus != null)
            {
                focus.GetWorldCorners(corners);
                var c = focus.GetComponentInParent<Canvas>().rootCanvas;
                Camera camera = c.renderMode == RenderMode.ScreenSpaceOverlay ? null : c.worldCamera;
                Vector2 a = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 b = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
                min = new Vector2(Mathf.Clamp01((a.x - 8f) / Screen.width), Mathf.Clamp01((a.y - 8f) / Screen.height));
                max = new Vector2(Mathf.Clamp01((b.x + 8f) / Screen.width), Mathf.Clamp01((b.y + 8f) / Screen.height));
            }
            Place(shades[0].rectTransform, Vector2.zero, new Vector2(min.x, 1));
            Place(shades[1].rectTransform, new Vector2(max.x, 0), Vector2.one);
            Place(shades[2].rectTransform, new Vector2(min.x, 0), new Vector2(max.x, min.y));
            Place(shades[3].rectTransform, new Vector2(min.x, max.y), new Vector2(max.x, 1));
            const float width = .003f;
            Place(edges[0].rectTransform, min, new Vector2(min.x + width, max.y));
            Place(edges[1].rectTransform, new Vector2(max.x - width, min.y), max);
            Place(edges[2].rectTransform, min, new Vector2(max.x, min.y + width));
            Place(edges[3].rectTransform, new Vector2(min.x, max.y - width), max);
        }

        internal static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); Place(r, min, max); return r;
        }
        internal static void Place(RectTransform r, Vector2 min, Vector2 max)
        { r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero; }
        private static Image Box(string name, Transform parent, Color color, bool blocks)
        {
            var image = Rect(name, parent, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = blocks; return image;
        }
        internal static TMP_Text Label(string name, Transform parent, string value, float size, Color color, float x0, float y0, float x1, float y1)
        {
            var t = Rect(name, parent, new Vector2(x0, y0), new Vector2(x1, y1)).gameObject.AddComponent<TextMeshProUGUI>();
            t.text = value; t.font = TMP_Settings.defaultFontAsset; t.fontSize = size; t.color = color;
            t.enableAutoSizing = true; t.fontSizeMin = size * .8f; t.fontSizeMax = size;
            t.alignment = TextAlignmentOptions.MidlineLeft; t.raycastTarget = false; return t;
        }
        internal static Button MakeButton(string name, Transform parent, string text, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var bg = Box(name, parent, BKS_UIStyle.Teal, true); Place(bg.rectTransform, min, max);
            var b = bg.gameObject.AddComponent<Button>(); b.targetGraphic = bg;
            var colors = b.colors; colors.disabledColor = new Color(.45f, .5f, .5f); b.colors = colors;
            var label = Label("Label", bg.transform, text, 26, Color.white, .04f, .05f, .96f, .95f);
            label.alignment = TextAlignmentOptions.Center;
            b.onClick.AddListener(action); return b;
        }
    }
}
