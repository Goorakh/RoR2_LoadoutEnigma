using LoadoutEnigma.ModCompatibility;
using LoadoutEnigma.Utilities.Extensions;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace LoadoutEnigma
{
    public static class SurvivorSkillComponentResolver
    {
        static Type[] _allBodyComponentTypes = [];

        static readonly Dictionary<int, Type[]> _requiredBodyComponentsBySkillIndex = [];

        static readonly Dictionary<Type, Dictionary<FieldInfo, object>> _bodyComponentDefaultValues = [];

        public static void PreCatalogInit()
        {
            // SkillSwap removes this component on init, so we can't record it there. Fun times.
            if (SkillSwapCompat.Enabled)
            {
                Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidSurvivor/VoidSurvivorBody.prefab").CallOnSuccess(voidFieldBody =>
                {
                    if (voidFieldBody.TryGetComponent(out VoidSurvivorController voidSurvivorController))
                    {
                        recordSurvivorComponentTemplateValues(voidSurvivorController);
                    }
                });
            }
        }

        [SystemInitializer(typeof(SkillCatalog), typeof(SurvivorCatalog))]
        static void Init()
        {
            HashSet<Type> allRequiredBodyComponentTypes = [];
            _requiredBodyComponentsBySkillIndex.Clear();

            foreach (SkillDef skill in SkillCatalog.allSkillDefs)
            {
                HashSet<Type> requiredBodyComponentTypes = [];

                if (skill is HuntressTrackingSkillDef)
                {
                    requiredBodyComponentTypes.Add(typeof(HuntressTracker));
                }

                if (skill is VoidSurvivorSkillDef)
                {
                    requiredBodyComponentTypes.Add(typeof(VoidSurvivorController));
                }

                switch (SkillCatalog.GetSkillName(skill.skillIndex))
                {
                    case "ChefDice":
                    case "ChefDiceBoosted":
                    case "ChefSear":
                    case "ChefSearBoosted":
                    case "ChefRolyPoly":
                    case "ChefRolyPolyBoosted":
                    case "YesChef":
                        requiredBodyComponentTypes.Add(typeof(ChefController));
                        break;
                    case "SeekerBodySoulSpiral":
                    case "SeekerBodySojourn":
                    case "SeekerBodyMeditate2":
                        requiredBodyComponentTypes.Add(typeof(SeekerController));
                        break;
                    case "FalseSonBodyClub":
                        requiredBodyComponentTypes.Add(typeof(FalseSonController));
                        break;
                }

                if (requiredBodyComponentTypes.Count > 0)
                {
                    _requiredBodyComponentsBySkillIndex[skill.skillIndex] = [.. requiredBodyComponentTypes];
                }

                allRequiredBodyComponentTypes.UnionWith(requiredBodyComponentTypes);
            }

            foreach (Type requiredBodyComponentType in allRequiredBodyComponentTypes)
            {
                if (_bodyComponentDefaultValues.ContainsKey(requiredBodyComponentType))
                    continue;

                Component templateComponent = null;

                foreach (SurvivorDef survivorDef in SurvivorCatalog.allSurvivorDefs)
                {
                    if (!survivorDef || !survivorDef.bodyPrefab)
                        continue;

                    if (survivorDef.bodyPrefab.TryGetComponent(requiredBodyComponentType, out Component component))
                    {
                        templateComponent = component;
                    }
                }

                if (!templateComponent)
                {
                    Log.Warning($"No template component found for {requiredBodyComponentType.FullName}");
                    continue;
                }

                recordSurvivorComponentTemplateValues(templateComponent);
            }

            _allBodyComponentTypes = [.. allRequiredBodyComponentTypes];
        }

        static void recordSurvivorComponentTemplateValues(Component templateComponent)
        {
            if (!templateComponent)
                throw new ArgumentNullException(nameof(templateComponent));

            Type componentType = templateComponent.GetType();

            List<FieldInfo> serializedFields = [];
            foreach (FieldInfo field in componentType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if ((field.IsPublic || field.GetCustomAttribute(typeof(SerializeField)) != null) &&
                    field.GetCustomAttribute(typeof(NonSerializedAttribute)) == null)
                {
                    serializedFields.Add(field);
                }
            }

            if (serializedFields.Count == 0)
            {
                Log.Warning($"No serializable fields found for {componentType.FullName}");
                return;
            }

            Dictionary<FieldInfo, object> defaultValuesDictionary = new Dictionary<FieldInfo, object>(serializedFields.Count);

            foreach (FieldInfo field in serializedFields)
            {
                object templateValue;
                try
                {
                    templateValue = field.GetValue(templateComponent);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Failed to get template value of field {field.DeclaringType.FullName}.{field.Name} from {templateComponent}: {e}");
                    continue;
                }

                defaultValuesDictionary[field] = templateValue;
            }

            _bodyComponentDefaultValues[componentType] = defaultValuesDictionary;
        }

        public static Type[] GetRequiredBodyComponentTypes(SkillDef skill)
        {
            if (!skill)
                return [];

            return GetRequiredBodyComponentTypes(skill.skillIndex);
        }

        public static Type[] GetRequiredBodyComponentTypes(int skillIndex)
        {
            return _requiredBodyComponentsBySkillIndex.TryGetValue(skillIndex, out Type[] requiredComponentTypes) ? requiredComponentTypes : [];
        }

        public static HG.ReadOnlyArray<Type> GetAllRequiredBodyComponentTypes()
        {
            return new HG.ReadOnlyArray<Type>(_allBodyComponentTypes);
        }

        public static void PopulateSurvivorBodyComponent(Component component)
        {
            if (!component)
                return;

            if (!_bodyComponentDefaultValues.TryGetValue(component.GetType(), out Dictionary<FieldInfo, object> fieldValues))
                return;

            foreach (KeyValuePair<FieldInfo, object> kvp in fieldValues)
            {
                kvp.Deconstruct(out FieldInfo field, out object fieldValue);

                try
                {
                    field.SetValue(component, fieldValue);
                }
                catch (Exception e)
                {
                    Log.Error_NoCallerPrefix($"Failed to populate field {field.DeclaringType.FullName}.{field.Name} on {component}: {e}");
                }
            }
        }
    }
}
