"""Recreate licensed student assets: python3 Tools/import_student.py pack.zip (requires Inkscape)."""
from pathlib import Path
import zipfile,uuid,hashlib,xml.etree.ElementTree as E,subprocess,sys
root=Path(__file__).resolve().parents[1];out=root/'Assets/CEVR/Resources/Student';out.mkdir(parents=True,exist_ok=True)
z=zipfile.ZipFile(sys.argv[1])
for src,dst in [('Model/characterMedium.fbx','Student.fbx'),('Animations/idle.fbx','Idle.fbx'),('Animations/run.fbx','Run.fbx')]:
 (out/dst).write_bytes(z.read(src))
svg=z.read('Skins/Source/criminalMaleA.svg').decode();svg=svg.replace('#009F78','#E9EBEF').replace('#037E60','#B6BDC8')
# Edit the supplied vector source: remove the moustache and scar paths, recolor shoe uppers.
tree=E.fromstring(svg);ns='{http://www.w3.org/2000/svg}'
for g in tree.iter():
 for child in list(g):
  d=child.get('d','')
  if d.startswith('M323.25 250.85') or d.startswith('M368.65 244.6'):g.remove(child)
  elif child.get('fill')=='#FFFFFF' and ('M716.2 386.35' in d or 'M716.2 211.35' in d):child.set('fill','#252831')
tree.set('width','1024');tree.set('height','1024');tree.set('viewBox','0 0 1024 1024')
E.SubElement(tree,ns+'path',{'fill':'#242A38','d':'M613 764 H1024 V1024 H613 Z'})
E.SubElement(tree,ns+'path',{'fill':'none','stroke':'#353D50','stroke-width':'3','d':'M625 790 H1015 M818 790 V1024'})
E.ElementTree(tree).write(out/'StudentUniform.svg',encoding='unicode')
subprocess.run(['inkscape',str(out/'StudentUniform.svg'),'--export-filename='+str(out/'StudentUniform.png'),'--export-width=1024','--export-height=1024'],check=True,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
(root/'docs/third-party/KENNEY_CHARACTER_LICENSE.txt').write_text(z.read('License.txt').decode().strip()+'\n')
(root/'docs/third-party/CHARACTER_SOURCE.md').write_text('''# Student character provenance

Mesh and idle/run animation: Kenney Animated Characters Protagonists 1.1 (CC0).
Source: https://kenney.nl/assets/animated-characters-protagonists
Archive SHA256: '''+hashlib.sha256(Path(sys.argv[1]).read_bytes()).hexdigest()+'''

StudentUniform.svg is an edited vector skin from the pack: white/grey collar, dark trousers, dark footwear and removal of moustache/scar. PNG is rendered from that SVG with Inkscape. This is a stylized student-inspired uniform, without official university insignia; it is not a photorealistic or anatomically realistic person.
''')
for p in [out,*out.iterdir()]:
 meta=Path(str(p)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n')
