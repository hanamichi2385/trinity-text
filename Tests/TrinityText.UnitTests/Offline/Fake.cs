using System;
using System.Reflection;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>Minimal interface stub (no mocking library): the handler answers every call.</summary>
    public class Fake : DispatchProxy
    {
        private Func<MethodInfo, object[], object> _handler;

        public static T Of<T>(Func<MethodInfo, object[], object> handler) where T : class
        {
            var proxy = Create<T, Fake>();
            ((Fake)(object)proxy)._handler = handler;
            return proxy;
        }

        protected override object Invoke(MethodInfo targetMethod, object[] args) => _handler(targetMethod, args);
    }
}
