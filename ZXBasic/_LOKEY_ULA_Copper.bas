#include "_LOKEY_ULA.bas"

Sub EternalLoop()
	DO
	LOOP
end sub

sub udgbars()
	DIM i as UBYTE
	FOR i = 0 TO 7
		POKE USR "a" + i, BIN 11111111
	NEXT i

	DIM y as UBYTE

	DIM xc,yc as UBYTE

	for yc=0 to 7
		ink yc
		for xc=0 to 7
			print at yc,xc;"\a";
		next xc
	next yc
	for yc=0 to 7
		paper yc
		for xc=0 to 7
			print at yc+8,xc;" ";
		next xc
	next yc
end sub

sub udgbars2()
	DIM i as UBYTE
	DIM chardata(7) as UBYTE =>  {1,3,7,15,31,63,127,255}
	FOR i = 0 TO 7
		POKE USR "a" + i, chardata(i)
	NEXT i

	DIM y as UBYTE

	DIM xc,yc,block as UBYTE
	for block=0 to 2
		for yc=0 to 7
			paper yc
			for xc=0 to 7
				ink xc
				bright 0
				flash 0
				print at yc+block*8,xc;"\a";
			next xc
			for xc=8 to 15
				ink xc
				bright 1
				flash 0
				print at yc+block*8,xc;"\a";
			next xc
			for xc=16 to 23
				ink xc
				bright 0
				flash 1
				print at yc+block*8,xc;"\a";
			next xc
			for xc=24 to 31
				ink xc
				bright 1
				flash 1
				print at yc+block*8,xc;"\a";
			next xc
		next yc
	next block
end sub

Sub Blocks()

	Bright 1
	Paper 7
	Ink 0
	LOKEYULA_Cls()

	udgbars2()

	Pause 100

	DIM y,onoff as UBYTE
	dim cstep as Float
	DIM c1,c2 as UBYTE

	cstep=256/192

	FOR y=0 TO 191
		LOKEYULA_SetCopperIntensity(y,y*cstep)
		Pause 1
	NEXT y

	pause 100

	FOR y=0 TO 191
		LOKEYULA_CopperIntensityRoll(1)
		Pause 1
	NEXT y

	Pause 100

	FOR y=0 TO 191
		LOKEYULA_CopperIntensityShift(1,0)
		Pause 1
	NEXT y

	pause 100

	c1=0
	c2=0
	FOR y=0 TO 191
		LOKEYULA_SetCopperPalette(y,c1)
		c2=c2+1
		if c2=24
			c1=c1+1
			c2=0
		end if
		Pause 1
	NEXT y

	Pause 100

	FOR y=0 TO 191
		LOKEYULA_CopperPaletteRoll(1)
		Pause 1
	NEXT y

	Pause 100

	FOR y=0 TO 191
		LOKEYULA_CopperPaletteShift(1,0)
		Pause 1
	NEXT y

end Sub

Sub Lines()

	Bright 1
	Paper 0
	Ink 0
	Flash 0
	CLS

	DIM tris(6,3,3) as INTEGER
	
	DIM counter as INTEGER = 0
	DIM linenum,pointnum,y,inty1,inty2,coly1,coly2,tempy as UBYTE
	DIM cstep as FLOAT
	inty1=0
	inty2=0
	coly1=0
	coly2=0
	cstep=256/192
	
	'Initial setup
	
	FOR linenum=0 TO 6
		for pointnum =0 to 3
			'0 is x, 1 is y, 2 is xspeed (default 1), 3 is yspeed (default 1)
			tris(linenum,pointnum,0)=Rnd()*256
			tris(linenum,pointnum,1)=Rnd()*192
			tris(linenum,pointnum,2)=Rnd()*10
			tris(linenum,pointnum,3)=Rnd()*10
		next pointnum
	next linenum

	DO
		if counter>200
			'Placeholder to do nothing at all, we're finished
		else
			if inty1<192 AND counter>10
				for tempy=0 TO 7
					LOKEYULA_SetCopperIntensity(inty1,inty1*cstep)
					inty1=inty1+1
				next tempy
			else
				if inty2<192 AND counter>10+24
					for tempy=0 TO 7
						LOKEYULA_SetCopperIntensity(inty2,0)
						inty2=inty2+1
					next tempy
				else
					if coly1<192 AND counter>10+24
						for tempy=0 TO 7
							LOKEYULA_SetCopperPalette(coly1,coly1/24)
							coly1=coly1+1
						next tempy
					else
					end if
				end if
			end if
		end if
		'Update positions
		FOR linenum=0 TO 6
			for pointnum =0 to 3
				'0 is x, 1 is y, 2 is xspeed (default 1), 3 is yspeed (default 1)
				tris(linenum,pointnum,0)=tris(linenum,pointnum,0)+tris(linenum,pointnum,2)
				if tris(linenum,pointnum,0)<0
					tris(linenum,pointnum,0)=0
					tris(linenum,pointnum,2)=tris(linenum,pointnum,2)*-1
				else
					if tris(linenum,pointnum,0)>255
						tris(linenum,pointnum,0)=255
						tris(linenum,pointnum,2)=tris(linenum,pointnum,2)*-1
					end if
				end if
				tris(linenum,pointnum,1)=tris(linenum,pointnum,1)+tris(linenum,pointnum,3)
				if tris(linenum,pointnum,1)<0
					tris(linenum,pointnum,1)=0
					tris(linenum,pointnum,3)=tris(linenum,pointnum,3)*-1
				else
					if tris(linenum,pointnum,1)>191
						tris(linenum,pointnum,1)=191
						tris(linenum,pointnum,3)=tris(linenum,pointnum,3)*-1
					end if
				end if
			next pointnum
		next linenum
		LOKEYULA_Cls()
		'Draw tris
		FOR linenum=0 TO 6
			INK linenum+1
			LOKEYULA_Triangle(tris(linenum,0,0),tris(linenum,0,1),tris(linenum,1,0),tris(linenum,1,1),tris(linenum,2,0),tris(linenum,2,1))
		next linenum
		counter=counter+1
	LOOP	
end Sub

Blocks()

Pause 50

Lines()

EternalLoop()
