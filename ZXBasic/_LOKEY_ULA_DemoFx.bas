#include "_LOKEY_ULA.bas"

Sub Test_ZX_Plot()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Pause(50)
	Dim x,y,row,col,ic,pc as UBYTE
	ic=0
	pc=0
	FOR row=0 to 23
		FOR col=0 TO 31
			Paper pc
			Ink ic
			x=col*8
			y=row*8
			Plot x+3,y+3
			Plot x+3,y+4
			Plot x+4,y+3
			Plot x+4,y+4
			ic=ic+1
			IF ic>7
				ic=0
			end if
		NEXT
		pc=pc+1
		if pc>7
			pc=0
		end if
	NEXT
	Pause(100)
end sub

Sub Test_LOKEYULA_Plot()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Pause(50)
	Dim row,col,ic,pc,x,y as UBYTE
	ic=0
	pc=0
	FOR row=0 to 23
		FOR col=0 TO 31
			Paper pc
			Ink ic
			x=col*8
			y=row*8
			LOKEYULA_Plot(x+3,y+3)
			LOKEYULA_Plot(x+3,y+4)
			LOKEYULA_Plot(x+4,y+3)
			LOKEYULA_Plot(x+4,y+4)
			ic=ic+1
			IF ic>7
				ic=0
			end if
		NEXT
		pc=pc+1
		if pc>7
			pc=0
		end if
	NEXT
	Pause(100)
end sub


Sub Test_LOKEYULA_Oscillators()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Pause(50)
	
	
	
	s=128
	t=0
	for i=0 to numdots
		x = x + y / 2
		y = y - x / 2
		plotPixel(x,y,color)
	next
	
	Dim row,col,ic,pc,x,y as UBYTE
	ic=0
	pc=0
	FOR row=0 to 23
		FOR col=0 TO 31
			Paper pc
			Ink ic
			x=col*8
			y=row*8
			LOKEYULA_Plot(x+3,y+3)
			LOKEYULA_Plot(x+3,y+4)
			LOKEYULA_Plot(x+4,y+3)
			LOKEYULA_Plot(x+4,y+4)
			ic=ic+1
			IF ic>7
				ic=0
			end if
		NEXT
		pc=pc+1
		if pc>7
			pc=0
		end if
	NEXT
	Pause(100)
end sub




DO
LOOP