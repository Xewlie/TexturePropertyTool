using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR

namespace Plugins.TexturePropertyTool.Scripts
{
    public class TextureReImporter : AssetPostprocessor
    {

        public static void ReimportAllTextures(List<Texture2D> textureList, PlatformType platformType, TextureImporterFormat mobileFormat, TextureImporterFormat desktopFormatRGB, TextureImporterFormat desktopFormatRGBA, int compressionQuality, bool setAlphaTransparency)
        {
            ImportAssets(textureList, platformType, mobileFormat, desktopFormatRGB, desktopFormatRGBA, compressionQuality, setAlphaTransparency);
            Resources.UnloadUnusedAssets();
        }

        private static void ImportAssets(List<Texture2D> textureList, PlatformType platformType, TextureImporterFormat mobileFormat, TextureImporterFormat desktopFormatRGB, TextureImporterFormat desktopFormatRGBA, int compressionQuality, bool setAlphaTransparency)
        {
            foreach (Texture2D texture in textureList)
            {
                string assetPath = AssetDatabase.GetAssetPath(texture);
                string platformString = GetPlatformString(platformType);

                switch (platformType)
                {
                    case PlatformType.Android or PlatformType.iOS:
                        // checks mobile if the texture should be reimported before attempting to.
                        if (CheckMobileFormat(platformString, mobileFormat, compressionQuality, assetPath, setAlphaTransparency))
                        {
                            Debug.Log("Reimporting mobile texture: " + assetPath + " with new settings");
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);   
                        }
                        else
                        {
                            Debug.LogWarning("Format already set for mobile texture: " + assetPath + " not setting new settings");
                        }
                        break;
                    case PlatformType.PC:
                        // checks pc if the texture should be reimported before attempting to.
                        if (CheckDesktopFormat(platformString, desktopFormatRGB, desktopFormatRGBA, compressionQuality, assetPath, setAlphaTransparency))
                        {
                            Debug.Log("Reimporting desktop texture: " + assetPath + " with new settings");
                            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.Default);   
                        }
                        else
                        {
                            Debug.LogWarning("Format already set for desktop texture: " + assetPath + " not setting new settings");   
                        }
                        break;
                    default:
                        Debug.LogWarning("Unknown Format set for TextureReimporter, skipping");
                        break;
                }
            }
        }

        private static bool CheckDesktopFormat(string platformString, TextureImporterFormat desktopFormatRGB, 
            TextureImporterFormat desktopFormatRGBA, int compressionQuality, string asset, bool setAlphaTransparency)
        {
        
            // Get the texture importer
            TextureImporter textureImporter = AssetImporter.GetAtPath(asset) as TextureImporter;
        
            // store texture format
            var currentFormat = textureImporter.GetPlatformTextureSettings(platformString).format;
        
            // will format change
            bool willFormatChange = textureImporter.DoesSourceTextureHaveAlpha() ? currentFormat != desktopFormatRGBA : currentFormat != desktopFormatRGB;
        
            // will compression change
            bool willQualityChange = textureImporter.compressionQuality != compressionQuality;
        
            // will alpha change
            bool willAlphaChange = CheckIfGoingToChangeAlpha(textureImporter, setAlphaTransparency);

            return willFormatChange || willQualityChange || willAlphaChange;
        }
    
        private static bool CheckMobileFormat(string platformString, TextureImporterFormat mobileFormat,
            int compressionQuality, string asset, bool setAlphaTransparency)
        {
            // Get the texture importer
            TextureImporter textureImporter = AssetImporter.GetAtPath(asset) as TextureImporter;
    
            // store texture format
            var currentFormat = textureImporter.GetPlatformTextureSettings(platformString).format;
    
            // will format change
            bool willFormatChange = currentFormat != mobileFormat;
    
            // will compression change
            bool willQualityChange = textureImporter.compressionQuality != compressionQuality;
    
            // will alpha change
            bool willAlphaChange = CheckIfGoingToChangeAlpha(textureImporter, setAlphaTransparency);

            return willFormatChange || willQualityChange || willAlphaChange;
        }
    
        private static bool CheckIfGoingToChangeAlpha(TextureImporter importer, bool autoSetAlphaIsTransparency)
        {
            return autoSetAlphaIsTransparency && importer.DoesSourceTextureHaveAlpha() && !importer.alphaIsTransparency;
        }

        private static string GetPlatformString(PlatformType platformType)
        {
            return platformType switch
            {
                PlatformType.Android => "Android",
                PlatformType.iOS => "iPhone",
                PlatformType.PC => "Standalone",
                _ => throw new ArgumentException($"Invalid PlatformType: {platformType}")
            };
        }
    }
}

#endif
