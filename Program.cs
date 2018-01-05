using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace logRunner
{
    class MainClass
    {
        static string root = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        static string slash = Path.DirectorySeparatorChar.ToString();
        
        
        public static class profile
        {
            public static string name;
            public static int index;
        }
        
        public class _nutrient
        {
            public string field;
            public string unit;
        }
        
        
        static List<_nutrient> nutrients = new List<_nutrient>();
        static List<string> dates = new List<string>();
        static string[] activeFieldsLines;
        static List<string> activeFields = new List<string>();
        static string outputLogFile;
        
        public static void Main(string[] args)
        {
            Console.WriteLine("...logger starting...");
            if (args.Length == 0)
                args = File.ReadAllLines($"{root}{slash}args.TXT");
                        
            profile.index = Convert.ToInt32(args[0]);
            outputLogFile = args[1];
			root = $"{root}{slash}usr{slash}profile{profile.index}{slash}";
            
            string[] profData = File.ReadAllLines($"{root}profile.TXT");            
            activeFieldsLines = File.ReadAllLines($"{root}activeFields.TXT");
            profile.name = profData[0];
            println(profile.name, ConsoleColor.Green);
            
            //
            foreach (string s in activeFieldsLines){
            //deal with mid-line comments
                string leading = s.Split('#')[0];
                if (leading != "") // && frmMain.activeFields.Contains(leading))
                {
                    _nutrient n = new _nutrient();
                    n.field = leading.Split('|')[0];
                    n.unit = leading.Split('|')[1];
                    nutrients.Add(n);
                }
                    //activeFields.Add(leading);
            }
            //
            
            println();
            for (int i = 2; i < args.Length; i++)
            {
                dates.Add(args[i]);
                printLog(args[i], nutrients);
            }
            
            println("press any key to exit...");
            Console.ReadKey();
        }

        private static void printLog(string date, List<_nutrient> nutrients)
        {
            println("==========", ConsoleColor.DarkCyan);
            println(date, ConsoleColor.DarkCyan);
            println("==========", ConsoleColor.DarkCyan);
            for (int i = 0; i < nutrients.Count; i++)     
            {
                println($"{nutrients[i].field}", ConsoleColor.Green);
                //println($" (RDA= '{nutrients[i].unit}')");
            
            }       
			println();
        }
        
        static List<string> outputLog = new List<string>();
        
        private static void println(string s, ConsoleColor color = ConsoleColor.White){
            Console.ForegroundColor = color;
            Console.WriteLine(s);
            Console.ForegroundColor = ConsoleColor.White;
            outputLog.Add(s);
        }

        private static void println() => println("");
        
        private static void print(string s, ConsoleColor color = ConsoleColor.White){
            Console.ForegroundColor = color;
            Console.Write(s);
            Console.ForegroundColor = ConsoleColor.White;
            outputLog[outputLog.Count - 1] += s;
        }
    }
}
