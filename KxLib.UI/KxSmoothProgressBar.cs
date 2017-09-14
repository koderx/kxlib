using System;
using System.Threading;
using System.Windows.Controls;

namespace KxLib.UI {
    public delegate void EventHandler(object sender, EventArgs e);
    /// <summary>
    /// WPF中使用的平滑进度条
    /// </summary>
    public class KxSmoothProgressBar {
        private delegate void InvokeCallback(int value);
        private ProgressBar target;
        private Thread animThread;
        private int maxValue;
        private int speed;
        private const int DEF_MAX = 10000;
        private int n_value;
        private int t_value;
        private int t_time;
        private bool right = true;
        public EventHandler Complete;

        public KxSmoothProgressBar(ProgressBar target) : this(target, 100) { }

        public KxSmoothProgressBar(ProgressBar target, int maxValue) {
            this.target = target;
            SetMaxValue(maxValue);
            target.Minimum = 0;
            target.Maximum = DEF_MAX;
            target.Value = 0;
            n_value = 0;
            animThread = new Thread(run);
            speed = 5;
        }
        public void SetCompleteCallback(EventHandler Complete) {
            this.Complete = Complete;
        }
        public void SetMaxValue(int maxValue) {
            if (maxValue < 0) {
                maxValue = 0;
            }
            this.maxValue = maxValue;
        }
        /// <summary>
        /// 每毫秒走多少格 一共10000格
        /// </summary>
        /// <param name="value">速度 最小1 最大100</param>
        public void SetSpeed(int value) {
            if (value > DEF_MAX) {
                value = DEF_MAX;
            } else if (value < 1) {
                value = 1;
            }
            this.speed = value;
        }
        public void SetValue(int value) {
            if (value > maxValue) {
                value = maxValue;
            } else if (value < 0) {
                value = 0;
            }
            value = Convert.ToInt32(DEF_MAX * value / maxValue);
            t_value = value;
            // 是否往右走
            right = (t_value > n_value);
            t_time = Environment.TickCount + (right ? t_value - n_value : n_value - t_value) / speed;
            if (!animThread.IsAlive) {
                animThread = new Thread(run);
                animThread.Start();
            }
        }
        public void SetValueNow(int value) {
            if (value > maxValue) {
                value = maxValue;
            } else if (value < 0) {
                value = 0;
            }
            value = Convert.ToInt32(DEF_MAX * value / maxValue);
            if (animThread.IsAlive) {
                t_time = 0;
                t_value = value;
            }
            _SetValue(value);
        }
        private void _SetValue(int value) {
            target.Dispatcher.Invoke(new Action(() => {
                target.Value = value;
                n_value = value;
            }));
        }
        private void run() {
            int d;
            while ((d = t_time - Environment.TickCount) > 0) {
                _SetValue(t_value - d * speed * (right ? 1 : -1));
                Thread.Sleep(1);
            }
            _SetValue(t_value);
            if (Complete != null) {
                Complete(null, EventArgs.Empty);
            }
        }
    }
}
