#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ReferenceHighlighter
{
    public class ReferenceHighlighterWindow : EditorWindow
    {
        #region Public Variables

        public static bool Visible = false;

        public (Object, bool) CurrentStackReference
        {
            get
            {
                if(_stackIndex == -1)
                {
                    return (null, false);
                }

                return _objectStack[_stackIndex];
            }
        }

        #endregion

        #region Private Variables

        private Editor               _referenceEditor     = null;
        private Object               _lastObjectReference = null;
        private List<(Object, bool)> _objectStack         = new List<(Object, bool)>();
        private int                  _stackIndex          = -1;

        #endregion

        #region Unity Methods

        private void Awake()
        {
            wantsMouseEnterLeaveWindow = true;
            _stackIndex                = -1;
            _objectStack.Clear();
            Visible = true;
        }

        private void OnGUI()
        {
            Event current = Event.current;

            if(current.type == EventType.MouseEnterWindow)
            {
                ReferenceHighlighter.ObjectReference = _lastObjectReference;

                if(ReferenceHighlighter.IsHierarchyObject)
                {
                    EditorApplication.RepaintHierarchyWindow();
                }
                else
                {
                    EditorApplication.RepaintProjectWindow();
                }
            }
            else if(current.type == EventType.MouseLeaveWindow)
            {
                ReferenceHighlighter.ObjectReference = null;

                EditorApplication.RepaintHierarchyWindow();
                EditorApplication.RepaintProjectWindow();
            }

            using(EditorGUILayout.HorizontalScope hScope = new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using(EditorGUI.DisabledGroupScope dScope = new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.ObjectField(_lastObjectReference, _lastObjectReference.GetType(), false);
                }

                using(EditorGUI.DisabledGroupScope dScope = new EditorGUI.DisabledGroupScope(_stackIndex == 0))
                {
                    if(GUILayout.Button("Previous", GUILayout.MaxWidth(70f)))
                    {
                        SelectPreviousReference();
                    }
                }

                using(EditorGUI.DisabledGroupScope dScope =
                    new EditorGUI.DisabledGroupScope(_stackIndex == _objectStack.Count - 1))
                {
                    if(GUILayout.Button("Next", GUILayout.MaxWidth(70f)))
                    {
                        SelectNextReference();
                    }
                }

                ReferenceHighlighter.AutoUpdateReference =
                    EditorGUILayout.ToggleLeft("Auto Update", ReferenceHighlighter.AutoUpdateReference,
                                               GUILayout.MaxWidth(90f));
            }

            _referenceEditor.DrawHeader();
            _referenceEditor.DrawDefaultInspector();
        }

        private void OnDestroy()
        {
            ReferenceHighlighter.ObjectReference = null;
            EditorApplication.RepaintHierarchyWindow();
            EditorApplication.RepaintProjectWindow();
            Visible = false;
        }

        #endregion

        #region Custom Methods

        public void UpdateWindowReference(bool updateStack = true)
        {
            if(_referenceEditor == null ||
               (ReferenceHighlighter.ObjectReference != null &&
                _lastObjectReference                 != ReferenceHighlighter.ObjectReference))
            {
                _lastObjectReference = ReferenceHighlighter.ObjectReference;
                _referenceEditor     = Editor.CreateEditor(ReferenceHighlighter.ObjectReference);

                if(updateStack)
                {
                    int stackCount = _objectStack.Count;

                    if(_stackIndex < stackCount - 1)
                    {
                        _objectStack.RemoveRange(_stackIndex + 1, stackCount - _stackIndex - 1);
                    }

                    _objectStack.Add((_lastObjectReference, ReferenceHighlighter.IsHierarchyObject));
                    _stackIndex++;
                }
            }
        }

        public void SelectPreviousReference()
        {
            if(_stackIndex > 0)
            {
                _stackIndex--;

                ReferenceHighlighter.ObjectReference = _objectStack[_stackIndex].Item1;
                ReferenceHighlighter.UpdateLastReference(ReferenceHighlighter.ObjectReference);
                ReferenceHighlighter.IsHierarchyObject = _objectStack[_stackIndex].Item2;
                UpdateWindowReference(false);
            }
        }

        public void SelectNextReference()
        {
            int stackCount = _objectStack.Count;

            if(stackCount > 0 && _stackIndex < stackCount - 1)
            {
                _stackIndex++;

                ReferenceHighlighter.ObjectReference = _objectStack[_stackIndex].Item1;
                ReferenceHighlighter.UpdateLastReference(ReferenceHighlighter.ObjectReference);
                ReferenceHighlighter.IsHierarchyObject = _objectStack[_stackIndex].Item2;
                UpdateWindowReference(false);
            }
        }

        #endregion
    }
}
#endif