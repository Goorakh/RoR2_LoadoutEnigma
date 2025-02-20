using RoR2;

namespace LoadoutEnigma
{
    static class Commands
    {
        [ConCommand(commandName = "loadout_enigma_force_advance")]
        static void CCAdvanceLoadoutEnigma(ConCommandArgs args)
        {
            CharacterBody senderBody = args.senderBody;
            if (!senderBody)
                return;

            if (!senderBody.hasEffectiveAuthority)
                return;

            if (senderBody.TryGetComponent(out LoadoutEnigmaController loadoutEnigmaController) && loadoutEnigmaController.enabled)
            {
                loadoutEnigmaController.SetRandomSkills();
            }
        }
    }
}
