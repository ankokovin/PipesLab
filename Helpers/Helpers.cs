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
            return nickName + "@" + nodeName;
        }
    }
}
