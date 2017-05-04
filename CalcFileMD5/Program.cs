using KxDotNetLib.Utilities;
using System.IO;

namespace CalcFileMD5 {
    class Program {
        static void Main(string[] args) {
            foreach (var fp in args) {
                if (File.Exists(fp)) {
                    string md5 = FileHelper.GetMD5HashFromFile(fp);
                    FileHelper.SaveTxt(fp + ".md5", md5);
                }
            }
            
        }
    }
}
