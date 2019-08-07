﻿using Microsoft.XmlDiffPatch;

namespace Tokenizer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using System.Xml.XPath;

    internal class Tokenizer
    {
        private readonly string _templateFile;
        private readonly string _tokenFile;
        private readonly string _outputFile;

        public Tokenizer(string templateFile, string tokenFile, string outputFile)
        {
            _templateFile = templateFile;
            _tokenFile = tokenFile;
            _outputFile = outputFile;
        }

        public int Run()
        {
            Console.WriteLine("Starting tokenization...");

            var templateFile = _templateFile;
            var tokenFile = _tokenFile;
            var outputFile = _outputFile;

            if (!File.Exists(templateFile))
            {
                Console.WriteLine($"Error, could not find template file '{templateFile}', exiting...");
                Environment.Exit(1);
            }
            
            if (!File.Exists(tokenFile))
            {
                Console.WriteLine($"Error, could not find token file '{tokenFile}', exiting...");
                Environment.Exit(1);
            }

            // TODO
            // add -force option and checking for last change date to skip re-generation of files without need for it

            // This does not work well for CI case 
            /*
            Console.WriteLine("Checking if files are up to date...");
            var templateFileLastChangeDate = File.GetLastWriteTimeUtc(_templateFile);
            var tokenFileLastChangeDate = File.GetLastWriteTimeUtc(_tokenFile);
            var outputFileLastChangeDate = File.GetLastWriteTimeUtc(_outputFile);
            var lastVersionFileLastChangeDate = File.GetLastWriteTimeUtc(CreateLastVersionFileName(_outputFile));

            if (lastVersionFileLastChangeDate >= outputFileLastChangeDate &&
                outputFileLastChangeDate >= templateFileLastChangeDate &&
                outputFileLastChangeDate >= tokenFileLastChangeDate)
            {
                Console.WriteLine("Seems that both token and template files are older than output file which is older then last version file, skipping tokenization.");
                return 0;
            }
            */

            if (File.Exists(outputFile))
            {
                Console.WriteLine("Output file exists, checking for untemplated changes...");
                var untemplatedChanges = CheckOutputFileForUntemplatedChanges(outputFile);
                if (untemplatedChanges)
                {
                    Console.WriteLine("Trying to open diff...");
                    TryToOpenDiffFor(outputFile);

                    return -1;
                }
            }

            Console.WriteLine("Tokenizing template file '{0}' using tokens from '{1}'...", templateFile, tokenFile);

            // load file
            var text = File.ReadAllText(templateFile);

            // load tokens
            var doc = XDocument.Load(tokenFile);

            // replace tokens
            foreach (var token in doc.XPathSelectElements("/tokens/token"))
            {
                var name = token.Element("name").Value;
                var value = token.Element("value").Value;
                var tokenPattern = "${" + name + "}";

                Console.WriteLine("{0} = {1}", name, value);

                // warn about not used tokens
                if (text.IndexOf(tokenPattern) < 0)
                {
                    if (Config.WarnAboutTokensNotFoundInFile)
                    {
                        // TODO yellow
                        Console.WriteLine("WARNING: Token {0} not found in file.", tokenPattern);
                    }

                    continue;
                }

                text = text.Replace(tokenPattern, value);
            }

            // search for missing tokens
            var matches = Regex.Matches(text, @"\$\{.+\}");
            if (matches.Count > 0)
            {
                // TODO red
                Console.Write("ERROR: Missing token(s):");
                var missingTokens = new List<string>();
                for (int i = 0; i < matches.Count; i++)
                    missingTokens.Add(matches[i].Value);
                missingTokens
                    .Distinct()
                    .ToList()
                    .ForEach(t => Console.Write(" " + t));

                Console.WriteLine();

                // exit with error
                return -1;
            }

            text = text
                .Replace("$GeneratedOn$", string.Format("$ Generated on: {0} $", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")))
                .Replace("$GeneratedBy$", string.Format("$ Generated by: {0} on {1} $", Environment.UserName, Environment.MachineName))
                .Replace("$GeneratedFromTemplateFile$", string.Format("$ Generated from template file: {0} $", templateFile))
                .Replace("$GeneratedUsingTokenFile$", string.Format("$ Generated using token file: {0} $", tokenFile));

            // save file
            Console.WriteLine("Saving result as file '{0}'...", outputFile);
            File.WriteAllText(outputFile, text);

            var lastVersion = CreateLastVersionFileName(outputFile);
            Console.WriteLine("Saving last version copy as file '{0}'...", lastVersion);
            File.WriteAllText(lastVersion, text);

            Console.WriteLine("Tokenization completed.");
            return 0;
        }

        public bool CheckOutputFileForUntemplatedChanges(string outputFile)
        {
            var lastVersion = CreateLastVersionFileName(outputFile);

            if (File.Exists(lastVersion))
            {
                Console.WriteLine("Comparing '{0}' to '{1}'...", lastVersion, outputFile);

                var untemplatedChanges = !CompareFiles(lastVersion, outputFile);
                if (!untemplatedChanges)
                {
                    Console.WriteLine("No untemplated changes detected - Last version file is identical to generated file.");
                }
                else
                {
                    Console.WriteLine("ERROR: Untemplated changes detected in '{0}' - Last version file is NOT identical to generated file. Please update your template file.", outputFile);
                }

                return untemplatedChanges;
            }

            Console.WriteLine("Last version file not found, skipping check assuming no untemplated changes.");
            return false;
        }

        private string CreateLastVersionFileName(string file)
        {
            return file + ".lastversion";
        }

        // TODO imporove the performance of this method some day
        // cause this reads the whole file which is not the fastest solution
        // but at this point there's no use case for big files
        /// <summary>
        /// Method compars 2 xml files, using old xml diff implementation.
        /// </summary>
        /// <param name="firstFile">Last version file.</param>
        /// <param name="secondFile">Generated file.</param>
        /// <returns>True if files are identical, false otherwise.</returns>
        private bool CompareFiles(string firstFile, string secondFile)
        {
            XmlDiff xmlDiff = new XmlDiff(XmlDiffOptions.IgnoreChildOrder
                                          | XmlDiffOptions.IgnoreNamespaces
                                          | XmlDiffOptions.IgnorePrefixes);
            return xmlDiff.Compare(firstFile, secondFile, false);
        }

        private void TryToOpenDiffFor(string outputFile)
        {
            const string DefaultDiffTool = @"C:\Program Files (x86)\WinMerge\WinMergeU.exe";
            const string DefaultDiffToolCallPattern = "\"{0}\" \"{1}\"";

            var diffTool = Environment.GetEnvironmentVariable("DIFFTOOL") ?? DefaultDiffTool;
            if (!File.Exists(diffTool))
            {
                Console.WriteLine("Diff tool '{0}' not found.", diffTool);
                Console.WriteLine("Either makre you have WinMerge available in the default location: {0} or set a custom diff tool using DIFFTOOL environment variable.", DefaultDiffTool);
                Console.WriteLine("The default call pattern for diff tool is '{0}', but you can provide custom call pattern using DIFFTOOLCALLPATTERN environment variable which is passed to string.Format().",
                    DefaultDiffToolCallPattern);
            }

            var diffToolCallPattern = Environment.GetEnvironmentVariable("DIFFTOOLCALLPATTERN") ?? DefaultDiffToolCallPattern;
            var lastVersion = CreateLastVersionFileName(outputFile);
            var diffToolCall = string.Format(diffToolCallPattern, lastVersion, outputFile);

            Console.WriteLine(diffTool);
            Console.WriteLine(diffToolCall);

            Process.Start(diffTool, diffToolCall);
        }
    }
}