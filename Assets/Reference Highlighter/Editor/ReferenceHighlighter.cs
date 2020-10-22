#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReferenceHighlighter
{
    [InitializeOnLoad]
    public class ReferenceHighlighter
    {
        #region Public Variables

        public static Object ObjectReference;
        public static bool   IsHierarchyObject   = false;
        public static bool   AutoUpdateReference = false;

        #endregion

        #region Private Variables

        private static Object                             _lastObjectReference = null;
        private static Texture2D                          _targetReferenceTexture;
        private static Texture2D                          _highlightTexture;
        private static HashSet<int>                       _currentHierarchyReferences = new HashSet<int>();
        private static HashSet<string>                       _currentProjectReferences = new HashSet<string>();
        private static FieldInfo                          eventCallbackInfo  = null;
        private static EditorApplication.CallbackFunction eventCallback      = null;

        #endregion

        static ReferenceHighlighter()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnDrawHierarchyItem;
            EditorApplication.projectWindowItemOnGUI   += OnDrawProjectItem;
            EditorApplication.update                   += AutoUpdateHighlighterWindow;

            _highlightTexture = new Texture2D(1, 1);

            _highlightTexture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? Color.blue : Color.magenta);
            _highlightTexture.Apply();

            _targetReferenceTexture = new Texture2D(1, 1);

            _targetReferenceTexture.SetPixel(0, 0, EditorGUIUtility.isProSkin ? Color.magenta : Color.cyan);
            _targetReferenceTexture.Apply();

            eventCallbackInfo = typeof(EditorApplication).GetField("globalEventHandler",
                                                                   BindingFlags.Static |
                                                                   BindingFlags.NonPublic);

            eventCallback =  (EditorApplication.CallbackFunction)eventCallbackInfo.GetValue(null);
            eventCallback += UpdateWindowOnKeyPress;

            eventCallbackInfo.SetValue(null, eventCallback);
        }

        ~ReferenceHighlighter()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnDrawHierarchyItem;
            EditorApplication.projectWindowItemOnGUI   -= OnDrawProjectItem;
            EditorApplication.update                   -= AutoUpdateHighlighterWindow;

            eventCallback -= UpdateWindowOnKeyPress;

            eventCallbackInfo.SetValue(null, eventCallback);
        }

        #region Custom Methods

        public static void OnDrawHierarchyItem(int id, Rect rect)
        {
            if(IsHierarchyObject)
            {
                if(ObjectReference != null && ObjectReference != _lastObjectReference)
                {
                    GameObject hierarchyObject = EditorUtility.InstanceIDToObject(id) as GameObject;

                    if(hierarchyObject)
                    {
                        if(ObjectReference != null && !_currentHierarchyReferences.Contains(id))
                        {
                            _lastObjectReference = ObjectReference;
                            ClearReferences();

                            if(ObjectReference.GetType().IsSubclassOf(typeof(Component)))
                            {
                                CaptureObjectHierarchy((_lastObjectReference as Component).gameObject);
                            }
                            else
                            {
                                CaptureObjectHierarchy(_lastObjectReference as GameObject);
                            }
                        }
                    }
                }
                else if(ObjectReference == null)
                {
                    _lastObjectReference = null;
                    ClearReferences();
                }

                if(_currentHierarchyReferences.Contains(id))
                {
                    float iconWidth = 16f;

                    rect.x     += iconWidth;
                    rect.width -= iconWidth;

                    if(_lastObjectReference.GetType().IsSubclassOf(typeof(Component)))
                    {
                        GUI.DrawTexture(rect,
                                        id == (_lastObjectReference as Component).gameObject.GetInstanceID()
                                            ? _targetReferenceTexture
                                            : _highlightTexture);
                    }
                    else
                    {
                        GUI.DrawTexture(rect,
                                        id == (_lastObjectReference as GameObject).GetInstanceID()
                                            ? _targetReferenceTexture
                                            : _highlightTexture);
                    }

                    rect.y -= 1f;

                    GUI.Label(rect, EditorUtility.InstanceIDToObject(id).name);
                }
            }
        }

        public static void OnDrawProjectItem(string guid, Rect rect)
        {
            if(!IsHierarchyObject)
            {
                if(ObjectReference != null && ObjectReference != _lastObjectReference)
                {
                    if(ObjectReference != null && !_currentProjectReferences.Contains(guid))
                    {
                        _lastObjectReference = ObjectReference;
                        ClearReferences();

                        CaptureObjectProject(_lastObjectReference);
                    }
                }
                else if(ObjectReference == null)
                {
                    _lastObjectReference = null;
                    ClearReferences();
                }

                if(_currentProjectReferences.Contains(guid))
                {
                    float iconWidth = 16f;

                    rect.x     += iconWidth;
                    rect.width -= iconWidth;

                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_lastObjectReference, out string referenceGUID,
                                                                   out long _);
                    
                    GUI.DrawTexture(rect, guid == referenceGUID
                                        ? _targetReferenceTexture
                                        : _highlightTexture);

                    rect.y -= 1f;

                    GUI.Label(rect, AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid)).name);
                }
            }
        }

        private static void CaptureObjectHierarchy(GameObject target)
        {
            _currentHierarchyReferences.Add(target.GetInstanceID());

            if(target.transform.parent != null)
            {
                CaptureObjectHierarchy(target.transform.parent.gameObject);
            }
        }

        private static void CaptureObjectProject(Object target)
        {
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out string referenceGUID,
                                                           out long _);

            _currentProjectReferences.Add(referenceGUID);
            
            string path = AssetDatabase.GUIDToAssetPath(referenceGUID);

            if(Path.GetFileName(path) != "Assets")
            {
                CaptureObjectProject(AssetDatabase.LoadAssetAtPath<Object>(Directory.GetParent(path).ToString()));
            }
        }

        private static void ShowHighlighterWindow()
        {
            if(ObjectReference != null)
            {
                ReferenceHighlighterWindow window = EditorWindow.GetWindow<ReferenceHighlighterWindow>();

                window.UpdateWindowReference();
                window.wantsMouseEnterLeaveWindow = true;
            }
        }

        private static void UpdateWindowOnKeyPress()
        {
            if(Event.current.control)
            {
                ShowHighlighterWindow();
            }
        }

        private static void AutoUpdateHighlighterWindow()
        {
            if(AutoUpdateReference)
            {
                ShowHighlighterWindow();
            }
        }

        public static void UpdateLastReference(Object reference)
        {
            _lastObjectReference = reference;
        }

        public static void ClearReferences()
        {
            _currentHierarchyReferences.Clear();
            _currentProjectReferences.Clear();
        }

        #endregion
    }
}
#endif