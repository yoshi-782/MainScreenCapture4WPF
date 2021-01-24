using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MainScreenCapture4WPF
{
    class MonitorInfo
    {
        public MonitorInfo()
        {
            GetAllScreens();
        }

        /// <summary>
        /// 画面の幅
        /// </summary>
        public int Width { get; private set; }

        /// <summary>
        /// 画面の高さ
        /// </summary>
        public int Height { get; private set; }

        /// <summary>
        /// 画面の左上のポイント
        /// </summary>
        public Point UpperLeftSource { get; private set; }

        /// <summary>
        /// 画面の右下のポイント
        /// </summary>
        public Point UpperLeftDestination { get; private set; }

        /// <summary>
        /// 画面名辞書
        /// </summary>
        public Dictionary<string, string> monitorsName { get; private set; } = new Dictionary<string, string>();

        /// <summary>
        /// モニターのサイズと位置取得
        /// </summary>
        /// <param name="screenName"></param>
        /// <returns>成功/失敗</returns>
        public bool GetScreenSize(string screenName)
        {
            if (monitorsName.ContainsKey(screenName))
            {
                var deviceName = monitorsName[screenName];

                foreach (var s in Screen.AllScreens)
                {
                    if (s.DeviceName == deviceName)
                    {
                        // 画面情報設定
                        this.Width = s.Bounds.Width;
                        this.Height = s.Bounds.Height;
                        this.UpperLeftSource = new Point(s.Bounds.X, s.Bounds.Y);
                        this.UpperLeftDestination = new Point(s.Bounds.X + s.Bounds.Width,
                                                              s.Bounds.Y + s.Bounds.Height);
                        return true;
                    }
                }
            }
            
            // デバイス名なし
            this.Width = 0;
            this.Height = 0;
            this.UpperLeftSource = new Point(0, 0);
            this.UpperLeftDestination = new Point(0, 0);
            return false;
        }

        /// <summary>
        /// すべてのモニター名取得
        /// </summary>
        private void GetAllScreens()
        {
            monitorsName.Clear();
            var i = 1;
            foreach (var s in Screen.AllScreens)
            {
                var name = $"モニター{i}";
                monitorsName.Add(name, s.DeviceName);
                i++;
            }
        }
    }
}
