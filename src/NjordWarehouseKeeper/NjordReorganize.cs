using NjordWarehouseKeeper.Storage;
using NjordWarehouseKeeper.UI;
using UnityEngine;

namespace NjordWarehouseKeeper
{
    /// <summary>
    /// On a timer, the keeper who owns this ZDO merges leftover stacks in
    /// nearby chests so the same item occupies as few slots as it can.
    /// </summary>
    internal sealed class NjordReorganize : MonoBehaviour
    {
        private const float OwnerRetry = 3f;

        private ZNetView _view;
        private Container _hub;
        private float _next;

        private void Awake()
        {
            _view = GetComponent<ZNetView>();
            _hub = GetComponent<Container>();
            _next = Time.time + Random.Range(8f, 20f);
        }

        private void Update()
        {
            if (NjordWarehouseKeeperPlugin.Reorganize == null
                || !NjordWarehouseKeeperPlugin.Reorganize.Value)
            {
                return;
            }

            if (Time.time < _next)
            {
                return;
            }

            if (_view == null || !_view.IsValid() || !_view.IsOwner() || _hub == null)
            {
                _next = Time.time + OwnerRetry;
                return;
            }

            ScheduleNext();
            if (NjordWarehouseKeeperMarker.OpenHub == _hub && NjordWarehouseKeeperPanel.IsDragging())
            {
                _next = Time.time + 2f;
                return;
            }

            var freed = StorageNetwork.Reorganize(_hub);
            if (freed > 0 && NjordWarehouseKeeperMarker.OpenHub == _hub)
            {
                NjordWarehouseKeeperPanel.RefreshAfterRemote();
            }
        }

        private void ScheduleNext()
        {
            var interval = NjordWarehouseKeeperPlugin.ReorganizeInterval != null
                ? NjordWarehouseKeeperPlugin.ReorganizeInterval.Value
                : 60f;
            if (interval < 15f)
            {
                interval = 15f;
            }

            _next = Time.time + interval + Random.Range(0f, 8f);
        }
    }
}
