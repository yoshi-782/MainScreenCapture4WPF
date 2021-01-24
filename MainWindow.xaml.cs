using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace MainScreenCapture4WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 保存するか
        /// </summary>
        private bool isSave = false;

        /// <summary>
        /// キャプチャ履歴のバインド用クラス
        /// </summary>
        private class HistoryImageList
        {
            public BitmapSource HistoryImage { get; set; }
        }

        /// <summary>
        /// キャプチャ履歴用バインドデータ
        /// </summary>
        private readonly ObservableCollection<HistoryImageList> historyImgList = new ObservableCollection<HistoryImageList>();

        /// <summary>
        /// モニター一覧のバインドデータ
        /// </summary>
        private ObservableCollection<string> screenList;

        /// <summary>
        /// テンポラリファイルのパス
        /// </summary>
        private readonly string tmpPath = Path.GetTempPath() + "プレビュー.bmp";

        /// <summary>
        /// コピー用パス
        /// </summary>
        private string fileCopyPath = "";

        /// <summary>
        /// モニター情報クラス
        /// </summary>
        private MonitorInfo monitorInfo;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // 初回画面表示時処理
            this.ContentRendered += (s, e) =>
            {
                topMastCheckButton.Click += (s, e) => this.Topmost = (bool)topMastCheckButton.IsChecked;
                this.historyList.ItemsSource = historyImgList;
                
                monitorInfo = new MonitorInfo();
                screenList = new ObservableCollection<string>(monitorInfo.monitorsName.Keys);
                screenList.Insert(0, "モニターを選択");
                this.screensName.ItemsSource = screenList;
                screensName.SelectedIndex = 0;
            };
        }

        /// <summary>
        /// キャプチャボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CapButton_Click(object sender, RoutedEventArgs e)
        {
            if (screensName.SelectedItem.ToString() == "モニターを選択")
            {
                MessageBox.Show("撮影する画面を選択してください。",
                                this.Title,
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                return;
            }

            if (!monitorInfo.GetScreenSize(screensName.SelectedItem?.ToString()))
            {
                MessageBox.Show("指定したモニターが存在しません。", 
                                this.Title, 
                                MessageBoxButton.OK, 
                                MessageBoxImage.Error);
                statusText.Text = "画面の撮影に失敗しました。";
                return;
            }

            var text = $"画面を撮影しました。{((bool)fileCopyCheckButton.IsChecked ? " 画像ファイルコピー済み" : " 画像データコピー済み")}";
            var bmp = new Bitmap(monitorInfo.Width, monitorInfo.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var g = Graphics.FromImage(bmp);
            if (IsDisplayRangeIn())
            {
                // 400ミリ秒非表示、その間スクショする
                this.Hide();
                DispatcherTimer timer = new DispatcherTimer();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 400);
                void timerEvent(object s, EventArgs ev)
                {
                    g.CopyFromScreen(monitorInfo.UpperLeftSource, new System.Drawing.Point(0, 0), bmp.Size);
                    var image = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(),
                                                                    IntPtr.Zero,
                                                                    Int32Rect.Empty,
                                                                    BitmapSizeOptions.FromEmptyOptions());
                    image.Freeze();
                    g.Dispose();

                    if (capImage.Source == null)
                    {
                        // 初回は履歴に挿入しない
                        capImage.Source = image;
                    }
                    else
                    {
                        historyImgList.Insert(0, new HistoryImageList { HistoryImage = (BitmapSource)capImage.Source });
                        capImage.Source = image;
                    }
                    
                    PicCopy(image);
                    isSave = false;
                    statusText.Text = text;
                    
                    this.Show();
                    timer.Stop();
                    timer.Tick -= timerEvent;
                };

                timer.Tick += timerEvent;
                timer.Start();
            }
            else
            {
                g.CopyFromScreen(monitorInfo.UpperLeftSource, new System.Drawing.Point(0, 0), bmp.Size);
                var image = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(),
                                                                IntPtr.Zero,
                                                                Int32Rect.Empty,
                                                                BitmapSizeOptions.FromEmptyOptions());
                image.Freeze();
                g.Dispose();

                if (capImage.Source == null)
                {
                    // 初回は履歴に挿入しない
                    capImage.Source = image;
                }
                else
                {
                    // 履歴挿入
                    historyImgList.Insert(0, new HistoryImageList { HistoryImage = (BitmapSource)capImage.Source });
                    capImage.Source = image;
                }

                PicCopy(image);
                isSave = false;
                statusText.Text = text;

                
            }
        }

        /// <summary>
        /// 画像をコピーボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void copyButton_Click(object sender, RoutedEventArgs e)
        {
            if (capImage.Source != null)
            {
                PicCopy((BitmapSource)capImage.Source);
                statusText.Text = (bool)fileCopyCheckButton.IsChecked ? "画像をファイルとしてコピーしました。" : "画像データをコピーしました。";
            }
        }

        /// <summary>
        /// 保存ボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (capImage.Source == null)
                return;

            if (isSave)
            {
                statusText.Text = "既に保存済みです。";
                return;
            }

            SavePicture((BitmapSource)capImage.Source);
        }

        /// <summary>
        /// プレビューボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            if (capImage.Source != null)
            {
                PicPreview((BitmapSource)capImage.Source);
                statusText.Text = "画像を開きます。";
            }
        }

        /// <summary>
        /// トグルボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            this.Width += (bool)historyShowButton.IsChecked ? 400 : -400;
        }

        /// <summary>
        /// ウィンドウを閉じる
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // テンポラリフォルダの画像削除
            if (File.Exists(tmpPath))
                File.Delete(tmpPath);

            if (File.Exists(fileCopyPath))
                File.Delete(fileCopyPath);
        }

        /// <summary>
        /// スクリーンショットの範囲内に自身のアプリが存在するか
        /// </summary>
        /// <returns>範囲内/範囲外</returns>
        private bool IsDisplayRangeIn()
        {
            var leftTop_leftSide = monitorInfo.UpperLeftSource.X <= this.Left && monitorInfo.UpperLeftSource.Y <= this.Top;
            var leftTop_rightSide = monitorInfo.UpperLeftDestination.X >= this.Left && monitorInfo.UpperLeftDestination.Y >= this.Top;
            var rightDown_leftSide = monitorInfo.UpperLeftSource.X <= (this.Left + this.Width) && monitorInfo.UpperLeftSource.Y <= (this.Top + this.Height);
            var rightDown_rightSide = monitorInfo.UpperLeftDestination.X >= (this.Left + this.Width) && monitorInfo.UpperLeftDestination.Y >= (this.Top + this.Height);

            // 左上の位置と右下の位置が画面の範囲内にあるかチェック
            return (leftTop_leftSide && leftTop_rightSide) || (rightDown_leftSide && rightDown_rightSide);
        }

        /// <summary>
        /// 画像をクリップボードに保存
        /// </summary>
        private void PicCopy(BitmapSource bitmap)
        {
            if ((bool)fileCopyCheckButton.IsChecked)
            {
                UpdateFileCopyPath();

                // ファイルとしてコピー
                using (FileStream stream = new FileStream(fileCopyPath, FileMode.Create))
                {
                    // テンポラリフォルダに保存
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(stream);
                }

                var files = new StringCollection();
                files.Add(fileCopyPath);
                Clipboard.SetFileDropList(files);
            }
            else
            {
                // データとしてコピー
                Clipboard.SetImage(bitmap);
            }
        }

        /// <summary>
        /// 画像プレビュー(開く)
        /// </summary>
        /// <param name="bitmap">画像データ</param>
        private void PicPreview(BitmapSource bitmap)
        {
            // テンポラリフォルダに保存
            using (FileStream stream = new FileStream(tmpPath, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                encoder.Save(stream);
            }

            //　画像を開く
            var proc = new Process();
            proc.StartInfo.FileName = tmpPath;
            proc.StartInfo.UseShellExecute = true;
            proc.Start();
            statusText.Text = "画像を開きます。";
        }

        /// <summary>
        /// キャプチャした画像を保存
        /// </summary>
        private void SavePicture(BitmapSource bitmap)
        {
            var sfd = new SaveFileDialog
            {
                FileName = $"スクリーンショット_{DateTime.Now:yyyyMMddhhmmss}.png",
                Filter = "PNG ファイル(*.png)|*.png|JPG ファイル(*jpg)|*jpg|BMP ファイル(*bmp)|*bmp",
            };

            if (sfd.ShowDialog() == true)
            {
                using (FileStream stream = new FileStream(sfd.FileName, FileMode.Create))
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

                
                isSave = true;
                statusText.Text = "画像を保存しました。";
            }
            else
            {
                statusText.Text = "保存を中止しました。";
            }
        }

        /// <summary>
        /// コピー用パスのアップデート
        /// </summary>
        private void UpdateFileCopyPath()
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
        /// 選択した履歴画像を削除
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HistoryImgDelete_Click(object sender, RoutedEventArgs e)
        {
            historyImgList.RemoveAt(historyList.SelectedIndex);
        }

        /// <summary>
        /// 選択した履歴画像をプレビュー表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HistoryImgPreview_Click(object sender, RoutedEventArgs e)
        {
            // テンポラリフォルダに画像を作成して表示
            PicPreview(historyImgList[historyList.SelectedIndex].HistoryImage);
        }

        /// <summary>
        /// 選択した履歴画像を保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HistoryImgSaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (capImage.Source != null)
            {
                SavePicture(historyImgList[historyList.SelectedIndex].HistoryImage);
            }
        }

        /// <summary>
        /// 選択した履歴画像をコピー
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HistoryImgCopyButton_Click(object sender, RoutedEventArgs e)
        {
            PicCopy(historyImgList[historyList.SelectedIndex].HistoryImage);
            statusText.Text = (bool)fileCopyCheckButton.IsChecked ? "画像をファイルとしてコピーしました。" : "画像データをコピーしました。";
        }
    }
}
