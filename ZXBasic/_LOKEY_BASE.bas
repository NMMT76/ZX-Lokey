Function Ticks() as ULong
	Dim b1,b2,b3 AS Ubyte
	b1=peek(UByte,23674)
	b2=peek(UByte,23673)
	b3=peek(UByte,23672)
	return 65536*b1+256*b2+ b3
END Function

sub WaitKey()
	DIM key AS STRING
	key=""
	WHILE key=""
		key=inkey()
	END WHILE
	Pause(1)
	WHILE key<>""
		key=inkey()
	END WHILE
end sub