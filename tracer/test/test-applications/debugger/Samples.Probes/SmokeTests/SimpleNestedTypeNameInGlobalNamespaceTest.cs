using System.Runtime.CompilerServices;
using Samples.Probes;
using Samples.Probes.Shared;
using Samples.Probes.SmokeTests;

public class SimpleNestedTypeNameInGlobalNamespaceTest : IRun
{
    public void Run()
    {
        new NestedType().MethodToInstrument(nameof(Run));
    }

    class NestedType
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [MethodProbeTestData("System.Void", new[] { "System.String" }, useFullTypeName: false)]
        public void MethodToInstrument(string callerName)
        {
            var arr = new[] { callerName, nameof(MethodToInstrument), nameof(SimpleTypeNameTest) };
            if (NoOp(arr).Length == arr.Length)
            {
                throw new IntentionalDebuggerException("Same length.");
            }
        }

        [MethodImpl(MethodImplOptions.NoOptimization)]
        string[] NoOp(string[] arr) => arr;
    }
}
