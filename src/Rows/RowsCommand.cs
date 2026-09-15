using Jotunn.Entities;

namespace Rows
{
    /// <summary>
    /// Temporary. Pretend extra people are on the benches so the speed can be
    /// tried from a single client. No models. Remove this when the numbers are
    /// settled.
    /// </summary>
    internal class RowsCommand : ConsoleCommand
    {
        public override string Name
        {
            get { return "rows"; }
        }

        public override string Help
        {
            get { return "rows [n] — pretend n extra people are rowing (temporary)."; }
        }

        public override void Run(string[] args, Terminal context)
        {
            if (args == null || args.Length == 0)
            {
                Write(context, "Dummy rowers: " + RowsCrew.Dummies + ". Usage: rows <count>");
                return;
            }

            int count;
            if (!int.TryParse(args[0], out count) || count < 0)
            {
                Write(context, "Usage: rows <count>");
                return;
            }

            RowsCrew.Dummies = count;
            Write(context, "Dummy rowers set to " + count + ".");
            RowsPlugin.Log.LogInfo("Dummy rowers set to " + count + ".");
        }

        private static void Write(Terminal context, string line)
        {
            if (context != null)
            {
                context.AddString(line);
                return;
            }

            if (Console.instance != null)
            {
                Console.instance.Print(line);
            }
        }
    }
}
