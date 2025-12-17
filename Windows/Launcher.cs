using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace StremioNeoLauncher
{
    class Program
    {
        // GITHUB ACTIONS WILL REPLACE THIS
        const string TargetUrl = "REPLACE_ME_URL";

        static void Main(string[] args)
        {
            Console.Title = "Stremio Neo Launcher";
            Console.WriteLine("Checking Stremio shortcuts...");

            string[] paths = {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Stremio.lnk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Stremio.lnk"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\Start Menu\Programs\LNV\Stremio\Stremio.lnk")
            };

            string stremioExePath = null;
            bool patched = false;

            try 
            {
                // Use Late Binding (Reflection) to access WScript.Shell so we don't need external DLLs
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);

                foreach (string path in paths)
                {
                    if (File.Exists(path))
                    {
                        try 
                        {
                            dynamic shortcut = shell.CreateShortcut(path);
                            
                            // Capture where Stremio is actually installed
                            if (string.IsNullOrEmpty(stremioExePath))
                            {
                                stremioExePath = shortcut.TargetPath;
                            }

                            string arguments = shortcut.Arguments;

                            if (!arguments.Contains(TargetUrl))
                            {
                                if (string.IsNullOrWhiteSpace(arguments))
                                {
                                    shortcut.Arguments = "--webui-url=\"" + TargetUrl + "\"";
                                }
                                else
                                {
                                    shortcut.Arguments = arguments + " --webui-url=\"" + TargetUrl + "\"";
                                }
                                shortcut.Save();
                                Console.WriteLine("[FIXED] " + Path.GetFileName(path));
                                patched = true;
                            }
                            else
                            {
                                Console.WriteLine("[OK] " + Path.GetFileName(path));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("[SKIP] Could not read " + Path.GetFileName(path) + ": " + ex.Message);
                        }
                    }
                }

                // Launch Stremio
                if (!string.IsNullOrEmpty(stremioExePath) && File.Exists(stremioExePath))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("\nLaunching Stremio...");
                    Process.Start(stremioExePath, "--webui-url=\"" + TargetUrl + "\"");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] Could not find Stremio installation.");
                    Console.WriteLine("Please ensure Stremio is installed.");
                    Console.ReadLine(); // Pause so user can see error
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Critical Error: " + ex.Message);
                Console.ReadLine();
            }
        }
    }
}