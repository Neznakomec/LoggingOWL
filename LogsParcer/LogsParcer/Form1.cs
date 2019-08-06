using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace Parser
{
    public partial class Form1 : Form
    {
        StreamReader sr;
        StreamWriter sw;
        List<String> records;

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
                BufferedStream bs = new BufferedStream(fs, 10485760);
                sr = new StreamReader(bs);
            }
        }

        private void calcButton_Click(object sender, EventArgs e)
        {
            Thread thread = new Thread(new ThreadStart(calcButton_Click_Async));
            thread.Start();
        }

        private void calcButton_Click_Async()
        {
            records = new List<string>();

            string read = null;
            int index = 0;
            records.Add("instrument;time;bid;ask;last");
            while ((read = sr.ReadLine()) != null)
            {
                string result = processString(read);
                if (result != null)
                {
                    records.Add(result);
                }
                index++;
                /*if (index % 10000 == 0) { 
                Console.WriteLine("{0} processed {1} lines", DateTime.Now, index);
                }*/
            }
            Console.WriteLine("Done");

            sr.Close();

        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                sw = new StreamWriter(dialog.FileName);
            }

            for (int i = 0; i < records.Count; i++)
            {
                sw.WriteLine(records[i]);
            }

            records.Clear();

            sw.Close();

        }

        Regex shooterString = new Regex(@"(.*) T.* INF.*OW-1475: (.*) underlying=\(([\d.]+)M\/([\d.]+)M\/([\d.]+)M\), bidPrice=([\d.]+)M, askPrice=([\d.]+)M");

        private String processString(String read)
        {
            if (!read.Contains("underlying"))
            {
                return null;
            }
            read = read.Trim();
            Match matchShooter = shooterString.Match(read);
            if (matchShooter.Success == false)
            {
                return null;
            }

            TimeSpan time = TimeSpan.Parse(matchShooter.Groups[1].Value);
            String instrument = matchShooter.Groups[2].Value;
            Decimal bid = Decimal.Parse(matchShooter.Groups[3].Value, CultureInfo.InvariantCulture);
            Decimal last = Decimal.Parse(matchShooter.Groups[4].Value, CultureInfo.InvariantCulture);
            Decimal ask = Decimal.Parse(matchShooter.Groups[5].Value, CultureInfo.InvariantCulture);

            String warn = null;
            Decimal prev, curr;

            curr = bid;
            if (!lasts.TryGetValue(instrument, out prev))
            {
                prev = curr;
            }
            lasts[instrument] = curr;
            if ((instrument.Contains("ED") && Math.Abs(bid - prev) >= 0.0005m)
                ||
                (instrument.Contains("BR") && Math.Abs(bid - prev) >= 0.04m)
                ||
                (instrument.Contains("Si") && Math.Abs(bid - prev) >= 20m))
            {
                warn = String.Format("{0};{1};{2}->{3}", time, instrument, prev, curr);
            }

            if (warn != null)
            {
                Console.WriteLine(warn);
            }
            return String.Format("{0};{1};{2};{3};{4}", instrument, time, bid, ask, last);
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
