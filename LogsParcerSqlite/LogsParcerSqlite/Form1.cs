using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Data.SQLite;
using Newtonsoft.Json;

namespace Parser
{
    public partial class Form1 : Form
    {
        StreamReader sr;
        StreamWriter sw;

        private readonly int READ_BUFFER_SIZE = 10 * 1024 * 1024;
        private readonly string TABLE_CREATE = "CREATE TABLE IF NOT EXISTS Ticks(RowID INTEGER PRIMARY KEY AUTOINCREMENT,logtime TEXT, id INTEGER, count INTEGER, optionbase TEXT, fullcode TEXT, classCode TEXT, time INTEGER, sendtime INTEGER, logtimeint INTEGER, json TEXT)";
        private readonly string TABLE_REPLACE = "REPLACE INTO Ticks(logtime, id, count, optionbase, fullcode, classCode, time, sendtime, logtimeint, json) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

        private String dbFileName;
        private SQLiteConnection m_dbConn;
        private SQLiteCommand m_sqlCmd;

        Dictionary<String, Decimal> lasts = new Dictionary<string, decimal>();

        public Form1()
        {
            InitializeComponent();
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = File.Open(dialog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                BufferedStream bs = new BufferedStream(fs, READ_BUFFER_SIZE);
                sr = new StreamReader(bs);
                string dir = Path.GetDirectoryName(fs.Name);
                string filename = Path.GetFileNameWithoutExtension(fs.Name) + ".db3";
                dbFileName = Path.Combine(dir, filename);
            }
        }

        private void calcButton_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(calcButton_Click_Async));
            thread.Start();
        }

        private void calcButton_Click_Async()
        {
            try
            {
                m_dbConn = new SQLiteConnection("Data Source=" + dbFileName + ";Version=3;");
                m_sqlCmd = new SQLiteCommand();
                m_dbConn.Open();
                m_sqlCmd.Connection = m_dbConn;

                m_sqlCmd.CommandText = TABLE_CREATE;
                m_sqlCmd.ExecuteNonQuery();
            }
            catch (SQLiteException ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }

            string read = null;
            int index = 0;

            using (SQLiteTransaction tr = m_dbConn.BeginTransaction())
            {
                while ((read = sr.ReadLine()) != null)
                {
                    processString(read);
                    index++;
                    if (index % 10000 == 0)
                    {
                        Console.WriteLine("{0} processed {1} lines", DateTime.Now, index);
                    }
                }
                tr.Commit();
            }

            Console.WriteLine("Done");

            sr.Close();
            m_dbConn.Close();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                sw = new StreamWriter(dialog.FileName);
            }

            sw.Close();
        }

        private void processString(String read)
        {
            if (read.Contains("CHUNK: "))
            {
                String[] words = read.Split(new[] { "CHUNK: " }, StringSplitOptions.None);
                String logTime = words[0].Split(' ')[0];
                int logTimeInt = Convert.ToInt32(TimeSpan.Parse(logTime).ToString("hhmmss"));
                JObject json = JObject.Parse(words[words.Length - 1]);

                if (json.ContainsKey("body"))
                {
                    int chunkId = (int)json["id"];
                    int count = json.Value<int?>("count") ?? -1;
                    JArray body = (JArray) json["body"];

                    foreach (var item in body.Children())
                    {
                        string optionbase = (string)item["optionbase"];
                        if (optionbase == null)
                        {
                            continue; //filtering only interesting us data
                        }

                        int time = (int)item["time"];
                        int sendtime = item.Value<int?>("sendtime") ?? -1;
                        string fullcode = (string)item["fullCode"];
                        string classcode = (string)item["classCode"];

                        try
                        {
                            m_sqlCmd.CommandText = TABLE_REPLACE;
                            m_sqlCmd.Parameters.AddWithValue("logtime", logTime);
                            m_sqlCmd.Parameters.AddWithValue("id", chunkId);
                            m_sqlCmd.Parameters.AddWithValue("count", count);
                            m_sqlCmd.Parameters.AddWithValue("optionbase", optionbase);
                            m_sqlCmd.Parameters.AddWithValue("fullcode", fullcode);
                            m_sqlCmd.Parameters.AddWithValue("classcode", classcode);
                            m_sqlCmd.Parameters.AddWithValue("time", time);
                            m_sqlCmd.Parameters.AddWithValue("sendtime", sendtime);
                            m_sqlCmd.Parameters.AddWithValue("logtimeint", logTimeInt);
                            m_sqlCmd.Parameters.AddWithValue("json", item.ToString(Formatting.None));


                            m_sqlCmd.ExecuteNonQuery();
                        }
                        catch (SQLiteException ex)
                        {
                            MessageBox.Show("Error: " + ex.Message);
                        }
                    }

                }
            }
        }

        private void openFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();


            if (dialog.ShowDialog() == DialogResult.OK)
            {
                sr = File.OpenText(dialog.FileName);
            }
        }
    }


}
