using System;
using System.Reflection;
using Fsp;

namespace ProbeWinFsp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Probing Fsp assembly...");
            var assembly = typeof(FileSystemBase).Assembly;
            foreach (var type in assembly.GetExportedTypes())
            {
                // Console.WriteLine($"Type: {type.FullName}");
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                {
                    bool interesting = method.Name.Contains("Dir") || method.Name.Contains("Fill");
                    
                    if (!interesting)
                    {
                        foreach (var param in method.GetParameters())
                        {
                            if (param.ParameterType == typeof(IntPtr) && param.Name.Contains("Buffer"))
                            {
                                interesting = true;
                                break;
                            }
                        }
                    }

                    if (interesting)
                    {
                        Console.WriteLine($"{type.Name}.{method.Name} ({(method.IsStatic ? "Static" : "Instance")}) -> {method.ReturnType.Name}");
                        foreach (var param in method.GetParameters())
                        {
                            Console.WriteLine($"  {param.ParameterType} {param.Name}");
                        }
                        Console.WriteLine();
                    }
                }
            }
        }
    }
}
