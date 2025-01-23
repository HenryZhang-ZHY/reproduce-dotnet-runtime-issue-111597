# Reproduce steps

1. Clone the repository, change the working directory to the repository root

2. Run the following command to publish the NetFramework Application
```powershell
dotnet publish .\NetFrameworkConsoleApp\NetFrameworkConsoleApp.csproj --runtime win-x64 --no-self-contained --configuration Release /p:EnvironmentName=Production
```

3. Execute the published application
```powershell
.\NetFrameworkConsoleApp\bin\Release\net48\win-x64\publish\NetFrameworkConsoleApp.exe
```

You can see the output is
```powershell
Unhandled Exception: System.BadImageFormatException: Could not load file or assembly 'Microsoft.Data.SqlClient.SNI.x64' or one of its dependencies. The module was expected to contain an assembly manifest.
   at System.Reflection.RuntimeAssembly._nLoad(AssemblyName fileName, String codeBase, Evidence assemblySecurity, RuntimeAssembly locationHint, StackCrawlMark& stackMark, IntPtr pPrivHostBinder, Boolean throwOnFileNotFound, Boolean forIntrospection, Boolean suppressSecurityChecks)
   at System.Reflection.RuntimeAssembly.InternalLoadAssemblyName(AssemblyName assemblyRef, Evidence assemblySecurity, RuntimeAssembly reqAssembly, StackCrawlMark& stackMark, IntPtr pPrivHostBinder, Boolean throwOnFileNotFound, Boolean forIntrospection, Boolean suppressSecurityChecks)
   at System.Reflection.RuntimeAssembly.InternalLoad(String assemblyString, Evidence assemblySecurity, StackCrawlMark& stackMark, IntPtr pPrivHostBinder, Boolean forIntrospection)
   at System.Reflection.RuntimeAssembly.InternalLoad(String assemblyString, Evidence assemblySecurity, StackCrawlMark& stackMark, Boolean forIntrospection)
   at System.Reflection.Assembly.Load(String assemblyString)
   at NetFrameworkConsoleApp.Program.Main(String[] args)
```

4. Run the following command to publish the NetCore Application
```powershell
dotnet publish .\NetCoreConsoleApp\NetCoreConsoleApp.csproj --runtime win-x64 --no-self-contained --configuration Release /p:EnvironmentName=Production
```

5. Execute the published application
```powershell
.\NetCoreConsoleApp\bin\Release\net8.0\win-x64\publish\NetCoreConsoleApp.exe
```

You can see the output is
```powershell
Unhandled exception. System.IO.FileNotFoundException: Could not load file or assembly 'Microsoft.Data.SqlClient.SNI, Culture=neutral, PublicKeyToken=null'. The system cannot find the file specified.
File name: 'Microsoft.Data.SqlClient.SNI, Culture=neutral, PublicKeyToken=null'
   at System.Reflection.RuntimeAssembly.InternalLoad(AssemblyName assemblyName, StackCrawlMark& stackMark, AssemblyLoadContext assemblyLoadContext, RuntimeAssembly requestingAssembly, Boolean throwOnFileNotFound)
   at System.Reflection.Assembly.Load(String assemblyString)
   at Program.<Main>$(String[] args)
```

Use the following command to check whether the `Microsoft.Data.SqlClient.SNI` assembly is present in the published folder
```powershell
Test-Path .\NetCoreConsoleApp\bin\Release\net8.0\win-x64\publish\Microsoft.Data.SqlClient.SNI.dll
```

# Reason

1. `Assembly.Load` is a `load-by-name` API, the AssemblyLoadContext only execute the [managed assembly default probing logic](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/default-probing#managed-assembly-default-probing) for it. Since the `Microsoft.Data.SqlClient.SNI` assembly is a native assembly, it is not listed in `TRUSTED_PLATFORM_ASSEMBLIES`, that's why we get the `System.IO.FileNotFoundException` exception.
reference: https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/loading-managed#algorithm

2. To make it works like the .NET Framework application, we need to handle the AssemblyResolve event to probe the assembly by ourselves, like this:
```csharp
AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
{
    var assemblyName = new AssemblyName(args.Name).Name;
    var assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyName}.dll");
    return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
};
```

Then we can get the same exception as the .NET Framework application.
```powershell
Unhandled exception. System.BadImageFormatException: Could not load file or assembly 'Microsoft.Data.SqlClient.SNI, Culture=neutral, PublicKeyToken=null'. An attempt was made to load a program with an incorrect format.
File name: 'Microsoft.Data.SqlClient.SNI, Culture=neutral, PublicKeyToken=null' ---> System.BadImageFormatException: Bad IL format. The format of the file 'D:\git\oss\reproduce-dotnet-runtime-issue-111597\NetCoreConsoleApp\bin\Release\net8.0\win-x64\publish\Microsoft.Data.SqlClient.SNI.dll' is invalid.
   at System.Runtime.Loader.AssemblyLoadContext.LoadFromAssemblyPath(String assemblyPath)
   at System.Reflection.Assembly.LoadFrom(String assemblyFile)
   at Program.<>c.<<Main>$>b__0_0(Object _, ResolveEventArgs args) in D:\git\oss\reproduce-dotnet-runtime-issue-111597\NetCoreConsoleApp\Program.cs:line 7
   at System.Runtime.Loader.AssemblyLoadContext.InvokeResolveEvent(ResolveEventHandler eventHandler, RuntimeAssembly assembly, String name)
   at System.Reflection.RuntimeAssembly.InternalLoad(AssemblyName assemblyName, StackCrawlMark& stackMark, AssemblyLoadContext assemblyLoadContext, RuntimeAssembly requestingAssembly, Boolean throwOnFileNotFound)
   at System.Reflection.Assembly.Load(String assemblyString)
   at Program.<Main>$(String[] args)
```
