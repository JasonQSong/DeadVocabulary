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
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Threading;
using Excel = Microsoft.Office.Interop.Excel;
using Forms = System.Windows.Forms;
using System.IO;
using System.Xml.Serialization;

namespace WpfDeadVocabulary
{
    /// <summary>
    /// StudyWindow.xaml 的交互逻辑
    /// </summary>
    public partial class StudyWindow : Window
    {
        public Record NowRecord { get; set; }
        private object missing = System.Reflection.Missing.Value;
        private XmlSerializer XmlRecordFormat = new XmlSerializer(typeof(Record));
        private Random randseed = new Random();
        private Thread th = null;
        private Thread savethread = null;
        private Thread loadthread = null;
        Excel.Application ExcelApp = null;
        DispatcherTimer timerAutoLoad = new DispatcherTimer();
        DispatcherTimer timerAutoWrong = new DispatcherTimer();
        DispatcherTimer timerNowTime = new DispatcherTimer();
        Forms.OpenFileDialog openFileDialogSelectVocabulary = new Forms.OpenFileDialog();
        OptionWindow optionWindow = null;
        SearchBar searchBar=null;

        private List<string> Choises = new List<string>();
        public bool ShowNext { get; set; }
        public int AutoSaveLimit { get; set; }

        private int _autoSaveCount = 0;
        private int AutoSaveCount
        {
            get { return this._autoSaveCount; }
            set
            {
                value = (AutoSaveLimit == 0) ? -1 : value;
                this._autoSaveCount = value;
                if (value >= this.AutoSaveLimit)
                {
                    this.SaveToXml("AutoSave.xml");
                    this._autoSaveCount = 0;
                }
                else if (value == this.AutoSaveLimit / 2)
                {
                    this.SaveToXml("AutoSave.bak.xml");
                }
            }
        }
        
        public StudyWindow()
        {
            InitializeComponent();
            Initialize();
        }

        public DateTime AutoWrongChooseStartTime { get; set; }
        private void Initialize()
        {
            this.NowRecord = null;
            this.ShowNext = false;
            this.AutoSaveLimit = 50;
            openFileDialogSelectVocabulary.Filter = "Dead Vocabulary Database (*.xlsx)|*.xlsx";
            openFileDialogSelectVocabulary.FileName = "Book.xlsx";
            this.timerAutoLoad.IsEnabled = false;
            this.timerAutoLoad.Interval = TimeSpan.FromSeconds(1);
            this.timerAutoLoad.Tick += new EventHandler((sender, e) =>
            {
                timerAutoLoad.IsEnabled = false;
                if (NowRecord == null)
                {
                    if (System.IO.File.Exists("AutoSave.xml"))
                    {
                        if (MessageBox.Show("Do you want to load from 'AutoSave.xml'?", "Autosave", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                            this.LoadFromXml("AutoSave.xml");
                    }
                }
            });
            this.timerAutoWrong.IsEnabled = false;
            this.timerAutoWrong.Interval = TimeSpan.FromSeconds(1);
            this.timerAutoWrong.Tick += new EventHandler((sender, e) => {
                if (NowRecord == null)
                    return;
                if (NowRecord.Settings.LearnJudge == LearnJudge.Choose)
                {
                    if (DateTime.Now - AutoWrongChooseStartTime >= TimeSpan.FromSeconds(5))
                    {
                        this.EnterInput("<F>");
                    }
                }
            });

            this.timerNowTime.IsEnabled = false;
            this.timerNowTime.Interval = TimeSpan.FromSeconds(1);
            this.timerNowTime.Tick += new EventHandler((sender, e) =>
            {
                statusBarItemNowTime.Text = DateTime.Now.ToString();
            });
            this.timerNowTime.IsEnabled = true;
        }
        ~StudyWindow()
        {
            if (ExcelApp != null)
                ExcelApp.Quit();
            if (searchBar != null)
                searchBar.Close();
        }
        public bool CanPronounceWordById(int id)
        {
            if (System.IO.File.Exists(@"voice\" + NowRecord.AllWords[id].Hash + ".mp3"))
                return true;
            return false;
        }
        public bool PronounceWordById(int id)
        {
            return PlaySound(@"voice\" + NowRecord.AllWords[id].Hash + ".mp3");
        }
        public bool PlaySound(string path)
        {
            if (System.IO.File.Exists(path))
            {
                this.mediaWordSpeaker.Source = new Uri(path, UriKind.Relative);
                this.mediaWordSpeaker.Play();
                return true;
            }
            return false;
        }
        public string GetDescriptionById(int id, bool multiline, bool one)
        {
            string ret = "";
            if (one)
            {
                if (NowRecord.AllWords[id].Des.Count > 0)
                    ret += NowRecord.AllWords[id].Des[randseed.Next() % NowRecord.AllWords[id].Des.Count];
            }
            else
            {
                foreach (Des des in NowRecord.AllWords[id].Des)
                    ret += des.ToString() + (multiline ? Environment.NewLine : "; ");
            }
            return ret;
        }
        private void RenderWord(bool full, bool wrong)
        {
            textBoxInput.Text = "Input";
            progressBarLevel.Value = 0;
            labelAnswer.Text = "Answer";
            buttonPlaySound.Visibility = Visibility.Visible;
            labelPho.Text = "Phonetic";
            labelDes.Text = "Description";
            labelSen.Text = "Sentence";
            textBoxWrongWord.Text = "Wrong word";
            textBoxWrongWord.Visibility = Visibility.Visible;
            buttonPeek.Visibility = Visibility.Hidden;
            if (NowRecord == null)
                return;
            if (NowRecord.NowWord < 0)
                return;
            textBoxInput.Text = "";
            if (NowRecord.Settings.LearnJudge == LearnJudge.Spell && NowRecord.AllWords[NowRecord.NowWord].Level == 0 && NowRecord.AllWords[NowRecord.NowWord].First > DateTime.MinValue)
                textBoxInput.Text = NowRecord.AllWords[NowRecord.NowWord].Spell.Length > 3 ? NowRecord.AllWords[NowRecord.NowWord].Spell.Substring(0,3) : "";
            textBoxInput.SelectAll();
            progressBarLevel.Value = 0;
            labelAnswer.Text = "";
            labelPho.Text = "";
            labelDes.Text = "";
            labelSen.Text = "";
            textBoxNote.Text = "";
            textBoxWrongWord.Text = "";
            textBoxWrongWord.Visibility = Visibility.Hidden;
            if(!CanPronounceWordById(NowRecord.NowWord))
                buttonPlaySound.Visibility = Visibility.Visible;
            if (wrong)
            {
                labelAnswer.Foreground = new SolidColorBrush(Color.FromRgb(192, 0, 0));
                labelDes.Foreground = new SolidColorBrush(Color.FromRgb(192, 0, 0));
                if (NowRecord.WrongWord > 0)
                {
                    textBoxWrongWord.Text = NowRecord.AllWords[NowRecord.WrongWord].Spell + Environment.NewLine;
                    foreach (Des des in NowRecord.AllWords[NowRecord.WrongWord].Des)
                        this.textBoxWrongWord.Text += des.ToString() + Environment.NewLine;
                    textBoxWrongWord.Visibility = Visibility.Visible;
                }
            }
            else
            {
                labelAnswer.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                labelDes.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                textBoxWrongWord.Visibility = Visibility.Hidden;
            }
            if (full)
            {
                PronounceWordById(NowRecord.NowWord);
                labelAnswer.Text = NowRecord.AllWords[NowRecord.NowWord].Spell;
                progressBarLevel.Value = NowRecord.AllWords[NowRecord.NowWord].Level;
                labelPho.Text = NowRecord.AllWords[NowRecord.NowWord].Pho;
                foreach (Des des in NowRecord.AllWords[NowRecord.NowWord].Des)
                    this.labelDes.Text += des.ToString() + Environment.NewLine;
                labelDes.Visibility = Visibility.Visible;
                labelChooseDes.Visibility = Visibility.Hidden;
                foreach (Sen sen in NowRecord.AllWords[NowRecord.NowWord].Sen)
                    this.labelSen.Text += sen.ToString() + Environment.NewLine;
                textBoxNote.Text = NowRecord.AllWords[NowRecord.NowWord].Note;
                textBoxNote.Visibility = Visibility.Visible;
                buttonPeek.Visibility = Visibility.Hidden;
            }
            else
            {
                if (NowRecord.Settings.PlaySound)
                    PronounceWordById(NowRecord.NowWord);
                if (NowRecord.Settings.ShowWord)
                    labelAnswer.Text = NowRecord.AllWords[NowRecord.NowWord].Spell;
                progressBarLevel.Value = NowRecord.AllWords[NowRecord.NowWord].Level;
                if (NowRecord.Settings.ShowPho)
                    labelPho.Text = NowRecord.AllWords[NowRecord.NowWord].Pho;
                if (NowRecord.Settings.ShowDes)
                {
                    this.labelDes.Text = this.labelDes.Text = GetDescriptionById(NowRecord.NowWord, true, NowRecord.Settings.ShowDesOne);
                    if (NowRecord.Settings.LearnJudge == LearnJudge.Choose)
                    {
                        labelChooseDes.Visibility = Visibility.Visible;
                        labelDes.Visibility = Visibility.Hidden;
                        this.Choises.Clear();
                        int maxChoice = NowRecord.AllWords[NowRecord.NowWord].Level / 2 + 1;
                        maxChoice = (maxChoice < 1) ? 1 : maxChoice;
                        maxChoice = (maxChoice > 5) ? 5 : maxChoice;
                        int correctChoice = randseed.Next() % maxChoice + 1;
                        this.Choises.Add("<F>");
                        List<int> disturb = new List<int>();
                        for (int i = 0; i < NowRecord.DivideWords.Count; i++)
                            for (int j = NowRecord.DivideWords[i].Count - 1; j >= 0; j--)
                                if (NowRecord.AllWords[NowRecord.DivideWords[i][j]].Last - DateTime.Now < TimeSpan.FromMinutes(30))
                                    disturb.Add(NowRecord.DivideWords[i][j]);
                        while (disturb.Count < 15)
                            disturb.Add(randseed.Next() % (NowRecord.AllWords.Count - 1) + 1);
                        disturb.Remove(NowRecord.NowWord);
                        string tmpdes;
                        for (int i = 1; i <= 8; i++)
                        {
                            tmpdes = "";
                            if (i == correctChoice)
                            {
                                this.Choises.Add("<C>");
                                tmpdes = (maxChoice > 4) ? GetDescriptionById(NowRecord.NowWord, false, true) : GetDescriptionById(NowRecord.NowWord, maxChoice <= 2, NowRecord.Settings.ShowDesOne);
                            }
                            else if (i <= maxChoice)
                            {
                                this.Choises.Add("<F>");
                                int tmp = randseed.Next() % disturb.Count;
                                tmpdes = (maxChoice > 4) ? GetDescriptionById(disturb[tmp], false, true) : GetDescriptionById(disturb[tmp], maxChoice <= 2, NowRecord.Settings.ShowDesOne);
                                disturb.RemoveAt(tmp);
                            }
                            switch (i)
                            {
                                case 1:
                                    buttonChoose1.Content = tmpdes;
                                    break;
                                case 2:
                                    buttonChoose2.Content = tmpdes;
                                    break;
                                case 3:
                                    buttonChoose3.Content = tmpdes;
                                    break;
                                case 4:
                                    buttonChoose4.Content = tmpdes;
                                    break;
                                case 5:
                                    buttonChoose5.Content = tmpdes;
                                    break;
                                case 6:
                                    buttonChoose6.Content = tmpdes;
                                    break;
                                case 7:
                                    buttonChoose7.Content = tmpdes;
                                    break;
                                case 8:
                                    buttonChoose8.Content = tmpdes;
                                    break;
                            }
                            AutoWrongChooseStartTime = DateTime.Now;
                            timerAutoWrong.IsEnabled = true;
                        }
                    }
                    else
                    {
                        labelChooseDes.Visibility = Visibility.Hidden;
                        labelDes.Visibility = Visibility.Visible;
                    }
                }
                if (NowRecord.Settings.ShowSen)
                {
                    if (NowRecord.Settings.ShowSenOne)
                    {
                        if (NowRecord.AllWords[NowRecord.NowWord].Sen.Count > 0)
                            this.labelSen.Text = NowRecord.AllWords[NowRecord.NowWord].Sen[randseed.Next() % NowRecord.AllWords[NowRecord.NowWord].Sen.Count].ToString() + Environment.NewLine;
                    }
                    else
                    {
                        this.labelSen.Text = "";
                        foreach (Sen sen in NowRecord.AllWords[NowRecord.NowWord].Sen)
                            this.labelSen.Text += sen.ToString() + Environment.NewLine;
                    }
                }
                textBoxNote.Text = NowRecord.AllWords[NowRecord.NowWord].Note;
                textBoxNote.Visibility = Visibility.Hidden;
                buttonPeek.Visibility = Visibility.Visible;
            }
        }
        private void menuItemNew_Click(object sender, RoutedEventArgs e)
        {
            if (NowRecord != null)
                return;
            string filename=@"data\FullVocabulary.xml";
            if (!System.IO.File.Exists(filename))
            {
                MessageBox.Show("The data file doesn't exist!", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            loadthread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    this.SetProgressBar("Loading: " + filename, 0, 100, true);
                    FileStream fileStream = new FileStream(filename, FileMode.Open);
                    NowRecord = this.XmlRecordFormat.Deserialize(fileStream) as Record;
                    fileStream.Close();
                    this.SetProgressBar("Load complete: " + filename, 100, 100, false);
                    this.RecordFileName = "";
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        RenderWord(false, false);
                        this.AlterOptions();
                    }));
                }
                catch (Exception ex)
                {
                    this.StatusMessage = ex.ToString();
                }
            }));
            loadthread.Start();
        }
        private void CreateNewRecord(object filename)
        {
            try
            {
                ExcelApp = new Microsoft.Office.Interop.Excel.Application();
                Excel.Workbook workbook = ExcelApp.Workbooks.Open(filename.ToString(), missing, missing, missing, missing, missing, missing, missing, missing, missing, missing, missing, missing, missing, missing);
                NowRecord = new Record();
                NowRecord.LoadingVocabulary = true;
                NowRecord.ColumeDefinitionDecoded += new EventHandler((sender, e) => { this.SetProgressBar("Colume definition decoded", 0, 100, false); });
                NowRecord.WordsCounted += new EventHandler((sender, e) =>
                {
                    this.SetProgressBar("Total words: " + NowRecord.Total.ToString(), 0, NowRecord.Total, false);
                    this.Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        AlterOptions();
                        NextWord();
                    }));
                });
                NowRecord.WordDecoded += new EventHandler((sender, e) => { this.SetProgressBar("Decoding: " + NowRecord.Decoding.ToString() + " / " + NowRecord.Total.ToString(), NowRecord.Decoding); });
                NowRecord.VocabularDecoded += new EventHandler((sender, e) => { this.SetProgressBar("Decode complete", 100, 100, false); });
                NowRecord.DecodeVocabulary(workbook);
                NowRecord.LoadingVocabulary = false;
            }
            finally
            {
                if (ExcelApp != null)
                    ExcelApp.Quit();
            }
        }
        private void SetProgressBar(string statusMessage, int value, int total, bool isIndeterminate)
        {
            if (value < 0 || value > total)
                return;
            this.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                this.statusBarItemStatusMessage.Text = statusMessage;
                this.statusBarItemProgressBarMain.IsIndeterminate = isIndeterminate;
                this.statusBarItemProgressBarMain.Maximum = total;
                this.statusBarItemProgressBarMain.Value = value;
            }));
        }
        private void SetProgressBar(string statusMessage, int value)
        {
            if (value < 0)
                return;
            this.Dispatcher.BeginInvoke(new System.Action(() =>
            {
                this.statusBarItemStatusMessage.Text = statusMessage;
                this.statusBarItemProgressBarMain.Value = value;
            }));
        }
        public string StatusMessage
        {
            get { return statusBarItemStatusMessage.Text; }
            set { this.Dispatcher.BeginInvoke(new System.Action(() => { statusBarItemStatusMessage.Text = value; })); }
        }
        private void NextWord()
        {
            if (NowRecord == null)
                return;
            if (NowRecord.LoadingVocabulary)
                NowRecord.RefreshPreparedWords();
            NowRecord.GetNext();
            RenderWord(false, false);
            this.ShowNext = false;
        }
        private void SaveToXml(string filename)
        {
            if (NowRecord == null)
                return;
            savethread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    this.SetProgressBar("Saving: " + filename, 0, 100, true);
                    FileStream fileStream = new FileStream(filename, FileMode.Create);
                    this.XmlRecordFormat.Serialize(fileStream, NowRecord);
                    fileStream.Close();
                    this.SetProgressBar("Save complete: " + filename, 100, 100, false);
                }
                catch (Exception ex)
                {
                    this.StatusMessage = ex.ToString();
                }
            }));
            savethread.Start();
        }

        private void menuItemSaveAs_Click(object sender, RoutedEventArgs e)
        {
            if (NowRecord == null)
                return;
            Forms.SaveFileDialog saveFileDialogSelectRecord = new Forms.SaveFileDialog();
            saveFileDialogSelectRecord.Filter = "Dead Vocabulary Record (*.xml)|*.xml";
            saveFileDialogSelectRecord.FileName = "MyRecord.xml";
            if (saveFileDialogSelectRecord.ShowDialog() == Forms.DialogResult.OK)
                this.SaveToXml(saveFileDialogSelectRecord.FileName);
        }

        private void menuItemSave_Click(object sender, RoutedEventArgs e)
        {
            if (NowRecord == null)
                return;
            if (this.RecordFileName == "")
            {
                menuItemSaveAs_Click(sender, e);
                return;
            }
            this.SaveToXml(this.RecordFileName);
        }

        public void SetTitle(string title)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (title != "")
                    this.Title = title + " - Dead Vocabular v1.1";
                else
                    this.Title = "Dead Vocabular v1.1";
            }));
        }

        private string _recordFileName = "";
        public string RecordFileName
        {
            get { return _recordFileName; }
            set
            {
                this._recordFileName = value;
                this.SetTitle(value);
            }
        }
        private void LoadFromXml(string filename)
        {
            if (NowRecord != null)
                return;
            loadthread = new Thread(new ThreadStart(() =>
            {
                try
                {
                    this.SetProgressBar("Loading: " + filename, 0, 100, true);
                    FileStream fileStream = new FileStream(filename, FileMode.Open);
                    NowRecord = this.XmlRecordFormat.Deserialize(fileStream) as Record;
                    fileStream.Close();
                    this.SetProgressBar("Load complete: " + filename, 100, 100, false);
                    this.RecordFileName = filename;
                    this.Dispatcher.BeginInvoke(new Action(() => { RenderWord(false, false); }));
                }
                catch (Exception ex)
                {
                    this.StatusMessage = ex.ToString();
                }
            }));
            loadthread.Start();
        }

        private void menuItemLoad_Click(object sender, RoutedEventArgs e)
        {
            if (NowRecord != null)
            {
                MessageBox.Show("You are learning now! Please restart the program and start a new record.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            Forms.OpenFileDialog openFileDialogSelectRecord = new Forms.OpenFileDialog();
            openFileDialogSelectRecord.Filter = "Dead Vocabulary Record (*.xml)|*.xml";
            openFileDialogSelectRecord.FileName = "MyRecord.xml";
            if (openFileDialogSelectRecord.ShowDialog() == Forms.DialogResult.OK)
                this.LoadFromXml(openFileDialogSelectRecord.FileName);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.AutoSaveCount = int.MaxValue;
            if (ExcelApp != null)
                ExcelApp.Quit();
            if (searchBar != null)
                searchBar.Close();
        }

        private void EnterInput(string input)
        {
            if (NowRecord == null)
                return;
            this.timerAutoWrong.IsEnabled = false;
            bool jump = true;
            if (NowRecord.NowWord >= 0)
            {
                bool correct = NowRecord.Enter(input);
                jump = correct;
                if (ShowNext)
                {
                    jump = true;
                    ShowNext = false;
                }
                else
                {
                    if (input == "<F>")
                        ShowNext = true;
                }
            }
            if (jump)
                NextWord();
            else if (NowRecord.NowWord >= 0)
                RenderWord(true, true);
            AutoSaveCount++;
        }
        private void AlterOptions()
        {
            optionWindow = new OptionWindow();
            optionWindow.Owner = this;
            optionWindow.LoadOptions();
            if (optionWindow.ShowDialog() ?? false)
            {
                if (NowRecord == null)
                    return;
                optionWindow.SetOptions();
                NowRecord.RefreshPreparedWords();
                RenderWord(false, false);
            }
        }
        private void menuItemOption_Click(object sender, RoutedEventArgs e)
        {
            AlterOptions();
        }

        private void buttonRemember_Click(object sender, RoutedEventArgs e)
        {
            EnterInput("<R>");
        }

        private void buttonForget_Click(object sender, RoutedEventArgs e)
        {
            EnterInput("<F>");
        }
        private void textBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case (Key.Enter):
                    {
                        e.Handled = true;
                        EnterInput(textBoxInput.Text);
                        break;
                    }
                case (Key.Space):
                    {
                        e.Handled = true;
                        if (textBoxInput.Text == NowRecord.AllWords[NowRecord.NowWord].Spell)
                            break;
                        if (textBoxInput.Text.StartsWith("*"))
                            break;
                        if (NowRecord == null || NowRecord.NowWord == -1)
                            break;
                        string prompt = "";
                        for (int i = 0; i < NowRecord.AllWords[NowRecord.NowWord].Spell.Length; i++)
                        {
                            if (i >= textBoxInput.Text.Length || NowRecord.AllWords[NowRecord.NowWord].Spell[i] != textBoxInput.Text[i])
                            {
                                prompt += NowRecord.AllWords[NowRecord.NowWord].Spell[i];
                                break;
                            }
                            else
                            {
                                prompt += NowRecord.AllWords[NowRecord.NowWord].Spell[i];
                            }
                        }
                        if (prompt == NowRecord.AllWords[NowRecord.NowWord].Spell)
                            prompt = "*" + prompt;
                        textBoxInput.Text = prompt;
                        textBoxInput.SelectionStart = textBoxInput.Text.Length;
                        break;
                    }
            }
        }
        private void Choose(int choose)
        {
            if (choose < Choises.Count)
                EnterInput(Choises[choose]);
            else
                EnterInput("<F>");
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (NowRecord == null)
                return;
            switch (NowRecord.Settings.LearnJudge)
            {
                case LearnJudge.Choose:
                    switch (e.Key)
                    {
                        case (Key.D1):
                            e.Handled = true;
                            Choose(1);
                            break;
                        case (Key.D2):
                            e.Handled = true;
                            Choose(2);
                            break;
                        case (Key.D3):
                            e.Handled = true;
                            Choose(3);
                            break;
                        case (Key.D4):
                            e.Handled = true;
                            Choose(4);
                            break;
                        case (Key.D5):
                            e.Handled = true;
                            Choose(5);
                            break;
                        case (Key.D6):
                            e.Handled = true;
                            Choose(6);
                            break;
                        case (Key.D7):
                            e.Handled = true;
                            Choose(7);
                            break;
                        case (Key.D8):
                            e.Handled = true;
                            Choose(8);
                            break;
                    }
                    break;
                case LearnJudge.Select:
                    switch (e.Key)
                    {
                        case (Key.Oem4)://[
                            e.Handled = true;
                            buttonRemember_Click(null, new RoutedEventArgs());
                            break;
                        case (Key.Oem6)://]
                            e.Handled = true;
                            buttonForget_Click(null, new RoutedEventArgs());
                            break;
                        case (Key.OemPipe)://\
                            e.Handled = true;
                            buttonPeek_Click(null, new RoutedEventArgs());
                            break;
                    }
                    break;
            }
        }

        private void buttonPeek_Click(object sender, RoutedEventArgs e)
        {
            RenderWord(true, false);
            this.ShowNext = true;
        }

        private void textBoxNote_LostFocus(object sender, RoutedEventArgs e)
        {
            if (NowRecord == null || NowRecord.NowWord <= 0)
                return;
            NowRecord.AllWords[NowRecord.NowWord].Note = textBoxNote.Text;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.timerAutoLoad.IsEnabled = true;
        }

        private void buttonPlaySound_Click(object sender, RoutedEventArgs e)
        {
            if (NowRecord == null || NowRecord.NowWord <= 0)
                return;
            PronounceWordById(NowRecord.NowWord);
        }
        private void mainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (viewBoxContainer.Width != (viewBoxContainer.Height * MainPage.ActualWidth / MainPage.ActualHeight))
                viewBoxContainer.Width = (viewBoxContainer.Height * MainPage.ActualWidth / MainPage.ActualHeight);
        }
        private void viewBoxContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (viewBoxContainer.Width != (viewBoxContainer.Height * MainPage.ActualWidth / MainPage.ActualHeight))
                viewBoxContainer.Width = (viewBoxContainer.Height * MainPage.ActualWidth / MainPage.ActualHeight);

        }

        private void statusBarItemFullScreen_Checked(object sender, RoutedEventArgs e)
        {
            this.WindowStyle = WindowStyle.None;
            this.WindowState = WindowState.Maximized;
            if (viewBoxContainer.Width != (viewBoxContainer.Height * MainPage.ActualWidth / MainPage.ActualHeight))
                viewBoxContainer.Width = (viewBoxContainer.Height * MainPage.ActualWidth / MainPage.ActualHeight);
        }

        private void statusBarItemFullScreen_Unchecked(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.WindowStyle = WindowStyle.SingleBorderWindow;
            if (viewBoxContainer.Width != (viewBoxContainer.Height * MainPage.ActualWidth / MainPage.ActualHeight))
                viewBoxContainer.Width = (viewBoxContainer.Height * MainPage.ActualWidth / MainPage.ActualHeight);
        }

        private void menuItemUnsafeQuit_Click(object sender, RoutedEventArgs e)
        {
            this.AutoSaveLimit = 0;
            this.Close();
        }

        private void menuItemDecodeFromExcel_Click(object sender, RoutedEventArgs e)
        {
            if (NowRecord != null)
            {
                MessageBox.Show("You are learning now! Please restart the program and start a new record.", "Warning", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            if (openFileDialogSelectVocabulary.ShowDialog() == Forms.DialogResult.OK)
            {
                try
                {
                    this.SetProgressBar("Opening Excel Application...", 0, 100, true);
                    th = new System.Threading.Thread(new System.Threading.ParameterizedThreadStart(CreateNewRecord));
                    th.IsBackground = false;
                    th.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                    this.SetProgressBar("Opening Excel Application failed", 0, 100, false);
                }
            }
        }

        private void menuItemCloseNowRecord_Click(object sender, RoutedEventArgs e)
        {
            if (NowRecord != null)
            {
                if (MessageBox.Show("Do you really want to close now record without saving? (Ignore if saved)", "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    NowRecord = null;
                    RenderWord(false, false);
                }
            }
        }

        private void buttonChoose_Click(object sender, RoutedEventArgs e)
        {
            Button Sender = sender as Button;
            Choose(int.Parse(Sender.Tag.ToString()));
        }
        private void ribbon_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ribbon.SelectedItem == ribbonTabHide as object)
                ribbon.Height = 45;
            else
                ribbon.Height = double.NaN;
        }

        private void menuItemSafeQuit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void checkBoxSearchBar_Checked(object sender, RoutedEventArgs e)
        {
            if (searchBar == null)
            {
                searchBar = new SearchBar();
                searchBar.Owner = this;
                searchBar.Closed += new EventHandler((obj, ee) =>
                {
                    this.searchBar = null;
                });
            }
            searchBar.Show();
        }

        private void checkBoxSearchBar_Unchecked(object sender, RoutedEventArgs e)
        {
            if (searchBar != null)
                searchBar.Close();
        }

    }
    public class MyImageButton : Button
    {
        public static readonly DependencyProperty ImageWidthProperty = DependencyProperty.Register(
            "ImageWidth",
            typeof(double?),
            typeof(MyImageButton));
        public double? ImageWidth
        {
            get { return (double)GetValue(ImageWidthProperty); }
            set { SetValue(ImageWidthProperty, value); }
        }
        public static readonly DependencyProperty ImageHeightProperty = DependencyProperty.Register(
            "ImageHeight",
            typeof(double?),
            typeof(MyImageButton));
        public double? ImageHeight
        {
            get { return (double)GetValue(ImageHeightProperty); }
            set { SetValue(ImageHeightProperty, value); }
        }

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(
            "ImageSource",
            typeof(ImageSource),
            typeof(MyImageButton));
        public ImageSource ImageSource
        {
            get { return (ImageSource)GetValue(ImageSourceProperty); }
            set { SetValue(ImageSourceProperty, ImageSource); }
        }
    }
}
