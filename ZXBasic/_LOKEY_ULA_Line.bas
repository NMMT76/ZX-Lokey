#include "_LOKEY_ULA.bas"

Sub Test_ZX_Line()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Pause(50)
	Dim ic as UBYTE
	DIM x as UINTEGER
	ic=1
	FOR x=0 to 255 STEP 8
		Ink ic
		Plot x,0
		Draw 255-x*2,191
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
	Pause(100)
end sub

Sub Test_LOKEYULA_Line()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Pause(50)
	Dim ic as UBYTE
	DIM x as UINTEGER
	ic=1
	FOR x=0 to 255 STEP 8
		Ink ic
		LOKEYULA_Line(x,0,255-x,191)
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
	Pause(100)
end sub

Test_ZX_Line()
Test_LOKEYULA_Line()
DO
LOOP