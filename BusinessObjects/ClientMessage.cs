using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.BObjects
{
    [Serializable]
    public abstract class ClientRequest
    {
        public string nodeName;
        public string nickName;
        public string salt;
    }
    [Serializable]
    public class LogInRequest : ClientRequest
    {
    }
    [Serializable]
    public class LogOutRequest : ClientRequest
    {
    }

    [Serializable]
    public class MessageRequest : ClientRequest
    {
        public string Message;
    }
}
