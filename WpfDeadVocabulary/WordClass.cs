using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Excel = Microsoft.Office.Interop.Excel;
using System.Data;

namespace WpfDeadVocabulary
{

    [Serializable]
    public class Record
    {
        public List<Word> AllWords = new List<Word>();
        public List<List<int>> DivideWords = new List<List<int>>();
        public List<int> NewWords = new List<int>();
        public List<int> PreparedWords = new List<int>();
        public List<int> CustomWords = new List<int>();
        public List<int> FamiliarWords = new List<int>();
        public int Decoding { get; set; }
        public int Total { get; set; }
        public int NowWord = -1;
        public int WrongWord = -1;
        public LearnSettings Settings { get; set; }
        public ColumeDefinition ColDefinition { get; set; }
        public Record() { }
        private static bool ExistSheet(Excel.Sheets sheets, string name)
        {
            for (int i = 1; i <= sheets.Count; i++)
                if ((sheets[i] as Excel.Worksheet).Name == name)
                    return true;
            return false;
        }
        public event EventHandler ColumeDefinitionDecoded;
        public event EventHandler WordsCounted;
        public event EventHandler WordDecoded;
        public event EventHandler VocabularDecoded;
        public bool DecodeVocabulary(Excel.Workbook workbook)
        {
            this.ColDefinition = new ColumeDefinition();
            Excel.Worksheet columeDefinitionSheet = workbook.Sheets["ColumeDefinition"] as Excel.Worksheet;
            for (int i = 1; i <= columeDefinitionSheet.UsedRange.Rows.Count; i++)
            {
                switch ((columeDefinitionSheet.Cells[i, 1] as Excel.Range).Text.ToString())
                {
                    case "Id":
                        int.TryParse((columeDefinitionSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out this.ColDefinition.Id);
                        break;
                    case "Spell":
                        int.TryParse((columeDefinitionSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out this.ColDefinition.Spell);
                        break;
                    case "Pho":
                        int.TryParse((columeDefinitionSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out this.ColDefinition.Pho);
                        break;
                    case "DesNum":
                        int.TryParse((columeDefinitionSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out this.ColDefinition.DesNum);
                        break;
                    case "SenNum":
                        int.TryParse((columeDefinitionSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out this.ColDefinition.SenNum);
                        break;
                    case "Hash":
                        int.TryParse((columeDefinitionSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out this.ColDefinition.Hash);
                        break;
                    case "Book":
                        int.TryParse((columeDefinitionSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out this.ColDefinition.Book);
                        break;
                    case "Unit":
                        int.TryParse((columeDefinitionSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out this.ColDefinition.Unit);
                        break;
                    case "Order":
                        int.TryParse((columeDefinitionSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out this.ColDefinition.Order);
                        break;
                    case "Source":
                        int.TryParse((columeDefinitionSheet.Cells[i, 2] as Excel.Range).Text.ToString(), out this.ColDefinition.Source);
                        int sourcesNum;
                        if (int.TryParse((columeDefinitionSheet.Cells[i, 3] as Excel.Range).Text.ToString(), out sourcesNum))
                            for (int j = 1; j <= sourcesNum; j++)
                                this.ColDefinition.Sources.Add((columeDefinitionSheet.Cells[i, 3 + j] as Excel.Range).Text.ToString());
                        break;

                }
            }
            if (ColumeDefinitionDecoded != null) { ColumeDefinitionDecoded(this, new EventArgs()); }
            System.Threading.Thread.Sleep(1000);
            this.Settings = new LearnSettings(this.ColDefinition);
            Excel.Worksheet worksheet = null;
            if (ExistSheet(workbook.Sheets, "Compact"))
            {
                worksheet = workbook.Sheets["Compact"] as Excel.Worksheet;
            }
            else if (ExistSheet(workbook.Sheets, "All"))
            {
                worksheet = workbook.Sheets["All"] as Excel.Worksheet;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("No vocabulary sheet");
                return false;
            }
            Total = worksheet.UsedRange.Rows.Count;
            if (WordsCounted != null) { WordsCounted(this, new EventArgs()); }
            System.Threading.Thread.Sleep(1000);
            AllWords.Add(new Word());
            for (int i = 1; i <= Total; i++)
            {
                AllWords.Add(new Word(worksheet, this.ColDefinition, i));
                NewWords.Add(i);
                this.Decoding = i;
                if (WordDecoded != null) { WordDecoded(this, new EventArgs()); }
            }
            if (VocabularDecoded != null) { VocabularDecoded(this, new EventArgs()); }
            System.Threading.Thread.Sleep(1000);
            return true;
        }
        public bool LoadingVocabulary { get; set; }
        Random randseed = new Random();
        public bool GetNext()
        {
            if (NowWord > 0)
            {
                if (AllWords[NowWord].Level == int.MaxValue)
                {
                    FamiliarWords.Add(NowWord);
                    NowWord = -1;
                }
                else
                {
                    while (AllWords[NowWord].Level >= DivideWords.Count)
                        DivideWords.Add(new List<int>());
                    if (DivideWords[AllWords[NowWord].Level].Count == 0)
                    {
                        DivideWords[AllWords[NowWord].Level].Add(NowWord);
                        NowWord = -1;
                    }
                    else
                    {
                        for (int i = DivideWords[AllWords[NowWord].Level].Count - 1; i >= 0; i--)
                        {
                            if (AllWords[DivideWords[AllWords[NowWord].Level][i]].Next < AllWords[NowWord].Next)
                            {
                                DivideWords[AllWords[NowWord].Level].Insert(i + 1, NowWord);
                                NowWord = -1;
                                break;
                            }
                        }
                    }
                }
                if (NowWord > 0)
                {
                    DivideWords[AllWords[NowWord].Level].Add(NowWord);
                    NowWord = -1;
                }
            }
            foreach (List<int> words in DivideWords)
            {
                if (words.Count > 0)
                    if (AllWords[words[0]].Next < DateTime.Now)
                    {
                        NowWord = words[0];
                        words.RemoveAt(0);
                        return true;
                    }
            }
            int tmpIndex = -1;
            if (CustomWords.Count > 0)
            {
                tmpIndex = 0;
                NowWord = PreparedWords.ElementAt(tmpIndex);
                PreparedWords.RemoveAt(tmpIndex);
                return true;
            }
            if (PreparedWords.Count > 0)
            {
                tmpIndex = -1;
                switch (this.Settings.LearnOrder)
                {
                    case LearnOrder.Random:
                        tmpIndex = (PreparedWords.Count > 0) ? randseed.Next() % PreparedWords.Count : tmpIndex;
                        break;
                    case LearnOrder.Orderly:
                        for (int i = 0; i < PreparedWords.Count; i++)
                            tmpIndex = (tmpIndex == -1) || (string.Compare(AllWords[PreparedWords[i]].Spell, AllWords[PreparedWords[tmpIndex]].Spell) < 0) ? i : tmpIndex;
                        break;
                    case LearnOrder.Unit:
                        for (int i = 0; i < PreparedWords.Count; i++)
                            tmpIndex = (tmpIndex == -1) || (AllWords[PreparedWords[i]].Id < AllWords[PreparedWords[tmpIndex]].Id) ? i : tmpIndex;
                        break;
                }
                tmpIndex = (tmpIndex == -1) ? 0 : tmpIndex;
                NowWord = PreparedWords.ElementAt(tmpIndex);
                PreparedWords.RemoveAt(tmpIndex);
                return true;
            }
            if (AllWords.Count > 100)
            {
                tmpIndex = randseed.Next() % 100;
                for (int i = 0; i < DivideWords.Count; i++)
                {
                    if (DivideWords[i].Count > tmpIndex)
                    {
                        NowWord = DivideWords[i].ElementAt(tmpIndex);
                        DivideWords[i].RemoveAt(tmpIndex);
                        return true;
                    }
                    else
                    {
                        tmpIndex -= DivideWords[i].Count;
                    }
                }
            }
            if (NewWords.Count > 0)
            {
                tmpIndex = randseed.Next() % NewWords.Count;
                NowWord = NewWords.ElementAt(tmpIndex);
                NewWords.RemoveAt(tmpIndex);
                return true;
            }
            return false;
        }
        public void RefreshPreparedWords()
        {
            NewWords.AddRange(PreparedWords);
            PreparedWords.Clear();
            NewWords.Sort();
            for (int i = NewWords.Count - 1; i >= 0; i--)
            {
                if ((AllWords[NewWords[i]].Source & Settings.SourceScale) > 0)
                {
                    PreparedWords.Add(NewWords[i]);
                    NewWords.RemoveAt(i);
                }
            }
        }
        [NonSerialized]
        Dictionary<string, int> _searchIdByWordDictionary=new Dictionary<string,int>();
        public int SearchIdByWord(string word)
        {
            if(_searchIdByWordDictionary.Keys.Contains(word))
                return _searchIdByWordDictionary[word];
            for(int i=1;i<AllWords.Count;i++)
                if(AllWords[i].Spell==word)
                    return i;
            return -1;
        }
        public bool Enter(string input)
        {
            WrongWord = -1;
            if (AllWords[NowWord].Enter(input))
                return true;
            if (input != "<F>" && input != "")
                WrongWord = SearchIdByWord(input);
            return false;
        }
        public override string ToString()
        {
            string str = "";
            str += "==========RECORD==========" + Environment.NewLine;
            str += "Total: " + this.Total.ToString() + Environment.NewLine;
            str += "New words: " + this.NewWords.Count.ToString() + Environment.NewLine;
            str += "Prepared words: " + this.PreparedWords.Count.ToString() + Environment.NewLine;
            str += "Custom words: " + this.CustomWords.Count.ToString() + Environment.NewLine;
            str += "Known words: " + this.FamiliarWords.Count.ToString() + Environment.NewLine;
            str += Environment.NewLine;
            int seen = 0;
            for (int i = 0; i < this.DivideWords.Count; i++)
            {
                str += "Level[" + i.ToString() + "]: " + this.DivideWords[i].Count.ToString() + Environment.NewLine;
                seen += this.DivideWords[i].Count;
            }
            str += "Seen words: " + seen.ToString() + Environment.NewLine;
            str += Environment.NewLine;

            str += "==========NOW WORD==========" + Environment.NewLine;
            if (this.NowWord == -1)
            {
                str += "No word." + Environment.NewLine;
            }
            else
            {
                str += AllWords[NowWord].ToString();
                str += "Sources: ";
                for (int i = 0; i < this.ColDefinition.Sources.Count; i++)
                    if ((AllWords[NowWord].Source & BIT.B[i]) > 0)
                        str += this.ColDefinition.Sources[i] + "; ";
                str += Environment.NewLine;
            }
            return str;
        }
    }

    [Serializable]
    public class Word
    {
        public static TimeSpan[] Span = { TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(30), TimeSpan.FromHours(1), TimeSpan.FromHours(8), TimeSpan.FromDays(1), TimeSpan.FromDays(2), TimeSpan.FromDays(4), TimeSpan.FromDays(7), TimeSpan.FromDays(15), TimeSpan.FromDays(31), TimeSpan.FromDays(90), TimeSpan.FromDays(180), TimeSpan.FromDays(365), TimeSpan.FromDays(3652) };
        public int Id { get; set; }
        public int Level { get; set; }
        public int TopLevel { get; set; }
        public DateTime First { get; set; }
        public DateTime Last { get; set; }
        public DateTime Next { get; set; }
        public int Wrong { get; set; }
        public int Right { get; set; }
        public string ExactId { get; set; }
        public string Spell { get; set; }
        public string Pho { get; set; }
        public List<Des> Des = new List<Des>();
        public List<Sen> Sen = new List<Sen>();
        public string Hash { get; set; }
        public string Book { get; set; }
        public int Unit { get; set; }
        public int Order { get; set; }
        public ulong Source { get; set; }
        public string Note { get; set; }
        public Word() { }
        public Word(Excel.Worksheet worksheet, ColumeDefinition cd, int id)
        {

            Id = id;
            Level = 0;
            TopLevel = 0;
            First = Last = Next = DateTime.MinValue;
            Wrong = Right = 0;
            int tmp;
            this.ExactId = (worksheet.Cells[Id, cd.Id] as Excel.Range).Text.ToString();
            this.Spell = (worksheet.Cells[Id, cd.Spell] as Excel.Range).Text.ToString();
            this.Pho = (worksheet.Cells[Id, cd.Pho] as Excel.Range).Text.ToString();
            int desnum = 0;
            if (int.TryParse((worksheet.Cells[Id, cd.DesNum] as Excel.Range).Text.ToString(), out desnum))
                for (int i = 0; i < desnum; i++)
                    this.Des.Add(new Des((worksheet.Cells[Id, cd.DesNum + i * 2 + 1] as Excel.Range).Text.ToString(), (worksheet.Cells[Id, cd.DesNum + i * 2 + 2] as Excel.Range).Text.ToString()));
            int sennum = 0;
            if (int.TryParse((worksheet.Cells[Id, cd.SenNum] as Excel.Range).Text.ToString(), out sennum))
                for (int i = 0; i < sennum; i++)
                    this.Sen.Add(new Sen((worksheet.Cells[Id, cd.SenNum + i * 2 + 1] as Excel.Range).Text.ToString(), (worksheet.Cells[Id, cd.SenNum + i * 2 + 2] as Excel.Range).Text.ToString()));
            this.Hash = (worksheet.Cells[Id, cd.Hash] as Excel.Range).Text.ToString();
            this.Book = (worksheet.Cells[Id, cd.Book] as Excel.Range).Text.ToString();
            int.TryParse((worksheet.Cells[Id, cd.Unit] as Excel.Range).Text.ToString(), out tmp);
            this.Unit = tmp;
            int.TryParse((worksheet.Cells[Id, cd.Order] as Excel.Range).Text.ToString(), out tmp);
            this.Order = tmp;
            this.Source = 0;
            for (int i = 0; i < cd.Sources.Count; i++)
            {
                if ((worksheet.Cells[Id, cd.Source + i] as Excel.Range).Text.ToString() == "1")
                    this.Source |= BIT.B[i];
            }
            this.Note = "";
        }
        public override string ToString()
        {
            string result = "";
            result += "Id: " + Id.ToString() + Environment.NewLine;
            result += "Level: " + Level.ToString() + Environment.NewLine;
            result += "TopLevel: " + TopLevel.ToString()+Environment.NewLine;
            result += "First: " + First.ToString() + Environment.NewLine;
            result += "Last: " + Last.ToString() + Environment.NewLine;
            result += "Next: " + Next.ToString() + Environment.NewLine;
            result += "Wrong: " + Wrong.ToString() + Environment.NewLine;
            result += "Right: " + Right.ToString() + Environment.NewLine;
            result += "ExactId: " + ExactId.ToString() + Environment.NewLine;
            result += "Hash: " + Hash + Environment.NewLine;
            result += "Spell: " + Spell.ToString() + Environment.NewLine;
            result += "Pho: " + Pho.ToString() + Environment.NewLine;
            result += "Des: ";
            for (int i = 0; i < Des.Count; i++)
                result += Des[i].ToString() + "; ";
            result += Environment.NewLine;
            result += "Sen: ";
            for (int i = 0; i < Sen.Count; i++)
                result += Sen[i].ToString() + "; ";
            result += Environment.NewLine;
            return result;
        }
        public bool Enter(string input)
        {
            Last = DateTime.Now;
            bool judge = input == Spell || input == (Spell + "+")||input == (Spell + "++") || input == "<K>" || input == "<R>" || input == "<C>";
            if (judge)
            {
                if (input == Spell + "++" || input == "<K>")
                {
                    Right++;
                    Level = int.MaxValue;
                    Next = DateTime.MaxValue;
                }
                else if (input == Spell + "+")
                {
                    Right++;
                    Level = TopLevel;
                    if (Level < Span.Count())
                        Next = DateTime.Now + Span[Level];
                    else
                        Next = DateTime.MaxValue;
                }
                else if (DateTime.Now > Next || DateTime.Now > Last + TimeSpan.FromDays(2))
                {
                    Right++;
                    Level++;
                    if (First == DateTime.MinValue && input != "<C>")
                        Level += 3;
                    if (Wrong == 0 && input != "<C>")
                        Level++;
                    if ((DateTime.Now - Next) > Span[Level])
                        Level++;
                    TopLevel = (Level > TopLevel) ? Level : TopLevel;
                    if (Level < Span.Count())
                        Next = DateTime.Now + Span[Level];
                    else
                        Next = DateTime.MaxValue;
                }
            }
            else
            {
                Wrong++;
                Level = (Level <= 0) ? 0 : Level - 1;
                Next = DateTime.Now + Span[Level];
            }
            if (First == DateTime.MinValue)
                First = DateTime.Now;
            return judge;
        }
    }

    [Serializable]
    public class Des
    {
        public string D { get; set; }
        public string P { get; set; }
        public Des() { }
        public Des(string d, string p)
        {
            D = d;
            P = p;
        }
        public override string ToString()
        {
            return D + P;
        }
    }

    [Serializable]
    public class Sen
    {
        public string Es { get; set; }
        public string Cs { get; set; }
        public Sen() { }
        public Sen(string es, string cs)
        {
            Es = es;
            Cs = cs;
        }
        public override string ToString()
        {
            return Es + " " + Cs;
        }
    }

    [Serializable]
    public class ColumeDefinition
    {
        public int Id = 1;
        public int Spell = 2;
        public int Pho = 3;
        public int DesNum = 4;
        public int SenNum = 25;
        public int Hash = 46;
        public int Book = 47;
        public int Unit = 48;
        public int Order = 49;
        public int Link = 50;
        public int Source = 51;
        public List<string> Sources = new List<string>();
    }

    public enum LearnOrder { Orderly, Random, Unit }
    public enum LearnJudge { Spell, Select, Choose }
    [Serializable]
    public class LearnSettings
    {
        public bool ShowWord { get; set; }
        public bool PlaySound { get; set; }
        public bool ShowPho { get; set; }
        public bool ShowDes { get; set; }
        public bool ShowDesOne { get; set; }
        public bool ShowSen { get; set; }
        public bool ShowSenOne { get; set; }
        public LearnOrder LearnOrder { get; set; }
        public LearnJudge LearnJudge { get; set; }
        public ulong SourceScale { get; set; }

        public LearnSettings()
        {
            ShowWord = true;
            PlaySound = true;
            ShowPho = true;
            ShowDes = true;
            ShowDesOne = false;
            ShowSen = true;
            ShowSenOne = false;
            this.LearnOrder = LearnOrder.Random;
            this.LearnJudge = LearnJudge.Spell;
        }
        public LearnSettings(ColumeDefinition cd)
            : this()
        {
            for (int i = 0; i < cd.Sources.Count; ++i)
                SourceScale |= BIT.B[i];
        }
    }
    public class BIT
    {
        public static ulong[] B ={
            0x00000001,0x00000002,0x00000004,0x00000008,
            0x00000010,0x00000020,0x00000040,0x00000080,
            0x00000100,0x00000200,0x00000400,0x00000800,
            0x00001000,0x00002000,0x00004000,0x00008000,
            0x00010000,0x00020000,0x00040000,0x00080000,
            0x00100000,0x00200000,0x00400000,0x00800000,
            0x01000000,0x02000000,0x04000000,0x08000000,
            0x10000000,0x20000000,0x40000000,0x80000000
            };
    }
}