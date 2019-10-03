using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.BObjects
{
    [Serializable]
    abstract class LoginResult
    {
    }



    [Serializable]
    class FailedLoginResult : LoginResult
    {
        public string Message;
    }

    [Serializable]
    class SuccessfulLoginResult : LoginResult
    {
       
    }
}
