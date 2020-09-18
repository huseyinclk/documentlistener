using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FileManager
{
    public class AppSettingHelper
    {
        static AppSettingHelper defaultSetting = null;
        public static AppSettingHelper Default
        {
            get
            {
                if (defaultSetting == null) defaultSetting = new AppSettingHelper();
                return defaultSetting;
            }
        }


        public static string GetConnectionString()
        {
            return string.Format("Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST={0})(PORT={1})))(CONNECT_DATA=(SERVER=DEDICATED)(SERVICE_NAME={2})));User Id=uyumsoft;Password=uyumsoft;", AppSettingHelper.Default.orahost, AppSettingHelper.Default.oraport, AppSettingHelper.Default.oraservis);
        }

        const string SETTING_FILE_NAME = "config.dat";

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        private void WriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, string.Concat(System.Windows.Forms.Application.StartupPath, "\\config.dat"));
        }

        private string ReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, string.Concat(System.Windows.Forms.Application.StartupPath, "\\", SETTING_FILE_NAME));
            return temp.ToString();

        }

        private string ReadValue(string Section, string Key, string dvalue)
        {
            StringBuilder temp = new StringBuilder(255);
            int i = GetPrivateProfileString(Section, Key, "", temp, 255, string.Concat(System.Windows.Forms.Application.StartupPath, "\\", SETTING_FILE_NAME));
            if (i == 0)
            {
                WriteValue(Section, Key, dvalue);
                return dvalue;
            }
            return temp.ToString();

        }

        public string orahost
        {
            get
            {
                return ReadValue("DATA", "orahost", "weberp.hdrms.local");
            }
            set
            {
                WriteValue("DATA", "orahost", value);
            }
        }

        public string oraservis
        {
            get
            {
                return ReadValue("DATA", "oraservis", "uyumsoft");
            }
            set
            {
                WriteValue("DATA", "oraservis", value);
            }
        }

        public int oraport
        {
            get
            {
                return Convert.ToInt32(ReadValue("DATA", "oraport", "1521"), CultureInfo.CreateSpecificCulture("en-US"));
            }
            set
            {
                WriteValue("DATA", "oraport", value.ToString(CultureInfo.CreateSpecificCulture("en-US")));
            }
        }

        public string argeklasor
        {
            get
            {
                return ReadValue("PATH", "argeklasor", "C:\\temp");
            }
            set
            {
                WriteValue("PATH", "argeklasor", value);
            }
        }

        public string hedefklasor
        {
            get
            {
                return ReadValue("PATH", "hedefklasor", "C:\\temp");
            }
            set
            {
                WriteValue("PATH", "hedefklasor", value);
            }
        }

        public int branchid
        {
            get
            {
                return Convert.ToInt32(ReadValue("DATA", "branchid", "1010"), CultureInfo.CreateSpecificCulture("en-US"));
            }
            set
            {
                WriteValue("DATA", "branchid", value.ToString(CultureInfo.CreateSpecificCulture("en-US")));
            }
        }

        public int coid
        {
            get
            {
                return Convert.ToInt32(ReadValue("DATA", "coid", "191"), CultureInfo.CreateSpecificCulture("en-US"));
            }
            set
            {
                WriteValue("DATA", "coid", value.ToString(CultureInfo.CreateSpecificCulture("en-US")));
            }
        }

        public int userid
        {
            get
            {
                return Convert.ToInt32(ReadValue("DATA", "userid", "2773"), CultureInfo.CreateSpecificCulture("en-US"));
            }
            set
            {
                WriteValue("DATA", "userid", value.ToString(CultureInfo.CreateSpecificCulture("en-US")));
            }
        }

        public string mailhost
        {
            get
            {
                return ReadValue("MAIL", "mailhost", "srv01.turktelekomeposta.com");
            }
            set
            {
                WriteValue("MAIL", "mailhost", value);
            }
        }

        public int mailport
        {
            get
            {
                return Convert.ToInt32(ReadValue("MAIL", "mailport", "587"), CultureInfo.CreateSpecificCulture("en-US"));
            }
            set
            {
                WriteValue("MAIL", "mailport", value.ToString(CultureInfo.CreateSpecificCulture("en-US")));
            }
        }

        public string mailuser
        {
            get
            {
                return ReadValue("MAIL", "mailuser", "data@hidromas.com");
            }
            set
            {
                WriteValue("MAIL", "mailuser", value);
            }
        }

        public string mailpass
        {
            get
            {
                return Decrypt(ReadValue("MAIL", "mailpass", Encrypt("7nL99PyA")));
            }
            set
            {
                WriteValue("MAIL", "mailpass", Encrypt(value));
            }
        }


        public bool SendMail
        {
            get
            {
                return ReadValue("MAIL", "statu", "0") == "1";
            }
            set
            {
                WriteValue("MAIL", "statu", value ? "1" : "0");
            }
        }

        public int timeout
        {
            get
            {
                return Convert.ToInt32(ReadValue("APP", "timeout", "10"), CultureInfo.CreateSpecificCulture("en-US"));
            }
            set
            {
                WriteValue("APP", "timeout", value.ToString(CultureInfo.CreateSpecificCulture("en-US")));
            }
        }

        public bool test
        {
            get
            {
                return ReadValue("APP", "test", "0") == "1";
            }
            set
            {
                WriteValue("APP", "test", value ? "1" : "0");
            }
        }

        public global::System.Diagnostics.TraceLevel tracelavel
        {
            get
            {
                global::System.Diagnostics.TraceLevel level = System.Diagnostics.TraceLevel.Verbose;
                Enum.TryParse<System.Diagnostics.TraceLevel>(ReadValue("APP", "tracelavel", global::System.Diagnostics.TraceLevel.Verbose.ToString()),out level);
                return level;
            }
            set
            {
                WriteValue("APP", "tracelavel", value.ToString());
            }
        }

        public string ftppass
        {
            get
            {
                return Decrypt(ReadValue("FTP", "ftppass", Encrypt(".123456a")));
            }
            set
            {
                WriteValue("FTP", "ftppass", Encrypt(value));
            }
        }

        public string ftpuser
        {
            get
            {
                return ReadValue("FTP", "ftpuser", "uyumsoft");
            }
            set
            {
                WriteValue("FTP", "ftpuser", value);
            }
        }

        public string ftphost
        {
            get
            {
                return ReadValue("FTP", "ftphost", "weberp.hdrms.local");
            }
            set
            {
                WriteValue("FTP", "ftphost", value);
            }
        }

        public int ftpport
        {
            get
            {
                return Convert.ToInt32(ReadValue("FTP", "ftpport", "2121"), CultureInfo.CreateSpecificCulture("en-US"));
            }
            set
            {
                WriteValue("FTP", "ftpport", value.ToString(CultureInfo.CreateSpecificCulture("en-US")));
            }
        }

        public static string Encrypt(string clearText)
        {
            if (!string.IsNullOrWhiteSpace(clearText))
            {
                string EncryptionKey = "Whm!Uyum*.";
                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(clearBytes, 0, clearBytes.Length);
                            cs.Close();
                        }
                        clearText = Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            else
            {
                clearText = string.Empty;
            }
            return clearText;
        }
        public static string Decrypt(string cipherText)
        {
            if (!string.IsNullOrWhiteSpace(cipherText))
            {
                string EncryptionKey = "Whm!Uyum*.";
                cipherText = cipherText.Replace(" ", "+");
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
            }
            else
            {
                cipherText = string.Empty;
            }
            return cipherText;
        }
    }
}
