using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using void_lib;

namespace void_util_form
{
    public partial class MainForm : Form
    {
        private static ObservableCollection<DownloadView> Downloads { get; set; } = new ObservableCollection<DownloadView>();

        public MainForm()
        {
            InitializeComponent();

            listView2.DataBindings = Downloads;
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }
    }

    internal class DownloadView
    {
        public DownloadView(Download dl)
        {
            dl.ProgressChanged += (s, e) =>
            {
                switch (e)
                {
                    case LogVoidProgress l:
                        {
                            Console.WriteLine(l.Log);
                            break;
                        }
                    case LabelVoidProgress l:
                        {
                            Label = l.Label;
                            break;
                        }
                    case PercentageVoidProgress p:
                        {
                            ProgressInner = p.Percentage;
                            break;
                        }
                }
            };
        }

        private decimal ProgressInner { get; set; }
        public string Progress => $"{(100 * ProgressInner).ToString("0.0")}%";
        public string Label { get; set; }
    }
}
