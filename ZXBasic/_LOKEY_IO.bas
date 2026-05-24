'Include for the Lokey IO expansion device
'Unless otherwise stated, always assume that
'source/destination is 0 ROM 0, 1 ROM 1, 2 DISK, 3 ZX RAM, 4 SLOWRAM
'For obvious reasons, you can't write to ROM ;)

'LOKEYIO DATAPORT
#define LOKEYIOI_DATAPORT 137
'LOKEYIO COMMAND PORT
#define LOKEYIOI_COMMANDPORT 139

'LOKEY_IO Virtual Function Table

#define LOKEYIO_LOADSNAROM0 0
#define LOKEYIO_LOADSNAROM1 1
#define LOKEYIO_LOADSNADISK 2

#define LOKEYULAI_MEMCOPY 50
#define LOKEYULAI_MEMFILL 51
#define LOKEYULAI_MEMFILLRANDOM 52

Sub LOKEYIO_MemCopy(source as UBYTE, destination as UBYTE, sourcestartaddress as UINTEGER, destinationstartaddress as UINTEGER, length as UINTEGER)
    ' Total 8 bytes: 2 bytes (source, destination) + 6 bytes (3 * UINTEGER)
    OUT LOKEYIOI_DATAPORT, source
    OUT LOKEYIOI_DATAPORT, destination
    OUT LOKEYIOI_DATAPORT, PEEK(@sourcestartaddress)
    OUT LOKEYIOI_DATAPORT, PEEK(@sourcestartaddress + 1)
    OUT LOKEYIOI_DATAPORT, PEEK(@destinationstartaddress)
    OUT LOKEYIOI_DATAPORT, PEEK(@destinationstartaddress + 1)
    OUT LOKEYIOI_DATAPORT, PEEK(@length)
    OUT LOKEYIOI_DATAPORT, PEEK(@length + 1)
    OUT LOKEYIOI_COMMANDPORT, LOKEYULAI_MEMCOPY
End Sub

Sub LOKEYIO_MemFill(destination as UBYTE, destinationaddress as UINTEGER, length as UINTEGER, fillvalue as UBYTE)
    OUT LOKEYIOI_DATAPORT, destination
    OUT LOKEYIOI_DATAPORT, PEEK(@destinationaddress)
    OUT LOKEYIOI_DATAPORT, PEEK(@destinationaddress + 1)
    OUT LOKEYIOI_DATAPORT, PEEK(@length)
    OUT LOKEYIOI_DATAPORT, PEEK(@length + 1)
	   OUT LOKEYIOI_DATAPORT, fillvalue
    OUT LOKEYIOI_COMMANDPORT, LOKEYULAI_MEMFILL
End Sub

Sub LOKEYIO_MemFillRandom(destination as UBYTE, destinationaddress as UINTEGER, lowerbound as UBYTE, upperbound as UBYTE, length as UINTEGER)
    ' Total 7 bytes: 1 byte (destination) + 2 bytes (address) + 2 bytes (bounds) + 2 bytes (length)
    OUT LOKEYIOI_DATAPORT, destination
    OUT LOKEYIOI_DATAPORT, PEEK(@destinationaddress)
    OUT LOKEYIOI_DATAPORT, PEEK(@destinationaddress + 1)
    OUT LOKEYIOI_DATAPORT, lowerbound
    OUT LOKEYIOI_DATAPORT, upperbound
    OUT LOKEYIOI_DATAPORT, PEEK(@length)
    OUT LOKEYIOI_DATAPORT, PEEK(@length + 1)
    OUT LOKEYIOI_COMMANDPORT, LOKEYULAI_MEMRANDOMFILL
End Sub