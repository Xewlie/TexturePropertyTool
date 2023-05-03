using System;
using System.Collections.Generic;
using System.Linq;
using Editor;
using UnityEditor;
using UnityEngine;

namespace Plugins.TexturePropertyTool.Scripts.Editor
{
    
    // Utility class to manage EditorPrefs keys
    public static class EditorPrefsKeys
    {
        public const string PlatformType = "TexturePropertyTool_PlatformType";
        public const string DesktopFormatRGB = "TexturePropertyTool_DesktopFormatRGB";
        public const string DesktopFormatRGBA = "TexturePropertyTool_DesktopFormatRGBA";
        public const string MobileFormat = "TexturePropertyTool_MobileFormat";
        public const string CompressionQuality = "TexturePropertyTool_CompressionQuality";
        public const string AutoSetAlphaIsTransparency = "TexturePropertyTool_AutoSetAlphaIsTransparency";
    }

    public class TexturePropertyTool : TexturePropertyToolBase
    {
        // Define the tool name and header
        protected override string ToolName => "Texture Property Tool";
        protected override string Header => "Texture Property Tool";

        private class ToolSettings
        {
            public readonly List<string> IgnoreList = new() { "Assets/Plugins" };
            public readonly List<Texture2D> Textures = new();
            public PlatformType PlatformType = PlatformType.PC;
            public TextureImporterFormat MobileFormat = TextureImporterFormat.ASTC_4x4;
            public TextureImporterFormat DesktopFormatRGB = TextureImporterFormat.DXT1;
            public TextureImporterFormat DesktopFormatRGBA = TextureImporterFormat.DXT5;
            public bool AutoSetAlphaIsTransparency = false;
            public int CompressionQuality = 100;
        }

        private readonly ToolSettings settings = new();
        private Vector2 _scrollPosition = Vector2.zero;
        private readonly string[] _textureExtensions = { ".png", ".tga", ".jpg", ".jpeg" };
        private SerializedObject serializedIgnoreList;

        // Show the Texture Property Tool menu option
        [MenuItem("Tools/Texture Property Tool")]
        public static void ShowWindow()
        {
            GetWindow<TexturePropertyTool>("Texture Property Tool");
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            serializedIgnoreList = new SerializedObject(this);
            LoadEditorPrefs();
        }

        private void LoadEditorPrefs()
        {
            settings.PlatformType = (PlatformType)EditorPrefs.GetInt(EditorPrefsKeys.PlatformType, (int)settings.PlatformType);
            settings.DesktopFormatRGB = (TextureImporterFormat)EditorPrefs.GetInt(EditorPrefsKeys.DesktopFormatRGB, (int)settings.DesktopFormatRGB);
            settings.DesktopFormatRGBA = (TextureImporterFormat)EditorPrefs.GetInt(EditorPrefsKeys.DesktopFormatRGBA, (int)settings.DesktopFormatRGBA);
            settings.MobileFormat = (TextureImporterFormat)EditorPrefs.GetInt(EditorPrefsKeys.MobileFormat, (int)settings.MobileFormat);
            settings.CompressionQuality = EditorPrefs.GetInt(EditorPrefsKeys.CompressionQuality, settings.CompressionQuality);
            settings.AutoSetAlphaIsTransparency = EditorPrefs.GetBool(EditorPrefsKeys.AutoSetAlphaIsTransparency, settings.AutoSetAlphaIsTransparency);
        }

        private void SaveEditorPrefs()
        {
            EditorPrefs.SetInt(EditorPrefsKeys.PlatformType, (int)settings.PlatformType);
            EditorPrefs.SetInt(EditorPrefsKeys.DesktopFormatRGB, (int)settings.DesktopFormatRGB);
            EditorPrefs.SetInt(EditorPrefsKeys.DesktopFormatRGBA, (int)settings.DesktopFormatRGBA);
            EditorPrefs.SetInt(EditorPrefsKeys.CompressionQuality, settings.CompressionQuality);
            EditorPrefs.SetInt(EditorPrefsKeys.MobileFormat, (int)settings.MobileFormat);
            EditorPrefs.SetInt(EditorPrefsKeys.AutoSetAlphaIsTransparency, settings.AutoSetAlphaIsTransparency ? 1 : 0);
        }

        // Draw any additional control elements
        protected override void DrawAdditionalControls()
        {
            // Draw the IgnoreList GUI element
            DrawIgnoreList();

            // Draw the GUI element for selecting the platform type
            settings.PlatformType = (PlatformType)EditorGUILayout.EnumPopup("Platform Type", settings.PlatformType);

            // Switch statement to choose which GUI elements to draw based on the platform type selected
            switch (settings.PlatformType)
            {
                case PlatformType.PC:
                    settings.DesktopFormatRGB = (TextureImporterFormat)EditorGUILayout.EnumPopup("Desktop Format RGB", settings.DesktopFormatRGB);
                    settings.DesktopFormatRGBA = (TextureImporterFormat)EditorGUILayout.EnumPopup("Desktop Format RGBA", settings.DesktopFormatRGBA);
                    break;
                case PlatformType.Android or PlatformType.iOS:
                    settings.MobileFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("Mobile Format", settings.MobileFormat);
                    break;
            }

            // GUI element for selecting the compression quality
            settings.CompressionQuality = EditorGUILayout.IntSlider(new GUIContent("Compression Quality", "Set the compression quality for the selected platform."),
                settings.CompressionQuality, 0, 100);

            EditorGUILayout.BeginHorizontal();
            EditorGUIUtility.labelWidth = 180;  // You can adjust this value as needed
            settings.AutoSetAlphaIsTransparency = EditorGUILayout.Toggle(new GUIContent("Auto Set Alpha Is Transparency", "Adjust the space between the label and the toggle."), settings.AutoSetAlphaIsTransparency);
            EditorGUILayout.EndHorizontal();

            // Button to process textures
            if (settings.Textures.Count > 1)
            {
                if (GUILayout.Button("Process Textures"))
                {
                    OnProcessTexturesButtonClick();
                }
            }

            SaveEditorPrefs();
        }

        // Action to be taken when the "Load" button is clicked
        protected override void OnLoadButtonClick()
        {
            // Load all textures using the filtering methods below
            LoadAllTextures();
        }

        // Action to be taken when the "Process Textures" button is clicked
        private void OnProcessTexturesButtonClick()
        {
            // Processing logic
            Debug.Log("Processing textures...");

            // Run the Reimport Logic, always sets flag back off in finally bl
            try
            {
                EditorPrefs.SetBool("TexturePropertyTool_IsProcessing", true); // Set flag before processing
                TextureReImporter.ReimportAllTextures(settings.Textures, settings.PlatformType, settings.MobileFormat, settings.DesktopFormatRGB, settings.DesktopFormatRGBA, settings.CompressionQuality, settings.AutoSetAlphaIsTransparency);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                EditorPrefs.SetBool("TexturePropertyTool_IsProcessing", false); // Unset flag after processing
            }

        }

        // Method to filter and load all textures
        private void LoadAllTextures()
        {
            // Find all texture GUIDs in the project and convert them to their asset paths
            var texturePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets" });
            var filteredPaths = texturePaths.Select(AssetDatabase.GUIDToAssetPath).Where(assetPath =>
                IsCorrectTextureExtension(assetPath) && IsNotInIgnoreList(assetPath)).ToList();

            // Clear current saved textures and add loaded textures to the list
            settings.Textures.Clear();
            foreach (var loadedTexture in filteredPaths.Select(AssetDatabase.LoadAssetAtPath<Texture2D>))
            {
                if (loadedTexture != null)
                {
                    settings.Textures.Add(loadedTexture);   
                }
            }
        }

        // Check if the asset path ends with a correct texture extension
        private bool IsCorrectTextureExtension(string assetPath)
        {
            return _textureExtensions.Any(extension => assetPath.EndsWith(extension, System.StringComparison.OrdinalIgnoreCase));
        }

        // Check if the asset path is not in the IgnoreList
        private bool IsNotInIgnoreList(string assetPath)
        {
            return settings.IgnoreList.All(ignorePath => !assetPath.StartsWith(ignorePath));
        }

        // Draw the IgnoreList GUI element
        private void DrawIgnoreList()
        {
            GUILayout.Label("Ignore List:", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            for (var i = 0; i < settings.IgnoreList.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                settings.IgnoreList[i] = EditorGUILayout.TextField(settings.IgnoreList[i]);
                if (GUILayout.Button("-", GUILayout.Width(30)))
                {
                    settings.IgnoreList.RemoveAt(i);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;

            // "Add" button for IgnoreList
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+", GUILayout.Width(30)))
            {
                settings.IgnoreList.Add("");
            }
            GUILayout.EndHorizontal();
        }

        // Draw the list of loaded textures
        private void DrawTextureList()
        {
            if (settings.Textures == null) return;

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));
            for (var i = 0; i < settings.Textures.Count; i++)
            {
                settings.Textures[i] = (Texture2D)EditorGUILayout.ObjectField(settings.Textures[i], typeof(Texture2D), false);
            }
            EditorGUILayout.EndScrollView();
        }

        protected override void OnGUI()
        {
            base.OnGUI();

            // Draw the list of loaded textures
            DrawTextureList();
        }
    }
}

