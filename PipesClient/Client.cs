using System;
using System.Threading;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;

using System.Xml.Serialization;

namespace Pipes
{
    public partial class frmMain : Form
    {
        private bool LoggedIn = false;
        private string nickname = string.Empty;
        private Int32 PipeHandle;   // дескриптор канала
        private string PipeName;
        private Thread t;
        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
            this.Text += "     " + Dns.GetHostName();   // выводим имя текущей машины в заголовок формы
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            uint BytesWritten = 0;  // количество реально записанных в канал байт
            string xml;
            if (LoggedIn)
            {
                var req = new BObjects.MessageRequest
                {
                    Message = tbMessage.Text,
                    nickName = nickname,
                    nodeName = Dns.GetHostName()
                };


                XmlSerializer xmlSerializer = new XmlSerializer(typeof(BObjects.MessageRequest));

                using (var sw = new StringWriter())
                {
                    xmlSerializer.Serialize(sw, req);
                    xml = sw.ToString();
                }
            }
            else
            {
                var req = new BObjects.LogInRequest { nickName = tbMessage.Text, nodeName = Dns.GetHostName() };
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(BObjects.LogInRequest));

                using (var sw = new StringWriter())
                {
                    xmlSerializer.Serialize(sw, req);
                    xml = sw.ToString();
                }
            }
            byte[] buff = Encoding.Unicode.GetBytes(xml);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт

            // открываем именованный канал, имя которого указано в поле tbPipe
            var PipeHandleO = DIS.Import.CreateFile(tbPipe.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(PipeHandleO, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(PipeHandleO);                                                                 // закрываем дескриптор канала
            if (!LoggedIn)
            {
                nickname = tbMessage.Text;
                stop = false;
                t = new Thread(HandlePipe);
                t.Start();
            }
        }

        private void lblMessage_Click(object sender, EventArgs e)
        {

        }
        bool stop;


        private void ShowMessage(string msg)
        {
            messageTextBox.Invoke((MethodInvoker)delegate
            {
                if (msg != "")
                    messageTextBox.Text += "\n" + msg;                             // выводим полученное сообщение на форму
            });
        }

        private void onLoggedIn()
        {
            tbPipe.Invoke((MethodInvoker)delegate
            {
                tbPipe.Enabled = false;
            });
            lblMessage.Invoke((MethodInvoker)delegate
            {
                lblMessage.Text = "Сообщение";
            });
            disconnectButton.Invoke((MethodInvoker)delegate
            {
                disconnectButton.Enabled = true;
            });
            ShowMessage("Logged in!");
        }

        private void onLoggedOut()
        {
            tbPipe.Invoke((MethodInvoker)delegate
            {
                tbPipe.Enabled = true;
            });
            lblMessage.Invoke((MethodInvoker)delegate
            {
                lblMessage.Text = "Имя пользователя";
            });
            disconnectButton.Invoke((MethodInvoker)delegate
            {
                disconnectButton.Enabled = false;
            });
            ShowMessage("Logged out!");
        }

        private void HandlePipe()
        {
            PipeHandle = DIS.Import.CreateNamedPipe(Helpers.ClientPipeName(Dns.GetHostName(), nickname), DIS.Types.PIPE_ACCESS_DUPLEX, DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT, DIS.Types.PIPE_UNLIMITED_INSTANCES, 0, 1024, DIS.Types.NMPWAIT_WAIT_FOREVER, (uint)0);
            uint realBytesReaded = 0;   // количество реально прочитанных из канала байтов
            while (!stop)
            {
                if (DIS.Import.ConnectNamedPipe(PipeHandle, 0))
                {
                    byte[] buff = new byte[1024];                                           // буфер прочитанных из канала байтов
                    DIS.Import.FlushFileBuffers(PipeHandle);                                // "принудительная" запись данных, расположенные в буфере операционной системы, в файл именованного канала
                    DIS.Import.ReadFile(PipeHandle, buff, 1024, ref realBytesReaded, 0);    // считываем последовательность байтов из канала в буфер buff
                    string reseviedMessage = Encoding.Unicode.GetString(buff);
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(BObjects.ServerMessage));
                    BObjects.ServerMessage msg;
                    using (var sr = new StringReader(Helpers.ClearString(reseviedMessage)))
                        msg = (BObjects.ServerMessage)xmlSerializer.Deserialize(sr);
                    if (!LoggedIn)
                    {
                        if (msg is BObjects.LoginResult)
                        {
                            if (msg is BObjects.SuccessfulLoginResult)
                            {
                                LoggedIn = true;
                                onLoggedIn();
                            }
                            else
                            {
                                var failres = (BObjects.FailedLoginResult)msg;
                                ShowMessage(failres.Message);
                            }
                        }
                    }
                    else
                    {
                        if (msg is BObjects.UserMessage || msg is BObjects.NewUserMessage || msg is BObjects.QuitUserMessage)
                        {
                            ShowMessage(Helpers.DisplayMessage(msg));
                        }else if (msg is BObjects.ShutDownMessage)
                        {
                            ShowMessage(Helpers.DisplayMessage(msg));
                            onLoggedOut();
                            stop = true;
                        }
                    }
                    
                }
            }
           
        }

        private void disconnectButton_Click(object sender, EventArgs e)
        {
            stop = true;
            onLoggedOut();
            SendDisconnect();
        }

        private void SendDisconnect()
        {
            if (!LoggedIn)
                return;
            uint BytesWritten=0;
            string xml;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(BObjects.LogOutRequest));
            var req = new BObjects.LogOutRequest { nickName = nickname, nodeName = Dns.GetHostName() };
            using (var sw = new StringWriter())
            {
                xmlSerializer.Serialize(sw, req);
                xml = sw.ToString();
            }
            byte[] buff = Encoding.Unicode.GetBytes(xml);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт

            // открываем именованный канал, имя которого указано в поле tbPipe
            var PipeHandleO = DIS.Import.CreateFile(tbPipe.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(PipeHandleO, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(PipeHandleO);
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            stop = true;
            SendDisconnect();
        }
    }
}
