#include "_LOKEY_ULA.bas"

Sub Test_ZX_Circle()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim ic as UBYTE
	DIM radius as UINTEGER
	ic=1
	FOR radius=1 to 90 STEP 2
		Ink ic
		Circle 128,96,radius
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
	Pause(100)
end sub

Sub Test_LOKEY_Circle()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim ic as UBYTE
	DIM radius as UINTEGER
	ic=1
	FOR radius=1 to 90 STEP 2
		Ink ic
		LOKEYULA_Circle(128,96,radius)
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
	Pause(100)
end sub

Sub Test_LOKEY_Circle_Filled()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim ic as UBYTE
	DIM radius as UINTEGER
	ic=1
	FOR radius=1 to 90 STEP 2
		Ink ic
		LOKEYULA_CircleFilled(128,96,radius,LOKEYULAI_FILLOVERDRAW)
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
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
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
	Pause(100)
end sub

Test_ZX_Circle()
Test_LOKEY_Circle()
Test_LOKEY_Circle_Filled()
'Test_LOKEY_Circle_FilledPattern()
DO
LOOP