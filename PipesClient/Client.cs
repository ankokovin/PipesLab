using System;
using System.Diagnostics;
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
using Newtonsoft.Json;

namespace Pipes
{
    public partial class frmMain : Form
    {
        private bool LoggedIn = false;
        private string nickname = string.Empty;
        private Int32 PipeHandle;   // дескриптор канала
        private Thread t;
        private string name;
        // конструктор формы
        public frmMain()
        {
            InitializeComponent();
            t = new Thread(HandlePipe);
            t.Start();
            name = this.Text + "     " + Dns.GetHostName();
            this.Text = name;   // выводим имя текущей машины в заголовок формы
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            uint BytesWritten = 0;  // количество реально записанных в канал байт
            string json;
            if (LoggedIn)
            {
                var req = new BObjects.MessageRequest
                {
                    Message = tbMessage.Text,
                    nickName = nickname,
                    nodeName = Dns.GetHostName()
                };

                
                json = JsonConvert.SerializeObject(req,  new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });

                
            }
            else
            {
                salt = Guid.NewGuid().ToString();
                nickname = tbMessage.Text;
                var req = new BObjects.LogInRequest { nickName = nickname, nodeName = Dns.GetHostName(),salt=salt };
                json = JsonConvert.SerializeObject(req,  new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All
                });

            }
            byte[] buff = Encoding.Unicode.GetBytes(json);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт

            // открываем именованный канал, имя которого указано в поле tbPipe
            var PipeHandleO = DIS.Import.CreateFile(tbPipe.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(PipeHandleO, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(PipeHandleO);                                                                 // закрываем дескриптор канала
            if (!LoggedIn)
            {
                nickname = tbMessage.Text;
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
            this.Invoke((MethodInvoker)delegate{
                this.Text = name + " " + nickname;
            });
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
            this.Invoke((MethodInvoker)delegate {
                this.Text = name;
            });
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
        string salt;
        bool opened;
        bool connected;
        private void HandlePipe()
        {
            uint realBytesReaded = 0;   // количество реально прочитанных из канала байтов
            opened = false;
            while (!stop)
            {
                if (nickname.Length > 0)
                {
                    if (!opened)
                    {
                        opened = true;
                        Debug.WriteLine("got nickname:" + nickname);
                        string pipename = Helpers.ClientPipeName(Dns.GetHostName(), nickname,salt, true);
                        Debug.WriteLine("client pipename:" + pipename);
                        PipeHandle = DIS.Import.CreateNamedPipe(pipename, DIS.Types.PIPE_ACCESS_DUPLEX, DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT, DIS.Types.PIPE_UNLIMITED_INSTANCES, 0, 1024, DIS.Types.NMPWAIT_WAIT_FOREVER, (uint)0);
                        if (PipeHandle == -1)
                        {
                            throw new Exception("Couldn't create pipe");
                        }
                    }

                    if ( DIS.Import.ConnectNamedPipe(PipeHandle, 0))
                    {
                        connected = true;
                        byte[] buff = new byte[1024];                                           // буфер прочитанных из канала байтов
                        DIS.Import.FlushFileBuffers(PipeHandle);                                // "принудительная" запись данных, расположенные в буфере операционной системы, в файл именованного канала
                        DIS.Import.ReadFile(PipeHandle, buff, 1024, ref realBytesReaded, 0);    // считываем последовательность байтов из канала в буфер buff
                        string reseviedMessage = Encoding.Unicode.GetString(buff);
                        Debug.WriteLine("Client got " + reseviedMessage);
                        var msg = JsonConvert.DeserializeObject< BObjects.ServerMessage>(reseviedMessage, new JsonSerializerSettings
                        {
                            TypeNameHandling = TypeNameHandling.All
                        });

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
                                    nickname = string.Empty;
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
                            }
                            else if (msg is BObjects.ShutDownMessage || msg is BObjects.LogoutAcceptMessage)
                            {
                                ShowMessage(Helpers.DisplayMessage(msg));
                                onLoggedOut();
                                LoggedIn = false;
                                nickname = string.Empty;
                            }
                        }

                    }
                    DIS.Import.DisconnectNamedPipe(PipeHandle);                             // отключаемся от канала клиента 
                    connected = false;
                }
                else
                {
                    opened = false;
                }
                Thread.Sleep(500);
            }
           
        }

        private void disconnectButton_Click(object sender, EventArgs e)
        {
            opened = false;
            SendDisconnect();
        }

        private void SendDisconnect()
        {
            if (!LoggedIn)
                return;
            uint BytesWritten=0;
            string json;
            var req = new BObjects.LogOutRequest { nickName = nickname, nodeName = Dns.GetHostName() };
            json = JsonConvert.SerializeObject(req,Formatting.Indented, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.All
            });
            byte[] buff = Encoding.Unicode.GetBytes(json);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт

            // открываем именованный канал, имя которого указано в поле tbPipe
            var PipeHandleO = DIS.Import.CreateFile(tbPipe.Text, DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(PipeHandleO, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(PipeHandleO);
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            stop = true;
            SendDisconnect();
            if (t != null)
                t.Abort();          // завершаем поток

            if (PipeHandle != -1)
                DIS.Import.CloseHandle(PipeHandle);     // закрываем дескриптор канала
        }
    }
}
