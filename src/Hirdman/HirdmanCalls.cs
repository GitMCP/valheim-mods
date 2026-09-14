using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// The two things in the mod that have to cross the network under their own power.
    ///
    /// Everything else a retainer does travels for free in its ZDO, because a standing
    /// order is state and state is what a ZDO is for. These two are not state. Ringing a
    /// bell has to reach retainers that this machine cannot see - the peer holding one is
    /// often the server, or another player - and a scout's discoveries have to reach one
    /// particular player's map and no one else's. Both are events aimed at somebody, so
    /// both are messages.
    /// </summary>
    internal static class HirdmanCalls
    {
        private const string RecallCall = "Hirdman_Recall";
        private const string ScoutCall = "Hirdman_Scouted";

        private static bool _registered;

        /// <summary>
        /// Hooked to the start of a game rather than to plugin load, because the routing
        /// layer only exists once there is a world and a connection.
        /// </summary>
        internal static void Register()
        {
            if (_registered || ZRoutedRpc.instance == null)
            {
                return;
            }

            ZRoutedRpc.instance.Register<long, Vector3>(RecallCall, OnRecall);
            ZRoutedRpc.instance.Register<long, Vector3>(ScoutCall, OnScouted);
            _registered = true;
        }

        internal static void Forget()
        {
            _registered = false;
        }

        /// <summary>
        /// Calls a player's retainers home.
        /// </summary>
        /// <returns>
        /// How many this machine could answer for, which is the number the player is
        /// about to watch walk in. Retainers held by another peer answer too, a moment
        /// later and out of sight.
        /// </returns>
        internal static int Recall(long owner, Vector3 home)
        {
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RecallCall, owner, home);
            }

            return Answer(owner, home);
        }

        /// <summary>Tells a scout's employer what the scout can see.</summary>
        internal static void Scouted(long owner, Vector3 point)
        {
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, ScoutCall, owner, point);
            }
        }

        private static void OnRecall(long sender, long owner, Vector3 home)
        {
            Answer(owner, home);
        }

        /// <summary>
        /// Answers for the retainers this peer holds. Writing the same order twice does
        /// nothing, so it does not matter that the call also comes back to the ringer.
        /// </summary>
        private static int Answer(long owner, Vector3 home)
        {
            var answered = 0;

            foreach (var retainer in HirdmanRoster.Loaded())
            {
                var nview = retainer.GetComponent<ZNetView>();
                if (nview == null || !nview.IsValid())
                {
                    continue;
                }

                var contract = HirdmanContract.Read(nview.GetZDO());
                if (contract.Owner != owner)
                {
                    continue;
                }

                // Only the peer that owns a creature may write its ZDO. Claiming it here
                // would be a tug of war between every peer that heard the bell.
                if (!nview.IsOwner())
                {
                    continue;
                }

                // The bell is where home is. Ringing one somewhere new moves the whole
                // household, which is what a player who has moved on would expect.
                contract.Home = home;
                contract.Write(nview.GetZDO());

                var order = HirdmanOrder.Read(nview.GetZDO());
                order.Job = HirdmanJob.Idle;
                order.Anchor = home;
                order.Write(nview.GetZDO());

                HirdmanSpeech.Say(retainer, "Coming.");
                answered++;
            }

            return answered;
        }

        private static void OnScouted(long sender, long owner, Vector3 point)
        {
            var player = Player.m_localPlayer;
            if (player == null || Minimap.instance == null || player.GetPlayerID() != owner)
            {
                return;
            }

            // The same call the game makes for a walking player, at the radius the mod
            // allows a scout. Exploration is per-character and saved with it, so this is
            // a permanent addition to one person's map and nobody else's.
            Minimap.instance.Explore(point, HirdmanPlugin.ScoutSight.Value);
        }
    }
}
