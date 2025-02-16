using RoR2.Skills;
using System.Collections.Generic;

namespace LoadoutEnigma.Utilities
{
    public sealed class SkillFamilyCatalogIndexComparer : IComparer<SkillFamily>
    {
        public static readonly SkillFamilyCatalogIndexComparer Instance = new SkillFamilyCatalogIndexComparer();

        public int Compare(SkillFamily x, SkillFamily y)
        {
            return x.catalogIndex.CompareTo(y.catalogIndex);
        }
    }
}
