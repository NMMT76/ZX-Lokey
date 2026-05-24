#include "_LOKEY_ULA.bas"

Sub Test_ZX_Rectangle()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim ic as UBYTE
	DIM length as UINTEGER
	ic=1
	FOR length=1 to 191 STEP 4
		Ink ic
		Plot 0,0
		Draw 0,length-1
		Draw length-1,0
		Draw 0,-(length-1)
		Draw -(length-1),0
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
	Pause(100)
end sub

Sub Test_LOKEY_Rectangle()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim ic as UBYTE
	DIM length as UINTEGER
	ic=1
	FOR length=1 to 191 STEP 4
		Ink ic
		LOKEYULA_Rectangle(0,0,length-1,length-1)
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
	Pause(100)
end sub

Sub Test_LOKEY_Rectangle_Filled()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim ic as UBYTE
	DIM length as UINTEGER
	ic=1
	FOR length=1 to 191 STEP 4
		Ink ic
		'ULAZXI_FILLOVERDRAW
		LOKEYULA_RectangleFilled(0,0,length-1,length-1,0)
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
	Pause(100)
end sub

Sub Test_LOKEY_Rectangle_FilledPattern()
	Bright 1
	Paper 0
	Ink 0
	CLS
	Dim ic as UBYTE
	DIM length as UINTEGER
	ic=1
	FOR length=1 to 191 STEP 4
		Ink ic
		LOKEYULA_RectangleFilled(0,0,length-1,length-1,LOKEYULAI_FILLOVERDRAW bOR LOKEYULAI_FILLPATTERN bOR Rnd()*3)
		ic=ic+1
		IF ic>7
			ic=1
		end if
	NEXT
	Pause(100)
end sub

Test_ZX_Rectangle()
Test_LOKEY_Rectangle()
Test_LOKEY_Rectangle_Filled()
'Test_LOKEY_Rectangle_FilledPattern()

DO
LOOP