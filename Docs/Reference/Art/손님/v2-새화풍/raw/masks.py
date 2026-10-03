from PIL import Image
W,H=64,80
def mk(fn):
    m=Image.new('RGB',(W,H),(0,0,0)); p=m.load()
    for y in range(H):
        for x in range(W):
            if fn(x,y): p[x,y]=(255,255,255)
    return m
face_keep=lambda x,y: 21<=x<=42 and 23<=y<=34
neck=lambda x,y: 27<=x<=36 and y==36
top=lambda x,y: (37<=y<=56 and 15<=x<=48) or (57<=y<=60 and (15<=x<=23 or 40<=x<=48))
bottom=lambda x,y: (57<=y<=59 and 24<=x<=39) or (60<=y<=76 and 19<=x<=44)
hair=lambda x,y: 0<=y<=56 and 4<=x<=59 and not face_keep(x,y) and not neck(x,y) and not top(x,y)
face=lambda x,y: 21<=x<=42 and 23<=y<=34
for n,f in [('hair',hair),('top',top),('bottom',bottom),('face',face)]:
    mk(f).save('mask_%s.png'%n, optimize=True)
hairlong=lambda x,y: hair(x,y) or (37<=y<=58 and (11<=x<=22 or 41<=x<=52))
mk(hairlong).save('mask_hairlong.png', optimize=True)
