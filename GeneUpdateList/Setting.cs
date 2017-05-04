using KxDotNetLib.Utilities;
using LitJson;

namespace GeneUpdateList {
    class Setting {
        public static string WorkPlace="";
        public string workPlace {
            get { return WorkPlace; }
            set { WorkPlace = value; }
        }
        public static void Load() {
            Setting setting=JsonMapper.ToObject<Setting>(FileHelper.LoadTxt(@"Config.json"));
        }
        public static void Save() {
            FileHelper.SaveTxt(@"Config.json", JsonMapper.ToJson(new Setting()));
        }
    }
}
