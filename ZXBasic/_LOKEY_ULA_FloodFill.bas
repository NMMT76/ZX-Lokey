#include "_LOKEY_ULA.bas"

Sub Test_LOKEY_FloodFill()
	Paper 7
	Ink 0
	CLS
	LOKEYULA_Rectangle(20,20,60,60)
	pause(100)
	Ink 2
	LOKEYULA_FloodFill(30,30)
	Pause(5)
end sub

Test_LOKEY_FloodFill
DO
LOOP