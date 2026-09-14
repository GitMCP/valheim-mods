using Hirdman.Patches;
using Hirdman.Work;
using UnityEngine;

namespace Hirdman
{
    /// <summary>
    /// Carries out a retainer's standing order.
    ///
    /// This runs only on the peer that owns the retainer, and only for the parts of
    /// behaviour the game does not already do better. Following is left entirely to
    /// <see cref="MonsterAI"/>, which has a tamed wolf's worth of experience at walking
    /// behind someone; a fight is left to it as well, the moment it has a target. What is
    /// written here is the deciding: which job is in force, whether this frame belongs to
    /// the job or to the game, and throwing away everything a job remembered when the
    /// order changes under it.
    ///
    /// The work itself is in <see cref="HirdmanWork"/>, one class per job. That split is
    /// what keeps this file the same length it was when there were four jobs instead of
    /// eleven.
    /// </summary>
    internal class HirdmanBrain : MonoBehaviour
    {
        private const float OrderPollInterval = 0.5f;

        private ZNetView _nview;
        private MonsterAI _ai;
        private HirdmanBody _body;
        private HirdmanBag _bag;

        private const string OrderRpc = "Hirdman_Order";
        private const string DismissRpc = "Hirdman_Dismiss";

        private HirdmanOrder _order;
        private HirdmanWork _work;
        private float _polledAt;
        private bool _carrying;
        private bool _rpc;
        private bool _wandering;

        private void Awake()
        {
            _nview = GetComponent<ZNetView>();
            _ai = GetComponent<MonsterAI>();
            _body = new HirdmanBody(gameObject);
            _bag = new HirdmanBag(gameObject);

            if (_nview != null && _nview.IsValid())
            {
                _order = HirdmanOrder.Read(_nview.GetZDO());
            }

            _work = HirdmanWork.For(_order.Job);
            Bind();
        }

        /// <summary>
        /// The owner is the only peer that may write the order. Everyone else sends it
        /// here and this method writes it down.
        /// </summary>
        private void Bind()
        {
            if (_rpc || _nview == null || !_nview.IsValid())
            {
                return;
            }

            _nview.Register<ZPackage>(OrderRpc, RPC_Order);
            _nview.Register(DismissRpc, RPC_Dismiss);
            _rpc = true;
        }

        private void RPC_Order(long sender, ZPackage package)
        {
            if (_nview == null || !_nview.IsValid() || !_nview.IsOwner() || package == null)
            {
                return;
            }

            var order = HirdmanOrder.Unpack(package);
            order.Write(_nview.GetZDO());
            Heard();
        }

        private void RPC_Dismiss(long sender)
        {
            if (_nview != null && _nview.IsValid() && _nview.IsOwner())
            {
                Leave();
            }
        }

        /// <summary>
        /// A last chance to write down what the retainer is holding, for the ordinary
        /// case where a player simply walks far enough away that the game unloads it.
        /// </summary>
        private void OnDestroy()
        {
            if (_bag != null)
            {
                _bag.Save();
            }
        }

        /// <summary>
        /// Gives a retainer an order, from any peer.
        ///
        /// Only the peer that owns a creature may write its ZDO. Taking ownership here
        /// used to be how the order travelled, the same way opening a chest does - but
        /// a walking person is not a chest, and yanking the simulation onto the speaker
        /// is what made them glitch and fall over the moment they were told to follow.
        /// The owner is asked instead, and writes it down itself.
        /// </summary>
        internal static bool Give(GameObject retainer, HirdmanOrder order)
        {
            var nview = retainer == null ? null : retainer.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            if (nview.IsOwner())
            {
                order.Write(nview.GetZDO());
                var brain = retainer.GetComponent<HirdmanBrain>();
                if (brain != null)
                {
                    brain.Heard();
                }

                return true;
            }

            nview.InvokeRPC(OrderRpc, order.Pack());
            return true;
        }

        /// <summary>
        /// The order changed; pick it up now rather than waiting for the next poll.
        /// Pressing Use to follow has to interrupt a swing, not queue politely behind it.
        /// Giving the same chest order again also has to start a fresh search, because a
        /// haul that has already put everything away will otherwise decide there is
        /// nothing to do.
        /// </summary>
        internal void Heard()
        {
            if (_nview == null || !_nview.IsValid())
            {
                return;
            }

            Apply(HirdmanOrder.Read(_nview.GetZDO()), restart: true);
        }

        /// <summary>Writes the bag and puts on whatever armour is in it.</summary>
        internal void Stow()
        {
            if (_bag != null)
            {
                _bag.Save();
            }

            if (_body != null)
            {
                _body.Wear();
            }
        }

        internal static HirdmanOrder Orders(GameObject retainer)
        {
            var nview = retainer == null ? null : retainer.GetComponent<ZNetView>();
            return nview != null && nview.IsValid()
                ? HirdmanOrder.Read(nview.GetZDO())
                : default(HirdmanOrder);
        }

        /// <summary>
        /// Decides whether the game's own AI should run this frame.
        /// </summary>
        /// <returns>
        /// True when this brain has taken the frame, and vanilla should be skipped.
        /// </returns>
        internal bool Think(float dt)
        {
            if (_nview == null || !_nview.IsValid() || _ai == null)
            {
                return false;
            }

            Bind();

            if (!_nview.IsOwner())
            {
                // Someone else is running this retainer now, and is free to empty its
                // arms without this peer hearing about it.
                _carrying = false;
                _bag.Forget();
                return false;
            }

            // Done here rather than on waking because the game hands a creature its
            // default items in its own start-up, which has not happened yet then, and
            // because taking over a retainer mid-task is how it arrives from a peer that
            // has been carrying things around on its behalf.
            if (!_carrying)
            {
                _carrying = true;
                _bag.Load();
                HirdmanLooks.Dress(gameObject);
                _body.Wear();
                PlayerPatch.Wake(GetComponent<Player>());
            }

            _bag.Keep();

            PollOrder();

            if (_order.Job == HirdmanJob.Dismissed)
            {
                Leave();
                return true;
            }

            // Something dropped at their feet is for them, whether they are working or
            // walking with you. Jobs also scoop what they themselves just made; this is
            // the case of an employer handing over an axe.
            if (_ai.GetTargetCreature() == null)
            {
                _body.Pocket(2.5f);
            }

            // A fight outranks any order except the one that went looking for it. Handing
            // the frame back is also how a retainer gets the game's own flinching,
            // circling and weapon choice for free.
            if (_ai.GetTargetCreature() != null && _order.Job != HirdmanJob.Hunt)
            {
                Wander(false);
                return false;
            }

            // Following and waiting are the game's own behaviour: a tamed creature that
            // is not following mills about a patrol point. Taking those frames would
            // leave them standing to attention, which is the opposite of waiting.
            if (_order.Job == HirdmanJob.Follow || _order.Job == HirdmanJob.Idle)
            {
                Wander(_order.Job == HirdmanJob.Idle);
                return false;
            }

            Wander(false);
            return _work.Run(_body, _order, dt);
        }

        /// <summary>
        /// Rereads the order, rather than being told when it changes. A retainer can
        /// change hands between peers mid-task, and the new owner has to pick up an
        /// order it never saw given.
        /// </summary>
        private void PollOrder()
        {
            if (Time.time - _polledAt < OrderPollInterval)
            {
                return;
            }

            var next = HirdmanOrder.Read(_nview.GetZDO());
            if (next.SameAs(_order))
            {
                _polledAt = Time.time;
                return;
            }

            Apply(next, restart: false);
        }

        private void Apply(HirdmanOrder next, bool restart)
        {
            var was = _order.Job;
            _order = next;
            _polledAt = Time.time;
            _wandering = false;

            if (restart || was != next.Job)
            {
                _work = HirdmanWork.For(next.Job);
            }

            _body.Abort();
            _ai.SetFollowTarget(next.Job == HirdmanJob.Follow ? Master() : null);
        }

        /// <summary>
        /// Waiting is vanilla milling around the spot they were told to wait, not
        /// standing on it. The dvergr AI already knows how to pick a reachable point;
        /// our own idle walks used to pick ones with no path and then treat that as
        /// having arrived, which looks like standing still.
        /// </summary>
        private void Wander(bool idle)
        {
            if (idle)
            {
                _ai.m_randomMoveRange = 8f;
                _ai.m_randomMoveInterval = 2.5f;
                if (!_wandering)
                {
                    _wandering = true;
                    _ai.SetPatrolPoint(_order.Anchor);
                }

                return;
            }

            if (_wandering)
            {
                _wandering = false;
                _ai.m_randomMoveRange = 0f;
                _ai.m_randomMoveInterval = 0f;
                _ai.ResetPatrolPoint();
            }
        }

        /// <summary>Sends a retainer out of service, from any peer.</summary>
        internal static bool Dismiss(GameObject retainer)
        {
            var nview = retainer == null ? null : retainer.GetComponent<ZNetView>();
            if (nview == null || !nview.IsValid())
            {
                return false;
            }

            if (nview.IsOwner())
            {
                var brain = retainer.GetComponent<HirdmanBrain>();
                if (brain != null)
                {
                    brain.Leave();
                }

                return true;
            }

            nview.InvokeRPC(DismissRpc);
            return true;
        }

        /// <summary>
        /// Drops what they were carrying so an axe is not deleted with them, then
        /// removes the creature. Only the owner may destroy the ZDO.
        /// </summary>
        private void Leave()
        {
            _body.EmptyPockets();
            if (ZNetScene.instance != null)
            {
                ZNetScene.instance.Destroy(gameObject);
            }
        }

        private GameObject Master()
        {
            return _order.Master == ZDOID.None || ZNetScene.instance == null
                ? null
                : ZNetScene.instance.FindInstance(_order.Master);
        }
    }
}
