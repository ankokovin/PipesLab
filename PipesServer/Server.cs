using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Serialization;
using System.IO;

namespace Pipes
{
    public partial class frmMain : Form
    {
        private Dictionary<string, string> Nicknames = new Dictionary<string, string>();


        private Int32 PipeHandle;                                                       // дескриптор канала
        private string PipeName = "\\\\" + Dns.GetHostName() + "\\pipe\\ServerPipe";    // имя канала, Dns.GetHostName() - метод, возвращающий имя машины, на которой запущено приложение
        private Thread t;                                                               // поток для обслуживания канала
        private bool _continue = true;                                                  // флаг, указывающий продолжается ли работа с каналом

        // конструктор формы
        public frmMain()
        {
            InitializeComponent();

            // создание именованного канала
            PipeHandle = DIS.Import.CreateNamedPipe("\\\\.\\pipe\\ServerPipe", DIS.Types.PIPE_ACCESS_DUPLEX, DIS.Types.PIPE_TYPE_BYTE | DIS.Types.PIPE_WAIT, DIS.Types.PIPE_UNLIMITED_INSTANCES, 0, 1024, DIS.Types.NMPWAIT_WAIT_FOREVER, (uint)0);

            // вывод имени канала в заголовок формы, чтобы можно было его использовать для ввода имени в форме клиента, запущенного на другом вычислительном узле
            this.Text += "     " + PipeName;
            
            // создание потока, отвечающего за работу с каналом
            t = new Thread(ReceiveMessage);
            t.Start();
        }

        

        private void ReceiveMessage()
        {
            string reseviedMessage = "";            // прочитанное сообщение
            uint realBytesReaded = 0;   // количество реально прочитанных из канала байтов

            // входим в бесконечный цикл работы с каналом
            while (_continue)
            {
                if (DIS.Import.ConnectNamedPipe(PipeHandle, 0))
                {
                    byte[] buff = new byte[1024];                                           // буфер прочитанных из канала байтов
                    DIS.Import.FlushFileBuffers(PipeHandle);                                // "принудительная" запись данных, расположенные в буфере операционной системы, в файл именованного канала
                    DIS.Import.ReadFile(PipeHandle, buff, 1024, ref realBytesReaded, 0);    // считываем последовательность байтов из канала в буфер buff
                    reseviedMessage = Encoding.Unicode.GetString(buff);                                 // выполняем преобразование байтов в последовательность символов
                    BObjects.ClientRequest request;
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(BObjects.ClientRequest));
                    using (StringReader sr = new StringReader(Helpers.ClearString(reseviedMessage)))
                    {
                        request = (BObjects.ClientRequest)xmlSerializer.Deserialize(sr);
                    }
                    BObjects.ServerMessage resultMessage = null;
                    if (request is BObjects.LogInRequest lir)
                    {
                        if (Nicknames.ContainsKey(lir.nickName))
                            resultMessage = new BObjects.FailedLoginResult { Message = "Такое имя уже занято" };
                        else
                        {
                            resultMessage = new BObjects.SuccessfulLoginResult();
                            Nicknames.Add(lir.nickName, lir.nodeName);
                        }
                        GivePrivateMessage(resultMessage, lir.nodeName, lir.nickName);
                        if (resultMessage is BObjects.SuccessfulLoginResult)
                            resultMessage = new BObjects.NewUserMessage { Nickname = lir.nickName };
                        else
                            resultMessage = null;
                    }else if (request is BObjects.LogOutRequest lor)
                    {
                        Nicknames.Remove(lor.nickName);
                        resultMessage = new BObjects.QuitUserMessage { Nickname = lor.nickName };
                    }else if (request is BObjects.MessageRequest mr)
                    {
                        resultMessage = new BObjects.UserMessage { Nickname = mr.nickName, Message = mr.Message };
                    }
                    

                    GiveMessage(resultMessage);

                    DIS.Import.DisconnectNamedPipe(PipeHandle);                             // отключаемся от канала клиента 
                    Thread.Sleep(500);                                                      // приостанавливаем работу потока перед тем, как приcтупить к обслуживанию очередного клиента
                }
            }
        }
        private void GivePrivateMessage(BObjects.ServerMessage msg, string nodeName, string nickname)
        {
            uint BytesWritten = 0;
            string xml;
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(BObjects.ServerMessage));

            using (var sw = new StringWriter())
            {
                xmlSerializer.Serialize(sw, msg);
                xml = sw.ToString();
            }
            byte[] buff = Encoding.Unicode.GetBytes(xml);    // выполняем преобразование сообщения (вместе с идентификатором машины) в последовательность байт

            // открываем именованный канал, имя которого указано в поле tbPipe
            var PipeHandleO = DIS.Import.CreateFile(Helpers.ClientPipeName(nodeName, nickname), DIS.Types.EFileAccess.GenericWrite, DIS.Types.EFileShare.Read, 0, DIS.Types.ECreationDisposition.OpenExisting, 0, 0);
            DIS.Import.WriteFile(PipeHandleO, buff, Convert.ToUInt32(buff.Length), ref BytesWritten, 0);         // выполняем запись последовательности байт в канал
            DIS.Import.CloseHandle(PipeHandleO);
        }

        private void GiveMessage(BObjects.ServerMessage msg)
        {
            if (msg == null) return;
            rtbMessages.Invoke((MethodInvoker)delegate
            {
                  rtbMessages.Text += "\n" + Helpers.DisplayMessage(msg);                             // выводим полученное сообщение на форму
            });

            foreach(var pair in Nicknames)
            {
                GivePrivateMessage(msg, pair.Value, pair.Key);
            }
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            _continue = false;      // сообщаем, что работа с каналом завершена

            if (t != null)
                t.Abort();          // завершаем поток
            
            if (PipeHandle != -1)
                DIS.Import.CloseHandle(PipeHandle);     // закрываем дескриптор канала
        }
    }
}