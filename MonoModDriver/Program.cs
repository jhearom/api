using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace MonoModDriver
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("usage: MonoModDriver <monomod-exe> <input> <output> [mod-dir] [dep-dir...]");
                return 2;
            }

            string monoModPath = Path.GetFullPath(args[0]);
            string monoModDir = Path.GetDirectoryName(monoModPath) ?? Directory.GetCurrentDirectory();
            string inputPath = args[1];
            string outputPath = args[2];
            string modDir = args.Length >= 4 ? args[3] : monoModDir;
            int depDirStart = args.Length >= 4 ? 4 : 3;

            AppDomain.CurrentDomain.AssemblyResolve += (_, eventArgs) =>
            {
                string assemblyName = new AssemblyName(eventArgs.Name).Name + ".dll";
                string candidate = Path.Combine(monoModDir, assemblyName);
                return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
            };

            Directory.SetCurrentDirectory(monoModDir);

            try
            {
                Assembly monoModAssembly = Assembly.LoadFrom(monoModPath);
                Type monoModderType = monoModAssembly.GetType("MonoMod.MonoModder", throwOnError: true);
                object monoModder = Activator.CreateInstance(monoModderType);

                monoModderType.GetField("InputPath").SetValue(monoModder, inputPath);
                monoModderType.GetField("OutputPath").SetValue(monoModder, outputPath);

                IList dependencyDirs = (IList)monoModderType.GetField("DependencyDirs").GetValue(monoModder);
                for (int i = depDirStart; i < args.Length; i++)
                {
                    string depDir = args[i];
                    if (!string.IsNullOrWhiteSpace(depDir) && Directory.Exists(depDir))
                    {
                        dependencyDirs.Add(depDir);
                    }
                }

                monoModderType.GetMethod("Read").Invoke(monoModder, null);
                monoModderType.GetMethod("ReadMod", new[] { typeof(string) }).Invoke(monoModder, new object[] { modDir });
                monoModderType.GetMethod("MapDependencies", Type.EmptyTypes).Invoke(monoModder, null);
                monoModderType.GetMethod("AutoPatch").Invoke(monoModder, null);
                monoModderType.GetMethod("Write", new[] { typeof(Stream), typeof(string) }).Invoke(monoModder, new object[] { null, null });

                (monoModder as IDisposable)?.Dispose();
                return 0;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                Console.WriteLine(ex.InnerException);
                return 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }
    }
}
