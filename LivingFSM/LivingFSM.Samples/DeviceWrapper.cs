using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LivingFSM.Samples
{
    public class DeviceWrapper
    {
        private readonly Device device;

        public DeviceWrapper(Device device)
        {
            this.device = device;
        }
    }
}