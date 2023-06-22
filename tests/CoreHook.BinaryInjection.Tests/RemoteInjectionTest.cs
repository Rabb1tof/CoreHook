using System.Diagnostics;
using System.Reflection;

namespace CoreHook.BinaryInjection.Tests;

[Collection("Remote Injection Tests")]
public class RemoteInjectionTestSimpleParameter
{
    private const string TestModuleDir = "Test";

    [Theory]
    [InlineData("System32")]
    [InlineData("SysWOW64")]
    private void TestRemotePluginSimpleParameter(string applicationFolder)
    {
        const string testHookLibrary = "CoreHook.Tests.SimpleParameterTest.dll";
        const string remoteArgument = "Berner";

        var testProcess = StartProcess(Path.Combine(Environment.ExpandEnvironmentVariables("%Windir%"), applicationFolder, "notepad.exe"));

        Thread.Sleep(500);

        InjectDllIntoTarget(testProcess, GetTestDllPath(testHookLibrary), PipePlatformBase.GetUniquePipeName(), remoteArgument);

        Assert.Equal(remoteArgument, ReadFromProcess(testProcess));

        EndProcess(testProcess);
    }
    //}

    //[Collection("Remote Injection Tests")]
    //public class RemoteInjectionTestComplexParameter
    //{
    [Theory]
    [InlineData("System32")]
    [InlineData("SysWOW64")]
    private void TestRemotePluginComplexParameter(string applicationFolder)
    {
        const string testHookLibrary = "CoreHook.Tests.ComplexParameterTest.dll";
        const string testMessageParameter = "Berner";

        var complexParameter = new ComplexParameter
        {
            Message = testMessageParameter,
            HostProcessId = Environment.ProcessId
        };

        var testProcess = StartProcess(Path.Combine(Environment.ExpandEnvironmentVariables("%Windir%"), applicationFolder, "notepad.exe"));

        Thread.Sleep(500);

        InjectDllIntoTarget(testProcess, GetTestDllPath(testHookLibrary), PipePlatformBase.GetUniquePipeName(), complexParameter);

        Assert.Equal(complexParameter.Message, ReadFromProcess(testProcess));
        Assert.Equal(complexParameter.HostProcessId.ToString(), ReadFromProcess(testProcess));

        EndProcess(testProcess);
    }

    internal static string GetTestDllPath(string dllName)
    {
        return Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), TestModuleDir, dllName);
    }


    internal static void InjectDllIntoTarget(Process target, string injectionLibrary, string injectionPipeName, params object[] remoteArguments)
    {
        //var (coreRootPath, coreLoadLibrary, _, _, _) = ModulesPathHelper.GetCoreLoadPaths(false);
        //var logger = Mock.Of<ILogger>();

        //using var injector = new RemoteInjector(target.Id, null, injectionPipeName, logger);
        //injector.Inject(injectionLibrary, "", new Managed.NetHostStartArguments(coreLoadLibrary, coreRootPath, false, null));
    }


    internal static Process StartProcess(string fileName)
    {
        var testProcess = new Process
        {
            StartInfo =
                {
                    FileName = fileName,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                }
        };

        testProcess.Start();

        return testProcess;
    }

    internal static void EndProcess(Process process)
    {
        process?.Kill();
        process = null;
    }

    internal static string ReadFromProcess(Process target)
    {
        return target.StandardOutput.ReadLine();
    }

}
