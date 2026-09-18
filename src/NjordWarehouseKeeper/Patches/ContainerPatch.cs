using HarmonyLib;

namespace NjordWarehouseKeeper.Patches
{
    /// <summary>
    /// Vanilla chests refuse a second player while <see cref="Container.IsInUse"/>
    /// is set, and opening one hands ZDO ownership to the requester. Njord's
    /// dummy inventory is never listed, so several people can talk to him at
    /// once: the owner always grants, ownership stays put, and he is never
    /// marked in-use. Nearby chests still use the vanilla exclusive lock.
    /// </summary>
    [HarmonyPatch(typeof(Container))]
    internal static class ContainerPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("RPC_RequestOpen")]
        private static bool GrantHubOpen(Container __instance, long uid, long playerID)
        {
            return !GrantHub(__instance, uid, playerID, "RPC_OpenResponse");
        }

        [HarmonyPrefix]
        [HarmonyPatch("RPC_RequestStack")]
        private static bool GrantHubStack(Container __instance, long uid, long playerID)
        {
            return !GrantHub(__instance, uid, playerID, "RPC_StackResponse");
        }

        [HarmonyPrefix]
        [HarmonyPatch("RPC_RequestTakeAll")]
        private static bool GrantHubTakeAll(Container __instance, long uid, long playerID)
        {
            return !GrantHub(__instance, uid, playerID, "RPC_TakeAllResponse");
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Container.SetInUse))]
        private static bool SkipHubInUse(Container __instance)
        {
            return !NjordWarehouseKeeperMarker.IsHub(__instance);
        }

        private static bool GrantHub(Container container, long uid, long playerID, string response)
        {
            if (!NjordWarehouseKeeperMarker.IsHub(container))
            {
                return false;
            }

            var view = container.m_nview;
            if (view == null || !view.IsValid() || !view.IsOwner())
            {
                return true;
            }

            if (!container.CheckAccess(playerID))
            {
                view.InvokeRPC(uid, response, false);
                return true;
            }

            ZDOMan.instance.ForceSendZDO(uid, view.GetZDO().m_uid);
            view.InvokeRPC(uid, response, true);
            return true;
        }
    }
}
