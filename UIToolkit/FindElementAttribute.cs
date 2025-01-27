using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class FindElementAttribute : Attribute
{
    public string ElementName { get; }

    public FindElementAttribute(string elementName)
    {
        ElementName = elementName;
    }
}

public static class UIElementBinder
{
    public static void BindElements(VisualElement root, object target)
    {
        var fields = target.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<FindElementAttribute>();
            if (attribute == null) continue;

            string elementName = attribute.ElementName;

            var element = root.Q(elementName);
            if (element != null && field.FieldType.IsAssignableFrom(element.GetType()))
            {
                field.SetValue(target, element);
            }
            else
            {
                Debug.LogWarning($"Element '{elementName}' not found or incompatible with field '{field.Name}'.");
            }
        }
    }
}
