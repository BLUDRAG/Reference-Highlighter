#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReferenceHighlighter
{
    [CustomPropertyDrawer(typeof(Object), true)]
    public class ObjectHighlighterDrawer : PropertyDrawer
    {
        #region Private Variables

        private        bool       _currentlyMousingOver = false;
        private        MethodInfo _defaultDraw          = null;
        private static int        _totalMouseOvers      = 0;

        #endregion

        #region Unity Methods

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if(_defaultDraw == null)
            {
                _defaultDraw = typeof(EditorGUI).GetMethod("DefaultPropertyField",
                                                           BindingFlags.Static | BindingFlags.Public |
                                                           BindingFlags.NonPublic);
            }

            _defaultDraw.Invoke(null, new object[3]
                                      {
                                          position, property, label
                                      });

            Event current = Event.current;

            if(current.type == EventType.Repaint)
            {
                if(position.Contains(current.mousePosition))
                {
                    if(!_currentlyMousingOver)
                    {
                        _currentlyMousingOver = true;
                        _totalMouseOvers++;

                        if(property.objectReferenceValue)
                        {
                            ReferenceHighlighter.ClearReferences();
                            ReferenceHighlighter.ObjectReference   = property.objectReferenceValue;
                            ReferenceHighlighter.IsHierarchyObject = !AssetDatabase.Contains(property.objectReferenceValue);

                            EditorApplication.RepaintHierarchyWindow();
                            EditorApplication.RepaintProjectWindow();
                        }
                    }
                }
                else
                {
                    if(_currentlyMousingOver)
                    {
                        _currentlyMousingOver = false;
                        _totalMouseOvers--;
                    }

                    if(_totalMouseOvers <= 0)
                    {
                        if(ReferenceHighlighterWindow.Visible)
                        {
                            ReferenceHighlighter.ClearReferences();

                            ReferenceHighlighterWindow window = EditorWindow.GetWindow<ReferenceHighlighterWindow>();

                            ReferenceHighlighter.ObjectReference   = window.CurrentStackReference.Item1;
                            ReferenceHighlighter.IsHierarchyObject = window.CurrentStackReference.Item2;
                        }
                        else
                        {
                            ReferenceHighlighter.ObjectReference = null;
                            ReferenceHighlighter.ClearReferences();

                            EditorApplication.RepaintHierarchyWindow();
                            EditorApplication.RepaintProjectWindow();
                        }
                    }
                }
            }
        }

        #endregion
    }
}
#endif