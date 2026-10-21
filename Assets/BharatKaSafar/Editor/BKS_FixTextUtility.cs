using UnityEngine;
using UnityEditor;

namespace BharatKaSafar.EditorTools
{
    public static class BKS_FixTextUtility
    {
        [MenuItem("Bharat Ka Safar/Fix Floating Text Size")]
        public static void FixTextSize()
        {
            TextMesh[] texts = Object.FindObjectsOfType<TextMesh>();
            int count = 0;
            
            Undo.RecordObjects(texts, "Fix Text Size");
            
            foreach (var tm in texts)
            {
                if (tm.name == "WorldLabel" || tm.text.Contains("COW") || tm.text.Contains("STAR") || tm.text.Contains("POTHOLE") || tm.text.Contains("DHABA"))
                {
                    tm.fontSize = 120;
                    tm.characterSize = 0.1f;
                    tm.fontStyle = FontStyle.Bold;
                    EditorUtility.SetDirty(tm);
                    count++;
                }
            }
            
            Debug.Log($"[BKS FixText] Fixed size and thickness of {count} floating text labels in the scene.");
            
            // Mark scene as dirty so the changes can be saved
            if (count > 0)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
        }
    }
}
