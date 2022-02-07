using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Data;
using System.Text.RegularExpressions;
using System.Text;

namespace ExtensionGalore.Attributes
{
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfPropertyDrawer : PropertyDrawer
    {
        bool? showField  = true;

        #region AttributeHelpers

        private static FieldInfo GetField(object target, string fieldName)
        {
            return GetAllFieldsWithName(target, f => f.Name.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
        }
        private static IEnumerable<FieldInfo> GetAllFields(object target)
        {
            List<Type> types = new List<Type>() { target.GetType() };

            while (types.Last().BaseType != null)
            {
                types.Add(types.Last().BaseType);
            }

            for (int i = types.Count - 1; i >= 0; i--)
            {
                IEnumerable<FieldInfo> fieldInfos = types[i].GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);

                foreach (var fieldInfo in fieldInfos)
                {
                    yield return fieldInfo;
                }
            }
        }

        private static IEnumerable<FieldInfo> GetAllFieldsWithName(object target, Func<FieldInfo, bool> predicate)
        {
            return GetAllFields(target).Where(predicate);
        }


        #endregion

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIfAttribute = attribute as ShowIfAttribute;

            string expressionWithFields = ConvertToDataTableExpression(ReplaceFieldsWithValues(property.serializedObject.targetObject, showIfAttribute));
            showField = EvaluteExpression(expressionWithFields);

            if (showField == null)
            {
                Debug.LogWarning("The given expression is not valid!"); return;
            }

            if (showField == true)
            {
                EditorGUI.PropertyField(position, property, label); return;
            }
            else
            {
                GUIContent labelDisabled = label;
                labelDisabled.tooltip = showIfAttribute.greyOutToolTip ?? $"This field has been disabled by the following expression. [ { showIfAttribute.expression } ] { (label.tooltip != null ? $"\n\n { label.tooltip }" : null)}";

                switch (showIfAttribute.drawType)
                {
                    case DrawType.GreyOut:
                        EditorGUI.BeginDisabledGroup(true);
                        EditorGUI.PropertyField(position, property, labelDisabled, true);
                        EditorGUI.EndDisabledGroup(); return;
                }
            }

        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIfAttribute = attribute as ShowIfAttribute;

            if (showField == false)
            {
                switch (showIfAttribute.drawType)
                {
                    case DrawType.DontDraw: return -2;
                }
            }

            return base.GetPropertyHeight(property, label);
        }

        private static string ConvertToDataTableExpression(string rawExpression)
        {
            StringBuilder sb = new StringBuilder(rawExpression).Replace("&&", "and")
                                                               .Replace("||", "or")
                                                               .Replace("!=", "<>")
                                                               .Replace("==", "=");

            string result = sb.ToString();

            result = Regex.Replace(result, @"(?<!\d)(?=\.\d+)", "0");              // add zeros behind .0 => 0.0
            result = Regex.Replace(result, @"(?<=\d+)(?<!\.\d*)(?![\d\.])", ".0"); // add decimals behind 0 => 0.0
            result = Regex.Replace(result, @"(?<=\d)[fd]", "");                    // remove f or d from 0.0f => 0.0
            
            return result;
        }

        private static bool? EvaluteExpression(string expression)
        {
            DataTable dataTable = new();
            bool? result = null;

            try
            {
                result = dataTable.Compute(expression, "") as bool?;
            }
            catch (Exception) { }

            return result;
        }

        private static string ReplaceFieldsWithValues(UnityEngine.Object target, ShowIfAttribute showIf)
        {
            string result = showIf.expression;

            foreach (FieldInfo field in GetAllFields(target).ToArray())
            {
                // TODO: Implement enums better => allow enum == enum.one instead of enum == 1

                if (field.FieldType.IsEnum)
                {
                    Enum value = (Enum)field.GetValue(target);
                    result = result.Replace(field.Name, $"{ Convert.ChangeType(value, value.GetTypeCode()) }");
                }
                else
                {
                    result = result.Replace(field.Name, $"{ field.GetValue(target) }");
                }
            }

            return result;
        }
    }
}