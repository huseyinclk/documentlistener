using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileManager
{
    public class FTPFactory
    {

        private string remoteHost, remotePath, remoteUser, remotePass, mes;
        private int remotePort, bytes;
        private Socket clientSocket;

        private int retValue;
        private Boolean debug;
        private Boolean logined;
        private string reply;
        IPAddress ip;
        //public System.Windows.Forms.Label durumgosterge;
        //public System.Windows.Forms.TextBox durumGosterge2;
        //public System.Windows.Forms.ProgressBar progressbarim;


        private static int BLOCK_SIZE = 512;

        Byte[] buffer = new Byte[BLOCK_SIZE];
        Encoding ASCII = Encoding.ASCII;

        public static FTPFactory NewInstince()
        {
            FTPFactory ftp = new FTPFactory(AppSettingHelper.Default.ftphost, AppSettingHelper.Default.ftpuser, AppSettingHelper.Default.ftppass, AppSettingHelper.Default.ftpport);
            return ftp;
        }

        public FTPFactory()
        {
            //			remoteHost  = "localhost";
            //			remotePath  = ".";
            //			remoteUser  = "anonymous";
            //			remotePass  = "1234";
            //			remotePort  = 21;
            //			debug     = false;
            //			logined    = false;

            remoteHost = "192.168.55.100";
            remotePath = ".";
            remoteUser = "elterm";
            remotePass = "elterm";
            remotePort = 21;
            debug = false;
            logined = false;

        }
        public FTPFactory(string FtpIP)
        {
            remoteHost = FtpIP;
            remotePath = ".";
            remoteUser = "elterm";
            remotePass = "elterm";
            remotePort = 21;
            debug = false;
            logined = false;

        }

        public FTPFactory(string FtpIP, string user, string pass, int port)
        {
            remoteHost = FtpIP;
            remotePath = ".";
            remoteUser = user;
            remotePass = pass;
            remotePort = port;
            debug = false;
            logined = false;
            Logger.I(string.Concat("FTP sunucuna bağlanılıyor ..", remoteHost, remoteUser, remotePort));

        }

        ///
        /// Set the name of the FTP server to connect to.
        ///
        /// Server name
        public void setRemoteHost(string remoteHost)
        {
            this.remoteHost = remoteHost;
        }

        ///
        /// Return the name of the current FTP server.
        ///
        /// Server name
        public string getRemoteHost()
        {
            return remoteHost;
        }

        ///
        /// Set the port number to use for FTP.
        ///
        /// Port number
        public void setRemotePort(int remotePort)
        {
            this.remotePort = remotePort;
        }

        ///
        ///
        /// Return the current port number.
        ///
        /// Current port number
        public int getRemotePort()
        {
            return remotePort;
        }

        ///
        /// Set the remote directory path.
        ///
        /// The remote directory path
        public void setRemotePath(string remotePath)
        {
            this.remotePath = remotePath;
        }

        ///
        /// Return the current remote directory path.
        ///
        /// The current remote directory path.
        public string getRemotePath()
        {
            return remotePath;
        }

        ///
        /// Set the user name to use for logging into the remote server.
        ///
        /// Username
        public void setRemoteUser(string remoteUser)
        {
            this.remoteUser = remoteUser;
        }

        ///
        /// Set the password to user for logging into the remote server.
        ///
        /// Password
        public void setRemotePass(string remotePass)
        {
            this.remotePass = remotePass;
        }

        /// <summary>
        /// Asiri Yuklenme (EFE)
        /// </summary>
        /// <returns></returns>
        public string[] getFileList()
        {

            if (!logined)
            {
                login();
            }

            Socket cSocket = createDataSocket();

            sendCommand("NLST ");

            if (!(retValue == 150 || retValue == 125))
            {
                Logger.E(reply);
                return null;
                //throw new IOException(reply.Substring(4));
            }

            mes = "";

            while (true)
            {

                int bytes = cSocket.Receive(buffer, buffer.Length, 0);
                mes += ASCII.GetString(buffer, 0, bytes);

                if (bytes < buffer.Length)
                {
                    break;
                }
            }

            char[] seperator = { '\n' };
            string[] mess = mes.Split(seperator);

            cSocket.Close();

            readReply();

            if (retValue != 226)
            {
                Logger.E(reply);
                return null;
                //throw new IOException(reply.Substring(4));
            }
            return mess;

        }
        ///
        /// Return a string array containing the remote directory's file list.
        ///
        ///
        ///
        public string[] getFileList(string mask)
        {

            if (!logined)
            {
                login();
            }

            Socket cSocket = createDataSocket();

            sendCommand("NLST " + mask);

            if (!(retValue == 150 || retValue == 125))
            {
                Logger.E(reply);
                return null;
                //throw new IOException(reply.Substring(4));
            }

            mes = "";

            while (true)
            {

                int bytes = cSocket.Receive(buffer, buffer.Length, 0);
                mes += ASCII.GetString(buffer, 0, bytes);

                if (bytes < buffer.Length)
                {
                    break;
                }
            }

            char[] seperator = { '\n' };
            string[] mess = mes.Split(seperator);

            cSocket.Close();

            readReply();

            if (retValue != 226)
            {
                Logger.E(reply);
                return null;
                //throw new IOException(reply.Substring(4));
            }
            return mess;

        }

        ///
        /// Return the size of a file.
        ///
        ///
        ///
        public long getFileSize(string fileName)
        {
            if (!logined)
            {
                login();
            }
            Logger.I("dosya boyutu:" + "SIZE " + Path.GetFileName(fileName));
            sendCommand("SIZE " + Path.GetFileName(fileName));
            long size = 0;

            if (retValue == 213)
            {
                size = Int64.Parse(reply.Substring(4));
            }
            else
            {
                return 0;
                //throw new IOException(reply.Substring(4));
            }
            return size;
        }

        public string getFileModifiedTime(string fileName)
        {
            if (!logined)
            {
                login();
            }

            sendCommand("MDTM " + Path.GetFileName(fileName));
            string mtime;

            if (retValue == 213)
            {
                mtime = reply.Substring(4);
            }
            else
            {
                return "";
                //throw new IOException(reply.Substring(4));
            }
            return mtime;
        }

        ///
        /// Login to the remote server.
        ///
        public void login()
        {
            if (!IPAddress.TryParse(remoteHost, out ip))
                ip = Dns.GetHostEntry(remoteHost).AddressList[0];

            Logger.I(string.Concat("Sunucya bağlanılıyor Host", remoteHost, ", IP:", ip.ToString()));

            clientSocket = new
                Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new
                IPEndPoint(ip/*Dns.Resolve(remoteHost).AddressList[0]*/, remotePort);

            try
            {
                clientSocket.Connect(ep);
            }
            catch (Exception ex)
            {
                Logger.E(ex);
                return;
                //throw new IOException("Sunucu Bağlantısı Sağlanamadı");
                //MessageBox.Show(ex.Message);
            }

            readReply();
            if (retValue != 220)
            {
                close();
                Logger.E(reply);
                return;
                //throw new IOException(reply.Substring(4));
            }
            //if (debug)
            //    durumgosterge.Text = ("USER " + remoteUser);

            sendCommand("USER " + remoteUser);

            if (!(retValue == 331 || retValue == 230))
            {
                cleanup();
                Logger.E(reply);
                return;
                //throw new IOException(reply.Substring(4));
            }

            if (retValue != 230)
            {
                //if (debug)
                //    durumgosterge.Text = ("PASS xxx");

                sendCommand("PASS " + remotePass);
                if (!(retValue == 230 || retValue == 202))
                {
                    cleanup();
                    Logger.E(reply);
                    return;
                    //throw new IOException(reply.Substring(4));
                }
            }

            logined = true;
            //durumgosterge.Text = ("Baglanilan sunucu " + remoteHost);

            chdir(remotePath);

        }

        ///
        /// If the value of mode is true, set binary mode for downloads.
        /// Else, set Ascii mode.
        ///
        ///
        public void setBinaryMode(Boolean mode)
        {

            if (mode)
            {
                sendCommand("TYPE I");
            }
            else
            {
                sendCommand("TYPE A");
            }
            if (retValue != 200)
            {
                Logger.E(reply);
                //throw new IOException(reply.Substring(4));
            }
        }

        ///
        /// Download a file to the Assembly's local directory,
        /// keeping the same file name.
        ///
        ///
        public void download(string remFileName)
        {
            download(remFileName, "", false);
        }

        ///
        /// Download a remote file to the Assembly's local directory,
        /// keeping the same file name, and set the resume flag.
        ///
        ///
        ///
        public void download(string remFileName, Boolean resume)
        {
            download(remFileName, "", resume);
        }

        ///
        /// Download a remote file to a local file name which can include
        /// a path. The local file name will be created or overwritten,
        /// but the path must exist.
        ///
        ///
        ///
        public void download(string remFileName, string locFileName)
        {
            download(remFileName, locFileName, false);
        }

        ///
        /// Download a remote file to a local file name which can include
        /// a path, and set the resume flag. The local file name will be
        /// created or overwritten, but the path must exist.
        ///
        ///
        ///
        ///


        public void download(string remFileName, string locFileName, Boolean resume)
        {
            if (!logined)
            {
                login();
            }

            setBinaryMode(true);
            long allsize = 0, downsize = 0;

            Logger.I("Dosya Adı: " + remFileName);
            //durumGosterge2.Text = ("Dosya Adı: " + remFileName);

            if (locFileName.Equals(""))
            {
                locFileName = remFileName;
            }

            if (!File.Exists(locFileName))
            {
                Stream st = File.Create(locFileName);
                st.Close();
            }

            allsize = getFileSize(remFileName);

            FileStream output = new
                FileStream(locFileName, FileMode.Open);

            Socket cSocket = createDataSocket();

            long offset = 0;

            if (resume)
            {

                offset = output.Length;

                if (offset > 0)
                {
                    sendCommand("REST " + offset);
                    if (retValue != 350)
                    {
                        Logger.E(reply);
                        //throw new IOException(reply.Substring(4));
                        //Some servers may not support resuming.
                        offset = 0;
                    }
                }

                if (offset > 0)
                {
                    //if (debug)
                    //{
                    //    //							durumgosterge.Text=("seeking to " + offset);
                    //}
                    long npos = output.Seek(offset, SeekOrigin.Begin);
                    //						durumgosterge.Text=("new pos="+npos);
                }
                downsize += offset;
            }

            sendCommand("RETR " + remFileName);

            if (!(retValue == 150 || retValue == 125))
            {
                output.Close();
                Logger.E(reply);
                return;
                //throw new IOException(reply.Substring(4));
            }
            while (true)
            {
                bytes = cSocket.Receive(buffer, buffer.Length, 0);
                output.Write(buffer, 0, bytes);
                if (bytes <= 0)
                {
                    break;
                }
                downsize += bytes;
                //progressbarim.Maximum = Convert.ToInt32(allsize);
                //progressbarim.Value = Convert.ToInt32(downsize);
                //durumgosterge.Text = Convert.ToString(downsize) + "/" + Convert.ToString(allsize) + " Byte";
                //Application.DoEvents();
            }

            output.Close();
            if (cSocket.Connected)
            {
                cSocket.Close();
            }

            readReply();

            if (!(retValue == 226 || retValue == 250))
            {
                Logger.E(reply);
                //throw new IOException(reply.Substring(4));
            }
        }

        ///
        /// Upload a file.
        ///
        ///
        public void upload(string fileName)
        {
            upload(fileName, false);
        }

        ///
        /// Upload a file and set the resume flag.
        ///
        ///
        ///
        public void upload(string fileName, Boolean resume)
        {
            if (!File.Exists(fileName))
            {
                Logger.E("Kaynak dosya bulunamadı! " + fileName);
                return;
            }

            if (!logined)
            {
                login();
            }
            long offset = 0;

            if (resume)
            {
                try
                {
                    setBinaryMode(true);
                    offset = getFileSize(fileName);
                }
                catch (Exception exc)
                {
                    Logger.E(exc);
                    offset = 0;
                }
            }

            Socket cSocket = createDataSocket();

            if (offset > 0)
            {

                sendCommand("REST " + offset);
                if (retValue != 350)
                {
                    Logger.E(reply);
                    //throw new IOException(reply.Substring(4));
                    //Remote server may not support resuming.
                    offset = 0;
                }
            }

            sendCommand("STOR " + Path.GetFileName(fileName));

            if (!(retValue == 125 || retValue == 150))
            {
                Logger.E(reply);
                return;
                //throw new IOException(reply.Substring(4));
            }

            // open input stream to read source file
            Stream input = new FileStream(fileName, FileMode.Open);

            if (offset != 0)
            {
                //if (debug)
                //{
                //    durumgosterge.Text = ("seeking to " + offset);
                //}
                input.Seek(offset, SeekOrigin.Begin);
            }

            //durumgosterge.Text = ("Uploading file " + fileName + " to " + remotePath);

            while ((bytes = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                cSocket.Send(buffer, bytes, 0);
            }
            input.Close();

            //durumgosterge.Text = ("");

            if (cSocket.Connected)
            {
                cSocket.Close();
            }

            readReply();
            if (!(retValue == 226 || retValue == 250))
            {
                Logger.E(reply);
                //throw new IOException(reply.Substring(4));
            }
        }

        ///
        /// Delete a file from the remote FTP server.
        ///
        //
        public void deleteRemoteFile(string fileName)
        {
            if (!logined)
            {
                login();
            }

            sendCommand("DELE " + fileName);

            if (retValue != 250)
            {
                Logger.E(reply);
                //throw new IOException(reply.Substring(4));
            }

        }

        ///
        /// Rename a file on the remote FTP server.
        ///
        ///
        ///
        public void renameRemoteFile(string oldFileName, string newFileName)
        {

            if (!logined)
            {
                login();
            }

            sendCommand("RNFR " + oldFileName);

            if (retValue != 350)
            {
                Logger.E(reply);
                return;
                //throw new IOException(reply.Substring(4));
            }

            //  known problem
            //  rnto will not take care of existing file.
            //  i.e. It will overwrite if newFileName exist
            sendCommand("RNTO " + newFileName);
            if (retValue != 250)
            {
                Logger.E(reply);
                //throw new IOException(reply.Substring(4));
            }

        }

        ///
        /// Create a directory on the remote FTP server.
        ///
        ///
        public void mkdir(string dirName)
        {

            if (!logined)
            {
                login();
            }

            sendCommand("MKD " + dirName);

            if (retValue != 250)
            {
                Logger.E(reply);
                //throw new IOException(reply.Substring(4));
            }

        }

        ///
        /// Delete a directory on the remote FTP server.
        ///
        ///
        public void rmdir(string dirName)
        {

            if (!logined)
            {
                login();
            }

            sendCommand("RMD " + dirName);

            if (retValue != 250)
            {
                Logger.E(reply);
                //throw new IOException(reply.Substring(4));
            }

        }

        ///
        /// Change the current working directory on the remote FTP server.
        ///
        ///
        public void chdir(string dirName)
        {

            if (dirName.Equals("."))
            {
                return;
            }

            if (!logined)
            {
                login();
            }

            sendCommand("CWD " + dirName);

            if (retValue != 250)
            {
                Logger.E(reply);
                return;
                //throw new IOException(reply.Substring(4));
            }

            this.remotePath = dirName;

            Logger.I("Current directory is " + remotePath);

            //durumgosterge.Text = ("Current directory is " + remotePath);

        }

        ///
        /// Close the FTP connection.
        ///
        public void close()
        {

            if (clientSocket != null)
            {
                sendCommand("QUIT");
            }

            cleanup();
            //durumgosterge.Text = ("Baglanti Kapatildi...");
        }

        ///
        /// Set debug mode.
        ///
        ///
        public void setDebug(Boolean debug)
        {
            this.debug = debug;
        }

        private void readReply()
        {
            mes = "";
            reply = readLine();
            retValue = Int32.Parse(reply.Substring(0, 3));
        }

        private void cleanup()
        {
            if (clientSocket != null)
            {
                clientSocket.Close();
                clientSocket = null;
            }
            logined = false;
        }

        private string readLine()
        {

            while (true)
            {
                bytes = clientSocket.Receive(buffer, buffer.Length, 0);
                mes += ASCII.GetString(buffer, 0, bytes);
                if (bytes < buffer.Length)
                {
                    break;
                }
            }

            char[] seperator = { '\n' };
            string[] mess = mes.Split(seperator);

            if (mes.Length > 2)
            {
                mes = mess[mess.Length - 2];
            }
            else
            {
                mes = mess[0];
            }

            if (!mes.Substring(3, 1).Equals(" "))
            {
                return readLine();
            }

            //if (debug)
            //{
            //    for (int k = 0; k < mess.Length - 1; k++)
            //    {
            //        durumgosterge.Text = (mess[k]);
            //    }
            //}
            //Logger.I(string.Concat("FTP cevabı ..:", mes));
            return mes;
        }

        private void sendCommand(String command)
        {
            //Logger.I("FTP komut ..:", command);
            Byte[] cmdBytes =
                Encoding.ASCII.GetBytes((command + "\r\n").ToCharArray());
            clientSocket.Send(cmdBytes, cmdBytes.Length, 0);
            readReply();
        }

        private Socket createDataSocket()
        {
            sendCommand("PASV");

            if (retValue != 227)
            {
                Logger.E(reply);
                return null;
                //throw new IOException(reply.Substring(4));
            }

            int index1 = reply.IndexOf('(');
            int index2 = reply.IndexOf(')');
            string ipData =
                reply.Substring(index1 + 1, index2 - index1 - 1);
            int[] parts = new int[6];

            int len = ipData.Length;
            int partCount = 0;
            string buf = "";

            for (int i = 0; i < len && partCount <= 6; i++)
            {

                char ch = Convert.ToChar(ipData.Substring(i, 1));
                if (Char.IsDigit(ch))
                    buf += ch;
                else if (ch != ',')
                {
                    Logger.E("Malformed PASV reply: " + reply);
                    //throw new IOException("Malformed PASV reply: " + reply);
                }

                if (ch == ',' || i + 1 == len)
                {

                    try
                    {
                        parts[partCount++] = Int32.Parse(buf);
                        buf = "";
                    }
                    catch (Exception ex)
                    {
                        Logger.E("Malformed PASV reply: " + reply);
                        Logger.E(ex);
                        return null;
                        //throw new IOException("Malformed PASV reply: " + reply);
                    }
                }
            }

            string ipAddress = parts[0] + "." + parts[1] + "." +
                parts[2] + "." + parts[3];

            int port = (parts[4] << 8) + parts[5];

            Socket s = new
                Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ep = new
                IPEndPoint(ip/*Dns.Resolve(ipAddress).AddressList[0]*/, port);

            try
            {
                s.Connect(ep);
            }
            catch (Exception ex)
            {
                Logger.E("Sunucuya Bağlanılamıyor");
                Logger.E(ex);
                //throw new IOException("Sunucuya Bağlanılamıyor");
            }

            return s;
        }

        public bool FtpUploadFile(string fileName)
        {
            // Get the object used to communicate with the server.
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create("ftp://" + remoteHost + ":" + remotePort + "/" + Path.GetFileName(fileName));
            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.UsePassive = false;

            // This example assumes the FTP site uses anonymous logon.
            request.Credentials = new NetworkCredential(remoteUser, remotePass);

            // Copy the contents of the file to the request stream.
            StreamReader sourceStream = new StreamReader(fileName);
            byte[] fileContents = Encoding.UTF8.GetBytes(sourceStream.ReadToEnd());
            sourceStream.Close();
            request.ContentLength = fileContents.Length;

            Stream requestStream = request.GetRequestStream();
            requestStream.Write(fileContents, 0, fileContents.Length);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            return response.ExitMessage.Trim() == string.Empty;
        }

    }
}
