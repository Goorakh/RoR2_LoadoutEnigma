using RoR2;
using RoR2.Skills;

namespace LoadoutEnigma.Content
{
    public class DisabledSkillDef : SkillDef
    {
        public override bool CanExecute(GenericSkill skillSlot)
        {
            return false;
        }

        public override bool IsReady(GenericSkill skillSlot)
        {
            return false;
        }
    }
}
