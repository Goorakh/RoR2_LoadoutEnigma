using RoR2;
using RoR2.Skills;
using System;

namespace LoadoutEnigma.Content
{
    public class EnigmaSkillDef : SkillDef
    {
        public bool SingleSkillMode;

        public override BaseSkillInstanceData OnAssigned(GenericSkill skillSlot)
        {
            return new InstanceData(skillSlot);
        }

        public override void OnUnassigned(GenericSkill skillSlot)
        {
            if (skillSlot.skillInstanceData is InstanceData instanceData)
            {
                instanceData.Dispose();
            }

            base.OnUnassigned(skillSlot);
        }

        class InstanceData : BaseSkillInstanceData, IDisposable
        {
            public readonly LoadoutEnigmaController EnigmaController;

            public InstanceData(GenericSkill skillSlot)
            {
                EnigmaController = skillSlot.characterBody.GetComponent<LoadoutEnigmaController>();

                if (EnigmaController)
                {
                    EnigmaController.enabled = true;
                }
            }

            public void Dispose()
            {
                if (EnigmaController)
                {
                    EnigmaController.enabled = false;
                }
            }
        }
    }
}
