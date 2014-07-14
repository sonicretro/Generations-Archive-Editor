using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Ar00Lib;

namespace Ar00Pack
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length == 0)
                args = new string[] { "/?" };
            switch (args[0].ToLowerInvariant())
            {
                case "/t":
                    Main(ParseCommandLine(File.ReadAllText(args[1])));
                    break;
                case "/u":
                    try
                    {
                        Ar00File ar = new Ar00File(args[1]);
                        string dest = Environment.CurrentDirectory;
                        if (args.Length > 2)
                        {
                            dest = args[2];
                            Directory.CreateDirectory(dest);
                        }
                        foreach (Ar00File.File item in ar.Files)
                            File.WriteAllBytes(Path.Combine(dest, item.Name), item.Data);
                    }
                    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                    break;
                case "/p":
                    string fn = args[args.Length - 1];
                    int f = 1;
                    int pad = 0x40;
                    if (args[1].Equals("/pad", StringComparison.OrdinalIgnoreCase))
                    {
                        f += 2;
                        pad = int.Parse(args[2], System.Globalization.NumberStyles.HexNumber);
                    }
                    try
                    {
                        Ar00File ar = new Ar00File();
                        ar.Padding = pad;
                        for (int i = f; i < args.Length - 1; i++)
                            ar.Files.Add(new Ar00File.File(args[i]));
                        ar.Save(fn);
                    }
                    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                    break;
                case "/l":
                    try
                    {
                        Ar00File.GenerateArlFile(args[1]);
                    }
                    catch (Exception ex) { Console.WriteLine(ex.ToString()); }
                    break;
                case "/?":
                default:
                    Console.WriteLine("Arguments:");
                    Console.WriteLine();
                    Console.WriteLine("/?\tShow this help.");
                    Console.WriteLine();
                    Console.WriteLine("/t filename\tReads text file filename as a commandline.");
                    Console.WriteLine();
                    Console.WriteLine("/u filename [destination]\tExtracts filename to destination, or the current directory.");
                    Console.WriteLine();
                    Console.WriteLine("/p [/pad num] filenames destination\tPacks files into the destination archive. /pad num pads the files to num bytes in hex.");
                    Console.WriteLine();
                    Console.WriteLine("/l filename\tGenerates a listing file for the archive.");
                    Console.WriteLine();
                    Console.WriteLine("filename\tOpens file in the visual editor.");
                    Console.WriteLine();
                    break;
            }
        }

        public static string[] ParseCommandLine(string commandLine)
        {
            List<string> args2 = new List<string>();
            string curcmd = string.Empty;
            bool quotes = false;
            foreach (char item in commandLine)
            {
                switch (item)
                {
                    case ' ':
                        if (!quotes)
                        {
                            if (!string.IsNullOrEmpty(curcmd))
                                args2.Add(curcmd);
                            curcmd = string.Empty;
                        }
                        else
                            goto default;
                        break;
                    case '"':
                        if (quotes)
                        {
                            args2.Add(curcmd);
                            curcmd = string.Empty;
                            quotes = false;
                        }
                        else
                            quotes = true;
                        break;
                    default:
                        curcmd += item;
                        break;
                }
            }
            if (!string.IsNullOrEmpty(curcmd))
                args2.Add(curcmd);
            return args2.ToArray();
        }
    }
}