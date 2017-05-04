using System;
using System.Threading;
using KxLib.Utilities;
using System.Collections.Generic;
using System.IO;
using LitJson;

namespace KxLib.FileList {
    public class MessageEventArgs: EventArgs {
        public string Message;
        public MessageEventArgs(string msg) {
            Message = msg;
        }
    }
    public class ProgressEventArgs : EventArgs {
        public int Value;
        public ProgressEventArgs(int value) {
            Value = value;
        }
    }
    public delegate void MessageEvent(object sender, MessageEventArgs e);
    public delegate void ProgressEvent(object sender, ProgressEventArgs e);

    public class AppUpdate {
        private 
        HttpHelper client = new HttpHelper();
        public event EventHandler Start;
        public event EventHandler Finish;
        public event ProgressEvent Progress;
        public event MessageEvent Message;
        private int pvalue;
        private bool isStop;
        private string ServerHost;
        private string DownloadPath;
        private Thread thd;
        public int ProgressValue {
            get { return pvalue; }
            private set {
                pvalue = value;
                if (Progress != null)
                    Progress(this, new ProgressEventArgs(value));
            }
        }

        public AppUpdate(string downpath,string serverHost) {
            ServerHost = serverHost;
            DownloadPath = downpath;
            thd = new Thread(run);
            isStop = true;

        }
        public bool isRun() {
            return !isStop;
        }
        public void Stop() {
            if (!isStop) {
                isStop = true;
                if (Finish != null)
                    Finish(this, null);
            }
           
        }
        public void Begin() {
            if (Start != null)
                Start(this, null);
            isStop = false;
            thd.Start();
        }
        private void sendMessage(string msg) {
            if (Message != null)
                Message(this, new MessageEventArgs(msg));
        }
        private string makeUrl(string path) {
            path = path.Replace("\\","/");
            return "http://" + ServerHost + path;
        }
        private string makeLocalUrl(string path) {
            return Directory.GetCurrentDirectory() + path;
        }
        private void run() {
            ProgressValue = 0;
            sendMessage("正在检查更新");
            string jsondata=client.GET(makeUrl("/list.json"));
            ProgressValue = 500;
            List<UpdateFile> flist=new List<UpdateFile>();
            try {
                flist = JsonMapper.ToObject<List<UpdateFile>>(jsondata);
                //flist = JsonConvert.DeserializeObject<List<UpdateFile>>(jsondata);
            } catch (Exception ex) {
                sendMessage("服务器连接错误, " + ex.Message);
                Logger.Error(ex.ToString());
                Stop();
                return;
            }
            
            sendMessage("正在检查文件完整性(0/"+flist.Count+")");
            int i = 0;
            List<UpdateFile> needDownload = new List<UpdateFile>();
            foreach (UpdateFile uf in flist) {
                if (isStop) {
                    break;
                }
                i++;
                if (uf.size < 0) {
                    //文件夹
                    if (!Directory.Exists(makeLocalUrl(uf.path))) {
                        Directory.CreateDirectory(makeLocalUrl(uf.path));
                    }
                } else {
                    //文件
                    string md5 = FileHelper.GetMD5HashFromFile(makeLocalUrl(uf.path));
                    if (!File.Exists(makeLocalUrl(uf.path)) || md5 != uf.md5) {
                        //Console.WriteLine("{0}-{1}", makeLocalUrl(uf.path), uf.path);
                        //Console.WriteLine("{0}-{1}", FileHelper.GetMD5HashFromFile(makeLocalUrl(uf.path)), uf.md5);
                        Logger.Info(uf.path + " " + md5 + "=>" + uf.md5);
                        needDownload.Add(uf);
                    }
                }
                if (isStop) {
                    break;
                }
                sendMessage("正在检查文件完整性("+i+"/" + flist.Count + ")");
                ProgressValue = (i * 1500 / flist.Count)+500;
            }
            ProgressValue = 2000;
            i = 0;
            foreach (UpdateFile uf in needDownload) {
                if (isStop) {
                    break;
                }
                i++;
                sendMessage("正在更新游戏 (" + i + "/" + needDownload.Count + ") " + uf.path);
                ProgressValue = (i * 8000 / needDownload.Count) + 2000;
                try {
                    client.DownloadFile(makeUrl(uf.path), makeLocalUrl(uf.path));
                }catch(Exception ex) {
                    Console.WriteLine(ex.Message);
                }
                
            }
            sendMessage("更新完毕");
            ProgressValue = 10000;
            Stop();
        }
    }
}
