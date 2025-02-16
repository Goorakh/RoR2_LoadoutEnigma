using BepInEx.Bootstrap;
using LoadoutEnigma.Utilities;
using RiskOfOptions;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LoadoutEnigma.ModCompatibility
{
    static class RiskOfOptionsCompat
    {
        public static bool Enabled => Chainloader.PluginInfos.ContainsKey(RiskOfOptions.PluginInfo.PLUGIN_GUID);

        static Sprite _iconSprite;

        const string MOD_GUID = LoadoutEnigmaPlugin.PluginGUID;
        const string MOD_NAME = LoadoutEnigmaPlugin.PluginName;

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void AddOptions()
        {
            ModSettingsManager.SetModDescription($"Options for {MOD_NAME}", MOD_GUID, MOD_NAME);

            Sprite icon = tryGetIcon();
            if (icon)
            {
                ModSettingsManager.SetModIcon(icon, MOD_GUID, MOD_NAME);
            }

            // ModSettingsManager.AddOption(...)
        }

        static Sprite tryGetIcon()
        {
            if (!_iconSprite)
            {
                _iconSprite = tryGenerateIcon();

                if (!_iconSprite)
                {
                    Log.Warning("Failed to get config icon");
                }
            }

            return _iconSprite;
        }

        static Sprite tryGenerateIcon()
        {
            DirectoryInfo modPluginDir = new DirectoryInfo(Path.GetDirectoryName(LoadoutEnigmaPlugin.Instance.Info.Location));
            DirectoryInfo bepInExPluginsDir = new DirectoryInfo(BepInEx.Paths.PluginPath);

            FileInfo iconFile = FileUtils.SearchUpwards(modPluginDir, bepInExPluginsDir, "icon.png");
            if (iconFile == null)
                return null;

            Log.Debug($"Found icon file at: {iconFile.FullName}");

            byte[] imageBytes;
            try
            {
                imageBytes = File.ReadAllBytes(iconFile.FullName);
            }
            catch (Exception e)
            {
                Log.Error_NoCallerPrefix($"Failed to read icon file '{iconFile.FullName}': {e}");
                return null;
            }

            Texture2D iconTexture = new Texture2D(256, 256);
            iconTexture.name = $"tex{LoadoutEnigmaPlugin.PluginName}Icon";
            if (!iconTexture.LoadImage(imageBytes))
            {
                GameObject.Destroy(iconTexture);
                Log.Error("Failed to load icon into texture");
                return null;
            }

            Sprite icon = Sprite.Create(iconTexture, new Rect(0f, 0f, iconTexture.width, iconTexture.height), new Vector2(0.5f, 0.5f));
            icon.name = $"{LoadoutEnigmaPlugin.PluginName}Icon";

            return icon;
        }
    }
}
