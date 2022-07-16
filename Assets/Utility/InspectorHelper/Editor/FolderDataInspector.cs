using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Utility.Assetframe.Editor;

namespace InspectorHelper.Editor
{
    [CustomPropertyDrawer(typeof(FolderData))]
    public class FolderDataDrawer : PropertyDrawer
    {
        private SerializedProperty reference;
        private SerializedProperty customLabelName;
        private SerializedProperty prefixLabelName;
        private SerializedProperty suffixLabelName;
        private SerializedProperty previewLabelName;
        private Assembly assembly;
        private Type folderDataType;
        private MethodInfo getPreviewLabelName;

        private void Init(SerializedProperty property)
        {
            reference = property.FindPropertyRelative("Reference");
            customLabelName = property.FindPropertyRelative("CustomLabelName");
            prefixLabelName = property.FindPropertyRelative("PrefixLabelName");
            suffixLabelName = property.FindPropertyRelative("SuffixLabelName");
            previewLabelName = property.FindPropertyRelative("PreviewLabelName");
            if (assembly == null)
            {
                assembly = Assembly.GetAssembly(typeof(FolderData)); 
            }

            if (folderDataType == null)
            {
                folderDataType = assembly.GetType("Utility.Assetframe.Editor.FolderData");
            }
            if (getPreviewLabelName == null)
            {
                getPreviewLabelName = folderDataType.GetMethod("GetLabelName");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight= base.GetPropertyHeight(property, label);
            var labelTypeProperty = property.GetParent().GetParent().GetParent().FindPropertyRelative("LabelType");
            switch ((LabelType)labelTypeProperty.intValue )
            {
                case LabelType.None:
                    baseHeight *= 2;
                    break;
                case LabelType.SameAsFolder:
                    baseHeight *= 2;
                    break;
                case LabelType.FloderWithPrefixAndSuffix:
                    baseHeight *= 4;
                    break;
                case LabelType.Custom:
                    baseHeight *= 3;
                    break;
            }
            return baseHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);
            //Reference
            FolderReferencePropertyDrawer drawer = new FolderReferencePropertyDrawer();
            Rect referenceRect = position;
            float baseHeight = base.GetPropertyHeight(property, label);
            referenceRect.height=baseHeight;
            GUIContent referenceContent = new GUIContent("Folder");
            drawer.OnGUI(referenceRect, reference, referenceContent);
            //LabelName
            var labelTypeProperty = property.GetParent().GetParent().GetParent().FindPropertyRelative("LabelType");
            Rect string1Rect = referenceRect;
            string1Rect.y += baseHeight;
            Rect string2Rect = string1Rect;
            string2Rect.y += baseHeight;
            Rect string3Rect = string2Rect;
            string3Rect.y += baseHeight;
            LabelType labelType = (LabelType) labelTypeProperty.intValue;
            Rect previewRect=string1Rect;
            switch (labelType)
            {
                case LabelType.None:
                case LabelType.SameAsFolder:
                    break;
                case LabelType.FloderWithPrefixAndSuffix:
                    GUIContent prefixContent = new GUIContent("Prefix");
                    EditorGUI.PropertyField(string1Rect, prefixLabelName,prefixContent);
                    GUIContent suffixContent = new GUIContent("Suffix");
                    EditorGUI.PropertyField(string2Rect, suffixLabelName,suffixContent);
                    previewRect = string3Rect;
                    break;
                case LabelType.Custom:
                    GUIContent customContent = new GUIContent("Custom Label");
                    EditorGUI.PropertyField(string1Rect, customLabelName,customContent);
                    previewRect = string2Rect;
                    break;
            }
            //Preview
            if (getPreviewLabelName != null)
            {
                var value = property.GetValueBackward();
                previewLabelName.stringValue=getPreviewLabelName.Invoke(value,new object[]{labelType}) as string;
            }
            GUIContent previewContent = new GUIContent("Label Preview");
            EditorGUI.PropertyField(previewRect, previewLabelName,previewContent);
        }
    }
}