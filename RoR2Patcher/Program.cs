using System;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace RoR2DedicatedPatcher
{
    class Program
    {
        static int Main(string[] args)
        {
            string inputDll = "Risk of Rain 2/Risk of Rain 2_Data/Managed/RoR2.dll";
            string outputDll = "RoR2_Patched.dll";
            
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--input":
                    case "-i":
                        if (i + 1 < args.Length)
                            inputDll = args[++i];
                        break;
                    case "--output":
                    case "-o":
                        if (i + 1 < args.Length)
                            outputDll = args[++i];
                        break;
                }
            }
            
            if (!File.Exists(inputDll))
            {
                Console.WriteLine($"ERROR Input file not found: {inputDll}");
                return 1;
            }
            
            try
            {
                Console.WriteLine($"Loading assembly: {inputDll}");
                
                var resolver = new DefaultAssemblyResolver();
                var inputDir = Path.GetDirectoryName(Path.GetFullPath(inputDll));
                if (inputDir != null)
                {
                    resolver.AddSearchDirectory(inputDir);
                }
                
                var readerParams = new ReaderParameters
                {
                    AssemblyResolver = resolver,
                    ReadWrite = false
                };
                
                var assembly = AssemblyDefinition.ReadAssembly(inputDll, readerParams);
                Console.WriteLine($"Loaded assembly: {assembly.Name.Name}");
                
                // Apply patches
                PatchDedicatedServer(assembly);
                PatchAppId(assembly);
                PatchLoadStartupConfigs(assembly);
                PatchSteamworksLobbyManager(assembly);
                
                Console.WriteLine($"Saving to: {outputDll}");
                assembly.Write(outputDll);
                Console.WriteLine($"Patching complete. Output: {outputDll}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR Failed to patch: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
            
            return 0;
        }
        
        static void PatchDedicatedServer(AssemblyDefinition assembly)
        {
            Console.WriteLine("Patching DedicatedServer flag");
            
            var serverManager = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "SteamworksServerManager") ??
                                assembly.MainModule.Types.SelectMany(t => t.NestedTypes).FirstOrDefault(t => t.Name == "SteamworksServerManager");
            
            int patchCount = 0;
            foreach (var method in serverManager.Methods.Where(m => m.HasBody))
            {
                var instructions = method.Body.Instructions;
                for (int i = 0; i < instructions.Count - 1; i++)
                {
                    if (instructions[i].OpCode == OpCodes.Ldc_I4_0 &&
                        instructions[i + 1].OpCode == OpCodes.Callvirt &&
                        instructions[i + 1].Operand?.ToString().Contains("set_DedicatedServer") == true)
                    {
                        var processor = method.Body.GetILProcessor();
                        processor.Replace(instructions[i], processor.Create(OpCodes.Ldc_I4_1));
                        patchCount++;
                    }
                }
            }
            
            Console.WriteLine($"Patched {patchCount} DedicatedServer reference(s)");
        }
        
        static void PatchAppId(AssemblyDefinition assembly)
        {
            Console.WriteLine("Patching AppID references...");
            
            const uint CLIENT_APP_ID = 632360;
            const uint SERVER_APP_ID = 1180760;
            int patchCount = 0;
            
            // Patch RoR2Application.appId field
            var appIdField = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "RoR2Application")
                            ?.Fields.FirstOrDefault(f => f.Name == "appId");
            if (appIdField?.HasConstant == true && appIdField.Constant.Equals(CLIENT_APP_ID))
            {
                appIdField.Constant = SERVER_APP_ID;
                patchCount++;
            }
            
            // Patch AppID references in methods (skip SteamworksServerManager constructor)
            foreach (var type in assembly.MainModule.Types)
            {
                foreach (var method in type.Methods.Where(m => m.HasBody))
                {
                    if (type.Name == "SteamworksServerManager" && method.IsConstructor) continue;
                    
                    foreach (var inst in method.Body.Instructions)
                    {
                        if (inst.OpCode == OpCodes.Ldc_I4 && inst.Operand.Equals((int)CLIENT_APP_ID))
                        {
                            inst.Operand = (int)SERVER_APP_ID;
                            patchCount++;
                        }
                    }
                }
            }
            
            Console.WriteLine($"Patched {patchCount} AppID reference(s)");
        }
        
        static void PatchLoadStartupConfigs(AssemblyDefinition assembly)
        {
            Console.WriteLine("Patching RunConfigsAfterLoad for server auto-start...");
            
            var consoleType = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "Console");
            var runConfigsMethod = consoleType?.Methods.FirstOrDefault(m => m.Name == "RunConfigsAfterLoad");
            var submitCmdMethod = consoleType?.Methods.FirstOrDefault(m => m.Name == "SubmitCmd");
            var handlerAttrType = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "NetworkMessageHandlerAttribute");
            var collectHandlersMethod = handlerAttrType?.Methods.FirstOrDefault(m => m.Name == "CollectHandlers");
            
            var instructions = runConfigsMethod.Body.Instructions;
            var processor = runConfigsMethod.Body.GetILProcessor();
            
            int insertPoint = -1;
            for (int i = 0; i < instructions.Count; i++)
            {
                if (instructions[i].Operand?.ToString().Contains("exec autoexec") == true)
                {
                    for (int j = i; j < instructions.Count; j++)
                    {
                        if (instructions[j].Operand?.ToString().Contains("SubmitCmd") == true)
                        {
                            insertPoint = j + 1;
                            break;
                        }
                    }
                    break;
                }
            }
            
            // Insert CollectHandlers call
            var callCollectHandlers = new[]
            {
                processor.Create(OpCodes.Call, collectHandlersMethod)
            };
            
            foreach (var inst in callCollectHandlers.Reverse())
            {
                processor.InsertBefore(instructions[insertPoint], inst);
            }
            
            // Insert exec server_startup
            var execServerStartup = new[]
            {
                processor.Create(OpCodes.Ldarg_0),
                processor.Create(OpCodes.Ldnull),
                processor.Create(OpCodes.Ldstr, "exec server_startup"),
                processor.Create(OpCodes.Ldc_I4_0),
                processor.Create(OpCodes.Call, submitCmdMethod)
            };
            
            foreach (var inst in execServerStartup.Reverse())
            {
                processor.InsertBefore(instructions[insertPoint], inst);
            }
            
            Console.WriteLine("Patched RunConfigsAfterLoad");
        }
        
        static void PatchSteamworksLobbyManager(AssemblyDefinition assembly)
        {
            Console.WriteLine("Patching SteamworksLobbyManager.Init() for lobby behavior...");
            
            var steamworksLobbyManagerType = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "SteamworksLobbyManager");
            var initMethod = steamworksLobbyManagerType?.Methods.FirstOrDefault(m => m.Name == "Init");
            var lobbyManagerType = assembly.MainModule.Types.FirstOrDefault(t => t.Name == "LobbyManager");
            var lobbyDataType = lobbyManagerType?.NestedTypes.FirstOrDefault(t => t.Name == "LobbyData");
            var parameterlessConstructor = lobbyDataType?.Methods.FirstOrDefault(m => 
                m.IsConstructor && m.Parameters.Count == 0);
            
            var instructions = initMethod.Body.Instructions;
            
            for (int i = 0; i < instructions.Count - 3; i++)
            {
                var stlocInst = instructions[i];
                var ldargInst = instructions[i + 1];
                var ldlocInst = instructions[i + 2];
                var newobjInst = instructions[i + 3];
                
                if (stlocInst.OpCode == OpCodes.Stloc_0 &&
                    ldargInst.OpCode == OpCodes.Ldarg_0 &&
                    ldlocInst.OpCode == OpCodes.Ldloc_0 && 
                    newobjInst.OpCode == OpCodes.Newobj && 
                    newobjInst.Operand?.ToString().Contains("LobbyData") == true &&
                    newobjInst.Operand.ToString().Contains("LobbyDataSetupState"))
                {
                    var processor = initMethod.Body.GetILProcessor();
                    var importedConstructor = assembly.MainModule.ImportReference(parameterlessConstructor);
                    
                    processor.Remove(stlocInst);
                    processor.Remove(ldlocInst);
                    newobjInst.Operand = importedConstructor;
                    
                    Console.WriteLine("Patched SteamworksLobbyManager.Init()");
                    break;
                }
            }
        }
    }
}