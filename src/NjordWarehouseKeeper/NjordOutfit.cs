using UnityEngine;

namespace NjordWarehouseKeeper
{
    /// <summary>
    /// Visual-only dressing and poses for Njord. Items stay in the chests;
    /// the look is a set of prefab hashes on his piece ZDO, one per slot.
    /// </summary>
    internal enum NjordPose
    {
        Sheathed = 0,
        Drawn = 1,
    }

    internal enum NjordSlot
    {
        Chest,
        Legs,
        Helmet,
        Shoulder,
        Utility,
        Trinket,
        HandLeft,
        HandRight,
    }

    internal static class NjordOutfit
    {
        internal const int PoseCount = 2;

        private const string PoseKey = "njord.pose";
        private const string ChestKey = "njord.chest";
        private const string LegsKey = "njord.legs";
        private const string HelmetKey = "njord.helmet";
        private const string ShoulderKey = "njord.shoulder";
        private const string ShoulderVarKey = "njord.shoulder.var";
        private const string ShoulderQualKey = "njord.shoulder.q";
        private const string UtilityKey = "njord.utility";
        private const string TrinketKey = "njord.trinket";
        private const string HandLeftKey = "njord.handL";
        private const string HandLeftVarKey = "njord.handL.var";
        private const string HandLeftQualKey = "njord.handL.q";
        private const string HandRightKey = "njord.handR";
        private const string HandRightQualKey = "njord.handR.q";

        internal static bool IsEquipable(ItemDrop.ItemData item)
        {
            return TrySlot(item, out _);
        }

        internal static bool TrySlot(ItemDrop.ItemData item, out NjordSlot slot)
        {
            slot = NjordSlot.Chest;
            var shared = item?.m_shared;
            if (shared == null)
            {
                return false;
            }

            var type = shared.m_itemType;
            if (shared.m_attachOverride != ItemDrop.ItemData.ItemType.None)
            {
                type = shared.m_attachOverride;
            }

            switch (type)
            {
                case ItemDrop.ItemData.ItemType.Chest:
                    slot = NjordSlot.Chest;
                    return true;
                case ItemDrop.ItemData.ItemType.Legs:
                    slot = NjordSlot.Legs;
                    return true;
                case ItemDrop.ItemData.ItemType.Helmet:
                    slot = NjordSlot.Helmet;
                    return true;
                case ItemDrop.ItemData.ItemType.Shoulder:
                    slot = NjordSlot.Shoulder;
                    return true;
                case ItemDrop.ItemData.ItemType.Utility:
                    slot = NjordSlot.Utility;
                    return true;
                case ItemDrop.ItemData.ItemType.Trinket:
                    slot = NjordSlot.Trinket;
                    return true;
                case ItemDrop.ItemData.ItemType.Shield:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                    slot = NjordSlot.HandLeft;
                    return true;
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                case ItemDrop.ItemData.ItemType.Tool:
                case ItemDrop.ItemData.ItemType.Torch:
                    slot = NjordSlot.HandRight;
                    return true;
                default:
                    return false;
            }
        }

        internal static bool Toggle(Container hub, ItemDrop.ItemData item)
        {
            if (hub == null || !TrySlot(item, out var slot))
            {
                return false;
            }

            var hash = PrefabHash(item);
            if (hash == 0)
            {
                return false;
            }

            var wearing = IsWearing(hub, item);
            WriteSlot(hub, slot, wearing ? 0 : hash, item, clear: wearing);
            if (!wearing)
            {
                ClearConflicts(hub, item, slot);
            }

            Apply(hub);
            return true;
        }

        internal static bool IsWearing(Container hub, ItemDrop.ItemData item)
        {
            if (hub == null || !TrySlot(item, out var slot))
            {
                return false;
            }

            var zdo = Zdo(hub);
            if (zdo == null)
            {
                return false;
            }

            return ReadHash(zdo, slot) == PrefabHash(item) && PrefabHash(item) != 0;
        }

        internal static NjordPose CyclePose(Container hub)
        {
            var current = ReadPose(hub);
            var next = (NjordPose)(((int)current + 1) % PoseCount);
            WritePose(hub, next);
            Apply(hub);
            return next;
        }

        internal static NjordPose ReadPose(Container hub)
        {
            var zdo = Zdo(hub);
            if (zdo == null)
            {
                return NjordPose.Sheathed;
            }

            var value = zdo.GetInt(PoseKey, 0);
            if (value < 0 || value >= PoseCount)
            {
                return NjordPose.Sheathed;
            }

            return (NjordPose)value;
        }

        internal static string ActionToken(NjordPose pose)
        {
            return pose == NjordPose.Drawn ? "$njord_pose_sheathe" : "$njord_pose_draw";
        }

        internal static void Apply(Container hub)
        {
            if (hub == null)
            {
                return;
            }

            var hook = hub.GetComponentInChildren<NjordLookHook>(true);
            var vis = hook != null
                ? hook.GetComponent<VisEquipment>()
                : hub.GetComponentInChildren<VisEquipment>(true);
            Apply(hub, vis, hook != null ? hook.GetComponent<Animator>() : null);
        }

        internal static void Apply(Container hub, VisEquipment vis, Animator animator)
        {
            if (vis == null)
            {
                return;
            }

            var view = ViewOf(hub);
            if (view == null)
            {
                view = vis.m_nViewOverride != null ? vis.m_nViewOverride : vis.m_nview;
            }

            if (view != null)
            {
                vis.m_nViewOverride = view;
                vis.m_nview = view;
            }

            vis.m_playerComponent = null;
            vis.m_isPlayer = true;
            vis.m_isArmorStand = false;

            if (vis.m_nview == null)
            {
                ApplyAnimator(animator, ReadPose(hub), 0, 0);
                return;
            }

            var zdo = view != null && view.IsValid() ? view.GetZDO() : null;
            var owner = zdo != null && view.IsOwner();

            var chest = zdo == null ? 0 : zdo.GetInt(ChestKey, 0);
            var legs = zdo == null ? 0 : zdo.GetInt(LegsKey, 0);
            var helmet = zdo == null ? 0 : zdo.GetInt(HelmetKey, 0);
            var shoulder = zdo == null ? 0 : zdo.GetInt(ShoulderKey, 0);
            var shoulderVar = zdo == null ? 0 : zdo.GetInt(ShoulderVarKey, 0);
            var shoulderQual = zdo == null ? 0 : zdo.GetInt(ShoulderQualKey, 0);
            var utility = zdo == null ? 0 : zdo.GetInt(UtilityKey, 0);
            var trinket = zdo == null ? 0 : zdo.GetInt(TrinketKey, 0);
            var handL = zdo == null ? 0 : zdo.GetInt(HandLeftKey, 0);
            var handLVar = zdo == null ? 0 : zdo.GetInt(HandLeftVarKey, 0);
            var handLQual = zdo == null ? 0 : zdo.GetInt(HandLeftQualKey, 0);
            var handR = zdo == null ? 0 : zdo.GetInt(HandRightKey, 0);
            var handRQual = zdo == null ? 0 : zdo.GetInt(HandRightQualKey, 0);
            var pose = ReadPose(hub);
            var drawn = pose == NjordPose.Drawn;

            if (chest == 0)
            {
                chest = DefaultHash(ItemDrop.ItemData.ItemType.Chest, "ArmorLeatherChest", "leather");
            }

            if (legs == 0)
            {
                legs = DefaultHash(ItemDrop.ItemData.ItemType.Legs, "ArmorLeatherLegs", "leather");
            }

            try
            {
                vis.SetHelmetItem(helmet);
                vis.SetShoulderItem(shoulder, shoulderVar, shoulderQual);
                vis.SetUtilityItem(utility);
                vis.SetTrinketItem(trinket == 0 ? "" : PrefabName(trinket));
                vis.SetChestItem(chest);
                vis.SetLegItem(legs);

                if (drawn)
                {
                    vis.SetLeftItem(handL, handLVar, handLQual);
                    vis.SetRightItem(handR, handRQual);
                    vis.SetLeftBackItem(0, 0, 0);
                    vis.SetRightBackItem(0, 0);
                }
                else
                {
                    vis.SetLeftItem(0, 0, 0);
                    vis.SetRightItem(0, 0);
                    vis.SetLeftBackItem(handL, handLVar, handLQual);
                    vis.SetRightBackItem(handR, handRQual);
                }

                if (owner)
                {
                    EnsureHair(vis, zdo);
                }

                vis.UpdateVisuals();
            }
            catch (System.Exception ex)
            {
                NjordWarehouseKeeperPlugin.Log.LogWarning("Njord visuals failed: " + ex);
            }

            ApplyAnimator(animator, pose, drawn ? handR : 0, drawn ? handL : 0);
        }

        internal static ZNetView ViewOf(Container hub)
        {
            if (hub == null)
            {
                return null;
            }

            if (hub.m_nview != null)
            {
                return hub.m_nview;
            }

            return hub.GetComponent<ZNetView>();
        }

        internal static void ApplyAnimator(Animator animator, NjordPose pose, int drawnRight, int drawnLeft)
        {
            if (animator == null || !animator.isInitialized)
            {
                return;
            }

            try
            {
                animator.SetBool("encumbered", false);
                animator.SetBool("crouching", false);
                animator.SetBool("flying", false);
                animator.SetBool("inWater", false);
                animator.SetBool("wakeup", false);
                animator.SetBool("intro", false);
                animator.SetBool("dead", false);
                animator.SetBool("onGround", true);
                animator.SetFloat("forward_speed", 0f);
                animator.SetFloat("sideway_speed", 0f);
                animator.SetFloat("turn_speed", 0f);

                animator.ResetTrigger("emote_stop");
                animator.SetBool("emote_sit", false);
                animator.SetBool("emote_flex", false);

                var state = ItemDrop.ItemData.AnimationState.Unarmed;
                if (pose == NjordPose.Drawn)
                {
                    state = DrawnState(drawnRight, drawnLeft);
                }

                animator.SetFloat("statef", (float)state);
                animator.SetInteger("statei", (int)state);
            }
            catch (System.Exception)
            {
            }
        }

        internal static int PrefabHash(ItemDrop.ItemData item)
        {
            var name = PrefabName(item);
            return string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode();
        }

        internal static string PrefabName(ItemDrop.ItemData item)
        {
            if (item?.m_dropPrefab != null)
            {
                return Utils.GetPrefabName(item.m_dropPrefab);
            }

            var shared = item?.m_shared?.m_name;
            var sample = FindPrefab(shared);
            return sample == null ? "" : Utils.GetPrefabName(sample);
        }

        private static void WritePose(Container hub, NjordPose pose)
        {
            var zdo = OwnZdo(hub);
            if (zdo == null)
            {
                return;
            }

            zdo.Set(PoseKey, (int)pose);
        }

        private static void WriteSlot(Container hub, NjordSlot slot, int hash, ItemDrop.ItemData item, bool clear)
        {
            var zdo = OwnZdo(hub);
            if (zdo == null)
            {
                return;
            }

            var variant = clear || item == null ? 0 : item.m_variant;
            var quality = clear || item == null ? 0 : item.m_quality;
            switch (slot)
            {
                case NjordSlot.Chest:
                    zdo.Set(ChestKey, hash);
                    break;
                case NjordSlot.Legs:
                    zdo.Set(LegsKey, hash);
                    break;
                case NjordSlot.Helmet:
                    zdo.Set(HelmetKey, hash);
                    break;
                case NjordSlot.Shoulder:
                    zdo.Set(ShoulderKey, hash);
                    zdo.Set(ShoulderVarKey, variant);
                    zdo.Set(ShoulderQualKey, quality);
                    break;
                case NjordSlot.Utility:
                    zdo.Set(UtilityKey, hash);
                    break;
                case NjordSlot.Trinket:
                    zdo.Set(TrinketKey, hash);
                    break;
                case NjordSlot.HandLeft:
                    zdo.Set(HandLeftKey, hash);
                    zdo.Set(HandLeftVarKey, variant);
                    zdo.Set(HandLeftQualKey, quality);
                    break;
                case NjordSlot.HandRight:
                    zdo.Set(HandRightKey, hash);
                    zdo.Set(HandRightQualKey, quality);
                    break;
            }
        }

        private static void ClearConflicts(Container hub, ItemDrop.ItemData item, NjordSlot slot)
        {
            var type = item?.m_shared?.m_itemType ?? ItemDrop.ItemData.ItemType.None;
            var overrideType = item?.m_shared?.m_attachOverride ?? ItemDrop.ItemData.ItemType.None;
            if (overrideType != ItemDrop.ItemData.ItemType.None)
            {
                type = overrideType;
            }

            var occupiesBoth = type == ItemDrop.ItemData.ItemType.TwoHandedWeapon
                || type == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft
                || type == ItemDrop.ItemData.ItemType.Bow
                || type == ItemDrop.ItemData.ItemType.Attach_Atgeir
                || type == ItemDrop.ItemData.ItemType.Tool;

            if (occupiesBoth)
            {
                var other = slot == NjordSlot.HandLeft ? NjordSlot.HandRight : NjordSlot.HandLeft;
                WriteSlot(hub, other, 0, null, clear: true);
                return;
            }

            if (slot != NjordSlot.HandRight && slot != NjordSlot.HandLeft)
            {
                return;
            }

            var otherSlot = slot == NjordSlot.HandLeft ? NjordSlot.HandRight : NjordSlot.HandLeft;
            var otherHash = ReadHash(Zdo(hub), otherSlot);
            if (otherHash == 0)
            {
                return;
            }

            var otherItem = ItemOf(otherHash);
            var otherType = otherItem?.m_shared?.m_itemType ?? ItemDrop.ItemData.ItemType.None;
            var otherOccupiesBoth = otherType == ItemDrop.ItemData.ItemType.TwoHandedWeapon
                || otherType == ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft
                || otherType == ItemDrop.ItemData.ItemType.Bow
                || otherType == ItemDrop.ItemData.ItemType.Attach_Atgeir
                || otherType == ItemDrop.ItemData.ItemType.Tool;
            var keepLeft = otherType == ItemDrop.ItemData.ItemType.Shield
                || otherType == ItemDrop.ItemData.ItemType.Torch;
            if (otherOccupiesBoth || (slot == NjordSlot.HandRight && !keepLeft && otherSlot == NjordSlot.HandLeft))
            {
                WriteSlot(hub, otherSlot, 0, null, clear: true);
            }
        }

        private static int ReadHash(ZDO zdo, NjordSlot slot)
        {
            if (zdo == null)
            {
                return 0;
            }

            switch (slot)
            {
                case NjordSlot.Chest:
                    return zdo.GetInt(ChestKey, 0);
                case NjordSlot.Legs:
                    return zdo.GetInt(LegsKey, 0);
                case NjordSlot.Helmet:
                    return zdo.GetInt(HelmetKey, 0);
                case NjordSlot.Shoulder:
                    return zdo.GetInt(ShoulderKey, 0);
                case NjordSlot.Utility:
                    return zdo.GetInt(UtilityKey, 0);
                case NjordSlot.Trinket:
                    return zdo.GetInt(TrinketKey, 0);
                case NjordSlot.HandLeft:
                    return zdo.GetInt(HandLeftKey, 0);
                case NjordSlot.HandRight:
                    return zdo.GetInt(HandRightKey, 0);
                default:
                    return 0;
            }
        }

        private static void EnsureHair(VisEquipment vis, ZDO zdo)
        {
            if (vis == null || zdo == null)
            {
                return;
            }

            if (zdo.GetInt(ZDOVars.s_hairItem) == 0)
            {
                vis.SetHairItem("Hair4".GetStableHashCode());
            }

            if (zdo.GetInt(ZDOVars.s_beardItem) == 0)
            {
                vis.SetBeardItem("Beard5".GetStableHashCode());
            }
        }

        private static ItemDrop.ItemData.AnimationState DrawnState(int rightHash, int leftHash)
        {
            var right = ItemOf(rightHash);
            if (right?.m_shared != null)
            {
                return right.m_shared.m_animationState;
            }

            var left = ItemOf(leftHash);
            if (left?.m_shared != null)
            {
                if (left.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Torch)
                {
                    return ItemDrop.ItemData.AnimationState.LeftTorch;
                }

                return left.m_shared.m_animationState;
            }

            return ItemDrop.ItemData.AnimationState.Unarmed;
        }

        private static ItemDrop.ItemData ItemOf(int hash)
        {
            if (hash == 0 || ObjectDB.instance == null)
            {
                return null;
            }

            var prefab = ObjectDB.instance.GetItemPrefab(hash);
            return prefab == null ? null : prefab.GetComponent<ItemDrop>()?.m_itemData;
        }

        private static string PrefabName(int hash)
        {
            var prefab = hash == 0 || ObjectDB.instance == null
                ? null
                : ObjectDB.instance.GetItemPrefab(hash);
            return prefab == null ? "" : Utils.GetPrefabName(prefab);
        }

        private static GameObject FindPrefab(string sharedName)
        {
            var db = ObjectDB.instance;
            if (db == null || db.m_items == null || string.IsNullOrEmpty(sharedName))
            {
                return null;
            }

            for (var i = 0; i < db.m_items.Count; i++)
            {
                var go = db.m_items[i];
                var drop = go == null ? null : go.GetComponent<ItemDrop>();
                if (drop?.m_itemData?.m_shared != null && drop.m_itemData.m_shared.m_name == sharedName)
                {
                    return go;
                }
            }

            return null;
        }

        private static int DefaultHash(ItemDrop.ItemData.ItemType type, string prefab, params string[] needles)
        {
            var name = NjordLook.FindArmor(type, prefab, needles);
            return string.IsNullOrEmpty(name) ? 0 : name.GetStableHashCode();
        }

        private static bool Own(Container hub)
        {
            var view = ViewOf(hub);
            if (view == null || !view.IsValid())
            {
                return false;
            }

            if (!view.IsOwner())
            {
                view.ClaimOwnership();
            }

            return view.IsOwner();
        }

        private static ZDO OwnZdo(Container hub)
        {
            return Own(hub) ? Zdo(hub) : null;
        }

        private static ZDO Zdo(Container hub)
        {
            var view = ViewOf(hub);
            return view == null || !view.IsValid() ? null : view.GetZDO();
        }
    }
}
