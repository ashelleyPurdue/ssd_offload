using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssd_offload
{
    class Program
    {
        private static Properties.Settings Settings = Properties.Settings.Default;

        public delegate void SubcommandMethod(string[] args);
        private static Dictionary<string, SubcommandMethod> subcommands = new Dictionary<string, SubcommandMethod>();

        static void Main(string[] args)
        {
            // TODO: Error if no arguments

            // Populate the subcommand table
            subcommands.Add("-set_offload_dest", SetOffloadDest);
            subcommands.Add("-get_offload_dest", GetOffloadDest);

            // If the first argument is a subcommand, go to its method instead.
            // Subcommands start with a "-".
            if (args[0][0] == '-')
            {
                // Error if subcommand doesn't exist
                if (!subcommands.ContainsKey(args[0]))
                    ExitWithError("subcommand \"" + args[0] + "\" not recognized.");

                // Execute it
                subcommands[args[0]](args);
                return;
            }

            // TODO: Default behavior
            Console.WriteLine("No subcommands");
        }

        private static void ExitWithError(string error)
        {
            Console.WriteLine(error);
            Environment.Exit(1);
        }

        private static void SetOffloadDest(string[] args)
        {
            // Error if not enough arguments
            if (args.Length < 2)
                ExitWithError("Usage: ssd_offload -set_offload_dest <full path to the folder where we're keeping all of our offloaded files>");

            // TODO: Error if path doesn't exist
            // Change the settings
            Settings.OffloadDest = args[1];
            Settings.Save();
            Console.WriteLine("Set offload destination to " + args[1]);
        }

        private static void GetOffloadDest(string[] args)
        {
            // Display the offload directory
            Console.WriteLine("Offload destination is " + Settings.OffloadDest);
        }
    }
}
