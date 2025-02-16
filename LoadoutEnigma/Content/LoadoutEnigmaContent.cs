using EntityStates;
using LoadoutEnigma.Utilities.Extensions;
using RoR2.ContentManagement;
using RoR2.Skills;
using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LoadoutEnigma.Content
{
    internal class LoadoutEnigmaContent : IContentPackProvider
    {
        readonly ContentPack _contentPack = new ContentPack();

        public string identifier => LoadoutEnigmaPlugin.PluginGUID;

        internal LoadoutEnigmaContent()
        {
        }

        internal void Register()
        {
            ContentManager.collectContentPackProviders += addContentPackProvider =>
            {
                addContentPackProvider(this);
            };
        }

        public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
        {
            _contentPack.identifier = identifier;

            DisabledSkillDef disabledSkillDef = ScriptableObject.CreateInstance<DisabledSkillDef>();
            ((ScriptableObject)disabledSkillDef).name = nameof(SkillDefs.DisabledSkill);
            disabledSkillDef.skillName = nameof(SkillDefs.DisabledSkill);
            disabledSkillDef.skillNameToken = "CAPTAIN_SKILL_USED_UP_NAME";
            disabledSkillDef.activationStateMachineName = "there's no way somebody has a state machine with this name";
            disabledSkillDef.activationState = new SerializableEntityStateType(typeof(Idle));
            disabledSkillDef.fullRestockOnAssign = true;
            disabledSkillDef.dontAllowPastMaxStocks = true;
            disabledSkillDef.hideStockCount = true;
            Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC2/Common/DisabledSkill.asset").CallOnSuccess(disabledSkill => disabledSkillDef.icon = disabledSkill.icon);

            SkillDef enigmaDisabledSkillDef = ScriptableObject.CreateInstance<SkillDef>();
            ((ScriptableObject)enigmaDisabledSkillDef).name = nameof(SkillDefs.EnigmaDisabled);
            enigmaDisabledSkillDef.skillName = nameof(SkillDefs.EnigmaDisabled);
            enigmaDisabledSkillDef.skillNameToken = "SKILL_ENIGMA_DISABLED_NAME";
            enigmaDisabledSkillDef.skillDescriptionToken = "SKILL_ENIGMA_DISABLED_DESC";
            enigmaDisabledSkillDef.keywordTokens = [];
            enigmaDisabledSkillDef.activationStateMachineName = "there's no way somebody has a state machine with this name";
            enigmaDisabledSkillDef.activationState = new SerializableEntityStateType(typeof(Idle));
            Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Enigma/texArtifactRandomEquipmentDisabled.png").CallOnSuccess(icon => enigmaDisabledSkillDef.icon = icon);

            EnigmaSkillDef enigmaEnabledSkillDef = ScriptableObject.CreateInstance<EnigmaSkillDef>();
            ((ScriptableObject)enigmaEnabledSkillDef).name = nameof(SkillDefs.EnigmaEnabled);
            enigmaEnabledSkillDef.skillName = nameof(SkillDefs.EnigmaEnabled);
            enigmaEnabledSkillDef.skillNameToken = "SKILL_ENIGMA_ENABLED_NAME";
            enigmaEnabledSkillDef.skillDescriptionToken = "SKILL_ENIGMA_ENABLED_DESC";
            enigmaEnabledSkillDef.keywordTokens = [];
            enigmaEnabledSkillDef.activationStateMachineName = "there's no way somebody has a state machine with this name";
            enigmaEnabledSkillDef.activationState = new SerializableEntityStateType(typeof(Idle));
            enigmaEnabledSkillDef.SingleSkillMode = false;
            Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Enigma/texArtifactRandomEquipmentEnabled.png").CallOnSuccess(icon => enigmaEnabledSkillDef.icon = icon);

            EnigmaSkillDef enigmaEnabledSingleModeSkillDef = ScriptableObject.CreateInstance<EnigmaSkillDef>();
            ((ScriptableObject)enigmaEnabledSingleModeSkillDef).name = nameof(SkillDefs.EnigmaEnabledSingleMode);
            enigmaEnabledSingleModeSkillDef.skillName = nameof(SkillDefs.EnigmaEnabledSingleMode);
            enigmaEnabledSingleModeSkillDef.skillNameToken = "SKILL_ENIGMA_SINGLE_MODE_ENABLED_NAME";
            enigmaEnabledSingleModeSkillDef.skillDescriptionToken = "SKILL_ENIGMA_SINGLE_MODE_ENABLED_DESC";
            enigmaEnabledSingleModeSkillDef.keywordTokens = [];
            enigmaEnabledSingleModeSkillDef.activationStateMachineName = "there's no way somebody has a state machine with this name";
            enigmaEnabledSingleModeSkillDef.activationState = new SerializableEntityStateType(typeof(Idle));
            enigmaEnabledSingleModeSkillDef.SingleSkillMode = true;
            Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Enigma/texArtifactRandomEquipmentEnabled.png").CallOnSuccess(icon => enigmaEnabledSingleModeSkillDef.icon = icon);

            _contentPack.skillDefs.Add([disabledSkillDef, enigmaDisabledSkillDef, enigmaEnabledSkillDef, enigmaEnabledSingleModeSkillDef]);

            ContentLoadHelper.PopulateTypeFields(typeof(SkillDefs), _contentPack.skillDefs);

            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
        {
            ContentPack.Copy(_contentPack, args.output);
            args.ReportProgress(1f);
            yield break;
        }

        public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
        {
            args.ReportProgress(1f);
            yield break;
        }

        public static class SkillDefs
        {
            public static SkillDef DisabledSkill;

            public static SkillDef EnigmaDisabled;

            public static SkillDef EnigmaEnabled;

            public static SkillDef EnigmaEnabledSingleMode;
        }
    }
}
