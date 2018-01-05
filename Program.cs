using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace logRunner
{
    class MainClass
    {
        static string root = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        static string sl = Path.DirectorySeparatorChar.ToString();
        
        
        public static class profile
        {
            public static string name;
            public static int index;
        }
        
        public class _nutrient
        {
            public string field;
            public string rda;
            public string consumed;
        }
       
        
        static Dictionary<string, string[]> usdaPairs;
        static string usdaroot;
        static List<_nutrient> nutrients = new List<_nutrient>();
        static List<string> dates = new List<string>();
        static string[] activeFieldsLines;
        static string[] usdaNutKeyLines;
        static string outputLogFile;

        public static void Main(string[] args)
        {
            //printp("10 mg", "15 mg");
            //.ReadKey();
            println("...fetching global keys...", ConsoleColor.DarkCyan);

            if (args.Length == 0)
                args = File.ReadAllLines($"{root}{sl}args.TXT");

            //parses the arguments
            profile.index = Convert.ToInt32(args[0]);
            outputLogFile = args[1];
            usdaroot = $"{root}{sl}usr{sl}share{sl}DBs{sl}USDAstock{sl}";
            usdaNutKeyLines = File.ReadAllLines($"{usdaroot}_nutKeyPairs.TXT");
            root = $"{root}{sl}usr{sl}profile{profile.index}{sl}";

            string[] profData = File.ReadAllLines($"{root}profile.TXT");
            activeFieldsLines = File.ReadAllLines($"{root}activeFields.TXT");
            profile.name = profData[0];
            println(profile.name, ConsoleColor.Green);

            //grabs the active nutrients
            foreach (string s in activeFieldsLines)
            {
                string leading = s.Split('#')[0].Trim();
                if (leading != "") // && frmMain.activeFields.Contains(leading))
                {
                    _nutrient n = new _nutrient();
                    n.field = leading.Split('|')[0];
                    n.rda = leading.Split('|')[1];
                    nutrients.Add(n);
                }
            }

            println("...reading in USDAstock...", ConsoleColor.DarkCyan);
            //compares against usda fields
            List<string> fields = new List<string>();
            for (int i = 0; i < nutrients.Count; i++)
                foreach (string s in usdaNutKeyLines)
                    if (s.Split('|')[1] == nutrients[i].field)                    
                        fields.Add(nutrients[i].field);

            //reads in data from main database
            usdaPairs = new Dictionary<string, string[]>();
            foreach (string s in fields)
                foreach (string st in usdaNutKeyLines)
                    if (st.Split('|')[1] == s)
                    {
                        string[] lines = File.ReadAllLines($"{usdaroot}{st.Split('|')[0]}");
                        usdaPairs.Add(s, lines);                  
                    }             

            //prints the results            
            println();
            for (int i = 2; i < args.Length; i++)
            {
                dates.Add(args[i]);
                printLog(args[i], nutrients);
            }

            println("press any key to exit...");
            Console.ReadKey();
        }

        private static void printLog(string date, List<_nutrient> nuts)
        {
            println("==========", ConsoleColor.DarkCyan);
            println(date, ConsoleColor.DarkCyan);
            println("==========", ConsoleColor.DarkCyan);
            Dictionary<string, double> cons = new Dictionary<string, double>();
            foreach (_nutrient n in nuts)
                cons.Add(n.field, 0.0);
            string[] foodDayLines = File.ReadAllLines($"{root}foodlog{sl}{date}.TXT");
            foreach (string s in foodDayLines)
                if (s.StartsWith("USDAstock"))
                    ;
                    
            println(string.Join("\n", foodDayLines));
            //???
            println();
        }

        private static double printp(string consumed, string rda)
        {
            int c = 0;
            int r = 1;
            try
            {
                r = Convert.ToInt32(rda.Split(' ')[0]);
                c = Convert.ToInt32(consumed.Split(' ')[0]);
            }
            catch (Exception e)
            {
                printE(e);
                return 0;
            }
            double x = (double)c / (double)r;
            ConsoleColor color = ConsoleColor.Green;
            if (x < 0.7)
                color = ConsoleColor.DarkYellow;
            else if (x < 0.5)
                color = ConsoleColor.DarkRed;

            string prog = "==================================================";
            int eL = prog.Length;
            eL = Convert.ToInt32(eL * x);
            int spac = prog.Length - eL;
            prog = "<";
            for (int i = 0; i < eL; i++)
                prog += "=";
            for (int i = 0; i < spac; i++)
                prog += " ";
            prog += $"> {100 * Math.Round((double)x, 2)}%";
            println(prog, color);
            return x;
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

        public static void printE(Exception ex)
        {
            println($" ===============\n  --Exception--\n ===============", ConsoleColor.DarkRed);
            println($"{DateTime.Now.ToString()}\n{ ex.Source}, { ex.HResult}\n{ ex.Data}");
            println($"{ ex.Message}", ConsoleColor.DarkRed);
            println($"\n\n{ ex.TargetSite}\n{ ex.StackTrace}");
            println(" ===================\n  --End exception--\n ===================", ConsoleColor.DarkRed);
        }
    }
}
