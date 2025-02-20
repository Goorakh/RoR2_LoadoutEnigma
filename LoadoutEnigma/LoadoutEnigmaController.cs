using EntityStates;
using EntityStates.Toolbot;
using HG;
using LoadoutEnigma.Content;
using LoadoutEnigma.Utilities.Extensions;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoadoutEnigma
{
    public class LoadoutEnigmaController : MonoBehaviour
    {
        public GenericSkill EnigmaSkillSlot;

        CharacterBody _body;

        SkillOverrideManager[] _skillOverrides = [];

        GenericSkill _currentSingleEnabledSkillSlot;

        List<Behaviour> _requiredBodySkillComponents = [];

        bool _skillOverridesDirty;

        EnigmaSkillDef currentEnigmaSkill => EnigmaSkillSlot ? EnigmaSkillSlot.skillDef as EnigmaSkillDef : null;

        void Awake()
        {
            _body = GetComponent<CharacterBody>();

            int skillSlotCount = _body.skillLocator.skillSlotCount;

            if (!EnigmaSkillSlot)
            {
                for (int i = 0; i < skillSlotCount; i++)
                {
                    GenericSkill skillSlot = _body.skillLocator.GetSkillAtIndex(i);
                    if (skillSlot && skillSlot.skillFamily && LoadoutEnigmaCatalog.IsEnigmaSkillFamily(skillSlot.skillFamily))
                    {
                        EnigmaSkillSlot = skillSlot;
                        break;
                    }
                }
            }

            List<GenericSkill> passiveSkillSlots = new List<GenericSkill>(skillSlotCount);
            for (int i = 0; i < skillSlotCount; i++)
            {
                GenericSkill skillSlot = _body.skillLocator.GetSkillAtIndex(i);
                if (skillSlot && skillSlot.skillFamily)
                {
                    string familyName = SkillCatalog.GetSkillFamilyName(skillSlot.skillFamily.catalogIndex);
                    if (familyName.Contains("Passive", StringComparison.OrdinalIgnoreCase))
                    {
                        passiveSkillSlots.Add(skillSlot);
                    }
                }
            }

            List<GenericSkill> overridableSkillSlots = new List<GenericSkill>(skillSlotCount);
            for (int i = 0; i < skillSlotCount; i++)
            {
                GenericSkill skillSlot = _body.skillLocator.GetSkillAtIndex(i);
                if (skillSlot != EnigmaSkillSlot && !passiveSkillSlots.Contains(skillSlot) && skillSlot.activationState.stateType != typeof(Idle))
                {
                    overridableSkillSlots.Add(skillSlot);
                }
            }

            _skillOverrides = new SkillOverrideManager[overridableSkillSlots.Count];
            for (int i = 0; i < _skillOverrides.Length; i++)
            {
                _skillOverrides[i] = new SkillOverrideManager(overridableSkillSlots[i]);
            }

            ReadOnlyArray<Type> allRequiredComponentTypes = SurvivorSkillComponentResolver.GetAllRequiredBodyComponentTypes();
            _requiredBodySkillComponents.EnsureCapacity(allRequiredComponentTypes.Length);
            for (int i = 0; i < allRequiredComponentTypes.Length; i++)
            {
                Component requiredComponent = GetComponent(allRequiredComponentTypes[i]);
                if (requiredComponent && requiredComponent is Behaviour requiredBehaviorComponent && !requiredBehaviorComponent.enabled)
                {
                    // Trigger component init
                    requiredBehaviorComponent.enabled = true;
                    requiredBehaviorComponent.enabled = false;

                    _requiredBodySkillComponents.Add(requiredBehaviorComponent);
                }
            }
        }

        void OnEnable()
        {
            _body.onSkillActivatedAuthority += onSkillActivatedAuthority;

            foreach (SkillOverrideManager overrideManager in _skillOverrides)
            {
                overrideManager.OnSkillOverrideChanged += onSkillOverrideChanged;
            }

            SetRandomSkills();
        }

        void OnDisable()
        {
            _body.onSkillActivatedAuthority -= onSkillActivatedAuthority;

            foreach (SkillOverrideManager overrideManager in _skillOverrides)
            {
                overrideManager.OnSkillOverrideChanged -= onSkillOverrideChanged;

                overrideManager.LastActivationStateMachine = null;
                overrideManager.IsSwitchPending = false;
                overrideManager.CurrentOverrideSkill = null;
                overrideManager.IsDisabled = false;
            }
        }

        void onSkillOverrideChanged(SkillOverrideManager overrideManager)
        {
            _skillOverridesDirty = true;
        }

        void onSkillActivatedAuthority(GenericSkill skill)
        {
            GenericSkill activatedSkill = skill;
            EntityStateMachine activatedStateMachine = activatedSkill.stateMachine;
            if (activatedSkill.currentSkillOverride != -1)
            {
                GenericSkill overrideSkillSource = null;

                GenericSkill.SkillOverride activeSkillOverride = ArrayUtils.GetSafe(activatedSkill.skillOverrides, activatedSkill.currentSkillOverride);
                if (activeSkillOverride != null)
                {
                    switch (activeSkillOverride.source)
                    {
                        case ToolbotDualWieldBase toolbotDualWield:
                            if (activatedSkill == _body.skillLocator.secondary)
                                overrideSkillSource = toolbotDualWield.primary2Slot;
                            
                            break;
                        case SkillOverrideManager:
                            overrideSkillSource = activatedSkill;
                            break;
                    }
                }

                if (!overrideSkillSource)
                {
                    Log.Debug($"Not counting use of overridden skill {activatedSkill.skillDef} (index={_body.skillLocator.GetSkillSlotIndex(activatedSkill)}) for {_body}");
                    return;
                }

                Log.Debug($"Determined override skill source {overrideSkillSource.skillDef} (index={_body.skillLocator.GetSkillSlotIndex(overrideSkillSource)}) for {_body}");

                activatedSkill = overrideSkillSource;
            }

            SkillOverrideManager overrideManager = findSlotOverrideManager(activatedSkill);
            if (overrideManager == null)
            {
                Log.Warning($"Failed to find override manager for activated skill {SkillCatalog.GetSkillName(activatedSkill.skillDef.skillIndex)} on {_body}");
                return;
            }

            overrideManager.LastActivationStateMachine = activatedStateMachine;
            overrideManager.IsSwitchPending = true;
        }

        void FixedUpdate()
        {
            if (_body.hasEffectiveAuthority)
            {
                foreach (SkillOverrideManager overrideManager in _skillOverrides)
                {
                    if (!overrideManager.CurrentOverrideSkill)
                        continue;

                    bool isUnusableSkill = false;

                    if (overrideManager.CurrentOverrideSkill is CaptainOrbitalSkillDef captainOrbitalSkillDef &&
                        !captainOrbitalSkillDef.isAvailable)
                    {
                        isUnusableSkill = true;
                    }

                    if (overrideManager.CurrentOverrideSkill is CaptainSupplyDropSkillDef captainSupplyDropSkillDef)
                    {
                        bool anySupplyDropAvailable = false;

                        foreach (string supplyDropSlotName in captainSupplyDropSkillDef.supplyDropSkillSlotNames)
                        {
                            GenericSkill supplyDropSlot = _body.skillLocator.FindSkill(supplyDropSlotName);
                            if (supplyDropSlot && supplyDropSlot.IsReady())
                            {
                                anySupplyDropAvailable = true;
                            }
                        }

                        if (!anySupplyDropAvailable)
                        {
                            isUnusableSkill = true;
                        }
                    }

                    if (isUnusableSkill)
                    {
                        Log.Debug($"Skipping unusable skill {SkillCatalog.GetSkillName(overrideManager.CurrentOverrideSkill.skillIndex)}");

                        overrideManager.IsSwitchPending = true;
                    }
                }

                handlePendingSkillActivations();

                if (_skillOverridesDirty)
                {
                    _skillOverridesDirty = false;
                    refreshRequiredSkillComponents();
                }
            }
        }

        public void SetRandomSkills()
        {
            foreach (SkillOverrideManager overrideManager in _skillOverrides)
            {
                overrideManager.IsSwitchPending = true;
            }
        }

        void refreshRequiredSkillComponents()
        {
            HashSet<Type> requiredComponentTypes = [];
            foreach (SkillOverrideManager overrideManager in _skillOverrides)
            {
                if (overrideManager.CurrentOverrideSkill)
                {
                    Type[] requiredComponentsForSkill = SurvivorSkillComponentResolver.GetRequiredBodyComponentTypes(overrideManager.CurrentOverrideSkill);
                    requiredComponentTypes.UnionWith(requiredComponentsForSkill);
                }
            }

            foreach (Behaviour bodyComponent in _requiredBodySkillComponents)
            {
                if (bodyComponent)
                {
                    bodyComponent.enabled = requiredComponentTypes.Contains(bodyComponent.GetType());
                }
            }
        }

        void handlePendingSkillActivations()
        {
            bool switchedAnySkill = false;

            foreach (SkillOverrideManager overrideManager in _skillOverrides)
            {
                if (overrideManager.ShouldSwitchSkill)
                {
                    overrideManager.IsSwitchPending = false;

                    SkillDef nextSkill = pickNextSkill(overrideManager);

                    overrideManager.CurrentOverrideSkill = nextSkill;

                    switchedAnySkill = true;
                }
            }

            if (switchedAnySkill)
            {
                updateSingleEnabledSkillSlot();
            }
        }

        SkillDef pickNextSkill(SkillOverrideManager overrideManager)
        {
            if (overrideManager == null || !overrideManager.SkillSlot || !overrideManager.SkillSlot.skillFamily)
                return null;

            List<SkillDef> availableSkills = new List<SkillDef>(overrideManager.SkillSlot.skillFamily.variants.Length);

            NetworkUser bodyNetworkUser = Util.LookUpBodyNetworkUser(_body);

            LocalUser bodyLocalUser = bodyNetworkUser ? bodyNetworkUser.localUser : null;

            foreach (SkillFamily.Variant variant in overrideManager.SkillSlot.skillFamily.variants)
            {
                if (variant.skillDef == overrideManager.CurrentOverrideSkill)
                    continue;

                bool isBlacklisted = LoadoutEnigmaPlugin.SkillBlacklist.Contains(variant.skillDef.skillIndex);
                if (isBlacklisted)
                    continue;

                bool isVariantUnlocked = true;
                if (variant.unlockableDef)
                {
                    if (bodyLocalUser != null && bodyLocalUser.userProfile != null)
                    {
                        isVariantUnlocked = bodyLocalUser.userProfile.HasUnlockable(variant.unlockableDef);
                    }
                    else if (bodyNetworkUser)
                    {
                        isVariantUnlocked = bodyNetworkUser.unlockables.Contains(variant.unlockableDef);
                    }
                    else
                    {
                        Log.Error($"Failed to determine unlockable status for skill variant {variant.skillDef}. Assuming not unlocked.");
                        isVariantUnlocked = false;
                    }
                }

                if (!isVariantUnlocked)
                    continue;

                Type[] requiredComponentTypes = SurvivorSkillComponentResolver.GetRequiredBodyComponentTypes(variant.skillDef);

                bool missingComponents = false;
                foreach (Type requiredComponentType in requiredComponentTypes)
                {
                    if (!GetComponent(requiredComponentType))
                    {
                        missingComponents = true;
                        break;
                    }
                }

                if (missingComponents)
                    continue;

                availableSkills.Add(variant.skillDef);
            }

            if (availableSkills.Count == 0)
            {
                SkillDef fallbackSkill = overrideManager.SkillSlot.baseSkill;
                if (overrideManager.CurrentOverrideSkill)
                {
                    fallbackSkill = overrideManager.CurrentOverrideSkill;
                }

                availableSkills.Add(fallbackSkill);
            }

            return availableSkills[UnityEngine.Random.Range(0, availableSkills.Count)];
        }

        void updateSingleEnabledSkillSlot()
        {
            bool singleSkillSlot = false;
            if (currentEnigmaSkill)
            {
                singleSkillSlot = currentEnigmaSkill.SingleSkillMode;
            }

            GenericSkill singleEnabledSkillSlot = singleSkillSlot ? pickNextSingleEnabledSkillSlot() : null;

            foreach (SkillOverrideManager overrideManager in _skillOverrides)
            {
                bool isActivatableSkill = _body.skillLocator.FindSkillSlot(overrideManager.SkillSlot) != SkillSlot.None;
                bool isSkillEnabled = !singleEnabledSkillSlot || overrideManager.SkillSlot == singleEnabledSkillSlot;

                overrideManager.IsDisabled = isActivatableSkill && !isSkillEnabled;
            }

            _currentSingleEnabledSkillSlot = singleEnabledSkillSlot;
        }

        GenericSkill pickNextSingleEnabledSkillSlot()
        {
            List<GenericSkill> availableSlots = new List<GenericSkill>(_body.skillLocator.skillSlotCount);

            for (SkillSlot slot = 0; slot <= SkillSlot.Special; slot++)
            {
                GenericSkill skillSlot = _body.skillLocator.GetSkill(slot);
                if (skillSlot)
                {
                    availableSlots.Add(skillSlot);
                }
            }

            if (availableSlots.Count == 0)
            {
                availableSlots.Add(_currentSingleEnabledSkillSlot);
            }

            return availableSlots[UnityEngine.Random.Range(0, availableSlots.Count)];
        }

        SkillOverrideManager findSlotOverrideManager(GenericSkill skillSlot)
        {
            foreach (SkillOverrideManager overrideManager in _skillOverrides)
            {
                if (overrideManager.SkillSlot == skillSlot)
                {
                    return overrideManager;
                }
            }

            return null;
        }

        class SkillOverrideManager
        {
            public readonly GenericSkill SkillSlot;

            public EntityStateMachine LastActivationStateMachine;

            public bool IsSwitchPending;

            SkillDef _currentOverrideSkill;
            public SkillDef CurrentOverrideSkill
            {
                get
                {
                    return _currentOverrideSkill;
                }
                set
                {
                    if (value == _currentOverrideSkill)
                        return;

                    _currentOverrideSkill = value;

                    resolveSkillOverride();
                }
            }

            bool _isDisabled;
            public bool IsDisabled
            {
                get
                {
                    return _isDisabled;
                }
                set
                {
                    if (_isDisabled == value)
                        return;

                    _isDisabled = value;

                    resolveSkillOverride();
                }
            }

            public SkillDef ResolvedOverrideSkill { get; private set; }

            public bool ShouldSwitchSkill => IsSwitchPending && (!LastActivationStateMachine || LastActivationStateMachine.IsInMainState());

            public event Action<SkillOverrideManager> OnSkillOverrideChanged;

            public SkillOverrideManager(GenericSkill skillSlot)
            {
                SkillSlot = skillSlot;
            }

            void resolveSkillOverride()
            {
                SkillDef resolvedOverrideSkill = _currentOverrideSkill;

                if (_isDisabled)
                {
                    resolvedOverrideSkill = LoadoutEnigmaContent.SkillDefs.DisabledSkill;
                }

                if (ResolvedOverrideSkill == resolvedOverrideSkill)
                    return;

                bool wasDisabled = ResolvedOverrideSkill is DisabledSkillDef;

                float oldStockFraction = 1f;
                if (SkillSlot.maxStock > 0)
                {
                    oldStockFraction = SkillSlot.stock / (float)SkillSlot.maxStock;
                }

                if (ResolvedOverrideSkill)
                {
                    SkillSlot.UnsetSkillOverride(this, ResolvedOverrideSkill, GenericSkill.SkillOverridePriority.Replacement);
                }

                ResolvedOverrideSkill = resolvedOverrideSkill;

                if (ResolvedOverrideSkill)
                {
                    SkillSlot.SetSkillOverride(this, ResolvedOverrideSkill, GenericSkill.SkillOverridePriority.Replacement);
                }

                int stock = Mathf.Min(SkillSlot.stock, Mathf.CeilToInt(oldStockFraction * SkillSlot.maxStock));

                if (wasDisabled)
                {
                    stock = Mathf.Max(stock, Mathf.Min(SkillSlot.maxStock, 1));
                }

                SkillSlot.stock = stock;

                OnSkillOverrideChanged?.Invoke(this);
            }
        }
    }
}
