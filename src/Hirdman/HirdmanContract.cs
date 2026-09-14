using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Who a retainer works for, and where it calls home.
    ///
    /// Kept apart from <see cref="HirdmanOrder"/> because the two change on completely
    /// different clocks. An order changes whenever someone speaks; a contract is written
    /// once, when the coins change hands, and then only ever read - by the bell deciding
    /// whose retainers to call, by the outpost counting how many someone already has, and
    /// by a client deciding whether a scout's discoveries belong on its map.
    ///
    /// The owner is a player ID rather than a <see cref="ZDOID"/> because it has to
    /// survive the player logging out, dying, and coming back as a new character object.
    /// A ZDOID names one incarnation; a player ID names the person.
    /// </summary>
    internal struct HirdmanContract
    {
        private const string OwnerKey = "hird_owner";
        private const string OwnerNameKey = "hird_ownername";
        private const string HomeKey = "hird_home";

        /// <summary>Whose retainer this is. Zero means nobody has claimed it.</summary>
        internal long Owner;

        /// <summary>Only ever shown, never matched on.</summary>
        internal string OwnerName;

        /// <summary>The outpost it was hired at, and the ground the bell calls it to.</summary>
        internal Vector3 Home;

        internal static HirdmanContract Read(ZDO zdo)
        {
            return new HirdmanContract
            {
                Owner = zdo.GetLong(OwnerKey, 0L),
                OwnerName = zdo.GetString(OwnerNameKey, string.Empty),
                Home = zdo.GetVec3(HomeKey, zdo.GetPosition()),
            };
        }

        internal static HirdmanContract Of(GameObject retainer)
        {
            var nview = retainer == null ? null : retainer.GetComponent<ZNetView>();
            return nview != null && nview.IsValid()
                ? Read(nview.GetZDO())
                : default(HirdmanContract);
        }

        internal void Write(ZDO zdo)
        {
            zdo.Set(OwnerKey, Owner);
            zdo.Set(OwnerNameKey, OwnerName ?? string.Empty);
            zdo.Set(HomeKey, Home);
        }

        /// <summary>
        /// Signs a retainer up, from whichever peer took the payment. Writing a ZDO means
        /// owning it first, which is the same move the game makes when you open someone
        /// else's chest.
        /// </summary>
        internal static bool Sign(GameObject retainer, HirdmanContract contract)
        {
            var nview = retainer == null ? null : retainer.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            if (!nview.IsOwner())
            {
                nview.ClaimOwnership();
            }

            contract.Write(nview.GetZDO());
            return true;
        }

        internal bool BelongsTo(Player player)
        {
            return player != null && Owner != 0L && Owner == player.GetPlayerID();
        }
    }
}
