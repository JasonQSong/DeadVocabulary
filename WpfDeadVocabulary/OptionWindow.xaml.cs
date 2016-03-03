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
    /// OptionWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OptionWindow : Window
    {
        public OptionWindow()
        {
            InitializeComponent();
        }
        private Record NowRecord
        {
            get { return (Owner as StudyWindow).NowRecord; }
        }
        public List<SourceWrap> SourcesData = new List<SourceWrap>();
        public void LoadOptions()
        {
            if (NowRecord == null)
            {
                textBoxDetail.Text = "No Record." + Environment.NewLine;
                return;
            }
            textBoxDetail.Text = "";
            checkBoxShowWord.IsChecked = NowRecord.Settings.ShowWord;
            checkBoxPlaySound.IsChecked = NowRecord.Settings.PlaySound;
            checkBoxShowPho.IsChecked = NowRecord.Settings.ShowPho;
            checkBoxShowDes.IsChecked = NowRecord.Settings.ShowDes;
            checkBoxShowDesOne.IsChecked = NowRecord.Settings.ShowDesOne;
            checkBoxShowSen.IsChecked = NowRecord.Settings.ShowSen;
            checkBoxShowSenOne.IsChecked = NowRecord.Settings.ShowSenOne;
            switch (NowRecord.Settings.LearnOrder)
            {
                case LearnOrder.Orderly:
                    radioNewWordsOrderly.IsChecked = true;
                    break;
                case LearnOrder.Random:
                    radioNewWordsRandom.IsChecked = true;
                    break;
                case LearnOrder.Unit:
                    radioNewWordsUnit.IsChecked = true;
                    break;
            }
            switch (NowRecord.Settings.LearnJudge)
            {
                case LearnJudge.Spell:
                    radioJudgeSpell.IsChecked = true;
                    break;
                case LearnJudge.Select:
                    radioJudgeSelect.IsChecked = true;
                    break;
                case LearnJudge.Choose:
                    radioJudgeChoose.IsChecked = true;
                    break;
            }
            this.SourcesData.Clear();
            for (int i = 0; i < NowRecord.ColDefinition.Sources.Count; i++)
            {
                this.SourcesData.Add(new SourceWrap() { BookSelected = (NowRecord.Settings.SourceScale & BIT.B[i]) > 0, Book = NowRecord.ColDefinition.Sources[i],Count=0 });
            }
            for (int i = 0; i < NowRecord.PreparedWords.Count; i++)
                for (int j = 0; j < SourcesData.Count; j++)
                    if ((NowRecord.AllWords[NowRecord.PreparedWords[i]].Source & BIT.B[j]) > 0)
                        SourcesData[j].Count++;
            for (int i = 0; i < NowRecord.NewWords.Count; i++)
                for (int j = 0; j < SourcesData.Count; j++)
                    if ((NowRecord.AllWords[NowRecord.NewWords[i]].Source & BIT.B[j]) > 0)
                        SourcesData[j].Count++;
            this.listViewSelectSources.ItemsSource = this.SourcesData;
            textBoxDetail.Text = NowRecord.ToString();
        }
        public bool SetOptions()
        {
            if (NowRecord == null)
                return false;
            NowRecord.Settings.ShowWord = checkBoxShowWord.IsChecked ?? false;
            NowRecord.Settings.PlaySound = checkBoxPlaySound.IsChecked ?? false;
            NowRecord.Settings.ShowPho = checkBoxShowPho.IsChecked ?? false;
            NowRecord.Settings.ShowDes = checkBoxShowDes.IsChecked ?? false;
            NowRecord.Settings.ShowDesOne = checkBoxShowDesOne.IsChecked ?? false;
            NowRecord.Settings.ShowSen = checkBoxShowSen.IsChecked ?? false;
            NowRecord.Settings.ShowSenOne = checkBoxShowSenOne.IsChecked ?? false;
            if (radioNewWordsOrderly.IsChecked ?? false)
                NowRecord.Settings.LearnOrder = LearnOrder.Orderly;
            else if (radioNewWordsRandom.IsChecked ?? false)
                NowRecord.Settings.LearnOrder = LearnOrder.Random;
            else if (radioNewWordsUnit.IsChecked ?? false)
                NowRecord.Settings.LearnOrder = LearnOrder.Unit;
            if (radioJudgeSpell.IsChecked ?? false)
                NowRecord.Settings.LearnJudge = LearnJudge.Spell;
            else if (radioJudgeSelect.IsChecked ?? false)
                NowRecord.Settings.LearnJudge = LearnJudge.Select;
            else if (radioJudgeChoose.IsChecked ?? false)
                NowRecord.Settings.LearnJudge = LearnJudge.Choose;
            NowRecord.Settings.SourceScale = 0;
            for (int i = 0; i < NowRecord.ColDefinition.Sources.Count; i++)
                if (this.SourcesData[i].BookSelected)
                    NowRecord.Settings.SourceScale |= BIT.B[i];
            return true;
        }
        private void buttonOK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void buttonCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        public class SourceWrap
        {
            public string Book { get; set; }
            public int Count { get; set; }
            public bool BookSelected { get; set; }
        }

        private void buttonPresetMaster_Click(object sender, RoutedEventArgs e)
        {
            checkBoxShowWord.IsChecked = false;
            checkBoxPlaySound.IsChecked = false;
            checkBoxShowPho.IsChecked = false;
            checkBoxShowDes.IsChecked = true;
            checkBoxShowDesOne.IsChecked = false;
            checkBoxShowSen.IsChecked = false;
            checkBoxShowSenOne.IsChecked = false;
            radioNewWordsRandom.IsChecked = true;
            radioJudgeSpell.IsChecked = true;
        }

        private void buttonPresetUnderstand_Click(object sender, RoutedEventArgs e)
        {
            checkBoxShowWord.IsChecked = true;
            checkBoxPlaySound.IsChecked = true;
            checkBoxShowPho.IsChecked = true;
            checkBoxShowDes.IsChecked = false;
            checkBoxShowDesOne.IsChecked = false;
            checkBoxShowSen.IsChecked = false;
            checkBoxShowSenOne.IsChecked = false;
            radioNewWordsRandom.IsChecked = true;
            radioJudgeSelect.IsChecked = true;
        }

        private void buttonPresetVague_Click(object sender, RoutedEventArgs e)
        {
            checkBoxShowWord.IsChecked = true;
            checkBoxPlaySound.IsChecked = true;
            checkBoxShowPho.IsChecked = true;
            checkBoxShowDes.IsChecked = true;
            checkBoxShowDesOne.IsChecked = false;
            checkBoxShowSen.IsChecked = false;
            checkBoxShowSenOne.IsChecked = false;
            radioNewWordsRandom.IsChecked = true;
            radioJudgeChoose.IsChecked = true;
        }

    }

}