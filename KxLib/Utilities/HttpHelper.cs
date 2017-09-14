using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text.RegularExpressions;
using RE = System.Text.RegularExpressions.Regex;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
/***************************************************************************************************************************************************
* *文件名：HttpHelper.cs(HttpProc.cs)
* *创建人：HeDaode
* *修订人：KoderX
* *日  期：2015-08-07(2007.09.01)
* *描  述：实现HTTP协议中的GET、POST请求
* *使  用：using KxLib.Utilities;
WebClient client = new WebClient();
client.Encoding = System.Text.Encoding.Default;//默认编码方式，根据需要设置其他类型
client.GET("http://www.baidu.com");//普通get请求
MessageBox.Show(client.Response);//获取返回的网页源代码
client.DownloadFile("http://www.codepub.com/upload/163album.rar",@"C:\163album.rar");//下载文件
client.GET("http://passport.baidu.com/?login","username=zhangsan&password=123456");//提交表单，此处是登录百度的示例
client.UploadFile("http://hiup.baidu.com/zhangsan/upload", @"file1=D:\1.mp3");//上传文件
client.UploadFile("http://hiup.baidu.com/zhangsan/upload", "folder=myfolder&size=4003550",@"file1=D:\1.mp3");//提交含文本域和文件域的表单
*****************************************************************************************************************************************************/
namespace KxLib.Utilities {
    ///<summary>
    ///上传事件委托
    ///</summary>
    ///<param name="sender"></param>
    ///<param name="e"></param>
    public delegate void WebClientUploadEvent(object sender, UploadEventArgs e);

    ///<summary>
    ///下载事件委托
    ///</summary>
    ///<param name="sender"></param>
    ///<param name="e"></param>
    public delegate void WebClientDownloadEvent(object sender, DownloadEventArgs e);


    ///<summary>
    ///上传事件参数
    ///</summary>
    public struct UploadEventArgs {
        ///<summary>
        ///上传数据总大小
        ///</summary>
        public long totalBytes;
        ///<summary>
        ///已发数据大小
        ///</summary>
        public long bytesSent;
        ///<summary>
        ///发送进度(0-1)
        ///</summary>
        public double sendProgress;
        ///<summary>
        ///发送速度Bytes/s
        ///</summary>
        public double sendSpeed;
    }

    ///<summary>
    ///下载事件参数
    ///</summary>
    public struct DownloadEventArgs {
        ///<summary>
        ///下载数据总大小
        ///</summary>
        public long totalBytes;
        ///<summary>
        ///已接收数据大小
        ///</summary>
        public long bytesReceived;
        ///<summary>
        ///接收数据进度(0-1)
        ///</summary>
        public double ReceiveProgress;
        ///<summary>
        ///当前缓冲区数据
        ///</summary>
        public byte[] receivedBuffer;
        ///<summary>
        ///接收速度Bytes/s
        ///</summary>
        public double receiveSpeed;
    }

    ///<summary>
    ///实现向WEB服务器发送和接收数据
    ///</summary>
    public class HttpHelper {
        private WebHeaderCollection requestHeaders, responseHeaders;
        private TcpClient clientSocket;
        private MemoryStream postStream;
        private Encoding encoding = Encoding.Default;
        private const string BOUNDARY = "--HEDAODE--";
        private const int SEND_BUFFER_SIZE = 10245;
        private const int RECEIVE_BUFFER_SIZE = 10245;
        private int timeOut = 10000;
        private string cookie = "";
        private string respHtml = "";
        private string strRequestHeaders = "";
        private string strResponseHeaders = "";
        private int statusCode = 0;
        private bool isCanceled = false;
        public event WebClientUploadEvent UploadProgressChanged;
        public event WebClientDownloadEvent DownloadProgressChanged;

        // https的
        private CookieContainer cookieContainer = new CookieContainer();
        public string Referer { get; set; } = "";
        public string UserAgent { get; set; } = "";

        ///<summary>
        ///初始化WebClient类
        ///</summary>
        public HttpHelper() {
            responseHeaders = new WebHeaderCollection();
            requestHeaders = new WebHeaderCollection();
        }

        public HttpHelper(Encoding encoding) {
            responseHeaders = new WebHeaderCollection();
            requestHeaders = new WebHeaderCollection();
            this.encoding = encoding;
        }

        public void SetTimeOut(int msec) {
            timeOut = msec;
        }

        ///<summary>
        ///读取指定URL的文本
        ///</summary>
        ///<param name="URL">请求的地址</param>
        ///<returns>服务器响应文本</returns>
        public string GET(string URL) {
            requestHeaders.Add("Connection", "close");
            SendRequestData(URL, "GET");
            return GetHtml();
        }

        ///<summary>
        ///读取指定URL的文本
        ///</summary>
        ///<param name="URL">请求的地址</param>
        ///<returns>服务器响应文本</returns>
        public string GET_Https(string URL) {
            ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.CookieContainer = cookieContainer;
            request.Method = "GET";
            request.Accept = "*/*";
            request.UserAgent = UserAgent;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), encoding);
            this.respHtml = reader.ReadToEnd();
            foreach (Cookie ck in response.Cookies) {
                this.cookie += ck.Name + "=" + ck.Value + ";";
            }
            cookieContainer.Add(response.Cookies);
            reader.Close();
            return respHtml;
        }

        public void ClearCookie() {
            this.cookie = "";
            cookieContainer = new CookieContainer();
        }

        ///<summary>
        ///采用https协议访问网络
        ///</summary>
        ///<param name="URL">url地址</param>
        ///<param name="strPostdata">发送的数据</param>
        ///<returns></returns>
        public string POST_Https(string URL, string strPostdata,string[] header=null) {
            ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
            request.CookieContainer = cookieContainer;
            request.Method = "POST";
            request.Accept = "*/*";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Referer = Referer;
            request.UserAgent = UserAgent;
            Referer = "";
            if (header != null) {
                foreach (var item in header) {
                    request.Headers.Add(item);
                }
            }
            byte[] buffer = this.encoding.GetBytes(strPostdata);
            request.ContentLength = buffer.Length;
            request.GetRequestStream().Write(buffer, 0, buffer.Length);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StreamReader reader = new StreamReader(response.GetResponseStream(), encoding);
            this.respHtml = reader.ReadToEnd();
            foreach (System.Net.Cookie ck in response.Cookies) {
                this.cookie += ck.Name + "=" + ck.Value + ";";
            }
            cookieContainer.Add(response.Cookies);
            reader.Close();
            return respHtml;
        }
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors) {
            return true; //总是接受
        }
        ///<summary>
        ///读取指定URL的文本
        ///</summary>
        ///<param name="URL">请求的地址</param>
        ///<param name="postData">向服务器发送的文本数据</param>
        ///<returns>服务器响应文本</returns>
        public string OpenRead(string URL, string postData) {
            byte[] sendBytes = encoding.GetBytes(postData);
            postStream = new MemoryStream();
            postStream.Write(sendBytes, 0, sendBytes.Length);
            requestHeaders.Add("Content-Length", postStream.Length.ToString());
            requestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
            requestHeaders.Add("Connection", "close");
            SendRequestData(URL, "POST");
            return GetHtml();
        }
        public string POST(string postUrl, string paramData) {
            Console.WriteLine("URL: " + postUrl);
            Console.WriteLine("DATA: " + paramData);
            string ret = string.Empty;
            try {
                byte[] byteArray = encoding.GetBytes(paramData); //转化
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(postUrl));
                webReq.Method = "POST";
                webReq.ContentType = "application/x-www-form-urlencoded";
                webReq.ContentLength = byteArray.Length;
                Stream newStream = webReq.GetRequestStream();
                newStream.Write(byteArray, 0, byteArray.Length);//写入参数
                newStream.Close();
                HttpWebResponse response = (HttpWebResponse)webReq.GetResponse();
                StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.Default);
                ret = sr.ReadToEnd();
                sr.Close();
                response.Close();
                newStream.Close();
            } catch (Exception ex) {
                Console.WriteLine("POST数据错误: " + ex.Message);
            }
            return ret;
        }

        ///<summary>
        ///读取指定URL的流
        ///</summary>
        ///<param name="URL">请求的地址</param>
        ///<param name="postData">向服务器发送的数据</param>
        ///<returns>服务器响应流</returns>
        public Stream GetStream(string URL, string postData) {
            byte[] sendBytes = encoding.GetBytes(postData);
            postStream = new MemoryStream();
            postStream.Write(sendBytes, 0, sendBytes.Length);
            requestHeaders.Add("Content-Length", postStream.Length.ToString());
            requestHeaders.Add("Content-Type", "application/x-www-form-urlencoded");
            requestHeaders.Add("Connection", "close");
            SendRequestData(URL, "POST");
            MemoryStream ms = new MemoryStream();
            SaveNetworkStream(ms);
            return ms;
        }


        ///<summary>
        ///上传文件到服务器
        ///</summary>
        ///<param name="URL">请求的地址</param>
        ///<param name="fileField">文件域(格式如:file1=C:\test.mp3&file2=C:\test.jpg)</param>
        ///<returns>服务器响应文本</returns>
        public string UploadFile(string URL, string fileField) {
            return UploadFile(URL, "", fileField);
        }

        ///<summary>
        ///上传文件和数据到服务器
        ///</summary>
        ///<param name="URL">请求地址</param>
        ///<param name="textField">文本域(格式为:name1=value1&name2=value2)</param>
        ///<param name="fileField">文件域(格式如:file1=C:\test.mp3&file2=C:\test.jpg)</param>
        ///<returns>服务器响应文本</returns>
        public string UploadFile(string URL, string textField, string fileField) {
            postStream = new MemoryStream();
            if (textField != "" && fileField != "") {
                WriteTextField(textField);
                WriteFileField(fileField);
            } else if (fileField != "") {
                WriteFileField(fileField);
            } else if (textField != "") {
                WriteTextField(textField);
            } else
                throw new Exception("文本域和文件域不能同时为空。");
            //写入结束标记
            byte[] buffer = encoding.GetBytes("--" + BOUNDARY + "--\r\n");
            postStream.Write(buffer, 0, buffer.Length);
            //添加请求标头
            requestHeaders.Add("Content-Length", postStream.Length.ToString());
            requestHeaders.Add("Content-Type", "multipart/form-data; boundary=" + BOUNDARY);
            requestHeaders.Add("Connection", "Keep-Alive");
            //发送请求数据
            SendRequestData(URL, "POST", true);
            //返回响应文本
            return GetHtml();
        }


        ///<summary>
        ///分析文本域，添加到请求流
        ///</summary>
        ///<param name="textField">文本域</param>
        private void WriteTextField(string textField) {
            string[] strArr = RE.Split(textField, "&");
            textField = "";
            foreach (string var in strArr) {
                Match M = RE.Match(var, "([^=]+)=(.+)");
                textField += "--" + BOUNDARY + "\r\n";
                textField += "Content-Disposition: form-data; name=\"" + M.Groups[1].Value + "\"\r\n\r\n" + M.Groups[2].Value + "\r\n";
            }
            byte[] buffer = encoding.GetBytes(textField);
            postStream.Write(buffer, 0, buffer.Length);
        }

        ///<summary>
        ///分析文件域，添加到请求流
        ///</summary>
        ///<param name="fileField">文件域</param>
        private void WriteFileField(string fileField) {
            string filePath = "";
            int count = 0;
            string[] strArr = RE.Split(fileField, "&");
            foreach (string var in strArr) {
                Match M = RE.Match(var, "([^=]+)=(.+)");
                filePath = M.Groups[2].Value;
                fileField = "--" + BOUNDARY + "\r\n";
                fileField += "Content-Disposition: form-data; name=\"" + M.Groups[1].Value + "\"; filename=\"" + Path.GetFileName(filePath) + "\"\r\n";
                fileField += "Content-Type: image/jpeg\r\n\r\n";
                byte[] buffer = encoding.GetBytes(fileField);
                postStream.Write(buffer, 0, buffer.Length);
                //添加文件数据
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                buffer = new byte[50000];
                do {
                    count = fs.Read(buffer, 0, buffer.Length);
                    postStream.Write(buffer, 0, count);
                } while (count > 0);
                fs.Close();
                fs.Dispose();
                fs = null;
                buffer = encoding.GetBytes("\r\n");
                postStream.Write(buffer, 0, buffer.Length);
            }
        }

        ///<summary>
        ///从指定URL下载数据流
        ///</summary>
        ///<param name="URL">请求地址</param>
        ///<returns>数据流</returns>
        public Stream DownloadData(string URL) {
            requestHeaders.Add("Connection", "close");
            SendRequestData(URL, "GET");
            MemoryStream ms = new MemoryStream();
            SaveNetworkStream(ms, true);
            return ms;
        }


        ///<summary>
        ///从指定URL下载文件
        ///</summary>
        ///<param name="URL">文件URL地址</param>
        ///<param name="fileName">文件保存路径,含文件名(如:C:\test.jpg)</param>
        public void DownloadFile(string URL, string fileName) {
            WebClient df = new WebClient();
            df.DownloadFile(URL, fileName);
        }
        ///<summary>
        ///向服务器发送请求
        ///</summary>
        ///<param name="URL">请求地址</param>
        ///<param name="method">POST或GET</param>
        ///<param name="showProgress">是否显示上传进度</param>
        private void SendRequestData(string URL, string method, bool showProgress) {
            if (URL.ToLower().StartsWith("https")) {
                Logger.Log($"https连接 {URL}");
                ServicePointManager.ServerCertificateValidationCallback = CheckValidationResult;
            }
            //clientSocket = new TcpClient();
            Uri URI = new Uri(URL);
            //Console.WriteLine("URL: "+ URI.Host + ":"+ URI.Port);
            clientSocket =TimeOutSocket.Connect(URI.Host, URI.Port, timeOut);
            //clientSocket.Connect(URI.Host, URI.Port);
            requestHeaders.Add("Host", URI.Host);
            byte[] request = GetRequestHeaders(method + " " + URI.PathAndQuery + " HTTP/1.1");
            clientSocket.Client.Send(request);
            //若有实体内容就发送它
            if (postStream != null) {
                byte[] buffer = new byte[SEND_BUFFER_SIZE];
                int count = 0;
                Stream sm = clientSocket.GetStream();
                postStream.Position = 0;
                UploadEventArgs e = new UploadEventArgs();
                e.totalBytes = postStream.Length;
                System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();//计时器
                timer.Start();
                do {
                    //如果取消就推出
                    if (isCanceled) {
                        break;
                    }
                    //读取要发送的数据
                    count = postStream.Read(buffer, 0, buffer.Length);
                    //发送到服务器
                    sm.Write(buffer, 0, count);
                    //是否显示进度
                    if (showProgress) {
                        //触发事件
                        e.bytesSent += count;
                        e.sendProgress = (double)e.bytesSent / (double)e.totalBytes;
                        double t = timer.ElapsedMilliseconds / 1000;
                        t = t <= 0 ? 1 : t;
                        e.sendSpeed = (double)e.bytesSent / t;
                        if (UploadProgressChanged != null) {
                            UploadProgressChanged(this, e);
                        }
                    }
                } while (count > 0);
                timer.Stop();
                postStream.Close();
                //postStream.Dispose();
                postStream = null;
            }//end if
        }

        ///<summary>
        ///向服务器发送请求
        ///</summary>
        ///<param name="URL">请求URL地址</param>
        ///<param name="method">POST或GET</param>
        private void SendRequestData(string URL, string method) {
            SendRequestData(URL, method, false);
        }


        ///<summary>
        ///获取请求头字节数组
        ///</summary>
        ///<param name="request">POST或GET请求</param>
        ///<returns>请求头字节数组</returns>
        private byte[] GetRequestHeaders(string request) {
            requestHeaders.Add("Accept", "*/*");
            requestHeaders.Add("Accept-Language", "zh-cn");
            requestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36");
            string headers = request + "\r\n";
            foreach (string key in requestHeaders) {
                headers += key + ":" + requestHeaders[key] + "\r\n";
            }
            //有Cookie就带上Cookie
            if (cookie != "") {
                headers += "Cookie:" + cookie + "\r\n";
            }
            //空行，请求头结束
            headers += "\r\n";
            strRequestHeaders = headers;
            requestHeaders.Clear();
            return encoding.GetBytes(headers);
        }



        ///<summary>
        ///获取服务器响应文本
        ///</summary>
        ///<returns>服务器响应文本</returns>
        private string GetHtml() {
            MemoryStream ms = new MemoryStream();
            SaveNetworkStream(ms);//将网络流保存到内存流
            StreamReader sr = new StreamReader(ms, encoding);
            respHtml = sr.ReadToEnd();
            sr.Close();
            ms.Close();
            return respHtml;
        }

        ///<summary>
        ///将网络流保存到指定流
        ///</summary>
        ///<param name="toStream">保存位置</param>
        ///<param name="needProgress">是否显示进度</param>
        private void SaveNetworkStream(Stream toStream, bool showProgress) {
            //获取要保存的网络流
            NetworkStream NetStream = clientSocket.GetStream();
            byte[] buffer = new byte[RECEIVE_BUFFER_SIZE];
            int count = 0, startIndex = 0;
            MemoryStream ms = new MemoryStream();
            MemoryStream ms2 = new MemoryStream();
            for (int i = 0; i < 3; i++) {
                count = NetStream.Read(buffer, 0, 500);
                ms.Write(buffer, 0, count);
            }
            if (ms.Length == 0) {
                NetStream.Close();
                throw new Exception("远程服务器没有响应");
            }
            buffer = ms.GetBuffer();
            count = (int)ms.Length;
            GetResponseHeader(buffer, out startIndex);//分析响应，获取响应头和响应实体
            count -= startIndex;
            ms2.Write(buffer, startIndex, count);
            DownloadEventArgs e = new DownloadEventArgs();
            if (responseHeaders["Content-Length"] != null) {
                e.totalBytes = long.Parse(responseHeaders["Content-Length"]);
            } else {
                e.totalBytes = -1;
            }
            //启动计时器
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            timer.Start();
            do {
                //如果取消就退出
                if (isCanceled) {
                    break;
                }
                //显示下载进度
                if (showProgress) {
                    e.bytesReceived += count;
                    e.ReceiveProgress = (double)e.bytesReceived / (double)e.totalBytes;
                    byte[] tempBuffer = new byte[count];
                    Array.Copy(buffer, startIndex, tempBuffer, 0, count);
                    e.receivedBuffer = tempBuffer;
                    double t = (timer.ElapsedMilliseconds + 0.1) / 1000;
                    e.receiveSpeed = (double)e.bytesReceived / t;
                    startIndex = 0;
                    if (DownloadProgressChanged != null) {
                        DownloadProgressChanged(this, e);
                    }
                }
                //读取网路数据到缓冲区
                count = NetStream.Read(buffer, 0, buffer.Length);
                //将缓存区数据保存到指定流
                ms2.Write(buffer, 0, count);
            } while (count > 0);
            timer.Stop();//关闭计时器
            if (responseHeaders["Content-Length"] != null) {
                ms2.SetLength(long.Parse(responseHeaders["Content-Length"]));
            }
            //else
            //{
            //    toStream.SetLength(toStream.Length);
            //    responseHeaders.Add("Content-Length", toStream.Length.ToString());//添加响应标头
            //}
            ms2.Position = 0;

            // 处理Transfer-Encoding: chunked的形式
            if (responseHeaders["Transfer-Encoding"] != null && responseHeaders["Transfer-Encoding"] == "chunked") {

                var msbuffer = ms2.GetBuffer();
                int i, start = 0, end = msbuffer.Length;
                for (i = start; i < end; i++) {
                    if (msbuffer[i] == 13 && msbuffer[i + 1] == 10) {
                        var buf = new char[i - start];
                        Array.Copy(msbuffer, start, buf, 0, i - start);
                        var sb2 = new StringBuilder();
                        sb2.Append(buf);
                        var length = int.Parse(sb2.ToString(), System.Globalization.NumberStyles.AllowHexSpecifier);
                        if (length == 0) {
                            break;
                        }
                        toStream.Write(msbuffer, i + 2, length);
                        start = i + length + 4;
                        i = start - 1;
                    }
                }
                responseHeaders["Transfer-Encoding"] = null;
                responseHeaders["Content-Length"] = toStream.Length.ToString();
            } else {
                toStream.Write(ms2.GetBuffer(), 0, Convert.ToInt32(ms2.Length));
            }
            toStream.Position = 0;

            //关闭网络流和网络连接
            NetStream.Close();
            ms2.Close();
            clientSocket.Close();
        }


        ///<summary>
        ///将网络流保存到指定流
        ///</summary>
        ///<param name="toStream">保存位置</param>
        private void SaveNetworkStream(Stream toStream) {
            SaveNetworkStream(toStream, false);
        }



        ///<summary>
        ///分析响应流，去掉响应头
        ///</summary>
        ///<param name="buffer"></param>
        private void GetResponseHeader(byte[] buffer, out int startIndex) {
            responseHeaders.Clear();
            string html = encoding.GetString(buffer);
            StringReader sr = new StringReader(html);
            int start = html.IndexOf("\r\n\r\n") + 4;//找到空行位置
            strResponseHeaders = html.Substring(0, start);//获取响应头文本
                                                          //获取响应状态码
                                                          //
            if (sr.Peek() > -1) {
                //读第一行字符串
                string line = sr.ReadLine();
                //分析此行字符串,获取服务器响应状态码
                Match M = RE.Match(line, @"\d\d\d");
                if (M.Success) {
                    statusCode = int.Parse(M.Value);
                }
            }
            //获取响应头
            //
            while (sr.Peek() > -1) {
                //读一行字符串
                string line = sr.ReadLine();
                //若非空行
                if (line != "") {
                    //分析此行字符串，获取响应标头
                    Match M = RE.Match(line, "([^:]+):(.+)");
                    if (M.Success) {
                        try {        //添加响应标头到集合
                            responseHeaders.Add(M.Groups[1].Value.Trim(), M.Groups[2].Value.Trim());
                        } catch { }
                        //获取Cookie
                        if (M.Groups[1].Value == "Set-Cookie") {
                            M = RE.Match(M.Groups[2].Value, "[^=]+=[^;]+");
                            cookie += M.Value.Trim() + ";";
                        }
                    }
                }
                //若是空行，代表响应头结束响应实体开始。（响应头和响应实体间用一空行隔开）
                else {
                    //如果响应头中没有实体大小标头，尝试读响应实体第一行获取实体大小
                    if (responseHeaders["Content-Length"] == null && sr.Peek() > -1) {
                        //读响应实体第一行
                        line = sr.ReadLine();
                        //分析此行看是否包含实体大小
                        Match M = RE.Match(line, "~[0-9a-fA-F]{1,15}");
                        if (M.Success) {
                            //将16进制的实体大小字符串转换为10进制
                            int length = int.Parse(M.Value, System.Globalization.NumberStyles.AllowHexSpecifier);
                            responseHeaders.Add("Content-Length", length.ToString());//添加响应标头
                            strResponseHeaders += M.Value + "\r\n";
                        }
                    }
                    break;//跳出循环
                }//End If
            }//End While
            sr.Close();
            //实体开始索引
            startIndex = encoding.GetBytes(strResponseHeaders).Length;
        }


        ///<summary>
        ///取消上传或下载,要继续开始请调用Start方法
        ///</summary>
        public void Cancel() {
            isCanceled = true;
        }

        ///<summary>
        ///启动上传或下载，要取消请调用Cancel方法
        ///</summary>
        public void Start() {
            isCanceled = false;
        }

        //*************************************************************
        //以下为属性
        //*************************************************************

        ///<summary>
        ///获取或设置请求头
        ///</summary>
        public WebHeaderCollection RequestHeaders {
            set {
                requestHeaders = value;
            }
            get {
                return requestHeaders;
            }
        }

        ///<summary>
        ///获取响应头集合
        ///</summary>
        public WebHeaderCollection ResponseHeaders {
            get {
                return responseHeaders;
            }
        }

        ///<summary>
        ///获取请求头文本
        ///</summary>
        public string StrRequestHeaders {
            get {
                return strRequestHeaders;
            }
        }

        ///<summary>
        ///获取响应头文本
        ///</summary>
        public string StrResponseHeaders {
            get {
                return strResponseHeaders;
            }
        }

        ///<summary>
        ///获取或设置Cookie
        ///</summary>
        public string Cookie {
            set {
                cookie = value;
            }
            get {
                return cookie;
            }
        }

        ///<summary>
        ///获取或设置编码方式(默认为系统默认编码方式)
        ///</summary>
        public Encoding Encoding {
            set {
                encoding = value;
            }
            get {
                return encoding;
            }
        }

        ///<summary>
        ///获取服务器响应文本
        ///</summary>
        public string Response {
            get {
                return respHtml;
            }
        }


        ///<summary>
        ///获取服务器响应状态码
        ///</summary>
        public int StatusCode {
            get {
                return statusCode;
            }
        }

        ///<summary>
        ///获取服务器响应状态码
        ///</summary>
        public void AddHeader(String key, String value) {
            requestHeaders.Add(key, value);
        }
        #region 工具函数
        // Fields
        private static char[] _htmlEntityEndingChars = new char[] { ';', '&' };

        //Methods
        public static string UrlEncode(string str) {
            if (str == null) {
                return null;
            }
            Encoding e = Encoding.UTF8;//你的编码 也可能是GBK
            return Encoding.ASCII.GetString(UrlEncodeToBytes(str, e));
        }

        public static byte[] UrlEncodeToBytes(string str, Encoding e) {
            if (str == null) {
                return null;
            }
            byte[] bytes = e.GetBytes(str);
            return UrlEncode(bytes, 0, bytes.Length);
        }

        static byte[] UrlEncode(byte[] bytes, int offset, int count) {
            int num = 0;
            int num2 = 0;
            for (int i = 0; i < count; i++) {
                char ch = (char)bytes[offset + i];
                if (ch == ' ') {
                    num++;
                } else if (!IsUrlSafeChar(ch)) {
                    num2++;
                }
            }
            if ((num == 0) && (num2 == 0)) {
                return bytes;
            }
            byte[] buffer = new byte[count + (num2 * 2)];
            int num4 = 0;
            for (int j = 0; j < count; j++) {
                byte num6 = bytes[offset + j];
                char ch2 = (char)num6;
                if (IsUrlSafeChar(ch2)) {
                    buffer[num4++] = num6;
                } else if (ch2 == ' ') {
                    buffer[num4++] = 0x2b;
                } else {
                    buffer[num4++] = 0x25;
                    buffer[num4++] = (byte)IntToHex((num6 >> 4) & 15);
                    buffer[num4++] = (byte)IntToHex(num6 & 15);
                }
            }
            return buffer;
        }

        public static bool IsUrlSafeChar(char ch) {
            if ((((ch >= 'a') && (ch <= 'z')) || ((ch >= 'A') && (ch <= 'Z'))) || ((ch >= '0') && (ch <= '9'))) {
                return true;
            }
            switch (ch) {
                case '(':
                case ')':
                case '*':
                case '-':
                case '.':
                case '_':
                case '!':
                    return true;
            }
            return false;
        }
        public static char IntToHex(int n) {
            if (n <= 9) {
                return (char)(n + 0x30);
            }
            return (char)((n - 10) + 0x61);
        }
        static public string UrlDecode(string value) {
            if (value == null) {
                return null;
            }
            Encoding encoding = Encoding.UTF8;//你的编码 也可能是GBK
            int length = value.Length;
            UrlDecoder decoder = new UrlDecoder(length, encoding);
            for (int i = 0; i < length; i++) {
                char ch = value[i];
                if (ch == '+') {
                    ch = ' ';
                } else if ((ch == '%') && (i < (length - 2))) {
                    if ((value[i + 1] == 'u') && (i < (length - 5))) {
                        int num3 = HexToInt(value[i + 2]);
                        int num4 = HexToInt(value[i + 3]);
                        int num5 = HexToInt(value[i + 4]);
                        int num6 = HexToInt(value[i + 5]);
                        if (((num3 < 0) || (num4 < 0)) || ((num5 < 0) || (num6 < 0))) {
                            goto Label_010B;
                        }
                        ch = (char)((((num3 << 12) | (num4 << 8)) | (num5 << 4)) | num6);
                        i += 5;
                        decoder.AddChar(ch);
                        continue;
                    }
                    int num7 = HexToInt(value[i + 1]);
                    int num8 = HexToInt(value[i + 2]);
                    if ((num7 >= 0) && (num8 >= 0)) {
                        byte b = (byte)((num7 << 4) | num8);
                        i += 2;
                        decoder.AddByte(b);
                        continue;
                    }
                }
                Label_010B:
                if ((ch & 0xff80) == 0) {
                    decoder.AddByte((byte)ch);
                } else {
                    decoder.AddChar(ch);
                }
            }
            return decoder.GetString();
        }

        public static int HexToInt(char h) {
            if ((h >= '0') && (h <= '9')) {
                return (h - '0');
            }
            if ((h >= 'a') && (h <= 'f')) {
                return ((h - 'a') + 10);
            }
            if ((h >= 'A') && (h <= 'F')) {
                return ((h - 'A') + 10);
            }
            return -1;
        }
        ///////////////////////////
        public static string HtmlEncode(string value) {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }

            StringWriter output = new StringWriter(CultureInfo.InvariantCulture);
            HtmlEncode(value, output);
            return output.ToString();
        }

        public static unsafe void HtmlEncode(string value, TextWriter output) {
            if (value != null) {
                if (output == null) {
                    throw new ArgumentNullException("output");
                }
                int num = IndexOfHtmlEncodingChars(value, 0);
                if (num == -1) {
                    output.Write(value);
                } else {
                    int num2 = value.Length - num;
                    fixed (char* str = (value.ToCharArray()))
                    {
                        char* chPtr = str;
                        char* chPtr2 = chPtr;
                        while (num-- > 0) {
                            chPtr2++;
                            output.Write(chPtr2[0]);
                        }
                        while (num2-- > 0) {
                            chPtr2++;
                            char ch = chPtr2[0];
                            if (ch <= '>') {
                                switch (ch) {
                                    case '&':
                                        {
                                            output.Write("&amp;");
                                            continue;
                                        }
                                    case '\'':
                                        {
                                            output.Write("'");
                                            continue;
                                        }
                                    case '"':
                                        {
                                            output.Write("&quot;");
                                            continue;
                                        }
                                    case '<':
                                        {
                                            output.Write("&lt;");
                                            continue;
                                        }
                                    case '>':
                                        {
                                            output.Write("&gt;");
                                            continue;
                                        }
                                }
                                output.Write(ch);
                                continue;
                            }
                            if ((ch >= '\x00a0') && (ch < 'Ā')) {
                                output.Write("&#");
                                output.Write(ch.ToString(NumberFormatInfo.InvariantInfo));
                                output.Write(';');
                            } else {
                                output.Write(ch);
                            }
                        }
                    }
                }
            }
        }
        public static string HtmlDecode(string value) {
            if (string.IsNullOrEmpty(value)) {
                return value;
            }

            StringWriter output = new StringWriter(CultureInfo.InvariantCulture);
            HtmlDecode(value, output);
            return output.ToString();
        }

        public static void HtmlDecode(string value, TextWriter output) {
            if (value != null) {
                if (output == null) {
                    throw new ArgumentNullException("output");
                }
                if (value.IndexOf('&') < 0) {
                    output.Write(value);
                } else {
                    int length = value.Length;
                    for (int i = 0; i < length; i++) {
                        char ch = value[i];
                        if (ch == '&') {
                            int num3 = value.IndexOfAny(_htmlEntityEndingChars, i + 1);
                            if ((num3 > 0) && (value[num3] == ';')) {
                                string entity = value.Substring(i + 1, (num3 - i) - 1);
                                if ((entity.Length > 1) && (entity[0] == '#')) {
                                    ushort num4;
                                    if ((entity[1] == 'x') || (entity[1] == 'X')) {
                                        ushort.TryParse(entity.Substring(2), NumberStyles.AllowHexSpecifier, (IFormatProvider)NumberFormatInfo.InvariantInfo, out num4);
                                    } else {
                                        ushort.TryParse(entity.Substring(1), NumberStyles.Integer, (IFormatProvider)NumberFormatInfo.InvariantInfo, out num4);
                                    }
                                    if (num4 != 0) {
                                        ch = (char)num4;
                                        i = num3;
                                    }
                                } else {
                                    i = num3;
                                    char ch2 = HtmlEntities.Lookup(entity);
                                    if (ch2 != '\0') {
                                        ch = ch2;
                                    } else {
                                        output.Write('&');
                                        output.Write(entity);
                                        output.Write(';');
                                        goto Label_0117;
                                    }
                                }
                            }
                        }
                        output.Write(ch);
                        Label_0117:;
                    }
                }
            }
        }
        private static unsafe int IndexOfHtmlEncodingChars(string s, int startPos) {
            int num = s.Length - startPos;
            fixed (char* str = (s.ToCharArray()))
            {
                char* chPtr = str;
                char* chPtr2 = chPtr + startPos;
                while (num > 0) {
                    char ch = chPtr2[0];
                    if (ch <= '>') {
                        switch (ch) {
                            case '&':
                            case '\'':
                            case '"':
                            case '<':
                            case '>':
                                return (s.Length - num);

                            case '=':
                                goto Label_0086;
                        }
                    } else if ((ch >= '\x00a0') && (ch < 'Ā')) {
                        return (s.Length - num);
                    }
                    Label_0086:
                    chPtr2++;
                    num--;
                }
            }
            return -1;
        }
        #endregion
    }

    class TimeOutSocket {
        private static bool IsConnectionSuccessful = false;
        private static Exception socketexception;
        private static ManualResetEvent TimeoutObject = new ManualResetEvent(false);

        public static TcpClient Connect(string host,int port, int timeoutMSec) {
            TimeoutObject.Reset();
            socketexception = null;

            TcpClient tcpclient = new TcpClient();

            tcpclient.BeginConnect(host, port,
                new AsyncCallback(CallBackMethod), tcpclient);

            if (TimeoutObject.WaitOne(timeoutMSec, false)) {
                if (IsConnectionSuccessful) {
                    return tcpclient;
                } else {
                    throw socketexception;
                }
            } else {
                tcpclient.Close();
                throw new TimeoutException("TimeOut Exception");
            }
        }
        private static void CallBackMethod(IAsyncResult asyncresult) {
            try {
                IsConnectionSuccessful = false;
                TcpClient tcpclient = asyncresult.AsyncState as TcpClient;

                if (tcpclient.Client != null) {
                    tcpclient.EndConnect(asyncresult);
                    IsConnectionSuccessful = true;
                }
            } catch (Exception ex) {
                IsConnectionSuccessful = false;
                socketexception = ex;
            } finally {
                TimeoutObject.Set();
            }
        }
    }
    /// <summary>
    /// class UrlDecoder
    /// </summary>
    class UrlDecoder {
        // Fields
        private int _bufferSize;
        private byte[] _byteBuffer;
        private char[] _charBuffer;
        private Encoding _encoding;
        private int _numBytes;
        private int _numChars;

        // Methods
        internal UrlDecoder(int bufferSize, Encoding encoding) {
            this._bufferSize = bufferSize;
            this._encoding = encoding;
            this._charBuffer = new char[bufferSize];
        }

        internal void AddByte(byte b) {
            if (this._byteBuffer == null) {
                this._byteBuffer = new byte[this._bufferSize];
            }
            this._byteBuffer[this._numBytes++] = b;
        }

        internal void AddChar(char ch) {
            if (this._numBytes > 0) {
                this.FlushBytes();
            }
            this._charBuffer[this._numChars++] = ch;
        }

        private void FlushBytes() {
            if (this._numBytes > 0) {
                this._numChars += this._encoding.GetChars(this._byteBuffer, 0, this._numBytes, this._charBuffer, this._numChars);
                this._numBytes = 0;
            }
        }

        internal string GetString() {
            if (this._numBytes > 0) {
                this.FlushBytes();
            }
            if (this._numChars > 0) {
                return new string(this._charBuffer, 0, this._numChars);
            }
            return string.Empty;
        }
    }
    /// <summary>
    /// class HtmlEntities
    /// </summary>
    static class HtmlEntities {
        // Fields
        private static string[] _entitiesList = new string[] {
        "\"-quot", "&-amp", "'-apos", "<-lt", ">-gt", "\x00a0-nbsp", "\x00a1-iexcl", "\x00a2-cent", "\x00a3-pound", "\x00a4-curren", "\x00a5-yen", "\x00a6-brvbar", "\x00a7-sect", "\x00a8-uml", "\x00a9-copy", "\x00aa-ordf",
        "\x00ab-laquo", "\x00ac-not", "\x00ad-shy", "\x00ae-reg", "\x00af-macr", "\x00b0-deg", "\x00b1-plusmn", "\x00b2-sup2", "\x00b3-sup3", "\x00b4-acute", "\x00b5-micro", "\x00b6-para", "\x00b7-middot", "\x00b8-cedil", "\x00b9-sup1", "\x00ba-ordm",
        "\x00bb-raquo", "\x00bc-frac14", "\x00bd-frac12", "\x00be-frac34", "\x00bf-iquest", "\x00c0-Agrave", "\x00c1-Aacute", "\x00c2-Acirc", "\x00c3-Atilde", "\x00c4-Auml", "\x00c5-Aring", "\x00c6-AElig", "\x00c7-Ccedil", "\x00c8-Egrave", "\x00c9-Eacute", "\x00ca-Ecirc",
        "\x00cb-Euml", "\x00cc-Igrave", "\x00cd-Iacute", "\x00ce-Icirc", "\x00cf-Iuml", "\x00d0-ETH", "\x00d1-Ntilde", "\x00d2-Ograve", "\x00d3-Oacute", "\x00d4-Ocirc", "\x00d5-Otilde", "\x00d6-Ouml", "\x00d7-times", "\x00d8-Oslash", "\x00d9-Ugrave", "\x00da-Uacute",
        "\x00db-Ucirc", "\x00dc-Uuml", "\x00dd-Yacute", "\x00de-THORN", "\x00df-szlig", "\x00e0-agrave", "\x00e1-aacute", "\x00e2-acirc", "\x00e3-atilde", "\x00e4-auml", "\x00e5-aring", "\x00e6-aelig", "\x00e7-ccedil", "\x00e8-egrave", "\x00e9-eacute", "\x00ea-ecirc",
        "\x00eb-euml", "\x00ec-igrave", "\x00ed-iacute", "\x00ee-icirc", "\x00ef-iuml", "\x00f0-eth", "\x00f1-ntilde", "\x00f2-ograve", "\x00f3-oacute", "\x00f4-ocirc", "\x00f5-otilde", "\x00f6-ouml", "\x00f7-divide", "\x00f8-oslash", "\x00f9-ugrave", "\x00fa-uacute",
        "\x00fb-ucirc", "\x00fc-uuml", "\x00fd-yacute", "\x00fe-thorn", "\x00ff-yuml", "Œ-OElig", "œ-oelig", "Š-Scaron", "š-scaron", "Ÿ-Yuml", "ƒ-fnof", "ˆ-circ", "˜-tilde", "Α-Alpha", "Β-Beta", "Γ-Gamma",
        "Δ-Delta", "Ε-Epsilon", "Ζ-Zeta", "Η-Eta", "Θ-Theta", "Ι-Iota", "Κ-Kappa", "Λ-Lambda", "Μ-Mu", "Ν-Nu", "Ξ-Xi", "Ο-Omicron", "Π-Pi", "Ρ-Rho", "Σ-Sigma", "Τ-Tau",
        "Υ-Upsilon", "Φ-Phi", "Χ-Chi", "Ψ-Psi", "Ω-Omega", "α-alpha", "β-beta", "γ-gamma", "δ-delta", "ε-epsilon", "ζ-zeta", "η-eta", "θ-theta", "ι-iota", "κ-kappa", "λ-lambda",
        "μ-mu", "ν-nu", "ξ-xi", "ο-omicron", "π-pi", "ρ-rho", "ς-sigmaf", "σ-sigma", "τ-tau", "υ-upsilon", "φ-phi", "χ-chi", "ψ-psi", "ω-omega", "ϑ-thetasym", "ϒ-upsih",
        "ϖ-piv", " -ensp", " -emsp", " -thinsp", "‌-zwnj", "‍-zwj", "‎-lrm", "‏-rlm", "–-ndash", "—-mdash", "‘-lsquo", "’-rsquo", "‚-sbquo", "“-ldquo", "”-rdquo", "„-bdquo",
        "†-dagger", "‡-Dagger", "•-bull", "…-hellip", "‰-permil", "′-prime", "″-Prime", "‹-lsaquo", "›-rsaquo", "‾-oline", "⁄-frasl", "€-euro", "ℑ-image", "℘-weierp", "ℜ-real", "™-trade",
        "ℵ-alefsym", "←-larr", "↑-uarr", "→-rarr", "↓-darr", "↔-harr", "↵-crarr", "⇐-lArr", "⇑-uArr", "⇒-rArr", "⇓-dArr", "⇔-hArr", "∀-forall", "∂-part", "∃-exist", "∅-empty",
        "∇-nabla", "∈-isin", "∉-notin", "∋-ni", "∏-prod", "∑-sum", "−-minus", "∗-lowast", "√-radic", "∝-prop", "∞-infin", "∠-ang", "∧-and", "∨-or", "∩-cap", "∪-cup",
        "∫-int", "∴-there4", "∼-sim", "≅-cong", "≈-asymp", "≠-ne", "≡-equiv", "≤-le", "≥-ge", "⊂-sub", "⊃-sup", "⊄-nsub", "⊆-sube", "⊇-supe", "⊕-oplus", "⊗-otimes",
        "⊥-perp", "⋅-sdot", "⌈-lceil", "⌉-rceil", "⌊-lfloor", "⌋-rfloor", "〈-lang", "〉-rang", "◊-loz", "♠-spades", "♣-clubs", "♥-hearts", "♦-diams"
     };
        private static Dictionary<string, char> _lookupTable = GenerateLookupTable();

        // Methods
        private static Dictionary<string, char> GenerateLookupTable() {
            Dictionary<string, char> dictionary = new Dictionary<string, char>(StringComparer.Ordinal);
            foreach (string str in _entitiesList) {
                dictionary.Add(str.Substring(2), str[0]);
            }
            return dictionary;
        }

        public static char Lookup(string entity) {
            char ch;
            _lookupTable.TryGetValue(entity, out ch);
            return ch;
        }
    }
}
