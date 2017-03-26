using System;
using System.Reflection;

namespace Microsoft.WPFWizardExample
{
    public static class ReflectionHelper
    {

        public static void SwapAll<T>(T sourceInstance, T destinationInstance, bool declaredOnly = false)
        {
            var fields = typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | (declaredOnly ? BindingFlags.DeclaredOnly : 0));
            foreach (var f in fields)
            {
                f.SetValue(destinationInstance, f.GetValue(sourceInstance));
            }
        }

        public static void Swap<T>(T sourceInstance, T destinationInstance, string fieldName)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (field == null)
            {
                throw new ArgumentOutOfRangeException($"The field name {fieldName} was not found on type {typeof(T)}");
            }
            field.SetValue(destinationInstance, field.GetValue(sourceInstance));
        }

        public static object GetFieldValue(object instance, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (field == null)
            {
                throw new ArgumentOutOfRangeException($"The field name {fieldName} was not found on type {instance.GetType()}");
            }
            return field.GetValue(instance);
        }

        public static void SetFieldValue(object instance, object fieldValue, string fieldName)
        {
            var field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (field == null)
            {
                throw new ArgumentOutOfRangeException($"The field name {fieldName} was not found on type {instance.GetType()}");
            }
            field.SetValue(instance, fieldValue);
        }

        public static object GetPropertyValue(object instance, string propName)
        {
            var prop = instance.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (prop == null)
            {
                throw new ArgumentOutOfRangeException($"The field name {propName} was not found on type {instance.GetType()}");
            }
            return prop.GetValue(instance);
        }

        public static void SetPropertyValue(object instance, object propValue, string propName)
        {
            var prop = instance.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
            if (prop == null)
            {
                throw new ArgumentOutOfRangeException($"The field name {propName} was not found on type {instance.GetType()}");
            }
            prop.SetValue(instance, propValue);
        }
    }
}
