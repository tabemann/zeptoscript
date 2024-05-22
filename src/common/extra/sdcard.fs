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

begin-module zscript-sdcard

  zscript-oo import
  zscript-block-dev import

  \ Initialize the SD card
  method init-sd ( sd -- )

  \ Enable block zero writes
  method write-sd-block-zero! ( enabled sd -- )

  begin-module zscript-sdcard-internal
    
    \ SD card class
    foreign-constant forth::sd::<sd> foreign-<sd>

    \ Class size
    1 1 foreign forth::oo::class-size foreign-class-size

    \ Initialize an SD card
    4 0 foreign forth::oo::init-object foreign-init-object
    
    \ Initialize the SD card
    1 0 foreign forth::sd::init-sd foreign-init-sd
    ( sd -- )

    \ Enable block zero writes
    2 0 foreign forth::sd::write-sd-block-zero! foreign-write-sd-block-zero!
    ( enabled sd -- )
    
    \ Get block size
    1 1 foreign forth::block-dev::block-size foreign-block-size
    ( dev -- bytes )
    
    \ Get block count
    1 1 foreign forth::block-dev::block-count foreign-block-count
    ( dev -- blocks )

    \ Write block
    4 0 foreign forth::block-dev::block! foreign-block!
    ( c-addr u block-index dev -- )
    
    \ Write part of a block
    5 0 foreign forth::block-dev::block-part! foreign-block-part!
    ( c-addr u offset block-index dev -- )

    \ Read block
    4 0 foreign forth::block-dev::block@ foreign-block@
    ( c-addr u block-index dev -- )
    
    \ Read part of a block
    5 0 foreign forth::block-dev::block-part@ foreign-block-part@
    ( c-addr u offset block-index dev -- )

    \ Flush blocks
    1 0 foreign forth::block-dev::flush-blocks foreign-flush-blocks
    ( dev -- )
    
    \ Clear cached blocks
    1 0 foreign forth::block-dev::clear-blocks foreign-clear-blocks
    ( dev -- )
    
    \ Set write-through cache mode
    2 0 foreign forth::block-dev::write-through! foreign-write-through!
    ( write-through dev -- )
    
    \ Get write-through cache mode
    1 1 foreign forth::block-dev::write-through@ foreign-write-through@
    ( dev -- write-through )
    
  end-module> import
  
  begin-class sd

    member: my-sd

    \ Get the <sd> object
    :private sd@ ( self -- my-sd ) my-sd@ unsafe::bytes>addr-len drop ;

    \ Constructor
    :method new { cs-pin spi-device self -- }
      foreign-<sd> foreign-class-size make-bytes self my-sd!
      cs-pin spi-device foreign-<sd> self sd@ foreign-init-object
    ;
    
    \ Initialize the SD card
    :method init-sd { self -- }
      self sd@ foreign-init-sd
    ;
    
    \ Enable block zero writes
    :method write-sd-block-zero! { enabled self -- }
      enabled self sd@ foreign-write-sd-block-zero!
    ;

    \ Get block size
    :method block-size { self -- bytes }
      self sd@ foreign-block-size
    ;
    
    \ Get block count
    :method block-count { self -- blocks }
      self sd@ foreign-block-count
    ;

    \ Write block
    :method block! { bytes block-index self -- }
      block-index self block-count u< averts x-block-out-of-range
      16 cells ensure
      bytes unsafe::bytes>addr-len block-index self sd@ foreign-block!
    ;
    
    \ Write part of a block
    :method block-part! { bytes offset block-index self -- }
      block-index self block-count u< averts x-block-out-of-range
      16 cells ensure
      bytes unsafe::bytes>addr-len offset block-index
      self sd@ foreign-block-part!
    ;

    \ Read block
    :method block@ { bytes block-index self -- }
      block-index self block-count u< averts x-block-out-of-range
      16 cells ensure
      bytes unsafe::bytes>addr-len block-index self sd@ foreign-block@
    ;
    
    \ Read part of a block
    :method block-part@ { bytes offset block-index self -- }
      block-index self block-count u< averts x-block-out-of-range
      16 cells ensure
      bytes unsafe::bytes>addr-len offset block-index
      self sd@ foreign-block-part@
    ;

    \ Flush blocks
    :method flush-blocks { self -- }
      self sd@ foreign-flush-blocks
    ;
    
    \ Clear cached blocks
    :method clear-blocks { self -- }
      self sd@ foreign-clear-blocks
    ;
    
    \ Set write-through cache mode
    :method write-through! { write-through self -- }
      write-through self sd@ foreign-write-through!
    ;
    
    \ Get write-through cache mode
    :method write-through@ { self -- write-through }
      self sd@ foreign-write-through@
    ;

  end-class
  
end-module
