using UnityEditor;
using UnityEngine;

namespace Editor
{
    public abstract class TexturePropertyToolBase : EditorWindow
    {
        protected abstract string ToolName { get; }
        protected abstract string Header { get; }

        private GUIStyle HeaderStyle;
        private GUIContent LoadButtonContent => new("Load Textures");

        protected virtual void OnEnable()
        {
            InitializeStyles();
        }

        private void InitializeStyles()
        {
            HeaderStyle = new GUIStyle
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = new GUIStyleState
                {
                    textColor = Color.white
                }
            };
        }

        protected virtual void OnGUI()
        {
            DrawHeader();
            DrawAdditionalControls();
            DrawLoadButton();
        }

        private void DrawHeader()
        {
            if (HeaderStyle == null)
            {
                InitializeStyles();
            }
            EditorGUILayout.LabelField(Header, HeaderStyle, GUILayout.Height(30));
        }

        // Override this method in derived classes to add more controls
        protected virtual void DrawAdditionalControls() { }

        private void DrawLoadButton()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button(LoadButtonContent))
            {
                OnLoadButtonClick();
            }

            GUILayout.EndHorizontal();
        }

        // Override this method in derived classes to implement the logic for the load button
        protected abstract void OnLoadButtonClick();
    }
}