#include <asc.bas>
#include "_LOKEY_FPU.bas"

'LOKEYFPU_EvaluateExpression(expressionstringaddress as UINTEGER,parametercount as UBYTE,parametersbaseaddress as UINTEGER,destinationfloataddress as UINTEGER)

DIM expressionstring as String
DIM expressionstringarray(256) as UBYTE
DIM parametercount as UBYTE
DIM parametersbaseaddress(98) as UINTEGER
DIM destinationfloat as Float
DIM floats(5) as Float

DIM charindex,charcount as UBYTE


floats(0)=1.0
floats(1)=2.0
floats(2)=3.0
floats(3)=1.0
floats(4)=1.0
floats(5)=1.0

parametersbaseaddress(0)=@floats(0)
parametersbaseaddress(1)=@floats(1)
parametersbaseaddress(2)=@floats(2)
parametersbaseaddress(3)=@floats(3)
parametersbaseaddress(4)=@floats(4)
parametersbaseaddress(5)=@floats(5)

expressionstring="Sin([par00])+Cos([par01])+Tan([par02])+Asin([par03])+Acos([par04])+Atan([par05])"
charcount=Len(expressionstring)-1

'Copy string to array
FOR charindex=0 TO charcount
	expressionstringarray(charindex)=asc(expressionstring,charindex)
NEXT charindex
expressionstringarray(charcount+1)=0 'Zero terminated

Print @expressionstring
Print expressionstring

LOKEYFPU_EvaluateExpression(@expressionstringarray(0),6,@parametersbaseaddress(0),@destinationfloat)
LOKEYFPU_EvaluateExpression(@expressionstringarray(0),6,@parametersbaseaddress(0),@destinationfloat)

print Str(destinationfloat)

DO
LOOP