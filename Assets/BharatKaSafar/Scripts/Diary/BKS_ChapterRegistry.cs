using System.Collections.Generic;
using UnityEngine;
using BharatKaSafar.Core;

namespace BharatKaSafar.Diary
{
    /// <summary>
    /// All chapters in journey order (Resources/BKS_ChapterRegistry), so runtime screens such as the
    /// Safar Diary can list every state without scene references. Filled by Bharat Ka Safar > Diary.
    /// </summary>
    [CreateAssetMenu(menuName = "Bharat Ka Safar/Chapter Registry", fileName = "BKS_ChapterRegistry")]
    public class BKS_ChapterRegistry : ScriptableObject
    {
        public List<ChapterData> chapters = new List<ChapterData>();

        private static BKS_ChapterRegistry cached;
        public static BKS_ChapterRegistry Load()
        {
            if (cached == null) cached = Resources.Load<BKS_ChapterRegistry>("BKS_ChapterRegistry");
            return cached;
        }
    }
}
