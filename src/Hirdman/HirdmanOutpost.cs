using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// The banner post where retainers are hired.
    ///
    /// Building a person was the wrong shape for this. A hammer piece is placed once and
    /// costs its materials once, which makes every retainer a separate building project
    /// and gives the mod nowhere to put the rules that actually matter - how many you may
    /// keep, what each one costs, and whose they are. An outpost is built once and then
    /// hires as often as you can pay, which is both how a mercenary post reads and where
    /// those rules belong.
    /// </summary>
    internal static class HirdmanOutpost
    {
        internal const string PrefabName = "hirdman_outpost";

        /// <summary>
        /// A two-metre wooden post. Cloned rather than authored so that the piece arrives
        /// with the game's own collider, <see cref="WearNTear"/> and <see cref="Piece"/>;
        /// only the model is replaced.
        /// </summary>
        private const string CloneSource = "wood_pole2";

        internal static readonly RequirementConfig[] Resources =
        {
            new RequirementConfig("Wood", 20, 0, true),
            new RequirementConfig("Stone", 10, 0, true),
            new RequirementConfig("LeatherScraps", 8, 0, true),
        };

        internal static void Register()
        {
            var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabName, CloneSource);
            if (prefab == null)
            {
                HirdmanPlugin.Log.LogError($"Could not clone '{CloneSource}' for the outpost.");
                return;
            }

            Build(prefab);
            prefab.AddComponent<HirdmanOutpostPost>();

            var config = new PieceConfig
            {
                Name = $"${PrefabName}_name",
                Description = $"${PrefabName}_description",
                PieceTable = PieceTables.Hammer,
                Category = PieceCategories.Misc,
                CraftingStation = CraftingStations.Workbench,
                Requirements = Resources,
            };

            if (HirdmanAssets.Icon != null)
            {
                config.Icon = HirdmanAssets.Icon;
            }

            if (!PieceManager.Instance.AddPiece(new CustomPiece(prefab, fixReference: false, config)))
            {
                HirdmanPlugin.Log.LogError($"Failed to register piece '{PrefabName}'.");
            }
        }

        /// <summary>
        /// A post with a war banner hanging from a crossarm: the sight that means someone
        /// here is hiring.
        /// </summary>
        private static void Build(GameObject prefab)
        {
            var visual = HirdmanShapes.Clear(prefab, "OutpostVisual");

            var parts = HirdmanShapes.Wood(visual, "Post",
                new Vector3(0f, 1.2f, 0f), new Vector3(0.2f, 2.4f, 0.2f));

            parts += HirdmanShapes.Wood(visual, "Crossarm",
                new Vector3(0f, 2.32f, 0f), new Vector3(1f, 0.14f, 0.14f));

            // The cloth is modelled hanging below its own pivot, and lies in the plane
            // its pivot faces, so it is placed by its top edge and turned a quarter turn
            // to hang along the crossarm rather than across it.
            parts += HirdmanShapes.Fit("piece_banner01", "default", visual, "Banner",
                new Vector3(0f, 2.28f, 0f),
                Vector3.one * 0.55f,
                Quaternion.Euler(0f, 90f, 0f),
                new Vector3(0f, 0.045f, 0f));

            HirdmanPlugin.Log.LogInfo($"Built the outpost from {parts} borrowed part(s).");
        }
    }

    /// <summary>
    /// The hiring itself. Runs on the machine of whoever pressed Use, which is where the
    /// coins are and where the answer has to be shown.
    /// </summary>
    internal class HirdmanOutpostPost : MonoBehaviour, Hoverable, Interactable
    {
        /// <summary>Far enough in front that a new recruit does not spawn inside the post.</summary>
        private static readonly Vector3 SpawnOffset = new Vector3(0f, 0f, 1.8f);

        /// <summary>
        /// The piece's origin is the foot of the post, so without this the prompt would
        /// be drawn in the grass rather than up by the banner.
        /// </summary>
        private const float HoverHeight = 1.9f;

        private ZNetView _nview;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
        }

        public string GetHoverName()
        {
            return Localization.instance.Localize($"${HirdmanOutpost.PrefabName}_name");
        }

        public string GetHoverText()
        {
            var player = Player.m_localPlayer;
            var price = HirdmanPlugin.HirePrice.Value;
            var cap = HirdmanPlugin.HireLimit.Value;
            var hired = player == null ? 0 : HirdmanRoster.CountFor(player);
            var coins = player == null ? 0 : HirdmanWallet.Count(player);

            var text = $"{GetHoverName()}\n{hired}/{cap} in your service, {coins} coins on you\n";
            text += hired >= cap
                ? "[<color=yellow><b>$KEY_Use</b></color>] Nobody else will sign on"
                : $"[<color=yellow><b>$KEY_Use</b></color>] Hire for {price} coins";

            return Localization.instance.Localize(text);
        }

        public float GetHoverOffset()
        {
            return HoverHeight;
        }

        public bool Interact(Humanoid user, bool hold, bool alt)
        {
            // Holding Use repeats every frame, and hiring is not something to do sixty
            // times a second.
            if (hold)
            {
                return false;
            }

            var player = user as Player;
            if (player == null || _nview == null || !_nview.IsValid())
            {
                return false;
            }

            var cap = HirdmanPlugin.HireLimit.Value;
            if (HirdmanRoster.CountFor(player) >= cap)
            {
                player.Message(MessageHud.MessageType.Center, $"You already keep {cap} in your service.");
                return true;
            }

            var price = HirdmanPlugin.HirePrice.Value;
            if (!HirdmanWallet.Pay(player, price))
            {
                player.Message(MessageHud.MessageType.Center, $"Nobody signs on for less than {price} coins.");
                return true;
            }

            var retainer = Hire(player);
            if (retainer == null)
            {
                // Refunding is the only honest answer to a failure after payment.
                HirdmanWallet.Refund(player, price);
                player.Message(MessageHud.MessageType.Center, "Nobody answered the call.");
                return true;
            }

            player.Message(MessageHud.MessageType.Center,
                $"{HirdmanNames.Of(retainer)} enters your service.");
            return true;
        }

        public bool UseItem(Humanoid user, ItemDrop.ItemData item)
        {
            return false;
        }

        private GameObject Hire(Player player)
        {
            var prefab = ZNetScene.instance == null
                ? null
                : ZNetScene.instance.GetPrefab(HirdmanRetainer.PrefabName);
            if (prefab == null)
            {
                HirdmanPlugin.Log.LogError($"'{HirdmanRetainer.PrefabName}' is not registered.");
                return null;
            }

            var home = transform.position;
            var spot = home + transform.rotation * SpawnOffset;
            var retainer = Object.Instantiate(prefab, spot, transform.rotation);

            HirdmanNames.Christen(retainer);

            HirdmanContract.Sign(retainer, new HirdmanContract
            {
                Owner = player.GetPlayerID(),
                OwnerName = player.GetPlayerName(),
                Home = home,
            });

            // Told to wait here, so a new recruit stands by the post that hired it rather
            // than inheriting whatever an empty ZDO happens to mean.
            HirdmanBrain.Give(retainer, new HirdmanOrder
            {
                Job = HirdmanJob.Idle,
                Anchor = home,
                Master = HirdmanOrder.Identify(player),
                Subject = string.Empty,
            });

            return retainer;
        }
    }

    /// <summary>
    /// Coins, looked up rather than named. The item's display token is what an inventory
    /// matches on, and reading it off the prefab means a rename in a game update cannot
    /// quietly make everything free.
    /// </summary>
    internal static class HirdmanWallet
    {
        private const string CoinPrefab = "Coins";

        private static string _token;

        private static string Token()
        {
            if (!string.IsNullOrEmpty(_token))
            {
                return _token;
            }

            var coins = ObjectDB.instance == null ? null : ObjectDB.instance.GetItemPrefab(CoinPrefab);
            var drop = coins == null ? null : coins.GetComponent<ItemDrop>();
            _token = drop == null ? null : drop.m_itemData.m_shared.m_name;

            if (string.IsNullOrEmpty(_token))
            {
                HirdmanPlugin.Log.LogWarning("Could not find out what coins are called.");
            }

            return _token;
        }

        internal static int Count(Player player)
        {
            var token = Token();
            return player == null || token == null
                ? 0
                : player.GetInventory().CountItems(token, -1, false);
        }

        internal static bool Pay(Player player, int price)
        {
            if (price <= 0)
            {
                return true;
            }

            var token = Token();
            if (token == null || Count(player) < price)
            {
                return false;
            }

            player.GetInventory().RemoveItem(token, price, -1, false);
            return true;
        }

        internal static void Refund(Player player, int price)
        {
            if (price <= 0 || player == null)
            {
                return;
            }

            player.GetInventory().AddItem(CoinPrefab, price, 1, 0, 0L, string.Empty, false, true);
        }
    }
}
