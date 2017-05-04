using System;
using System.Windows.Forms;
using System.IO;
using KxDotNetLib.FileList;
using System.Collections.Generic;
using KxDotNetLib.Utilities;
using LitJson;

namespace GeneUpdateList {
    public partial class Form1 : Form {

        List<UpdateFile> ufList =new List<UpdateFile>();
        List<string> FileCheckedList;
        private string workPlace;
        private string jsonSavePath;
        private int nodeCount,nowNode;
        private bool thdRunning;
        public Form1() {
            InitializeComponent();
        }
        private void init() {
            Setting.WorkPlace = workPlace;
            Setting.Save();
            this.Text = Application.ProductName + " - " + workPlace;
            lblStatus.Text = workPlace;
            tvList.Nodes.Clear();
            if(File.Exists(workPlace + "\\.checkedlist")) {
                FileCheckedList = JsonMapper.ToObject<List<string>>(FileHelper.LoadTxt(workPlace + "\\.checkedlist"));
            } else {
                FileCheckedList = null;
            }
            if (FileCheckedList == null) {
                FileCheckedList= new List<string>();
            }
            FileHelper.SaveTxt(workPlace + "\\.checkedlist", JsonMapper.ToJson(FileCheckedList));
            TreeNode tn = tvList.Nodes.Add(@"\", "目录");
            nodeCount = 1;
            if (FileCheckedList.Contains(@"\") || FileCheckedList.Count==0) {
                tn.Checked = true;
            }
            
            DirectoryInfo Dir = new DirectoryInfo(workPlace);
            openDir(tn, Dir);
            tn.Expand();
        }
        private void openDir(TreeNode tn, DirectoryInfo di) {
            string name = di.FullName.Substring(workPlace.Length);
            TreeNode nexttn;
            if (name.Length == 0) {
                nexttn = tn;
            } else {
                nexttn = tn.Nodes.Add(name, di.Name);
                nodeCount++;
                if (FileCheckedList.Contains(name) || FileCheckedList.Count == 0) {
                    nexttn.Checked = true;
                }
                if (FileCheckedList.Contains("#"+name)) {
                    nexttn.Expand();
                }
            }
            foreach (DirectoryInfo d in di.GetDirectories()) {
                openDir(nexttn, d);
            }
            foreach (FileInfo f in di.GetFiles("*.*", SearchOption.TopDirectoryOnly)) {
                string name2 = f.FullName.Substring(workPlace.Length);
                if (name2.Equals("\\.checkedlist")) {
                    continue;
                }
                TreeNode tn2= nexttn.Nodes.Add(name2, f.Name);
                nodeCount++;
                if (FileCheckedList.Contains(name2) || FileCheckedList.Count == 0) {
                    tn2.Checked = true;
                }
            }
        }
        private void menuOpenFolder_Click(object sender, EventArgs e) {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "请选择目录";
            dialog.SelectedPath = workPlace;
            if (dialog.ShowDialog() == DialogResult.OK) {
                string foldPath = dialog.SelectedPath;
                workPlace = foldPath;
                init();
            }
        }

        private void Form1_Load(object sender, EventArgs e) {
            tvList.CheckBoxes = true;
            this.Text = Application.ProductName;
            Setting.Load();
            if (Setting.WorkPlace.Length > 0) {
                workPlace = Setting.WorkPlace;
                init();
            }
        }

        private void menuExit_Click(object sender, EventArgs e) {
            this.Close();
        }

        private void menuRefresh_Click(object sender, EventArgs e) {
            init();
        }

        private void menuMakeList_Click(object sender, EventArgs e) {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.InitialDirectory = workPlace;
            dialog.Title = "请选择保存位置";
            dialog.FileName = "list.json";
            if (dialog.ShowDialog() == DialogResult.OK) {
                jsonSavePath = dialog.FileName;
                pbStatus.Maximum = nodeCount;
                pbStatus.Value = 0;
                lblStatus.Text = "正在生成列表 0%";
                //Thread thd = new Thread(makeList);
                //thd.Start();
                makeList();
                /*while (thdRunning || thd.ThreadState==ThreadState.Running) {
                    Thread.Sleep(111);
                }*/
                String json = JsonMapper.ToJson(ufList);

                FileHelper.SaveTxt(jsonSavePath, json);
                //FileHelper.SaveTxt(filename+".gzip", StringHelper.Zip(json));
                FileHelper.SaveTxt(workPlace + "\\.checkedlist", JsonMapper.ToJson(FileCheckedList));
                
                pbStatus.Value = nodeCount;
                lblStatus.Text = "导出成功 " + jsonSavePath;
            }
        }
        private void makeList() {

            if (tvList.InvokeRequired) {
                tvList.BeginInvoke(new Action(() => {
                    makeList();
                }));
            } else {
                thdRunning = true;
                nowNode = 0;
                ufList.Clear();
                FileCheckedList.Clear();
                TreeNodeCollection tnc = tvList.Nodes;
                foreach (TreeNode tn in tnc) {
                    expandNode(tn);
                }
                thdRunning = false;
            }
        }
        private void expandNode(TreeNode tnp) {
            nowNode++;
            pbStatus.Value = nowNode;
            lblStatus.Text = "正在生成列表 " + (100 * nowNode / nodeCount) + "%";
            if (tnp.Checked) {
                string path = workPlace + tnp.Name;
                if (File.Exists(path)) {
                    // 是文件
                    UpdateFile uf = new UpdateFile();
                    uf.path = tnp.Name;
                    uf.md5 = FileHelper.GetMD5HashFromFile(path);
                    uf.size = new FileInfo(path).Length;
                    ufList.Add(uf);
                    FileCheckedList.Add(tnp.Name);
                } else if (Directory.Exists(path)) {
                    UpdateFile uf = new UpdateFile();
                    uf.path = tnp.Name;
                    uf.md5 = "";
                    uf.size = -1;
                    ufList.Add(uf);

                    FileCheckedList.Add(tnp.Name);

                    foreach (TreeNode tn in tnp.Nodes) {
                        expandNode(tn);
                    }
                } else {
                    // 都不是
                }
            }
            if (tnp.IsExpanded) {
                FileCheckedList.Add("#" + tnp.Name);
            }
            
        }

        private void tvList_AfterCheck(object sender, TreeViewEventArgs e) {
            if (e.Action != TreeViewAction.ByMouse) {
                return;
            }
            CheckNode(e.Node, e.Node.Checked,false);
        }
        private void CheckNode(TreeNode tnp,bool check,bool onlyParent) {
            tnp.Checked = check;
            if(check && tnp.Parent!=null && tnp.Parent.Checked == false) {
                CheckNode(tnp.Parent, true, true);
            }
            if (onlyParent) {
                return;
            }
            foreach (TreeNode tn in tnp.Nodes) {
                CheckNode(tn, check,false);
            }
        }

    }
}
