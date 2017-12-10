using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace ssd_offload
{
    class Program
    {
        private static Properties.Settings Settings = Properties.Settings.Default;

        public delegate void SubcommandMethod(string[] args);
        private static Dictionary<string, SubcommandMethod> subcommands = new Dictionary<string, SubcommandMethod>();

        // Pilfered from StackOverflow: https://stackoverflow.com/questions/11156754/what-the-c-sharp-equivalent-of-mklink-j
        #region importing stuff for CreateSymbolicLink
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(
        string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }
        #endregion


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

        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // Recursively copies the given directory and all of its subdirectories.
            // Pilfered and tweaked from https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
            // WHY is this not already included in the System.IO namespace???

            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            foreach (DirectoryInfo subdir in dirs)
            {
                string temppath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, temppath);
            }
        }

        private static string SSDToHDDPath(string ssdPath)
        {
            // Converts the given path on the SSD to the path on the HDD

            // Separate the drive letter from the actual path
            int driveLetterEnd = GetDriveLetterEnd(ssdPath);
            string driveLetter = ssdPath.Substring(0, driveLetterEnd);
            string pathWithoutLetter = ssdPath.Substring(driveLetterEnd + 1);

            // Construct the new path
            return Settings.OffloadDest + "\\" + driveLetter + pathWithoutLetter;
        }

        private static int GetDriveLetterEnd(string fullpath)
        {
            // Returns the index of the colon after the drive letter.
            // It doesn't necessarily need to be a single letter

            for (int i = 0; i < fullpath.Length; i++)
            {
                char c = fullpath[i];

                // Error if we hit a slash
                if (c == '\\')
                    throw new Exception("" + fullpath + " does not have a valid drive letter");

                // Stop when we hit a colon
                if (c == ':')
                    return i;
            }

            // Error if we never ran into a colon
            throw new Exception("Could not find a colon in " + fullpath);
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

            // Get the path that we're offloading the folder to.
            string fullPathOnHDD = SSDToHDDPath(fullPathOfTarget);

            // Error if there is already a folder or file there
            if (Directory.Exists(fullPathOnHDD) || File.Exists(fullPathOnHDD))
                ExitWithError("There is already a file or folder at the path \"" + fullPathOnHDD + "\".  Aborting.");

            // Copy the folder to the destination
            Console.WriteLine("Copying...");
            DirectoryCopy(fullPathOfTarget, fullPathOnHDD);

            // Delete the original
            Console.WriteLine("Deleting original...");
            Directory.Delete(fullPathOfTarget, true);

            // Create a symlink in place of the original
            Console.WriteLine("Create symbolic link...");
            CreateSymbolicLink(fullPathOfTarget, fullPathOnHDD, SymbolicLink.Directory);
        }

        private static void SetOffloadDest(string[] args)
        {
            // Error if not enough arguments
            if (args.Length < 2)
                ExitWithError("Usage: ssd_offload -set_offload_dest <full path to the folder where we're keeping all of our offloaded files>");

            string dir = args[1].TrimEnd('\\');

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
            Console.WriteLine(Settings.OffloadDest);
        }

        #endregion
    }
}
