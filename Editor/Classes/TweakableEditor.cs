using UnityEngine;
using System.Collections;
using UnityEditor;

// Yoinked from https://answers.unity.com/questions/316286/how-to-remove-script-field-in-inspector.html. Thx XP.

/// <summary>
/// A simple class to inherit from when only minor tweaks to a component's inspector are required.
/// In such cases, a full custom inspector is normally overkill but, by inheriting from this class, custom tweaks become trivial.
/// 
/// To hide items from being drawn, simply override GetInvisibleInDefaultInspector, returning a string[] of fields to hide.
/// To draw/add extra GUI code/anything else you want before the default inspector is drawn, override OnBeforeDefaultInspector.
/// Similarly, override OnAfterDefaultInspector to draw GUI elements after the default inspector is drawn.
/// </summary>
public abstract class TweakableEditor : Editor
{
     public override void OnInspectorGUI()
    {
        serializedObject.Update();

        OnBeforeDefaultInspector();
        DrawPropertiesExcluding(serializedObject, GetHiddenInDefaultInspector());
        OnAfterDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }

    protected virtual void OnBeforeDefaultInspector() { }

    protected virtual void OnAfterDefaultInspector() { }

    protected virtual string[] GetHiddenInDefaultInspector()
    {
        return new string[0];
    }
}
