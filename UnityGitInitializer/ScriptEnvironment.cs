using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Miguel.Environment
{
    public sealed class EnvironmentVariables
    {
        internal static class SpecialCharacter
        {
            public static readonly string _comment = "===";
            public static readonly string _environment = "env:";
            public static readonly char _keysplitter = '@';
            public static readonly char _variable = '#';
            public static readonly char _attacher = '=';
            public static readonly string _whiteSpacePattern = @"^\s+$[\r\n]*";
        }

        Dictionary<string, string> environmentVariables;
        private static readonly EnvironmentVariables instance = new EnvironmentVariables();
        public static EnvironmentVariables Instance
        {
            get
            {
                return instance;
            }
        }

        static EnvironmentVariables()
        {
        }

        private EnvironmentVariables()
        {
            environmentVariables = new Dictionary<string, string>();
            Initialize();
        }

        public string this[string key]
        {
            get
            {
                string value;
                if (environmentVariables.TryGetValue(key, out value))
                {
                    return value;
                }
                else
                {
                    throw new System.Exception($"Environment Variable {key} does not exist!");
                }
            }

            set
            {
                if(environmentVariables.ContainsKey(key))
                {
                    environmentVariables[key] = value;
                    Console.WriteLine($"Overwriting {key} with {value}");
                }
            }
        }

        private void Initialize()
        {
            string[] content = ParseFile(@".\EnvironmentVariables.txt");
            foreach (string line in content)
            {
                Add(line);
            }
        }

        private string[] ParseFile(string file)
        {
            string content = FileManipulator.ReadFile(file);
            content = Regex.Replace(content, SpecialCharacter._whiteSpacePattern, string.Empty, RegexOptions.Multiline).Trim();
            string[] lines = content.Split(new[] { System.Environment.NewLine }, StringSplitOptions.None);
            return lines.Where(line => !line.StartsWith(SpecialCharacter._comment)).ToArray();
        }

        private void Add(string line)
        {
            string[] input = line.Split(SpecialCharacter._keysplitter);

            string key = input[0];
            string command = input[1];
            string value = "";

            if (command.Contains(SpecialCharacter._environment))
            {
                string env = line.Split(':').ElementAt(1);
                value = System.Environment.GetEnvironmentVariable(env);
            }
            else if (command.Contains(SpecialCharacter._attacher))
            {
                string[] args = command.Split(SpecialCharacter._attacher);

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].Contains(SpecialCharacter._variable))
                    {
                        args[i] = this[args[i].TrimStart(SpecialCharacter._variable)];
                    }
                    value = Path.Combine(value, args[i]);
                }
            }
            else
            {
                value = command;
            }

            environmentVariables.Add(key, value);
        }
    }
}
