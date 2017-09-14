using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;

namespace KxLib.UI {
    public class KxWindow : Window {
        private readonly ResourceDictionary mWindowResouce = new ResourceDictionary();
        private readonly ControlTemplate mTemplate;

        private const int WM_NCHITTEST = 0x0084;
        private const int WM_GETMINMAXINFO = 0x0024;

        private const int CORNER_WIDTH = 12; //拐角宽度  
        private const int MARGIN_WIDTH = 4; // 边框宽度  
        private Point mMousePoint = new Point(); //鼠标坐标  
        private Button mMaxButton;
        private bool mIsShowMax;
        private bool mIsShowSkin;
        private int ControlBoxWidth;
        private SkinTypeEnum mSkinType;

        /// <summary>  
        /// 是否显示最大化按钮  
        /// </summary>  
        public bool IsShowMax {
            get {
                return mIsShowMax;
            }
            set {
                mIsShowMax = value;
            }
        }

        /// <summary>  
        /// 是否显示换肤按钮
        /// </summary>  
        public bool IsShowSkin {
            get {
                return mIsShowSkin;
            }
            set {
                mIsShowSkin = value;
            }
        }

        /// <summary>  
        /// 皮肤类型  
        /// </summary>  
        public SkinTypeEnum SkinType {
            get {
                return mSkinType;
            }
            set {
                if (mSkinType != SkinTypeEnum.Light) {
                    this.Resources.MergedDictionaries.RemoveAt(this.Resources.MergedDictionaries.Count - 1);
                }
                //this.Resources.MergedDictionaries.Clear();
                //this.Resources.MergedDictionaries.Add(mWindowResouce);
                var r = new ResourceDictionary();
                switch (value) {
                    case SkinTypeEnum.Dark:
                        r.Source = new Uri("KxLib.UI;component/Skins/Dark.xaml", UriKind.Relative);
                        this.Resources.MergedDictionaries.Add(r);
                        break;
                    case SkinTypeEnum.Blue:
                        r.Source = new Uri("KxLib.UI;component/Skins/Blue.xaml", UriKind.Relative);
                        this.Resources.MergedDictionaries.Add(r);
                        break;
                    case SkinTypeEnum.Red:
                        r.Source = new Uri("KxLib.UI;component/Skins/Red.xaml", UriKind.Relative);
                        this.Resources.MergedDictionaries.Add(r);
                        break;
                    case SkinTypeEnum.Pink:
                        r.Source = new Uri("KxLib.UI;component/Skins/Pink.xaml", UriKind.Relative);
                        this.Resources.MergedDictionaries.Add(r);
                        break;
                    case SkinTypeEnum.Image:
                        r.Source = new Uri("KxLib.UI;component/Skins/Image.xaml", UriKind.Relative);
                        this.Resources.MergedDictionaries.Add(r);
                        break;
                }
                mSkinType = value;
            }
        }

        private ImageSource mBackImage;
        /// <summary>  
        /// 背景图片  
        /// </summary>  
        public ImageSource BackImage {
            get {
                return mBackImage;
            }
            set {
                mBackImage = value;
            }
        }

        /// <summary>  
        /// 窗口  
        /// </summary>  
        public KxWindow() {
            mWindowResouce.Source = new Uri("KxLib.UI;component/Themes/AllThemes.xaml", UriKind.Relative);
            this.Resources.MergedDictionaries.Add(mWindowResouce);
            this.Style = (Style)mWindowResouce["WindowStyle"];
            var windowTemplate = (ControlTemplate)mWindowResouce["WindowTemplate"];
            this.Template = windowTemplate;
            mTemplate = windowTemplate;
            this.Loaded += new RoutedEventHandler(KxWindow_Loaded);
            this.MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
            this.MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
            SkinType = SkinTypeEnum.Light;
        }

        private void KxWindow_Loaded(object sender, RoutedEventArgs e) {
            var titleText = (TextBlock)mTemplate.FindName("TitleText", this);
            var backBorder = (Border)mTemplate.FindName("BorderBack", this);
            var headBorder = (Border)mTemplate.FindName("TitleBar", this);
            var FussWindowBorder = (Border)mTemplate.FindName("FussWindowBorder", this);
            FussWindowBorder.MouseLeftButtonDown += (s1, e1) => this.DragMove();
            ((Image)mTemplate.FindName("ImgApp", this)).Source = this.Icon;
            // 控制按钮宽度
            var controlBox = (StackPanel)mTemplate.FindName("ControlBox", this);
            ControlBoxWidth = Convert.ToInt32(controlBox.ActualWidth) + MARGIN_WIDTH * 2;
            // 标题绑定更新
            Binding b1 = new Binding("Title") { Source = this };
            titleText.SetBinding(TextBlock.TextProperty, b1);

            mMaxButton = (Button)mTemplate.FindName("MaxButton", this);
            if (!IsShowMax) {
                mMaxButton.Visibility = Visibility.Collapsed;
            } else {
                mMaxButton.Visibility = Visibility.Visible;
            }
            var mSkinButton = (Button)mTemplate.FindName("SkinButton", this);
            if (!IsShowSkin) {
                mSkinButton.Visibility = Visibility.Collapsed;
            } else {
                mSkinButton.Visibility = Visibility.Visible;
            }

            if (SkinType == SkinTypeEnum.Image) {
                backBorder.Background = new ImageBrush(mBackImage);
            }


            ((Button)mTemplate.FindName("MinButton", this)).Click += (s2, e2) => {
                this.WindowState = WindowState.Minimized;
            };
            mMaxButton.Click += (s3, e3) => {
                //SystemParameters.WorkArea
                if (this.WindowState == WindowState.Normal) {
                    this.WindowState = WindowState.Maximized;
                } else {
                    this.WindowState = WindowState.Normal;
                }
            };
            ((Button)mTemplate.FindName("CloseButton", this)).Click += (s4, e4) => this.Close();
            mSkinButton.Click += (s5, e5) => {
                SkinButton_Click(s5, e5);
            };
            var hwndSource = PresentationSource.FromVisual(this) as HwndSource;
            if (hwndSource != null) {
                hwndSource.AddHook(new HwndSourceHook(WndProc));
            }
        }

        public virtual void SkinButton_Click(object sender, RoutedEventArgs e) {
            // Add Your Code
        }

        /// <summary>  
        /// 窗口消息  
        /// </summary>  
        /// <param name="hwnd"></param>  
        /// <param name="msg"></param>  
        /// <param name="wParam"></param>  
        /// <param name="lParam"></param>  
        /// <param name="handled"></param>  
        /// <returns></returns>  
        protected virtual IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
            switch (msg) {
                case WM_NCHITTEST:
                    mMousePoint.X = (lParam.ToInt32() & 0xFFFF);
                    mMousePoint.Y = (lParam.ToInt32() >> 16);
                    handled = true;
                    //窗体为最大化时（如果最大化，Left、Top属性都会造成影响）  
                    if (this.WindowState == WindowState.Normal && IsShowMax) {
                        #region 拖拽改变窗体大小  
                        //左上角  
                        if ((mMousePoint.Y - Top <= CORNER_WIDTH) && (mMousePoint.X - Left <= CORNER_WIDTH)) {
                            return new IntPtr((int)HitTest.HTTOPLEFT);
                        }
                        //左下角  
                        if ((this.ActualHeight + this.Top - this.mMousePoint.Y <= CORNER_WIDTH) && (this.mMousePoint.X - this.Left <= CORNER_WIDTH)) {
                            return new IntPtr((int)HitTest.HTBOTTOMLEFT);
                        }
                        //右上角  
                        if ((this.mMousePoint.Y - this.Top <= CORNER_WIDTH) && (this.ActualWidth + this.Left - this.mMousePoint.X <= CORNER_WIDTH)) {
                            return new IntPtr((int)HitTest.HTTOPRIGHT);
                        }
                        //右下角  
                        if ((this.ActualWidth + this.Left - this.mMousePoint.X <= CORNER_WIDTH) && (this.ActualHeight + this.Top - this.mMousePoint.Y <= CORNER_WIDTH)) {
                            return new IntPtr((int)HitTest.HTBOTTOMRIGHT);
                        }
                        //左侧  
                        if (this.mMousePoint.X - (this.Left + 4) <= MARGIN_WIDTH) {
                            return new IntPtr((int)HitTest.HTLEFT);
                        }
                        //右侧  
                        if (this.ActualWidth + this.Left - 4 - this.mMousePoint.X <= MARGIN_WIDTH) {
                            return new IntPtr((int)HitTest.HTRIGHT);
                        }
                        //上侧  
                        if (this.mMousePoint.Y - (this.Top + 4) <= MARGIN_WIDTH) {
                            return new IntPtr((int)HitTest.HTTOP);
                        }
                        //下侧  
                        if (this.ActualHeight + this.Top - 4 - this.mMousePoint.Y <= MARGIN_WIDTH) {
                            return new IntPtr((int)HitTest.HTBOTTOM);
                        }
                        #endregion
                        //正常情况下移动窗体  
                        if (this.mMousePoint.Y - this.Top > 0 && this.mMousePoint.Y - this.Top < 32 && this.Left + this.ActualWidth - mMousePoint.X > ControlBoxWidth) {
                            return new IntPtr((int)HitTest.HTCAPTION);
                        }
                    }
                    //最大化时移动窗体，让窗体正常化  
                    if (this.WindowState == WindowState.Maximized) {
                        if (this.mMousePoint.Y < 40 && this.ActualWidth - mMousePoint.X > 110) {
                            return new IntPtr((int)HitTest.HTCAPTION);
                        }
                    }
                    //将其他区域设置为客户端，避免鼠标事件被屏蔽  
                    return new IntPtr((int)HitTest.HTCLIENT);
            }
            return IntPtr.Zero;
        }
    }

    /// <summary>  
    /// 包含了鼠标的各种消息  
    /// </summary>  
    public enum HitTest : int {
        /// <summary>  
        /// HTERROR  
        /// </summary>  
        HTERROR = -2,
        /// <summary>  
        /// HTTRANSPARENT  
        /// </summary>  
        HTTRANSPARENT = -1,
        /// <summary>  
        /// HTNOWHERE  
        /// </summary>  
        HTNOWHERE = 0,
        /// <summary>  
        /// HTCLIENT  
        /// </summary>  
        HTCLIENT = 1,
        /// <summary>  
        /// HTCAPTION  
        /// </summary>  
        HTCAPTION = 2,
        /// <summary>  
        /// HTSYSMENU  
        /// </summary>  
        HTSYSMENU = 3,
        /// <summary>  
        /// HTGROWBOX  
        /// </summary>  
        HTGROWBOX = 4,
        /// <summary>  
        /// HTSIZE  
        /// </summary>  
        HTSIZE = HTGROWBOX,
        /// <summary>  
        /// HTMENU  
        /// </summary>  
        HTMENU = 5,
        /// <summary>  
        /// HTHSCROLL  
        /// </summary>  
        HTHSCROLL = 6,
        /// <summary>  
        /// HTVSCROLL  
        /// </summary>  
        HTVSCROLL = 7,
        /// <summary>  
        /// HTMINBUTTON  
        /// </summary>  
        HTMINBUTTON = 8,
        /// <summary>  
        /// HTMAXBUTTON  
        /// </summary>  
        HTMAXBUTTON = 9,
        /// <summary>  
        /// HTLEFT  
        /// </summary>  
        HTLEFT = 10,
        /// <summary>  
        /// HTRIGHT  
        /// </summary>  
        HTRIGHT = 11,
        /// <summary>  
        /// HTTOP  
        /// </summary>  
        HTTOP = 12,
        /// <summary>  
        /// HTTOPLEFT  
        /// </summary>  
        HTTOPLEFT = 13,
        /// <summary>  
        /// HTTOPRIGHT  
        /// </summary>  
        HTTOPRIGHT = 14,
        /// <summary>  
        /// HTBOTTOM  
        /// </summary>  
        HTBOTTOM = 15,
        /// <summary>  
        /// HTBOTTOMLEFT  
        /// </summary>  
        HTBOTTOMLEFT = 16,
        /// <summary>  
        /// HTBOTTOMRIGHT  
        /// </summary>  
        HTBOTTOMRIGHT = 17,
        /// <summary>  
        /// HTBORDER  
        /// </summary>  
        HTBORDER = 18,
        /// <summary>  
        /// HTREDUCE  
        /// </summary>  
        HTREDUCE = HTMINBUTTON,
        /// <summary>  
        /// HTZOOM  
        /// </summary>  
        HTZOOM = HTMAXBUTTON,
        /// <summary>  
        /// HTSIZEFIRST  
        /// </summary>  
        HTSIZEFIRST = HTLEFT,
        /// <summary>  
        /// HTSIZELAST  
        /// </summary>  
        HTSIZELAST = HTBOTTOMRIGHT,
        /// <summary>  
        /// HTOBJECT  
        /// </summary>  
        HTOBJECT = 19,
        /// <summary>  
        /// HTCLOSE  
        /// </summary>  
        HTCLOSE = 20,
        /// <summary>  
        /// HTHELP  
        /// </summary>  
        HTHELP = 21
    }
    /// <summary>  
    /// 皮肤类型  
    /// </summary>  
    public enum SkinTypeEnum {
        /// <summary>
        /// 亮色调
        /// </summary>
        Light,
        /// <summary>
        /// 黑色调
        /// </summary>
        Dark,
        /// <summary>
        /// 蓝色调
        /// </summary>
        Blue,
        /// <summary>
        /// 红色调
        /// </summary>
        Red,
        /// <summary>
        /// 粉色调
        /// </summary>
        Pink,
        /// <summary>
        /// 背景图片
        /// </summary>
        Image
    }
}
