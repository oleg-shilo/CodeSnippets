using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Reflection;
using Newtonsoft.Json;
using static System.Reflection.BindingFlags;

//  <package id="Mono.Reflection" version="1.1.0.0" targetFramework="net472" />

public static class StaticAnalyzer
{
    public static string FullName(this MethodInfo method)
        => $"{method.DeclaringType.FullName}.{method.Name}";

    public static void VisitMethodCalls(this Assembly asm, Func<MethodInfo, MethodInfo, bool> visit)
    {
        var methods = asm.GetTypes()
                         .SelectMany(x => x.GetMethods(Public | NonPublic | Instance | Static)
                                           .Where(y => y.DeclaringType.Assembly == asm));

        foreach (MethodInfo caller in methods)
            foreach (Instruction instruction in caller.GetInstructions())
            {
                var calledMethod = instruction.Operand as MethodInfo;

                if (calledMethod != null)
                {
                    if (!visit(caller, calledMethod))
                        break;
                }
            }
    }

    public static IEnumerable<MethodInfo> GetUnReferencedMethods(this Assembly asm)
    {
        var methods = asm.GetTypes()
                         .SelectMany(x => x.GetMethods(Public | NonPublic | Instance | Static)
                                           .Where(y => y.DeclaringType.Assembly == asm));
        var notCalled = methods.ToList();

        foreach (MethodInfo caller in methods)
        {
            if (notCalled.Any() == false)
                break;

            foreach (Instruction instruction in caller.GetInstructions())
            {
                var calledMethod = instruction.Operand as MethodInfo;

                if (calledMethod != null)
                {
                    if (notCalled.Contains(calledMethod))
                        notCalled.Remove(calledMethod);
                }
            }
        }

        return notCalled;
    }
}

namespace ConsoleApp
{
    class Program
    {
        static void Main3(string[] args)
        {
        }

        static void Main2()
        {
        }

        static void Main(string[] args)
        {
            Main2();

            var json = JsonConvert.SerializeObject(123);

            // find all calls `JsonConvert.SerializeObject`
            Assembly.GetExecutingAssembly().VisitMethodCalls(
                (caller, called) =>
                {
                    if (called.FullName() == "Newtonsoft.Json.JsonConvert.SerializeObject")
                    {
                        Console.WriteLine($"{caller.FullName()} -> {called.FullName()}");
                    }
                    return true;
                });

            // find all methods from this assembly that are never called.
            var deadMethods = Assembly.GetExecutingAssembly().GetNonReferencedMethods();
        }
    }
}
