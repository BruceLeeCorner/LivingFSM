using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LivingObject
{
    public interface IModular
    {
        string Module { get; }
        string Group { get; }
        string Name { get; }
        string Id { get; }
    }
}