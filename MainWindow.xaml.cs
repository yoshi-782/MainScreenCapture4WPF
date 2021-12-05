using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MainScreenCapture4WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
        private readonly ObservableCollection<HistoryImageList> historyImgList = new();

        /// <summary>
        /// モニター一覧のバインドデータ
        /// </summary>
        private ObservableCollection<string> screenList;

        /// <summary>
        /// キャプチャーAPI
        /// </summary>
        private readonly CaptureAPI.CapAPI capAPI = new();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            
            // 初回画面表示時処理
            this.ContentRendered += (s, e) =>
            {
                // 画面を最前面にするかのイベント処理
                topMastCheckButton.Click += (s, e) => this.Topmost = (bool)topMastCheckButton.IsChecked;
                // キャプチャ画像の履歴
                this.historyList.ItemsSource = historyImgList;
                screenList = new ObservableCollection<string>(capAPI.MonitorsName);
                //screenList.Insert(0, "モニターを選択");
                this.screensName.ItemsSource = screenList;
                screensName.SelectedIndex = 0;
            };
        }

        /// <summary>
        /// ウィンドウのサイズ・位置をRectangleに変換
        /// </summary>
        /// <returns>ウィンドウのサイズ・位置情報</returns>
        private Rectangle GetRectangle() => new((int)this.Left, (int)this.Top, (int)this.Width, (int)this.Height);

        /// <summary>
        /// キャプチャボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CapButton_Click(object sender, RoutedEventArgs e)
        {
            //if (screensName.SelectedItem.ToString() == "モニターを選択")
            //{
            //    MessageBox.Show("撮影する画面を選択してください。",
            //                    this.Title,
            //                    MessageBoxButton.OK,
            //                    MessageBoxImage.Warning);
            //    return;
            //}

            //if (!monitorInfo.GetScreenSize(screensName.SelectedItem?.ToString()))
            //{
            //    MessageBox.Show("指定したモニターが存在しません。", 
            //                    this.Title, 
            //                    MessageBoxButton.OK, 
            //                    MessageBoxImage.Error);
            //    statusText.Text = "画面の撮影に失敗しました。";
            //    return;
            //}

            // モニターのサイズ取得
            if (capAPI.GetScreenRect(screensName.SelectedItem.ToString(), out Rectangle rect) == false)
            {
                MessageBox.Show("指定したモニターが存在しません。",
                                this.Title,
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return;
            }

            var text = $"画面を撮影しました。{((bool)fileCopyCheckButton.IsChecked ? " 画像ファイルコピー済み" : " 画像データコピー済み")}";
            BitmapSource image = null;
            

            if (capAPI.IsDisplayRangeIn(rect, GetRectangle()))
            {
                this.Hide();
                DispatcherTimer timer = new();
                timer.Interval = new TimeSpan(0, 0, 0, 0, 400);
                void timerEvent(object s, EventArgs ev)
                {
                    image = capAPI.Capture(rect);
                    timer.Stop();
                    timer.Tick -= timerEvent;

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

                    capAPI.PicCopy(image, (bool)fileCopyCheckButton.IsChecked);
                    statusText.Text = text;

                    this.Show();
                }

                timer.Tick += timerEvent;
                timer.Start();
            }
            else
            {
                image = capAPI.Capture(rect);

                if (capImage.Source == null)
                {
                    // 初回は履歴に挿入しない
                    capImage.Source = image;
                }
                else
                {
                    // 履歴に挿入
                    historyImgList.Insert(0, new HistoryImageList { HistoryImage = (BitmapSource)capImage.Source });
                    capImage.Source = image;
                }

                capAPI.PicCopy(image, (bool)fileCopyCheckButton.IsChecked);
                statusText.Text = text;
            }
        }

        /// <summary>
        /// 画像をコピーボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (capImage.Source != null)
            {
                capAPI.PicCopy((BitmapSource)capImage.Source, (bool)fileCopyCheckButton.IsChecked);
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

            //if (capAPI.IsSave)
            //{
            //    statusText.Text = "既に保存済みです。";
            //    return;
            //}

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
                capAPI.PreviewPicture((BitmapSource)capImage.Source);
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
            capAPI.DeleteTemporaryFile();
        }

        /// <summary>
        /// キャプチャした画像を保存
        /// </summary>
        private void SavePicture(BitmapSource bitmap)
        {
            if (capAPI.SavePicture(bitmap))
            {
                statusText.Text = "画像を保存しました。";
            }
            else
            {
                statusText.Text = "保存を中止しました。";
            }
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
            capAPI.PreviewPicture(historyImgList[historyList.SelectedIndex].HistoryImage);
            statusText.Text = "画像を開きます。";
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
            capAPI.PicCopy(historyImgList[historyList.SelectedIndex].HistoryImage, (bool)fileCopyCheckButton.IsChecked);
            statusText.Text = (bool)fileCopyCheckButton.IsChecked ? "画像をファイルとしてコピーしました。" : "画像データをコピーしました。";
        }
    }
}
