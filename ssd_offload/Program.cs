﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ssd_offload
{
    class Program
    {
        private static Properties.Settings Settings = Properties.Settings.Default;

        public delegate void SubcommandMethod(string[] args);
        private static Dictionary<string, SubcommandMethod> subcommands = new Dictionary<string, SubcommandMethod>();

        static void Main(string[] args)
        {
            // Error if no arguments
            if (args.Length == 0)
                ExitWithError("No arguments given.  Type \"ssd_offload -help\" for help.");     // TODO: Implement the -help subcommand

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

            // Default behavior
            OffloadToHDD(args);
        }

        private static void ExitWithError(string error)
        {
            Console.WriteLine("ERROR: " + error);
            Environment.Exit(1);
        }

        private static string SSDToHDDPath(string ssdPath)
        {
            // Converts the given path on the SSD to the path on the HDD

            // Get the drive letter
            string driveLetter = GetDriveLetter(ssdPath);
            Console.WriteLine(driveLetter);

            return null;
        }

        private static string GetDriveLetter(string fullpath)
        {
            // Returns the drive letter of the path
            // It doesn't necessarily need to be a single letter

            StringBuilder builder = new StringBuilder();
            bool foundColon = false;

            foreach (char c in fullpath)
            {
                // Error if we hit a slash
                if (c == '\\')
                    throw new Exception("" + fullpath + " does not have a valid drive letter");

                // Stop when we hit a colon
                if (c == ':')
                {
                    foundColon = true;
                    break;
                }

                // Add it to the current drive letter
                builder.Append(c);
            }

            // Error if we never ran into a colon
            if (!foundColon)
                throw new Exception("Could not find a colon in " + fullpath);

            return builder.ToString();
        }


        #region subcommands

        private static void OffloadToHDD(string[] args)
        {
            // Moves the given folder to the offload directory, then replaces it with a symlink
            // Usage: ssd_offload <relative path>

            // Error if no arguments
            if (args.Length == 0)
                ExitWithError("No folder specified.  Which folder do you want moved to your HDD?");

            // Get the full path of the specified folder
            string fullPathOfTarget = Path.GetFullPath(args[0]);

            // Error if it's not a directory
            if (!Directory.Exists(fullPathOfTarget))
                ExitWithError("No such folder \"" + fullPathOfTarget + "\"");

            // TODO: Actually move it.
            string fullPathOnHDD = SSDToHDDPath(fullPathOfTarget);
            Console.WriteLine("Pretending to move \"" + fullPathOfTarget + "\" to \"" + fullPathOnHDD + "\"");
        }

        private static void SetOffloadDest(string[] args)
        {
            // Error if not enough arguments
            if (args.Length < 2)
                ExitWithError("Usage: ssd_offload -set_offload_dest <full path to the folder where we're keeping all of our offloaded files>");

            string dir = args[1];

            // Error if path doesn't exist
            if (!Directory.Exists(dir))
                ExitWithError("The directory " + dir + " does not exist.");

            // Change the settings
            Settings.OffloadDest = dir;
            Settings.Save();
            Console.WriteLine("Set offload destination to " + dir);
        }

        private static void GetOffloadDest(string[] args)
        {
            // Display the offload directory
            Console.WriteLine("Offload destination is " + Settings.OffloadDest);
        }

        #endregion
    }
}
