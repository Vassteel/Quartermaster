using Quartermaster.Cosmetics;
int checks=0;
foreach(var name in new[]{"mailbox","eagle","penguin"}) {
 using var file=File.OpenRead(Path.Combine(args[0],name,"model.bin"));
 var data=OwlModelData.Read(file, tiledUv:name=="mailbox");
 if(data.Levels.Count!=2)throw new Exception("Missing LOD");checks++;
 int last=int.MaxValue;
 foreach(var level in data.Levels){int triangles=level.Sum(p=>p.Triangles.Length/3);if(triangles>last||triangles>6000)throw new Exception("Mesh budget or LOD growth");last=triangles;checks++;}
 if(name=="mailbox")
 {
  if(data.Levels.Any(level=>level.Any(part=>part.Material<0||part.Material>3||part.Pivot!=0)))throw new Exception("Mailbox must contain only the approved fixed plank and timber surfaces");
  if(data.Levels.Any(level=>level.Length!=4))throw new Exception("Mailbox must batch into four material surfaces");
  var points=data.Levels[0].SelectMany(p=>Enumerable.Range(0,p.Vertices.Length/8).Select(i=>(x:p.Vertices[i*8],y:p.Vertices[i*8+1],z:p.Vertices[i*8+2]))).ToArray();
  if(points.Min(p=>p.y)<-.001f||points.Max(p=>p.y)<1.95f||points.Max(p=>p.y)>2.05f||points.Any(p=>Math.Abs(p.x)>.55f||Math.Abs(p.z)>.75f))throw new Exception("Mailbox orientation, height or origin drifted from placement bounds");
  using var strict=File.OpenRead(Path.Combine(args[0],name,"model.bin"));
  bool rejected=false;try{OwlModelData.Read(strict);}catch(InvalidDataException){rejected=true;}
  if(!rejected)throw new Exception("Atlas readers must continue rejecting tiled UVs");
  checks+=4;
 }
 Console.WriteLine($"{name}: valid runtime mesh and LODs");
}
Console.WriteLine($"PASS: {checks} postal asset checks plus bounded binary-reader validation.");
