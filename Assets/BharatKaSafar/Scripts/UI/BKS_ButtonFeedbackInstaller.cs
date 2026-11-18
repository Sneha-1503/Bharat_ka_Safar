using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace BharatKaSafar.UI
{
    /// <summary>Keeps adding BKS_ButtonFeedback to every button (scenes and runtime popups). Created by BKS_ButtonFeedback at startup.</summary>
    public class BKS_ButtonFeedbackInstaller : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // also catches popups made at runtime (pause, revive, result…)
            var wait = new WaitForSecondsRealtime(0.4f);
            while (true)
            {
                foreach (var b in FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    if (b != null && b.GetComponent<BKS_ButtonFeedback>() == null && !Skip(b)) b.gameObject.AddComponent<BKS_ButtonFeedback>();
                yield return wait;
            }
        }

        private static bool Skip(Button b)
        {
            if (b.GetComponent<BKS_NoButtonFeedback>() != null) return true;
            string n = b.name.ToLowerInvariant();
            // pedals: no feedback; horn: has its own horn sound
            return n.Contains("throttle") || n.Contains("brake") || n.Contains("gas") || n.Contains("accel") || n.Contains("horn");
        }
    }
}
