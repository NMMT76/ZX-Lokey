#include "_LOKEY_ULA.bas"

Sub Test_ZX_Print()
	Bright 1
	Paper 7
	Ink 0
	CLS
	DIM n as UBYTE
	FOR n=32 TO 127
		PRINT CHR$ n;
	NEXT n
	Pause(100)
end sub

Sub Test_LOKEY_PrintChar()
	Bright 1
	Paper 7
	Ink 0
	CLS
	DIM y as UBYTE
	DIM x as UINTEGER
	DIM count as UBYTE
	count=32
	WHILE count<128
		LOKEYULA_PrintChar(x,y,count,0)
		x=x+8
		if x>248
			y=y+8
			x=0
		end if
		count=count+1
	end while
	Pause(100)
end sub

Sub Test_LOKEY_PrintCharMasked()
	Bright 1
	Paper 7
	Ink 0
	CLS
	DIM y as UBYTE
	DIM x as UINTEGER
	DIM count as UBYTE
	count=32
	WHILE count<128
		LOKEYULA_PrintChar(x,y,count,1)
		x=x+6
		if x>248
			y=y+8
			x=0
		end if
		count=count+1
	end while
	Pause(100)
end sub

Sub Test_LOKEY_Circle_FilledPattern()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim ic as UBYTE
	DIM radius as UINTEGER
	ic=1
	FOR radius=1 to 90 STEP 2
		Ink ic
		LOKEYULA_CircleFilled(128,96,radius,LOKEYULAI_FILLOVERDRAW bOR LOKEYULAI_FILLPATTERN bOR Rnd()*3)
		Pause 1
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
	Pause(100)
end sub

'Test_ZX_Print()
Test_LOKEY_PrintChar()
Test_LOKEY_PrintCharMasked()
'Test_LOKEY_Circle_Filled()
'Test_LOKEY_Circle_FilledPattern()
DO
LOOP