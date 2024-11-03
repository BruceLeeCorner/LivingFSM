using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LivingObject
{
    public class ObjectWapper : IObjectWrapper
    {
        private readonly StrongReferenceMessenger messenger;
        private string id;

        public ObjectWapper(string module, string group, string name)
        {
            Module = module;
            Group = group;
            Name = name;
            messenger = new StrongReferenceMessenger();
        }

        public string Group { get; }
        public string Id => string.IsNullOrWhiteSpace(Id) ? (this.id = $"{Module}.{Group}.{Name}") : this.id;
        public string Module { get; }
        public string Name { get; }

        public void Register(string id, string JsonParamArray)
        {
            throw new NotImplementedException();
        }

        public void DO(string id, string methodName, string JsonParamArray)
        {
            throw new NotImplementedException();
        }

        public void DO(string id, string methodName,params object[] args)
        {

            if (!args.All(item =>
            {
                var typeInfo = item.GetType().GetTypeInfo();
                return typeInfo.IsPrimitive || typeInfo.IsEnum || typeInfo.Equals(typeof(string).GetTypeInfo());
            }))
            {
                throw new ArgumentException();
            }
            messenger.Send(args, id + "." + methodName);
        }



        public void Sub(string id, string methodName, Delegate @delegate)
        {
            string key = id + "." + methodName;
            messenger.Register<ObjectWapper, object[], string>(this, key, (a, b) =>
            {
                @delegate.DynamicInvoke(b);
            });
        }
    }
}