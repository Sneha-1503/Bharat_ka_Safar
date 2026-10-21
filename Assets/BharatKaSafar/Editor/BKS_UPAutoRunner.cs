#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace BharatKaSafar.EditorTools
{
    // DISABLED: this used to run BKS_UPSetupUtility.BuildCompleteUPChapter() automatically
    // every time the Unity editor started, which regenerates/overwrites the UP chapter assets,
    // UP scenes and Build Settings. Run the UP builder manually from its menu if ever needed.
    // [InitializeOnLoad]
    public static class BKS_UPAutoRunner
    {
        private const string KEY = "BKS_UP_SETUP_COMPLETED_V1";

        static BKS_UPAutoRunner()
        {
            EditorApplication.delayCall += CheckAndRun;
        }

        private static void CheckAndRun()
        {
            if (SessionState.GetBool(KEY, false)) return;
            SessionState.SetBool(KEY, true);

            Debug.Log("[BKS_UPAutoRunner] Triggering automated build of Uttar Pradesh chapter...");
            BKS_UPSetupUtility.BuildCompleteUPChapter();
        }
    }
}
#endif
