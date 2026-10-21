using System.Collections.Generic;
using UnityEngine;
using BharatKaSafar.Missions;

namespace BharatKaSafar.Core
{
    /// <summary>
    /// Describes a Monument within a Chapter. Contains the missions available at this monument.
    /// </summary>
    [CreateAssetMenu(fileName = "MonumentData", menuName = "Bharat Ka Safar/Monument Data", order = 2)]
    public class MonumentData : ScriptableObject
    {
        [Header("Identity")]
        public string monumentID;
        public string monumentName;
        [TextArea] public string shortDescription;

        [Header("Art")]
        [Tooltip("Placeholder if missing: PLACEHOLDER_MONUMENT_IMAGE")]
        public Sprite monumentImage;

        [Header("Missions")]
        [Tooltip("Missions available when this monument is selected")]
        public List<MissionData> availableMissions = new List<MissionData>();

#if UNITY_EDITOR
        public void InitForEditor(string mID, string mName, string desc, List<MissionData> missions)
        {
            monumentID = mID;
            monumentName = mName;
            shortDescription = desc;
            availableMissions = missions ?? new List<MissionData>();
        }
#endif
    }
}
