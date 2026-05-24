'LOKEY_FPU DATAPORT
#define LOKEYFPUI_DATAPORT 133
'LOKEY_FPU COMMAND PORT
#define LOKEYFPUI_COMMANDPORT 135

'LOKEY_FPU Virtual Function Table

#define LOKEYFPUI_RNDBYTES 20
#define LOKEYFPUI_RNDFLOAT 21
#define LOKEYFPUI_RNDFIXED 22

#define LOKEYFPUI_FLOATADD 50
#define LOKEYFPUI_FLOATSUB 51
#define LOKEYFPUI_FLOATMUL 52
#define LOKEYFPUI_FLOATDIV 53
#define LOKEYFPUI_FLOATPOW 54
#define LOKEYFPUI_FLOATEXP 55
#define LOKEYFPUI_FLOATLN 56
#define LOKEYFPUI_FLOATSIN 57
#define LOKEYFPUI_FLOATCOS 58
#define LOKEYFPUI_FLOATTAN 59
#define LOKEYFPUI_FLOATASN 60
#define LOKEYFPUI_FLOATACS 61
#define LOKEYFPUI_FLOATATN 62
#define LOKEYFPUI_FLOATSQRT 63
#define LOKEYFPUI_FLOATABS 64
#define LOKEYFPUI_FLOATCOMPARE 65

#define LOKEYFPUI_FLOATMULADD 66
#define LOKEYFPUI_FLOATMULSUB 67
#define LOKEYFPUI_FLOATDIVADD 68
#define LOKEYFPUI_FLOATDIVSUB 69

#define LOKEYFPUI_FLOATINRANGE 70
#define LOKEYFPUI_NORMALIZEV3 71
#define LOKEYFPUI_RAYSPHEREHIT 72
#define LOKEYFPUI_FLOATROTATE2D 73

#define LOKEYFPUI_DOTPRODUCTV3 74

#define LOKEYFPUI_EVALUATEEXPRESSION 240

Sub LOKEYFPU_RndBytes(count as UBYTE)
	'Requests count bytes, max count 16
	'Results must be read from LOKEYFPUI_DATAPORT with IN
	OUT LOKEYFPUI_DATAPORT,count
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_RNDBYTES
end sub

Sub LOKEYFPU_RndFloat(fltdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fltdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fltdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_RNDFLOAT
end sub

Sub LOKEYFPU_RndFixed(fixdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fixdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fixdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_RNDFIXED
end sub

Sub LOKEYFPU_FloatAdd(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATADD
end sub

Sub LOKEYFPU_FloatSub(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATSUB
end sub
Sub LOKEYFPU_FloatMul(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATMUL
end sub
Sub LOKEYFPU_FloatDiv(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATDIV
end sub
Sub LOKEYFPU_FloatPow(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATPOW
end sub
Sub LOKEYFPU_FloatExp(fpsourceaddress1 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATEXP
end sub
Sub LOKEYFPU_FloatLn(fpsourceaddress1 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATLN
end sub
Sub LOKEYFPU_FloatSin(fpsourceaddress1 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATSIN
end sub
Sub LOKEYFPU_FloatCos(fpsourceaddress1 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATCOS
end sub
Sub LOKEYFPU_FloatTan(fpsourceaddress1 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATTAN
end sub
Sub LOKEYFPU_FloatAsin(fpsourceaddress1 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATASN
end sub
Sub LOKEYFPU_FloatAcos(fpsourceaddress1 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATACS
end sub
Sub LOKEYFPU_FloatAtan(fpsourceaddress1 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATATN
end sub
Sub LOKEYFPU_FloatSqrt(fpsourceaddress1 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATSQRT
end sub
Sub LOKEYFPU_FloatAbs(fpsourceaddress1 as UINTEGER,fpdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATABS
end sub
Sub LOKEYFPU_FloatCompare(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,ubdestinationaddress as UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@ubdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@ubdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATCOMPARE
end sub

Sub LOKEYFPU_FloatDotProductV3(fpvector3address1 as UINTEGER,fpvector3address2 as UINTEGER,fpdestinationaddress as UINTEGER)
	'vector1 * vector2 into floatdest, assumes all values are float and vec x,y,z are sequential in memory
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpvector3address1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpvector3address1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpvector3address2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpvector3address2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_DOTPRODUCTV3
end sub

SUB LOKEYFPU_NormalizeV3(vector3ptr AS UINTEGER, vector3resultptr AS UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@vector3ptr)
	OUT LOKEYFPUI_DATAPORT,PEEK (@vector3ptr+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@vector3resultptr)
	OUT LOKEYFPUI_DATAPORT,PEEK (@vector3resultptr+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_NORMALIZEV3
END SUB

SUB LOKEYFPU_FloatRaySphereHit(originptr AS UINTEGER, directionptr AS UINTEGER, spherepositionptr AS UINTEGER, sphereradiusptr AS UINTEGER, nearhitptr AS UINTEGER, farhitptr AS UINTEGER)
	OUT LOKEYFPUI_DATAPORT,PEEK (@originptr)
	OUT LOKEYFPUI_DATAPORT,PEEK (@originptr+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@directionptr)
	OUT LOKEYFPUI_DATAPORT,PEEK (@directionptr+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@spherepositionptr)
	OUT LOKEYFPUI_DATAPORT,PEEK (@spherepositionptr+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@sphereradiusptr)
	OUT LOKEYFPUI_DATAPORT,PEEK (@sphereradiusptr+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@nearhitptr)
	OUT LOKEYFPUI_DATAPORT,PEEK (@nearhitptr+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@farhitptr)
	OUT LOKEYFPUI_DATAPORT,PEEK (@farhitptr+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_RAYSPHEREHIT
END SUB

Sub LOKEYFPU_FloatMulAdd(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpsourceaddress3 as UINTEGER,fpdestinationaddress as UINTEGER)
	'a*b+c Fused Mul Add (FMA)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATMULADD
end sub
Sub LOKEYFPU_FloatMulSub(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpsourceaddress3 as UINTEGER,fpdestinationaddress as UINTEGER)
	'a*b-c Fused Mul Sub (FMS)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATMULSUB
end sub

Sub LOKEYFPU_FloatDivAdd(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpsourceaddress3 as UINTEGER,fpdestinationaddress as UINTEGER)
	'a/b+c Fused Div Add (FDA)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATDIVADD
end sub
Sub LOKEYFPU_FloatDivSub(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpsourceaddress3 as UINTEGER,fpdestinationaddress as UINTEGER)
	'a/b-c Fused Div Sub (FDS)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATDIVSUB
end sub

Sub LOKEYFPU_FloatInRange(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpsourceaddress3 as UINTEGER,fpdestinationaddress as UINTEGER)
	'(1*2)+(3*4)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATINRANGE
end sub

Sub LOKEYFPU_FloatRotate2D(fpsourceaddress1 as UINTEGER,fpsourceaddress2 as UINTEGER,fpsourceaddress3 as UINTEGER,fpdestinationaddress1 as UINTEGER,fpdestinationaddress2 as UINTEGER)
	'p4=p1*cos(p3)−p2*sin(p3),p5=p1*sin(p3)+p2*cos(p3)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress2+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpsourceaddress3+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress1+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress2)
	OUT LOKEYFPUI_DATAPORT,PEEK (@fpdestinationaddress2+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_FLOATROTATE2D
end sub

Sub LOKEYFPU_EvaluateExpression(expressionstringaddress as UINTEGER,parametercount as UBYTE,parametersbaseaddress as UINTEGER,destinationfloataddress as UINTEGER)
	'//expression string address, parameter count, parameters base address, destination address
	OUT LOKEYFPUI_DATAPORT,PEEK (@expressionstringaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@expressionstringaddress+1)
	OUT LOKEYFPUI_DATAPORT,parametercount
	OUT LOKEYFPUI_DATAPORT,PEEK (@parametersbaseaddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@parametersbaseaddress+1)
	OUT LOKEYFPUI_DATAPORT,PEEK (@destinationfloataddress)
	OUT LOKEYFPUI_DATAPORT,PEEK (@destinationfloataddress+1)
	OUT LOKEYFPUI_COMMANDPORT,LOKEYFPUI_EVALUATEEXPRESSION
end sub