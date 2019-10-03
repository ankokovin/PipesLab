using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.BObjects
{
    [Serializable]
    abstract class ServerMessage
    {
    }
    [Serializable]
    class NewUserMessage : ServerMessage
    {

        public string Nickname;
    }
    [Serializable]
    class QuitUserMessage : ServerMessage
    {

        public string Nickname;
    }
    [Serializable]
    class UserMessage : ServerMessage
    {

        public string Nickname;
        public string Message;
    }
    [Serializable]
    class ShutDownMessage : ServerMessage
    {

    }
}
