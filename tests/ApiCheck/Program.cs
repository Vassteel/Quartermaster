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
// Harmony scans convention-named lifecycle methods during PatchAll, even on
// helper classes. An instance RPC named Prepare prevents the entire mod loading.
foreach (var type in mod.GetTypes())
foreach (var method in type.Methods.Where(m => m.Name is "Prepare" or "Cleanup" or "TargetMethod" or "TargetMethods"))
    if (!method.IsStatic) Fail("Harmony lifecycle name collision: " + method.FullName);
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
        if (tp == null || (tp.ParameterType is ByReferenceType targetRef ? targetRef.ElementType.FullName : tp.ParameterType.FullName) != actual) Fail($"Hook argument {patch.Name}.{p.Name} mismatches target {target.FullName}");
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
// Cloned building templates must exist in this game installation. A valid C#
// call cannot catch a misspelled prefab name such as the old "piece_sign".
var manifestPath=Path.Combine(game,"valheim_Data/StreamingAssets/SoftRef/manifest_extended");
var prefabNames=File.ReadLines(manifestPath).Select(line=>line.Trim())
    .Where(line=>line.StartsWith("path in bundle: ")&&line.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase))
    .Select(line=>Path.GetFileNameWithoutExtension(line.Substring("path in bundle: ".Length))).ToHashSet(StringComparer.Ordinal);
foreach(var method in mod.Types.Where(t=>t.Name=="BuildPieces"||t.Name=="ClayResource"||t.Name=="Apothecary").SelectMany(t=>t.Methods).Where(m=>m.HasBody))
foreach(var call in method.Body.Instructions.Where(i=>i.Operand is MethodReference r&&r.Name=="CreateClonedPrefab"))
{
    var argument=call.Previous;
    while(argument!=null&&argument.OpCode!=OpCodes.Ldstr)argument=argument.Previous;
    if(argument==null||!prefabNames.Contains((string)argument.Operand))Fail("Missing native building template: "+argument?.Operand);
    else reflected++;
}
// These category hooks live on classes, unlike the method-level hooks above.
var usageList=api.Types.Single(t=>t.Name=="ByUsagePieceList");
foreach(var mapping in new[]{("AddQuartermasterUsageCategory",".ctor"),("QuartermasterUsageLabel","GetTagDisplayName"),("HideUnrelatedQuartermasterCategory","UpdateAvailableTags"),("FilterQuartermasterUsageCategory","GetAvailablePiecesWithTag")})
{
    var patchType=mod.Types.Single(t=>t.Name==mapping.Item1);
    var attribute=patchType.CustomAttributes.Single(a=>a.AttributeType.Name=="HarmonyPatch");
    if(((TypeReference)attribute.ConstructorArguments[0].Value).FullName!=usageList.FullName)Fail("Build menu hook target: "+mapping.Item1);
    var target=usageList.Methods.Single(m=>m.Name==mapping.Item2);
    var patch=patchType.Methods.Single(m=>m.Name=="Prefix"||m.Name=="Postfix");
    foreach(var parameter in patch.Parameters)
    {
        var expected=parameter.Name.StartsWith("___")?usageList.Fields.SingleOrDefault(f=>f.Name==parameter.Name.Substring(3))?.FieldType:
            parameter.Name=="__result"?target.ReturnType:target.Parameters.SingleOrDefault(p=>p.Name==parameter.Name)?.ParameterType;
        var actual=parameter.ParameterType is ByReferenceType byRef?byRef.ElementType:parameter.ParameterType;
        if(expected==null||expected.FullName!=actual.FullName)Fail("Build menu injection: "+mapping.Item1+"."+parameter.Name);
    }
    if(mapping.Item2==".ctor"&&(target.Parameters.Count!=1||target.Parameters[0].ParameterType.FullName!="System.String"))Fail("Build menu constructor changed");
    hooks++;
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
int canPickupSite = autoInstructions.FindIndex(i => i.Operand is MethodReference r && r.DeclaringType.Name == "ItemDrop" && r.Name == "CanPickup");
int attractionSite = autoInstructions.FindIndex(i => i.Operand is MethodReference r && r.Name == "set_position");
if (canPickupSite < 0 || attractionSite < 0 || canPickupSite >= attractionSite)
    Fail("Scoped pickup fallback must precede native attraction");
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
foreach(var asset in new[]{("Quartermaster.Owl.model","model.bin"),("Quartermaster.Owl.albedo","albedo.png")})
{
    var resource=mod.Resources.OfType<EmbeddedResource>().SingleOrDefault(r=>r.Name==asset.Item1);
    if(resource==null || !resource.GetResourceData().SequenceEqual(File.ReadAllBytes(Path.Combine(sourceRoot,"assets/owl",asset.Item2))))
        Fail("Missing or stale embedded owl asset: "+asset.Item1);
}
foreach(var asset in new[]{("Quartermaster.Ledger.model","model.bin"),("Quartermaster.Ledger.icon","icon.png")})
{
    var resource=mod.Resources.OfType<EmbeddedResource>().SingleOrDefault(r=>r.Name==asset.Item1);
    if(resource==null||!resource.GetResourceData().SequenceEqual(File.ReadAllBytes(Path.Combine(sourceRoot,"assets/ledger",asset.Item2))))Fail("Missing or stale ledger asset: "+asset.Item1);
}
var dropSaver=api.Types.Single(t=>t.Name=="ItemDrop").Methods.Single(m=>m.Name=="SaveToZDO");
if(!dropSaver.IsPublic||!dropSaver.IsStatic)Fail("Native item metadata persistence must be available for overflow drops");
var owlFactory=mod.Types.Single(t=>t.Name=="DepositGull").Methods.Single(m=>m.Name=="CreateBird");
if(!owlFactory.Body.Instructions.Any(i=>i.OpCode==OpCodes.Newobj && i.Operand is MethodReference m && m.DeclaringType.Name=="QuartermasterOwl"))
    Fail("Deposit chest must instantiate the upgraded owl rig");
var register=mod.Types.Single(t=>t.Name=="BuildPieces").Methods.Single(m=>m.Name=="Register");
if(!register.Body.Instructions.Any(i=>i.OpCode==OpCodes.Ldstr && i.Operand is string value && value=="piece_chest_blackmetal"))Fail("Dedicated chest must inherit black metal storage and recipe");
if(register.Body.Instructions.Count(i=>i.Operand is MethodReference m && m.DeclaringType.Name=="PieceManager" && m.Name=="AddPiece")!=1)
    Fail("Only ledger should receive a build recipe from BuildPieces");
if(!register.Body.Instructions.Any(i=>i.Operand is MethodReference m && m.DeclaringType.Name=="PrefabManager" && m.Name=="AddPrefab"))
    Fail("Saved chest prefab must remain registered without a build recipe");
foreach(var name in new[]{"piece_chest_wood","sign","crystal_wall_1x1"})
    if(!prefabNames.Contains(name))Fail("Missing native storage material template: "+name);
foreach(var type in mod.Types.Where(t=>t.Name is "StorageMaterials" or "ClayModel" or "ApothecaryArt"))
foreach(var method in type.Methods.Where(m=>m.HasBody))
    if(method.Body.Instructions.Any(i=>i.Operand is MethodReference m&&m.DeclaringType.Name=="Shader"&&m.Name=="Find"))
        Fail("Storage art must use loaded material references, not stripped shader lookup: "+type.Name);
if(register.Body.Instructions.Any(i=>i.Operand is MethodReference m && m.DeclaringType.Name=="QuartermasterChestModel" && m.Name=="Build"))
    Fail("Custom chest visuals must remain disabled while retaining the registered prefab");
foreach (var entry in new (string type, string name, string returns, string[] args)[] {
    ("Ship", "HaveControllingPlayer", "System.Boolean", Array.Empty<string>()),
    ("Container", "Load", "System.Boolean", Array.Empty<string>()),
    ("Container", "Save", "System.Void", Array.Empty<string>()),
    ("PrivateArea", "IsEnabled", "System.Boolean", Array.Empty<string>()),
    ("PrivateArea", "IsInside", "System.Boolean", new[]{"UnityEngine.Vector3","System.Single"}),
    ("PrivateArea", "IsPermitted", "System.Boolean", new[]{"System.Int64"}),
    ("Player", "TryPlacePiece", "System.Boolean", new[]{"Piece"}),
    ("Player", "HaveRequirements", "System.Boolean", new[]{"Recipe","System.Boolean","System.Int32","System.Int32"}),
    ("Player", "HaveRequirements", "System.Boolean", new[]{"Piece","Player/RequirementMode"}),
    ("InventoryGui", "DoCrafting", "System.Void", new[]{"Player"}),
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
foreach (var name in new[] { "HaveRequirementItems", "HaveRequirements", "SetupRequirement", "GetFirstRequiredItem" })
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
// Hive output bypasses OnCreateNew; our scoped SetStack observer must remain valid.
var hiveExtract=api.Types.Single(t=>t.Name=="Beehive").Methods.Single(m=>m.Name=="RPC_Extract");
if(!hiveExtract.Body.Instructions.Any(i=>i.Operand is MethodReference r&&r.DeclaringType.Name=="ItemDrop"&&r.Name=="SetStack"))
    Fail("Beehive no longer finalizes native drops through SetStack");
var hiveObserver=mod.Types.Single(t=>t.Name=="RuntimePatches").Methods.Single(m=>m.Name=="HiveOutput");
if(!hiveObserver.CustomAttributes.Any(a=>a.AttributeType.Name=="HarmonyPostfix"))Fail("Honey collection must observe, not replace, native item spawns");
var spawnObserver = mod.Types.Single(t => t.Name == "RuntimePatches").Methods.Single(m => m.Name == "SmelterOutput");
if (spawnObserver.ReturnType.FullName != "System.Void") Fail("Production observation must not suppress vanilla Spawn");
var cookingStatus = api.Types.Single(t => t.Name == "CookingStation").NestedTypes.Single(t => t.Name == "Status");
foreach (var state in new[]{("NotDone",0),("Done",1),("Burnt",2)})
    if (!cookingStatus.Fields.Any(f => f.Name == state.Item1 && Equals(f.Constant,state.Item2))) Fail("Cooking slot state mismatch: " + state.Item1);
// Read only plugin metadata (no third-party source reconstruction) for conflict identities.
foreach (var path in Directory.GetFiles(Path.Combine(game, "BepInEx/plugins"), "*.dll", SearchOption.AllDirectories).Where(p => Path.GetFileName(p) is "AutomaticFermenters.dll"))
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
foreach (var type in mod.Types.Where(t => t.Name is "GullToss" or "DepositGull" or "OwlCourier" or "OwlWork" or "OwlBeeMotes"))
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
// World pickup must use the native owner-checked single-item handler, preserving
// oversized saved piles (SetStack clamps those to the current maximum).
var removeOne=api.Types.Single(t=>t.Name=="ItemDrop").Methods.Single(m=>m.Name=="RemoveOne");
if(!api.Types.Single(t=>t.Name=="ItemDrop").Fields.Any(f=>f.Name=="s_instances"&&f.IsStatic&&f.FieldType.FullName=="System.Collections.Generic.List`1<ItemDrop>"))
    Fail("Owl cleanup item-registry recovery no longer matches the game");
else reflected++;
if(!removeOne.Body.Instructions.Any(i=>i.Operand is MethodReference r&&r.Name=="CanPickup")||
   !removeOne.Body.Instructions.Any(i=>i.Operand is MethodReference r&&r.Name=="Save"))
    Fail("World-item pickup ownership or persistence semantics changed");
foreach(var type in mod.Types.Where(t=>t.Name=="OwlWork"))
foreach(var method in type.Methods.Where(m=>m.HasBody))
    if(method.Body.Instructions.Any(i=>i.Operand is string text&&text.StartsWith("RPC_")))
        Fail("Owl activity observation must not invoke production RPCs");
// Verify the two exact native instruction sequences used by the report fixes.
var hints = api.Types.Single(t => t.Name == "KeyHints").Methods.Single(m => m.Name == "Update");
if (hints.Body.Instructions.Count(i => i.Operand is MethodReference r && r.DeclaringType.Name == "ZInput" && r.Name == "GetKeyDown" &&
    i.Previous.OpCode == OpCodes.Ldc_I4_1 && i.Previous.Previous.OpCode == OpCodes.Ldc_I4 && Equals(i.Previous.Previous.Operand, 290)) != 1)
    Fail("Native F9 controller-layout instruction sequence changed");
var inventoryType = api.Types.Single(t => t.Name == "Inventory");
var savedAdd = inventoryType.Methods.Single(m => m.Name == "AddItem" && m.Parameters.Count == 14 && m.Parameters[0].ParameterType.FullName == "System.Int32");
if (savedAdd.Body.Instructions.Count(i => i.Operand is MethodReference r && r.DeclaringType.FullName == "UnityEngine.Mathf" && r.Name == "Min" &&
    r.Parameters.All(p => p.ParameterType.FullName == "System.Int32") && i.Previous.Operand is FieldReference f && f.Name == "m_maxStackSize") != 1)
    Fail("Saved inventory quantity clamp changed");
bool ReachesSavedAdd(MethodDefinition method, HashSet<string> seen)
{
    if (method == savedAdd) return true;
    if (!method.HasBody || !seen.Add(method.FullName)) return false;
    return method.Body.Instructions.Select(i => i.Operand).OfType<MethodReference>()
        .Where(r => r.DeclaringType.FullName == inventoryType.FullName)
        .Any(r => ReachesSavedAdd(r.Resolve(), seen));
}
foreach (var loader in inventoryType.Methods.Where(m => m.Name is "Load" or "LoadOld"))
    if (!ReachesSavedAdd(loader, new())) Fail("Inventory loader bypasses preserved saved quantity: " + loader.Name);
// Creature behavior and spawning must remain completely native.
foreach (var type in mod.Types)
foreach (var method in type.Methods)
foreach (var attribute in method.CustomAttributes.Where(a => a.AttributeType.FullName == "HarmonyLib.HarmonyPatch" && a.ConstructorArguments.Count >= 2))
{
    var target = (TypeReference)attribute.ConstructorArguments[0].Value;
    var name = (string)attribute.ConstructorArguments[1].Value;
    if ((target.Name is "BaseAI" or "MonsterAI") && name == "UpdateAI" ||
        target.Name is "SpawnSystem" or "SpawnArea" or "CreatureSpawner")
        Fail("Creature behavior override remains: " + method.FullName);
}
foreach(var entry in new[]{("InventoryGui","m_craftRecipe"),("InventoryGui","m_craftUpgradeItem"),("InventoryGui","m_multiCrafting"),("InventoryGui","m_multiCraftAmount"),("Player","m_placementGhost"),("Player","m_noPlacementCost"),("PrivateArea","m_allAreas")})
{if(!api.Types.Single(t=>t.Name==entry.Item1).Fields.Any(f=>f.Name==entry.Item2))Fail("Shared operation reflection field: "+entry);else reflected++;}
var placement=api.Types.Single(t=>t.Name=="Player").Methods.Single(m=>m.Name=="UpdatePlacement");
if(placement.Body.Instructions.Count(i=>i.Operand is MethodReference m&&m.Name=="TryPlacePiece")!=1)Fail("Placement transaction hook must wrap exactly one native placement call");
// Optional third argument verifies the exact InventorySlots build in a test profile.
if (args.Length > 2)
{
    using var slots = ModuleDefinition.ReadModule(args[2]);
    var slotsType = slots.Types.SingleOrDefault(t => t.FullName == "InventorySlots.InventorySlotsPlugin");
    foreach (var contract in new[] {
        ("IsUsableRegularCell", new[] { "Inventory", "Player", "Vector2i" }),
        ("IsFavoriteSlot", new[] { "Player", "Vector2i" }) })
    {
        if (slotsType == null || !slotsType.Methods.Any(m => m.Name == contract.Item1 && m.IsStatic &&
            m.ReturnType.FullName == "System.Boolean" && m.Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(contract.Item2)))
            Fail("InventorySlots policy contract changed: " + contract.Item1);
        else reflected++;
    }
    foreach (string panel in new[] { "InventorySlots_CustomSlotPanel", "InventorySlots_PlayerStatPanelHost" })
        if (slotsType == null || !slotsType.Fields.Any(f => f.HasConstant && Equals(f.Constant, panel)))
            Fail("InventorySlots UI hierarchy changed: " + panel);
        else reflected++;
}
// Shared chests patch these by hand (Harmony binds prefix arguments by parameter name).
foreach (var entry in new[] {
    ("Container", "RPC_RequestOpen", "System.Void", new[]{"System.Int64","System.Int64"}, new[]{"uid","playerID"}),
    ("Container", "StackAll", "System.Void", Array.Empty<string>(), Array.Empty<string>()),
    ("Container", "Load", "System.Boolean", Array.Empty<string>(), Array.Empty<string>()),
    ("Container", "CheckForChanges", "System.Void", Array.Empty<string>(), Array.Empty<string>()),
    ("InventoryGui", "UpdateContainer", "System.Void", new[]{"Player"}, new[]{"player"}),
    ("InventoryGui", "OnSelectedItem", "System.Void", new[]{"InventoryGrid","ItemDrop/ItemData","Vector2i","InventoryGrid/Modifier"}, new[]{"grid","item","pos","mod"}),
    ("InventoryGui", "OnTakeAll", "System.Void", Array.Empty<string>(), Array.Empty<string>()),
    ("InventoryGui", "OnStackAll", "System.Void", Array.Empty<string>(), Array.Empty<string>()),
    ("InventoryGui", "OnRightClickItem", "System.Void", new[]{"InventoryGrid","ItemDrop/ItemData","Vector2i"}, new[]{"grid","item","pos"}),
    ("InventoryGui", "OnDropOutside", "System.Void", Array.Empty<string>(), Array.Empty<string>()),
    ("InventoryGui", "SetupDragItem", "System.Void", new[]{"ItemDrop/ItemData","Inventory","System.Int32"}, new[]{"item","inventory","amount"}) })
{
    var matches = api.Types.Single(t => t.Name == entry.Item1).Methods.Where(m => m.Name == entry.Item2).ToArray();
    if (matches.Length != 1 || matches[0].ReturnType.FullName != entry.Item3 || !matches[0].Parameters.Select(p => p.ParameterType.FullName).SequenceEqual(entry.Item4)
        || !matches[0].Parameters.Select(p => p.Name).SequenceEqual(entry.Item5)) Fail("Shared chest hook " + entry.Item1 + "." + entry.Item2);
    else { reflected++; hooks++; }
}
foreach (var entry in new[] { ("Container","m_inUse","System.Boolean"), ("Container","m_lastRevision","System.UInt32"), ("InventoryGui","m_currentContainer","Container"),
    ("InventoryGui","m_dragItem","ItemDrop/ItemData"), ("InventoryGui","m_dragInventory","Inventory"), ("InventoryGui","m_dragAmount","System.Int32"), ("InventoryGui","m_dragGo","UnityEngine.GameObject") })
    if (!api.Types.Single(t => t.Name == entry.Item1).Fields.Any(f => f.Name == entry.Item2 && f.FieldType.FullName == entry.Item3)) Fail("Shared chest field " + entry); else reflected++;
{
    // The transpiler replaces the first Container.IsOwner call in UpdateContainer: the panel visibility check.
    var body = api.Types.Single(t => t.Name == "InventoryGui").Methods.Single(m => m.Name == "UpdateContainer").Body.Instructions;
    var first = body.FirstOrDefault(i => i.Operand is MethodReference r && r.DeclaringType.Name == "Container" && r.Name == "IsOwner");
    var before = first?.Previous;
    if (first == null || !(before?.Operand is FieldReference f && f.Name == "m_currentContainer")) Fail("Shared chest: container panel owner check changed");
    else reflected++;
    var awake = api.Types.Single(t => t.Name == "Container").Methods.Single(m => m.Name == "Awake").Body.Instructions;
    if (!awake.Any(i => i.Operand is string s && s == "RPC_OpenResponse")) Fail("Shared chest: RPC_OpenResponse registration changed"); else reflected++;
    var load = api.Types.Single(t => t.Name == "Inventory").Methods.Where(m => m.Name == "Load" && m.Parameters.Count == 1).ToArray();
    if (load.Length != 1) Fail("Shared chest: Inventory.Load(ZPackage) changed"); else reflected++;
}
Console.WriteLine($"Resolved {members} binary members, {hooks} Harmony hooks, {reflected} reflection targets. Failures: {failures}.");
Environment.ExitCode = failures == 0 ? 0 : 1;
