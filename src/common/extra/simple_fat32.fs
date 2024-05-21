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

begin-module zscript-simple-fat32

  zscript-oo import
  zscript-block-dev import
  zscript-sdcard import
  zscript-fs import
  zscript-fat32 import

  begin-module zscript-simple-fat32-internal

    1 0 foreign forth::pin::pull-up-pin pull-up-pin
    1 0 foreign forth::pin::output-pin output-pin
    2 0 foreign forth::spi::spi-pin spi-pin
    
  end-module> import

  \ Simple FAT32 filesystem class
  begin-class simple-fat32-fs

    member: my-sd
    member: my-fs
    
    \ Constructor
    :method new { sck-pin tx-pin rx-pin cs-pin spi-device self -- }
      rx-pin pull-up-pin spi-device rx-pin spi-pin
      tx-pin pull-up-pin spi-device tx-pin spi-pin
      sck-pin pull-up-pin spi-device sck-pin spi-pin
      cs-pin output-pin cs-pin pull-up-pin
      cs-pin spi-device make-sd self my-sd!
      self my-sd@ init-sd
      0 self my-sd@ make-mbr partition@ self my-sd@ make-fat32-fs self my-fs!
    ;

    \ Get root directory
    :method root-dir@ ( self -- dir ) my-fs@ root-dir@ ;

    \ Flush a filesystem
    :method flush ( self -- ) my-fs@ flush ;

    \ Set write-through cache mode
    :method write-through! ( write-through self -- ) my-sd@ write-through! ;

    \ Get write-through cache mode
    :method write-through@ ( self -- write-through ) my-sd@ write-through@ ;
    
  end-class
  
end-module
