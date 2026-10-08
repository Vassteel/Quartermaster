using Quartermaster.Cosmetics;

var path=Path.GetFullPath(args.Length>0?args[0]:Path.Combine(AppContext.BaseDirectory,"../../../../../assets/owl/model.bin"));
var bytes=File.ReadAllBytes(path);int checks=0;
void Check(bool condition,string label){checks++;if(!condition)throw new Exception(label);}
OwlModelData Read(byte[] source)=>OwlModelData.Read(new MemoryStream(source));
void Reject(byte[] source,string label)
{
    try {Read(source);throw new Exception("Accepted invalid model: "+label);}
    catch(Exception error) when(error is IOException || error is InvalidDataException){checks++;}
}
var data=Read(bytes);
Check(data.Levels.Count==2,"two distance levels");
var pivots=new[]{new[]{-.099f,.34f,-.015f},new[]{0f,.29f,0f},new[]{0f,.76f,.035f},new[]{-.22f,.63f,-.02f},new[]{.22f,.63f,-.02f},new[]{-.112f,.91f,.239f},new[]{.112f,.91f,.239f},new[]{.099f,.34f,-.015f}};
int[] counts=new int[2];
for(int level=0;level<2;level++)
{
    var parts=data.Levels[level];
    Check(parts.Length<=16,"draw call budget");
    Check(parts.Select(p=>p.Pivot).Distinct().Count()==8,"all animation pivots retained");
    float minY=float.MaxValue,maxY=float.MinValue;
    foreach(var part in parts)
    {
        counts[level]+=part.Triangles.Length/3;
        for(int i=0;i<part.Vertices.Length;i+=8)
        {
            float x=part.Vertices[i]+pivots[part.Pivot][0],y=part.Vertices[i+1]+pivots[part.Pivot][1],z=part.Vertices[i+2]+pivots[part.Pivot][2];
            minY=Math.Min(y,minY);maxY=Math.Max(y,maxY);
            Check(Math.Abs(x)<.4&&y>=-.015&&y<1.3&&Math.Abs(z)<.4,"fits existing chest perch and owl scale");
            float u=part.Vertices[i+6],v=part.Vertices[i+7];
            Check(Math.Abs(u-.5f)>.012f&&Math.Abs(v-.5f)>.012f,"atlas quadrant gutter");
        }
        for(int i=0;i<part.Triangles.Length;i+=3)
        {
            var a=part.Triangles[i]*8;var b=part.Triangles[i+1]*8;var c=part.Triangles[i+2]*8;var v=part.Vertices;
            var x1=v[b]-v[a];var y1=v[b+1]-v[a+1];var z1=v[b+2]-v[a+2];
            var x2=v[c]-v[a];var y2=v[c+1]-v[a+1];var z2=v[c+2]-v[a+2];
            var cx=y1*z2-z1*y2;var cy=z1*x2-x1*z2;var cz=x1*y2-y1*x2;
            Check(cx*cx+cy*cy+cz*cz>1e-20,"nondegenerate exported triangle");
            bool HeadSurface(int index)
            {
                // Crown and flush facial disks use analytic ellipsoid normals;
                // separate folded feather leaves have their own hard edge normals.
                float nx=v[index]/(.237f*.237f),ny=(v[index+1]+.76f-.89f)/(.204f*.204f),nz=(v[index+2]+.035f-.027f)/(.181f*.181f);
                float length=MathF.Sqrt(nx*nx+ny*ny+nz*nz);
                return (nx*v[index+3]+ny*v[index+4]+nz*v[index+5])/length>.9999f;
            }
            if(part.Pivot==2&&part.Material==0&&HeadSurface(a)&&HeadSurface(b)&&HeadSurface(c))
                Check(cx*(v[a+3]+v[b+3]+v[c+3])+cy*(v[a+4]+v[b+4]+v[c+4])+cz*(v[a+5]+v[b+5]+v[c+5])>0,"facial feather triangles face outward with their lighting normals");
        }
    }
    Check(minY<.015&&maxY>1,"feet contact and complete head");
}
Check(counts[0]<24000&&counts[1]<8000&&counts[1]<counts[0]*.4,"LOD triangle budgets");
Reject(bytes.Take(4).ToArray(),"truncated header");
Reject(bytes.Take(bytes.Length-1).ToArray(),"truncated mesh");
Reject(bytes.Concat(new byte[]{1}).ToArray(),"trailing data");
byte[] ChangeInt(int at,int value){var copy=(byte[])bytes.Clone();BitConverter.GetBytes(value).CopyTo(copy,at);return copy;}
Reject(ChangeInt(0,0),"signature");Reject(ChangeInt(4,int.MaxValue),"LOD count");
Reject(ChangeInt(8,int.MaxValue),"part allocation");Reject(ChangeInt(12,8),"unknown pivot");
Reject(ChangeInt(16,5),"unknown material");Reject(ChangeInt(20,int.MaxValue),"vertex allocation");
Reject(ChangeInt(24,4),"unaligned triangle count");
Reject(ChangeInt(28,BitConverter.SingleToInt32Bits(float.NaN)),"NaN geometry");
Reject(ChangeInt(28+6*4,BitConverter.SingleToInt32Bits(-1)),"invalid texture coordinate");
int firstIndices=28+BitConverter.ToInt32(bytes,20)*32;
Reject(ChangeInt(firstIndices,int.MaxValue),"out of range triangle");
Console.WriteLine($"Owl model: {checks:N0} checks passed; {counts[0]:N0}/{counts[1]:N0} near/far triangles.");
