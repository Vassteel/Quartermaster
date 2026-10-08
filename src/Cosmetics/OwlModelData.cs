using System;
using System.Collections.Generic;
using System.IO;

namespace Quartermaster.Cosmetics;

// Versioned, bounded asset reader kept independent of Unity for build validation.
internal sealed class OwlModelData
{
    internal sealed class Part
    {
        internal int Pivot, Material;
        internal float[] Vertices; // position XYZ, outward normal XYZ, atlas UV
        internal int[] Triangles;
    }
    internal readonly List<Part[]> Levels = new List<Part[]>();
    internal static OwlModelData Read(Stream stream, bool tiledUv = false, int materialCount = 4)
    {
        if(materialCount<1||materialCount>8)throw new ArgumentOutOfRangeException(nameof(materialCount));
        using(var reader=new BinaryReader(stream))
        {
            if(new string(reader.ReadChars(4))!="QMO1" || reader.ReadInt32()!=2)
                throw new InvalidDataException("Invalid owl model header.");
            var result=new OwlModelData();int totalVertices=0,totalIndices=0;
            for(int level=0;level<2;level++)
            {
                int count=reader.ReadInt32();
                if(count<1||count>32)throw new InvalidDataException("Invalid owl part count.");
                var parts=new Part[count];var keys=new HashSet<int>();
                for(int p=0;p<count;p++)
                {
                    var part=new Part {Pivot=reader.ReadInt32(),Material=reader.ReadInt32()};
                    int vertices=reader.ReadInt32(),indices=reader.ReadInt32();
                    if(part.Pivot<0||part.Pivot>7||part.Material<0||part.Material>=materialCount||!keys.Add(part.Pivot*materialCount+part.Material))
                        throw new InvalidDataException("Invalid owl pivot or material.");
                    if(vertices<3||vertices>60000||indices<3||indices>180000||indices%3!=0)
                        throw new InvalidDataException("Invalid owl geometry size.");
                    totalVertices+=vertices;totalIndices+=indices;
                    if(totalVertices>100000||totalIndices>300000)throw new InvalidDataException("Owl asset exceeds budget.");
                    part.Vertices=new float[vertices*8];part.Triangles=new int[indices];
                    for(int i=0;i<part.Vertices.Length;i++)
                    {
                        float value=reader.ReadSingle();
                        if(float.IsNaN(value)||float.IsInfinity(value)||Math.Abs(value)>4)
                            throw new InvalidDataException("Invalid owl vertex value.");
                        part.Vertices[i]=value;
                    }
                    for(int i=0;i<vertices;i++)
                    {
                        int j=i*8;float x=part.Vertices[j+3],y=part.Vertices[j+4],z=part.Vertices[j+5];
                        float norm=x*x+y*y+z*z;
                        // Furniture uses repeating native wood UVs; the finite +/-4 bound above still applies.
                        if(norm<.8f||norm>1.2f||(!tiledUv&&(part.Vertices[j+6]<0||part.Vertices[j+6]>1||part.Vertices[j+7]<0||part.Vertices[j+7]>1)))
                            throw new InvalidDataException("Invalid owl normal or UV.");
                    }
                    for(int i=0;i<indices;i++)
                    {
                        int index=reader.ReadInt32();if(index<0||index>=vertices)throw new InvalidDataException("Invalid owl index.");
                        part.Triangles[i]=index;
                    }
                    parts[p]=part;
                }
                result.Levels.Add(parts);
            }
            if(stream.ReadByte()!=-1)throw new InvalidDataException("Trailing owl model data.");
            return result;
        }
    }
}
