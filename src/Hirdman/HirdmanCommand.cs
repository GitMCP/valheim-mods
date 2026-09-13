using Jotunn.Entities;

namespace Hirdman
{
    /// <summary>
    /// Orders typed as a console command.
    ///
    /// This exists ahead of any window of its own because it proves the part that
    /// matters: a whole sentence goes in one end, and a retainer changes what it is doing
    /// at the other. A chat window and a language model both replace only the first half
    /// of that, and neither can be tested without a screen, whereas this can.
    /// </summary>
    internal class HirdmanCommand : ConsoleCommand
    {
        public override string Name => "hird";

        public override string Help =>
            "hird <order> - tell the nearest retainer what to do, in your own words";

        public override void Run(string[] args)
        {
            var player = Player.m_localPlayer;
            if (player == null)
            {
                Console.instance?.Print("There is nobody here to give orders.");
                return;
            }

            if (args == null || args.Length == 0)
            {
                Console.instance?.Print(Help);
                return;
            }

            var retainer = HirdmanRoster.Nearest(player.transform.position, HirdmanRoster.EarshotRadius);
            if (retainer == null)
            {
                Console.instance?.Print("No retainer within earshot.");
                return;
            }

            var sentence = string.Join(" ", args);
            HirdmanOrder order;
            string reply;
            if (!HirdmanParser.TryParse(sentence, player, retainer, out order, out reply))
            {
                HirdmanSpeech.Say(retainer, reply);
                Console.instance?.Print($"The retainer did not understand '{sentence}'.");
                return;
            }

            if (!HirdmanBrain.Give(retainer, order))
            {
                Console.instance?.Print("That retainer cannot be given orders right now.");
                return;
            }

            HirdmanSpeech.Say(retainer, reply);
            Console.instance?.Print($"Retainer: {order.Job}.");
        }
    }
}
