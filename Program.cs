using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

namespace logRunner
{
    class Program
    {
        #region pub decs
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
            public string fileName;
            //public string[] entries;
            public string rda;
            public string consumed;
            //public double intake;
            public string unit;
        }

        public class _foodObj{
            public string ndbno;
            public int index;
            public double grams;
        }
        


        static Dictionary<string, string[]> usdaFileNameLinePairs;
        static string usdaroot;
        static List<string> fields;

        static string[] activeFieldsLines;
        static string[] usdaNutKeyLines;
        static string[] ndbnos;

        static List<_nutrient> nutrients = new List<_nutrient>();
        static List<string> dates = new List<string>();

        static string outputLogFile;
        #endregion

        public static void Main(string[] args)
        {
            _nutrient np = new _nutrient();
            np.rda = "17 mg";
            np.consumed = "21 mg";
            printp(np);
            np.rda = "17 mg";
            np.consumed = "16 mg";
            printp(np);
            np.rda = "17 mg";
            np.consumed = "11 mg";
            printp(np);
            np.rda = "27 mg";
            np.consumed = "11 mg";
            printp(np);
            //Console.ReadKey();

            //updates user
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

            foreach (string s in usdaNutKeyLines)
            {
                if (s.Split('|')[1] == "NDBNo")
                    ndbnos = File.ReadAllLines($"{usdaroot}{s.Split('|')[0]}");
                foreach (_nutrient n in nutrients)
                    if (n.field == s.Split('|')[1])
                        n.fileName = s.Split('|')[0];
            }
            println("...reading in USDAstock...", ConsoleColor.DarkCyan);

            //compares against usda fields
            fields = new List<string>();
            for (int i = 0; i < nutrients.Count; i++)
                foreach (string s in usdaNutKeyLines)
                    if (s.Split('|')[1] == nutrients[i].field)
                        fields.Add(nutrients[i].field);
            
            //grabs only active fields
            List<_nutrient> newNutrients = new List<_nutrient>();
            foreach (_nutrient n in nutrients)
                if (fields.Contains(n.field))
                    newNutrients.Add(n);
            nutrients = newNutrients;

            //reads in data from main database
            usdaFileNameLinePairs = new Dictionary<string, string[]>();
            foreach (string s in fields)
                foreach (string st in usdaNutKeyLines)
                    if (st.Split('|')[1] == s)
                    {
                        string[] lines = File.ReadAllLines($"{usdaroot}{st.Split('|')[0]}");
                        usdaFileNameLinePairs.Add(st.Split('|')[0], lines);
                    }
            println();

            //reads in the user foodlog and computes results
            for (int i = 2; i < args.Length; i++)
            {
                dates.Add(args[i]);
                //prints the results  
                printLog("9-20-2017", nutrients);
                //printLog(args[i], nutrients); //uncomment this
            }
          
            println("press any key to exit...");
            Console.ReadKey();
        }

        #region printp and printLog
        private static void printLog(string date, List<_nutrient> nuts)
        {
            //breaks up by date
            println("==========", ConsoleColor.DarkCyan);
            println(date, ConsoleColor.DarkCyan);
            println("==========", ConsoleColor.DarkCyan);

            //where is this used?
            // Dictionary<string, double> cons = new Dictionary<string, double>();
            // foreach (_nutrient n in nuts)
            //     cons.Add(n.field, 0.0);

            string[] foodDayLines = File.ReadAllLines($"{root}foodlog{sl}{date}.TXT");

            //preps the calculation
            List<_foodObj> todaysFood = new List<_foodObj>();
            foreach (string s in foodDayLines)
                if (s.StartsWith("USDAstock"))
                {
                    _foodObj f = new _foodObj();
                    f.ndbno = s.Split('|')[1];
                    //string fileName = "";
                    //foreach (string st in usdaNutKeyLines)
                    //    if (st.Split('|')[1] == "NDBNo")
                    //        fileName = st.Split('|')[0];
                    //string[] lines = usdaFileNameLinePairs[fileName]; //DOESN'T CONTAIN NDB
                    for (int i = 0; i < lines.Length; i++)
                        if (lines[i] == f.ndbno)
                        {
                            f.index = i;
                            break;
                        }

                    try { f.grams = Convert.ToDouble(s.Split('|')[1]); }
                    catch (Exception e)
                    {
                        f.grams = 0;
                        printE(e);
                    }
                    todaysFood.Add(f);
                }

            //performs piecemeal addition
            foreach (_nutrient n in nuts)
            {
                string[] nutValLines = usdaFileNameLinePairs[n.field];
                foreach (_foodObj f in todaysFood)
                {
                    n.consumed +=0;

                }

            }
            //println(string.Join("\n", foodDayLines));
            //???
            println();
        }

        private static double printp(_nutrient nut)//(string consumed, string rda)
        {
            double c = 0;
            double r = 1;
            try
            {
                c = Convert.ToDouble(nut.consumed.Split(' ')[0]);
                r = Convert.ToDouble(nut.rda.Split(' ')[0]);
            }
            catch (Exception e)
            {
                printE(e);
                return 0;
            }
            double x = c / r;
            ConsoleColor color = ConsoleColor.Blue;
            if (x > 1.0)
            {
                x = 1.0;
                color = ConsoleColor.DarkMagenta;
            }
            else if (x < 0.7 && x > 0.5)
                color = ConsoleColor.Yellow;
            else if (x <= 0.5)
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
            prog += $"> {100 * Math.Round(c / r, 3)}%";
            println(prog, color);
            return x;
        }
        #endregion

        #region main print functions
        static List<string> outputLog = new List<string>();
        private static void println(string s, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(s);
            Console.ForegroundColor = ConsoleColor.White;
            outputLog.Add(s);
        }

        private static void println() => println("");

        private static void print(string s, ConsoleColor color = ConsoleColor.White)
        {
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
        #endregion
    }
}
