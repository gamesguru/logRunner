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
            public string rda;
            public double consumed = 0;
            public string unit;
			public string contrib;
        }

        public class _foodObj{
            public string ndbno;
            public string name;
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
            println(profile.name, ConsoleColor.Green);

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
                //dates.Add(args[i]);

                //prints the results  
                try { printLog(args[i], nutrients); }
                catch (Exception e)
                {
                    printE(e);
                    continue;
                }
                foreach (_nutrient n in nutrients)
                    n.consumed = 0;
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
            string nameFile = "";
            foreach (string s in usdaNutKeyLines)
                if (s.Split('|')[1] == "FoodName")
                    nameFile = s.Split('|')[0];
            List<_foodObj> todaysFood = new List<_foodObj>();
            foreach (string s in foodDayLines)
                if (s.StartsWith("USDAstock"))
                {
                    _foodObj f = new _foodObj();
                    f.ndbno = s.Split('|')[1];
                    for (int i = 0; i < ndbnos.Length; i++)
                        if (ndbnos[i] == f.ndbno)
                        {
                            f.index = i;
                            f.name = File.ReadAllLines($"{usdaroot}{nameFile}")[i];
                            break;
                        }

                    try { f.grams = Convert.ToDouble(s.Split('|')[2]); }
                    catch (Exception e)
                    {
                        f.grams = 0;
                        printE(e);
                    }
                    println($"{f.name} ({f.grams} g)");
                    todaysFood.Add(f);
                }

            //performs piecemeal addition
            foreach (_nutrient n in nuts)
            {
                string[] unitLines = File.ReadAllLines($"{usdaroot}_unitKeyPairs.TXT");
                foreach (string s in unitLines)
                    if (s.StartsWith(n.fileName))
                    {
                        n.unit = s.Split('|')[1];
                        break;
                    }
                //n.entries = File.ReadAllLines($"{usdaroot}{n.fileName}");
                string[] nutValLines = usdaFileNameLinePairs[n.fileName];

                //performs the addition
                foreach (_foodObj f in todaysFood)
                {
                    n.consumed += Convert.ToDouble(nutValLines[f.index]) * f.grams * 0.01;
                    n.contrib += $"{Math.Round(n.consumed, 3)}, ";
                }
                //try { n.consumed += Convert.ToInt32(nutValLines[f.index]); }
                //catch (Exception ex) { printE(ex); }
                //prints results
                printp(n);
            }
            println();
        }

        private static double printp(_nutrient nut)//(string consumed, string rda)
        {
            double c = 0;
            double r = 1;
            //c = nut.consumed;
            //r = Convert.ToDouble(nut.rda.Split(' ')[0]);
            try
            {
                c = nut.consumed;
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
            prog += $"> {100 * Math.Round(c / r, 3)}% \t[{Math.Round(c, 4)}/{nut.rda.Split(' ')[0]} {nut.unit} -- {nut.field}] {{{nut.contrib}}}";
            println(prog, color);
            //println();
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
