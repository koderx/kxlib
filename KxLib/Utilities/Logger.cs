using System;
using System.Diagnostics;
using System.IO;

namespace KxLib.Utilities {
    /// <summary>
    /// 日志记录类 其中Log只在DEBUG模式下输出 一般信息使用INFO即可
    /// </summary>
    public class Logger {
        private static bool hasInit=false;
        private static FileStream fs;
        private static StreamWriter sw;
        public static string Format = "[{0}][{1}]{2}";
        public static void Init(string FilePath) {
            fs = new FileStream(FilePath, FileMode.Append);
            sw = new StreamWriter(fs);
            hasInit = true;
        }
        [Conditional("DEBUG")]
        public static void Log(string Message) {
            WriteLog("DEBUG", Message);
            Debug.WriteLine(Message);
        }
        public static void Info(string Message) {
            WriteLog("INFO", Message);
        }
        public static void Warn(string Message) {
            WriteLog("WARN", Message);
        }
        public static void Error(string Message) {
            WriteLog("ERROR", Message);
            Debug.WriteLine(Message);
        }
        public static void Error(string Message,Exception ex) {
            Error(Message);
            Error(ex.ToString());
        }
        public static void Fatal(string Message) {
            WriteLog("FATAL", Message);
            Debug.WriteLine(Message);
        }
        private static void WriteLog(string Level,string Message) {
            if (!hasInit) {
                return;
            }
            string log = String.Format(Format, DateTime.Now.ToString(), Level, Message);
            if (sw!=null) {
                sw.WriteLine(log);
                sw.Flush();
            }
        }
        public static void Close() {
            if (sw != null) {
                sw.Close();
                sw = null;
            }
            hasInit = false;
        }
    }
}
