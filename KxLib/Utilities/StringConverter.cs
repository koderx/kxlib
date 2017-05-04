using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KxLib.Utilities {
    public static class StringConverter {
        public static int ToInt32(this string s) {
            return Convert.ToInt32(s);
        }
        public static long ToInt64(this string s) {
            return Convert.ToInt64(s);
        }
    }

    
}
