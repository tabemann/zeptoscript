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

begin-module zscript

  armv6m import
  
  \ We are out of memory
  : x-out-of-memory ." out of memory" cr ;

  \ We have a typing error
  : x-incorrect-type ." incorrect type" cr ;

  \ We have a size error
  : x-incorrect-size ." incorrect size" cr ;

  \ Offset out of range error
  : x-offset-out-of-range ." offset out of range" cr ;

  \ Null dereference
  : x-null-dereference ." null dereference" cr ;

  \ Unaligned dereference
  : x-unaligned-dereference ." unaligned dereference" cr ;

  \ Unsafe operation
  : x-unsafe-op ." unsafe operation" cr ;

  \ Value not small integer
  : x-not-small-int ." not small int" cr ;
  
  begin-module zscript-internal

    \ Are we initialized
    false value inited?
    
    \ Record syntax
    255 constant syntax-record
    
    \ The per-task zeptoscript structure
    \ This will ultimately be a user variable, but for testing purposes it will
    \ be a plain variable.
    variable zscript-state

    \ The zeptoscript structure
    begin-structure zscript-size

      \ The RAM globals array
      field: ram-globals-array

      \ The new RAM globals array
      field: new-ram-globals-array

      \ The flash globals array
      field: flash-globals-array
      
      \ The bottom of the from semi-space
      field: from-space-bottom
      
      \ The top of the from semi-space
      field: from-space-top
      
      \ The current to semi-space address
      field: to-space-current
      
      \ The bottom of the to semi-space
      field: to-space-bottom
      
      \ The top of the to semi-space
      field: to-space-top

    end-structure

    \ Get the RAM globals array
    : ram-globals-array@ zscript-state @ ram-globals-array @ ;

    \ Get the new RAM globals array
    : new-ram-globals-array@ zscript-state @ new-ram-globals-array @ ;

    \ Get the flash globals array
    : flash-globals-array@ zscript-state @ flash-globals-array @ ;

    \ Get the bottom of the from semi-space
    : from-space-bottom@ zscript-state @ from-space-bottom @ ;
    
    \ Get the top of the from semi-space
    : from-space-top@ zscript-state @ from-space-top @ ;
    
    \ Get the current to semi-space address
    : to-space-current@ zscript-state @ to-space-current @ ;
    
    \ Get the bottom of the to semi-space
    : to-space-bottom@ zscript-state @ to-space-bottom @ ;
    
    \ Get the top of the to semi-space
    : to-space-top@ zscript-state @ to-space-top @ ;

    \ Set the RAM globals array
    : ram-globals-array! zscript-state @ ram-globals-array ! ;

    \ Set the new RAM globals array
    : new-ram-globals-array! zscript-state @ new-ram-globals-array ! ;

    \ Set the flash globals array
    : flash-globals-array! zscript-state @ flash-globals-array ! ;
    
    \ Set the bottom of the from semi-space
    : from-space-bottom! zscript-state @ from-space-bottom ! ;
    
    \ Set the top of the from semi-space
    : from-space-top! zscript-state @ from-space-top ! ;
    
    \ Set the current to semi-space address
    : to-space-current! zscript-state @ to-space-current ! ;
    
    \ Set the bottom of the to semi-space
    : to-space-bottom! zscript-state @ to-space-bottom ! ;
    
    \ Set the top of the to semi-space
    : to-space-top! zscript-state @ to-space-top ! ;

    \ Tags
    1 constant word-tag

    \ Bit in the type indicating whether a value contains cells to GC
    \ Note that this is after subtracting two from the type
    8 constant has-values

    \ Type shift
    28 constant type-shift
    
    \ Size mask
    -1 32 type-shift - tuck lshift swap rshift constant size-mask

    \ Set whether a value is an integer
    : int? ( value -- int? ) [inlined] 1 and 0<> ;

    \ Get whether a value is an address
    : addr? ( value -- addr? ) [inlined] 1 and 0= ;

    \ Value to address
    : >addr ( value -- addr ) [inlined] 1 bic ;
    
    \ Relocate a block of memory
    : relocate ( orig -- new )
      code[
      0 tos cmp_,#_
      ne bc>
      pc 1 pop
      >mark
      1 r0 movs_,#_
      r0 tos tst_,_
      eq bc>
      pc 1 pop
      >mark
      ]code
      from-space-bottom@
      from-space-top@
      code[
      4 dp r0 ldr_,[_,#_]
      tos r0 cmp_,_
      lo bc>
      4 dp adds_,#_
      tos 1 dp ldm
      pc 1 pop
      >mark
      tos 1 dp ldm
      tos r0 cmp_,_
      hs bc>
      4 dp adds_,#_
      r0 tos movs_,_
      pc 1 pop
      >mark
      0 r0 tos ldr_,[_,#_]
      1 r1 movs_,#_
      r1 tos tst_,_
      eq bc>
      4 dp adds_,#_
      r1 tos bics_,_
      pc 1 pop
      >mark
      0 dp r0 str_,[_,#_]
      ]code
      size-mask and 1 rshift cell align { size }
      to-space-current@ { current }
      current size + { next-current }

      \ display-red
      \ ." [ "
      \ current h.8 space
      \ next-current h.8 space
      \ to-space-top@ h.8 space
      \ size .
      \ ." ] "
      \ display-normal
      
      to-space-top@ next-current >= averts x-out-of-memory
      dup current size move
      current 1 or swap !
      next-current to-space-current!
      current
    ;

    \ Relocate the stack
    : relocate-stack ( -- )
\       display-red
\       ."  Garbage collecting the stack... "
      sp@ stack-base @ swap begin 2dup > while
\         dup @ h.8 space
        dup @ relocate over !
        cell+
      repeat
      2drop
\       ."  Garbage collecting the return stack... "
      rp@ rstack-base @ swap begin 2dup > while
\         dup @ h.8 space
        dup @ relocate over !
        cell+
      repeat
      2drop
\       display-normal 
    ;

    \ Swap spaces
    : swap-spaces ( -- )
      to-space-bottom@
      to-space-top@
      from-space-bottom@
      from-space-top@
      to-space-top!
      dup to-space-current!
      to-space-bottom!
      from-space-top!
      from-space-bottom!
    ;

    \ Carry out a GC cycle
    : gc ( -- )
      swap-spaces
      relocate-stack
      ram-globals-array@ relocate ram-globals-array!
      new-ram-globals-array@ relocate new-ram-globals-array!
      flash-globals-array@ relocate flash-globals-array!
      to-space-bottom@ { gc-current }
      begin gc-current to-space-current@ < while
        gc-current @ { header }
        header size-mask and 1 rshift cell align { aligned-size }
        header [ has-values type-shift lshift ] literal and if
          gc-current aligned-size + { gc-current-end }
          gc-current cell+ begin dup gc-current-end < while
            dup @ relocate over ! cell+
          repeat
          drop
        then
        aligned-size +to gc-current
      repeat
    ;
    
    \ Case cells to nulls, integers, but not words
    : >small-int ( x -- 0 | int )
      code[
      0 tos cmp_,#_
      ne bc>
      pc 1 pop
      >mark
      30 tos r0 lsrs_,_,#_
      1 r0 cmp_,#_
      eq bc>
      2 r0 cmp_,#_
      eq bc>
      1 tos tos lsls_,_,#_
      1 r0 movs_,#_
      r0 tos orrs_,_
      pc 1 pop
      >mark
      >mark
      ]code
      ['] x-not-small-int ?raise
    ;

    \ Types
    0 >small-int constant null-type
    1 >small-int constant int-type
    2 >small-int constant bytes-type
    3 >small-int constant word-type
    4 >small-int constant 2word-type
    5 >small-int constant const-bytes-type
    6 >small-int constant xt-type
    7 >small-int constant tagged-type
    10 >small-int constant cells-type
    11 >small-int constant closure-type
    12 >small-int constant slice-type

    \ Cast nulls, integers, and words to cells, validating them
    : integral> ( 0 | int | addr -- x' )
      code[
      0 tos cmp_,#_
      ne bc>
      pc 1 pop
      >mark
      1 r0 movs_,#_
      r0 tos tst_,_
      eq bc>
      1 tos tos asrs_,_,#_
      pc 1 pop
      >mark
      0 tos r0 ldr_,[_,#_]
      type-shift r0 r0 lsrs_,_,#_
      [ word-type 1 rshift 2 - ] literal r0 cmp_,#_
      ne bc>
      cell tos tos ldr_,[_,#_]
      pc 1 pop
      >mark
      ]code
      ['] x-incorrect-type ?raise
    ;

    \ Get an allocation's type
    : >type ( value -- type )
      code[
      1 r0 movs_,#_
      r0 tos tst_,_
      eq bc>
      int-type tos movs_,#_
      pc 1 pop
      >mark
      0 tos cmp_,#_
      ne bc>
      null-type tos movs_,#_
      pc 1 pop
      >mark
      0 tos tos ldr_,[_,#_]
      type-shift tos tos lsrs_,_,#_
      2 tos adds_,#_
      1 tos tos lsls_,_,#_
      r0 tos orrs_,_
      ]code
    ;

    \ Get whether a value is null, an integer, or a word
    : integral? ( value -- integral? )
      code[
      1 r0 movs_,#_
      r0 tos tst_,_
      eq bc>
      31 tos tos lsls_,_,#_
      31 tos tos asrs_,_,#_
      pc 1 pop
      >mark
      0 tos cmp_,#_
      ne bc>
      31 r0 tos lsls_,_,#_
      31 tos tos asrs_,_,#_
      pc 1 pop
      >mark
      0 tos tos ldr_,[_,#_]
      type-shift tos tos lsrs_,_,#_
      word-type integral> 2 - tos cmp_,#_
      ne bc>
      31 r0 tos lsls_,_,#_
      31 tos tos asrs_,_,#_
      pc 1 pop
      >mark
      0 tos movs_,#_
      ]code
    ;

    \ Validate whether a value is an integer
    : validate-int ( value -- ) 1 and 0<> averts x-incorrect-type ;

    \ Validate whether a value is a word
    : validate-word ( value -- ) >type word-type = averts x-incorrect-type ;
    
    \ Get whether a value is an xt or closure
    : xt? ( value -- xt? )
      >type dup xt-type = swap closure-type = or
    ;

    \ Allocate memory as cells
    : allocate-cells { count type -- addr }
      count integral> 1+ cells >small-int { bytes }
      bytes integral> to-space-current@ + to-space-top@ > if
        gc
        bytes to-space-current@ +
        to-space-top@ <= averts x-out-of-memory
      then
      to-space-current@ { current }
      bytes integral> to bytes
      bytes current + to-space-current!
      current
      dup cell+ bytes cell - 0 fill
      bytes 1 lshift type integral> 2 - type-shift lshift or over !
    ;

    \ Allocate memory as bytes
    : allocate-bytes { count -- addr }
      count integral> cell+ cell align >small-int { bytes }
      bytes integral> to-space-current@ + to-space-top@ > if
        gc
        bytes to-space-current@ +
        to-space-top@ <= averts x-out-of-memory
      then
      to-space-current@ { current }
      bytes integral> to bytes
      bytes current + to-space-current!
      current
      dup cell+ bytes cell - 0 fill
      count integral> cell+ 1 lshift
      [ bytes-type integral> 2 - type-shift lshift ] literal or over !
    ;
    
    \ Allocate a cell
    : allocate-cell { type -- addr }
      to-space-current@ [ 2 cells ] literal + to-space-top@ > if
        gc
        to-space-current@ [ 2 cells ] literal + to-space-top@ <=
        averts x-out-of-memory
      then
      to-space-current@ { current }
      current [ 2 cells ] literal + to-space-current!
      current
      0 over cell+ !
      type integral> 2 - type-shift lshift
      [ 2 cells 1 lshift ] literal or over !
    ;

    \ Allocate a double-word
    : allocate-2cell { type -- addr }
      to-space-current@ [ 3 cells ] literal + to-space-top@ > if
        gc
        to-space-current@ [ 3 cells ] literal + to-space-top@
        <= averts x-out-of-memory
      then
      to-space-current@ { current }
      current [ 3 cells ] literal + to-space-current!
      current
      0 over cell+ !
      0 over [ 2 cells ] literal + !
      type integral> 2 - type-shift lshift
      [ 3 cells 1 lshift ] literal or over !
    ;

    \ Allocate a tagged value
    : allocate-tagged { count tag -- addr }
      count integral> [ 2 cells ] literal + cell align >small-int { bytes }
      bytes to-space-current@ + to-space-top@ > if
        gc
        bytes to-space-current@ +
        to-space-top@ <= averts x-out-of-memory
      then
      to-space-current@ { current }
      bytes integral> to bytes
      bytes current + to-space-current!
      current
      tag integral> over cell+ !
      dup [ 2 cells ] literal + bytes [ 2 cells ] literal - 0 fill
      count integral> [ 2 cells ] literal + 1 lshift
      [ tagged-type integral> 2 - type-shift lshift ] literal or over !
    ;

    \ Cast cells to nulls, integers, and words
    : >integral ( x -- 0 | int | addr )
      code[
      0 tos cmp_,#_
      ne bc>
      pc 1 pop
      >mark
      30 tos r0 lsrs_,_,#_
      1 r0 cmp_,#_
      eq bc>
      2 r0 cmp_,#_
      eq bc>
      1 tos tos lsls_,_,#_
      1 r0 movs_,#_
      r0 tos orrs_,_
      pc 1 pop
      >mark
      >mark
      ]code
      dup 1 and { lowest }
      1 or
      word-type allocate-cell swap 1 bic lowest or over cell+ !
    ;

    \ Convert a pair of cells to nulls, integers, or words
    : 2>integral ( x0 x1 -- 0|int|addr 0|int|addr )
      swap dup 1 and { lowest } 1 or swap
      >integral swap 1 bic lowest or >integral swap
    ;

    \ Convert a pair of nulls, integers, or words to cells
    : 2integral> ( 0|int|addr 0|int|addr -- x0 x1 )
      integral> swap integral> swap
    ;

    \ Convert any number of cells to nulls, integers, or words
    : n>integral ( xn ... x0 count -- 0|int|addr ... 0|int|addr )
      dup integral> cells [:
        { count buf }
        buf count integral> cells + buf ?do i ! loop
        buf dup count integral> 1- cells + ?do
          i @ >integral
        [ cell negate ] literal +loop
      ;] with-aligned-allot
    ;

    \ Convert any number of nulls, integers, or words to cells
    : nintegral> ( 0|int|addr ... 0|int|addr count -- xn ... x0 )
      dup integral> cells [:
        { count buf }
        buf count integral> cells + buf ?do i ! loop
        buf dup count integral> 1- cells + ?do
          i @ integral>
        [ cell negate ] literal +loop
      ;] with-aligned-allot
    ;

    \ Get the compilation state
    : state? state forth::@ >integral ;
  
    \ Cast cells to xts
    : >xt ( x -- xt )
      xt-type allocate-cell { xt-value }
      integral> xt-value cell+ !
      xt-value
    ;

    \ Convert an xt to an integral
    : xt>integral ( xt -- integral )
      dup >type xt-type = averts x-incorrect-type
      forth::cell+ @ >integral
    ;

    \ Convert an integral to an xt
    : integral>xt ( integral -- xt )
      integral> >xt
    ;

    \ Raise an exception with an integral value
    : integral-?raise ( integral-xt -- )
      integral> ?raise
    ;

    \ Normalize a value
    : normalize ( value -- value' )
      dup integral? if
        dup if
          dup 1 and if
            1 arshift
          else
            cell+ @
          then
        then
        >integral
      then
    ;

    \ Get whether an integer is a small integer
    : small-int? ( value -- small? )
      dup 1 and if
        drop true
      else
        0=
      then          
    ;
    
    \ Get the tag of a tagged value
    : >tag ( value -- tag )
      dup >type tagged-type = averts x-incorrect-type
      cell+ @ >integral
    ;
    
    \ Get an allocation's size - note that the return value is not an integral
    \ value
    : >size ( value -- size )
      code[
      0 tos cmp_,#_
      ne bc>
      pc 1 pop
      >mark
      1 r0 movs_,#_
      r0 tos tst_,_
      eq bc>
      0 tos movs_,#_
      pc 1 pop
      >mark
      0 tos tos ldr_,[_,#_]
      32 type-shift - tos tos lsls_,_,#_
      33 type-shift - tos tos lsrs_,_,#_
      ]code
    ;
    
    \ Get a value in a tagged data structure
    : t@+ ( index object -- value )
      dup >type tagged-type = averts x-incorrect-type
      swap integral> 2 + cells
      over >size over u> averts x-offset-out-of-range
      + @ >integral
    ;

    \ Set a value in a tagged data structure
    : t!+ ( value index object -- )
      dup >type tagged-type = averts x-incorrect-type
      swap integral> 2 + cells
      over >size over u> averts x-offset-out-of-range
      + swap integral> swap !
    ;

    \ Handle a number
    : do-handle-number ( addr bytes -- [value] -1 | 0 )
      parse-integer if
        >integral
        state @ if
          dup small-int? if
            lit,
          else
            integral> lit, postpone >integral
          then
        then
        true
      else
        drop false
      then
    ;

    \ RAM current global id
    0 value current-ram-global-id

    \ Specify current flash global variable ID
    : set-current-flash-global-id ( id -- )
      get-current
      swap
      internal set-current
      integral> s" *GLOBAL*" internal::constant-with-name
      set-current
    ;

    \ Get current flash global variable ID
    : get-current-flash-global-id ( -- id )
      s" *GLOBAL*" flash-latest find-all-dict dup if
        forth::>xt execute >integral
      else
        drop 0
      then
    ;

    \ Initial RAM global ID index
    0 constant current-ram-global-id-index

    \ Initial RAM global ID
    1 >small-int constant init-ram-global-id

    \ Initial RAM globals count
    1 >small-int constant init-ram-global-count

  end-module> import

  \ Types
  null-type constant null-type
  int-type constant int-type
  bytes-type constant bytes-type
  word-type constant word-type
  2word-type constant 2word-type
  const-bytes-type constant const-bytes-type
  xt-type constant xt-type
  tagged-type constant tagged-type
  cells-type constant cells-type
  closure-type constant closure-type
  slice-type constant slice-type

  \ Get the raw LIT,
  : raw-lit, ( x -- ) integral> lit, ;
  
  \ Redefine LIT,
  : lit, ( xt -- )
    dup small-int? if lit, else integral> lit, postpone >integral then
  ;

  \ Convert an address/length pair into constant bytes
  : >const-bytes ( c-addr u -- const-bytes )
    const-bytes-type allocate-2cell { const-bytes }
    integral> const-bytes [ 2 cells ] literal + !
    integral> const-bytes cell+ !
    const-bytes
  ;

  \ Convert an address/length pair into bytes
  : addr-len>bytes ( c-addr u -- bytes )
    dup allocate-bytes { bytes }
    swap integral> bytes cell+ rot integral> move
    bytes
  ;

  \ Get the raw bytes of a cells, bytes, or slice value
  : >raw ( x -- x' )
    begin
      dup >type case
        cells-type of true endof
        bytes-type of true endof
        const-bytes-type of true endof
        slice-type of cell+ @ false endof
        ['] x-incorrect-type ?raise
      endcase
    until
  ;

  \ Get the raw offset of a cells, bytes, or slice value
  : >raw-offset ( x -- offset )
    0 { offset }
    begin
      dup >type case
        cells-type of drop true endof
        bytes-type of drop true endof
        const-bytes-type of drop true endof
        slice-type of
          dup [ 2 cells ] literal + @
          offset integral> swap integral> + >integral to offset
          cell+ @ false
        endof
        ['] x-incorrect-type ?raise
      endcase
    until
    offset
  ;

  \ Get the length of cells in entries or bytes in bytes
  : >len ( cells | bytes -- len )
    dup >type case
      cells-type of
        >size cell - 2 rshift >integral
      endof
      bytes-type of
        >size cell - >integral
      endof
      const-bytes-type of
        [ 2 cells ] literal + @ >integral
      endof
      slice-type of
        [ 3 cells ] literal + @
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Convert two values into a pair
  : >pair ( x0 x1 -- pair )
    [ 2 >small-int ] literal cells-type allocate-cells { pair }
    pair [ 2 cells ] literal + !
    pair cell+ !
    pair
  ;

  \ Convert a pair into two values
  : pair> ( pair -- x0 x1 )
    dup >type case
      cells-type of
        dup >size [ 3 cells ] literal = averts x-incorrect-size
        dup cell+ @
        over [ 2 cells ] literal + @
      endof
      slice-type of
        dup >raw { raw }
        dup >raw-offset { offset }
        >len { len }
        raw >type cells-type = averts x-incorrect-type
        len [ 2 >small-int ] literal = averts x-incorrect-size
        raw offset 1+ cells + dup @
        swap cell+ @
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Convert three values into a triple
  : >triple ( x0 x1 x2 -- triple )
    [ 3 >small-int ] literal cells-type allocate-cells { triple }
    triple [ 3 cells ] literal + !
    triple [ 2 cells ] literal + !
    triple cell+ !
    triple
  ;

  \ Convert a triple into three values
  : triple> ( pair -- x0 x1 x2 )
    dup >type case
      cells-type of
        dup >size [ 4 cells ] literal = averts x-incorrect-size
        dup cell+ @
        over [ 2 cells ] literal + @
        rot [ 3 cells ] literal + @
      endof
      slice-type of
        dup >raw { raw }
        dup >raw-offset { offset }
        >len { len }
        raw >type cells-type = averts x-incorrect-type
        len [ 3 >small-int ] literal = averts x-incorrect-size
        raw offset 1+ cells + dup @
        swap cell+ dup @
        swap cell+ @
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Allocate a tuple
  : make-cells ( count -- tuple )
    cells-type allocate-cells
  ;

  \ Allocate bytes
  : make-bytes ( count -- bytes )
    allocate-bytes
  ;

  \ Create a cell sequence
  : >cells ( xn ... x0 count -- cells )
    dup cells-type allocate-cells { seq }
    integral> seq over 1+ cells + swap 0 ?do cell - tuck ! loop drop
    seq
  ;

  \ Create a byte sequence
  : >bytes ( cn ... c0 count -- bytes )
    dup allocate-bytes { seq }
    integral> seq over cell+ + swap 0 ?do
      1- tuck swap integral> swap c!
    loop drop
    seq
  ;

  \ Explode a cell sequence
  : cells> ( cells -- xn ... x0 count )
    dup >type case
      cells-type of
        dup >size cell - 2 rshift { count }
        count 0 ?do cell + dup @ swap loop drop
        count >integral
      endof
      slice-type of
        dup >raw { raw }
        dup >raw-offset { offset }
        >len { len }
        raw >type cells-type = averts x-incorrect-type
        raw offset integral> cells +
        len integral> 0 ?do cell + dup @ swap loop drop
        len
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Explode a byte sequence
  : bytes> ( bytes -- cn ... c0 count )
    dup >type case
      bytes-type of
        dup >size cell - { count }
        cell+ count 0 ?do dup c@ >integral swap 1+ loop drop
        count >integral
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { count }
        cell+ @ count 0 ?do dup c@ >integral swap 1+ loop drop
        count >integral
      endof
      slice-type of
        dup >raw { raw }
        dup >raw-offset { offset }
        >len { len }
        raw >type case
          bytes-type of
            raw cell+ offset integral> +
            len integral> 0 ?do dup c@ >integral swap 1+ loop drop
          endof
          const-bytes-type of
            raw cell+ @ offset integral> +
            len integral> 0 ?do dup c@ >integral swap 1+ loop drop
          endof
          ['] x-incorrect-type ?raise
        endcase
        len
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Explode a cell sequence without pushing its count
  : cells-no-count> ( cells -- xn ... x0 )
    dup >type case
      cells-type of
        dup >size cell - 2 rshift { count }
        count 0 ?do cell + dup @ swap loop drop
      endof
      slice-type of
        dup >raw { raw }
        dup >raw-offset { offset }
        >len { len }
        raw >type cells-type = averts x-incorrect-type
        raw offset integral> cells +
        len integral> 0 ?do cell + dup @ swap loop drop
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;
  
  \ Explode a byte sequence without pushing its count
  : bytes-no-count> ( bytes -- cn ... c0 )
    dup >type case
      bytes-type of
        dup >size cell - { count }
        cell+ count 0 ?do dup c@ >integral swap 1+ loop drop
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { count }
        cell+ @ count 0 ?do dup c@ >integral swap 1+ loop drop
      endof
      slice-type of
        dup >raw { raw }
        dup >raw-offset { offset }
        >len { len }
        raw >type case
          bytes-type of
            raw cell+ offset integral> +
            len integral> 0 ?do dup c@ >integral swap 1+ loop drop
          endof
          const-bytes-type of
            raw cell+ @ offset integral> +
            len integral> 0 ?do dup c@ >integral swap 1+ loop drop
          endof
          ['] x-incorrect-type ?raise
        endcase
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Get a value in a cells data structure
  : @+ ( index object -- value )
    dup >type case
      cells-type of
        swap integral> 1+ cells
        over >size over u> averts x-offset-out-of-range
        + @
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        raw >type cells-type = averts x-incorrect-type
        slice >raw-offset { offset }
        slice >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        len index u> averts x-offset-out-of-range
        raw index offset + 1+ cells + @
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Set a value in a cells data structure
  : !+ ( value index object -- )
    dup >type case
      cells-type of
        swap integral> 1+ cells
        over >size over u> averts x-offset-out-of-range
        + !
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        raw >type cells-type = averts x-incorrect-type
        slice >raw-offset { offset }
        slice >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        len index u> averts x-offset-out-of-range
        raw index offset + 1+ cells + !
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Get a word in a bytes data structure
  : w@+ ( index object -- byte )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over 4 + u>= averts x-offset-out-of-range
        dup 3 and triggers x-unaligned-dereference
        +
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        swap integral> swap
        over len 4 + u<= averts x-offset-out-of-range
        over 3 and triggers x-unaligned-dereference
        cell+ @ +
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        slice >raw-offset { offset }
        >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        raw >type case
          bytes-type of
            len index 4 + u>= averts x-offset-out-of-range
            index 3 and triggers x-unaligned-dereference
            raw index offset + cell+ +
          endof
          const-bytes-type of
            len index 4 + u>= averts x-offset-out-of-range
            index 3 and triggers x-unaligned-dereference
            raw cell+ @ index offset + +
          endof
          ['] x-incorrect-type ?raise
        endcase
      endof
      ['] x-incorrect-type ?raise
    endcase
    @ >integral
  ;

  \ Set a word in a bytes data structure
  : w!+ ( byte index object -- )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over 4 + u>= averts x-offset-out-of-range
        dup 3 and triggers x-unaligned-dereference
        + 
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        slice >raw-offset { offset }
        >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        raw >type case
          bytes-type of
            len index 4 + u>= averts x-offset-out-of-range
            index 3 and triggers x-unaligned-dereference
            raw index offset + cell+ +
          endof
          ['] x-incorrect-type ?raise
        endcase
      endof
      ['] x-incorrect-type ?raise
    endcase
    swap integral> swap !
  ;

  \ Get a halfword in a bytes data structure
  : h@+ ( index object -- byte )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over 2 + u>= averts x-offset-out-of-range
        dup 1 and triggers x-unaligned-dereference
        +
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        swap integral> swap
        over len 2 + u<= averts x-offset-out-of-range
        over 1 and triggers x-unaligned-dereference
        cell+ @ +
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        slice >raw-offset { offset }
        >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        raw >type case
          bytes-type of
            len index 2 + u>= averts x-offset-out-of-range
            index 1 and triggers x-unaligned-dereference
            raw index offset + cell+ +
          endof
          const-bytes-type of
            len index 2 + u>= averts x-offset-out-of-range
            index 1 and triggers x-unaligned-dereference
            raw cell+ @ index offset + +
          endof
          ['] x-incorrect-type ?raise
        endcase
      endof
      ['] x-incorrect-type ?raise
    endcase
    h@ >integral
  ;

  \ Set a halfword in a bytes data structure
  : h!+ ( byte index object -- )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over 2 + u>= averts x-offset-out-of-range
        dup 1 and triggers x-unaligned-dereference
        + 
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        slice >raw-offset { offset }
        >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        raw >type case
          bytes-type of
            len index 2 + u>= averts x-offset-out-of-range
            index 1 and triggers x-unaligned-dereference
            raw index offset + cell+ +
          endof
          ['] x-incorrect-type ?raise
        endcase
      endof
      ['] x-incorrect-type ?raise
    endcase
    swap integral> swap h!
  ;

  \ Get a byte in a bytes data structure
  : c@+ ( index object -- byte )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over u> averts x-offset-out-of-range
        +
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        swap integral> swap
        over len u< averts x-offset-out-of-range
        cell+ @ +
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        slice >raw-offset { offset }
        >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        raw >type case
          bytes-type of
            len index u> averts x-offset-out-of-range
            raw index offset + cell+ +
          endof
          const-bytes-type of
            len index u> averts x-offset-out-of-range
            raw cell+ @ index offset + +
          endof
          ['] x-incorrect-type ?raise
        endcase
      endof
      ['] x-incorrect-type ?raise
    endcase
    c@ >integral
  ;

  \ Set a byte in a bytes data structure
  : c!+ ( byte index object -- )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over u> averts x-offset-out-of-range
        + 
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        slice >raw-offset { offset }
        >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        raw >type case
          bytes-type of
            len index u> averts x-offset-out-of-range
            raw index offset + cell+ +
          endof
          ['] x-incorrect-type ?raise
        endcase
      endof
      ['] x-incorrect-type ?raise
    endcase
    swap integral> swap c!
  ;

  \ Is something cells
  : cells? ( x -- cells? )
    begin
      dup >type case
        cells-type of drop true true endof
        slice-type of cell+ @ false endof
        nip false true rot
      endcase
    until
  ;  

  \ Is something bytes
  : bytes? ( x -- bytes? )
    begin
      dup >type case
        bytes-type of drop true true endof
        const-bytes-type of drop true true endof
        slice-type of cell+ @ false endof
        nip false true rot
      endcase
    until
  ;  
  
  \ Initialize zeptoscript
  defer init-zscript
  :noname { compile-size runtime-size -- }
    compile-size [ 2 cells ] literal align to compile-size
    runtime-size [ 2 cells ] literal align to runtime-size
    compiling-to-flash? if
      s" init" flash-latest find-all-dict
      get-current -rot
      forth set-current
      s" init" internal::start-compile visible
      ?dup if forth::>xt lit, postpone execute then
      runtime-size lit, runtime-size lit, postpone init-zscript
      internal::end-compile,
      set-current
      compile-size
    else
      runtime-size
    then { size }
    cell ram-align, ram-here zscript-size ram-allot zscript-state !
    ram-here size ram-allot { heap }
    heap from-space-bottom!
    heap size 1 rshift +
    dup from-space-top! dup to-space-current! to-space-bottom!
    heap size + to-space-top!
    0 ram-globals-array!
    0 new-ram-globals-array!
    0 flash-globals-array!
    init-ram-global-count cells-type allocate-cells { ram-globals }
    init-ram-global-id current-ram-global-id-index ram-globals !+
    ram-globals ram-globals-array!
    ram-globals new-ram-globals-array!
    get-current-flash-global-id
    cells-type allocate-cells flash-globals-array!
    ['] do-handle-number handle-number-hook !
  ; is init-zscript

  \ Copy from one value to another in a type-safe fashion
  : copy { value0 offset0 value1 offset1 count -- }
    count integral> to count
    offset0 integral> to offset0
    offset1 integral> to offset1
    value0 addr? averts x-incorrect-type
    value1 addr? averts x-incorrect-type
    value0 >raw { raw0 }
    value1 >raw { raw1 }
    value0 >raw-offset integral> { raw-offset0 }
    value1 >raw-offset integral> { raw-offset1 }
    raw0 >type { type0 }
    type0 const-bytes-type = if
      raw1 >type bytes-type = averts x-incorrect-type
      count 0>= averts x-offset-out-of-range
      value0 >len integral> offset0 count + >= averts x-offset-out-of-range
      value1 >len integral> offset1 count + >= averts x-offset-out-of-range
      raw0 cell+ @ offset0 + raw-offset0 +
      raw1 cell+ offset1 + raw-offset1 + count move
    else
      type0 raw1 >type = averts x-incorrect-type
      count 0>= averts x-offset-out-of-range
      value0 >len integral> offset0 count + >= averts x-offset-out-of-range
      value1 >len integral> offset1 count + >= averts x-offset-out-of-range
      type0 integral> 2 - has-values and if
        count cells to count
        offset0 cells to offset0
        offset1 cells to offset1
        raw-offset0 cells to raw-offset0
        raw-offset1 cells to raw-offset1
      then
      raw0 cell+ offset0 + raw-offset0 +
      raw1 cell+ offset1 + raw-offset1 + count move
    then
  ;
  
  \ Redefine S"
  : s" ( "string" -- triple )
    [immediate]
    state @ if
      postpone s"
      postpone 2>integral
      postpone >const-bytes
    else
      [char] " internal::parse-to-char
      2>integral
      addr-len>bytes
    then
  ;

  \ Redefer S\"
  : s\" ( "string" -- triple )
    [immediate]
    state @ if
      postpone s\"
      postpone 2>integral
      postpone >const-bytes
    else
      [:
        here dup [char] " esc-string::parse-esc-string
        here over -
        2>integral
        addr-len>bytes
        swap ram-here!
      ;] with-ram
    then
  ;
  
  \ Add two integers
  : + ( x0 x1 -- x2 )
    integral>
    swap integral> + >integral
  ;

  \ Add one to an integer
  : 1+ ( x0 -- x1 )
    integral> 1+ >integral
  ;

  \ Add a cell to an integer
  : cell+ ( x0 -- x1 )
    integral> cell+ >integral
  ;

  \ Subtract two integers
  : - ( x0 x1 -- x2 )
    swap integral>
    swap integral> - >integral
  ;

  \ Subtract one from an integer
  : 1- ( x0 -- x1 )
    integral> 1- >integral
  ;

  \ Multiply two integers
  : * ( x0 x1 -- x2 )
    integral>
    swap integral> * >integral
  ;

  \ Multiply an integer by two
  : 2* ( x0 -- x1 )
    integral> 1 lshift >integral
  ;

  \ Multiple an integer by four
  : 4* ( x0 -- x1 )
    integral> 2 lshift >integral
  ;

  \ Multiple an integer by a cell
  : cells ( x0 -- x1 )
    integral> 2 lshift >integral
  ;

  \ Divide two signed integers
  : / ( n0 n1 -- n2 )
    swap integral>
    swap integral> / >integral
  ;

  \ Divide a signed integer by two
  : 2/ ( n0 -- n1 )
    integral> 1 arshift >integral
  ;

  \ Divide a signed integer by four
  : 4/ ( n0 -- n1 )
    integral> 2 arshift >integral
  ;
  
  \ Divide two unsigned integers
  : u/ ( u0 u1 -- u2 )
    swap integral>
    swap integral> u/ >integral
  ;

  \ Get the modulus of two signed integers
  : mod ( n0 n1 -- n2 )
    swap integral>
    swap integral> mod >integral
  ;

  \ Get the modulus of two unsigned integers
  : umod ( u0 u1 -- u2 )
    swap integral>
    swap integral> umod >integral
  ;

  \ Or two integers
  : or ( x0 x1 -- x2 )
    integral>
    swap integral> or >integral
  ;

  \ And two integers
  : and ( x0 x1 -- x2 )
    integral>
    swap integral> and >integral
  ;

  \ Exclusive-or two integers
  : xor ( x0 x1 -- x2 )
    integral>
    swap integral> xor >integral
  ;

  \ Clear bits in an integer
  : bic ( x0 x1 -- x2 )
    integral>
    swap integral> bic >integral
  ;    

  \ Not an integer
  : not ( x0 -- x1 )
    integral> not >integral
  ;

  \ Invert an integer
  : invert ( x0 -- x1 )
    integral> forth::not >integral
  ;

  \ Left shift an integer
  : lshift ( x0 x1 -- x2 )
    swap integral>
    swap integral> lshift >integral
  ;

  \ Logical right shift an integer
  : rshift ( x0 x1 -- x2 )
    swap integral>
    swap integral> rshift >integral
  ;

  \ Arithmetic right shift an integer
  : arshift ( x0 x1 -- x2 )
    swap integral>
    swap integral> arshift >integral
  ;

  \ Align a value to a power of two
  : align ( x0 x1 -- x2 )
    swap integral>
    swap integral> align >integral
  ;
  
  \ Get whether two values are equal
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : = { x0 x1 -- flag }
    x0 integral? x1 integral? forth::and if
      x0 integral> x1 integral> =
    else
      x0 >type xt-type = x1 >type xt-type = and if
        x0 forth::cell+ @ x1 forth::cell+ @ =
      else
        x0 x1 =
      then
    then
  ;

  \ Get whether two values are unequal
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : <> { x0 x1 -- flag }
    x0 integral? x1 integral? forth::and if
      x0 integral> x1 integral> <>
    else
      x0 >type xt-type = x1 >type xt-type = and if
        x0 forth::cell+ @ x1 forth::cell+ @ <>
      else
        x0 x1 <>
      then
    then
  ;

  \ Signed less than
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : < ( x0 x1 -- flag )
    integral> swap integral> >
  ;

  \ Signed greater than
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : > ( x0 x1 -- flag )
    integral> swap integral> forth::<
  ;

  \ Signed less than or equal
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : <= ( x0 x1 -- flag )
    integral> swap integral> >=
  ;

  \ Signed greater than or equal
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : >= ( x0 x1 -- flag )
    integral> swap integral> forth::<=
  ;

  \ Unsigned less than
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : u< ( x0 x1 -- flag )
    integral> swap integral> u>
  ;

  \ Unsigned greater than
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : u> ( x0 x1 -- flag )
    integral> swap integral> forth::u<
  ;

  \ Unsigned less than or equal
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : u<= ( x0 x1 -- flag )
    integral> swap integral> u>=
  ;

  \ Unsigned greater than or equal
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : u>= ( x0 x1 -- flag )
    integral> swap integral> forth::u<=
  ;

  \ Equal to zero
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : 0= ( n -- flag )
    dup integral? if integral> 0= else drop false then
  ;

  \ Not equal to zero
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : 0<> ( n -- flag )
    dup integral? if integral> 0<> else drop true then
  ;

  \ Less than zero
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : 0< ( n -- flag )
    integral> 0<
  ;

  \ Greater than zero
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : 0> ( n -- flag )
    integral> 0>
  ;

  \ Less than or equal to zero
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : 0<= ( n -- flag )
    integral> 0<=
  ;

  \ Greater than or equal to zero
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : 0>= ( n -- flag )
    integral> 0>=
  ;

  \ Get the minimum of two numbers
  : min ( n0 n1 -- n2 )
    integral> swap integral> min >integral
  ;

  \ Get the maximum of two numbers
  : max ( n0 n1 -- n2 )
    integral> swap integral> max >integral
  ;

  \ Cell constant
  cell >small-int constant cell
  
  \ Redefine IF
  : if ( flag -- )
    [immediate]
    state @ if
      postpone integral>
    else
      integral>
    then
    postpone if
  ;

  \ Redefine WHILE
  : while ( flag -- )
    [immediate]
    [compile-only]
    postpone integral>
    postpone while
  ;

  \ Redefine UNTIL
  : until ( flag -- )
    [immediate]
    [compile-only]
    postpone integral>
    postpone until
  ;

  \ Redefine ?DO
  : ?do ( end start -- )
    [immediate]
    state @ forth::if
      postpone 2dup
      postpone =
      postpone if
      postpone 2drop
      0 lit, 0 lit,
      postpone then
    else
      2dup = if 2drop 0 0 then
    then
    postpone ?do
  ;

  \ Close a DO LOOP
  : loop ( compile: loop-addr leave-addr -- ) ( runtime: -- )
    [immediate] [compile-only]
    undefer-lit
    internal::syntax-do internal::verify-syntax internal::drop-syntax
    internal::end-block
    swap
    [ armv6m-instr import ]

    tos internal::push,
    
    internal::find-i-var 4 forth::*
    dup 128 forth::< forth::if
      tos ldr_,[sp,#_]
    else
      r2 internal::literal,
      r2 add4_,sp
      0 r2 tos ldr_,[_,#_]
    then
    
    postpone integral>
    1 tos adds_,#_
    tos internal::push,
    
    internal::find-limit-var 4 forth::*
    dup 128 forth::< forth::if
      tos ldr_,[sp,#_]
    else
      r2 internal::literal,
      r2 add4_,sp
      0 r2 tos ldr_,[_,#_]
    then

    postpone integral>
    tos r1 movs_,_
    tos 1 dp ldm
    
    r1 tos cmp_,_
    eq bc>

    postpone >integral
    
    internal::find-i-var 4 forth::*
    dup 128 forth::< forth::if
      tos str_,[sp,#_]
    else
      r2 internal::literal,
      r2 add4_,sp
      0 r2 tos str_,[_,#_]
    then
    tos 1 dp ldm
    rot internal::branch,
    >mark
    tos 1 dp ldm
    [ armv6m-instr unimport ]
    internal::end-block
    here 1 forth::or 0 rot internal::literal!

    implicit-internal::try-end-implicit
  ;

  \ Close a DO +LOOP
  : +loop ( compile: loop-addr leave-addr -- ) ( runtime: -- )
    [immediate] [compile-only]
    undefer-lit
    internal::syntax-do internal::verify-syntax internal::drop-syntax
    internal::end-block
    swap
    [ armv6m-instr import ]
    
    tos internal::push,
    
    internal::find-i-var 4 forth::*
    dup 128 forth::< forth::if
      tos ldr_,[sp,#_]
    else
      r3 internal::literal,
      r3 add4_,sp
      0 r3 tos ldr_,[_,#_]
    then

    postpone 2dup
    postpone +

    tos 1 push
    tos 1 dp ldm
    postpone 2integral>
    tos internal::push,
    tos 1 pop
    
    internal::find-i-var 4 forth::*
    dup 128 forth::< forth::if
      tos str_,[sp,#_]
    else
      r3 internal::literal,
      r3 add4_,sp
      0 r3 tos str_,[_,#_]
    then

    postpone integral>
    tos internal::push,
    
    internal::find-limit-var 4 forth::*
    dup 128 forth::< forth::if
      tos ldr_,[sp,#_]
    else
      r3 internal::literal,
      r3 add4_,sp
      0 r3 tos ldr_,[_,#_]
    then

    postpone integral>
    tos r1 movs_,_
    r0 1 dp ldm
    r2 1 dp ldm
    tos 1 dp ldm
    
    0 tos cmp_,#_
    lt bc>

    tos 1 dp ldm
    
    r1 r2 cmp_,_
    le bc>
    4 pick internal::branch,
    >mark

    r0 r1 cmp_,_
    le bc>
    4 pick internal::branch,

    2swap >mark

    tos 1 dp ldm

    r1 r2 cmp_,_
    ge bc>
    4 pick internal::branch,
    >mark
    
    r0 r1 cmp_,_
    gt bc>
    4 pick internal::branch,

    >mark
    >mark

    drop

    [ armv6m-instr unimport ]
    internal::end-block
    here 1 forth::or 0 rot internal::literal!

    implicit-internal::try-end-implicit
  ;
  
  \ Redefine CASE
  : case ( x -- )
    [immediate]
    state @ forth::if
      postpone normalize
    else
      normalize
    then
    postpone case
  ;

  \ Redefine OF
  : of ( x -- )
    [immediate]
    [compile-only]
    postpone normalize
    postpone of
  ;

  \ Redefine .
  : . ( n -- )
    integral> .
  ;

  \ Redefine U.
  : u. ( u -- )
    integral> u.
  ;

  \ Redefine (.)
  : (.) ( n -- )
    integral> (.)
  ;

  \ Redefine (U.)
  : (u.) ( n -- )
    integral> (u.)
  ;

  \ Type a character
  : emit ( c -- )
    integral> emit
  ;

  \ Redefine H.1
  : h.1 ( x -- )
    integral> h.1
  ;

  \ Redefine H.2
  : h.2 ( x -- )
    integral> h.2
  ;

  \ Redefine H.4
  : h.4 ( x -- )
    integral> h.4
  ;

  \ Redefine H.8
  : h.8 ( x -- )
    integral> h.8
  ;

  \ Redefine H.16
  : h.16 ( d -- )
    2integral> h.16
  ;

  \ Redefine SPACES
  : spaces ( x -- )
    integral> spaces
  ;

  \ Get an arbitrary slice of a sequence
  : >slice { offset len seq -- slice }
    seq >raw { raw-seq }
    seq >raw-offset { raw-offset }
    seq >len { raw-len }
    offset 0>= averts x-offset-out-of-range
    len 0>= averts x-offset-out-of-range
    offset len + raw-len <= averts x-offset-out-of-range
    [ 3 >small-int ] literal slice-type allocate-cells { slice }
    raw-seq slice forth::cell+ !
    raw-offset offset + slice [ 2 forth::cells ] literal forth::+ !
    len slice [ 3 forth::cells ] literal forth::+ !
    slice
  ;

  \ Truncate the start of a sequence
  : truncate-start { count seq -- slice }
    seq >raw { raw-seq }
    seq >raw-offset { offset }
    seq >len { len }
    [ 3 >small-int ] literal slice-type allocate-cells { slice }
    raw-seq slice forth::cell+ !
    offset count + slice [ 2 forth::cells ] literal forth::+ !
    len count - slice [ 3 forth::cells ] literal forth::+ !
    slice
  ;

  \ Truncate the end of a sequence
  : truncate-end { count seq -- slice }
    seq >raw { raw-seq }
    seq >raw-offset { offset }
    seq >len { len }
    [ 3 >small-int ] literal slice-type allocate-cells { slice }
    raw-seq slice forth::cell+ !
    offset slice [ 2 forth::cells ] literal forth::+ !
    len count - slice [ 3 forth::cells ] literal forth::+ !
    slice
  ;

  \ Get the type of a value
  : >type ( value -- type )
    >type
  ;

  \ Get whether a value is integral
  : integral? ( value -- integral? )
    integral?
  ;
  
  \ Get whether a value is a small integer
  : small-int? ( value -- small-int? )
    small-int?
  ;
  
  \ Get a token
  : token ( runtime: "name" -- seq | 0 )
    token 2>integral dup 0<> if addr-len>bytes else 2drop 0 then
  ;

  \ Get a word from a token
  : token-word ( runtime: "name" -- word )
    token-word >integral { word }
    cell word-tag allocate-tagged { tagged-word }
    word 0 tagged-word t!+
    tagged-word
  ;

  \ Get an execution token from a word
  : word>xt ( word -- xt )
    dup >type tagged-type = averts x-incorrect-type
    dup >tag word-tag = averts x-incorrect-type
    xt-type allocate-cell { xt-value }
    0 swap t@+ integral> forth::>xt xt-value forth::cell+ !
    xt-value
  ;

  \ End lambda
  : ;] ( -- )
    [immediate]
    [compile-only]
    undefer-lit
    internal::get-syntax dup internal::syntax-lambda forth::= forth::if
      drop internal::drop-syntax
      word-exit-hook @ ?execute
      word-end-hook @ ?execute
      [ thumb-2? forth::not ] [if] internal::consts-inline, [then]
      $BD00 h,
      here rot internal::branch-back!
      forth::lit,
      xt-type lit, postpone allocate-cell
      postpone tuck
      postpone forth::cell+
      postpone !
    else
      internal::syntax-naked-lambda forth::=
      averts internal::x-unexpected-syntax
      internal::drop-syntax
      internal::syntax-word internal::push-syntax
      postpone ;
      xt-type allocate-cell
      tuck
      forth::cell+
      !
    then
  ;
  
  \ Execute a closure
  : execute ( xt -- )
    dup >type case
      xt-type of forth::cell+ @ endof
      closure-type of
        dup >size over forth::+ swap forth::cell+ { xt-addr }
        begin
          forth::cell forth::- dup @ swap dup xt-addr forth::=
        forth::until drop
        integral>
      endof
      ['] x-incorrect-type ?raise
    endcase
    execute
  ;
  
  \ Try a closure
  : try ( xt -- )
    dup >type case
      xt-type of forth::cell+ @ endof
      closure-type of
        dup >size over forth::+ swap forth::cell+ { xt-addr }
        begin
          forth::cell forth::- dup @ swap dup xt-addr forth::=
        forth::until drop
        integral>
      endof
      ['] x-incorrect-type ?raise
    endcase
    try >integral
  ;

  
  \ Execute a non-null closure
  : ?execute ( xt -- )
    dup integral? not if
      execute
    else
      integral> forth::0= averts x-incorrect-type
    then
  ;
  
  begin-module unsafe
    
    \ Get the address and size of a bytes or constant bytes value
    : bytes>addr-len ( value -- addr len )
      dup bytes? averts x-incorrect-type
      dup >len { len }
      dup >raw-offset { offset }
      >raw dup >type case
        bytes-type of
          forth::cell+ >integral offset +
        endof
        const-bytes-type of
          dup forth::cell+ @ >integral offset +
        endof
        ['] x-incorrect-type ?raise
      endcase
      len
    ;
    
    \ Redefine @
    : @ ( addr -- x )
      integral>
      dup averts x-null-dereference
      dup 3 forth::and triggers x-unaligned-dereference
      @ >integral
    ;
  
    \ Redefine !
    : ! ( x addr -- )
      integral>
      dup averts x-null-dereference
      dup 3 forth::and triggers x-unaligned-dereference
      swap integral> swap !
    ;

    \ Redefine +!
    : +! ( x addr -- )
      integral>
      dup averts x-null-dereference
      dup 3 forth::and triggers x-unaligned-dereference
      swap integral> swap +!
    ;

    \ Redefine BIS!
    : bis! ( x addr -- )
      integral>
      dup averts x-null-dereference
      dup 3 forth::and triggers x-unaligned-dereference
      swap integral> swap bis!
    ;
    
    \ Redefine BIC!
    : bic! ( x addr -- )
      integral>
      dup averts x-null-dereference
      dup 3 forth::and triggers x-unaligned-dereference
      swap integral> swap bic!
    ;

    \ Redefine XOR!
    : xor! ( x addr -- )
      integral>
      dup averts x-null-dereference
      dup 3 forth::and triggers x-unaligned-dereference
      swap integral> swap xor!
    ;

    \ Redefine H@
    : h@ ( addr -- h )
      integral>
      dup averts x-null-dereference
      dup 1 forth::and triggers x-unaligned-dereference
      h@ >integral
    ;
  
    \ Redefine H!
    : h! ( h addr -- )
      integral>
      dup averts x-null-dereference
      dup 1 forth::and triggers x-unaligned-dereference
      swap integral> swap h!
    ;
  
    \ Redefine H+!
    : h+! ( h addr -- )
      integral>
      dup averts x-null-dereference
      dup 1 forth::and triggers x-unaligned-dereference
      swap integral> swap h+!
    ;

    \ Redefine HBIS!
    : hbis! ( x addr -- )
      integral>
      dup averts x-null-dereference
      dup 1 forth::and triggers x-unaligned-dereference
      swap integral> swap hbis!
    ;
    
    \ Redefine HBIC!
    : hbic! ( x addr -- )
      integral>
      dup averts x-null-dereference
      dup 1 forth::and triggers x-unaligned-dereference
      swap integral> swap hbic!
    ;

    \ Redefine HXOR!
    : hxor! ( x addr -- )
      integral>
      dup averts x-null-dereference
      dup 1 forth::and triggers x-unaligned-dereference
      swap integral> swap hxor!
    ;

    \ Redefine C@
    : c@ ( addr -- c )
      integral>
      dup averts x-null-dereference
      c@ >integral
    ;
  
    \ Redefine C!
    : c! ( c addr -- )
      integral>
      dup averts x-null-dereference
      swap integral> swap c!
    ;

    \ Redefine C+!
    : c+! ( c addr -- )
      integral>
      dup averts x-null-dereference
      swap integral> swap c+!
    ;

    \ Redefine CBIS!
    : cbis! ( x addr -- )
      integral>
      dup averts x-null-dereference
      swap integral> swap cbis!
    ;
    
    \ Redefine CBIC!
    : cbic! ( x addr -- )
      integral>
      dup averts x-null-dereference
      swap integral> swap cbic!
    ;

    \ Redefine CXOR!
    : cxor! ( x addr -- )
      integral>
      dup averts x-null-dereference
      swap integral> swap hxor!
    ;

    \ Redefine MOVE
    : move { src dest bytes -- }
      src integral> to src
      dest integral> to dest
      bytes integral> to bytes
      src dest bytes move
    ;

    \ Redefine FILL
    : fill { addr bytes val -- }
      addr integral> to addr
      bytes integral> to bytes
      val integral> to val
      addr bytes val fill
    ;
    
    \ Cast a value from an integer
    : integral> ( value -- x ) integral> ;

    \ Get the HERE pointer
    : here ( -- x ) here >integral ;

    \ ALLOT space
    : allot ( x -- ) integral> allot ;
    
  end-module

  \ Type a string
  : type ( seq -- )
    unsafe::bytes>addr-len 2integral> type
  ;
  
  \ Get an xt at interpretation-time
  : ' ( "name" -- xt )
    token-word word>xt
  ;

  \ Get an xt at compile-time
  : ['] ( 'name" -- xt )
    [immediate]
    [compile-only]
    token-word word>xt xt>integral lit,
    postpone >xt
  ;

  \ Raise an exception
  : ?raise ( xt -- )
    dup 0<> if
      dup >type xt-type = averts x-incorrect-type
      forth::cell+ @ ?raise
    else
      drop
    then
  ;
  
  \ Assert that a value is true, otherwise raise a specified exception
  : averts ( f "name" -- )
    [immediate]
    forth::token-word forth::>xt
    state @ forth::if
      postpone 0=
      postpone if
      rot forth::lit,
      postpone forth::?raise
      postpone then
    else
      swap 0= if
        forth::?raise
      else
        drop
      then
    then
  ;

  \ Assert that a value is false, otherwise raise a specified exception
  : triggers ( f "name" -- )
    [immediate]
    forth::token-word forth::>xt
    state @ forth::if
      postpone 0<>
      postpone if
      rot forth::lit,
      postpone forth::?raise
      postpone then
    else
      swap 0<> if
        forth::?raise
      else
        drop
      then
    then
  ;

  \ Always raise an exception; this is needed due to issues with compiling code
  \ before zeptoscript is initialized
  : raise ( "name" -- )
    [immediate]
    forth::token-word forth::>xt
    state @ forth::if
      forth::lit, postpone forth::?raise
    else
      forth::?raise
    then
  ;
  
  \ Start compiling a word with a name
  : start-compile ( seq -- )
    unsafe::bytes>addr-len 2integral> internal::start-compile
  ;

  \ End compiling, exported to match start-compile
  : end-compile, ( -- )
    internal::end-compile,
  ;

  \ Define a constant with a name
  : constant-with-name ( x name -- )
    swap dup small-int? if
      swap unsafe::bytes>addr-len 2integral> internal::constant-with-name
    else
      swap start-compile visible lit, end-compile,
    then
  ;

  \ Redefine [CHAR]
  : [char] ( "name" -- x )
    [immediate]
    [compile-only]
    char >small-int lit,
  ;

  \ Redefine CHAR
  : char ( "name" -- x )
    char >small-int
  ;

  \ Begin declaring a record
  : begin-record ( "name" -- token offset )
    syntax-record internal::push-syntax
    token dup 0<> averts x-token-expected
    0
  ;

  \ Finish declaring a record
  : end-record ( token offset -- )
    syntax-record internal::verify-syntax internal::drop-syntax
    { name count }
    name >len { len }
    len 1+ make-bytes { accessor-name }
    [char] > 0 accessor-name c!+
    name 0 accessor-name [ 1 >small-int ] literal len copy
    accessor-name start-compile visible
    count lit,
    postpone >cells
    end-compile,
    [char] > len accessor-name c!+
    name 0 accessor-name 0 len copy
    accessor-name start-compile visible
    postpone cells-no-count>
    end-compile,
    s" make-" { make-prefix }
    [ 5 >small-int ] literal len + make-bytes to accessor-name
    make-prefix 0 accessor-name 0 [ 5 >small-int ] literal copy
    name 0 accessor-name [ 5 >small-int ] literal len copy
    accessor-name start-compile visible
    count lit,
    cells-type lit,
    postpone allocate-cells
    end-compile,
    s" -size" { size-suffix }
    name 0 accessor-name 0 len copy
    size-suffix 0 accessor-name len [ 5 >small-int ] literal copy
    count accessor-name constant-with-name
  ;

  \ Create a field in a record
  : item: ( offset "name" -- offset' )
    { offset }
    syntax-record internal::verify-syntax
    token dup 0<> averts x-token-expected { name }
    name >len { len }
    len 1+ make-bytes { accessor-name }
    name 0 accessor-name 0 len copy
    [char] @ len accessor-name c!+
    accessor-name start-compile visible
    offset lit,
    postpone swap
    postpone @+
    end-compile,
    [char] ! len accessor-name c!+
    accessor-name start-compile visible
    offset lit,
    postpone swap
    postpone !+
    end-compile,
    offset 1+
  ;
  
  \ Make a foreign word usable
  : foreign ( in-count out-count xt "name" -- )
    { in-count out-count xt }
    token dup 0<> averts x-token-expected
    start-compile visible
    in-count lit,
    postpone nintegral>
    xt xt>integral raw-lit, postpone forth::execute
    out-count lit,
    postpone n>integral
    internal::end-compile,
  ;

  \ Execute a foreign word
  : execute-foreign { in-count out-count xt -- }
    in-count nintegral>
    xt execute
    out-count n>integral
  ;

  continue-module zscript-internal

    \ Currently unsupported data type
    : x-currently-unsupported-data-type ( -- )
      ." currently unsupported data type" cr
    ;
    
    \ Compile adding to a cell variable
    : compile-add-cell-local { index -- }
      undefer-lit
      6 internal::push,
      index 128 forth::< forth::if
        [ armv6m-instr import ]
        index 4 forth::* tos ldr_,[sp,#_]
        [ armv6m-instr unimport ]
      else
        [ armv6m-instr import ]
        index 4 forth::* r0 internal::literal,
        r0 add4_,sp
        0 r0 tos ldr_,[_,#_]
        [ armv6m-instr unimport ]
      then
      postpone +
      index 128 forth::< forth::if
        [ armv6m-instr import ]
        index 4 forth::* tos str_,[sp,#_]
        [ armv6m-instr unimport ]
      else
        [ armv6m-instr import ]
        index 4 forth::* r0 internal::literal,
        r0 add4_,sp
        0 r0 tos str_,[_,#_]
        [ armv6m-instr unimport ]
      then
      6 internal::pull,
    ;
    
    \ Add to a local variable
    : parse-add-local ( name -- match? )
      unsafe::bytes>addr-len 2integral> 2>r 0 internal::local-buf-top @ begin
        dup internal::local-buf-bottom @ forth::<
      forth::while
        dup forth::1+ forth::count 2r@ forth::equal-case-strings? forth::if
          rdrop rdrop c@ forth::case
            internal::cell-local forth::of compile-add-cell-local endof
            internal::cell-addr-local forth::of compile-add-cell-local endof
            raise x-currently-unsupported-data-type
            \ double-local of compile-add-double-local endof
            \ double-addr-local of compile-add-double-local endof
          endcase
          true exit
        else
          dup c@ dup internal::double-local forth::=
          swap internal::double-addr-local forth::= forth::or forth::if
            forth::1+ dup c@ forth::1+ forth::+ swap 2 forth::+ swap
          else
            forth::1+ dup c@ forth::1+ forth::+ swap forth::1+ swap
          then
        then
      repeat
      rdrop rdrop 2drop false
    ;

    \ Compile adding to a VALUE
    : compile-+to-value ( xt -- )
      internal::value-addr@ forth::lit,
      postpone dup
      postpone forth::@
      postpone rot
      postpone +
      postpone swap
      postpone forth::!
    ;
    
  end-module

  \ Find a word
  : find ( seq -- word|0 )
    unsafe::bytes>addr-len 2integral> find dup forth::if
      >integral { word }
      forth::cell word-tag allocate-tagged { tagged-word }
      word 0 tagged-word t!+
      tagged-word
    then
  ;
  
  \ Add to a local or a VALUE
  \ Do not execute this after this point before zeptoscript is initialized
  : +to ( x "name" -- )
    [immediate]
    token dup 0<> averts x-token-expected
    state? if
      dup parse-add-local not if
        find dup 0<> averts x-unknown-word word>xt compile-+to-value
      else
        drop
      then
    else
      find dup 0<> averts x-unknown-word word>xt xt>integral integral>
      internal::value-addr@ tuck @ + swap !
    then
  ;

  \ Define a CONSTANT
  : constant ( x "name" -- )
    dup small-int? if
      constant
    else
      token dup 0<> averts x-token-expected
      start-compile visible
      lit,
      end-compile,
    then
  ;

  \ Define a 2CONSTANT
  : 2constant ( x0 x1 "name" -- )
    dup small-int? over small-int? and if
      2constant
    else
      token dup 0<> averts x-token-expected
      start-compile visible
      swap lit, lit,
      end-compile,
    then
  ;
  
  continue-module zscript-internal

    \ Get a RAM global at an index
    : ram-global@ ( index -- x )
      ram-globals-array@ @+
    ;

    \ Set a RAM global at an index
    : ram-global! ( x index -- )
      ram-globals-array@ !+
    ;

    \ Get a flash global at an index
    : flash-global@ ( index -- x )
      flash-globals-array@ @+
    ;

    \ Set a flash global at an index
    : flash-global! ( x index -- )
      flash-globals-array@ !+
    ;

    \ Create a RAM global
    : ram-global ( "name" -- )
      token dup 0<> averts x-token-expected { name }
      name >len { len }
      current-ram-global-id-index ram-global@ { index }
      index 1+ current-ram-global-id-index ram-global!
      index 1+ cells-type allocate-cells { new-globals }
      new-globals new-ram-globals-array!
      ram-globals-array@ 0 new-ram-globals-array@ 0 index copy
      new-ram-globals-array@ ram-globals-array!
      len 1+ allocate-bytes { accessor-name }
      name 0 accessor-name 0 len copy
      forth::[char] @ >integral len accessor-name c!+
      accessor-name start-compile visible
      index lit,
      postpone ram-global@
      end-compile,
      forth::[char] ! >integral len accessor-name c!+
      accessor-name start-compile visible
      index lit,
      postpone ram-global!
      end-compile,
    ;

    \ Create a flash global
    : flash-global ( "name" -- )
      token dup 0<> averts x-token-expected { name }
      name >len { len }
      get-current-flash-global-id { index }
      len 1+ allocate-bytes { accessor-name }
      name 0 accessor-name 0 len copy
      forth::[char] @ >integral len accessor-name c!+
      accessor-name start-compile visible
      index lit,
      postpone flash-global@
      end-compile,
      forth::[char] ! >integral len accessor-name c!+
      accessor-name start-compile visible
      index lit,
      postpone flash-global!
      end-compile,
      index 1+ set-current-flash-global-id
    ;
    
  end-module

  \ Create a global
  : global ( "name" -- )
    compiling-to-flash? if flash-global else ram-global then
  ;
  
  \ Bind a scope to a lambda
  : bind ( xn ... x0 count xt -- closure )
    dup >type xt-type = averts x-incorrect-type
    swap { arg-count }
    arg-count 1+ closure-type allocate-cells { closure }
    closure forth::cell+
    swap xt>integral over ! forth::cell+
    begin arg-count 0> while
      tuck ! forth::cell+
      arg-count 1- to arg-count
    repeat
    drop
    closure
  ;
  
  \ Redefine PICK
  : pick ( xn ... x0 u -- x )
    integral> pick
  ;

  \ Redefine ROLL
  : roll ( xn ... x0 u -- xn-1 ... x0 xn u )
    integral> roll
  ;

  \ Set BASE
  : base! ( base -- )
    integral> base forth::!
  ;

  \ Get BASE
  : base@ ( -- base )
    base forth::@ >integral
  ;

  \ Execute an xt with a BASE
  : with-base ( base xt -- )
    base@ { old-base }
    swap base! execute old-base base!
  ;

  \ Parse an integer
  : parse-integer ( seq -- n success )
    unsafe::bytes>addr-len 2integral> parse-integer 2>integral
  ;

  \ Duplicate a cell or byte sequence; this converts slices to non-slices and
  \ constant byte sequences into non-constant byte sequences
  : duplicate { seq0 -- seq1 }
    seq0 cells? if
      seq0 >len { len }
      len make-cells { seq1 }
      seq0 0 seq1 0 len copy
      seq1
    else
      seq0 bytes? averts x-incorrect-type
      seq0 >len { len }
      len make-bytes { seq1 }
      seq0 0 seq1 0 len copy
      seq1
    then
  ;
  
  \ Concatenate two cell or byte sequences
  : concat { seq0 seq1 -- seq2 }
    seq0 cells? if
      seq1 cells? averts x-incorrect-type
      seq0 >len { len0 }
      seq1 >len { len1 }
      len0 len1 + make-cells { seq2 }
      seq0 0 seq2 0 len0 copy
      seq1 0 seq2 len0 len1 copy
      seq2
    else
      seq0 bytes? averts x-incorrect-type
      seq1 bytes? averts x-incorrect-type
      seq0 >len { len0 }
      seq1 >len { len1 }
      len0 len1 + make-bytes { seq2 }
      seq0 0 seq2 0 len0 copy
      seq1 0 seq2 len0 len1 copy
      seq2
    then
  ;

  \ Iterate over a cell sequence
  : iter { seq xt -- }
    seq cells? if
      seq >len 0 ?do i seq @+ xt execute loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ xt execute loop
    then
  ;

  \ Map a cell or byte sequence into a new cell sequence
  : map { seq xt -- seq' }
    seq cells? if
      seq >len { len }
      len make-cells { seq' }
      len 0 ?do i seq @+ xt execute i seq' !+ loop
      seq'
    else
      seq bytes? averts x-incorrect-type
      seq >len { len }
      len make-bytes { seq' }
      len 0 ?do i seq c@+ xt execute i seq' c!+ loop
      seq'
    then
  ;

  \ Map a cell or byte sequence in place
  : map! { seq xt -- }
    seq cells? if
      seq >len 0 ?do i seq @+ xt execute i seq !+ loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ xt execute i seq c!+ loop
    then      
  ;

  \ Make a zeroed bit sequence
  : make-bits { len -- bits }
    len [ 8 >small-int ] literal align
    [ 3 >small-int ] literal rshift cell+ make-bytes { bits }
    len 0 bits w!+
    bits
  ;

  \ Get the length of bits
  : bits>len ( bits -- len )
    0 swap w@+
  ;

  \ Set a bit in a bit sequence
  : bit! { bit index bits -- }
    index bits bits>len u< averts x-offset-out-of-range
    index [ 3 >small-int ] literal rshift cell+ { index' }
    index' bits c@+ bit if
      [ 1 >small-int ] literal index [ $7 >small-int ] literal and lshift or
    else
      [ 1 >small-int ] literal index [ $7 >small-int ] literal and lshift bic
    then
    index' bits c!+
  ;
  
  \ Get a bit in a bit sequence
  : bit@ { index bits -- }
    index bits bits>len u< averts x-offset-out-of-range
    index [ 3 >small-int ] literal rshift cell+ bits c@+
    [ 1 >small-int ] literal index [ $7 >small-int ] literal and lshift and 0<>
  ;

  \ Filter a cell or byte sequence
  : filter { seq xt -- seq' }
    seq cells? seq bytes? or averts x-incorrect-type
    seq >len { len }
    len make-bits { bits }
    0 { count }
    len 0 ?do
      i seq @+ xt execute if
        true i bits bit!
        count [ 1 >small-int ] literal + to count
      then
    loop
    seq cells? if
      count make-cells { seq' }
      0 { current }
      len 0 ?do
        i bits bit@ if
          i seq @+ current seq' !+
          current [ 1 >small-int ] literal + to current
        then
      loop
      seq'
    else
      count make-bytes { seq' }
      0 { current }
      len 0 ?do
        i bits bit@ if
          i seq c@+ current seq' c!+
          current [ 1 >small-int ] literal + to current
        then
      loop
      seq'
    then
  ;

  \ Fold left over a cell or byte sequence
  : fold-left ( x ) { seq xt -- }
    seq cells? if
      seq >len 0 ?do i seq @+ xt execute loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ xt execute loop
    then
  ;

  \ Fold right over a cell or byte sequence
  : fold-right ( x ) { seq xt -- }
    seq cells? if
      seq >len dup 0> if
        0 swap 1- ?do i seq @+ swap xt execute -1 +loop
      else
        drop
      then
    else
      seq bytes? averts x-incorrect-type
      seq >len dup 0> if
        0 swap 1- ?do i seq c@+ swap xt execute -1 +loop
      else
        drop
      then
    then
  ;

  \ Reverse a sequence producing a new sequence
  : reverse { seq -- seq' }
    seq >len { len }
    seq cells? if
      len make-cells { seq' }
      len 0 ?do i seq @+ len 1- i - seq' !+ loop
      seq'
    else
      seq bytes? averts x-incorrect-type
      len make-bytes { seq' }
      len 0 ?do i seq c@+ len 1- i - seq' c!+ loop
      seq'
    then
  ;

  \ Reverse a sequence in place
  : reverse! { seq -- }
    seq >len { len }
    seq cells? if
      len [ 1 >small-int ] literal rshift 0 ?do
        len 1- i - { rev-i }
        i seq @+ rev-i seq @+ i seq !+ rev-i seq !+
      loop
    else
      seq bytes? averts x-incorrect-type
      len [ 1 >small-int ] literal rshift 0 ?do
        len 1- i - { rev-i }
        i seq c@+ rev-i seq c@+ i seq c!+ rev-i seq c!+
      loop
    then
  ;

  continue-module zscript-internal

    \ Partition a cell sequence for quicksort
    : partition-cells! { lo hi seq xt -- i }
      hi seq @+ { pivot }
      [ -1 >small-int ] literal { x }
      hi 0 ?do
        i seq @+ { item }
        item pivot xt execute if
          x 1+ to x
          x seq @+ { x-item }
          x-item i seq !+
          item x seq !+
        then
      loop
      x 1+ to x
      x seq @+ { x-item }
      hi seq @+ { hi-item }
      hi-item x seq !+
      x-item hi seq !+
      x
    ;

    \ Quicksort a cell sequence in place for quicksort
    : qsort-cells! { lo hi seq xt -- } \ xt is ( x0 x1 -- le? )
      lo hi < lo 0>= and if
        lo hi seq xt partition-cells!
        lo over 1- seq xt recurse
        1+ hi seq xt recurse
      then
    ;

    \ Partition a byte sequence for quicksort
    : partition-bytes! { lo hi seq xt -- i }
      hi seq c@+ { pivot }
      [ -1 >small-int ] literal { x }
      hi 0 ?do
        i seq c@+ { item }
        item pivot xt execute if
          x 1+ to x
          x seq c@+ { x-item }
          x-item i seq c!+
          item x seq c!+
        then
      loop
      x 1+ to x
      x seq c@+ { x-item }
      hi seq c@+ { hi-item }
      hi-item x seq c!+
      x-item hi seq c!+
      x
    ;

    \ Quicksort a byte sequence in place for quicksort
    : qsort-bytes! { lo hi seq xt -- } \ xt is ( x0 x1 -- le? )
      lo hi < lo 0>= and if
        lo hi seq xt partition-bytes!
        lo over 1- seq xt recurse
        1+ hi seq xt recurse
      then
    ;

  end-module

  \ Quicksort a cell or byte sequence in place
  : qsort! { seq xt -- }
    seq cells? if
      0 seq >len 1- seq xt qsort-cells!
    else
      seq bytes? averts x-incorrect-type
      0 seq >len 1- seq xt qsort-bytes!
    then
  ;

  \ Quicksort a cell or byte sequence, copying it
  : qsort ( seq xt -- )
    swap duplicate tuck swap qsort!
  ;
  
  \ Unsafe operations raising exceptions outside of UNSAFE module
  : @ raise x-unsafe-op ;
  : ! raise x-unsafe-op ;
  : +! raise x-unsafe-op ;
  : bis! raise x-unsafe-op ;
  : bic! raise x-unsafe-op ;
  : xor! raise x-unsafe-op ;
  : h@ raise x-unsafe-op ;
  : h! raise x-unsafe-op ;
  : h+! raise x-unsafe-op ;
  : hbis! raise x-unsafe-op ;
  : hbic! raise x-unsafe-op ;
  : hxor! raise x-unsafe-op ;
  : c@ raise x-unsafe-op ;
  : c! raise x-unsafe-op ;
  : c+! raise x-unsafe-op ;
  : cbis! raise x-unsafe-op ;
  : cbic! raise x-unsafe-op ;
  : cxor! raise x-unsafe-op ;
  : move raise x-unsafe-op ;
  : fill raise x-unsafe-op ;
  : here raise x-unsafe-op ;
  : allot raise x-unsafe-op ;

  \ Empty cells
  \
  \ This is not garbage collected so does not need to be in the heap.
  forth::here forth::constant empty-cells
  cells-type integral> 2 forth::-
  type-shift forth::lshift
  forth::cell 1 forth::lshift forth::or ,

  \ Empty bytes
  \
  \ This is not garbage collected so does not need to be in the heap.
  forth::here forth::constant empty-bytes
  bytes-type integral> 2 forth::-
  type-shift forth::lshift
  forth::cell 1 forth::lshift forth::or ,
  
end-module
