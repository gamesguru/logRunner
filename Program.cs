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
        static string rootSpare = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        static string sl = Path.DirectorySeparatorChar.ToString();

        static bool printDetail = false;
        static bool saveLog = false;
        static string[] profData;
        
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
            //public List<int> indices;
            public double grams;
        }   
        
        public class _relDB
        {
            public string path;
            public string[] ndbLines;
            public string[] valLines;
            public string[] nutLines;
            //key-name dictionary? performance issues?
        }

        static Dictionary<string, string[]> usdaFileNameLinePairs;
        static string usdaroot;
        static List<string> fields;
        static List<_relDB> relDBs = new List<_relDB>();

        static string[] activeFieldsLines;
        static string[] usdaNutKeyLines;
        static string[] ndbnos;

        static List<_nutrient> nutrients = new List<_nutrient>();

        static string outputLogFile;
        #endregion

        public static void Main(string[] args)
        {

            string[] sets = File.ReadAllLines($"{root}{sl}lsettings.ini");
            foreach (string s in sets)
                if (s.StartsWith("[PrintDetail]"))
                    printDetail = Convert.ToBoolean(s.Replace("[PrintDetail]", "").Replace("\t", ""));
                else if (s.StartsWith("[SaveLog]"))
                    saveLog = Convert.ToBoolean(s.Replace("[SaveLog]", "").Replace("\t", ""));
                else if (s.StartsWith("[Args]") && args.Length < 3)
                    args = s.Replace("[Args]", "").Replace("\t", "").Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

            println("Settings:", ConsoleColor.Green);            
			println(string.Join("\n", sets));
			println("\n\n...fetching global keys...", ConsoleColor.DarkRed);
   
            //parses the arguments
            profile.index = Convert.ToInt32(args[0]);
            outputLogFile = args[1];
            usdaroot = $"{root}{sl}usr{sl}share{sl}DBs{sl}USDAstock{sl}";
            usdaNutKeyLines = File.ReadAllLines($"{usdaroot}_nutKeyPairs.TXT");
            root = $"{root}{sl}usr{sl}profile{profile.index}{sl}"; //sets to user root

            profData = File.ReadAllLines($"{root}profile.TXT");
            activeFieldsLines = File.ReadAllLines($"{root}activeFields.TXT");
            profile.name = profData[0];

            //grabs the active nutrients
            foreach (string s in activeFieldsLines)
                if (s.Split('#')[0].Trim() != "")
                {
                    _nutrient n = new _nutrient();
                    n.field = s.Split('#')[0].Trim().Split('|')[0];
                    n.rda = s.Split('#')[0].Trim().Split('|')[1];
                    nutrients.Add(n);
                }            

            foreach (string s in usdaNutKeyLines)
            {
                if (s.Split('|')[1] == "NDBNo")
                    ndbnos = File.ReadAllLines($"{usdaroot}{s.Split('|')[0]}");
                foreach (_nutrient n in nutrients)
                    if (n.field == s.Split('|')[1])
                        n.fileName = s.Split('|')[0];
            }
            println("...reading in USDAstock...", ConsoleColor.DarkRed);
            print("\nCurrent user: ");
            println(profile.name, ConsoleColor.Green);

            //compares against usda fields
            fields = new List<string>();
            for (int i = 0; i < nutrients.Count; i++)
            {
                foreach (string s in usdaNutKeyLines)
                    if (s.Split('|')[1] == nutrients[i].field)
                        fields.Add(nutrients[i].field);
                foreach (string s in Directory.GetDirectories($"{rootSpare}{sl}usr{sl}share{sl}_rel_USDAstock"))
                    if (!s.Split(Path.DirectorySeparatorChar)[s.Split(Path.DirectorySeparatorChar).Length - 1].StartsWith("_"))
                        foreach (string st in File.ReadAllLines($"{s}{sl}_dbInfo.TXT"))
                            if (st.StartsWith("[Field]"))
                                fields.Add(st.Replace("[Field]", ""));                
            }
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
                        usdaFileNameLinePairs.Add(st.Split('|')[0], File.ReadAllLines($"{usdaroot}{st.Split('|')[0]}"));
                    
            //outputs the legend        
            println();
            //println("warn", ConsoleColor.Yellow);
            //println("crit", ConsoleColor.DarkRed);
            //println("ideal", ConsoleColor.Blue);
            //println("over", ConsoleColor.DarkBlue);

            //loops thru dates, reads in the user foodlog and computes results
            for (int i = 2; i < args.Length; i++)
            {
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
            if (saveLog){
                Directory.CreateDirectory(Path.GetDirectoryName(outputLogFile));
				File.WriteAllLines(outputLogFile, outputLog);
                Console.Write("Log saved to ");
                println(outputLogFile, ConsoleColor.Cyan);
            }
            println("press any key to exit...");
            Console.ReadKey();
            Console.WriteLine();
        }

        #region printp and printLog
        private static void printLog(string date, List<_nutrient> nuts)
        {
            //breaks up by date
            println($"==========\n{date}\n==========", ConsoleColor.DarkCyan);

            //preps the calculation
            string[] foodDayLines = File.ReadAllLines($"{root}foodlog{sl}{date}.TXT");
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
            println($"\n{profData[0]}'s NUTRITION DETAIL REPORT {date}\n", ConsoleColor.Green);
            //performs piecemeal addition
            //usda specific.. :(
            string[] unitLines = File.ReadAllLines($"{usdaroot}_unitKeyPairs.TXT");
            foreach (_nutrient n in nuts)
            {
                foreach (string s in unitLines)
                    if (n.fileName != null && s.StartsWith(n.fileName))
                    {
                        n.unit = s.Split('|')[1];
                        break;
                    }
                try
                {
                    string[] nutValLines = usdaFileNameLinePairs[n.fileName];

                    //performs the addition
                    foreach (_foodObj f in todaysFood)
                    {
                        try { n.consumed += Convert.ToDouble(nutValLines[f.index]) * f.grams * 0.01; }
                        catch (Exception e) { printE(e); }
                        n.contrib += $"{Math.Round(n.consumed, 3)}, ";
                    }
                }
                catch { }
				}

            //rel - multi
            foreach (_nutrient n in nuts)
            {
                foreach (string s in Directory.GetDirectories($"{rootSpare}{sl}usr{sl}share{sl}_rel_USDAstock"))
                    if (!s.Split(Path.DirectorySeparatorChar)[s.Split(Path.DirectorySeparatorChar).Length - 1].StartsWith("_"))
                    {
                        _relDB r = new _relDB();
                        r.path = s;
                        r.ndbLines = File.ReadAllLines($"{s}{sl}NDB.TXT");
                        r.valLines = File.ReadAllLines($"{s}{sl}VAL.TXT");
                        r.nutLines = File.ReadAllLines($"{s}{sl}NUT.TXT");
                        foreach (string st in File.ReadAllLines($"{s}{sl}_dbInfo.TXT"))
                            if (st.StartsWith("[Field]"))
                                fields.Add(st.Replace("[Field]", ""));
                    }

                foreach (_foodObj f in todaysFood)
                    try
                    {
                        for (int i = 0; i < ndblines.Length; i++)
                            if (ndblines[i] == f.ndbno && nutlines[i] == n.field)
                            {
                                n.consumed += Convert.ToDouble(vallines[i]) * f.grams * 0.01;
                                if (printDetail)
                                    println($"{f.name}//{n.field}//{Convert.ToDouble(vallines[i]) * f.grams * 0.01}");
                                break; //this should be okay, as a 1:1 uniqueness is guaranteed
                            }

                    }
                    catch { }
            }

            //prints results
            foreach (_nutrient n in nutrients)
                printp(n);
            println();
        }

        private static double printp(_nutrient nut)
        {
            double c = 0;
            double r = 1;
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
			if (x > 1.9)            
				color = ConsoleColor.DarkGray;
            if (x > 1.0)
                x = 1.0;
            
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
            string pad = "";
            for (int i = 0; i < (20 - $"[{Math.Round(c, 1)}/{nut.rda.Split(' ')[0]} {nut.unit}".Length); i++)
                pad += " ";
            if (printDetail)
                prog += $"> {100 * Math.Round(c / r, 3)}% \t[{Math.Round(c, 1)}/{nut.rda.Split(' ')[0]} {nut.unit}{pad} -- {nut.field}] {{{nut.contrib}}}";
            else
                prog += $"> {100 * Math.Round(c / r, 3)}% \t[{Math.Round(c, 1)}/{nut.rda.Split(' ')[0]} {nut.unit}{pad} -- {nut.field}]";
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

        private static void printE(Exception ex)
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
