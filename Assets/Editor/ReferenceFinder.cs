using UnityEngine;
using UnityEditor;
using System.Reflection;
using System;


public class ReferenceFinder
{
    [MenuItem("Tools/Find References to Selected GameObject")]
    private static void FindReferencesToSelectedGameObject()
    {
        GameObject selectedObject = Selection.activeGameObject;

        if (selectedObject == null)
        {
            Debug.LogWarning("No GameObject selected. Please select a GameObject in the Hierarchy.");
            return;
        }

        int referenceCount = 0;
        Debug.Log($"<color=cyan>Searching for references to '{selectedObject.name}'...</color>");

        GameObject[] allObjects = UnityEngine.Object.FindObjectsByType<GameObject>(UnityEngine.FindObjectsSortMode.None);

        foreach (GameObject go in allObjects)
        {
            Component[] components = go.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null) continue;

                FieldInfo[] fields = component.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (FieldInfo field in fields)
                {
                    try
                    {
                        object fieldValue = field.GetValue(component);

                        if (fieldValue as GameObject == selectedObject ||
                            (fieldValue is Component comp && comp.gameObject == selectedObject))
                        {
                            Debug.Log($"Found reference on GameObject: <b>{go.name}</b>, in Component: <b>{component.GetType().Name}</b>, in Field: <b>{field.Name}</b>", go);
                            referenceCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"Could not inspect field <b>{field.Name}</b> on component <b>{component.GetType().Name}</b> on GameObject <b>{go.name}</b>. Error: {ex.Message}", go);
                    }
                }
            }
        }

        if (referenceCount == 0)
        {
            Debug.Log($"No references found for '{selectedObject.name}' in the current scene.");
        }
        else
        {
            Debug.Log($"<color=lime>Finished search. Found {referenceCount} reference(s) to '{selectedObject.name}'.</color>");
        }
    }
}