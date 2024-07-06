\ Copyright (c) 2024 Travis Bemann
\
\ Permission is hereby granted, free of charge, to any person obtaining a copy
\ of this software and associated documentation files (the "Software"), to deal
\ in the Software without restriction, including without limitation the rights
\ to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
\ copies of the Software, and to permit persons to whom the Software is
\ furnished to do so, subject to the following conditions:
\ 
\ The above copyright notice and this permission notice shall be included in
\ all copies or substantial portions of the Software.
\ 
\ THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
\ IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
\ FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
\ AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
\ LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
\ OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
\ SOFTWARE.

compile-to-ram

#include src/common/extra/init_fat32.fs

begin-module zscript-prepare-blocks-fat32

  zscript-fat32 import
  zscript-block-dev import
  zscript-blk import
  
  \ Prepare the FAT32 filesystem
  : prepare ( -- )
    make-blocks { dev }
    dev make-mbr { mbr }
    mbr mbr-valid? not if
      erase-all-blocks
      true dev write-blk-block-zero!
      true dev write-through!
      4 dev zscript-init-fat32-tool::init-partition-and-fat32
    then
  ;
  
end-module> import

prepare

compile-to-flash

\ Compile code to set up blocks FAT32 on boot
defined? zscript-setup-blocks-fat32 not [if]

  begin-module zscript-setup-blocks-fat32

    zscript-block-dev import
    zscript-simple-blocks-fat32 import
    
    \ The blocks FAT32 filesystem
    global my-fs
    
    \ Setup the blocks FAT32 filesystem
    : setup ( -- )
      make-simple-blocks-fat32-fs my-fs!
      true my-fs@ write-through!
      my-fs@ zscript-fs-tools::current-fs!
    ;
    
  end-module> import

  initializer setup

[then]

reboot
