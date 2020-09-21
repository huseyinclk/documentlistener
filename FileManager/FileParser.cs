using FileManager;
using System;
using System.Collections.Generic;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HidromasOzel
{
    public class FileParser
    {
        public FileParser(string dosyaAd, string constr)
        {
            this.DoParse(dosyaAd, constr);
        }
        public PdfFileType FileType { get; set; } = PdfFileType.Bilinmiyor;
        public int RelationId { get; set; }
        public int RelationObject { get; set; }

        private void DoParse(string dosyaAd, string constr)
        {
            try
            {
                string[] data = null;
                using (OracleConnection ora = new OracleConnection(constr))
                {
                    OracleCommand comm = ora.CreateCommand();
                    comm.CommandType = System.Data.CommandType.Text;
                    data = dosyaAd.Split('_');

                    if (data == null || data.Length == 0) return;

                    ora.Open();

                    #region -- İş İstasyonu Dosyası (Dosya IsIstasyonKodu_UGTLXXX ile oluşur.)
                    if (dosyaAd.ToUpper().IndexOf("UGTL") != -1)
                    {
                        comm.CommandText = "SELECT W.WSTATION_ID, 249 as releationid, 476273311 as RelationObject, 3 as dosya FROM UYUMSOFT.PRDD_WSTATION W WHERE W.BRANCH_ID = 1010 AND W.CO_ID = 191 AND W.WSTATION_CODE = :istasyonkod";
                        comm.Parameters.AddWithValue(":istasyonkod", data[0]);

                        Logger.I(string.Concat(comm.CommandText, "Prm:", ":istasyonkod", data[0]));

                        using (OracleDataReader dr = comm.ExecuteReader())
                        {
                            if (dr != null && dr.Read())
                            {
                                if (!dr.IsDBNull(0))
                                    this.RelationId = Convert.ToInt32(dr.GetValue(0));
                                if (!dr.IsDBNull(2))
                                    this.RelationObject = Convert.ToInt32(dr.GetValue(2));
                                if (!dr.IsDBNull(3))
                                    this.FileType = IntToFileType(Convert.ToInt32(dr.GetValue(3)));
                            }
                            if (dr != null) dr.Close();
                            return;
                        }
                    }
                    #endregion
                    #region -- Ürün Ağacı Dosyası (Dosya Adı Sonunda "UA" veya "OG" yer alır.)
                    if (dosyaAd.ToUpper().IndexOf("UA") != -1 || dosyaAd.ToUpper().IndexOf("OG") != -1)
                    {

                        comm.CommandText = "SELECT U.BOM_M_ID, 56348 as Releationid, -2089279086 as OG_RelationObject, 1 as dosya FROM UYUMSOFT.INVD_BRANCH_ITEM B INNER JOIN UYUMSOFT.INVD_ITEM M ON B.ITEM_ID = M.ITEM_ID INNER JOIN UYUMSOFT.PRDD_BOM_M U ON B.ITEM_ID = U.ITEM_ID WHERE B.BRANCH_ID = 1010 AND B.CO_ID = 191 AND M.ITEM_CODE = :stokkod AND M.ITEM_CODE || REPLACE(REPLACE(U.ALTERNATIVE_NO,'-',''),'_','') = REPLACE(REPLACE(:dosyaadi,'-',''),'_','')";
                        comm.Parameters.AddWithValue(":stokkod", data[0]);
                        comm.Parameters.AddWithValue(":dosyaadi", dosyaAd.Replace("UA", "").Replace("OG", ""));

                        Logger.I(string.Concat(comm.CommandText, "\tPrm:", ":stokkod", data[0], ":dosyaadi", dosyaAd.Replace("UA", "").Replace("OG", "")));

                        using (OracleDataReader dr = comm.ExecuteReader())
                        {
                            if (dr != null && dr.Read())
                            {
                                if (!dr.IsDBNull(0))
                                    this.RelationId = Convert.ToInt32(dr.GetValue(0));
                                if (!dr.IsDBNull(2))
                                    this.RelationObject = Convert.ToInt32(dr.GetValue(2));
                                if (!dr.IsDBNull(3))
                                    this.FileType = IntToFileType(Convert.ToInt32(dr.GetValue(3)));
                                if (dr != null) dr.Close();
                                return;
                            }
                        }
                    }
                    #endregion
                    #region -- Ürün Rotası Dosyası (Dosya Stok kodu ve Rota Alternatif No ile oluşur.)
                    comm = ora.CreateCommand();
                    comm.CommandType = System.Data.CommandType.Text;
                    comm.CommandText = "SELECT R.PRODUCT_ROUTE_M_ID, 2588 as releationid, 1098530797 as RelationObject, 2 as dosya FROM UYUMSOFT.INVD_BRANCH_ITEM B INNER JOIN UYUMSOFT.INVD_ITEM M ON B.ITEM_ID = M.ITEM_ID INNER JOIN UYUMSOFT.PRDD_PRODUCT_ROUTE_M R ON B.ITEM_ID = R.ITEM_ID WHERE B.BRANCH_ID = 1010 AND B.CO_ID = 191 AND M.ITEM_CODE = :stokkod AND M.ITEM_CODE || REPLACE(REPLACE(R.ALTERNATIVE_NO,'-',''),'_','') = REPLACE(REPLACE(:dosyaadi,'-',''),'_','')";
                    comm.Parameters.AddWithValue(":stokkod", data[0]);
                    comm.Parameters.AddWithValue(":dosyaadi", dosyaAd);

                    Logger.I(string.Concat(comm.CommandText, "\tPrm:", ":stokkod", data[0], ":dosyaadi", dosyaAd));

                    using (OracleDataReader dr = comm.ExecuteReader())
                    {
                        if (dr != null && dr.Read())
                        {
                            if (!dr.IsDBNull(0))
                                this.RelationId = Convert.ToInt32(dr.GetValue(0));
                            if (!dr.IsDBNull(2))
                                this.RelationObject = Convert.ToInt32(dr.GetValue(2));
                            if (!dr.IsDBNull(3))
                                this.FileType = IntToFileType(Convert.ToInt32(dr.GetValue(3)));
                            if (dr != null) dr.Close();
                            return;
                        }
                    }
                    #endregion
                    #region -- Ürün Rota Detayı Dosyası (StokKodu_RotaAlternatifNo_OprsSıraNo_OprsKodu şeklinde oluşur.)
                    comm = ora.CreateCommand();
                    comm.CommandType = System.Data.CommandType.Text;
                    comm.CommandText = @"SELECT RD.PRODUCT_ROUTE_D_ID, R.PRODUCT_ROUTE_M_ID, -1228144813 as RelationObject, 2 as dosya 
FROM UYUMSOFT.INVD_BRANCH_ITEM            B 
INNER JOIN UYUMSOFT.INVD_ITEM             M ON B.ITEM_ID = M.ITEM_ID 
INNER JOIN UYUMSOFT.PRDD_PRODUCT_ROUTE_M  R ON B.ITEM_ID = R.ITEM_ID 
INNER JOIN UYUMSOFT.PRDD_PRODUCT_ROUTE_D RD ON R.PRODUCT_ROUTE_M_ID = RD.PRODUCT_ROUTE_M_ID 
INNER JOIN UYUMSOFT.PRDD_OPERATION        O ON RD.OPERATION_ID      = O.OPERATION_ID 
WHERE B.BRANCH_ID = 1010 AND B.CO_ID = 191 AND M.ITEM_CODE = :stokkod AND (M.ITEM_CODE || REPLACE(REPLACE(R.ALTERNATIVE_NO,'-',''),'_','') || RD.OPERATION_SEQUENTIAL || O.OPERATION_CODE = REPLACE(REPLACE(:dosyaadi,'-',''),'_','') 
OR M.ITEM_CODE || REPLACE(REPLACE(R.ALTERNATIVE_NO,'-',''),'_','') || '0' || RD.OPERATION_SEQUENTIAL || O.OPERATION_CODE = REPLACE(REPLACE(:dosyaadi,'-',''),'_',''))";
                    comm.Parameters.AddWithValue(":stokkod", data[0]);
                    comm.Parameters.AddWithValue(":dosyaadi", dosyaAd);

                    Logger.I(string.Concat(comm.CommandText, "\tPrm:", ":stokkod", data[0], ":dosyaadi", dosyaAd));

                    using (OracleDataReader dr = comm.ExecuteReader())
                    {
                        if (dr != null && dr.Read())
                        {
                            if (!dr.IsDBNull(0))
                                this.RelationId = Convert.ToInt32(dr.GetValue(0));
                            if (!dr.IsDBNull(2))
                                this.RelationObject = Convert.ToInt32(dr.GetValue(2));
                            if (!dr.IsDBNull(3))
                                this.FileType = IntToFileType(Convert.ToInt32(dr.GetValue(3)));
                            if (dr != null) dr.Close();
                            return;
                        }
                    }
                    #endregion
                    #region -- Stok Dosyası (Dosya Stok Kodu ile oluşur.)
                    comm = ora.CreateCommand();
                    comm.CommandType = System.Data.CommandType.Text;
                    comm.CommandText = @"SELECT M.ITEM_ID, 69076 as releationid, 1690415898 as RelationObject, 4 as dosya
FROM UYUMSOFT.INVD_BRANCH_ITEM B INNER JOIN UYUMSOFT.INVD_ITEM M ON B.ITEM_ID = M.ITEM_ID
WHERE B.BRANCH_ID = 1010 AND B.CO_ID = 191 AND M.ITEM_CODE = :dosyaadi";
                    comm.Parameters.AddWithValue(":dosyaadi", dosyaAd);

                    Logger.I(string.Concat(comm.CommandText, "\tPrm:", ":stokkod", data[0], ":dosyaadi", dosyaAd));

                    using (OracleDataReader dr = comm.ExecuteReader())
                    {
                        if (dr != null && dr.Read())
                        {
                            if (!dr.IsDBNull(0))
                                this.RelationId = Convert.ToInt32(dr.GetValue(0));
                            if (!dr.IsDBNull(2))
                                this.RelationObject = Convert.ToInt32(dr.GetValue(2));
                            if (!dr.IsDBNull(3))
                                this.FileType = IntToFileType(Convert.ToInt32(dr.GetValue(3)));
                            if (dr != null) dr.Close();
                            return;
                        }
                    }
                    #endregion
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Trace.WriteLine("Dosya tipi bulmada hata!");
                System.Diagnostics.Trace.WriteLine(exc.Message);
                System.Diagnostics.Trace.WriteLine(exc.StackTrace);
                //MessageBox.Show("Dosya tipi bulmada hata! " + exc.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        private PdfFileType IntToFileType(int value)
        {
            if (value == 0)
            {
                return PdfFileType.Bilinmiyor;
            }
            else if (value == 1)
            {
                return PdfFileType.UrunAgacKod;
            }
            else if (value == 2)
            {
                return PdfFileType.RotaKod;
            }
            else if (value == 3)
            {
                return PdfFileType.IstasyonKod;
            }
            else if (value == 4)
            {
                return PdfFileType.StokKod;
            }
            else
            {
                return PdfFileType.Diger;
            }
        }

    }
}
