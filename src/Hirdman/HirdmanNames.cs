using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Gives every retainer a name of its own.
    ///
    /// Four things called "Hirdman" standing in a yard are four copies of a prefab. Four
    /// things called Gunnar, Sigrun, Hallr and Yrsa are a household, and the difference
    /// costs one string in a ZDO. It matters more than it sounds: the point of the mod is
    /// companions who read as people, and nothing undoes that faster than not being able
    /// to tell which one you just spoke to.
    /// </summary>
    internal static class HirdmanNames
    {
        private const string NameKey = "hird_name";

        private static readonly string[] Roll =
        {
            "Gunnar", "Sigrun", "Hallr", "Yrsa", "Bersi", "Thora", "Ketill", "Astrid",
            "Ozur", "Ingrid", "Hroar", "Solveig", "Ulf", "Gudrun", "Steinar", "Ragna",
            "Egill", "Freydis", "Bjarni", "Halla", "Onund", "Thurid", "Skarde", "Vigdis",
        };

        /// <summary>Names a fresh recruit, on the peer that just hired it.</summary>
        internal static void Christen(GameObject retainer)
        {
            var nview = retainer == null ? null : retainer.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return;
            }

            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            var name = Roll[Random.Range(0, Roll.Length)];
            nview.GetZDO().Set(NameKey, name);
            nview.GetZDO().Set(ZDOVars.s_playerName, name);
            nview.GetZDO().Set(ZDOVars.s_tamedName, name);

            var player = retainer.GetComponent<Player>();
            if (player != null)
            {
                player.SetPlayerID(nview.GetZDO().m_uid.ID, name);
            }
        }

        internal static string Of(GameObject retainer)
        {
            var nview = retainer == null ? null : retainer.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return "Hirdman";
            }

            var given = nview.GetZDO().GetString(NameKey, string.Empty);
            return string.IsNullOrEmpty(given) ? "Hirdman" : given;
        }
    }
}
