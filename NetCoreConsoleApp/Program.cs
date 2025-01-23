using System.Reflection;

AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
{
    var assemblyName = new AssemblyName(args.Name).Name;
    var assemblyPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{assemblyName}.dll");
    return File.Exists(assemblyPath) ? Assembly.LoadFrom(assemblyPath) : null;
};

Assembly.Load("NormalClrAssembly");
Assembly.Load("Microsoft.Data.SqlClient.SNI");
