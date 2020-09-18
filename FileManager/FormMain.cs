using DevExpress.Data.Filtering;
using DevExpress.Xpo;
using HidromasOzel;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.OracleClient;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileManager
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        private static object lockObject = new object();

        private void Dosyalar()
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                listView.BeginUpdate();
                listView.Items.Clear();

                var topFiles = (from q in new XPQuery<PdfFileInfo>(XpoDefault.Session)
                                orderby q.Oid descending
                                select new { q.Oid, q.Name, q.Length, q.UploadMsg, q.MailMsg, q.IsDelete, q.CreationTime, q.Aktarim }).Take(100).ToList();

                if (topFiles != null && topFiles.Count > 0)
                {
                    foreach (var f in topFiles)
                    {
                        ListViewItem item = new ListViewItem();
                        item.ImageIndex = Convert.ToInt32(f.Aktarim);
                        item.Text = f.Oid.ToString();
                        item.SubItems.Add(f.Name);
                        item.SubItems.Add(f.Length.ToString());
                        item.SubItems.Add(f.IsDelete ? "√" : "-");
                        item.SubItems.Add(f.CreationTime.ToString());
                        item.SubItems.Add(f.UploadMsg);
                        item.SubItems.Add(f.MailMsg);
                        listView.Items.Add(item);
                    }
                    Application.DoEvents();
                }
            }
            catch (Exception exc)
            {
                //Utility.Hata(exc);
                Logger.E(exc);
            }
            finally
            {
                listView.EndUpdate();
                Cursor.Current = Cursors.Default;
            }
        }

        private void timerStartup_Tick(object sender, EventArgs e)
        {
            Logger.I("Uygulama başladı");
            StaticsVariable.APPVISIBLE = false;
            //this.Hide();
            timerJop.Interval = AppSettingHelper.Default.timeout * 60000;
            if (Directory.Exists(AppSettingHelper.Default.argeklasor))
                fileSystemWatcher.Path = AppSettingHelper.Default.argeklasor;
            else
                Logger.E(string.Format("{0} klasör yolu hatalı", AppSettingHelper.Default.argeklasor));
            this.notifyIconApp.Visible = true;
            timerStartup.Enabled = false;
            timerJop_Tick(timerJop, EventArgs.Empty);
            timerJop.Enabled = true;
        }

        private void fileSystemWatcher_Created(object sender, System.IO.FileSystemEventArgs e)
        {
            if (e.ChangeType == System.IO.WatcherChangeTypes.Created)
            {
                new Thread(new ParameterizedThreadStart(Kaydet)).Start(e.FullPath);
            }
        }

        private void Kaydet(object paramobj)
        {
            try
            {
                Thread.Sleep(1000);
                Thread.Sleep(1000);
                FileInfo inf = new FileInfo(paramobj.ToString());
                lock (lockObject)
                {
                    using (UnitOfWork wrk = new UnitOfWork())
                    {
                        PdfFileInfo pdf = new PdfFileInfo(wrk);
                        pdf.Name = inf.Name;
                        pdf.FullName = inf.FullName;
                        pdf.Extension = inf.Extension;
                        pdf.CreationTime = inf.CreationTime;
                        try
                        {
                            pdf.Length = inf.Length;
                        }
                        catch
                        {
                        }
                        //pdf.FileType = PDFExpression.DosyaTuru(Path.GetFileNameWithoutExtension(inf.FullName));
                        pdf.FileType = PdfFileType.Bilinmiyor;
                        pdf.ChangeType = WatcherChangeTypes.Created;
                        pdf.Save();
                        Logger.I(pdf.ToString());
                        wrk.CommitChanges();
                    }
                }
                if (!StaticsVariable.APPVISIBLE)
                    this.notifyIconApp.ShowBalloonTip(1000, "UyumSoft", "Yeni dosya algılandı." + inf.Name, ToolTipIcon.Info);
                else
                    Logger.I("Yeni dosya algılandı." + inf.Name);
            }
            catch (Exception exc)
            {
                Logger.E(exc.Message);
                Logger.E(exc.StackTrace);
            }
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            System.Diagnostics.Trace.Listeners.Add(new TextTraceListener(richTrace));
        }

        private void fileSystemWatcher_Changed(object sender, System.IO.FileSystemEventArgs e)
        {

        }

        private void notifyIconApp_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            StaticsVariable.APPVISIBLE = true;
            this.Size = new Size(800, 600);
            this.Show();
            this.notifyIconApp.Visible = false;
        }

        private void timerJop_Tick(object sender, EventArgs e)
        {
            timerJop.Enabled = false;

            Dosyalar();

            using (UnitOfWork wrk = new UnitOfWork())
            {
                bool mailsend = AppSettingHelper.Default.SendMail;
                XPCollection<PdfFileInfo> allfiles = new XPCollection<PdfFileInfo>(wrk, CriteriaOperator.Parse("Aktarim=?", 0), new SortProperty[] { new SortProperty() { Property = "OID", PropertyName = "Oid", Direction = DevExpress.Xpo.DB.SortingDirection.Descending } });
                allfiles.TopReturnedObjects = 100;
                if (allfiles.Count > 0)
                {
                    FTPFactory ftp = FTPFactory.NewInstince();
                    Logger.I(allfiles.Count + " adet dosya aktarılacak.");
                    using (OraHelper ora = new OraHelper())
                    {
                        foreach (PdfFileInfo f in allfiles)
                        {
                            object objIds = null;
                            if (f.FileType == PdfFileType.Bilinmiyor)
                            {
                                FileParser ozel = new FileParser(Path.GetFileNameWithoutExtension(f.FullName), AppSettingHelper.GetConnectionString());
                                f.FileType = ozel.FileType;
                                f.RelationId = ozel.RelationId;
                                f.RelationObject = ozel.RelationObject;
                            }

                            if (f.FileType == PdfFileType.Bilinmiyor || f.FileType == PdfFileType.Diger)
                            {
                                Logger.I("Dosya hatalı! Eksik parametre:" + f.Name);
                                f.UploadMsg = "Dosya türü bilinmiyor:" + f.Name;
                                f.Aktarim = AktarimDurumu.RelationIdYok;
                                f.Save();
                                continue;
                            }

                            #region İptal
                            /*else if (f.FileType == PdfFileType.UrunAgacKod)
                            {
                                f.UrunAgacRevizyonKodu = PDFExpression.RevizyonNumarasi(f.Name);

                                objIds = ora.ExecuteScalar(string.Format("SELECT U.BOM_M_ID FROM UYUMSOFT.INVD_BRANCH_ITEM B INNER JOIN UYUMSOFT.INVD_ITEM M ON B.ITEM_ID = M.ITEM_ID INNER JOIN UYUMSOFT.PRDD_BOM_M U ON B.ITEM_ID = U.ITEM_ID WHERE B.BRANCH_ID = '{0}' AND B.CO_ID = '{1}' AND M.ITEM_CODE = '{2}' AND replace(replace(U.ALTERNATIVE_NO, '-', ''),'_','') = '{3}'", Properties.Settings.Default.branchid, Properties.Settings.Default.coid, f.StokKodu, f.UrunAgacRevizyonKodu), null);
                                if (objIds != null)
                                {
                                    relationId = Convert.ToInt32(objIds);
                                }
                                else
                                {
                                    Utility.WriteTrace("Ürün ağaç kodu bulunamadı:" + f.StokKodu + "," + f.UrunAgacRevizyonKodu);
                                    f.UploadMsg = "Ürün ağaç kodu bulunamadı:" + f.StokKodu + "," + f.UrunAgacRevizyonKodu;
                                    f.Aktarim = AktarimDurumu.RelationIdYok;
                                    f.Save();
                                    continue;
                                }
                            }
                            else if (f.FileType == PdfFileType.RotaKod)
                            {
                                f.UrunAgacRevizyonKodu = PDFExpression.RevizyonNumarasi(f.Name);
                                f.OperasyonNo = PDFExpression.OperasyonSiraNo(f.Name);
                                f.OperasyonKodu = PDFExpression.OperasyonKod(f.Name);

                                if (f.OperasyonNo > 0)
                                {
                                    objIds = ora.ExecuteScalar(string.Format("SELECT D.PRODUCT_ROUTE_D_ID FROM UYUMSOFT.INVD_BRANCH_ITEM B INNER JOIN UYUMSOFT.INVD_ITEM M ON B.ITEM_ID = M.ITEM_ID INNER JOIN UYUMSOFT.PRDD_PRODUCT_ROUTE_M R ON R.ITEM_ID = M.ITEM_ID INNER JOIN UYUMSOFT.PRDD_PRODUCT_ROUTE_D D ON R.PRODUCT_ROUTE_M_ID = D.PRODUCT_ROUTE_M_ID INNER JOIN UYUMSOFT.PRDD_OPERATION O ON D.OPERATION_ID = O.OPERATION_ID WHERE B.BRANCH_ID = '{0}' AND B.CO_ID = '{1}' AND M.ITEM_CODE = '{2}' AND replace(replace(R.ALTERNATIVE_NO, '-', ''),'_','') = '{3}' AND O.OPERATION_CODE = '{4}' AND D.OPERATION_NO = {5} AND ROWNUM = 1", Properties.Settings.Default.branchid, Properties.Settings.Default.coid, f.StokKodu, f.UrunAgacRevizyonKodu, f.OperasyonKodu, f.OperasyonNo), null);
                                }
                                else
                                {
                                    f.UrunAgacRevizyonKodu = PDFExpression.RotaRevizyonNumarasi(f.Name);
                                    objIds = ora.ExecuteScalar(string.Format("SELECT R.PRODUCT_ROUTE_M_ID FROM UYUMSOFT.INVD_BRANCH_ITEM B INNER JOIN UYUMSOFT.INVD_ITEM M ON B.ITEM_ID = M.ITEM_ID INNER JOIN UYUMSOFT.PRDD_PRODUCT_ROUTE_M R ON R.ITEM_ID = M.ITEM_ID  WHERE B.BRANCH_ID = '{0}' AND B.CO_ID = '{1}' AND M.ITEM_CODE = '{2}' AND replace(replace(R.ALTERNATIVE_NO, '-', ''),'_','') = '{3}' AND ROWNUM = 1", Properties.Settings.Default.branchid, Properties.Settings.Default.coid, f.StokKodu, f.UrunAgacRevizyonKodu), null);
                                }
                                if (objIds != null)
                                {
                                    relationId = Convert.ToInt32(objIds);
                                }
                                else
                                {
                                    Utility.WriteTrace("Ürün rota kodu bulunamadı:" + f.StokKodu + "," + f.UrunAgacRevizyonKodu);
                                    f.UploadMsg = "Ürün rota kodu bulunamadı:" + f.StokKodu + "," + f.UrunAgacRevizyonKodu;
                                    f.Aktarim = AktarimDurumu.RelationIdYok;
                                    f.Save();
                                    continue;
                                }
                            }
                            else if (f.FileType == PdfFileType.IstasyonKod)
                            {
                                objIds = ora.ExecuteScalar(string.Format("SELECT WSTATION_ID FROM UYUMSOFT.PRDD_WSTATION WHERE BRANCH_ID = '{0}' AND CO_ID = '{1}' AND WSTATION_CODE = '{2}'", Properties.Settings.Default.branchid, Properties.Settings.Default.coid, f.StokKodu), null);
                                if (objIds != null)
                                {
                                    relationId = Convert.ToInt32(objIds);
                                }
                                else
                                {
                                    Utility.WriteTrace("Ürün ağaç kodu bulunamadı:" + f.StokKodu + "," + f.UrunAgacRevizyonKodu);
                                    f.UploadMsg = "Ürün ağaç kodu bulunamadı:" + f.StokKodu + "," + f.UrunAgacRevizyonKodu;
                                    f.Aktarim = AktarimDurumu.RelationIdYok;
                                    f.Save();
                                    continue;
                                }
                            }*/

                            #endregion

                            if (f.RelationId > 0)
                            {
                                try
                                {
                                    Logger.I("Dosya kopyalanıyor:" + f.FullName + " to " + f.Name);

                                    //SaveACopyfileToServer(f.FullName, string.Concat(Properties.Settings.Default.hedefklasor, "\\", f.Name));
                                    //SaveACopyfileToServer(f.FullName, f.Name);
                                    //ftp.uploadwithupdate(f.FullName);
                                    //long size = ftp.getFileSize(f.Name);
                                    //if (size > 0)
                                    //    ftp.deleteRemoteFile(f.Name);

                                    var fx = new FileInfo(f.FullName);
                                    fx.Attributes = FileAttributes.Normal;
                                    fx.IsReadOnly = false;

                                    if (!ftp.upload(f.FullName))
                                    {
                                        Logger.I("Dosya klasore kopyalanamadı:" + f.FullName);
                                        f.UploadMsg = "Dosya klasore kopyalanamadı";
                                        f.Aktarim = AktarimDurumu.Kopyalanamadi;
                                        f.Save();
                                        continue;
                                    }

                                    //try
                                    //{
                                    //    File.Copy(f.FullName, string.Concat(Properties.Settings.Default.hedefklasor, "\\", f.Name), true);
                                    //}
                                    //catch (IOException _ioexc)
                                    //{
                                    //    Logger.I("Dosya klasore kopyalanamadı:" + _ioexc.Message);
                                    //    f.UploadMsg = "Dosya klasore kopyalanamadı:" + _ioexc.Message;
                                    //    f.Aktarim = AktarimDurumu.Kopyalanamadi;
                                    //    f.Save();
                                    //    continue;
                                    //}

                                }
                                catch (IOException io)
                                {
                                    Logger.I("Dosya klasore kopyalanamadı:" + io.Message + ",detay" + io.StackTrace);
                                    f.UploadMsg = "Dosya klasore kopyalanamadı:" + io.Message;
                                    f.Aktarim = AktarimDurumu.Kopyalanamadi;
                                    f.Save();
                                    continue;
                                }
                                catch (Exception exc)
                                {
                                    Logger.I("Dosya klasore kopyalanamadı:" + exc.Message + ",detay" + exc.StackTrace);
                                    f.UploadMsg = "Dosya klasore kopyalanamadı:" + exc.Message;
                                    f.Aktarim = AktarimDurumu.Kopyalanamadi;
                                    f.Save();
                                    continue;
                                }

                                try
                                {
                                    Logger.I("Dosya siliniyor:" + f.FullName);

                                    File.Delete(f.FullName);
                                    f.IsDelete = true;
                                }
                                catch (IOException io)
                                {
                                    f.IsDelete = false;
                                    Logger.E("Dosya silinemedi:" + io.Message);
                                }
                                catch (Exception exc)
                                {
                                    f.IsDelete = false;
                                    Logger.E("Dosya silinemedi:" + exc.Message);
                                }
                            }

                            //if (File.Exists(string.Concat(Properties.Settings.Default.hedefklasor, "\\", f.Name)))
                            if (ftp.getFileSize(f.Name) > 0)
                            {
                                try
                                {
                                    OracleParameter[] delParameters = new OracleParameter[2];
                                    delParameters[0] = new OracleParameter(":RELATION_OBJECT", f.RelationObject);
                                    delParameters[1] = new OracleParameter(":RELATION_ID", f.RelationId);

                                    string delExtra = "";
                                    if (f.Name.IndexOf("UA") != -1)
                                        delExtra = " AND SH0RT_FILE_NAME LIKE '%UA%' ";
                                    if (f.Name.IndexOf("OG") != -1)
                                        delExtra = " AND SH0RT_FILE_NAME LIKE '%OG%' ";
                                    if (f.Name.IndexOf("UGTL") != -1)
                                        delExtra = string.Format(" AND SH0RT_FILE_NAME = '{0}' ", f.Name);

                                    ora.Exec("DELETE FROM GNLD_UPLOAD_FILE WHERE RELATION_OBJECT = :RELATION_OBJECT AND RELATION_ID = :RELATION_ID " + delExtra, delParameters);
                                }
                                catch (Exception delexception)
                                {
                                    Logger.E("Öncei dökümanlar silinemedi! Hata:" + delexception.Message);
                                }

                            }

                            int uploadFileId = 1;
                            objIds = ora.ExecuteScalar("SELECT MAX(UPLOAD_FILE_ID) AS UPLOAD_FILE_ID FROM GNLD_UPLOAD_FILE", null);

                            if (objIds != null && object.ReferenceEquals(objIds, DBNull.Value) == false)
                            {
                                uploadFileId = Convert.ToInt32(objIds) + 1;
                            }
                            string commandText = "INSERT INTO GNLD_UPLOAD_FILE (UPLOAD_FILE_ID, RELATION_OBJECT, RELATION_ID, SH0RT_FILE_NAME, LONG_FILE_NAME, DOCUMENT_TYPE, DESCRIPTION, CREATE_DATE, CREATE_USER_ID) VALUES (:UPLOAD_FILE_ID, :RELATION_OBJECT, :RELATION_ID, :SH0RT_FILE_NAME, :LONG_FILE_NAME, :DOCUMENT_TYPE, :DESCRIPTION, :CREATE_DATE, :CREATE_USER_ID)";
                            OracleParameter[] oraParameters = new OracleParameter[9];
                            oraParameters[0] = new OracleParameter(":UPLOAD_FILE_ID", uploadFileId);
                            oraParameters[1] = new OracleParameter(":RELATION_OBJECT", f.RelationObject);
                            oraParameters[2] = new OracleParameter(":RELATION_ID", f.RelationId);
                            oraParameters[3] = new OracleParameter(":SH0RT_FILE_NAME", f.Name);
                            oraParameters[4] = new OracleParameter(":LONG_FILE_NAME", f.Name);
                            oraParameters[5] = new OracleParameter(":DOCUMENT_TYPE", StaticsVariable.DOCUMENT_TYPE);
                            oraParameters[6] = new OracleParameter(":DESCRIPTION", StaticsVariable.DESCRIPTION);
                            oraParameters[7] = new OracleParameter(":CREATE_DATE", DateTime.Now);
                            oraParameters[8] = new OracleParameter(":CREATE_USER_ID", AppSettingHelper.Default.userid);
                            if (!ora.Exec(commandText, oraParameters))
                            {
                                Logger.W("Veritabanına yazılamadı!" + f.Name);
                                f.UploadMsg = "Veritabanına yazılamadı!";
                                f.Aktarim = AktarimDurumu.Kaydedilemedi;
                                f.Save();
                            }
                            else
                            {
                                OracleParameter[] selParameters = new OracleParameter[2];
                                selParameters[0] = new OracleParameter(":UPLOAD_FILE_ID", uploadFileId);
                                selParameters[1] = new OracleParameter(":SH0RT_FILE_NAME", f.Name);
                                objIds = ora.ExecuteScalar("SELECT UPLOAD_FILE_ID FROM GNLD_UPLOAD_FILE WHERE UPLOAD_FILE_ID = :UPLOAD_FILE_ID OR SH0RT_FILE_NAME = :SH0RT_FILE_NAME", selParameters);

                                if (objIds != null && object.ReferenceEquals(objIds, DBNull.Value) == false)
                                {
                                    uploadFileId = Convert.ToInt32(objIds);
                                }

                                try
                                {
                                    if (mailsend)
                                    {
                                        MailHelper.MailSend(f.Name);
                                        f.IsMailSend = true;
                                    }
                                }
                                catch (Exception exc)
                                {
                                    f.IsMailSend = false;
                                    f.MailMsg = exc.Message;
                                }
                                f.IsUploaded = true;
                                f.UploadFileId = uploadFileId;
                                f.UploadMsg = string.Format("{0} aktarıldı.", uploadFileId);
                                f.Aktarim = AktarimDurumu.Aktarildi;
                                f.Save();
                            }
                        }

                    }
                    ftp.close();
                    ftp = null;
                    wrk.CommitChanges();
                    Dosyalar();
                }
                else
                {
                    Logger.I("Aktarılacak dosya yok.");
                    //MailHelper.MailSend("TTTTTTTT");
                }
            }

            timerJop.Enabled = true;
        }

        private void timerJop_TickXX(object sender, EventArgs e)
        {
            timerJop.Enabled = false;

            Dosyalar();

            using (UnitOfWork wrk = new UnitOfWork())
            {
                XPCollection<PdfFileInfo> allfiles = new XPCollection<PdfFileInfo>(wrk, CriteriaOperator.Parse("Aktarim=?", 0), new SortProperty[] { new SortProperty() { Property = "OID", PropertyName = "Oid", Direction = DevExpress.Xpo.DB.SortingDirection.Descending } });
                allfiles.TopReturnedObjects = 100;
                if (allfiles.Count > 0)
                {
                    Logger.I(allfiles.Count + " adet dosya aktarılacak.");
                    //using (OraHelper ora = new OraHelper())
                    {
                        foreach (PdfFileInfo f in allfiles)
                        {
                            Logger.I("Dosya kopyalanıyor:" + f.FullName + " to " + string.Concat(f.Name));

                            SaveACopyfileToServer(f.FullName, f.Name);


                            if (CheckileFromServer(f.Name))
                            {
                                Logger.I("Dosya aktarıldı " + f.Name);
                            }

                        }

                    }
                    wrk.CommitChanges();
                    Dosyalar();
                }
                else
                {
                    Logger.I("Aktarılacak dosya yok.");
                    //MailHelper.MailSend("TTTTTTTT");
                }
            }

            timerJop.Enabled = true;
        }

        private static void SaveACopyfileToServer(string filePath, string saveName)
        {
            Logger.I(saveName + ",  Aktarılıyor ...");
            FTPFactory ftp = FTPFactory.NewInstince();
            ftp.upload(filePath, true);
            //var directory = Path.GetDirectoryName(savePath).Trim();
            //var username = "hdrms\barset";
            //var password = "!brst123";
            //var filenameToSave = Path.GetFileName(savePath);

            //if (!directory.EndsWith("\\"))
            //    filenameToSave = "\\" + filenameToSave;

            //var command = "NET USE " + directory + " /delete";
            //ExecuteCommand(command, 5000);

            //command = "NET USE " + directory + " /user:" + username + " " + password;
            //ExecuteCommand(command, 5000);

            //command = " copy \"" + filePath + "\"  \"" + directory + filenameToSave + "\" /Z /Y";

            //ExecuteCommand(command, 5000);


            //command = "NET USE " + directory + " /delete";
            //ExecuteCommand(command, 5000);
        }

        private static bool CheckileFromServer(string fileName)
        {
            try
            {
                FTPFactory ftp = FTPFactory.NewInstince();
                if (ftp != null)
                {
                    long size = ftp.getFileSize(fileName);
                    return size > 0;
                }
                //var directory = Path.GetDirectoryName(savePath).Trim();
                //var username = "hdrms\barset";
                //var password = "!brst123";
                //var filenameToSave = Path.GetFileName(savePath);

                //if (!directory.EndsWith("\\"))
                //    filenameToSave = "\\" + filenameToSave;

                //var command = "NET USE " + directory + " /delete";
                //ExecuteCommand(command, 5000);

                //command = "NET USE " + directory + " /user:" + username + " " + password;
                //ExecuteCommand(command, 5000);

                //command = " copy \"" + filePath + "\"  \"" + directory + filenameToSave + "\" /Z /Y";

                //ExecuteCommand(command, 5000);


                //command = "NET USE " + directory + " /delete";
                //ExecuteCommand(command, 5000);
            }
            catch (Exception exc)
            {
                Logger.E("Dosya kontrol hatası:" + exc.Message + ", Detay:" + exc.StackTrace);
            }
            return false;
        }

        public static int ExecuteCommand(string command, int timeout)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", "/C " + command)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                WorkingDirectory = "C:\\",
            };

            var process = Process.Start(processInfo);
            process.WaitForExit(timeout);
            var exitCode = process.ExitCode;
            process.Close();
            return exitCode;
        }

        private void FormMain_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
                this.notifyIconApp.Visible = true;
                this.notifyIconApp.ShowBalloonTip(600, "UyumSoft", "Uygulama çalışıyor.", ToolTipIcon.Info);
            }
            else if (WindowState == FormWindowState.Normal)
            {
                this.Show();
                this.notifyIconApp.Visible = false;
            }
        }

        private void btnAyarlar_Click(object sender, EventArgs e)
        {
            FormSettings settings = new FormSettings();
            if (settings.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                timerJop.Interval = AppSettingHelper.Default.timeout * 60000;
                fileSystemWatcher.Path = AppSettingHelper.Default.argeklasor;
            }
        }

        private void mnuList_Opening(object sender, CancelEventArgs e)
        {
            btnSend.Enabled = btnUpd.Enabled = btnDel.Enabled = listView.SelectedIndices.Count > 0;
        }

        private void btnLogClear_Click(object sender, EventArgs e)
        {
            richTrace.Text = "";
        }

        private void bntOpenTrace_Click(object sender, EventArgs e)
        {
            Process.Start(string.Format("\"{0}\"", Utility.TraceName));
        }

        private void btnDel_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView.SelectedIndices.Count > 0)
                {
                    int oid = Convert.ToInt32(listView.Items[listView.SelectedIndices[0]].Text);
                    if (Utility.Sor(oid + " nolu kayıt silinecek kabul ediyor musunuz?"))
                    {
                        PdfFileInfo file = XpoDefault.Session.GetObjectByKey<PdfFileInfo>(oid);
                        if (file != null) file.Delete();
                        Dosyalar();
                    }
                }
            }
            catch (Exception exc)
            {
                Utility.Hata(exc);
            }
        }

        private void btnUpd_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView.SelectedIndices.Count > 0)
                {
                    int oid = Convert.ToInt32(listView.Items[listView.SelectedIndices[0]].Text);
                    if (Utility.Sor(oid + " nolu kayıt gönderildi olarak güncellenecek, kabul ediyor musunuz?"))
                    {
                        PdfFileInfo file = XpoDefault.Session.GetObjectByKey<PdfFileInfo>(oid);
                        if (file != null)
                        {
                            file.UploadMsg = "";
                            file.Aktarim = AktarimDurumu.Aktarildi;
                            file.Save();
                        }
                        Dosyalar();
                    }
                }
            }
            catch (Exception exc)
            {
                Utility.Hata(exc);
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                if (listView.SelectedIndices.Count > 0)
                {
                    int oid = Convert.ToInt32(listView.Items[listView.SelectedIndices[0]].Text);
                    if (Utility.Sor(oid + " nolu kayıt gönderilmedi olarak güncellenecek, kabul ediyor musunuz?"))
                    {
                        PdfFileInfo file = XpoDefault.Session.GetObjectByKey<PdfFileInfo>(oid);
                        if (file != null)
                        {
                            file.FileType = PdfFileType.Bilinmiyor;
                            file.UploadMsg = "";
                            file.Aktarim = AktarimDurumu.Bekliyor;
                            file.Save();
                        }
                        Dosyalar();
                    }
                }
            }
            catch (Exception exc)
            {
                Utility.Hata(exc);
            }
        }

        private void yenileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Dosyalar();
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            FormKod kd = new FormKod();
            kd.ShowDialog();
        }
    }
}
