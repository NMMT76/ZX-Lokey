# ZX-Lokey
The [ZX Loki](https://en.wikipedia.org/wiki/Loki_(computer)) was the machine that never was. Too ambitious, too late. The ZX Lokey (pun intended) is a more modest take on the idea.

## Before you start:

The main ZX side "development stack" for the Lokey is [ZX Basic](https://github.com/boriel-basic/zxbasic) as it's faster to prototype and test things. The "cannonical" command used to compile examples into SNA's that the Lokey can use is "zxbc program.bas -O2 --explicit --strict -f sna --BASIC --autorun".

"Cannonical command vectors". While the ports used by LokeyULA (_LOKEY_ULA.bas), LokeyIO (_LOKEY_IO.bas) and LokeyFPU (_LOKEY_FPU.bas) are not expected to change, command "vectors" might and some features, while present in source, are **NOT** finished. If you want to write something for the Lokey, the commands that are already "cannonical" are the following, meaning that while the vector number might change, the functionality and calling convention will **NOT**.

**DO** refer to the corresponding Sub's, like LOKEYULA_Plot() for the calling convention, and **DO** use said Sub's instead of "inlining" the commands if you don't need every little % of performance.

From LokeyIO:
LOKEYIO_LOADSNAROM0, LOKEYIO_LOADSNAROM1, LOKEYIO_LOADSNADISK, LOKEYULAI_MEMCOPY, LOKEYULAI_MEMFILL, LOKEYULAI_MEMFILLRANDOM

From LokeyULA:
LOKEYULAI_CLS, LOKEYULAI_CLEARBITMAP, LOKEYULAI_CLEARATTRIBUTES, LOKEYULAI_PLOT, LOKEYULAI_LINE, LOKEYULAI_RECTANGLE, LOKEYULAI_RECTANGLEFILLED, LOKEYULAI_TRIANGLE, LOKEYULAI_TRIANGLEFILLED, LOKEYULAI_CIRCLE, LOKEYULAI_CIRCLEFILLED, LOKEYULAI_SCROLL, LOKEYULAI_ROLL, LOKEYULAI_PRINTCHAR

(the "filled" functions don't currently support pattern fill, I'm still working out how i want it to work on a "API level")

From LokeyFPU:
LOKEYFPUI_RNDBYTES, LOKEYFPUI_RNDFLOAT, LOKEYFPUI_RNDFIXED, LOKEYFPUI_FLOATADD,LOKEYFPUI_FLOATSUB,LOKEYFPUI_FLOATMUL,LOKEYFPUI_FLOATDIV,LOKEYFPUI_FLOATPOW,LOKEYFPUI_FLOATEXP,LOKEYFPUI_FLOATLN,LOKEYFPUI_FLOATSIN, LOKEYFPUI_FLOATCOS, LOKEYFPUI_FLOATTAN, LOKEYFPUI_FLOATASN, LOKEYFPUI_FLOATACS, LOKEYFPUI_FLOATATN, LOKEYFPUI_FLOATSQRT, LOKEYFPUI_FLOATABS, LOKEYFPUI_FLOATCOMPARE, LOKEYFPUI_FLOATMULADD, LOKEYFPUI_FLOATMULSUB, LOKEYFPUI_FLOATDIVADD, LOKEYFPUI_FLOATDIVSUB

Most of the features in the LokeyIO and LokeyULA will be "interpreted BASIC" accessible, but those in LokeyFPU will not (unless a mod'ed ROM is used), as they require the LokeyFPU receiveing the variable address, which isn't trivial from (standard) interpreted BASIC.

Features that are still not finished or done:

* Blitter (still not implemented)
* Create blank disk (TODO)
* Expression evaluation (exists, but API calling convention still not decided, work in progress)

Nice to have:

* A "patched" [OpenSE Basic Rom](https://sourceforge.net/projects/sebasic/) that used the Lokey's capabilities. Integrating into "standard BASIC" would be really nice, but Z80 asm is not my thing so... maybe.

## Quick history:
After i was done playing with my [EdgeMaster](https://github.com/NMMT76/Zero-Emulator-EM) idea, where i was intentionally avoiding any form of direct host interaction, i started toying around with the idea of a "more advanced" ZX. And many ideas came and went, more colors, more RAM, more this and that. And it kept going around, and it wasn't feeling "right". Until i stumbled uppon an article about the Loki. It 100% sounded like "wishware" but, in some sense it just looked like it was overambitious and way too late. So, what if it had been less and less? And the Lokey idea was born.

## Features
The Lokey borrows ideas from the Loki, the Amiga, the MSX and many others. All the "new bits" it has over the ZX Spectrum are simply "filling the gaps" where the ZX was sorelly lacking.

* Faster CPU (currently at 4.375Mhz, might go to 5.25 as the Z80B was already available even at the ZX launch, ideally would have been 7Mhz, but the Z80H is from 1986, too late in the game)
* 2 ROM slots (64KB max, read only)
* 1 "Disk drive" (64KB max, read/write)
* "Storage RAM" (64KB, Z80 can't directly access it)
* Dedicated "LokeyIO" ASIC. A primitive Blitter by any other name. Because the previous items are all 16but addressable and the ZX mmemory itself is also so (16KB ROM+48KB RAM), the "Memcopy" ASIC can be used orthogonally as a simple "copy from source to dest, this address to that address, this amount". ROM's can't be destination and the 16KB ROM part of the ZX normal address space can't be written to, for obvious reasons
* Same ASIC would also be able to MemFill and MemRandomFill any valid target, ie not ROM
* Primitive graphics processor (LokeyULA) with internal 8KB buffer, able to draw lines, circles, rectangles and triangles, line or filled
* Primitive Blitter (integrated into LokeyULA), able to copy "bitmap blocks" from RAM/StorageRAM to display ram, draw text (specialized form of bitmap block copy) and Scroll/Roll the display ram (another specialized form of bitmap block copy)
* Primitive Copper (integrated into LokeyULA), ables to change palette per scanline. Two modes, either bright palette with intensity reducion, meaning you can (per scanline) darken the colors, thus having 256 "darker shades" of the base colors or "monochrome palette", where it uses a monochrome palette of the original colors and the INK/PAPER values are an index to a shade on that palette
* FPU expansion (LokeyULA), with floating and fixed point operation support, and hardware based random number generator
* Debug expansion (integrated into LokeyIO, only of use to developers really, so they can Debug() information out without changing the display content)

## Supporting tech
The "support emulator" is still [Zero](https://github.com/ArjunNair/Zero-Emulator). The reason its not a fork is that it has been severelly cut down and changed to remove any bit that wasn't "essential" to teh Lokey. And the prunning and reworking is still ongoing. Being .Net and GDI based, its not as fast (or "speed stable") as it could be, but easy to develop for. And much care was taken to NOT use anything "exotic", thus adding the Lokey to other emulators, FPGA clones or similar should be pretty straightforward, as almost everything is either "memory operations" or "standard floating math operations".
