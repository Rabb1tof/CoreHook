using System;
using System.Threading;

namespace CoreHook.BinaryInjection;

internal class InjectionState
{
    // TODO: use a SemaforeSlim instead?
    public readonly Mutex ThreadLock = new Mutex(false);
    public readonly ManualResetEvent Completion = new ManualResetEvent(false);
    public Exception? Error;
}
