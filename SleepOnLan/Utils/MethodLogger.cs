using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SleepOnLan
{
    public static class MethodBaseExtension
    {
        public static string GetMethodContextName(this MethodBase method)
        {
            try
            {
                if (method.DeclaringType.GetInterfaces().Any(i => i == typeof(IAsyncStateMachine)))
                {
                    var generatedType = method.DeclaringType;
                    var originalType = generatedType.DeclaringType;
                    var foundMethod = originalType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                        .Single(m => m.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType == generatedType);
                    return foundMethod.DeclaringType.Name + "." + foundMethod.Name.Replace(".ctor", foundMethod.DeclaringType.Name);
                }
            }
            catch
            {
            }
            return method.DeclaringType.Name + "." + method.Name.Replace(".ctor", method.DeclaringType.Name);
        }
    }

    public class MethodLogger : IDisposable
    {
        private readonly ILogger _logger;
        private string MethodName { get; set; }

        public MethodLogger(ILogger logger)
        {
            _logger = logger;
            MethodName = new StackFrame(1).GetMethod().GetMethodContextName();
            _logger.LogTrace("--> Entering {MethodName}", MethodName);
        }

        public void Dispose()
        {
            _logger.LogTrace("<-- Leaving {MethodName}", MethodName);
            GC.SuppressFinalize(this);
        }
    }
}

