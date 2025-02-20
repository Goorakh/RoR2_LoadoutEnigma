using BepInEx;
using BepInEx.Configuration;
using LoadoutEnigma.Content;
using LoadoutEnigma.ModCompatibility;
using R2API.Utils;
using RoR2;
using System.Diagnostics;

namespace LoadoutEnigma
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(R2API.R2API.PluginGUID)]
    [BepInDependency(R2API.SkillsAPI.PluginGUID)]
    [BepInDependency(RiskOfOptions.PluginInfo.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(SkillSwapCompat.PLUGIN_GUID, BepInDependency.DependencyFlags.SoftDependency)]
    public class LoadoutEnigmaPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Gorakh";
        public const string PluginName = "LoadoutEnigma";
        public const string PluginVersion = "1.0.0";

        static LoadoutEnigmaPlugin _instance;
        internal static LoadoutEnigmaPlugin Instance => _instance;

        public static ConfigEntry<string> SkillBlacklistConfig { get; internal set; }
        public static ConfigSkillIndexList SkillBlacklist { get; private set; }

        void Awake()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            SingletonHelper.Assign(ref _instance, this);

            Log.Init(Logger);

            SkillBlacklistConfig = Config.Bind("General", "Skill Blacklist", "ToolbotBodySwap,ToolbotDualWield", new ConfigDescription("A comma-separated list of skills to exclude from Loadout Enigma"));
            SkillBlacklist = new ConfigSkillIndexList(SkillBlacklistConfig);

            LoadoutEnigmaContent content = new LoadoutEnigmaContent();
            content.Register();

            LanguageFolderHandler.Register(System.IO.Path.GetDirectoryName(Info.Location));

            if (RiskOfOptionsCompat.Enabled)
            {
                RiskOfOptionsCompat.AddOptions();
            }

            if (SkillSwapCompat.Enabled)
            {
                SkillSwapCompat.Init();
            }

            SurvivorSkillComponentResolver.PreCatalogInit();

            SystemInitializerInjector.InjectDependency(typeof(Loadout), typeof(LoadoutEnigmaCatalog));
            SystemInitializerInjector.InjectDependency(typeof(Loadout.BodyLoadoutManager), typeof(LoadoutEnigmaCatalog));

            stopwatch.Stop();
            Log.Message_NoCallerPrefix($"Initialized in {stopwatch.Elapsed.TotalMilliseconds:F0}ms");
        }

        void OnDestroy()
        {
            SingletonHelper.Unassign(ref _instance, this);
        }
    }
}
