using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.BObjects
{
    [Serializable]
    public abstract class ServerMessage
    {
    }
    [Serializable]
    public class NewUserMessage : ServerMessage
    {

        public string Nickname;
    }
    [Serializable]
    public class QuitUserMessage : ServerMessage
    {

        public string Nickname;
    }
    [Serializable]
    public class UserMessage : ServerMessage
    {

        public string Nickname;
        public string Message;
    }
    [Serializable]
    public class ShutDownMessage : ServerMessage
    {

    }
}
