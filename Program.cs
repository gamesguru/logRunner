using System;
using System.Collections.Generic;
using System.Linq;
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
        static double warn;
        static ConsoleColor warnColor;
        static double crit;
        static ConsoleColor critColor;
        static double over;
        static ConsoleColor overColor;
        static ConsoleColor defaultColor;

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
            public bool ext;
        }

        public class _foodObj
        {
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

            List<string> sets = new List<string>();
            foreach (string s in File.ReadAllLines($"{root}{sl}lsettings.ini"))
                if (!s.StartsWith("#") && s.Length > 4)
                    sets.Add(s.Split('#')[0]);

            foreach (string s in sets)
                if (s == null || s.Length == 0)
                    continue;
                else if (s.StartsWith("[PrintDetail]"))
                    printDetail = Convert.ToBoolean(s.Replace("[PrintDetail]", "").Replace("\t", ""));
                else if (s.StartsWith("[SaveLog]"))
                    saveLog = Convert.ToBoolean(s.Replace("[SaveLog]", "").Replace("\t", ""));
                else if (s.StartsWith("[Warn]"))
                {
                    warn = Convert.ToDouble(s.Replace("[Warn]", "").Replace("\t", "").Split(' ')[0]);
                    warnColor = _color(s.Replace("[Warn]", "").Replace("\t", "").Split(' ')[1]);
                }
                else if (s.StartsWith("[Crit]"))
                {
                    crit = Convert.ToDouble(s.Replace("[Crit]", "").Replace("\t", "").Split(' ')[0]);
                    critColor = _color(s.Replace("[Crit]", "").Replace("\t", "").Split(' ')[1]);
                }
                else if (s.StartsWith("[Over]"))
                {
                    over = Convert.ToDouble(s.Replace("[Over]", "").Replace("\t", "").Split(' ')[0]);
                    overColor = _color(s.Replace("[Over]", "").Replace("\t", "").Split(' ')[1]);
                }
                else if (s.StartsWith("[DefaultColor]"))
                    defaultColor = _color(s.Replace("[DefaultColor]", "").Replace("\t", ""));
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
            foreach (string s in profData)
                if (s.StartsWith("[Name]"))
                    profile.name = s.Replace("[Name]", "");

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
                //base db
                foreach (string s in usdaNutKeyLines)
                    if (s.Split('|')[1] == nutrients[i].field)
                        fields.Add(nutrients[i].field);
                //ext db
                foreach (string s in Directory.GetDirectories($"{rootSpare}{sl}usr{sl}share{sl}_rel_USDAstock"))
                    if (!s.Split(Path.DirectorySeparatorChar)[s.Split(Path.DirectorySeparatorChar).Length - 1].StartsWith("_"))
                        foreach (string st in File.ReadAllLines($"{s}{sl}_dbInfo.TXT"))
                            if (st.StartsWith("[Field]"))
                                fields.Add(st.Replace("[Field]", ""));
                //paired fields (user-specific)
                //TODO: add work here
                //foreach (string s in Directory.GetDi)             
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
            println();

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
            if (saveLog)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(outputLogFile));
                File.WriteAllLines(outputLogFile, outputLog);
                Console.Write("Log saved to ");
                println(outputLogFile, ConsoleColor.Cyan);
            }
            println("press any key to exit...");
            Console.ReadKey();
            Console.WriteLine();
        }

        static ConsoleColor c;
        private static ConsoleColor _color(string color)
        {
            switch (color)
            {
                case "Black":
                    c = ConsoleColor.Black;
                    break;
                case "Blue":
                    c = ConsoleColor.Blue;
                    break;
                case "Cyan":
                    c = ConsoleColor.Cyan;
                    break;
                case "DarkBlue":
                    c = ConsoleColor.DarkBlue;
                    break;
                case "DarkCyan":
                    c = ConsoleColor.DarkCyan;
                    break;
                case "DarkGray":
                    c = ConsoleColor.DarkGray;
                    break;
                case "DarkGreen":
                    c = ConsoleColor.DarkGreen;
                    break;
                case "DarkMagenta":
                    c = ConsoleColor.DarkMagenta;
                    break;
                case "DarkRed":
                    c = ConsoleColor.DarkRed;
                    break;
                case "DarkYellow":
                    c = ConsoleColor.DarkYellow;
                    break;
                case "Gray":
                    c = ConsoleColor.Gray;
                    break;
                case "Green":
                    c = ConsoleColor.Green;
                    break;
                case "Magenta":
                    c = ConsoleColor.Magenta;
                    break;
                case "Red":
                    c = ConsoleColor.Red;
                    break;
                case "White":
                    c = ConsoleColor.White;
                    break;
                case "Yellow":
                    c = ConsoleColor.Yellow;
                    break;
                default:
                    println($"unknown color: {color}", ConsoleColor.Yellow);
                    c = ConsoleColor.White;
                    break;
            }
            return c;
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
            print($"\n{profile.name}'s ", ConsoleColor.Green);
            print("NUTRITION DETAIL REPORT ");
            println($"{ date}\n", ConsoleColor.Green);
            //performs piecemeal addition
            //usda specific.. :(
            println("...computing USDA totals...", ConsoleColor.DarkRed);
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
            println("...computing extended database totals...", ConsoleColor.DarkRed);
            foreach (string s in Directory.GetDirectories($"{rootSpare}{sl}usr{sl}share{sl}_rel_USDAstock"))
                if (!s.Split(Path.DirectorySeparatorChar)[s.Split(Path.DirectorySeparatorChar).Length - 1].StartsWith("_"))
                {
                    _relDB r = new _relDB();
                    r.path = s;
                    r.ndbLines = File.ReadAllLines($"{s}{sl}NDB.TXT");
                    r.valLines = File.ReadAllLines($"{s}{sl}VAL.TXT");
                    r.nutLines = File.ReadAllLines($"{s}{sl}NUT.TXT");
                    relDBs.Add(r);
                    foreach (string st in File.ReadAllLines($"{s}{sl}_dbInfo.TXT"))
                        if (st.StartsWith("[Field]") && !fields.Contains(st.Replace("[Field]", "")))
                            fields.Add(st.Replace("[Field]", ""));
                }
            foreach (_nutrient n in nuts)
                foreach (_foodObj f in todaysFood)
                    foreach (_relDB r in relDBs)
                    {
                        n.ext = true;
                        for (int i = 0; i < r.ndbLines.Length; i++)
                            if (Convert.ToInt32(r.ndbLines[i]) == Convert.ToInt32(f.ndbno) && r.nutLines[i] == n.field)
                                try
                                {
                                    //if (n.field == "Naringenin")
                                    //Console.WriteLine($"{n.field}, {f.ndbno}, {f.name}, {r.valLines[i]}, {i}\n{Convert.ToInt32("09112")}, {Convert.ToInt32("9112")}"); //TODO: debug here; DONE!!
                                    n.consumed += Convert.ToDouble(r.valLines[i]) * f.grams * 0.01;
                                    //if (printDetail)
                                    //    println($"{f.name}//{n.field}//{Convert.ToDouble(r.valLines[i]) * f.grams * 0.01}");
                                    n.unit = n.rda.Split(' ')[1];
                                    n.contrib += $"{Math.Round(n.consumed, 3)}, ";

                                    break; //this should be okay, as each ndb listing (per food) has one specific nutrient input
                                }
                                catch (Exception ex)
                                {
                                    printE(ex);
                                }
                    }
            int m = nuts.Count;
            List<_nutrient> nuts2 = new List<_nutrient>();
            for (int i = 0; i < m; i++)
                try
                {
                    if (nuts[i].consumed / Convert.ToDouble(nuts[i].rda.Split(' ')[0]) > 0.01 || !nuts[i].ext)
                        nuts2.Add(nuts[i]);
                }
                catch (Exception e) { println(e.ToString()); }

            //prints results
            foreach (_nutrient n in nuts2)// nutrients)
                printp(n);
            println();
            println($"...{nuts.Count - nuts2.Count} minor fields with negligible data, they are not reported here...");
            if (printDetail)
                foreach (_nutrient n in nuts)
                    if (!nuts2.Contains(n))
                        println(n.field);
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
            ConsoleColor color = defaultColor;
            if (x > over)
                color = overColor;
            if (x > 1.0)
                x = 1.0;

            else if (x < warn && x > crit)
                color = warnColor;
            else if (x <= crit)
                color = critColor;

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
            prog += $"> {100 * Math.Round(c / r, 3)}% \t[{Math.Round(c, 1)}/{nut.rda.Split(' ')[0]} {nut.unit}{pad} -- {nut.field}]";
            if (printDetail)
                prog += $" {{{nut.contrib}}}";
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
            println($"{DateTime.Now.ToString()}\n{ex.Source}, {ex.HResult}\n{ex.Data}");
            println($"{ex.Message}", ConsoleColor.DarkRed);
            println($"\n\n{ex.TargetSite}\n{ex.StackTrace}");
            println(" ===================\n  --End exception--\n ===================", ConsoleColor.DarkRed);
        }
        #endregion
    }
}