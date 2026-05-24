# Zero-EM
Fork of ArjunNair's Zero Emulator, trimmed down to ZX Spectrum 48K only and with added EdgeMaster "virtual expansion device" and ULA-SX (simple expansion) support.

A potential implementation of the EdgeMaster in real hardware would/should always be based about some form of microcontroller, as that allows the simplest form of "expansion through code"

While Zero-EM is more of a "developer fun" thing, that would allow ZX/C# developers to push the machine in very creative ways, there is nothing whatsoever stopping it from being real hardware. By being a PIO device, the entry bar is quite low and there's no shortage of current microcontrollers that would be up to the task of providing at the very least "basic functionality", like "pseudo RAM expansion", "file IO", "accelerated math", etc... Running in an emulator only makes it easier to showcase ideas because you don't need real hardware to test them.

## Changes:
- Added a "DMA like" mode to video playback/transfer, just as a showcase, Zero IS NOT cycle accurate and thus this is just a way to showcase stuff if you DON'T expect (or need) it to be accurate on real world hardware.

# Acknowledgements
First off, many thanks for [Arjun Nair](https://github.com/ArjunNair) without whom this wouldn't have been possible. Zero having such a good structure for devices, unlike many other emulators, made it a very nice experience to create the EM. Also many thanks to [Jose Rodriguez-Rosa](https://github.com/boriel-basic/zxbasic) as without ZX Basic, the ZX side of the code would neither be so "clean" or efficient. To [György Kőszeg](https://github.com/koszeggy/KGySoft.Drawing) for his image library that is at the core of the image conversion process into B&W images. To [NAudio](https://github.com/naudio/NAudio) for the great audio library that allows the ZX to play MP3 (and more) through the host. To [DirectShow.Net](https://directshownet.sourceforge.net/) for the webcam capture capabilities. And last but not least to [Accord](http://accord-framework.net/) for the video "capture" capabilites.

# Concept
The whole idea behind the EdgeMaster, so named because it does NOT do DMA/busmastering, was that, given a "slow enough" platform, even a simple PIO device implementing a very basic form of RPC could:
1. provide infinite expansion/functionality
2. provide some form of acceleration
bounded only by:
- the IO speed
- the external "coprocessor" speed

While 1 is quite self evident, 2 is a bit more complicated. For it to be true, the time of execution for the "original code" needs to be higher than the time it takes to push data to the EM, do whatever work needs to be done and get the results back from the EM.

# Benchmarking
(all results from here on are based on Zero running on a R5 5600, YMMV)

One of the "weakest" part of the ZX is floating point operations. As show in EM_Tests_MathBasic, delegating the operations to the EM, even the basic math floating point functions will be "slightly faster" to "4x faster".
When you go to trigonometric functions, as in EM_Tests_BENCH01, the speed gains become very evident, in two different ways. One, the EM is so fast that is spends most of the time waiting for the ZX to fetch the results, the ZX is never waiting on the EM to finish. Two, because of the former, from the ZX point of view, the time the EM takes for a simple operation is no different from the time the EM takes to perform a not so simple operation. EM_Tests_BENCH02 and EM_Tests_BENCH03 perfectly illustrate this.

EM_Tests_BENCH04 is just an adaptation of [ZX Basic's Mandelbrot demo]([https://github.com/boriel-basic/zxbasic](https://github.com/boriel-basic/zxbasic/blob/main/examples/mandel.bas)). Unlike all the other "benchmarks", this one shows the EM assisted version before the "plain" version. You'll understand why when you run it.

In the EM_Suite folder you can also find the EM_Base.bas file which is a "base include" to help using the EM from ZX Basic. While you CAN use the EM from Sinclair Basic, it won't be anywhere near as "straightforward" due to the lack of simple and/or efficient access to a variables memory location, which is fundamental to sending/receiving data that is not simple bytes.

A word of warning or reminder, as i had to "rediscover" this almost 40y past. The Z80 does NOT have a cache. Loops ARE expensive. The following image shows how "raw transfer" speed to/from the EM changes with the amount of "unrolling". The graph is an "S shaped" curve where the impact of the loop start very high and then proceeds to becom negligible with diminishing returns as the unrol becomes larger. The difference between the write and the read speeds is attibutable to the fact that in the test the ZX is writing a constant value to the EM but is reading a value from the EM and writing it to memory.

![EM_UnrolledTransferSpeed](/ZiggyWin/ZiggyWin/EM_Suite/EM_UnrolledTransferSpeed.png)

The graph also showcases the tradeoff with have with a simple PIO device. While with "modest" unrolls you can get speeds in the 20-60KB/s range, which is huge by ZX standards, we aren't even close to "50fps full frame transfer" speeds. That is definitely "DMA territory". This is clearly illustrated in this Bad Apple mp4 playback frame using PIO. If you used the "showcase quasi DMA" methods used in EM_Tests_PlayVideoDMA.bas, playback would be flawless.

![EM_PlayVideo_Bad_Apple frame](/ZiggyWin/ZiggyWin/EM_Suite/EM_PlayVideo_Bad_Apple.png)

While much of the "tearing" come from not being done in synch with the VBI, most of it comes from simply not being fast enough.

A crude adaptation of Gabriel Gambetta's (https://www.gabrielgambetta.com/zx-raytracer.html) raytracer, (while obviosuly buggy), clearly illustrated the imense speedup in "computationally heavy" tasks.

![EM_ZXRaytracer](/ZiggyWin/ZiggyWin/EM_Suite/EM_ZXRaytracer.png)

# Quickstart
If you made it this far, and haven't just straight out jumped into the code to see how it works, here is a brief summary, but refer to the examples in EM_Suite and EM_Base.bas and code comments for more specifics:

The EM has two read/write ports, a dataport (63) and a command port(191). It uses a "virtual table" of functions, with 0-2 being reserved. 0 is "reset", forcing the EM to reset itself, 1 is "Register action" and 2 is "Load library" (not implemented).

To execute an action you must first register it by pushing the desired vtable "slot" you want (3-255), pushing a null terminated string of the action name to the dataport and pushing 1 to the commandport. Then you read the command port until you get 0 back. 
EMRegisterAction() from EM_Base does all this for you

Once an action is registered, you simply push whatever data the action needs to the dataport, push the previously registers action number to the command port and read the command port until it returns 0. EMRunCommand() and EMRunCommandNoWait() from EM_Base do this for you. If using EMRunCommand(), you're doing synchronous calls and the action will be finished when it returns. If EMRunCommandNoWait() you're doing asynchronous calls, and MUST check that the action has finished before fetching results by checkign if the command port returns 0.

Once the action is finished, you read whatever the result was from the dataport. Mind you that different actions have different results, and for lack of documentation, please refer to their implementation in EMInternalLib().

# Closing notes

The EM includes provision for "quasi-DMA", where the EM writes to memory mimicking the Z80. This is NOT accurate, and should only be used in "what if the EM COULD do DMA?" investigative scenarios. CaptureImageBWPIO() and CaptureImageBWDMA() showcase it (in a VERY crude way). A (proper) EM-DMA device would have far more impressive capabilities than "regular EM" due to not needing the Z80 to do the "read bus, write memory" part, but it would also not be such a simple and unassuming implementation. Speed<>Simplicity tradeoff.

Note : I did write, and then removed Math functions using DMA. In them, the ZX would write not the values but the memory addresses and the EM would fetch them from memory using "DMA". Removed because, no way to be "real world accurate". It made all operations at least twice as fast. Simple reasoning tells us in a normal operation using floats and PIO, the Z80 reads 5 bytes from memory and writes 5 bytes to the bus per floating point value. At Z80 access speed. Using DMA and writing the value address in memory, the Z80 only writes 2 bytes (at Z80 access speed) and the EM reads 5 bytes per floating point value (at EM access speed). If we consider uncontended access, thats 30 (3*5+3*5, 3T rw) vs 11 (best case, 2*3+5*1, assuming the EM could do 1T rw) vs 16 (more realistic case, 2*3+5*2, with the EM doing 2T rw). From the Z80's viewpoint, the speed differential to a modern microcontroller is so large that any "simple" operation looks instantaneous, bound only by the time it takes to move data across the bus. But, once more, Speed<>Simplicity tradeoff.

## ULA-SX
Zero-EM also includes an "extra", the ULA-SX (simple expansion). While similar to the ULAX, it takes a different view on how to repurpose the attibute bits. While the ULAX uses BRIGHT/FLASH as a "bank index", the ULA-SX uses them to extend the foreground color by two bits, effectivelly giving 32/8 foreground/background color. While the ULA-SX has less than the theoretical ULAX maximum combinations, it makes the artist life "simpler" as any foreground/background is available in any attribute block. All colors RGB value can be redefined by the user. Please refer to ULA_SX.cs for more information. The ULASXDEMO showcases it using the (close to) standard ZX 8c background colors with 32 foreground colors in a gradient from black to white.

![ULASXDEMO](/ZiggyWin/ZiggyWin/EM_Suite/ULASX/ULASXDEMO.png)

The ULA-SX wouldn't be a too far fetched thing to "make happen" as it fundamentally only requires a few "bytes" (internal registers) more than the original ULA, but unlike the EM it would require "internal changes" to the machine and not just and "add on device".

# Go have fun
Now with all said, go and have fun. What whacky stuff can you do with your "expanded ZX"?
