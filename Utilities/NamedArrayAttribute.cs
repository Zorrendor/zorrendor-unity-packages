using UnityEngine;
using System;
using System.Text.RegularExpressions;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
#endif

// Defines an attribute that makes the array use enum values as labels.
// Use like this:
//      [NamedArray(typeof(eDirection))] public GameObject[] m_Directions;
public class NamedArrayAttribute : PropertyAttribute
{
    public readonly Type targetEnum;
    public NamedArrayAttribute(Type targetEnum)
    {
        this.targetEnum = targetEnum;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(NamedArrayAttribute))]
public class NamedArrayDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        PropertyField propertyField = new PropertyField(property);
        var config = attribute as NamedArrayAttribute;
        if (config == null)// || !property.isArray)
        {
            return propertyField;
        }
        
        string pattern = @"data\[(\d+)\]";
        var match = Regex.Match(property.propertyPath, pattern, RegexOptions.RightToLeft);
        if (match.Success)
        {
            string[] enumNames = System.Enum.GetNames(config.targetEnum);
            if (int.TryParse(match.Groups[1].Value, out int index) && enumNames.Length > index)
            {
                propertyField.label = enumNames[index];
            }
        }
        return propertyField;
    }
}
#endif
