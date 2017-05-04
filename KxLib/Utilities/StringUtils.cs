using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace KxLib.Utilities {
    public static class StringUtils {
        /// <summary>
        /// 分割字符串
        /// </summary>
        /// <param name="str"></param>
        /// <param name="splitstr"></param>
        /// <returns></returns>
        public static string[] Split(this string str, string splitstr) {
            string[] strArray = null;
            if ((str != null) && (str != "")) {
                strArray = new Regex(splitstr).Split(str);
            }
            return strArray;
        }

        /// <summary>
        /// 对指定字符串进行 MD5 哈希
        /// </summary>
        /// <param name="s">源字符串</param>
        /// <param name="lcase">转换成小写</param>
        /// <returns>MD5哈希后的字符串</returns>
        public static string MD5(this string s, bool lcase = false) {
            //md5加密
            //s = System.Web.Security.FormsAuthentication.HashPasswordForStoringInConfigFile(s, "md5").ToString();
            byte[] result = Encoding.Default.GetBytes(s);    //tbPass为输入密码的文本框  
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] output = md5.ComputeHash(result);
            string md5str = BitConverter.ToString(output).Replace("-", "");
            if (lcase) {
                md5str = md5str.ToLower();
            }
            return md5str;
        }
    }
}
