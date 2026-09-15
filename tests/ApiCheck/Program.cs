using Mono.Cecil;
using Mono.Cecil.Cil;
var game = args[0]; var plugin = args[1];
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.Combine(game, "valheim_Data/Managed")); resolver.AddSearchDirectory(Path.Combine(game, "BepInEx/core"));
resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
var parameters = new ReaderParameters { AssemblyResolver = resolver };
using var mod = ModuleDefinition.ReadModule(plugin, parameters);
using var api = ModuleDefinition.ReadModule(Path.Combine(game, "valheim_Data/Managed/assembly_valheim.dll"), parameters);
int failures = 0, members = 0, hooks = 0, reflected = 0;
void Fail(string s) { Console.WriteLine("FAIL " + s); failures++; }
foreach (var reference in mod.GetMemberReferences())
{
    if (!(reference.DeclaringType.Namespace == "" || reference.DeclaringType.Namespace.StartsWith("UnityEngine") || reference.DeclaringType.Namespace.StartsWith("BepInEx") || reference.DeclaringType.Namespace.StartsWith("HarmonyLib") || reference.DeclaringType.Namespace == "TMPro")) continue;
    try { object resolved = reference is MethodReference method ? method.Resolve() : reference is FieldReference field ? field.Resolve() : null; if (resolved == null) Fail(reference.FullName); else members++; }
    catch (Exception e) { Fail(reference.FullName + " " + e.Message); }
}
foreach (var type in mod.Types)
foreach (var patch in type.Methods)
{
    var attribute = patch.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch" && a.ConstructorArguments.Count >= 2);
    if (attribute == null) continue;
    var targetType = (TypeReference)attribute.ConstructorArguments[0].Value;
    string targetName = (string)attribute.ConstructorArguments[1].Value;
    var candidates = targetType.Resolve().Methods.Where(m => m.Name == targetName).ToList();
    if (attribute.ConstructorArguments.Count >= 3 && attribute.ConstructorArguments[2].Value is CustomAttributeArgument[] arguments)
    {
        var names = arguments.Select(a => ((TypeReference)a.Value).FullName).ToArray();
        candidates = candidates.Where(m => m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(names)).ToList();
    }
    if (candidates.Count != 1) { Fail($"Hook {patch.Name}: {targetType.Name}.{targetName} matched {candidates.Count}"); continue; }
    var target = candidates[0]; hooks++;
    foreach (var p in patch.Parameters.Where(p => !p.Name.StartsWith("__")))
    {
        if (patch.CustomAttributes.Any(a => a.AttributeType.Name is "HarmonyTranspiler" or "HarmonyFinalizer")) continue;
        var tp = target.Parameters.FirstOrDefault(t => t.Name == p.Name);
        string actual = p.ParameterType is ByReferenceType b ? b.ElementType.FullName : p.ParameterType.FullName;
        if (tp == null || tp.ParameterType.FullName != actual) Fail($"Hook argument {patch.Name}.{p.Name} mismatches target {target.FullName}");
    }
    var result = patch.Parameters.FirstOrDefault(p => p.Name == "__result");
    if (result != null && ((ByReferenceType)result.ParameterType).ElementType.FullName != target.ReturnType.FullName) Fail("Result type " + patch.Name);
}
foreach (var entry in new (string type, string name, string returns, string[] args)[] {
    ("Container", "CheckAccess", "System.Boolean", new[]{"System.Int64"}),
    ("Inventory", "Changed", "System.Void", new[]{"System.Boolean", "System.Boolean"}),
    ("Smelter", "RPC_AddOre", "System.Void", new[]{"System.Int64", "System.String", "System.Boolean"}),
    ("Smelter", "RPC_AddFuel", "System.Void", new[]{"System.Int64"}),
    ("Fireplace", "RPC_AddFuel", "System.Void", new[]{"System.Int64"}),
    ("Fermenter", "RPC_AddItem", "System.Void", new[]{"System.Int64", "System.Int32", "System.Boolean"}),
    ("Fermenter", "RPC_Tap", "System.Void", new[]{"System.Int64"}),
    ("Fermenter", "UpdateCover", "System.Void", new[]{"System.Single", "System.Boolean"}),
    ("Fermenter", "GetFermentationTime", "System.Double", Array.Empty<string>()),
    ("CookingStation", "IsFireLit", "System.Boolean", Array.Empty<string>()),
    ("CookingStation", "RPC_AddItem", "System.Void", new[]{"System.Int64", "System.String", "System.Boolean"}),
    ("CookingStation", "RPC_RemoveDoneItem", "System.Void", new[]{"System.Int64", "UnityEngine.Vector3", "System.Int32"})
}) {
    var t = api.Types.Single(t => t.Name == entry.type);
    if (!t.Methods.Any(m => m.Name == entry.name && m.ReturnType.FullName == entry.returns && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(entry.args))) Fail("Reflection target " + entry.type + "." + entry.name); else reflected++;
}
foreach (var field in new[] { ("m_hasRoof", "System.Boolean"), ("m_exposed", "System.Boolean"), ("m_delayedTapItem", "System.Int32") })
    if (!api.Types.Single(t => t.Name == "Fermenter").Fields.Any(f => f.Name == field.Item1 && f.FieldType.FullName == field.Item2)) Fail("Fermenter reflection field " + field); else reflected++;
foreach (var name in new[] { "m_blockedSmoke", "m_haveRoof" })
    if (!api.Types.Single(t => t.Name == "Smelter").Fields.Any(f => f.Name == name && f.FieldType.FullName == "System.Boolean")) Fail("Smelter reflection field " + name); else reflected++;
foreach (var name in new[] { "HaveRequirementItems", "HaveRequirements", "SetupRequirement" })
{
    var t = api.Types.Single(t => t.Name == (name == "SetupRequirement" ? "InventoryGui" : "Player"));
    var methods = t.Methods.Where(m => m.Name == name && m.HasBody);
    if (name == "HaveRequirements") methods = methods.Where(m => m.Parameters.Count == 2 && m.Parameters[0].ParameterType.FullName == "Piece");
    int sites = methods.Sum(m => m.Body.Instructions.Count(i => i.Operand is MethodReference r && r.DeclaringType.FullName == "Inventory" && r.Name == "CountItems" && r.Parameters.Count == 3));
    if (sites == 0) Fail("Crafting transpiler has no CountItems call site: " + name);
    else Console.WriteLine($"Crafting hook {name}: {sites} material-count call sites");
}
foreach (var retired in new[] { "StackSizeService", "AutoRefuelService", "NativeInterface", "ContainerSizeService" })
    if (mod.Types.Any(t => t.Name == retired)) Fail("Retired implementation shipped: " + retired);
var stationType = api.Types.Single(t => t.Name == "CraftingStation");
if (!stationType.Fields.Any(f => f.Name == "m_allStations" && f.IsStatic && f.FieldType.FullName == "System.Collections.Generic.List`1<CraftingStation>"))
    Fail("Station coverage registry injection does not match");
else reflected++;
var coverage = mod.Types.Single(t => t.Name == "StationCoverage");
foreach (var method in coverage.Methods)
foreach (var patch in method.CustomAttributes.Where(a => a.AttributeType.Name == "HarmonyPatch"))
    if (((TypeReference)patch.ConstructorArguments[0].Value).FullName != "CraftingStation" || (string)patch.ConstructorArguments[1].Value != "HaveBuildStationInRange")
        Fail("Coverage must only extend building lookup, not station interaction: " + method.Name);
foreach (var name in new[] { "HaveRequirements", "CheckCanRemovePiece" })
{
    var methods = api.Types.Single(t => t.Name == "Player").Methods.Where(m => m.Name == name && m.HasBody);
    if (!methods.Any(m => m.Body.Instructions.Any(i => i.Operand is MethodReference r && r.DeclaringType.Name == "CraftingStation" && r.Name == "HaveBuildStationInRange")))
        Fail("Station coverage call site missing: Player." + name);
}
foreach (var target in new[] { ("Smelter", "Spawn"), ("Fermenter", "DelayedTap"), ("CookingStation", "SpawnItem") })
{
    var method = api.Types.Single(t => t.Name == target.Item1).Methods.Single(m => m.Name == target.Item2);
    if (!method.Body.Instructions.Any(i => i.Operand is MethodReference r && r.DeclaringType.Name == "ItemDrop" && r.Name == "OnCreateNew" && r.Parameters[0].ParameterType.Name == "ItemDrop"))
        Fail("Native production tag call missing: " + target);
}
var spawnObserver = mod.Types.Single(t => t.Name == "RuntimePatches").Methods.Single(m => m.Name == "SmelterOutput");
if (spawnObserver.ReturnType.FullName != "System.Void") Fail("Production observation must not suppress vanilla Spawn");
var cookingStatus = api.Types.Single(t => t.Name == "CookingStation").NestedTypes.Single(t => t.Name == "Status");
foreach (var state in new[]{("NotDone",0),("Done",1),("Burnt",2)})
    if (!cookingStatus.Fields.Any(f => f.Name == state.Item1 && Equals(f.Constant,state.Item2))) Fail("Cooking slot state mismatch: " + state.Item1);
// Read only plugin metadata (no third-party source reconstruction) for conflict identities.
foreach (var path in Directory.GetFiles(Path.Combine(game, "BepInEx/plugins"), "*.dll", SearchOption.AllDirectories).Where(p => Path.GetFileName(p) is "Hearthkeeper.dll" or "AutomaticFermenters.dll"))
{
    using var other = ModuleDefinition.ReadModule(path);
    foreach (var a in other.Types.SelectMany(t => t.CustomAttributes).Where(a => a.AttributeType.Name == "BepInPlugin")) Console.WriteLine("Installed original identity: " + string.Join(", ", a.ConstructorArguments.Select(a => a.Value)));
}
Console.WriteLine($"Resolved {members} binary members, {hooks} Harmony hooks, {reflected} reflection targets. Failures: {failures}.");
Environment.ExitCode = failures == 0 ? 0 : 1;
