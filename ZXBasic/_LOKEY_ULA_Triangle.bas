#include "_LOKEY_ULA.bas"

Sub Test_ZX_Triangle()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim x,y as UINTEGER
	Dim ic as UBYTE
	ic=1
	For y=0 TO 150 STEP 25
		FOR x=0 to 240 STEP 10
			Ink ic
			Plot x,y
			Draw 9,0
			Draw -4,25
			Draw -4,-25
			ic=ic+1
			IF ic>7
				ic=1
			end if
		NEXT
	NEXT
	Pause(100)
end sub

Sub Test_LOKEY_Triangle()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim x,y as UINTEGER
	Dim ic as UBYTE
	ic=1
	For y=0 TO 150 STEP 25
		FOR x=0 to 240 STEP 10
			Ink ic
			LOKEYULA_Triangle(x,y,x+9,y,x+4,y+25)
			ic=ic+1
			IF ic>7
				ic=1
			end if
		NEXT
	NEXT
	Pause(100)
end sub

Sub Test_LOKEY_Triangle_Filled()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim x,y as UINTEGER
	Dim ic as UBYTE
	ic=1
	For y=0 TO 150 STEP 25
		FOR x=0 to 240 STEP 10
			Ink ic
			LOKEYULA_TriangleFilled(x,y,x+9,y,x+4,y+25,LOKEYULAI_FILLOVERDRAW)
			ic=ic+1
			IF ic>7
				ic=1
			end if
		NEXT
	NEXT
	Pause(100)
end sub

Sub Test_LOKEY_Triangle_FilledPattern()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim x,y as UINTEGER
	Dim ic as UBYTE
	ic=1
	For y=0 TO 150 STEP 25
		FOR x=0 to 240 STEP 10
			Ink ic
			LOKEYULA_TriangleFilled(x,y,x+9,y,x+4,y+25,LOKEYULAI_FILLOVERDRAW bOR LOKEYULAI_FILLPATTERN bOR Rnd()*3)
			ic=ic+1
			IF ic>7
				ic=1
			end if
		NEXT
	NEXT
	Pause(100)
end sub

Test_ZX_Triangle()
Test_LOKEY_Triangle()
Test_LOKEY_Triangle_Filled()
'Test_LOKEY_Triangle_FilledPattern()
DO
LOOP