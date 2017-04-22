using System;
using System.IO;
using System.Linq;
using System.Text;
namespace SymbolicLink
{
    static class Program
    {
#if DEBUG
        private static StringBuilder logs = new StringBuilder();
#endif

        private static void Log(string str)
        {
#if DEBUG
            logs.AppendLine(str);
#endif
        }

        private static void CheckSource(string dir)
        {
            if (Directory.Exists(dir)) return;
            Directory.CreateDirectory(dir);
#if DEBUG
            logs.AppendLine($"{dir} not exists,create one");
#endif
        }
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {

            var filePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            var pwd = Path.GetDirectoryName(filePath);
            var configFile = $"{pwd}\\{Path.GetFileNameWithoutExtension(filePath)}.cfg";
            if (!File.Exists(configFile)) return;
            var maps = File.ReadAllLines(configFile).Where(c=>!c.StartsWith(":")).Select(c => c.Split('*')).Where(c => c.Length == 2).ToDictionary(k => k[0], v => v[1]);
            var rgx = new System.Text.RegularExpressions.Regex(@"%(\w+)%", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            foreach (var map in maps)
            {
                var source = map.Value;
                var target = map.Key;
                if (rgx.IsMatch(map.Value))
                {
                    foreach (System.Text.RegularExpressions.Match match in rgx.Matches(map.Value))
                    {
                        if (!Enum.TryParse(match.Value.Replace("%", ""), out Environment.SpecialFolder folder)) continue;
                        var gp = Environment.GetFolderPath(folder);
                        source = source.Replace(match.Value, gp);
                    }
                }

                if (rgx.IsMatch(map.Key))
                {
                    foreach (System.Text.RegularExpressions.Match match in rgx.Matches(map.Key))
                    {
                        if (!Enum.TryParse(match.Value.Replace("%", ""), out Environment.SpecialFolder folder)) continue;
                        var gp = Environment.GetFolderPath(folder);
                        target = target.Replace(match.Value, gp);
                    }
                }
                Log($"{source} => {target}");
                if (Directory.Exists(target))
                {
                    if (SymbolicHelper.IsSymbolic(target))
                    {
                        var s = SymbolicHelper.GetTarget(target);
                        if (string.CompareOrdinal(s, source) == 0)
                        {
                            Log($"{s} linked to {target}, Very Good\n");
                            continue;
                        }
                        Log($"{s} not link to {target},Remove. ({s})");
                        SymbolicHelper.Delete(target);
                    }
                    else
                    {
                        var t = target + DateTime.Now.ToFileTime();
                        Log($"{target} Not Symbolic,Backup to {t}");
                        System.Windows.Forms.MessageBox.Show($"{target}\n{t}");
                        Directory.Move(target, t);
                    }
                }
                CheckSource(source);
                Log($"Link {source} to {target}");
                SymbolicHelper.Create(source, target);
                Log("Link Finished\n");
            }
#if DEBUG
            File.WriteAllText($"{pwd}\\logs.txt", logs.ToString());
            System.Diagnostics.Process.Start($"{pwd}\\logs.txt");
#endif
        }
    }
}
