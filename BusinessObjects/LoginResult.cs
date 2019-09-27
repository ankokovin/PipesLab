using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessObjects
{
    abstract class LoginResult
    {
    }

    class FailedLoginResult : LoginResult
    {
        string Message;
    }

    class SuccessfulLoginResult : LoginResult
    {
       
    }
}
