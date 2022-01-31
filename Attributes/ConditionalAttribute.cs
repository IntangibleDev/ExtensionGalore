using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

/// <summary> How to display the field if one of the logic gates conditions ( <see cref="OneBoolGate"/> or <seealso cref="TwoBoolsGate"/> ) is met. </summary>
public enum DrawType
{
    /// <summary> Greys out the field, and doesn't hide it. </summary>
    GreyOut,
    /// <summary> Hides the field completely, so that it isn't visible. </summary>
    DontDraw,
    /// <summary> Hides the field completely, so that it isn't visible, but keeps the height. </summary>
    DontDrawKeepHeight,
}

/// <summary> What conditional operation to use on a singular boolean. </summary>
public enum OneBoolGate
{
    /// <summary> Returns TRUE when bool1 is equal to TRUE. </summary>
    None,
    /// <summary> Returns TRUE when bool1 is equal to FALSE. </summary>
    Not,
}

/// <summary> What conditional operation to use on 2 booleans. </summary>
public enum TwoBoolsGate
{
    /// <summary> Returns TRUE when bool1 AND bool2 are equal to TRUE. </summary>
    And,          
    /// <summary> Returns TRUE when bool1 OR bool2 are equal to TRUE. </summary>
    Or,
    /// <summary> Returns FALSE when bool1 AND bool2 are equal to TRUE. </summary>
    Nand,
    /// <summary> Returns TRUE when bool1 AND bool2 are equal to FALSE. </summary>
    Nor,
    /// <summary> Returns FALSE when bool1 AND bool2 are equal to EACH OTHER. </summary>
    Xor,
    /// <summary> Returns TRUE when bool1 AND bool2 are equal to EACH OTHER. </summary>
    Xnor,
}

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
sealed class ConditionalAttribute : PropertyAttribute
{
    public readonly OneBoolGate? oneBoolConditionType = null;
    public readonly TwoBoolsGate? twoBoolsConditionType = null;
    public readonly DrawType drawType;

    public readonly string condition1;
    public readonly string condition2;

    public ConditionalAttribute(string conditionProperty1, DrawType drawMode = DrawType.GreyOut, OneBoolGate conditionalOperator = OneBoolGate.None)
    {
        condition1 = conditionProperty1;
        drawType = drawMode;
        oneBoolConditionType = conditionalOperator;
    }

    public ConditionalAttribute(string conditionProperty1, string conditionProperty2, DrawType drawMode = DrawType.GreyOut, TwoBoolsGate conditionalOperator = TwoBoolsGate.And)
    {
        condition1 = conditionProperty1;
        condition2 = conditionProperty2;
        drawType = drawMode;
        twoBoolsConditionType = conditionalOperator;
    }
}

[CustomPropertyDrawer(typeof(ConditionalAttribute))]
public class ConditionalAttributeEditor : PropertyDrawer
{
    #region AttributeHelpers

    private static FieldInfo GetField(object target, string fieldName)
    {
        return GetAllFields(target, f => f.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
    }

    private static IEnumerable<FieldInfo> GetAllFields(object target, Func<FieldInfo, bool> predicate)
    {
        List<Type> types = new List<Type>() { target.GetType() };

        while (types.Last().BaseType != null)
        {
            types.Add(types.Last().BaseType);
        }

        for (int i = types.Count - 1; i >= 0; i--)
        {
            IEnumerable<FieldInfo> fieldInfos = types[i].GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(predicate);

            foreach (var fieldInfo in fieldInfos)
            {
                yield return fieldInfo;
            }
        }
    }

    #endregion

    private bool MeetsConditions(SerializedProperty sp, ConditionalAttribute conditionalAttribute)
    {
        UnityEngine.Object target = sp.serializedObject.targetObject;

        FieldInfo conditionField1 = GetField(target, conditionalAttribute.condition1);
        FieldInfo conditionField2 = GetField(target, conditionalAttribute.condition2);

        bool? fieldValue1 = ObjectToNullableBoolean(conditionField1?.GetValue(target));
        bool? fieldValue2 = ObjectToNullableBoolean(conditionField2?.GetValue(target));

        if (conditionalAttribute.oneBoolConditionType != null && fieldValue1 != null)
        {
            return HandleOneBoolConditions((OneBoolGate)conditionalAttribute.oneBoolConditionType, (bool)fieldValue1);
        }

        if (conditionalAttribute.twoBoolsConditionType != null && fieldValue1 != null && fieldValue2 != null)
        {
            return HandleTwoBoolConditions((TwoBoolsGate)conditionalAttribute.twoBoolsConditionType, (bool)fieldValue1, (bool)fieldValue2);
        }

        return true;
    }

    private static bool HandleOneBoolConditions(OneBoolGate oneBoolConditionType, bool fieldValue1)
    {
        switch (oneBoolConditionType)
        {
            case OneBoolGate.None: return  fieldValue1;
            case OneBoolGate.Not:  return !fieldValue1;

            default: return true;
        }
    }

    private static bool HandleTwoBoolConditions(TwoBoolsGate twoBoolsConditionType, bool fieldValue1, bool fieldValue2)
    {
        switch (twoBoolsConditionType)
        {
            case TwoBoolsGate.And: return fieldValue1 && fieldValue2;
            case TwoBoolsGate.Or: return fieldValue1 || fieldValue2;
            case TwoBoolsGate.Nand: return !(fieldValue1 && fieldValue2);
            case TwoBoolsGate.Nor: return !(fieldValue1 || fieldValue2);
            case TwoBoolsGate.Xor: return (fieldValue1 && !fieldValue2) || (!fieldValue1 && fieldValue2);
            case TwoBoolsGate.Xnor: return (fieldValue1 && fieldValue2) || (!fieldValue1 && !fieldValue2);
            default: return true;
        }
    }

    private static bool? ObjectToNullableBoolean(object fieldObject)
    {
        switch (fieldObject)
        {
            case bool boolean: return boolean;
            case float real: return real > 0;
            case int integer: return integer > 0;
            case Transform transform: return transform != null;
            
            default: return null;
        }
    }





    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        ConditionalAttribute conditionalAttribute = attribute as ConditionalAttribute;

        bool conditionsMet = MeetsConditions(property, conditionalAttribute);

        if (conditionsMet)
        {
            EditorGUI.PropertyField(position, property, label, true);
            return;
        }

        switch (conditionalAttribute.drawType)
        {
            case DrawType.DontDrawKeepHeight | DrawType.DontDraw:
                return;
            case DrawType.GreyOut:
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.EndDisabledGroup();
                return;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        ConditionalAttribute conditionAttribute = attribute as ConditionalAttribute;

        if (!MeetsConditions(property, conditionAttribute))
        {
            switch (conditionAttribute.drawType)
            {
                case DrawType.DontDraw: return -base.GetPropertyHeight(property, label);
            }

        }

        return base.GetPropertyHeight(property, label);
    }
}
