using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.BObjects
{
    [Serializable]
    public abstract class LoginResult : ServerMessage
    {
    }

    [Serializable]
    public class FailedLoginResult : LoginResult
    {
        public string Message;
    }

    [Serializable]
    public class SuccessfulLoginResult : LoginResult
    {
       
    }
}
