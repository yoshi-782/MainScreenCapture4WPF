using Microsoft.Win32;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MainScreenCapture4WPF.CaptureAPI
{
    public class CapAPI
    {
        /// <summary>
        /// すべてのモニター名取得
        /// </summary>
        public string[] MonitorsName => monitorInfo.MonitorsName.Keys.ToArray();

        /// <summary>
        /// モニター情報クラス
        /// </summary>
        private readonly MonitorInfo monitorInfo;

        /// <summary>
        /// WindowsAPI
        /// </summary>
        private readonly WindowsAPI winApi;

        /// <summary>
        /// テンポラリファイルのパス
        /// </summary>
        private readonly string tmpPath = $"{Path.GetTempPath()}プレビュー.bmp";

        /// <summary>
        /// コピー用パス
        /// </summary>
        private string fileCopyPath = "";

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CapAPI()
        {
            monitorInfo = new MonitorInfo();
            winApi = new WindowsAPI();
        }

        /// <summary>
        /// 実行されてるすべてのウィンドウのタイトル名取得
        /// </summary>
        /// <returns></returns>
        public string[] GetAllWindowTitle() => winApi.GetAllWindowTitle();

        /// <summary>
        /// 指定したウィンドウのサイズと位置を取得
        /// </summary>
        /// <param name="title">ウィンドウタイトル</param>
        /// <param name="rect">ウィンドウサイズ・位置</param>
        /// <returns>取得成功: true 取得失敗: false</returns>
        public bool GetWindowRect(string title, out Rectangle rect) => winApi.GetWindowRect(title, out rect);

        /// <summary>
        /// 指定の画面のRectangle取得
        /// </summary>
        /// <param name="screenName">画面名</param>
        /// <param name="rect">サイズ・位置</param>
        /// <returns>成功: true 失敗: false</returns>
        public bool GetScreenRect(string screenName, out Rectangle rect) => monitorInfo.GetScreenRect(screenName, out rect);

        /// <summary>
        /// キャプチャ
        /// </summary>
        /// <param name="rect">キャプチャするサイズ・位置情報</param>
        /// <returns>キャプチャイメージ</returns>
        public BitmapSource Capture(Rectangle rect)
        {
            var bmp = new Bitmap(rect.Width, rect.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(rect.Location, new System.Drawing.Point(0, 0), bmp.Size);
            BitmapSource image = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(),
                                                                       IntPtr.Zero,
                                                                       Int32Rect.Empty,
                                                                       BitmapSizeOptions.FromEmptyOptions());
            image.Freeze();
            g.Dispose();
            return image;
        }

        /// <summary>
        /// キャプチャした画像を保存
        /// </summary>
        public bool SavePicture(BitmapSource bitmap)
        {
            var sfd = new SaveFileDialog
            {
                FileName = $"スクリーンショット_{DateTime.Now:yyyyMMddhhmmss}.png",
                Filter = "PNG ファイル(*.png)|*.png|JPG ファイル(*jpg)|*jpg|BMP ファイル(*bmp)|*bmp",
            };

            if (sfd.ShowDialog() == true)
            {
                using (FileStream stream = new(sfd.FileName, FileMode.Create))
                {
                    switch (Path.GetExtension(sfd.FileName))
                    {
                        case ".jpg":
                            JpegBitmapEncoder jpgEncoder = new JpegBitmapEncoder();
                            jpgEncoder.Frames.Add(BitmapFrame.Create(bitmap));
                            jpgEncoder.Save(stream);
                            break;

                        case ".png":
                            PngBitmapEncoder pngEncoder = new PngBitmapEncoder();
                            pngEncoder.Frames.Add(BitmapFrame.Create(bitmap));
                            pngEncoder.Save(stream);
                            break;

                        case ".bmp":
                            BmpBitmapEncoder bmpEncoder = new BmpBitmapEncoder();
                            bmpEncoder.Frames.Add(BitmapFrame.Create(bitmap));
                            bmpEncoder.Save(stream);
                            break;
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// スクリーンショットの範囲内に自身のアプリが存在するか
        /// </summary>
        /// <param name="scrRct">画面のサイズ・位置</param>
        /// <param name="winRect">ウィンドウのサイズ・位置</param>
        /// <returns>範囲内: true 範囲外: false</returns>
        public bool IsDisplayRangeIn(Rectangle scrRct, Rectangle winRect)
        {
            var leftTop_leftSide = scrRct.X <= winRect.X && scrRct.Y <= winRect.Y;
            var leftTop_rightSide = (scrRct.X + scrRct.Width) >= winRect.X && (scrRct.Y + scrRct.Height) >= winRect.Y;
            var rightDown_leftSide = scrRct.X <= (winRect.X + winRect.Width) && scrRct.Y <= (winRect.Y + winRect.Height);
            var rightDown_rightSide = (scrRct.X + scrRct.Width) >= (winRect.X + winRect.Width) && (scrRct.Y + scrRct.Height) >= (winRect.Y + winRect.Height);

            // 左上の位置と右下の位置が画面の範囲内にあるかチェック
            return (leftTop_leftSide && leftTop_rightSide) || (rightDown_leftSide && rightDown_rightSide);
        }

        /// <summary>
        /// プレビュー画像の保存
        /// </summary>
        /// <param name="bitmap">画像データ</param>
        public void PreviewPicture(BitmapSource image)
        {
            // テンポラリフォルダに保存
            using (FileStream stream = new(tmpPath, FileMode.Create))
            {
                PngBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }

            //　画像を開く
            var proc = new Process();
            proc.StartInfo.FileName = tmpPath;
            proc.StartInfo.UseShellExecute = true;
            proc.Start();
        }

        /// <summary>
        /// コピー用パスのアップデート
        /// </summary>
        public void UpdateFileCopyPath()
        {
            if (fileCopyPath.Length > 0)
            {
                if (File.Exists(fileCopyPath))
                {
                    File.Delete(fileCopyPath);
                }
            }

            // パスの作成
            fileCopyPath = @$"{Path.GetTempPath()}スクリーンショット_{DateTime.Now:yyyyMMddhhmmss}.png";
        }

        /// <summary>
        /// 画像をクリップボードに保存
        /// </summary>
        public void PicCopy(BitmapSource image, bool isFileCopy)
        {
            if (isFileCopy)
            {
                UpdateFileCopyPath();

                // ファイルとしてコピー
                using (FileStream stream = new(fileCopyPath, FileMode.Create))
                {
                    // テンポラリフォルダに保存
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(stream);
                }

                var files = new StringCollection();
                files.Add(fileCopyPath);
                Clipboard.SetFileDropList(files);
            }
            else
            {
                // データとしてコピー
                Clipboard.SetImage(image);
            }
        }

        /// <summary>
        /// テンポラリフォルダの画像削除
        /// </summary>
        public void DeleteTemporaryFile()
        {
            if (File.Exists(tmpPath))
                File.Delete(tmpPath);

            if (File.Exists(fileCopyPath))
                File.Delete(fileCopyPath);
        }
    }
}
