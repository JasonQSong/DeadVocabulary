using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Excel = Microsoft.Office.Interop.Excel;
using System.Threading;
using System.Web.Script.Serialization;

namespace WpfDeadVocabulary
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            System.IO.FileStream logfilestream = System.IO.File.Open("log.txt", System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.Write);
            LogFileWriter = new System.IO.StreamWriter(logfilestream);
            LogFileWriter.AutoFlush = true;
        }
        ~MainWindow()
        {
            if (ExcelApp != null)
                ExcelApp.Quit();
            LogFileWriter.Close();
        }

        Excel.Application ExcelApp = null;
        System.IO.StreamWriter LogFileWriter;
        Thread th = null;
        public object missing = System.Reflection.Missing.Value;
        private static bool ExistSheet(Excel.Sheets sheets, string name)
        {
            for (int i = 1; i <= sheets.Count; i++)
                if ((sheets[i] as Excel.Worksheet).Name == name)
                    return true;
            return false;
        }
        private void buttonTrim_Click(object sender, RoutedEventArgs e)
        {
            if (th != null && th.ThreadState == ThreadState.Running)
                return;
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!System.IO.Directory.Exists("voice"))
                    System.IO.Directory.CreateDirectory("voice");
                th = new Thread(new ThreadStart(() =>
                {
                    lock (LogFileWriter) { LogFileWriter.WriteLine(string.Format("{0}:{1}", DateTime.Now, "Start background thread.")); }
                    Thread.CurrentThread.IsBackground = true;
                    ExcelApp = new Excel.Application();
                    Excel.Workbook workbook = ExcelApp.Workbooks.Open(ofd.FileName, missing, missing, missing, missing, missing, missing, missing, missing, missing, missing, missing, missing, missing, missing);
                    if (!ExistSheet(workbook.Sheets, "ColumeDefinition"))
                    {
                        MessageBox.Show("'ColumeDefinition' sheet doesn't exist");
                        return;
                    }
                    Excel.Worksheet CDSheet = workbook.Sheets["ColumeDefinition"] as Excel.Worksheet;

                    Dictionary<string, int> dic = new Dictionary<string, int>();
                    Dictionary<string, bool> hashdic = new Dictionary<string, bool>();
                    List<Excel.Worksheet> booklist = new List<Excel.Worksheet>();
                    int SPELL = 1;
                    int HASH = 1;
                    int LINK = 1;
                    int SOURCE = 1;
                    int sourcecount = 0;
                    for (int i = 1; i <= CDSheet.UsedRange.Rows.Count; i++)
                    {
                        switch ((CDSheet.Cells[i, 1] as Excel.Range).Text.ToString())
                        {
                            case "Spell":
                                int.TryParse((CDSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out SPELL);
                                break;
                            case "Hash":
                                int.TryParse((CDSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out HASH);
                                break;
                            case "Link":
                                int.TryParse((CDSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out LINK);
                                break;
                            case "Source":
                                int.TryParse((CDSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out SOURCE);
                                int.TryParse((CDSheet.Cells[i, 3] as Excel.Range).Text.ToString(), out sourcecount);
                                for (int j = 1; j <= sourcecount; j++)
                                    booklist.Add(workbook.Sheets[(CDSheet.Cells[i, 3 + j] as Excel.Range).Text.ToString()] as Excel.Worksheet);
                                break;
                        }
                    }
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        this.progressBarTotal.Maximum = booklist.Count;
                        this.textBlockProgressBarTotal.Text = "0 / " + booklist.Count.ToString();
                    }));

                    Excel.Worksheet ASheet = null;
                    if (ExistSheet(workbook.Sheets, "All"))
                    {
                        ASheet = workbook.Sheets["All"] as Excel.Worksheet;
                    }
                    else
                    {
                        ASheet = workbook.Sheets.Add(missing, CDSheet, 1, missing) as Excel.Worksheet;
                        ASheet.Name = "All";
                        int AllCount = 0;
                        string word = "";
                        string hash="";
                        for (int i = 0; i < booklist.Count; i++)
                        {
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                this.progressBarTotal.Value = i;
                                this.textBlockProgressBarTotal.Text = i.ToString() + " / " + booklist.Count.ToString();
                                this.progressBarCurrent.Maximum = booklist[i].UsedRange.Rows.Count;
                                this.textBlockProgressBarCurrent.Text = "0 / " + booklist[i].UsedRange.Rows.Count.ToString();
                            }));
                            for (int j = 1; j <= booklist[i].UsedRange.Rows.Count; j++)
                            {
                                word = (booklist[i].Cells[j, SPELL] as Excel.Range).Text.ToString();
                                AllCount++;
                                (booklist[i].Rows[j, missing] as Excel.Range).Copy(ASheet.Rows[AllCount, missing]);
                                ASheet.Cells[AllCount, SOURCE + i] = "1";
                                if (dic.Keys.Contains(word))
                                {
                                    ASheet.Cells[AllCount, LINK] = dic[word].ToString();
                                    ASheet.Cells[dic[word], SOURCE + i] = "1";
                                }
                                else
                                {
                                    ASheet.Cells[AllCount, LINK] = "0";
                                    dic[word] = AllCount;
                                }
                                hash = (booklist[i].Cells[j, HASH] as Excel.Range).Text.ToString();
                                if (!hashdic.Keys.Contains(hash))
                                {
                                    hashdic[hash] = true;
                                    if (!System.IO.File.Exists(@"voice\" + hash + ".mp3"))
                                    {
                                        while (true)
                                        {
                                            if (Downloading <= 10)
                                                break;
                                            Thread.Sleep(1000);
                                        }
                                        ThreadPool.QueueUserWorkItem(new WaitCallback(DownloadVoice), hash);
                                    }
                                }
                                this.Dispatcher.Invoke(new Action(() =>
                                {
                                    this.progressBarCurrent.Value = j;
                                    this.textBlockProgressBarCurrent.Text = j.ToString() + " / " + booklist[i].UsedRange.Rows.Count.ToString();
                                }));
                            }
                        }
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.progressBarTotal.Value = booklist.Count;
                            this.textBlockProgressBarTotal.Text = booklist.Count.ToString() + " / " + booklist.Count.ToString();
                            this.progressBarCurrent.Maximum = AllCount;
                            this.textBlockProgressBarCurrent.Text = "0 / " + AllCount.ToString();
                        }));
                        workbook.Save();
                    }
                    Excel.Worksheet CSheet = null;
                    if (ExistSheet(workbook.Sheets, "Compact"))
                    {
                        CSheet = workbook.Sheets["Compact"] as Excel.Worksheet;
                    }
                    else
                    {
                        CSheet = workbook.Sheets.Add(missing, ASheet, 1, missing) as Excel.Worksheet;
                        CSheet.Name = "Compact";
                        int CompactCount = 0;
                        for (int i = 1; i <= ASheet.UsedRange.Rows.Count; i++)
                        {
                            if ((ASheet.Cells[i, LINK] as Excel.Range).Text.ToString() == "0")
                            {
                                CompactCount++;
                                (ASheet.Rows[i, missing] as Excel.Range).Copy(CSheet.Rows[CompactCount, missing]);
                            }
                            this.Dispatcher.Invoke(new Action(() =>
                            {
                                this.progressBarCurrent.Value = i;
                                this.textBlockProgressBarCurrent.Text = i.ToString() + " / " + ASheet.UsedRange.Rows.Count.ToString();
                            }));
                        }
                    }
                    workbook.Save();
                    ExcelApp.Quit();
                }));
                th.Start();
            }
        }
        int Downloading = 0;
        public void DownloadVoice(object hash)
        {
            Downloading++;
            string url = "http://bwvocabulary.storage.aliyun.com/voice/" + hash.ToString() + ".mp3";
            System.Net.WebClient webclient = new System.Net.WebClient();
            try
            {
                webclient.DownloadFile(url, @"voice\" + hash.ToString() + ".mp3");
            }
            catch (Exception ex)
            {
                lock (LogFileWriter) { LogFileWriter.WriteLine(string.Format("{0}:{1},{2}", DateTime.Now, hash, ex)); }
            }
            finally
            {
                Downloading--;
            }
        }
        private void Test_Click(object sender, RoutedEventArgs e)
        {
        }

        private void buttonStartCrawl_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}