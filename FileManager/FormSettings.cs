using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileManager
{
    public partial class FormSettings : Form
    {
        public FormSettings()
        {
            InitializeComponent();
        }

        MailAdres currentAdres = null;

        private void Adresler()
        {
            try
            {
                listView1.BeginUpdate();
                listView1.Items.Clear();

                XPCollection<MailAdres> allAdress = new XPCollection<MailAdres>(CriteriaOperator.Parse(""), null);
                for (int i = 0; i < allAdress.Count; i++)
                {
                    ListViewItem item = new ListViewItem();
                    item.Tag = allAdress[i];
                    item.Text = allAdress[i].Isim;
                    item.Checked = allAdress[i].Gonderilsin;
                    item.SubItems.Add(allAdress[i].Adres);
                    item.SubItems.Add(allAdress[i].Gonderilsin ? "√" : "");
                    listView1.Items.Add(item);
                }

                listView1.EndUpdate();
                Application.DoEvents();
            }
            catch (Exception exc)
            {
                Utility.Hata(exc);
            }
        }

        private void Temizle()
        {
            currentAdres = null;
            txtAd.Text = "";
            txtAdres.Text = "";
            chkstatu.Checked = false;
            txtAd.Focus();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtMailHost.Text) ||
                string.IsNullOrEmpty(txtHost.Text) ||
                string.IsNullOrEmpty(txtMailPass.Text) ||
                string.IsNullOrEmpty(txtMailUser.Text) ||
                string.IsNullOrEmpty(txtftpip.Text) ||
                string.IsNullOrEmpty(txtftpuser.Text) ||
                string.IsNullOrEmpty(txtftppass.Text))
            {
                Utility.Hata("Alanlar boş bırakılamaz!");
                return;
            }

            if (!Directory.Exists(txtHedefKlasor.Text))
            {
                Utility.Hata("Hedef klasör hatalı!");
                return;
            }

            if (!Directory.Exists(txtKaynakKlasor.Text))
            {
                Utility.Hata("Kaynak klasör hatalı!");
                return;
            }

            if (!Regex.IsMatch(txtMailUser.Text, @"^([\w\.\-]+)@((?!\.|\-)[\w\-]+)((\.(\w){2,3})+)$"))
            {
                Utility.Hata("Mail adresi formatı hatalı!");
                return;
            }

            TraceLevel trace = TraceLevel.Off;
            Enum.TryParse<TraceLevel>(cmdtracelavel.Text, out trace);
            AppSettingHelper.Default.tracelavel = trace;

            AppSettingHelper.Default.mailhost = txtMailHost.Text;
            AppSettingHelper.Default.mailpass = txtMailPass.Text;
            AppSettingHelper.Default.mailuser = txtMailUser.Text;
            AppSettingHelper.Default.orahost = txtHost.Text;
            AppSettingHelper.Default.oraservis = txtServis.Text;
            AppSettingHelper.Default.argeklasor = txtKaynakKlasor.Text;
            AppSettingHelper.Default.hedefklasor = txtHedefKlasor.Text;
            AppSettingHelper.Default.ftphost = txtftpip.Text;
            AppSettingHelper.Default.ftpuser = txtftpuser.Text;
            AppSettingHelper.Default.ftppass = txtftppass.Text;
            AppSettingHelper.Default.timeout = (int)nmTimeout.Value;
            AppSettingHelper.Default.userid = (int)nmuserid.Value;
            AppSettingHelper.Default.oraport = (int)nmoraport.Value;
            AppSettingHelper.Default.mailport = (int)nmmailport.Value;
            AppSettingHelper.Default.coid = (int)nmcoid.Value;
            AppSettingHelper.Default.branchid = (int)nmbranchid.Value;
            AppSettingHelper.Default.ftpport = (int)nmftpport.Value;
            AppSettingHelper.Default.test = chkTest.Checked;
            AppSettingHelper.Default.SendMail = chkstatu.Checked;

            //Properties.Settings.Default.Save();
            //Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            //string path = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            Utility.Bilgi("Ayarlar config.dat dosyasına kaydedildi");


            this.DialogResult = System.Windows.Forms.DialogResult.OK;
        }

        private void FormSettings_Load(object sender, EventArgs e)
        {
            string[] names = Enum.GetNames(typeof(TraceLevel));
            cmdtracelavel.Items.AddRange(names);
            cmdtracelavel.Text = AppSettingHelper.Default.tracelavel.ToString();

            txtMailHost.Text = AppSettingHelper.Default.mailhost;
            txtMailPass.Text = AppSettingHelper.Default.mailpass;
            txtMailUser.Text = AppSettingHelper.Default.mailuser;
            txtHost.Text = AppSettingHelper.Default.orahost;
            txtServis.Text = AppSettingHelper.Default.oraservis;
            txtKaynakKlasor.Text = AppSettingHelper.Default.argeklasor;
            txtHedefKlasor.Text = AppSettingHelper.Default.hedefklasor;
            txtftpip.Text = AppSettingHelper.Default.ftphost;
            txtftpuser.Text = AppSettingHelper.Default.ftpuser;
            txtftppass.Text = AppSettingHelper.Default.ftppass;
            nmTimeout.Value = AppSettingHelper.Default.timeout;
            nmuserid.Value = AppSettingHelper.Default.userid;
            nmoraport.Value = AppSettingHelper.Default.oraport;
            nmmailport.Value = AppSettingHelper.Default.mailport;
            nmcoid.Value = AppSettingHelper.Default.coid;
            nmbranchid.Value = AppSettingHelper.Default.branchid;
            nmftpport.Value = AppSettingHelper.Default.ftpport;
            chkTest.Checked = AppSettingHelper.Default.test;
            chkstatu.Checked = AppSettingHelper.Default.SendMail;


            Adresler();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedIndices.Count > 0)
            {
                currentAdres = listView1.Items[listView1.SelectedIndices[0]].Tag as MailAdres;
                if (currentAdres != null)
                {
                    txtAd.Text = currentAdres.Isim;
                    txtAdres.Text = currentAdres.Adres;
                    chkstatu.Checked = currentAdres.Gonderilsin;
                }
            }
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            Temizle();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (currentAdres == null)
            {
                currentAdres = new MailAdres();
            }

            if (string.IsNullOrEmpty(txtAd.Text) || string.IsNullOrEmpty(txtAdres.Text))
            {
                Utility.Hata("Alanlar boş bırakılamaz!");
                return;
            }

            if (!Regex.IsMatch(txtAdres.Text, @"^([\w\.\-]+)@((?!\.|\-)[\w\-]+)((\.(\w){2,3})+)$"))
            {
                Utility.Hata("Mail adresi formatı hatalı!");
                return;
            }

            TraceLevel trace = TraceLevel.Off;
            Enum.TryParse<TraceLevel>(cmdtracelavel.Text, out trace);
            AppSettingHelper.Default.tracelavel = trace;
            currentAdres.Adres = txtAdres.Text;
            currentAdres.Isim = txtAd.Text;
            currentAdres.Gonderilsin = chkstatu.Checked;
            currentAdres.Save();
            Temizle();
            Adresler();
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            if (currentAdres == null)
            {
                Utility.Hata("Listeden silinecek adresi seçin!");
                return;
            }

            if (!Utility.Sor(currentAdres.Isim + " mail adresi silinecek onaylıyor musunuz?")) return;

            currentAdres.Delete();
            Temizle();
            Adresler();
        }

        private void listView1_ItemChecked(object sender, ItemCheckedEventArgs e)
        {

        }

        private void btnKaynakFolder_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fl = new FolderBrowserDialog();
            fl.Description = "İzlenecek klasörü seçin.";
            fl.ShowNewFolderButton = false;
            if (fl.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtKaynakKlasor.Text = fl.SelectedPath;
            }
        }

        private void btnHedefFold_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fl = new FolderBrowserDialog();
            fl.Description = "Dosyaların kopyalanacağı klasörü seçin.";
            fl.ShowNewFolderButton = false;
            if (fl.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtHedefKlasor.Text = fl.SelectedPath;
            }
        }
    }
}
