using Mono.Cecil;
using Mono.Cecil.Cil;
var game = args[0]; var plugin = args[1];
var resolver = new DefaultAssemblyResolver();
resolver.AddSearchDirectory(Path.Combine(game, "valheim_Data/Managed")); resolver.AddSearchDirectory(Path.Combine(game, "BepInEx/core"));
resolver.AddSearchDirectory(Path.Combine(game, "BepInEx/plugins/ValheimModding-Jotunn"));
resolver.AddSearchDirectory(Path.GetDirectoryName(typeof(object).Assembly.Location));
var parameters = new ReaderParameters { AssemblyResolver = resolver };
using var mod = ModuleDefinition.ReadModule(plugin, parameters);
using var api = ModuleDefinition.ReadModule(Path.Combine(game, "valheim_Data/Managed/assembly_valheim.dll"), parameters);
int failures = 0, members = 0, hooks = 0, reflected = 0;
void Fail(string s) { Console.WriteLine("FAIL " + s); failures++; }
if(!api.Types.Single(t=>t.Name=="Chat").Fields.Any(f=>f.Name=="m_hideTimer" && f.FieldType.FullName=="System.Single"))Fail("Chat visibility field changed");
foreach (var reference in mod.GetMemberReferences())
{
    if (!(reference.DeclaringType.Namespace == "" || reference.DeclaringType.Namespace.StartsWith("UnityEngine") || reference.DeclaringType.Namespace.StartsWith("BepInEx") || reference.DeclaringType.Namespace.StartsWith("HarmonyLib") || reference.DeclaringType.Namespace == "TMPro" || reference.DeclaringType.Namespace.StartsWith("Jotunn"))) continue;
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
    foreach (var injected in patch.Parameters.Where(p => p.Name.StartsWith("___")))
    {
        var field = targetType.Resolve().Fields.FirstOrDefault(f => f.Name == injected.Name.Substring(3));
        string injectedType = injected.ParameterType is ByReferenceType fieldRef ? fieldRef.ElementType.FullName : injected.ParameterType.FullName;
        if (field == null || field.FieldType.FullName != injectedType) Fail("Injected field " + patch.Name + "." + injected.Name);
    }
    if (result != null && (result.ParameterType is ByReferenceType resultRef ? resultRef.ElementType.FullName : result.ParameterType.FullName) != target.ReturnType.FullName) Fail("Result type " + patch.Name);
}
// The pickup filter must run before attraction, ownership requests and actual pickup.
var autoPickup = api.Types.Single(t => t.Name == "Player").Methods.Single(m => m.Name == "AutoPickup");
var autoInstructions = autoPickup.Body.Instructions.ToList();
var eligibility = autoInstructions.Where(i => i.OpCode == OpCodes.Ldfld && i.Operand is FieldReference f && f.DeclaringType.Name == "ItemDrop" && f.Name == "m_autoPickup").ToList();
if (eligibility.Count != 1) Fail("Pickup eligibility field read changed");
else foreach (var callName in new[]{"RequestOwn", "Pickup", "set_position"})
{
    int site = autoInstructions.FindIndex(i => i.Operand is MethodReference r && r.Name == callName);
    if (site < 0 || autoInstructions.IndexOf(eligibility[0]) >= site) Fail("Pickup filter must precede " + callName);
}
if (!api.Types.Single(t => t.Name == "ItemDrop").Methods.Where(m => m.Name is "Pickup" or "PickupUpdate").All(m =>
    m.Body.Instructions.Any(i => i.Operand is MethodReference r && r.Name == "Pickup" && r.DeclaringType.Name == "Humanoid")))
    Fail("Manual pickup ownership retry no longer reaches Humanoid.Pickup");
// Class-level placement hook: validate the private injected ghost field too.
var preview=mod.Types.Single(t=>t.Name=="DepositChestPreview");
var previewPatch=preview.CustomAttributes.Single(a=>a.AttributeType.Name=="HarmonyPatch");
var player=api.Types.Single(t=>t.Name=="Player");
if((string)previewPatch.ConstructorArguments[1].Value!="SetupPlacementGhost" || player.Methods.Count(m=>m.Name=="SetupPlacementGhost" && m.Parameters.Count==0)!=1)
    Fail("Deposit chest preview hook changed");
if(!player.Fields.Any(f=>f.Name=="m_placementGhost" && f.FieldType.FullName=="UnityEngine.GameObject"))Fail("Placement ghost injection changed");
else hooks++;
var chestResource=(EmbeddedResource)mod.Resources.Single(r=>r.Name=="Quartermaster.DepositChest.model");
var sourceRoot=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(plugin)!,"../../.."));
if(!chestResource.GetResourceData().SequenceEqual(File.ReadAllBytes(Path.Combine(sourceRoot,"assets/deposit-chest/model.bin"))))Fail("Stale embedded deposit chest mesh");
var register=mod.Types.Single(t=>t.Name=="BuildPieces").Methods.Single(m=>m.Name=="Register");
if(!register.Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldstr && i.Operand is string value && value=="piece_chest_blackmetal"))Fail("Dedicated chest must inherit black metal storage and recipe");
if(register.Body.Instructions.Any(i=>i.Operand is MethodReference m && m.DeclaringType.Name=="QuartermasterChestModel" && m.Name=="Build"))
    Fail("Custom chest visuals must remain disabled while retaining the registered prefab");
foreach (var entry in new (string type, string name, string returns, string[] args)[] {
    ("Ship", "HaveControllingPlayer", "System.Boolean", Array.Empty<string>()),
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
var itemTooltip = api.Types.Single(t => t.Name == "ItemDrop").NestedTypes.Single(t => t.Name == "ItemData").Methods.Single(m => m.Name == "GetTooltip" && m.IsStatic);
var tooltipStrings = itemTooltip.Body.Instructions.Select(i => i.Operand).OfType<string>().ToArray();
if (!tooltipStrings.Contains("$achievements_cheated_item_inventory") || !tooltipStrings.Contains("\n<color=#808080><i>") || !tooltipStrings.Contains("</i></color>"))
    Fail("Item tooltip notice format has changed");
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
// Cosmetic props must never become real items, network objects or physics bodies.
var gullActor=mod.Types.Single(t=>t.Name=="DepositGull");
var createBird=gullActor.Methods.Single(m=>m.Name=="CreateBird");
if(createBird.Body.Instructions.Any(i=>i.Operand is MethodReference m && m.DeclaringType.Name=="Transform" && m.Name=="SetParent"))
    Fail("Deposit gull root must remain outside chest hierarchy to avoid inherited glow");
foreach(var name in new[]{"OnDisable","OnDestroy"})
    if(!gullActor.Methods.Any(m=>m.Name==name && m.HasBody)) Fail("Detached gull cleanup missing: "+name);
foreach (var type in mod.Types.Where(t => t.Name is "GullToss" or "DepositGull"))
foreach (var method in type.Methods.Where(m => m.HasBody))
foreach (var instruction in method.Body.Instructions)
{
    if (instruction.Operand is not MethodReference call) continue;
    if (call.DeclaringType.Name == "Inventory" && call.Name is "AddItem" or "RemoveItem" or "MoveItemToThis")
        Fail("Cosmetic gull mutates inventory: " + call.FullName);
    if (call.DeclaringType.Name == "ItemDrop" && call.Name is "DropItem" or "OnCreateNew")
        Fail("Cosmetic gull creates real items: " + call.FullName);
    if (call.DeclaringType.FullName == "UnityEngine.Object" && call.Name == "Instantiate")
        Fail("Cosmetic gull clones a live prefab: " + call.FullName);
    if (call is GenericInstanceMethod generic && call.Name == "AddComponent" &&
        generic.GenericArguments.Any(t => t.Name is "ZNetView" or "ItemDrop" or "Rigidbody" or "SphereCollider" or "BoxCollider"))
        Fail("Cosmetic gull adds gameplay component: " + call.FullName);
}
// Ward deterrence relies on the native ownership/maintenance gate before combat.
var monster=api.Types.Single(t=>t.Name=="MonsterAI");
var baseAi=api.Types.Single(t=>t.Name=="BaseAI");
foreach(var member in new[]{("PrivateArea","m_allAreas"),("MonsterAI","m_targetCreature"),("MonsterAI","m_targetStatic")})
{if(!api.Types.Single(t=>t.Name==member.Item1).Fields.Any(f=>f.Name==member.Item2))Fail("Ward field missing: "+member);else reflected++;}
if(!baseAi.Methods.Any(m=>m.Name=="Flee"&&m.Parameters.Count==2&&m.Parameters[0].ParameterType.FullName=="System.Single"&&m.Parameters[1].ParameterType.FullName=="UnityEngine.Vector3"))Fail("Ward flee signature changed");else reflected++;
if(!monster.Methods.Any(m=>m.Name=="Wakeup"&&m.Parameters.Count==0))Fail("Ward wakeup signature changed");else reflected++;
var update=monster.Methods.Single(m=>m.Name=="UpdateAI");
var baseCall=update.Body.Instructions.FirstOrDefault(i=>i.OpCode==OpCodes.Call&&i.Operand is MethodReference m&&m.DeclaringType.Name=="BaseAI"&&m.Name=="UpdateAI");
if(baseCall==null||baseCall.Next.OpCode.FlowControl!=FlowControl.Cond_Branch||baseCall.Next.Next.OpCode!=OpCodes.Ldc_I4_0||baseCall.Next.Next.Next.OpCode!=OpCodes.Ret)Fail("MonsterAI no longer exits after the base gate");
Console.WriteLine($"Resolved {members} binary members, {hooks} Harmony hooks, {reflected} reflection targets. Failures: {failures}.");
Environment.ExitCode = failures == 0 ? 0 : 1;
