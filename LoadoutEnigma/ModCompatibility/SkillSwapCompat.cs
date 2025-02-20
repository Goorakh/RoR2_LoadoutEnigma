using BepInEx.Bootstrap;
using RoR2.UI;
using RoR2BepInExPack.Utilities;
using System.Runtime.CompilerServices;

namespace LoadoutEnigma.ModCompatibility
{
    static class SkillSwapCompat
    {
        public const string PLUGIN_GUID = "pseudopulse.SkillSwap";

        public static bool Enabled => Chainloader.PluginInfos.ContainsKey(PLUGIN_GUID);

        class DummyClass { }
        static readonly FixedConditionalWeakTable<LoadoutPanelController.Row, DummyClass> _enigmaRows = new FixedConditionalWeakTable<LoadoutPanelController.Row, DummyClass>();

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void Init()
        {
            // Terrible hooks required here because SkillSwap doesn't call orig in its hooks, ignoring all skill sorting

            On.RoR2.UI.LoadoutPanelController.Row.FromSkillSlot += Row_FromSkillSlot;
            On.RoR2.UI.LoadoutPanelController.Rebuild += LoadoutPanelController_Rebuild;
        }

        static object Row_FromSkillSlot(On.RoR2.UI.LoadoutPanelController.Row.orig_FromSkillSlot orig, LoadoutPanelController owner, RoR2.BodyIndex bodyIndex, int skillSlotIndex, RoR2.GenericSkill skillSlot)
        {
            object result = orig(owner, bodyIndex, skillSlotIndex, skillSlot);

            if (result is LoadoutPanelController.Row row)
            {
                if (LoadoutEnigmaCatalog.IsEnigmaSkillFamily(skillSlot.skillFamily))
                {
                    _enigmaRows.Add(row, new DummyClass());
                }
            }

            return result;
        }

        static void LoadoutPanelController_Rebuild(On.RoR2.UI.LoadoutPanelController.orig_Rebuild orig, LoadoutPanelController self)
        {
            orig(self);

            for (int i = 0; i < self.rows.Count; i++)
            {
                LoadoutPanelController.Row row = self.rows[i];

                if (_enigmaRows.TryGetValue(row, out _))
                {
                    self.rows.RemoveAt(i);
                    self.rows.Insert(0, row);

                    row.rowPanelTransform.SetAsFirstSibling();
                }
            }
        }
    }
}
