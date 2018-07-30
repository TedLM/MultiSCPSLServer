using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiSCPSLServer
{
    abstract class ServerHookCallback : MarshalByRefObject
    {
        public abstract string OnCreateFile(string fileName);

        public abstract string OnFindFirstFile(string fileName);
    }
}
