# zeptoscript

zeptoscript is a high-level scripting language based off of zeptoforth which runs on top of it which is meant for embedding in Forth applications. It has dynamic typing and automatic memory management along with optional support for object-orientation, double/S31.32 fixed-point numerics, S15.16 fixed-point numerics (this requires loading `extra/common/fixed32.fs` from zeptoforth in zeptoforth mode first), linked lists, maps (also known as associative arrays), sets, and asynchronous execution via "actions".

zeptoscript has the capability to call foreign, i.e. Forth, words. Note, however, that zeptoscript can only execute in a single zeptoforth task, and cannot be used within interrupt handlers.

## Installing zeptoscript

To install zeptoscript, load the latest zeptoforth onto your board and boot it, execute `compile-to-flash`, and the load `src/common/core.fs`. Afterwards reboot your board. Then, execute `compile-to-flash` again followed by `65536 65536 zscript::init-zscript` (or however much amount of RAM you wish to devote to the zeptoscript heap in the place of both `65536`es; note that only half of the RAM will be used at a time, so this amount must be twice the amount of heap space you wish to make available). After that, reboot your board another time. Once you have done this your board will boot into zeptoscript, and you will be able to compile zeptoscript code to both flash and RAM.

Note that, once zeptoscript has been loaded onto your board and `zscript::init-zscript` has been executed, you can return to zeptoforth with `enter-zforth` and then compile and execute zeptoforth code. Do not attempt to execute zeptoscript code at this point. To return to zeptoscript execute `zscript::enter-zscript`; if you execute this without having executed `zscript::init-zscript` before it is equivalent to having executed `65536 65536 zscript::init-zscript`.

You will probably want a more complete setup than this. To get this, the easiest approach is to upload `src/common/setup_common.fs` with zeptocom.js or with `utils/codeload3.sh`.

To do this with zeptocom.js, connect to the TTY device or virtual COM port (if you are using Windows) for your board with zeptocom.js under a web browser that supports the Web Serial API (i.e. Chrome, Chromium, or derivatives; note that Firefox, Safari, and mobile browsers are specifically not supported), click 'Send File', select `src/common/setup_common.fs`, and if prompted for a directory, select the root zeptoscript directory.

To do this with `utils/codeload3.sh` execute from the base zeptoscript directory:

```
$ utils/codeload3.sh -B 115200 -p <your device> serial src/common/setup_common.fs
```

where \<your device> is the TTY device for your board (e.g. `/dev/ttyACM0`).

Note that in both of these cases the board will be rebooted at the end of the compilation process, and will be ready for use once you reconnect to it.

Afterwards, you might want to add more capabilities. For instance:

* To add FAT32 filesystem support for SDHC/SDXC cards, upload `src/common/extra/setup_fat32.fs`
* To add SSD1306 OLED display driver support, upload `src/common/extra/setup_ssd1306.fs`
* To add SH1122 OLED text display driver support, upload `src/common/extra/setup_sh1122_text.fs`

Note that in all of these cases you should use zeptocom.js or `utils/codeload3.sh` for uploading, as these rely upon these tools' capability to load files included from within files. Also note that a module-already-defined error will occur if you attempt to upload any of these after they or any of their included files have been uploaded; e.g. do not attempt to upload `src/common/extra/setup_ssd1306.fs` and `src/common/extra/setup_sh1122_text.fs` unmodified to the same system without commenting out shared includes from the second file uploaded.

## Calling zeptoforth from zeptoscript

To call zeptoforth code from zeptoscript you will normally need to import it as "foreign" words. Note that you will see in the code included with zeptoscript that zeptoforth code is being called directly from zeptoscript, but considerable care is needed to do so, which will not be discussed here. To import "foreign" words use the following:

`foreign` ( *in-count* *out-count* "*foreign-name*" "*new-name*" -- )

This imports a foreign word named *foreign-name* as *new-name* with *in-count* arguments and *out-count* return values; note that it will be treated as taking and returning integral values.

`foreign-variable` ( "*foreign-name*" "*new-name*" -- )

This imports a foreign variable named *foreign-name* as a getter which returns an integral value named *new-name*`@` and a setter which takes an integral value named *new-name*`!`.

`foreign-hook-variable` ( "*foreign-name*" "*new-name*" -- )

This imports a foreign hook variable named *foreign-name* as a getter which returns an execution token named *new-name*`@` and a setter which takes an execution token named *new-name*`!`. Note that the execution tokens must not be partially applied.

`foreign-constant` ( "*foreign-name*" "*new-name*" -- )

This imports a foreign constant named *foreign-name* as an integral constant named *new-name*.

`foreign-double-constant` ( "*foreign-name*" "*new-name*" -- )

This imports a foreign double-cell constant named *foreign-name* as a double-cell constant named *new-name*.

`execute-foreign` ( ? *in-count* *out-count* *xt* -- ? )

This executes a foreign execution token *xt*, e.g. one gotten from a foreign hook variable, with *in-count* integral arguments and *out-count* integral return values.
