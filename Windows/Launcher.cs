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

                // Patch server.js for CORS
                if (!string.IsNullOrEmpty(stremioExePath) && File.Exists(stremioExePath))
                {
                    PatchServerJsForCors(stremioExePath);
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

        static void PatchServerJsForCors(string stremioExePath)
        {
            try
            {
                // Get Stremio installation directory (root folder)
                string stremioDir = Path.GetDirectoryName(stremioExePath);
                
                // server.js is in the root of Stremio folder
                string serverJsPath = Path.Combine(stremioDir, "server.js");

                if (!File.Exists(serverJsPath))
                {
                    Console.WriteLine("[WARN] Could not find server.js at: " + serverJsPath);
                    return;
                }

                // Read the file
                string content = File.ReadAllText(serverJsPath);
                
                // Extract domain from TargetUrl (remove https://)
                string domain = TargetUrl.Replace("https://", "").Replace("http://", "");
                
                // Check if already patched
                if (content.Contains(domain))
                {
                    Console.WriteLine("[OK] server.js already patched for CORS: " + domain);
                    return;
                }

                // Find the CORS pattern - look for the specific line
                string corsPattern = "process.env.NO_CORS || !req.headers.origin || req.headers.origin.match(\".strem.io(:80)?$\") || req.headers.origin.match(\".stremio.net(:80)?$\") || req.headers.origin.match(\".stremio.com(:80)?$\") || req.headers.origin.match(\"stremio-development.netlify.app(:80)?$\") || req.headers.origin.match(\"stremio.github.io(:80)?$\") || req.headers.origin.match(\"gstatic.com\") || \"https://stremio.github.io\" === req.headers.origin || req.headers.origin.match(\"(127.0.0.1|localhost):11470$\") || req.headers.origin.match(\"peario.xyz\")";
                
                // Add the new origin check before the ternary operator
                string newCorsCheck = corsPattern + " || req.headers.origin.match(\"" + domain + "\")";
                
                // Replace in content
                if (content.Contains(corsPattern))
                {
                    content = content.Replace(corsPattern, newCorsCheck);
                    
                    // Make backup
                    string backupPath = serverJsPath + ".backup";
                    if (!File.Exists(backupPath))
                    {
                        File.Copy(serverJsPath, backupPath);
                        Console.WriteLine("[INFO] Created backup: " + backupPath);
                    }
                    
                    // Write patched content
                    File.WriteAllText(serverJsPath, content);
                    Console.WriteLine("[FIXED] server.js patched for CORS: " + domain);
                }
                else
                {
                    Console.WriteLine("[WARN] Could not find CORS pattern in server.js. File may have different format.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARN] Failed to patch server.js: " + ex.Message);
            }
        }
    }
}