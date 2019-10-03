using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes
{
    public static class Helpers
    {
        public  static int GetSourceIdx(string Message)
        {
            return Message.IndexOf(">>") - 1;
        }


        public  static string ClearString(string input)
        {
            return input.Substring(0, input.IndexOf('\0'));
        }

        public static string ClientPipeName(string nodeName, string nickName)
        {
            return "\\\\" + nodeName + "\\pipe\\mychat@" + nodeName;
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
