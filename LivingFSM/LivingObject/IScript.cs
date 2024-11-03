using LivingObject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace LivingObject
{
    public interface IScript : IModular
    {
        void Register(string id, string methodName, Delegate @delegate);
        void Register(string module, string group, string name, string methodName, Delegate @delegate);
        void Execute(string id, string methodName, params object[] args);
        void Execute(string module, string group, string name, string methodName, params object[] args);

    }
}
