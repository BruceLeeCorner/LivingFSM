﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LivingObject
{
    public interface IDevice
    {
        void Initialize();
        void Terminate();
        void Monitor();
        void Reset();
    }
}
