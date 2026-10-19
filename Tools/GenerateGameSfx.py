import math,random,wave,array,uuid
from pathlib import Path
root=Path(__file__).resolve().parents[1]/'Assets/EmergenceLevel01/Audio/Clips';root.mkdir(parents=True,exist_ok=True)
specs={
'Footstep':(.105,180,95,.55),'Jump':(.24,280,900,.03),'DoubleJump':(.32,470,1500,.02),'Land':(.16,130,48,.48),
'SwitchStart':(.35,180,650,.06),'Joy':(.36,660,990,.0),'Anger':(.29,190,95,.23),'Sadness':(.5,510,260,.0),'Courage':(.4,330,660,.02),'Guardian':(.46,260,520,.0),'Memory':(.65,420,580,.0),
'Transfer':(.42,380,1150,.02),'Resonance':(.6,440,880,.0),'Fragment':(.4,1000,1600,.0),'Death':(.55,580,65,.24),
'BreakFragile':(.23,360,100,.85),'BreakReinforced':(.39,180,45,.9),'Vanish':(.46,720,210,.15),'Restore':(.38,220,780,.08),
'DoorOpen':(.58,110,260,.52),'DoorClose':(.48,210,60,.6),'SwitchPress':(.14,420,180,.3),'AssemblyPlace':(.22,300,700,.2),'Checkpoint':(.65,520,1040,.0),'Portal':(.65,240,1200,.12),'DialogueLine':(.075,680,610,.0),'SceneTransition':(.7,150,440,.2),'MemoryEnter':(.85,300,620,.08),'MemoryReturn':(.7,620,320,.05)}
rate=44100;rendered={}
def synth(name,dur,f0,f1,noise,variation=0):
 rng=random.Random(721+sum(map(ord,name))+variation);out=[];phase=0;filtered=0
 for i in range(int(rate*dur)):
  t=i/rate;x=t/dur
  env=min(1,t/.008)*min(1,(dur-t)/.035)*(1-x)**(.9 if 'Memory' in name else 1.6)
  freq=f0+(f1-f0)*x
  phase+=2*math.pi*freq/rate
  filtered=.72*filtered+.28*rng.uniform(-1,1)
  tone=math.sin(phase)+.22*math.sin(phase*2.01)
  if name in ['Joy','Guardian','Resonance','Checkpoint','Memory','MemoryEnter','MemoryReturn']:tone+=.35*math.sin(phase*1.5)+.22*math.sin(phase*1.25)
  if name in ['Transfer','SwitchStart']:tone*=.65+.35*math.sin(2*math.pi*16*t)
  out.append(env*((1-noise)*tone+noise*filtered*3))
 peak=max(abs(x) for x in out) or 1;return [int(x/peak*.65*32767) for x in out]
def save(path,samples):
 with wave.open(str(path),'wb') as w:w.setnchannels(1);w.setsampwidth(2);w.setframerate(rate);w.writeframes(array.array('h',samples).tobytes())
for name,spec in specs.items():
 samples=synth(name,*spec);rendered[name]=samples;save(root/(name+'.wav'),samples)
 if name=='Footstep':
  for v in [1,2]:save(root/(name+'_'+str(v)+'.wav'),synth(name,spec[0]*(1+v*.08),spec[1]*(1-v*.06),spec[2],spec[3],v))
preview=[]
order=['Footstep','Jump','Land','Joy','Anger','Sadness','Guardian','Transfer','Fragment','BreakFragile','DoorOpen','MemoryEnter']
for name in order:preview+=rendered[name]+[0]*int(rate*.2)
previewdir=Path(__file__).resolve().parents[1]/'Logs/Audio';previewdir.mkdir(parents=True,exist_ok=True);save(previewdir/'音效试听.wav',preview)
for f in root.glob('*.wav'):
 meta=Path(str(f)+'.meta')
 if not meta.exists():meta.write_text('fileFormatVersion: 2\nguid: '+uuid.uuid4().hex+'\n',encoding='utf-8')
print('Generated',len(list(root.glob('*.wav'))),'mono PCM clips; preview seconds',len(preview)/rate)
