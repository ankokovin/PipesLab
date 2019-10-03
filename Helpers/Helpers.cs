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

        public static string ClientPipeName(string nodeName, string nickName, bool local)
        {
            string pipename = Convert.ToBase64String(sha.ComputeHash(Encoding.Unicode.GetBytes(nodeName + "@" + nickName)));
            return "\\\\" + (local?".":nodeName) + "\\pipe\\"+ pipename;
        }

        public static string DisplayMessage(BObjects.ServerMessage msg)
        {
            if (msg is BObjects.UserMessage um)
            {
                return um.Nickname + " >> " + um.Message;
            }
            if (msg is BObjects.NewUserMessage nu)
            {
                return "Пользователь " + nu.Nickname + " вошёл в чат";
            }
            if (msg is BObjects.QuitUserMessage qu)
            {
                return "Пользователь " + qu.Nickname + " вышел из чата";
            }
            if (msg is BObjects.ShutDownMessage)
            {
                return "Сервер закрывается";
            }
            return string.Empty;
        }
    }
}
