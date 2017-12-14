using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Ar00Lib;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Generations_Archive_Editor
{
    public partial class MainForm : Form
    {
        CommonFileDialogFilter arFiles = new CommonFileDialogFilter("ar Files", "*.ar.*;*.pfd");
        CommonFileDialogFilter allFiles = new CommonFileDialogFilter("All Files", "*.*");

        public MainForm()
        {
            InitializeComponent();
        }

        private string filename;
        private Ar00File file;

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length == 1)
                file = new Ar00File();
            else
                LoadFile(args[1]);
        }

        private void LoadFile(string filename, bool clear = true)
        {
            try
            {
                file = new Ar00File(filename);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message);
                return;
            }
            this.filename = filename;
            if(clear){
                listView1.Items.Clear();
                imageList1.Images.Clear();
            }
            if(file.Files.Count == 0) return;
            listView1.BeginUpdate();
            foreach (Ar00File.File item in file.Files)
            {
                imageList1.Images.Add(GetIcon(item.Name));
                listView1.Items.Add(item.Name, imageList1.Images.Count - 1);
            }
            listView1.EndUpdate();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog a = new CommonOpenFileDialog
            {
                Filters = {arFiles, allFiles},
                EnsureFileExists = true
            };
            if (a.ShowDialog() == CommonFileDialogResult.Ok)
                LoadFile(a.FileName);
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(filename))
                saveAsToolStripMenuItem_Click(sender, e);
            else
            {
                using (PaddingDialog dlg = new PaddingDialog(file.Padding))
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK)
                        return;
                    file.Padding = (int)dlg.numericUpDown1.Value;
                }
                file.Save(filename);
                if (MessageBox.Show(this, "Generate ARL file?", "Generations Archive Editor", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    new Thread(() => {
                        Ar00File.GenerateArlFile(filename);
                    }).Start();
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
                using (CommonSaveFileDialog a = new CommonSaveFileDialog { Filters = {arFiles, allFiles }})
                {
                    if (a.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        using (PaddingDialog dlg = new PaddingDialog(file.Padding))
                        {
                            if (dlg.ShowDialog(this) != DialogResult.OK)
                                return;
                            file.Padding = (int)dlg.numericUpDown1.Value;
                        }
                        file.Save(a.FileName);
                        filename = a.FileName;
                        if(MessageBox.Show(this,
                                           "Generate ARL file?",
                                           "Generations Archive Editor",
                                           MessageBoxButtons.YesNo) ==
                           DialogResult.Yes){
                            string filename = a.FileName;
                            new Thread(()=>{
                                Ar00File.GenerateArlFile(filename);
                            }).Start();
                        }
                    }
                }
        }

        private void extractAllToolStripMenuItem_Click(object sender, EventArgs e){
            CommonOpenFileDialog a = new CommonOpenFileDialog{IsFolderPicker = true, EnsurePathExists = true};
            if (a.ShowDialog() == CommonFileDialogResult.Ok)
                new Thread(() => {
                    foreach (Ar00File.File item in file.Files)
                        File.WriteAllBytes(Path.Combine(a.FileName, item.Name), item.Data);
                }).Start();
        }

        ListViewItem selectedItem;
        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                selectedItem = listView1.GetItemAt(e.X, e.Y);
                if (selectedItem != null)
                    contextMenuStrip1.Show(listView1, e.Location);
            }
        }

        private void addFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommonOpenFileDialog a = new CommonOpenFileDialog
            {
                Multiselect = true
            };
            if (a.ShowDialog() == CommonFileDialogResult.Ok)
            {
                int i = file.Files.Count;
                foreach (string item in a.FileNames)
                {
                    file.Files.Add(new Ar00File.File(item));
                    imageList1.Images.Add(GetIcon(file.Files[i].Name));
                    listView1.Items.Add(file.Files[i].Name, i);
                    i++;
                }
            }
        }

        private void extractToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedItem == null) return;
            CommonSaveFileDialog a = new CommonSaveFileDialog
            {
                Filters = {allFiles},
                DefaultFileName = selectedItem.Text
            };
            if (a.ShowDialog() == CommonFileDialogResult.Ok)
                new Thread(()=>{
                    File.WriteAllBytes(a.FileName, file.Files[listView1.Items.IndexOf(selectedItem)].Data);
                }).Start();
        }

        private void replaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedItem == null) return;
            int i = listView1.Items.IndexOf(selectedItem);
            string fn = file.Files[i].Name;
            CommonOpenFileDialog a = new CommonOpenFileDialog{
                Filters = {allFiles},
                DefaultFileName = fn
            };
            if (a.ShowDialog() == CommonFileDialogResult.Ok)
            {
                file.Files[i] = new Ar00File.File(a.FileName);
                file.Files[i].Name = fn;
            }
        }

        private void insertToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedItem == null) return;
            CommonOpenFileDialog a = new CommonOpenFileDialog{
                Filters = {allFiles},
                Multiselect = true
            };
            if (a.ShowDialog() == CommonFileDialogResult.Ok)
            {
                int i = listView1.Items.IndexOf(selectedItem);
                foreach (string item in a.FileNames)
                {
                    file.Files.Insert(i, new Ar00File.File(item));
                    i++;
                }
                listView1.Items.Clear();
                imageList1.Images.Clear();
                listView1.BeginUpdate();
                for (int j = 0; j < file.Files.Count; j++)
                {
                    imageList1.Images.Add(GetIcon(file.Files[j].Name));
                    listView1.Items.Add(file.Files[j].Name, j);
                }
                listView1.EndUpdate();
            }
        }

        private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (selectedItem == null) return;
            int i = listView1.Items.IndexOf(selectedItem);
            file.Files.RemoveAt(i);
            listView1.Items.RemoveAt(i);
            imageList1.Images.RemoveAt(i);
        }

        private string oldName;
        private void listView1_BeforeLabelEdit(object sender, LabelEditEventArgs e)
        {
            oldName = e.Label;
        }

        private void listView1_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            if (oldName == e.Label) return;
            foreach (Ar00File.File item in file.Files)
            {
                if (item.Name.Equals(e.Label, StringComparison.OrdinalIgnoreCase))
                {
                    e.CancelEdit = true;
                    MessageBox.Show("This name is being used by another file.");
                    return;
                }
            }
            if (e.Label.IndexOfAny(Path.GetInvalidFileNameChars()) > -1)
            {
                e.CancelEdit = true;
                MessageBox.Show("This name contains invalid characters.");
                return;
            }
            file.Files[e.Item].Name = e.Label;
            imageList1.Images[e.Item] = GetIcon(e.Label).ToBitmap();
        }

        private void listView1_ItemActivate(object sender, EventArgs e)
        {
            string fp = Path.Combine(Path.GetTempPath(), file.Files[listView1.SelectedIndices[0]].Name);
            File.WriteAllBytes(fp, file.Files[listView1.SelectedIndices[0]].Data);
            Process.Start(fp);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            file.Files = new List<Ar00File.File>();
            listView1.Items.Clear();
            imageList1.Images.Clear();
        }

        private void listView1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.All;
        }

        private void listView1_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] dropfiles = (string[])e.Data.GetData(DataFormats.FileDrop, true);
                int i = file.Files.Count;
                foreach (string item in dropfiles)
                {
                    bool found = false;
                    for (int j = 0; j < file.Files.Count; j++)
                        if (file.Files[j].Name.Equals(Path.GetFileName(item), StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            file.Files[j] = new Ar00File.File(item);
                        }
                    if (found) continue;
                    file.Files.Add(new Ar00File.File(item));
                    imageList1.Images.Add(GetIcon(file.Files[i].Name));
                    listView1.Items.Add(file.Files[i].Name, i);
                    i++;
                }
            }
        }

        private void listView1_ItemDrag(object sender, ItemDragEventArgs e)
        {
            string fn = Path.Combine(Path.GetTempPath(), file.Files[listView1.SelectedIndices[0]].Name);
            File.WriteAllBytes(fn, file.Files[listView1.SelectedIndices[0]].Data);
            DoDragDrop(new DataObject(DataFormats.FileDrop, new[] { fn }), DragDropEffects.All);
        }

        private readonly Dictionary<string, Icon> iconstore = new Dictionary<string, Icon>();

        [DllImport("shell32.dll")]
        private static extern IntPtr ExtractIconA(int hInst, string lpszExeFileName, int nIconIndex);

        private Icon GetIcon(string file)
        {
            string iconpath = "C:\\Windows\\system32\\shell32.dll,0";
            string ext = file.IndexOf('.') > -1 ? file.Substring(file.LastIndexOf('.')) : file;
            if (iconstore.ContainsKey(ext.ToLowerInvariant()))
                return iconstore[ext.ToLowerInvariant()];
            RegistryKey k = Registry.ClassesRoot.OpenSubKey(ext);
            if (k == null)
                k = Registry.ClassesRoot.OpenSubKey("*");
            k = Registry.ClassesRoot.OpenSubKey((string)k.GetValue("", "*"));
            if (k != null)
            {
                k = k.OpenSubKey("DefaultIcon");
                if (k != null)
                    iconpath = (string)k.GetValue("", "C:\\Windows\\system32\\shell32.dll,0");
            }
            int iconind = 0;
            if (iconpath.LastIndexOf(',') > iconpath.LastIndexOf('.'))
            {
                iconind = int.Parse(iconpath.Substring(iconpath.LastIndexOf(',') + 1));
                iconpath = iconpath.Remove(iconpath.LastIndexOf(','));
            }
            try
            {
                return iconstore[ext.ToLowerInvariant()] = Icon.FromHandle(ExtractIconA(0, iconpath, iconind));
            }
            catch (Exception)
            {
                return iconstore[ext.ToLowerInvariant()] = Icon.FromHandle(ExtractIconA(0, "C:\\Windows\\system32\\shell32.dll", 0));
            }
        }

        private void quitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
