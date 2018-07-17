//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

namespace Microsoft.CognitiveServices.SpeechRecognition
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Media;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;
    using Microsoft.CognitiveServices.Speech;
    using Microsoft.CognitiveServices.Speech.Translation;
    using OpenCvSharp;
    using OpenCvSharp.Extensions;
    using System.Net.Http;
    using System.Net.WebSockets;
    using System.Net.Http.Headers;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Interaction logic for MainWindow.xaml　2
    /// </summary>
    public partial class MainWindow : System.Windows.Window, INotifyPropertyChanged
    {
        #region Events

        /// <summary>
        /// Implement INotifyPropertyChanged interface
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Helper function for INotifyPropertyChanged interface 
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        /// <param name="caller">Property name</param>
        private void OnPropertyChanged<T>([CallerMemberName]string caller = null)
        {
            var handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(caller));
            }
        }

        #endregion Events

        #region Helper functions

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            var log_source = new BitmapImage();
            log_source.BeginInit();
            log_source.UriSource = new Uri(@"C:\Users\営業推進2G-1T\Desktop\hayase\samples\TranslationServiceExample\Image\Logo_COM-ZAP.JPG");
            log_source.EndInit();
            log_Image.Source = log_source;

            var t_source = new BitmapImage();
            t_source.BeginInit();
            t_source.UriSource = new Uri(@"C:\Users\営業推進2G-1T\Desktop\hayase\samples\TranslationServiceExample\Image\illust-53.png");
            t_source.EndInit();
            t_Image.Source = t_source;
            _Image.Source = t_source;

        }


        private void SetCurrentText(TextBlock textBlock, string text)
        {
            this.Dispatcher.Invoke(
                () =>
                {
                    textBlock.Text = text;
                });
        }

        /// <summary>
        /// Raises the System.Windows.Window.Closed event.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            if (this.factory != null)
            {
                this.recognizer.Dispose();
                this.factory = null;
            }

            base.OnClosed(e);
        }

        /// <summary>
        /// Logs the recognition start.
        /// </summary>
        private void LogRecognitionStart(TextBox log, TextBlock currentText)
        {
            string recoSource = "microphone";
            this.SetCurrentText(currentText, string.Empty);
            log.Clear();
            //this.WriteLine(log, "\n--- Start speech recognition using " + recoSource + " ----\n\n");
            this.WriteLine(log, "\n--Start speech recognition--\n\n");
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        private void WriteLine(TextBox log)
        {
            this.WriteLine(log, string.Empty);
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        private void WriteLine(TextBox log, string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            Dispatcher.Invoke(() =>
            {
                log.AppendText((formattedStr + "\n"));
                log.ScrollToEnd();
            });
        }

        /*
        private void SaveKey_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveKeyToFile(SubscriptionKeyFileName, this.SubscriptionKey);
                MessageBox.Show("Keys are saved to your disk.\nYou do not need to paste it next time.", "Keys");
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Fail to save Keys. Error message: " + exception.Message,
                    "Keys",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void SaveKeyToFile(string fileName, string key)
        {
            using (System.IO.IsolatedStorage.IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                using (var oStream = new IsolatedStorageFileStream(fileName, FileMode.Create, isoStore))
                {
                    using (var writer = new StreamWriter(oStream))
                    {
                        writer.WriteLine(key);
                    }
                }
            }
        }
        */

        /*
        private string GetSubscriptionKeyFromFile(string fileName)
        {
            string subscriptionKey = null;

            using (IsolatedStorageFile isoStore = IsolatedStorageFile.GetStore(IsolatedStorageScope.User | IsolatedStorageScope.Assembly, null, null))
            {
                try
                {
                    using (var iStream = new IsolatedStorageFileStream(fileName, FileMode.Open, isoStore))
                    {
                        using (var reader = new StreamReader(iStream))
                        {
                            subscriptionKey = reader.ReadLine();
                        }
                    }
                }
                catch (FileNotFoundException)
                {
                    subscriptionKey = null;
                }
            }
            return subscriptionKey;
        }
        */

        #endregion


        //翻訳機能の変数を定義
        private TranslationRecognizer recognizer;
        private SpeechFactory factory;
        //private string subscriptionKey;
        //private const string SubscriptionKeyFileName = "SubscriptionKey.txt";
        private bool started;
        /*
        public string SubscriptionKey
        {
            get
            {
                return this.subscriptionKey;
            }

            set
            {
                this.subscriptionKey = value?.Trim();
                this.OnPropertyChanged<string>();
            }
        }
        */



        //カメラキャプチャの変数を定義
        public bool IsExitCapture { get; set; }


        //翻訳機能のイベントハンドラ
        /// <summary>
        /// Handles the Click event of the _startButton control.
        /// Checks if Subscription Key is valid
        /// Calls CreateRecognizer()
        /// Waits on a thread which is running the recognition
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            //講師側のカメラ
            Console.WriteLine("講師側側カメラ確認");
            System.Threading.ThreadPool.QueueUserWorkItem(this.Capture);

            //生徒側のカメラ
            Console.WriteLine("講師側カメラ確認");
            System.Threading.ThreadPool.QueueUserWorkItem(this.Capture2);

            this.started = true;
                this.LogRecognitionStart(this.crisLogText, this.crisCurrentText);
                this.CreateRecognizer();
                await Task.Run(async () => { await this.recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false); });

        }

        /// <summary>
        /// Handles the Click event of the _stopButton control.
        /// If recognition is running, starts a thread which stops the recognition
        /// </summary>
        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.started)
            {
                await Task.Run(async () => { await this.recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false); });
                this.started = false;
            }
        }

        /// <summary>
        /// Initializes the factory object with subscription key and region
        /// Initializes the recognizer object with a TranslationRecognizer
        /// Subscribes the recognizer to recognition Event Handlers
        /// If recognition is running, starts a thread which stops the recognition
        /// </summary>
        private void CreateRecognizer()
        {
            string region = "eastasia";
            string fromLanguage = "en-US";
            //var toLanguages = new List<string>() { "zh", "de" };
            var toLanguages = new List<string>() { "ja"};
            // var voiceChinese = "zh-CN-Yaoyao";
            var voiceChinese = "ja-JP-Ayumi";
            string sub_key = "7620810559eb4d1c96f770c5ee019bd3";
            //string sub_key = "91ad01e1da954931955dc87b6fb71c0c";

            //this.factory = SpeechFactory.FromSubscription(SubscriptionKey, region);
            this.factory = SpeechFactory.FromSubscription(sub_key, region);

            this.recognizer = this.factory.CreateTranslationRecognizer(fromLanguage, toLanguages, voiceChinese);

            this.recognizer.IntermediateResultReceived += this.OnPartialResponseReceivedHandler;
            this.recognizer.FinalResultReceived += this.OnFinalResponse;
            this.recognizer.SynthesisResultReceived += this.OnSynthesis;
            this.recognizer.RecognitionErrorRaised += this.OnError;
        }

        #region Recognition Event Handlers

        /// <summary>
        /// Called when a partial response is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="TranslationTextResultEventArgs"/> instance containing the event data.</param>
        private void OnPartialResponseReceivedHandler(object sender, TranslationTextResultEventArgs e)
        {
            string text = e.Result.Text;
            foreach(var t in e.Result.Translations)
            {
                //text += $"\nSame in {t.Key}: {t.Value}";
                text += $"\n{t.Value}";
            }

            this.SetCurrentText(this.crisCurrentText, text);
        }

        /// <summary>
        /// Called on final response.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="TranslationTextResultEventArgs"/> instance containing the event data.</param>
        private void OnFinalResponse(object sender, TranslationTextResultEventArgs e)
        {
            Console.WriteLine(e.Result.RecognitionStatus);
            if (e.Result.Text.Length == 0)
            {
                this.WriteLine(this.crisLogText, "Status: " + e.Result.RecognitionStatus);
                this.WriteLine(this.crisLogText, "No phrase response is available.");
            }
            else
            {
                string text = e.Result.Text;
                foreach (var t in e.Result.Translations)
                {
                    //text += $"\nSame in {t.Key}: {t.Value}";
                    text += $"\n{t.Value}";
                }

                this.SetCurrentText(this.crisCurrentText, text);
                text += "\n";
                this.WriteLine(this.crisLogText, text);
            }
        }

        /// <summary>
        /// Called when error occurs.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="RecognitionErrorEventArgs"/> instance containing the event data.</param>
        private void OnError(object sender, RecognitionErrorEventArgs e)
        {
            string text = $"Speech recognition: error occurred. Status: {e.Status}, FailureReason: {e.FailureReason}";
            this.SetCurrentText(this.crisCurrentText, text);
            text += "\n";
            this.WriteLine(this.crisLogText, text);
            if (this.started)
            {
                this.recognizer.StopContinuousRecognitionAsync().Wait();
                this.started = false;
            }
        }

        /// <summary>
        /// Called on availability of synthesized data.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="TranslationSynthesisEventArgs"/> instance containing the event data.</param>
        private void OnSynthesis(object sender, TranslationSynthesisResultEventArgs e)
        {
            Console.WriteLine("hoge0");
            Console.WriteLine(e.Result.Status);
            if (e.Result.Status == SynthesisStatus.Success)
            {
                Console.WriteLine("hoge1");
                using (var m = new MemoryStream(e.Result.Audio))
                {
                    Console.WriteLine("hoge2");
                    SoundPlayer simpleSound = new SoundPlayer(m);
                    simpleSound.Play();
                }
            }
        }

        
        //生徒側カメラキャプチャのイベントハンドラ
        //生徒側カメラ画像を取得して次々に表示を切り替える
        public virtual void Capture(object state)
        {
            Console.WriteLine("生徒側のカメラをキャプチャ");
            var camera = new VideoCapture(0/*0番目のデバイスを指定*/)
            {
                // キャプチャする画像のサイズフレームレートの指定
                FrameWidth = 480,
                FrameHeight = 270,
                // Fps = 60
            };

            using (var img = new Mat()) // 撮影した画像を受ける変数
            using (camera)
            {
                Console.WriteLine("講師側カメラの画像受け");
                while (true)
                {
                    if (this.IsExitCapture)
                    {
                        this.Dispatcher.Invoke(() => this._Image.Source = null);
                        break;
                    }

                    camera.Read(img); // Webカメラの読み取り（バッファに入までブロックされる

                    if (img.Empty())
                    {
                        break;
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        this._Image.Source = img.ToWriteableBitmap(); // WPFに画像を表示
                    });
                }
            }
        }

        //講師側のカメラをキャプチャ
        public virtual void Capture2(object state)
        {
            Console.WriteLine("生徒側のカメラをキャプチャ");
            var camera = new VideoCapture(1/*1番目のUSBデバイスを指定*/)
            {
                // キャプチャする画像のサイズフレームレートの指定
                FrameWidth = 480,
                FrameHeight = 270,
                // Fps = 60
            };

            using (var img = new Mat()) // 撮影した画像を受ける変数
            using (camera)
            {
                Console.WriteLine("講師側カメラの画像受け");
                while (true)
                {
                    if (this.IsExitCapture)
                    {
                        this.Dispatcher.Invoke(() => this.t_Image.Source = null);
                        break;
                    }

                    camera.Read(img); // Webカメラの読み取り（バッファに入までブロックされる

                    if (img.Empty())
                    {
                        break;
                    }

                    this.Dispatcher.Invoke(() =>
                    {
                        this.t_Image.Source = img.ToWriteableBitmap(); // WPFに画像を表示
                    });
                }
            }
        }

        /*
        /// 生徒側のCaptureボタンが押され時
        protected virtual void CameraButton_Click(object sender, RoutedEventArgs e)
        {
            //this.IsExitCapture = true;
            Console.WriteLine("生徒側カメラ確認");
            System.Threading.ThreadPool.QueueUserWorkItem(this.Capture);
        }
        */

        /// 講師側のCaptureボタンが押され時
        protected virtual void CameraButton_Click2(object sender, RoutedEventArgs e)
        {
            //this.IsExitCapture = true;
            Console.WriteLine("講師側カメラ確認");
            System.Threading.ThreadPool.QueueUserWorkItem(this.Capture2);
        }


        //体調判定ボタンが押された時(FaceAPI接続)
        private async void  SaveButton_Click(object sender, RoutedEventArgs e)

        {
            //  WriteableBitmapを渡しているので、その型へと戻す
            var image = (WriteableBitmap)t_Image.Source;

            //画面に表示
            FacePhoto.Source = image;

            //  Bitmap以外にも出力できるけれど、今回はBitmapにしておく
            //  また、ファイルは上書きで保存する
            using (var fs = new System.IO.FileStream("hoge.bmp", System.IO.FileMode.Create))
            {
                //  BmpBitmapEncoderの他に、PngBitmapEncoderとかもある
                var enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(image));
                enc.Save(fs);

                //MessageBox.Show("送信しました");
            }
            

            string Currentpath = Environment.CurrentDirectory;
            string imagepath = Currentpath + "/hoge.bmp";
            Console.WriteLine(imagepath);

            //リクエストURLとkey
            string base_url = "https://southeastasia.api.cognitive.microsoft.com/face/v1.0/detect";
            string request_param = "returnFaceId=true&returnFaceLandmarks=false" +
                "&returnFaceAttributes=age,gender,headPose,smile,facialHair,glasses," +
                "emotion,hair,makeup,occlusion,accessories,blur,exposure,noise";
            string face_url = base_url + "?" + request_param;
            string face_key = "ab4a56df77124c149575bd4a94560c44";

            //HTTPクライアント作成、ヘッダーにキーを設定
            HttpClient faceClient = new HttpClient();
            faceClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", face_key);

            //リクエストBodyの作成（関数呼び出し）
            byte[] byteData = GetImageAsByteArray(imagepath);

            //Azureに接続
            HttpResponseMessage response;
            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
              
                content.Headers.ContentType =
                    new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await faceClient.PostAsync(face_url, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();
             

                // Display the JSON response.
                Console.WriteLine("\nResponse:\n");
                Console.WriteLine(contentString);
                Console.WriteLine("\nPress Enter to exit...");

                //JSONを配列に入れる
                JArray face_array = JArray.Parse(contentString);

                //配列の中身を確認
                //Console.WriteLine(face_array[0]["faceAttributes"]["gender"].ToString());

                //顔の数だけ処理をする
                foreach (JObject item in face_array)
                {
                    JValue gender_value = (JValue)item["faceAttributes"]["gender"];
                    string gender = (string)gender_value.Value;
                    Console.WriteLine(gender);

                    JValue happiness_value = (JValue)item["faceAttributes"]["emotion"]["happiness"];
                    string happiness = happiness_value.Value.ToString();
                    Console.WriteLine(happiness);
                    string condition = "";

                    if (float.Parse(happiness) > 0.5)
                    {
                        condition = "Good Condition！";
                        Console.WriteLine(condition);
                        ConditionResult.Text = condition;

                    }
                    else
                    {
                        condition = "Bad Condition！";
                        Console.WriteLine(condition);
                        ConditionResult.Text = condition;
                    }

                }



            }

        }

        //画像データのBODY作成関数
       private byte[] GetImageAsByteArray(string filePath)
       //private byte[] GetImageAsByteArray(FileStream image)
        {
            using (FileStream fileStream =
                new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                return binaryReader.ReadBytes((int)fileStream.Length);
            }
            //throw new NotImplementedException();
        }

        #endregion

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_1(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_2(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_3(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBox_TextChanged_4(object sender, TextChangedEventArgs e)
        {

        }

        private void crisLogText_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void ConditionResult_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
    }
}
