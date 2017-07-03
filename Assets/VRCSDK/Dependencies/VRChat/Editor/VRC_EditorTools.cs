﻿#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using System.Reflection;
using System;

namespace VRCSDK2
{
    public static class VRC_EditorTools
    {
        private static LayerMask LayerMaskPopupInternal(LayerMask selectedValue, System.Func<int, string[], int> showMask)
        {
            string[] layerNames = InternalEditorUtility.layers;
            List<int> layerNumbers = new List<int>();

            foreach (string layer in layerNames)
                layerNumbers.Add(LayerMask.NameToLayer(layer));

            int mask = 0;
            for (int idx = 0; idx < layerNumbers.Count; ++idx)
                if (((1 << layerNumbers[idx]) & selectedValue.value) > 0)
                    mask |= (1 << idx);

            mask = showMask(mask, layerNames);

            selectedValue.value = 0;
            for (int idx = 0; idx < layerNumbers.Count; ++idx)
                if (((1 << idx) & mask) > 0)
                    selectedValue.value |= (1 << layerNumbers[idx]);

            return selectedValue;
        }

        public static LayerMask LayerMaskPopup(LayerMask selectedValue, params GUILayoutOption[] options)
        {
            return LayerMaskPopupInternal(selectedValue, (mask, layerNames) => EditorGUILayout.MaskField(mask, layerNames, options));
        }

        public static LayerMask LayerMaskPopup(string label, LayerMask selectedValue, params GUILayoutOption[] options)
        {
            return LayerMaskPopupInternal(selectedValue, (mask, layerNames) => EditorGUILayout.MaskField(label, mask, layerNames, options));
        }

        public static LayerMask LayerMaskPopup(Rect rect, LayerMask selectedValue, GUIStyle style = null)
        {
            System.Func<int, string[], int> show = (mask, layerNames) =>
            {
                if (style == null)
                    return EditorGUI.MaskField(rect, mask, layerNames);
                else
                    return EditorGUI.MaskField(rect, mask, layerNames, style);
            };
            return LayerMaskPopupInternal(selectedValue, show);
        }

        public static LayerMask LayerMaskPopup(Rect rect, string label, LayerMask selectedValue, GUIStyle style = null)
        {
            System.Func<int, string[], int> show = (mask, layerNames) =>
            {
                if (style == null)
                    return EditorGUI.MaskField(rect, label, mask, layerNames);
                else
                    return EditorGUI.MaskField(rect, label, mask, layerNames, style);
            };
            return LayerMaskPopupInternal(selectedValue, show);
        }
        
        private static T FilteredEnumPopupInternal<T>(T selectedValue, System.Func<T, bool> predicate, System.Func<int, string[], int> showPopup, System.Func<string, string> rename) where T : struct, System.IConvertible
        {
            if (!typeof(T).IsEnum)
                throw new System.ArgumentException(typeof(T).Name + " is not an Enum", "T");

            T[] ary = System.Enum.GetValues(typeof(T)).Cast<T>().Where(v => predicate(v)).ToArray();
            string[] names = ary.Select(e => System.Enum.GetName(typeof(T), e)).ToArray();

            int selectedIdx = 0;
            for (; selectedIdx < ary.Length; ++selectedIdx)
                if (ary[selectedIdx].Equals(selectedValue))
                    break;
            if (selectedIdx == ary.Length)
                selectedIdx = 0;

            if (ary.Length == 0)
                throw new System.ArgumentException("Predicate filtered out all options", "predicate");

            if (rename != null)
                return ary[showPopup(selectedIdx, names.Select(rename).ToArray())];
            else
                return ary[showPopup(selectedIdx, names)];
        }

        private static void FilteredEnumPopupInternal<T>(SerializedProperty enumProperty, System.Func<T, bool> predicate, System.Func<int, string[], int> showPopup, System.Func<string, string> rename) where T : struct, System.IConvertible
        {
            string selectedName = enumProperty.enumNames[enumProperty.enumValueIndex];
            T selectedValue = FilteredEnumPopupInternal<T>((T)System.Enum.Parse(typeof(T), selectedName), predicate, showPopup, rename);
            selectedName = selectedValue.ToString();
            for (int idx = 0; idx < enumProperty.enumNames.Length; ++idx)
                if (enumProperty.enumNames[idx] == selectedName)
                {
                    enumProperty.enumValueIndex = idx;
                    break;
                }
        }

        public static T FilteredEnumPopup<T>(string label, T selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, params GUILayoutOption[] options) where T : struct, System.IConvertible
        {
            return FilteredEnumPopupInternal(selectedValue, predicate, (selectedIdx, names) => EditorGUILayout.Popup(label, selectedIdx, names, options), rename);
        }

        public static T FilteredEnumPopup<T>(T selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, params GUILayoutOption[] options) where T : struct, System.IConvertible
        {
            return FilteredEnumPopupInternal(selectedValue, predicate, (selectedIdx, names) => EditorGUILayout.Popup(selectedIdx, names, options), rename);
        }

        public static T FilteredEnumPopup<T>(Rect rect, string label, T selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, GUIStyle style = null) where T : struct, System.IConvertible
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, label, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, label, selectedIdx, names);
            };
            return FilteredEnumPopupInternal(selectedValue, predicate, show, rename);
        }

        public static T FilteredEnumPopup<T>(Rect rect, T selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, GUIStyle style = null) where T : struct, System.IConvertible
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, selectedIdx, names);
            };
            return FilteredEnumPopupInternal(selectedValue, predicate, show, rename);
        }

        public static void FilteredEnumPopup<T>(string label, SerializedProperty selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, params GUILayoutOption[] options) where T : struct, System.IConvertible
        {
            FilteredEnumPopupInternal(selectedValue, predicate, (selectedIdx, names) => EditorGUILayout.Popup(label, selectedIdx, names, options), rename);
        }

        public static void FilteredEnumPopup<T>(SerializedProperty selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, params GUILayoutOption[] options) where T : struct, System.IConvertible
        {
            FilteredEnumPopupInternal(selectedValue, predicate, (selectedIdx, names) => EditorGUILayout.Popup(selectedIdx, names, options), rename);
        }

        public static void FilteredEnumPopup<T>(Rect rect, string label, SerializedProperty selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, GUIStyle style = null) where T : struct, System.IConvertible
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, label, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, label, selectedIdx, names);
            };
            FilteredEnumPopupInternal(selectedValue, predicate, show, rename);
        }

        public static void FilteredEnumPopup<T>(Rect rect, SerializedProperty selectedValue, System.Func<T, bool> predicate, System.Func<string, string> rename = null, GUIStyle style = null) where T : struct, System.IConvertible
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, selectedIdx, names);
            };
            FilteredEnumPopupInternal(selectedValue, predicate, show, rename);
        }

        private static VRC_Trigger.TriggerEvent CustomTriggerPopupInternal(VRC_Trigger sourceTrigger, VRC_Trigger.TriggerEvent selectedValue, System.Func<int, string[], int> show)
        {
            if (sourceTrigger == null)
                return null;

            VRC_Trigger.TriggerEvent[]  actionsAry = sourceTrigger.Triggers.Where(t => t.TriggerType == VRC_Trigger.TriggerType.Custom).ToArray();
            string[] names = actionsAry.Select(t => t.Name).ToArray();

            int selectedIdx = Math.Max(0, names.Length - 1);
            if (selectedValue != null)
                for (; selectedIdx > 0; --selectedIdx)
                    if (names[selectedIdx] == selectedValue.Name)
                        break;
            if (actionsAry.Length == 0)
                return null;

            return actionsAry[show(selectedIdx, names)];
        }

        public static VRC_Trigger.TriggerEvent CustomTriggerPopup(Rect rect, VRC_Trigger sourceTrigger, VRC_Trigger.TriggerEvent selectedValue, GUIStyle style = null)
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, selectedIdx, names);
            };

            return CustomTriggerPopupInternal(sourceTrigger, selectedValue, show);
        }

        public static VRC_Trigger.TriggerEvent CustomTriggerPopup(Rect rect, string label, VRC_Trigger sourceTrigger, VRC_Trigger.TriggerEvent selectedValue, GUIStyle style = null)
        {
            System.Func<int, string[], int> show = (selectedIdx, names) =>
            {
                if (style != null)
                    return EditorGUI.Popup(rect, label, selectedIdx, names, style);
                else
                    return EditorGUI.Popup(rect, label, selectedIdx, names);
            };

            return CustomTriggerPopupInternal(sourceTrigger, selectedValue, show);
        }

        public static VRC_Trigger.TriggerEvent CustomTriggerPopup(VRC_Trigger sourceTrigger, VRC_Trigger.TriggerEvent selectedValue, params GUILayoutOption[] options)
        {
            return CustomTriggerPopupInternal(sourceTrigger, selectedValue, (selectedIdx, names) => EditorGUILayout.Popup(selectedIdx, names, options));
        }

        public static VRC_Trigger.TriggerEvent CustomTriggerPopup(string label, VRC_Trigger sourceTrigger, VRC_Trigger.TriggerEvent selectedValue, params GUILayoutOption[] options)
        {
            return CustomTriggerPopupInternal(sourceTrigger, selectedValue, (selectedIdx, names) => EditorGUILayout.Popup(label, selectedIdx, names, options));
        }

        public static VRC_Trigger.TriggerEvent CustomTriggerPopup(string label, VRC_Trigger sourceTrigger, string selectedValue, params GUILayoutOption[] options)
        {
            if (sourceTrigger == null)
                return null;

            return CustomTriggerPopup(label, sourceTrigger, sourceTrigger.Triggers.FirstOrDefault(t => t.TriggerType == VRC_Trigger.TriggerType.Custom && t.Name == selectedValue), options);
        }

        public static VRC_Trigger.TriggerEvent CustomTriggerPopup(VRC_Trigger sourceTrigger, string selectedValue, params GUILayoutOption[] options)
        {
            if (sourceTrigger == null)
                return null;

            return CustomTriggerPopup(sourceTrigger, sourceTrigger.Triggers.FirstOrDefault(t => t.TriggerType == VRC_Trigger.TriggerType.Custom && t.Name == selectedValue), options);
        }

        public static VRC_Trigger.TriggerEvent CustomTriggerPopup(Rect rect, VRC_Trigger sourceTrigger, string selectedValue, GUIStyle style = null)
        {
            if (sourceTrigger == null)
                return null;

            return CustomTriggerPopup(rect, sourceTrigger, sourceTrigger.Triggers.FirstOrDefault(t => t.TriggerType == VRC_Trigger.TriggerType.Custom && t.Name == selectedValue), style);
        }

        public static VRC_Trigger.TriggerEvent CustomTriggerPopup(Rect rect, string label, VRC_Trigger sourceTrigger, string selectedValue, GUIStyle style = null)
        {
            if (sourceTrigger == null)
                return null;

            return CustomTriggerPopup(rect, label, sourceTrigger, sourceTrigger.Triggers.FirstOrDefault(t => t.TriggerType == VRC_Trigger.TriggerType.Custom && t.Name == selectedValue), style);
        }

        private static void InternalSerializedCustomTriggerPopup(SerializedProperty triggersProperty, SerializedProperty customProperty, System.Func<int, string[], int> show, System.Action fail)
        {
            if (customProperty == null || (customProperty.propertyType != SerializedPropertyType.String))
                throw new ArgumentException("Expected a string for customProperty");
            if (triggersProperty == null || (!triggersProperty.isArray && triggersProperty.propertyType != SerializedPropertyType.ObjectReference))
                throw new ArgumentException("Expected an object or array for triggersProperty");

            List<String> customNames = new List<string>();
            bool allNull = true;

            if (triggersProperty.isArray)
            {
                int idx;
                for (idx = 0; idx < triggersProperty.arraySize; ++idx)
                {
                    GameObject obj = triggersProperty.GetArrayElementAtIndex(idx).objectReferenceValue as GameObject;
                    if (obj != null)
                    {
                        customNames = obj.GetComponent<VRC_Trigger>().Triggers.Where(t => t.TriggerType == VRC_Trigger.TriggerType.Custom).Select(t => t.Name).ToList();
                        allNull = false;
                        break;
                    }
                }
                for (; idx < triggersProperty.arraySize; ++idx)
                {
                    GameObject obj = triggersProperty.GetArrayElementAtIndex(idx).objectReferenceValue as GameObject;
                    if (obj != null)
                    {
                        List<string> thisCustomNames = obj.GetComponent<VRC_Trigger>().Triggers.Where(t => t.TriggerType == VRC_Trigger.TriggerType.Custom).Select(t => t.Name).ToList();
                        customNames.RemoveAll(s => thisCustomNames.Contains(s) == false);
                    }
                }
            }
            else
            {
                GameObject obj = triggersProperty.objectReferenceValue as GameObject;
                if (obj != null)
                {
                    allNull = false;
                    customNames = obj.GetComponent<VRC_Trigger>().Triggers.Where(t => t.TriggerType == VRC_Trigger.TriggerType.Custom).Select(t => t.Name).ToList();
                }
            }

            if (customNames.Count == 0 && !allNull && triggersProperty.isArray)
            {
                fail();
                customProperty.stringValue = "";
            }
            else
            {
                if (customNames.Count == 0)
                    customNames.Add("");

                int selectedIdx = Math.Max(0, customNames.Count - 1);
                if (!string.IsNullOrEmpty(customProperty.stringValue))
                    for (; selectedIdx > 0; --selectedIdx)
                        if (customNames[selectedIdx] == customProperty.stringValue)
                            break;

                selectedIdx = show(selectedIdx, customNames.ToArray());
                customProperty.stringValue = customNames[selectedIdx];
            }
        }

        public static void CustomTriggerPopup(string label, SerializedProperty triggersProperty, SerializedProperty customProperty, params GUILayoutOption[] options)
        {
            InternalSerializedCustomTriggerPopup(triggersProperty, customProperty, (idx, names) => EditorGUILayout.Popup(label, idx, names, options), () => EditorGUILayout.HelpBox("Receivers do not have Custom Triggers which share names.", MessageType.Warning));
        }

        public static void CustomTriggerPopup(SerializedProperty triggersProperty, SerializedProperty customProperty, params GUILayoutOption[] options)
        {
            InternalSerializedCustomTriggerPopup(triggersProperty, customProperty, (idx, names) => EditorGUILayout.Popup(idx, names, options), () => EditorGUILayout.HelpBox("Receivers do not have Custom Triggers which share names.", MessageType.Warning));
        }

        public static void CustomTriggerPopup(Rect rect, SerializedProperty triggersProperty, SerializedProperty customProperty, GUIStyle style = null)
        {
            InternalSerializedCustomTriggerPopup(triggersProperty, customProperty, (idx, names) => style == null ? EditorGUI.Popup(rect, idx, names) : EditorGUI.Popup(rect, idx, names, style), () => EditorGUI.HelpBox(rect, "Receivers do not have Custom Triggers which share names.", MessageType.Warning));
        }

        public static void CustomTriggerPopup(Rect rect, string label, SerializedProperty triggersProperty, SerializedProperty customProperty, GUIStyle style = null)
        {
            InternalSerializedCustomTriggerPopup(triggersProperty, customProperty, (idx, names) => style == null ? EditorGUI.Popup(rect, label, idx, names) : EditorGUI.Popup(rect, label, idx, names, style), () => EditorGUI.HelpBox(rect, "Receivers do not have Custom Triggers which share names.", MessageType.Warning));
        }

        public static void DrawTriggerActionCallback(string actionLabel, VRC_Trigger trigger, VRC_EventHandler.VrcEvent e)
        {
            VRC_Trigger.TriggerEvent triggerEvent = VRC_EditorTools.CustomTriggerPopup(actionLabel, trigger, e.ParameterString); 
            e.ParameterString = triggerEvent == null ? null : triggerEvent.Name;    
        }

        public static Dictionary<string, List<MethodInfo>> GetSharedPublicMethodsOnGameObjects(SerializedProperty objectsProperty)
        {
            Dictionary<string, List<MethodInfo>> methods = new Dictionary<string, List<MethodInfo>>();

            int idx = 0;
            for (; idx < objectsProperty.arraySize; ++idx)
            {
                SerializedProperty prop = objectsProperty.GetArrayElementAtIndex(idx);
                GameObject obj = prop.objectReferenceValue != null ? prop.objectReferenceValue as GameObject : null;
                if (obj != null)
                {
                    methods = VRC_EditorTools.GetPublicMethodsOnGameObject(obj);
                    break;
                }
            }
            List<string> toRemove = new List<string>();
            for (; idx < objectsProperty.arraySize; ++idx)
            {
                SerializedProperty prop = objectsProperty.GetArrayElementAtIndex(idx);
                GameObject obj = prop.objectReferenceValue != null ? prop.objectReferenceValue as GameObject : null;
                if (obj != null)
                {
                    Dictionary<string, List<MethodInfo>> thisObjMethods = VRC_EditorTools.GetPublicMethodsOnGameObject(obj);
                    foreach (string className in methods.Keys.Where(s => thisObjMethods.Keys.Contains(s) == false))
                        toRemove.Add(className);
                }
            }

            foreach (string className in toRemove)
                methods.Remove(className);

            return methods;
        }

        public static Dictionary<string, List<MethodInfo>> GetPublicMethodsOnGameObject(GameObject go)
        {
            Dictionary<string, List<MethodInfo>> methods = new Dictionary<string, List<MethodInfo>>();
            if (go == null)
                return methods;

            Component[] cs = go.GetComponents<Component>();
            foreach (Component c in cs)
            {
                Type t = c.GetType();

                // if component is in UnityEngine namespace
                if ((!string.IsNullOrEmpty(t.Namespace) && t.Namespace.Contains("UnityEngine")) || methods.ContainsKey(t.Name))
                    continue;

                // if component is the eventhandler
                if (t == typeof(VRC_EventHandler))
                    continue;

                List<MethodInfo> l = GetPublicMethodsForClass(t);
                methods.Add(t.Name, l);
            }

            return methods;
        }

        public static List<MethodInfo> GetPublicMethodsForClass(Type t)
        {
            // Get the public methods.
            MethodInfo[] myArrayMethodInfo = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            List<MethodInfo> methods = new List<MethodInfo>();
            // Display information for all methods.
            for (int i = 0; i < myArrayMethodInfo.Length; i++)
            {
                MethodInfo myMethodInfo = (MethodInfo)myArrayMethodInfo[i];
                methods.Add(myMethodInfo);
            }
            return methods;
        }

        public static byte[] ReadBytesFromProperty(SerializedProperty property)
        {
            byte[] bytes = new byte[property.arraySize];
            for (int idx = 0; idx < property.arraySize; ++idx)
                bytes[idx] = (byte)property.GetArrayElementAtIndex(idx).intValue;
            return bytes;
        }

        public static void WriteBytesToProperty(SerializedProperty property, byte[] bytes)
        {
            property.arraySize = bytes != null ? bytes.Length : 0;
            for (int idx = 0; idx < property.arraySize; ++idx)
                property.GetArrayElementAtIndex(idx).intValue = (int)bytes[idx];
        }
    }
}
#endif