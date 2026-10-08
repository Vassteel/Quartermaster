"""Run actual material selection and art initialization with stripped-shader fixtures."""
import os,subprocess,tempfile
from xml.sax.saxutils import escape
from pathlib import Path
root=Path(__file__).resolve().parents[2]
xml_root=escape(str(root))
art=(root/'src/ApothecaryArt.cs').read_text()
# Execute the actual resource reader and Initialize/Release methods; omit unrelated
# Unity scene construction methods. Resources are the exact shipped binary/PNGs.
art=art[:art.index('    internal static Vector3[] Slots')]+art[art.index('    internal static void Release()'):]
with tempfile.TemporaryDirectory(prefix='quartermaster-materials-') as tmp:
 p=Path(tmp)
 p.joinpath('ArtInitialization.cs').write_text(art)
 for name in ['Program.cs','UnityStubs.cs']:p.joinpath(name).write_text((root/'tests/FurnitureMaterials'/name).read_text())
 p.joinpath('Materials.csproj').write_text(f'''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><ImplicitUsings>enable</ImplicitUsings></PropertyGroup><ItemGroup>
 <Compile Include="{xml_root}/src/StorageMaterials.cs" Link="StorageMaterials.cs"/>
 <EmbeddedResource Include="{xml_root}/assets/apothecary/model.bin" LogicalName="Quartermaster.Apothecary.model"/>
 <EmbeddedResource Include="{xml_root}/assets/apothecary/*.png" LogicalName="Quartermaster.Apothecary.%(Filename)"/>
 </ItemGroup></Project>''')
 subprocess.run([os.environ.get('DOTNET','dotnet'),'run','--project',str(p/'Materials.csproj'),'-c','Release'],check=True)
