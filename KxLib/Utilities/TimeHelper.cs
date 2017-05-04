using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KxLib.Utilities {
    public class TimeHelper {
        /// <summary>  
        /// 获取当前时间戳  
        /// </summary>  
        /// <param name="bflag">为真时获取10位时间戳,为假时获取13位时间戳.</param>  
        /// <returns></returns>  
        public static string GetTimeStamp(bool bflag = true) {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            string ret = string.Empty;
            if (bflag)
                ret = Convert.ToInt64(ts.TotalSeconds).ToString();
            else
                ret = Convert.ToInt64(ts.TotalMilliseconds).ToString();

            return ret;
        }

        public static DateTime GetDateTime(string ts) {
            if (ts.Length == 10) {
                DateTime time = DateTime.MinValue;
                DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                time = startTime.AddSeconds(ts.ToInt64());
                return time;
            } else if (ts.Length == 13) {
                DateTime time = DateTime.MinValue;
                DateTime startTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                time = startTime.AddMilliseconds(ts.ToInt64());
                return time;
            }
            throw new NotSupportedException("不支持转换这样的时间戳");
        }
    }
}
