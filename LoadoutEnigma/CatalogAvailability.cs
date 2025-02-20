using RoR2;
using RoR2.Skills;

namespace LoadoutEnigma
{
    public static class CatalogAvailability
    {
        public static ResourceAvailability SkillCatalog = new ResourceAvailability();

        [SystemInitializer(typeof(SkillCatalog))]
        static void InitSkillCatalog()
        {
            SkillCatalog.MakeAvailable();
        }
    }
}
