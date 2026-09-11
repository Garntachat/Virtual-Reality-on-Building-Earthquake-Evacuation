"""Convert the licensed Kenney Furniture Kit OBJ/MTL archive to Unity mesh data.
Usage: python3 Tools/import_kenney_furniture.py /path/to/kenney_furniture-kit.zip
Source: https://kenney.nl/assets/furniture-kit (CC0).
"""
import sys, json, zipfile, hashlib, uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
MODELS='chairCushion chairDesk table desk loungeSofaLong tableCoffee bookcaseOpen kitchenCabinet kitchenSink kitchenStove kitchenFridge computerScreen computerKeyboard laptop pottedPlant lampRoundFloor lampSquareCeiling books pillowBlue cabinetTelevision televisionModern rugRectangle'.split()
def main(archive):
 z=zipfile.ZipFile(archive); out=ROOT/'Assets/CEVR/Resources/Furniture';out.mkdir(parents=True,exist_ok=True)
 for name in MODELS:
  stem='Models/OBJ format/'+name
  colors={}; current=None
  for line in z.read(stem+'.mtl').decode().splitlines():
   t=line.split()
   if not t:continue
   if t[0]=='newmtl':current=t[1]
   elif t[0]=='Kd':colors[current]=list(map(float,t[1:4]))
  pos=[]; triangles={};material=None
  for line in z.read(stem+'.obj').decode().splitlines():
   t=line.split()
   if not t:continue
   if t[0]=='v':pos.append(list(map(float,t[1:4])))
   elif t[0]=='usemtl':material=t[1]
   elif t[0]=='f':
    ids=[int(v.split('/')[0])-1 for v in t[1:]]
    for i in range(1,len(ids)-1):triangles.setdefault(material,[]).extend([ids[0],ids[i+1],ids[i]])
  low=[min(v[a] for v in pos) for a in range(3)];high=[max(v[a] for v in pos) for a in range(3)]
  verts=[]
  for v in pos:
   verts.extend([round(-((v[0]-low[0])/(high[0]-low[0])-.5),6),round((v[1]-low[1])/(high[1]-low[1]),6),round((v[2]-low[2])/(high[2]-low[2])-.5,6)])
  data={'vertices':verts,'parts':[{'color':colors.get(m,[.6,.6,.6]),'triangles':ts} for m,ts in triangles.items()]}
  (out/(name+'.json')).write_text(json.dumps(data,separators=(',',':'))+'\n')
 license_dir=ROOT/'docs/third-party';license_dir.mkdir(exist_ok=True)
 (license_dir/'KENNEY_FURNITURE_LICENSE.txt').write_text(z.read('License.txt').decode().replace('\r','').replace('\t',''))
 (license_dir/'FURNITURE_SOURCE.md').write_text('# Furniture source\n\nKenney Furniture Kit: https://kenney.nl/assets/furniture-kit\n\nLicense: CC0. Models are converted from original OBJ/MTL data, triangulated and normalized. Original material diffuse colors retained. Conversion script: Tools/import_kenney_furniture.py.\n\nDownloaded archive SHA256: '+hashlib.sha256(Path(archive).read_bytes()).hexdigest()+'\n')
 for p in (ROOT/'Assets/CEVR').rglob('*'):
  if not p.name.endswith('.meta') and not Path(str(p)+'.meta').exists():Path(str(p)+'.meta').write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n'+('folderAsset: yes\n' if p.is_dir() else ''))
 print('Imported',len(MODELS),'CC0 furniture models')
if __name__=='__main__':main(sys.argv[1])
