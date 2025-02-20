using LoadoutEnigma.Content;
using LoadoutEnigma.ModCompatibility;
using LoadoutEnigma.Utilities;
using R2API;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoadoutEnigma
{
    public static class LoadoutEnigmaCatalog
    {
        static SkillFamily[] _enigmaSkillFamilies;

        [SystemInitializer(typeof(SkillCatalog), typeof(BodyCatalog), typeof(SurvivorCatalog), typeof(SurvivorSkillComponentResolver))]
        static void Init()
        {
            List<SkillFamily> addedSurvivorSkillFamilies = new List<SkillFamily>(SurvivorCatalog.survivorCount);

            HG.ReadOnlyArray<Type> bodySpecificComponentTypes = SurvivorSkillComponentResolver.GetAllRequiredBodyComponentTypes();

            foreach (SurvivorDef survivor in SurvivorCatalog.allSurvivorDefs)
            {
                if (!survivor || survivor.hidden || !survivor.bodyPrefab)
                    continue;

                BodyIndex survivorBodyIndex = BodyCatalog.FindBodyIndex(survivor.bodyPrefab);
                if (survivorBodyIndex == BodyIndex.None)
                    continue;

                SkillFamily survivorEnigmaSkillFamily = ScriptableObject.CreateInstance<SkillFamily>();
                ((ScriptableObject)survivorEnigmaSkillFamily).name = $"{BodyCatalog.GetBodyName(survivorBodyIndex)}EnigmaFamily";
                survivorEnigmaSkillFamily.defaultVariantIndex = 0;
                survivorEnigmaSkillFamily.variants = [
                    new SkillFamily.Variant
                    {
                        skillDef = LoadoutEnigmaContent.SkillDefs.EnigmaDisabled
                    },
                    new SkillFamily.Variant
                    {
                        skillDef = LoadoutEnigmaContent.SkillDefs.EnigmaEnabled
                    },
                    new SkillFamily.Variant
                    {
                        skillDef = LoadoutEnigmaContent.SkillDefs.EnigmaEnabledSingleMode
                    }
                ];

                ref GenericSkill[] cachedBodySkillSlots = ref BodyCatalog.skillSlots[(int)survivorBodyIndex];

                GenericSkill enigmaSkillSlot = survivor.bodyPrefab.AddComponent<GenericSkill>();
                enigmaSkillSlot.hideInCharacterSelect = true;

                // Appear at the top
                enigmaSkillSlot.SetOrderPriority(-100);
                enigmaSkillSlot.SetLoadoutTitleTokenOverride("LOADOUT_SKILL_ENIGMA");

                enigmaSkillSlot._skillFamily = survivorEnigmaSkillFamily;
                enigmaSkillSlot.skillName = "Enigma";

                cachedBodySkillSlots = [..cachedBodySkillSlots, enigmaSkillSlot];

                addedSurvivorSkillFamilies.Add(survivorEnigmaSkillFamily);

                LoadoutEnigmaController loadoutEnigmaController = survivor.bodyPrefab.AddComponent<LoadoutEnigmaController>();
                loadoutEnigmaController.EnigmaSkillSlot = enigmaSkillSlot;
                loadoutEnigmaController.enabled = false;

                if (SkillSwapCompat.Enabled)
                {
                    if (!survivor.bodyPrefab.GetComponent<HuntressTracker>())
                    {
                        HuntressTracker huntressTracker = survivor.bodyPrefab.AddComponent<HuntressTracker>();
                        huntressTracker.enabled = false;
                        SurvivorSkillComponentResolver.PopulateSurvivorBodyComponent(huntressTracker);
                    }

                    // TODO: Requires resolving component/child references, only add HuntressTracker for now
                    /*
                    for (int i = 0; i < bodySpecificComponentTypes.Length; i++)
                    {
                        Type componentType = bodySpecificComponentTypes[i];
                        if (survivor.bodyPrefab.GetComponent(componentType))
                            continue;

                        Component component = survivor.bodyPrefab.AddComponent(componentType);

                        if (component is Behaviour behaviour)
                        {
                            behaviour.enabled = false;
                        }

                        SurvivorSkillComponentResolver.PopulateSurvivorBodyComponent(component);
                    }
                    */
                }
            }

            if (addedSurvivorSkillFamilies.Count > 0)
            {
                SkillCatalog.SetSkillFamilies([.. SkillCatalog.allSkillFamilies, .. addedSurvivorSkillFamilies]);
            }

            _enigmaSkillFamilies = [.. addedSurvivorSkillFamilies];
            Array.Sort(_enigmaSkillFamilies, SkillFamilyCatalogIndexComparer.Instance);
        }

        public static bool IsEnigmaSkillFamily(SkillFamily skillFamily)
        {
            return Array.BinarySearch(_enigmaSkillFamilies, skillFamily, SkillFamilyCatalogIndexComparer.Instance) >= 0;
        }
    }
}
