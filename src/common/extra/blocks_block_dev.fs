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

begin-module zscript-blk

  zscript-oo import
  zscript-block-dev import

  \ Enable block zero writes
  method write-blk-block-zero! ( enabled sd -- )

  begin-module zscript-blk-internal
    
    \ Blocks class
    foreign-constant forth::blk::<blocks> foreign-<blocks>

    \ Class size
    1 1 foreign forth::oo::class-size foreign-class-size

    \ Initialize blocks
    2 0 foreign forth::oo::init-object foreign-init-object
    
    \ Enable block zero writes
    2 0 foreign forth::blk::write-blk-block-zero! foreign-write-blk-block-zero!
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
  
  begin-class blocks

    member: my-blocks

    \ Get the <blocks> object
    :private blocks@ ( self -- my-sd ) my-blocks@ unsafe::bytes>addr-len drop ;

    \ Constructor
    :method new { self -- }
      foreign-<blocks> foreign-class-size make-bytes self my-blocks!
      foreign-<blocks> self blocks@ foreign-init-object
    ;
    
    \ Enable block zero writes
    :method write-blk-block-zero! { enabled self -- }
      enabled self blocks@ foreign-write-blk-block-zero!
    ;

    \ Get block size
    :method block-size { self -- bytes }
      self blocks@ foreign-block-size
    ;
    
    \ Get block count
    :method block-count { self -- blocks }
      self blocks@ foreign-block-count
    ;

    \ Write block
    :method block! { bytes block-index self -- }
      block-index self block-count u< averts x-block-out-of-range
      16 cells ensure
      bytes unsafe::bytes>addr-len block-index self blocks@ foreign-block!
    ;
    
    \ Write part of a block
    :method block-part! { bytes offset block-index self -- }
      block-index self block-count u< averts x-block-out-of-range
      16 cells ensure
      bytes unsafe::bytes>addr-len offset block-index
      self blocks@ foreign-block-part!
    ;

    \ Read block
    :method block@ { bytes block-index self -- }
      block-index self block-count u< averts x-block-out-of-range
      16 cells ensure
      bytes unsafe::bytes>addr-len block-index self blocks@ foreign-block@
    ;
    
    \ Read part of a block
    :method block-part@ { bytes offset block-index self -- }
      block-index self block-count u< averts x-block-out-of-range
      16 cells ensure
      bytes unsafe::bytes>addr-len offset block-index
      self blocks@ foreign-block-part@
    ;

    \ Flush blocks
    :method flush-blocks { self -- }
      self blocks@ foreign-flush-blocks
    ;
    
    \ Clear cached blocks
    :method clear-blocks { self -- }
      self blocks@ foreign-clear-blocks
    ;
    
    \ Set write-through cache mode
    :method write-through! { write-through self -- }
      write-through self blocks@ foreign-write-through!
    ;
    
    \ Get write-through cache mode
    :method write-through@ { self -- write-through }
      self blocks@ foreign-write-through@
    ;

  end-class
  
end-module
