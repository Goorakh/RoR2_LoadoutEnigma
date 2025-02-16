using RoR2;
using RoR2.Skills;

namespace LoadoutEnigma
{
    static class GenericSkillHooks
    {
        public delegate void OnSkillChangedGlobalDelegate(GenericSkill skill, SkillDef previousSkill, SkillDef newSkill);
        public static event OnSkillChangedGlobalDelegate OnSkillChangedGlobal;

        [SystemInitializer]
        static void Init()
        {
            On.RoR2.GenericSkill.SetSkillInternal += GenericSkill_SetSkillInternal;
        }

        static void GenericSkill_SetSkillInternal(On.RoR2.GenericSkill.orig_SetSkillInternal orig, GenericSkill self, SkillDef newSkillDef)
        {
            SkillDef prevSkillDef = self.skillDef;

            orig(self, newSkillDef);

            if (prevSkillDef != self.skillDef)
            {
                OnSkillChangedGlobal?.Invoke(self, prevSkillDef, self.skillDef);
            }
        }
    }
}
