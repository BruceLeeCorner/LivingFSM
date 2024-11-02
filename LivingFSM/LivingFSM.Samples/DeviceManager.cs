using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LivingFSM.Samples
{
    /// <summary>
    /// 脚本化设备的套路：每个实例都有一个id，通过反射实现字符串调用方法，但是缺点是
    /// 要约束类的方法签名形式。另外一种方法是手动实现注册到(id,方法名，字符串格式的参数，返回值)。可以让类继承IScript.
    /// </summary>
    public class DeviceManager
    {
        public static DeviceManager Instance { get; private set; } = new DeviceManager();

        private DeviceManager()
        { }
     
        private Dictionary<string, IDevice> devices = new();
        private Dictionary<string, Timer> timers = new();

        public ITimerRuler TimerRuler { get; set; } = new DefaultTimerRuler();

        public IDevice GetDevice();

        public void Register(string module, string name, IDevice device)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(module, nameof(module));
            ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
            string key = module + "." + name;
            if (devices.ContainsKey(key))
            {
                throw new ArgumentException($"Device already exists with the same key({module}.{name}).");
            }
            devices[key] = device;
        }

        public void Initialize()
        {
            foreach (var device in devices)
            {
                device.Value.Initialize();
            }


        }

        private void Monitor()
        {

        }

        public object Invoke(string station,string module,string name,string memberName)
        {
            IDevice device = GetDevice();
            var member = device.GetType().GetMethod(memberName);
        }

        public void Reset()
        {

        }
    }

    public interface ITimerRuler
    {
        string Group(string module, string name);
    }

    public class DefaultTimerRuler : ITimerRuler
    {
        public string Group(string module, string name)
        {
            return module;
        }
    }

}