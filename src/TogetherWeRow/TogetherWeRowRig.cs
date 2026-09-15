using System.Collections.Generic;
using UnityEngine;

namespace TogetherWeRow
{
    /// <summary>
    /// One ship's worth of oars. Added when the ship wakes, finds every passenger
    /// chair that can reach the water, and hangs an oar on the outboard side of it.
    /// Seats on the mast are skipped. The oars row when that seat is taken and the
    /// helm has the ship under way, sail or no sail; otherwise they rest.
    /// </summary>
    internal class TogetherWeRowRig : MonoBehaviour
    {
        private readonly List<Bench> _benches = new List<Bench>();
        private Ship _ship;
        private bool _built;

        private void Awake()
        {
            _ship = GetComponent<Ship>();
        }

        private void Start()
        {
            Build();
        }

        private void Update()
        {
            if (!_built)
            {
                Build();
                return;
            }

            if (_ship == null)
            {
                return;
            }

            var moving = _ship.m_shipControlls != null
                         && _ship.m_shipControlls.HaveValidUser()
                         && _ship.GetSpeedSetting() != Ship.Speed.Stop;
            var time = Time.time;

            foreach (var bench in _benches)
            {
                if (bench.Pivot == null)
                {
                    continue;
                }

                var rowing = moving && TogetherWeRowCrew.Occupied(bench.Attach);
                bench.Pivot.localRotation = rowing
                    ? TogetherWeRowOars.StrokeAt(bench.Side, time)
                    : TogetherWeRowOars.Rest(bench.Side);
            }
        }

        private void Build()
        {
            if (_built || _ship == null)
            {
                return;
            }

            if (Jotunn.Managers.PrefabManager.Instance == null
                || Jotunn.Managers.PrefabManager.Instance.GetPrefab("wood_pole") == null)
            {
                return;
            }

            var chairs = GetComponentsInChildren<Chair>(true);
            foreach (var chair in chairs)
            {
                if (!chair.m_inShip || chair.m_attachPoint == null || !TogetherWeRowCrew.CanRow(_ship, chair.m_attachPoint))
                {
                    continue;
                }

                var local = transform.InverseTransformPoint(chair.m_attachPoint.position);
                var side = local.x >= 0f ? 1f : -1f;
                var pivot = TogetherWeRowOars.Build(transform, chair.m_attachPoint, side);
                _benches.Add(new Bench
                {
                    Attach = chair.m_attachPoint,
                    Pivot = pivot,
                    Side = side,
                });
            }

            _built = true;
            if (_benches.Count > 0)
            {
                TogetherWeRowPlugin.Log.LogInfo($"{_ship.name} gained {_benches.Count} oar(s).");
            }
        }

        private struct Bench
        {
            public Transform Attach;
            public Transform Pivot;
            public float Side;
        }
    }
}
