using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
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
            Downloads.CollectionChanged += (s, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        {
                            foreach (var item in e.NewItems)
                            {
                                var i = (DownloadView)item;
                                listView2.Items.Add(new ListViewItem(new string[] { i.Id.ToString(), "??", "0.00%", "0 B/s" })
                                {
                                    Name = i.Id.ToString()
                                });
                            }
                            break;
                        }
                }
            };
        }

        private string FormatBytes(decimal x)
        {
            if(x >= 1073741824)
            {
                return $"{(x / 1073741824m).ToString("0.00")} GiB";
            } else if(x >= 1048576)
            {
                return $"{(x / 1048576m).ToString("0.00")} MiB";
            }
            else if(x >= 1024)
            {
                return $"{(x / 1024m).ToString("0.00")} KiB";
            }else
            {
                return $"{(x).ToString("0.00")} B";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Downloads.Add(new DownloadView(textBox1.Text, (s, ev) =>
            {
                switch (ev)
                {
                    case PercentageVoidProgress p:
                        {
                            listView2.BeginInvoke(new Action(() =>
                            {
                                var i = listView2.Items.Find(ev.Id.ToString(), false);
                                var dl = ((Download)s);
                                foreach (var ii in i)
                                {
                                    ii.SubItems[1].Text = dl.Header?.name ?? "??";
                                    ii.SubItems[2].Text = $"{(100 * p.Percentage).ToString("0.00")}%";
                                    ii.SubItems[3].Text = $"{FormatBytes(dl.CalculatedSpeed)}/s";
                                }
                            }));
                            break;
                        }
                }
            }));
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            var items = listView2.SelectedItems;
            if(items.Count > 0)
            {

            } else
            {
                e.Cancel = true;
            }
        }
    }

    internal class DownloadView
    {
        private Download Me { get; set; }
        private Task DownloadResult { get; set; }
        private StringBuilder Log { get; set; } = new StringBuilder();

        public DownloadView(string url, EventHandler<VoidProgress> progress)
        {
            Me = new Download();
            Me.ProgressChanged += progress;
            Me.ProgressChanged += (s, e) =>
            {
                switch (e)
                {
                    case LogVoidProgress l:
                        {
                            Log.AppendLine(l.Log);
                            break;
                        }
                }
            };
            DownloadResult = Me.DownloadFileAsync(url);
        }

        public Guid Id => Me.Id;
    }
}
