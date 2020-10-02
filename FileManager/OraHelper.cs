using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.OracleClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    public class OraHelper : IDisposable
    {
        DbProviderFactory factory = null;
        System.Data.IDbConnection conn = null;
        System.Data.IDbCommand comm = null;

        public OraHelper()
        {

        }

        private void Connect()
        {
            if (factory == null)
                factory = DbProviderFactories.GetFactory(StaticsVariable.PROVIDERNAME);

            if (conn == null)
            {
                conn = factory.CreateConnection();
                if (AppSettingHelper.Default.test)
                    conn.ConnectionString = string.Format("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=192.168.0.11)(PORT=1521)))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME=uyumtest)));User Id=uyumsoft;Password=uyumsoft;");//StaticsVariable.CONNECTIONSTR;
                else
                    conn.ConnectionString = string.Format("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1})))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME={2})));User Id=uyumsoft;Password=uyumsoft;", AppSettingHelper.Default.orahost, AppSettingHelper.Default.oraport, AppSettingHelper.Default.oraservis);//StaticsVariable.CONNECTIONSTR;
                Logger.I(string.Format("Data Source=(HOST={0})(PORT={1})(SERVICE_NAME={2})", AppSettingHelper.Default.orahost, AppSettingHelper.Default.oraport, AppSettingHelper.Default.oraservis));
            }

            if (conn.State != ConnectionState.Open)
                conn.Open();

            comm = conn.CreateCommand();
        }

        public bool Exec(string commandText, IDataParameter[] parameters)
        {
            Connect();

            Logger.I(string.Concat(commandText, "\tPrm:", ParametersToString(parameters)));

            comm.Parameters.Clear();
            comm.CommandText = commandText;
            if (parameters != null)
            {
                for (int loop = 0; loop < parameters.Length; loop++)
                {
                    comm.Parameters.Add(parameters[loop]);
                }
            }
            return comm.ExecuteNonQuery() > 0;
        }

        public static string ParametersToString(IDataParameter[] parameters)
        {
            StringBuilder sb = new StringBuilder();
            if (parameters != null && parameters.Length > 0)
            {
                foreach (IDataParameter prm in parameters) sb.AppendFormat("\tName:{0},Value:{1}\t", prm.ParameterName, prm.Value);
            }
            else
            {
                sb.Append("null");
            }
            return sb.ToString();
        }

        public IDataReader ExecReader(string commandText, IDataParameter[] parameters)
        {
            Connect();

            Logger.I(string.Concat(commandText, "\tPrm:", ParametersToString(parameters)));

            comm.Parameters.Clear();
            comm.CommandText = commandText;
            if (parameters != null)
            {
                for (int loop = 0; loop < parameters.Length; loop++)
                {
                    comm.Parameters.Add(parameters[loop]);
                }
            }
            return comm.ExecuteReader();
        }

        public object ExecuteScalar(string commandText, IDataParameter[] parameters)
        {
            Connect();

            Logger.I(string.Concat(commandText, "\tPrm:", ParametersToString(parameters)));

            comm.Parameters.Clear();
            comm.CommandText = commandText;
            if (parameters != null)
            {
                for (int loop = 0; loop < parameters.Length; loop++)
                {
                    comm.Parameters.Add(parameters[loop]);
                }
            }
            return comm.ExecuteScalar();
        }

        public void DeleteRecord(PdfFileInfo fileInf)
        {
            try
            {
                OracleParameter[] delParameters = new OracleParameter[2];
                delParameters[0] = new OracleParameter(":RELATION_OBJECT", fileInf.RelationObject);
                delParameters[1] = new OracleParameter(":RELATION_ID", fileInf.RelationId);

                string delExtra = "";
                if (fileInf.Name.IndexOf("UA") != -1)
                    delExtra = " AND SH0RT_FILE_NAME LIKE '%UA%' ";
                if (fileInf.Name.IndexOf("OG") != -1)
                    delExtra = " AND SH0RT_FILE_NAME LIKE '%OG%' ";
                if (fileInf.Name.IndexOf("UGTL") != -1)
                    delExtra = string.Format(" AND SH0RT_FILE_NAME = '{0}' ", fileInf.Name);

                Exec("DELETE FROM GNLD_UPLOAD_FILE WHERE RELATION_OBJECT = :RELATION_OBJECT AND RELATION_ID = :RELATION_ID " + delExtra, delParameters);
            }
            catch (Exception exception)
            {
                Logger.E(string.Concat("Öncei dökümanlar silinemedi! Message:" + exception.Message, ",StackTrace:", exception.StackTrace));
            }
        }

        public int InsertRecord(PdfFileInfo fileInf)
        {
            try
            {
                int uploadFileId = 1;
                object objIds = ExecuteScalar("SELECT MAX(UPLOAD_FILE_ID) AS UPLOAD_FILE_ID FROM GNLD_UPLOAD_FILE", null);

                if (objIds != null && object.ReferenceEquals(objIds, DBNull.Value) == false)
                {
                    uploadFileId = Convert.ToInt32(objIds) + 1;
                }
                string commandText = "INSERT INTO GNLD_UPLOAD_FILE (UPLOAD_FILE_ID, RELATION_OBJECT, RELATION_ID, SH0RT_FILE_NAME, LONG_FILE_NAME, DOCUMENT_TYPE, DESCRIPTION, CREATE_DATE, CREATE_USER_ID) VALUES (:UPLOAD_FILE_ID, :RELATION_OBJECT, :RELATION_ID, :SH0RT_FILE_NAME, :LONG_FILE_NAME, :DOCUMENT_TYPE, :DESCRIPTION, :CREATE_DATE, :CREATE_USER_ID)";
                OracleParameter[] oraParameters = new OracleParameter[9];
                oraParameters[0] = new OracleParameter(":UPLOAD_FILE_ID", uploadFileId);
                oraParameters[1] = new OracleParameter(":RELATION_OBJECT", fileInf.RelationObject);
                oraParameters[2] = new OracleParameter(":RELATION_ID", fileInf.RelationId);
                oraParameters[3] = new OracleParameter(":SH0RT_FILE_NAME", fileInf.Name);
                oraParameters[4] = new OracleParameter(":LONG_FILE_NAME", fileInf.Name);
                oraParameters[5] = new OracleParameter(":DOCUMENT_TYPE", StaticsVariable.DOCUMENT_TYPE);
                oraParameters[6] = new OracleParameter(":DESCRIPTION", StaticsVariable.DESCRIPTION);
                oraParameters[7] = new OracleParameter(":CREATE_DATE", DateTime.Now);
                oraParameters[8] = new OracleParameter(":CREATE_USER_ID", AppSettingHelper.Default.userid);

                if (Exec(commandText, oraParameters))
                {
                    OracleParameter[] selParameters = new OracleParameter[2];
                    selParameters[0] = new OracleParameter(":UPLOAD_FILE_ID", uploadFileId);
                    selParameters[1] = new OracleParameter(":SH0RT_FILE_NAME", fileInf.Name);
                    objIds = ExecuteScalar("SELECT UPLOAD_FILE_ID FROM GNLD_UPLOAD_FILE WHERE UPLOAD_FILE_ID = :UPLOAD_FILE_ID OR SH0RT_FILE_NAME = :SH0RT_FILE_NAME", selParameters);

                    if (objIds != null && object.ReferenceEquals(objIds, DBNull.Value) == false)
                    {
                        return Convert.ToInt32(objIds);
                    }

                    return uploadFileId;
                }
                else
                {
                    return -1;
                }
            }
            catch (Exception exception)
            {
                Logger.E(string.Concat("Yeni kayıt eklenemedi! Message:" + exception.Message, ",StackTrace:", exception.StackTrace));
                return -2;
            }
        }

        #region IDisposable
        ~OraHelper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private bool disposed = false;

        private void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                if (comm != null)
                {
                    comm.Dispose();
                }
                if (conn != null)
                {
                    if (conn.State == ConnectionState.Open) conn.Close();
                    conn.Dispose();
                }
                conn = null;
                factory = null;
                comm = null;
            }

            disposed = true;
        }
        #endregion
    }
}
