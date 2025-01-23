using System.Reflection;

namespace NetFrameworkConsoleApp;

public static class Program
{
    public static void Main(string[] args)
    {
        Assembly.Load("NormalClrAssembly");
        Assembly.Load("Microsoft.Data.SqlClient.SNI.x64");
    }
}