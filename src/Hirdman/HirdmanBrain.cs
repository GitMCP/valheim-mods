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

        private HirdmanOrder _order;
        private HirdmanWork _work;
        private float _polledAt;
        private bool _carrying;
        private bool _rpc;

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

        /// <summary>The order changed; pick it up on the next think rather than waiting.</summary>
        internal void Heard()
        {
            _polledAt = 0f;
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
                return false;
            }

            // Following is the game's own behaviour, driven by a field rather than by a
            // job, so there is nothing to run and nothing to take the frame for.
            return _order.Job != HirdmanJob.Follow && _work.Run(_body, _order, dt);
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

            _polledAt = Time.time;

            var next = HirdmanOrder.Read(_nview.GetZDO());
            if (next.SameAs(_order))
            {
                return;
            }

            var was = _order.Job;
            _order = next;

            // A job remembers the tree it chose and how long it has been swinging at it.
            // None of that survives being told to do something else.
            if (was != next.Job)
            {
                _work = HirdmanWork.For(next.Job);
            }

            // Vanilla following is driven by this one field, so the order is expressed by
            // setting it and then staying out of the way.
            _ai.SetFollowTarget(next.Job == HirdmanJob.Follow ? Master() : null);
        }

        private GameObject Master()
        {
            return _order.Master == ZDOID.None || ZNetScene.instance == null
                ? null
                : ZNetScene.instance.FindInstance(_order.Master);
        }
    }
}
