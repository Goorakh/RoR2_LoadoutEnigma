using BepInEx.Configuration;
using RoR2.Skills;
using System;
using System.Collections;
using System.Collections.Generic;

namespace LoadoutEnigma
{
    public class ConfigSkillIndexList : IDisposable, IList<int>, IReadOnlyList<int>
    {
        readonly ConfigEntry<string> _configEntry;

        int[] _includedSkillIndices = [];

        public int this[int index]
        {
            get => _includedSkillIndices[index];
            set => _includedSkillIndices[index] = value;
        }

        public int Count => _includedSkillIndices.Length;

        public bool IsReadOnly => true;

        public ConfigSkillIndexList(ConfigEntry<string> configEntry)
        {
            _configEntry = configEntry;
            _configEntry.SettingChanged += onConfigValueChanged;

            CatalogAvailability.SkillCatalog.CallWhenAvailable(parseSkills);
        }

        public void Dispose()
        {
            _configEntry.SettingChanged -= onConfigValueChanged;
        }

        void onConfigValueChanged(object sender, EventArgs e)
        {
            if (CatalogAvailability.SkillCatalog.available)
            {
                parseSkills();
            }
        }

        void parseSkills()
        {
            string[] entries = _configEntry.Value.Split(',');

            HashSet<int> includedSkillIndices = new HashSet<int>(entries.Length);

            foreach (string entry in entries)
            {
                string trimmedEntry = entry.Trim();

                SkillDef matchingSkill = null;

                foreach (SkillDef skillDef in SkillCatalog.allSkillDefs)
                {
                    string skillName = SkillCatalog.GetSkillName(skillDef.skillIndex);
                    if (string.Equals(skillName, trimmedEntry, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingSkill = skillDef;
                        break;
                    }
                }

                SkillFamily matchingSkillFamily = null;

                foreach (SkillFamily skillFamily in SkillCatalog.allSkillFamilies)
                {
                    string skillFamilyName = SkillCatalog.GetSkillFamilyName(skillFamily.catalogIndex);
                    if (string.Equals(skillFamilyName, trimmedEntry, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingSkillFamily = skillFamily;
                        break;
                    }
                }

                if (matchingSkill)
                {
                    includedSkillIndices.Add(matchingSkill.skillIndex);
                }

                if (matchingSkillFamily)
                {
                    foreach (SkillFamily.Variant variant in matchingSkillFamily.variants)
                    {
                        if (variant.skillDef)
                        {
                            includedSkillIndices.Add(variant.skillDef.skillIndex);
                        }
                    }
                }
            }

            _includedSkillIndices = [.. includedSkillIndices];
            Array.Sort(_includedSkillIndices);
        }

        public bool Contains(int item)
        {
            return BinarySearch(item) >= 0;
        }

        public int IndexOf(int item)
        {
            int index = BinarySearch(item);
            return index >= 0 ? index : -1;
        }

        public int BinarySearch(int item)
        {
            return Array.BinarySearch(_includedSkillIndices, item);
        }

        public void CopyTo(int[] array, int arrayIndex)
        {
            _includedSkillIndices.CopyTo(array, arrayIndex);
        }

        public IEnumerator<int> GetEnumerator()
        {
            foreach (int skillIndex in _includedSkillIndices)
            {
                yield return skillIndex;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<int>.Add(int item)
        {
            throw new NotSupportedException();
        }

        void ICollection<int>.Clear()
        {
            throw new NotSupportedException();
        }

        void IList<int>.Insert(int index, int item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<int>.Remove(int item)
        {
            throw new NotSupportedException();
        }

        void IList<int>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }
    }
}
