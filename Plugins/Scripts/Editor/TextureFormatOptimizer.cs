using System.Linq;
using Editor;
using UnityEditor;
using UnityEngine;

namespace Plugins.TexturePropertyTool.Scripts.Editor
{
    public class TextureFormatOptimizer : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            // Only process textures if processing from Texture Property Tool
            // which is detected with an editor pref that's set before it starts and turned off when it ends
            if (!IsProcessingFromTool())
            {
                //Debug.Log("Skipping Texture because not processing from tool"); // remove this ltaer
                return;
            }

            // load configuration from texture property tool
            var settings = LoadSettings();
            
            // checks to make sure all texture property tool config exists, if not, abort
            if (CheckForInvalidSettings(settings)) return;

            var importer = assetImporter as TextureImporter;

            // Process Texture based on platform type
            switch (settings.Value.platformType)
            {
                case PlatformType.PC:
                {
                    ProcessDesktopTexture(importer, settings.Value.desktopFormatRGB, settings.Value.desktopFormatRGBA, settings.Value.compressionQuality, settings.Value.autoSetAlphaIsTransparency);
                    break;   
                }
                case PlatformType.Android or PlatformType.iOS:
                {
                    ProcessMobileTexture(importer, settings.Value.platformType, settings.Value.mobileFormat, settings.Value.compressionQuality, settings.Value.autoSetAlphaIsTransparency);
                    break;
                }
                default:
                {
                    Debug.LogWarning("Unknown PlatformType set for TextureFormatOptimizer, skipping");
                    break;   
                }
            }
        }

        // Process Desktop Texture
        private void ProcessDesktopTexture(TextureImporter importer, TextureImporterFormat desktopFormatRGB, 
            TextureImporterFormat desktopFormatRGBA, int compressionQuality, bool autoSetAlphaIsTransparency)
        {
            // Get Platform Settings for PC
            var platformSettings = importer.GetPlatformTextureSettings("Standalone");
            
            // if texture is single channel, skip (Only skipped for standalone/pc) haven't made support for that yet, will later
            // TO DO: I don't remember what issue there was for this, address it later.
            if (CheckIfSingleChannel(importer)) return;
            
            // set the "override for platform" tickbox
            platformSettings.overridden = true;

            // detect if the texture genuinely has transparency by physically checking the texture
            // if it does, use the texture property tool setting for appropriate format for RGB/RGBA
            // This is only for standalone, mobile formats generally have formats supporting both at the same time
            platformSettings.format = importer.DoesSourceTextureHaveAlpha() ? desktopFormatRGBA : desktopFormatRGB;

            // Sets the "Alpha is Transparency" tickbox in the texture if texture has it, and
            // it was enabled in the texture property tool, this won't change its current setting unless
            // you specifically asked it to
            if (autoSetAlphaIsTransparency)
            {
                importer.alphaIsTransparency = importer.DoesSourceTextureHaveAlpha();
            }

            // make both the platform texture settings object, and importer both set compression quality
            // TO DO: this is probably not needed, but I'm not sure, so I'm leaving it in for now
            importer.compressionQuality = compressionQuality;
            platformSettings.compressionQuality = compressionQuality;

            importer.SetPlatformTextureSettings(platformSettings);
        }

        // Process Mobile Texture
        private void ProcessMobileTexture(TextureImporter importer, PlatformType platformType, 
            TextureImporterFormat mobileFormat, int compressionQuality, bool autoSetAlphaIsTransparency)
        {
            // Get Platform Settings for Android or iOS
            var platformSettings = importer.GetPlatformTextureSettings(GetMobilePlatformString(platformType));

            // set the "override for platform" tickbox
            platformSettings.overridden = true;
            
            // Set the format from texture property tool
            platformSettings.format = mobileFormat;
            
            // Sets the "Alpha is Transparency" tickbox in the texture if texture has it, and
            // it was enabled in the texture property tool
            importer.alphaIsTransparency = autoSetAlphaIsTransparency && importer.DoesSourceTextureHaveAlpha();

            // make both the platform texture settings object, and importer both set compression quality
            // TO DO: this is probably not needed, but I'm not sure, so I'm leaving it in for now
            importer.compressionQuality = compressionQuality;
            platformSettings.compressionQuality = compressionQuality;

            importer.SetPlatformTextureSettings(platformSettings);
        }

        private static string GetMobilePlatformString(PlatformType platformType)
        {
            var platformString = (platformType == PlatformType.Android) ? "Android" : "iPhone";
            return platformString;
        }

        private bool CheckIfSingleChannel(TextureImporter importer)
        {
            if (importer.textureType == TextureImporterType.SingleChannel)
            {
                Debug.LogWarning($"Warning: Texture at {assetPath} doesn't support this tool yet, skipping");
                return true;
            }

            return false;
        }
        
        // scan texture to see if normal map (unused/not working)
        private bool ScanTexture(TextureImporter importer)
        {
            bool CheckIfNormalMap(Texture2D texture, float bluePercent)
            {
                const float threshold = 200f;
                int totalPixels = texture.width * texture.height;

                Color[] pixels = texture.GetPixels();

                int countBluePixels = pixels.Count(pixel => pixel.b > threshold / 255);

                float blueRatio = (float)countBluePixels / totalPixels;

                return blueRatio >= bluePercent;
            }
            
            if (importer.textureType != TextureImporterType.Default) return false;

            // Load the texture
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetImporter.assetPath);

            if (texture == null) return false;

            // Check if the texture is a normal map
            const float bluePercentage = 0.75f;
            bool isNormalMap = CheckIfNormalMap(texture, bluePercentage);

            return isNormalMap;
        }
        
        // Load Settings Mega Turple
        private (PlatformType platformType, TextureImporterFormat desktopFormatRGB, TextureImporterFormat desktopFormatRGBA, TextureImporterFormat mobileFormat, int compressionQuality, bool autoSetAlphaIsTransparency)? LoadSettings()
        {
            if (!EditorPrefs.HasKey(EditorPrefsKeys.PlatformType) ||
                !EditorPrefs.HasKey(EditorPrefsKeys.DesktopFormatRGB) ||
                !EditorPrefs.HasKey(EditorPrefsKeys.DesktopFormatRGBA) ||
                !EditorPrefs.HasKey(EditorPrefsKeys.MobileFormat) ||
                !EditorPrefs.HasKey(EditorPrefsKeys.CompressionQuality) || 
                !EditorPrefs.HasKey(EditorPrefsKeys.AutoSetAlphaIsTransparency))
            {
                return null;
            }

            return (
                (PlatformType)EditorPrefs.GetInt(EditorPrefsKeys.PlatformType),
                (TextureImporterFormat)EditorPrefs.GetInt(EditorPrefsKeys.DesktopFormatRGB),
                (TextureImporterFormat)EditorPrefs.GetInt(EditorPrefsKeys.DesktopFormatRGBA),
                (TextureImporterFormat)EditorPrefs.GetInt(EditorPrefsKeys.MobileFormat),
                EditorPrefs.GetInt(EditorPrefsKeys.CompressionQuality),
                EditorPrefs.GetBool(EditorPrefsKeys.AutoSetAlphaIsTransparency)
            );
        }
        
        private bool CheckForInvalidSettings(
            (PlatformType platformType, TextureImporterFormat desktopFormatRGB, TextureImporterFormat desktopFormatRGBA,
                TextureImporterFormat mobileFormat, int compressionQuality, bool autoSetAlphaIsTransparency)? settings)
        {
            if (!settings.HasValue)
            {
                Debug.LogWarning(
                    "Some Texture Property Tool settings are missing, so no texture format optimization will be performed.");
                return true;
            }

            return false;
        }
        
        private bool IsProcessingFromTool()
        {
            return EditorPrefs.GetBool("TexturePropertyTool_IsProcessing", false);
        }
        
    }
}
