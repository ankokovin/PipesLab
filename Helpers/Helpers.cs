using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Pipes
{
    public static class Helpers
    {
        public static SHA1 sha = new SHA1CryptoServiceProvider();

        public static string ClientPipeName(string nodeName, string nickName,string salt, bool local)
        {
            string pipename = Convert.ToBase64String(sha.ComputeHash(Encoding.Unicode.GetBytes(nodeName + nickName + salt)));
            return "\\\\" + (local?".":nodeName) + "\\pipe\\"+ pipename;
        }

        public static string DisplayMessage(BObjects.ServerMessage msg)
        {
            if (msg is BObjects.UserMessage)
            {
                var um = msg as BObjects.UserMessage;
                return um.Nickname + " >> " + um.Message;
            }
            if (msg is BObjects.NewUserMessage)
            {
                var nu = msg as BObjects.NewUserMessage;
                return "Пользователь " + nu.Nickname + " вошёл в чат";
            }
            if (msg is BObjects.QuitUserMessage)
            {
                var qu = msg as BObjects.QuitUserMessage;
                return "Пользователь " + qu.Nickname + " вышел из чата";
            }
            if (msg is BObjects.ShutDownMessage)
            {
                return "Сервер закрывается";
            }
            if (msg is BObjects.LogoutAcceptMessage)
            {
                return "Отсоединение";
            }
            return string.Empty;
        }
    }
}
