using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace InspectorHelper.Editor
{
    [CustomPropertyDrawer(typeof(InspectorButtonAttribute))]
    public class InspectorButtonPropertyDrawer : PropertyDrawer
    {
        private MethodInfo _eventMethodInfo = null;

        public override void OnGUI(Rect position, SerializedProperty prop, GUIContent label)
        {
            InspectorButtonAttribute inspectorButtonAttribute = (InspectorButtonAttribute)attribute;
            Rect buttonRect = new Rect(position.x + (position.width - position.width) * 0.5f, position.y, position.width, position.height);
            if (GUI.Button(buttonRect, inspectorButtonAttribute.MethodName) && prop.propertyType == SerializedPropertyType.Boolean)
            {
                var parentPath = prop.propertyPath.Remove(prop.propertyPath.Length-(prop.name.Length+1));
                var rootObject= prop.serializedObject;
                var actualProperty=rootObject.FindProperty(parentPath);
                //var actualObject=SerializedPropertyExtensions.GetValueBackward(prop,1);
                var actualValue = actualProperty.GetValueBackward();
                System.Type eventOwnerType = actualValue.GetType();
                string eventName = inspectorButtonAttribute.MethodName;
                if (_eventMethodInfo == null)
                    _eventMethodInfo = eventOwnerType.GetMethod(eventName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

                if (_eventMethodInfo != null)
                    _eventMethodInfo.Invoke(actualValue, null);
                else
                    Debug.LogWarning(string.Format("InspectorButton: Unable to find method {0} in {1}", eventName, eventOwnerType));
            }
        }
    }

}
