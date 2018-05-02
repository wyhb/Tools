using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wyhb.Joe.Common;

namespace Wyhb.Joe.Jenkins
{
    public static class Plugins
    {
        private const string VB_EXTENSION = ".vb";
        private const string ASSEMBLY_VERSION = "AssemblyVersion";
        private const string ASSEMBLY_FILE_VERSION = "AssemblyFileVersion";
        private static List<string> chgAssemblyLst = new List<string> { ASSEMBLY_VERSION, ASSEMBLY_FILE_VERSION };
        private const string VB_ASSEMBLY = "<Assembly:";
        private const string CSHARP_ASSEMBLY = "[assembly:";
        private static List<string> assemblyLst = new List<string> { VB_ASSEMBLY, CSHARP_ASSEMBLY };
        private const string ASSEMBLY_FILE = "*AssemblyInfo.*";
        private const string VERSION_0000 = "0000";
        private const string STR_0 = "0";

        #region ChgAssemblyVersion

        public static void ChgAssemblyVersion(string workspace, string version)
        {
            if (!VERSION_0000.Equals(version))
            {
                var index = version.ToCharArray().Select(x => x.ToString()).ToList().Select((x, idx) => new { Idx = idx, Char = x }).ToList().Where(x => !STR_0.Equals(x.Char)).Select(x => x.Idx).FirstOrDefault();

                var setVerLst = VERSION_0000.ToCharArray().Select(x => x.ToString()).ToList().ConvertAll(x => Int32.Parse(x)).ToList();
                setVerLst[index] = 1;

                if (Directory.Exists(workspace))
                {
                    Directory.EnumerateFiles(workspace, ASSEMBLY_FILE, SearchOption.AllDirectories).ToList().ForEach(file =>
                     {
                         using (var reader = new StreamReader(file))
                         {
                             var lineLst = reader.ReadToEnd().Replace(Const.STR_CRLF, Const.STR_LF).Split(Const.CHAR_LF).ToList();
                             lineLst.Select(line =>
                             {
                                 if (assemblyLst.Contains(line))
                                 {
                                     var currentVer = line.Substring(line.IndexOf(Const.STR_PARENTHESES_L) + Const.NUM_2, line.LastIndexOf(Const.STR_PARENTHESES_R) - line.IndexOf(Const.STR_PARENTHESES_L) - Const.NUM_3);
                                     var currentVerLst = currentVer.Split(Const.CHAR_DOT).ToList().ConvertAll(x => Int32.Parse(x)).ToList();
                                     line = line.Replace(currentVer, string.Join(Const.STR_DOT, currentVerLst.Zip(setVerLst, (ver, setVer) => ver + setVer).ToList()));
                                 }
                                 return line;
                             });
                         }
                     });
                }
            }
        }

        #endregion ChgAssemblyVersion
    }
}