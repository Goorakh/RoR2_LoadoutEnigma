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
using UnityEngine.AddressableAssets;

namespace LoadoutEnigma
{
    public class LoadoutEnigmaController : MonoBehaviour
    {
        public GenericSkill EnigmaSkillSlot;

        CharacterBody _body;

        SkillOverrideManager[] _skillOverrides = [];

        GenericSkill _currentSingleEnabledSkillSlot;

        EnigmaSkillDef currentEnigmaSkill => EnigmaSkillSlot ? EnigmaSkillSlot.skillDef as EnigmaSkillDef : null;

        void Awake()
        {
            _body = GetComponent<CharacterBody>();

            int skillSlotCount = _body.skillLocator.skillSlotCount;

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
        }

        void OnEnable()
        {
            _body.onSkillActivatedAuthority += onSkillActivatedAuthority;

            // Apply random skills on start
            foreach (SkillOverrideManager overrideManager in _skillOverrides)
            {
                overrideManager.IsSwitchPending = true;
            }

            updateSingleEnabledSkillSlot();
        }

        void OnDisable()
        {
            _body.onSkillActivatedAuthority -= onSkillActivatedAuthority;

            for (int i = 0; i < _skillOverrides.Length; i++)
            {
                _skillOverrides[i].LastActivationStateMachine = null;
                _skillOverrides[i].IsSwitchPending = false;
                _skillOverrides[i].CurrentOverrideSkill = null;
            }
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

            int skillSlotIndex = _body.skillLocator.GetSkillSlotIndex(activatedSkill);
            if (!ArrayUtils.IsInBounds(_skillOverrides, skillSlotIndex))
            {
                Log.Warning($"Activated skill {SkillCatalog.GetSkillName(activatedSkill.skillDef.skillIndex)} on {_body} is outside the bounds of skills array");
                return;
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
                handlePendingSkillActivations();
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

                    SkillDef nextSkill = pickNextSkill(overrideManager.SkillSlot);

                    overrideManager.CurrentOverrideSkill = nextSkill;

                    switchedAnySkill = true;
                }
            }

            if (switchedAnySkill)
            {
                updateSingleEnabledSkillSlot();
            }
        }

        SkillDef pickNextSkill(GenericSkill skillSlot)
        {
            if (!skillSlot || !skillSlot.skillFamily)
                return null;

            List<SkillDef> availableSkills = new List<SkillDef>(skillSlot.skillFamily.variants.Length);

            NetworkUser bodyNetworkUser = Util.LookUpBodyNetworkUser(_body);

            LocalUser bodyLocalUser = bodyNetworkUser ? bodyNetworkUser.localUser : null;

            foreach (SkillFamily.Variant variant in skillSlot.skillFamily.variants)
            {
                // Manually exclude problematic skills for now
                // TODO: Don't do this
                switch (SkillCatalog.GetSkillName(variant.skillDef.skillIndex))
                {
                    case "ChefDice":
                    case "ChefSear":
                    case "ChefRolyPoly":
                    case "YesChef":
                        if (!_body.TryGetComponent(out ChefController chefController) || !chefController.enabled)
                            continue;

                        break;

                    case "HuntressBodyFireSeekingArrow":
                    case "FireFlurrySeekingArrow":
                    case "HuntressBodyGlaive":
                        if (!_body.TryGetComponent(out HuntressTracker huntressTracker) || !huntressTracker.enabled)
                            continue;

                        break;

                    case "SeekerBodySoulSpiral":
                    case "SeekerBodySojourn":
                    case "SeekerBodyMeditate2":
                        if (!_body.TryGetComponent(out SeekerController seekerController) || !seekerController.enabled)
                            continue;

                        break;

                    case "CrushCorruption":
                        if (!_body.TryGetComponent(out VoidSurvivorController voidSurvivorController) || !voidSurvivorController.enabled)
                            continue;

                        break;

                    case "FalseSonBodyClub":
                        if (!_body.TryGetComponent(out FalseSonController falseSonController) || !falseSonController.enabled)
                            continue;
                    
                        break;
                }

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

                bool canSelectVariant = isVariantUnlocked && variant.skillDef != skillSlot.skillDef;

                if (canSelectVariant)
                {
                    availableSkills.Add(variant.skillDef);
                }
            }

            if (availableSkills.Count == 0)
            {
                availableSkills.Add(skillSlot.baseSkill);
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
                bool isSkillEnabled = !singleEnabledSkillSlot || overrideManager.SkillSlot == singleEnabledSkillSlot;

                overrideManager.IsDisabled = _body.skillLocator.FindSkillSlot(overrideManager.SkillSlot) != SkillSlot.None && !isSkillEnabled;
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

            SkillDef _resolvedOverrideSkill;

            public bool ShouldSwitchSkill => IsSwitchPending && (!LastActivationStateMachine || LastActivationStateMachine.IsInMainState());

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

                if (_resolvedOverrideSkill == resolvedOverrideSkill)
                    return;

                bool wasDisabled = _resolvedOverrideSkill is DisabledSkillDef;

                float oldStockFraction = 1f;
                if (SkillSlot.maxStock > 0)
                {
                    oldStockFraction = SkillSlot.stock / (float)SkillSlot.maxStock;
                }

                if (_resolvedOverrideSkill)
                {
                    SkillSlot.UnsetSkillOverride(this, _resolvedOverrideSkill, GenericSkill.SkillOverridePriority.Replacement);
                }

                _resolvedOverrideSkill = resolvedOverrideSkill;

                if (_resolvedOverrideSkill)
                {
                    SkillSlot.SetSkillOverride(this, _resolvedOverrideSkill, GenericSkill.SkillOverridePriority.Replacement);
                }

                int stock = Mathf.Min(SkillSlot.stock, Mathf.CeilToInt(oldStockFraction * SkillSlot.maxStock));

                if (wasDisabled)
                {
                    stock = Mathf.Max(stock, Mathf.Min(SkillSlot.maxStock, 1));
                }

                SkillSlot.stock = stock;
            }
        }
    }
}
