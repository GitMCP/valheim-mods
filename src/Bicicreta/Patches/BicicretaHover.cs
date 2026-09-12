namespace Bicicreta.Patches
{
    /// <summary>
    /// One hover label for the whole bicycle, so aiming at the frame and aiming at the
    /// saddle say the same thing.
    /// </summary>
    internal static class BicicretaHover
    {
        internal static string Name()
        {
            return Localization.instance.Localize($"${BicicretaMount.PrefabName}_name");
        }

        internal static string Text()
        {
            return Localization.instance.Localize(
                $"${BicicretaMount.PrefabName}_name\n" +
                $"[<color=yellow><b>$KEY_Use</b></color>] ${BicicretaMount.PrefabName}_ride");
        }
    }
}
