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

namespace WpfDeadVocabulary
{
    /// <summary>
    /// SearchBar.xaml 的交互逻辑
    /// </summary>
    public partial class SearchBar : Window
    {
        public SearchBar()
        {
            InitializeComponent();
            this.Left = (SystemParameters.WorkArea.Width - this.Width) / 2;
            this.Top = 0;
            /*
            System.Windows.Threading.DispatcherTimer timerBringToFront = new System.Windows.Threading.DispatcherTimer();
            timerBringToFront.Interval = TimeSpan.FromSeconds(5);
            timerBringToFront.Tick += new EventHandler((sender, e) =>
            {
                this.Activate();
            });
            timerBringToFront.Start();*/
        }
        private Record NowRecord
        {
            get { return (Owner as StudyWindow).NowRecord; }
        }
        private void textBoxSearchKey_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NowRecord == null)
                return;
            this.labelWordDetail.Text = "";
            this.labelTip.Text = "";
            int id = NowRecord.SearchIdByWord(textBoxSearchKey.Text);
            if (id > 0)
            {
                this.labelWordDetail.Text += NowRecord.AllWords[id].Spell + Environment.NewLine;
                this.labelWordDetail.Text += NowRecord.AllWords[id].Pho + Environment.NewLine;
                foreach (Des des in NowRecord.AllWords[id].Des)
                {
                    this.labelWordDetail.Text += des.ToString() + Environment.NewLine;
                }
                gridDetails.Visibility = Visibility.Visible;
                buttonAdd.Visibility = Visibility.Visible;
                if((Owner as StudyWindow).CanPronounceWordById(id))
                    buttonPlaySound.Visibility = Visibility.Visible;
            }
            else
            {
                gridDetails.Visibility = Visibility.Collapsed;
                buttonAdd.Visibility = Visibility.Collapsed;
                buttonPlaySound.Visibility = Visibility.Collapsed;
            }
        }

        private void buttonAdd_Click(object sender, RoutedEventArgs e)
        {
            AddWordToCustom();
        }
        public void AddWordToCustom()
        {
            if (NowRecord == null)
                return;
            int id = NowRecord.SearchIdByWord(textBoxSearchKey.Text);
            if (id > 0)
                if (NowRecord.AllWords[id].Level == 0)
                {
                    int index = NowRecord.NewWords.IndexOf(id);
                    if (index >= 0)
                    {
                        NowRecord.NewWords.RemoveAt(index);
                        NowRecord.CustomWords.Add(id);
                        labelTip.Text = "Add from new words";
                        return;
                    }
                    index = NowRecord.PreparedWords.IndexOf(id);
                    if (index >= 0)
                    {
                        NowRecord.PreparedWords.RemoveAt(index);
                        NowRecord.CustomWords.Add(id);
                        labelTip.Text = "Add from selected words";
                        return;
                    }

                }
        }

        private void textBoxSearchKey_GotFocus(object sender, RoutedEventArgs e)
        {
            this.textBoxSearchKey.Text="";
        }

        private void Window_Activated_1(object sender, EventArgs e)
        {
            this.textBoxSearchKey.Text = "";
            this.textBoxSearchKey.Focus();
        }

        private void textBoxSearchKey_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    AddWordToCustom();
                    break;
            }
        }

        private void buttonPlaySound_Click(object sender, RoutedEventArgs e)
        {
            if (NowRecord == null)
                return;
            int id = NowRecord.SearchIdByWord(textBoxSearchKey.Text);
            if (id > 0)
                (Owner as StudyWindow).PronounceWordById(id);
        }

    }
}
