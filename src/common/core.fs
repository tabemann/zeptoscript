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
  : x-unaligned-access ." unaligned access" cr ;

  \ Unsafe operation
  : x-unsafe-op ." unsafe operation" cr ;

  \ Value not small integer
  : x-not-small-int ." not small int" cr ;

  \ No sequence being defined
  : x-no-seq-being-defined ." no sequence being defined" cr ;

  \ Wrong sequence type
  : x-wrong-seq-type ." wrong sequence type" cr ;
  
  begin-module zscript-internal

    \ Are we initialized
    false value inited?

    \ Are we in zeptoscript
    false value in-zscript?

    \ Default compile-time heap size
    65536 constant default-compile-size

    \ Default runtime heap size
    65536 constant default-runtime-size

    \ constant bytes syntax
    254 constant syntax-const-bytes
    
    \ Saved handle-number-hook
    0 value saved-handle-number-hook

    \ Saved find-hook
    0 value saved-find-hook
    
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
      to-space-top@ next-current >= averts x-out-of-memory
      dup current size move
      current 1 or swap !
      next-current to-space-current!
      current
    ;

    \ Relocate the stack
    : relocate-stack ( -- )
      sp@ stack-base @ swap begin 2dup > while
        dup @ relocate over !
        cell+
      repeat
      2drop
      rp@ rstack-base @ swap begin 2dup > while
        dup @ relocate over !
        cell+
      repeat
      2drop
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
      flash-globals-array@ relocate flash-globals-array!
      to-space-bottom@ { gc-current }
      begin gc-current to-space-current@ u< while
        gc-current @ { header }
        header size-mask and 1 rshift cell align { aligned-size }
        header [ has-values type-shift lshift ] literal and if
          gc-current aligned-size + { gc-current-end }
          gc-current cell+ begin dup gc-current-end u< while
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

    \ Get whether a value is not false
    : flag> ( x -- flag )
      code[
      0 tos cmp_,#_
      ne bc>
      pc 1 pop
      >mark
      1 r0 movs_,#_
      r0 tos tst_,_
      eq bc>
      r0 tos cmp_,_
      eq bc>
      0 tos movs_,#_
      tos tos mvns_,_
      pc 1 pop
      >mark
      0 tos movs_,#_
      pc 1 pop
      >mark
      0 tos r0 ldr_,[_,#_]
      type-shift r0 r0 lsrs_,_,#_
      1 r0 cmp_,#_
       eq bc>
      0 tos movs_,#_
      tos tos mvns_,_
      pc 1 pop
      >mark
      4 tos r0 ldr_,[_,#_]
      0 r0 cmp_,#_
      eq bc>
      0 tos movs_,#_
      tos tos mvns_,_
      pc 1 pop
      >mark
      r0 tos movs_,#_
      ]code
    ;

    \ Types
    0 >small-int constant null-type
    1 >small-int constant int-type
    2 >small-int constant bytes-type
    3 >small-int constant big-int-type
    4 >small-int constant double-type
    5 >small-int constant const-bytes-type
    6 >small-int constant xt-type
    7 >small-int constant tagged-type
    8 >small-int constant symbol-type
    10 >small-int constant cells-type
    11 >small-int constant closure-type
    12 >small-int constant slice-type
    13 >small-int constant save-type
    14 >small-int constant ref-type
    15 >small-int constant force-type

    \ Tags
    1 >small-int constant word-tag

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
      1 r0 cmp_,#_
      ne bc>
      cell tos tos ldr_,[_,#_]
      pc 1 pop
      >mark
      ]code
      ['] x-incorrect-type ?raise
    ;

    \ Cast doubles to cells, validating them
    : double> ( double -- x0 x1 )
      code[
      0 tos cmp_,#_
      eq bc>
      1 r0 movs_,#_
      r0 tos tst_,_
      ne bc>
      0 tos r0 ldr_,[_,#_]
      type-shift r0 r0 lsrs_,_,#_
      double-type integral> 2 - r0 cmp_,#_
      ne bc>
      4 r7 subs_,#_
      8 r6 r0 ldr_,[_,#_]
      0 r7 r0 str_,[_,#_]
      4 r6 r6 ldr_,[_,#_]
      pc 1 pop
      >mark
      >mark
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
      big-int-type integral> 2 - tos cmp_,#_
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
    : validate-word ( value -- ) >type big-int-type = averts x-incorrect-type ;
    
    \ Get whether a value is an xt or closure
    : xt? ( value -- xt? )
      >type dup xt-type = swap closure-type = or
    ;

    \ Preemptively carry out a GC cycle to ensure that a certain amount of space
    \ is available
    : ensure { bytes -- }
      bytes integral> to-space-current@ + to-space-top@ > if
        gc
        bytes integral> to-space-current@ + to-space-top@
        <= averts x-out-of-memory
      then
    ;

    \ Low level routine used to check whether enough un-GC'ed space is available
    : needs-gc? ( bytes -- )
      to-space-current@ + to-space-top@ >
    ;

    \ Allocate memory as cells
    : allocate-cells { count type -- addr }
      count integral> 1+ cells >small-int { bytes }
      bytes integral> to-space-current@ + to-space-top@ > if
        gc
        bytes integral> to-space-current@ +
        to-space-top@ <= averts x-out-of-memory
      then
      to-space-current@ { current }
      bytes integral> to bytes
      bytes current + to-space-current!
      current
      dup cell+ bytes cell - 0 fill
      bytes 1 lshift type integral> 2 - type-shift lshift or over !
    ;
    
    \ Allocate a reference
    : allocate-ref { x -- addr }
      to-space-current@ [ 2 cells ] literal + to-space-top@ > if
        gc
        to-space-current@ [ 2 cells ] literal + to-space-top@ <=
        averts x-out-of-memory
      then
      to-space-current@ { current }
      current [ 2 cells ] literal + to-space-current!
      current
      x over cell+ !
      [ ref-type integral> 2 - type-shift lshift
      2 cells 1 lshift or ] literal over !
    ;

    \ Allocate memory as bytes
    : allocate-bytes { count type -- addr }
      count integral> cell+ cell align >small-int { bytes }
      bytes integral> to-space-current@ + to-space-top@ > if
        gc
        bytes integral> to-space-current@ +
        to-space-top@ <= averts x-out-of-memory
      then
      to-space-current@ { current }
      bytes integral> to bytes
      bytes current + to-space-current!
      current
      dup cell+ bytes cell - 0 fill
      count integral> cell+ 1 lshift
      type integral> 2 - type-shift lshift or over !
    ;

    \ Allocate a word
    : >big-int { x -- addr }
      to-space-current@ [ 2 cells ] literal + to-space-top@ > if
        x 1 and { lowest }
        x 1 or to x
        gc
        x 1 bic lowest or to x
        to-space-current@ [ 2 cells ] literal + to-space-top@ <=
        averts x-out-of-memory
      then
      to-space-current@ { current }
      current [ 2 cells ] literal + to-space-current!
      current
      x over cell+ !
      [ big-int-type integral> 2 - type-shift lshift 2 cells 1 lshift or ]
      literal over !
    ;
    
    \ Allocate a cell
    : allocate-cell { x type -- addr }
      to-space-current@ [ 2 cells ] literal + to-space-top@ > if
        x 1 and { lowest }
        x 1 or to x
        gc
        x 1 bic lowest or to x
        to-space-current@ [ 2 cells ] literal + to-space-top@ <=
        averts x-out-of-memory
      then
      to-space-current@ { current }
      current [ 2 cells ] literal + to-space-current!
      current
      x over cell+ !
      type integral> 2 - type-shift lshift
      [ 2 cells 1 lshift ] literal or over !
    ;

    \ Allocate a double-word
    : >double { x0 x1 -- addr }
      to-space-current@ [ 3 cells ] literal + to-space-top@ > if
        x0 1 and { lowest0 }
        x1 1 and { lowest1 }
        x0 1 or to x0
        x1 1 or to x1
        gc
        x0 1 bic lowest0 or to x0
        x1 1 bic lowest1 or to x1
        to-space-current@ [ 3 cells ] literal + to-space-top@
        <= averts x-out-of-memory
      then
      to-space-current@ { current }
      current [ 3 cells ] literal + to-space-current!
      x0 x1 current cell+ 2!
      current
      [ double-type integral> 2 - type-shift lshift 3 cells 1 lshift or ]
      literal over !
    ;

    \ Allocate a double-word
    : allocate-2cell { x0 x1 type -- addr }
      to-space-current@ [ 3 cells ] literal + to-space-top@ > if
        x0 1 and { lowest0 }
        x1 1 and { lowest1 }
        x0 1 or to x0
        x1 1 or to x1
        gc
        x0 1 bic lowest0 or to x0
        x1 1 bic lowest1 or to x1
        to-space-current@ [ 3 cells ] literal + to-space-top@
        <= averts x-out-of-memory
      then
      to-space-current@ { current }
      current [ 3 cells ] literal + to-space-current!
      x0 current cell+ !
      x1 current [ 2 cells ] literal + !
      current
      type integral> 2 - type-shift lshift
      [ 3 cells 1 lshift ] literal or over !
    ;

    \ Allocate a tagged value
    : allocate-tagged { count tag -- addr }
      count integral> [ 2 cells ] literal + cell align >small-int { bytes }
      bytes integral> to-space-current@ + to-space-top@ > if
        gc
        bytes integral> to-space-current@ +
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
      >big-int
    ;

    \ Construct a saved state
    : allocate-save ( -- save )
      handler @ >integral { handler }
      sp@ stack-base @ swap - cell -
      rp@ rstack-base @ swap - [ 2 cells ] literal -
      2dup >small-int swap >small-int { rstack-count stack-count }
      + [ 4 cells ] literal + >small-int { bytes }
      bytes integral> to-space-current@ + to-space-top@ > if
        gc
        bytes integral> to-space-current@ +
        to-space-top@ <= averts x-out-of-memory
      then
      to-space-current@ { current }
      bytes integral> to bytes
      bytes current + to-space-current!
      current
      bytes 1 lshift
      [ save-type integral> 2 - type-shift lshift ] literal or over !
      stack-count over cell+ !
      rstack-count over [ 2 cells ] literal + !
      handler over [ 3 cells ] literal + !
      stack-count integral> to stack-count
      rstack-count integral> to rstack-count
      sp@ [ 2 cells ] literal +
      over [ 4 cells ] literal + stack-count move
      rp@ [ 6 cells ] literal +
      over [ 4 cells ] literal + stack-count + rstack-count move
    ;

    \ Convert a pair of cells to nulls, integers, or words
    : 2>integral ( x0 x1 -- 0|int|addr 0|int|addr )
      4 cells needs-gc? if
        2 cells [: { buffer }
          buffer 2!
          gc
          4 cells needs-gc? triggers x-out-of-memory
          buffer 2@
        ;] with-aligned-allot
      then
      >integral swap >integral swap
    ;

    \ Convert a pair of nulls, integers, or words to cells
    : 2integral> ( 0|int|addr 0|int|addr -- x0 x1 )
      integral> swap integral> swap
    ;

    \ Convert a pair of cell-pairs into doubles
    : 2>double ( x0 x1 x2 x3 -- double0 double1 )
      6 cells needs-gc? if
        4 cells [: { buffer }
          buffer 2!
          buffer [ 2 cells ] literal + 2!
          gc
          6 cells needs-gc? triggers x-out-of-memory
          buffer [ 2 cells ] literal + 2@
          buffer 2@
        ;] with-aligned-allot
      then
      >double -rot >double swap
    ;

    \ Convert a pair of doubles into cell-pairs
    : 2double> ( double0 double1 -- x0 x1 x2 x3 )
      double> rot double> 2swap
    ;

    \ Convert any number of cells to nulls, integers, or words
    \ Note: do not call with a count of 0 or 1
    : n>integral ( xn ... x0 count -- 0|int|addr ... 0|int|addr )
      dup integral> cells [:
        { count buf }
        buf count integral> cells + buf ?do i ! cell +loop
        buf dup count integral> 1- cells + ?do
          i @ >integral
        [ cell negate ] literal +loop
      ;] with-aligned-allot
    ;

    \ Convert any number of nulls, integers, or words to cells
    \ Note: do not call with a count of 0 or 1
    : nintegral> ( 0|int|addr ... 0|int|addr count -- xn ... x0 )
      dup integral> cells [:
        { count buf }
        buf count integral> cells + buf ?do i ! cell +loop
        buf dup count integral> 1- cells + ?do
          i @ integral>
        [ cell negate ] literal +loop
      ;] with-aligned-allot
    ;

    \ Cast cells to xts
    : integral>xt ( x -- xt )
      integral> xt-type allocate-cell
    ;

    \ Convert an xt to an integral
    : xt>integral ( xt -- integral )
      dup >type xt-type = averts x-incorrect-type
      forth::cell+ @ >integral
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

    \ RAM global ID index
    0 constant current-ram-global-id-index

    \ Sequence definition index
    1 >small-int constant seq-define-index
    
    \ Initial RAM global ID
    2 >small-int constant init-ram-global-id

    \ Initial RAM globals count
    2 >small-int constant init-ram-global-count

    \ Cell sequence definition type
    0 >small-int constant define-cells

    \ Byte sequence definition type
    1 >small-int constant define-bytes
    
  end-module> import

  \ Types
  null-type constant null-type
  int-type constant int-type
  bytes-type constant bytes-type
  big-int-type constant big-int-type
  double-type constant double-type
  const-bytes-type constant const-bytes-type
  xt-type constant xt-type
  tagged-type constant tagged-type
  symbol-type constant symbol-type
  cells-type constant cells-type
  closure-type constant closure-type
  slice-type constant slice-type
  save-type constant save-type
  ref-type constant ref-type
  force-type constant force-type
  
  \ Ensure that a number of bytes are available in the heap without using said
  \ bytes triggering GC
  : ensure ( bytes -- ) ensure ;
  
  \ Get the raw LIT,
  : raw-lit, ( x -- ) integral> lit, ;
  
  \ Redefine LIT,
  : lit, ( x -- )
    dup small-int? if
      lit,
    else
      dup >type case
        big-int-type of
          integral> lit, postpone >integral
        endof
        double-type of
          double> swap lit, lit, postpone >double
        endof
        ['] x-incorrect-type ?raise
      endcase
    then
  ;

  \ Convert an address/length pair into constant bytes
  : addr-len>const-bytes ( c-addr u -- const-bytes )
    2integral> const-bytes-type allocate-2cell
  ;

  \ Convert an address/length pair into bytes
  : addr-len>bytes ( c-addr u -- bytes )
    dup bytes-type allocate-bytes { bytes }
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
        swap [ 2 cells ] literal + @
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

  \ Create a reference
  : ref ( x -- ref )
    allocate-ref
  ;

  \ Get a reference's value
  : ref@ ( ref -- x )
    dup >type ref-type = averts x-incorrect-type
    cell+ @
  ;

  \ Set a reference's value
  : ref! ( x ref -- )
    dup >type ref-type = averts x-incorrect-type
    cell+ !
  ;

  \ Create a cell sequence initialized to null
  : make-cells ( count -- cells )
    cells-type allocate-cells
  ;

  \ Create a byte sequence initialized to null
  : make-bytes ( count -- bytes )
    bytes-type allocate-bytes
  ;

  \ Create a cell sequence from elements on the stack
  : >cells ( xn ... x0 count -- cells )
    dup cells-type allocate-cells { seq }
    integral> seq over 1+ cells + swap 0 ?do cell - tuck ! loop drop
    seq
  ;

  \ Create a byte sequence from bytes on the stack
  : >bytes ( cn ... c0 count -- bytes )
    dup bytes-type allocate-bytes { seq }
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
        dup 3 and triggers x-unaligned-access
        +
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        swap integral> swap
        over cell+ len u<= averts x-offset-out-of-range
        over 3 and triggers x-unaligned-access
        cell+ @ +
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        slice >raw-offset { offset }
        slice >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        raw >type case
          bytes-type of
            len index 4 + u>= averts x-offset-out-of-range
            index 3 and triggers x-unaligned-access
            raw index offset + cell+ +
          endof
          const-bytes-type of
            len index 4 + u>= averts x-offset-out-of-range
            index 3 and triggers x-unaligned-access
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
        dup 3 and triggers x-unaligned-access
        + 
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        slice >raw-offset { offset }
        slice >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        raw >type case
          bytes-type of
            len index 4 + u>= averts x-offset-out-of-range
            index 3 and triggers x-unaligned-access
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
        dup 1 and triggers x-unaligned-access
        +
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        swap integral> swap
        over 2 + len u<= averts x-offset-out-of-range
        over 1 and triggers x-unaligned-access
        cell+ @ +
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        slice >raw-offset { offset }
        slice >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        raw >type case
          bytes-type of
            len index 2 + u>= averts x-offset-out-of-range
            index 1 and triggers x-unaligned-access
            raw index offset + cell+ +
          endof
          const-bytes-type of
            len index 2 + u>= averts x-offset-out-of-range
            index 1 and triggers x-unaligned-access
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
        dup 1 and triggers x-unaligned-access
        + 
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        slice >raw-offset { offset }
        slice >len { len }
        offset integral> to offset
        len integral> to len
        index integral> to index
        raw >type case
          bytes-type of
            len index 2 + u>= averts x-offset-out-of-range
            index 1 and triggers x-unaligned-access
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
        slice >len { len }
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
        slice >len { len }
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

  \ Get a cell in a cell sequence or slice and a byte in a byte sequence or
  \ slice
  : x@+ ( index object -- x )
    dup >type case
      cells-type of
        swap integral> 1+ cells
        over >size over u> averts x-offset-out-of-range
        + @
      endof
      bytes-type of
        swap integral> cell+
        over >size over u> averts x-offset-out-of-range
        + c@ >integral
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        swap integral> swap
        over len u< averts x-offset-out-of-range
        cell+ @ + c@ >integral
      endof
      slice-type of
        { index slice }
        slice >raw { raw }
        raw >type case
          cells-type of
            slice >raw-offset { offset }
            slice >len { len }
            offset integral> to offset
            len integral> to len
            index integral> to index
            len index u> averts x-offset-out-of-range
            raw index offset + 1+ cells + @
          endof
          bytes-type of
            begin
              slice >raw-offset { offset }
              slice >len { len }
              offset integral> to offset
              len integral> to len
              index integral> to index
              len index u> averts x-offset-out-of-range
              raw index offset + cell+ + c@
            end
            >integral
          endof
          const-bytes-type of
            begin
              slice >raw-offset { offset }
              slice >len { len }
              offset integral> to offset
              len integral> to len
              index integral> to index
              len index u> averts x-offset-out-of-range
              raw cell+ @ index offset + + c@
            end
            >integral
          endof
          ['] x-incorrect-type ?raise
        endcase
      endof
      ['] x-incorrect-type ?raise
    endcase
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

  continue-module zscript-internal
    
    \ Are we using old-style modules?
    1 value old-style-modules

    \ New style flag
    $FF000000 constant new-style-flag
    
    \ Is a module new-style
    : new-style? ( module -- flag ) 1 arshift new-style-flag and 0<> ;
    
    \ Filter out the new-style flag
    : filter-new-style-flag ( module -- module' ) integral> new-style-flag bic ;

    \ Make new-style module id
    : make-new-style ( module -- module' ) new-style-flag or >small-int ;

    \ Find a particular word in a provided module
    : do-find-with-module ( c-addr u -- word|0 )
      2dup internal::find-path-sep dup -1 <> if
        2 pick over internal::old-find-hook @ execute ?dup if
          >r 2 + tuck - -rot + swap r> -rot 2>r
          >r get-order r> >xt execute
          dup new-style? if
            filter-new-style-flag
            1 set-order 2r> find >r set-order r>
          else
            old-style-modules 1+ to old-style-modules
            1 set-order 2r> find >r set-order r>
            old-style-modules 1- to old-style-modules
          then
        else
          2drop drop 0
        then
      else
        drop internal::old-find-hook @ execute
      then
    ;

  end-module
  
  \ Initialize zeptoscript
  defer init-zscript
  :noname { compile-size runtime-size -- }
    inited? if exit then
    compile-size [ 2 cells ] literal align to compile-size
    runtime-size [ 2 cells ] literal align to runtime-size
    compiling-to-flash? if
      s" init" flash-latest find-all-dict
      get-current swap
      forth set-current
      s" init" internal::start-compile visible
      ?dup if forth::>xt forth::lit, postpone execute then
      runtime-size forth::lit, runtime-size forth::lit, postpone init-zscript
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
    0 flash-globals-array!
    init-ram-global-count cells-type allocate-cells { ram-globals }
    init-ram-global-id current-ram-global-id-index ram-globals !+
    ram-globals ram-globals-array!
    get-current-flash-global-id
    cells-type allocate-cells flash-globals-array!
    handle-number-hook @ to saved-handle-number-hook
    ['] do-handle-number handle-number-hook !
    zscript 1 set-order
    0 internal::module-stack-index !
    zscript internal::push-stack
    zscript internal::add
    find-hook @ to saved-find-hook
    ['] do-find-with-module find-hook !
    0 to old-style-modules
    true to inited?
    true to in-zscript?
  ; is init-zscript

  \ Re-enter zeptoforth
  : enter-zforth
    inited? not in-zscript? not or if exit then
    handle-number-hook @
    saved-handle-number-hook handle-number-hook !
    to saved-handle-number-hook
    forth 1 set-order
    0 internal::module-stack-index !
    forth internal::push-stack
    forth internal::add
    find-hook @
    saved-find-hook find-hook !
    to saved-find-hook
    false to in-zscript?
  ;

  \ Re-enter zscript
  : enter-zscript
    inited? not if
      default-compile-size default-runtime-size init-zscript
    else
      in-zscript? not if
        handle-number-hook @
        saved-handle-number-hook handle-number-hook !
        to saved-handle-number-hook
        zscript 1 set-order
        0 internal::module-stack-index !
        zscript internal::push-stack
        zscript internal::add
        find-hook @
        saved-find-hook find-hook !
        to saved-find-hook
        0 to old-style-modules
        true to in-zscript?
      then
    then
  ;
    
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
  : s" ( "string" -- bytes )
    [immediate]
    state @ if
      postpone s"
      postpone 2>integral
      postpone addr-len>const-bytes
    else
      [char] " internal::parse-to-char
      2>integral
      addr-len>bytes
    then
  ;

  \ Redefer S\"
  : s\" ( "string" -- bytes )
    [immediate]
    state @ if
      postpone s\"
      postpone 2>integral
      postpone addr-len>const-bytes
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

  \ Negate an integer
  : negate ( n0 -- n1 )
    integral> negate >integral
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
    2integral> bic >integral
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
  : < ( n0 n1 -- flag )
    integral> swap integral> >
  ;

  \ Signed greater than
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : > ( n0 n1 -- flag )
    integral> swap integral> forth::<
  ;

  \ Signed less than or equal
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : <= ( n0 n1 -- flag )
    integral> swap integral> >=
  ;

  \ Signed greater than or equal
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : >= ( n0 n1 -- flag )
    integral> swap integral> forth::<=
  ;

  \ Unsigned less than
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : u< ( u0 u1 -- flag )
    integral> swap integral> u>
  ;

  \ Unsigned greater than
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : u> ( u0 u1 -- flag )
    integral> swap integral> forth::u<
  ;

  \ Unsigned less than or equal
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : u<= ( u0 u1 -- flag )
    integral> swap integral> u>=
  ;

  \ Unsigned greater than or equal
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : u>= ( u0 u1 -- flag )
    integral> swap integral> forth::u<=
  ;

  \ Equal to zero
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : 0= ( x -- flag )
    dup integral? if integral> 0= else drop false then
  ;

  \ Not equal to zero
  \ Note that this takes advantage of the fact that TRUE and FALSE do not change
  : 0<> ( x -- flag )
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

  \ Get the absolute value of a number
  : abs ( n -- u )
    integral> abs >integral
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
  : if ( x -- )
    [immediate]
    state @ if
      postpone flag>
    else
      flag>
    then
    postpone if
  ;

  \ Redefine WHILE
  : while ( x -- )
    [immediate]
    [compile-only]
    postpone flag>
    postpone while
  ;

  \ Redefine UNTIL
  : until ( x -- )
    [immediate]
    [compile-only]
    postpone flag>
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
    0 swap t@+ integral> forth::>xt xt-type allocate-cell
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
    else
      internal::syntax-naked-lambda forth::=
      averts internal::x-unexpected-syntax
      internal::drop-syntax
      internal::syntax-word internal::push-syntax
      postpone ;
      xt-type allocate-cell
    then
  ;
  
  \ Execute a closure
  : execute ( xt | closure -- )
    dup >type case
      xt-type of forth::cell+ @ execute exit endof
      closure-type of
        dup >size over forth::+ swap forth::cell+ { xt-addr }
        begin
          forth::cell forth::- dup @ swap dup xt-addr forth::=
        forth::until drop
      endof
      save-type of endof  
      ['] x-incorrect-type ?raise
    endcase
    dup integral? if integral> execute exit then
    dup >type save-type = averts x-incorrect-type
    dup [ 3 forth::cells ] literal forth::+ forth::@ integral>
    forth::handler forth::!
    dup forth::cell+ forth::@ integral>
    over [ 2 forth::cells ] literal forth::+ forth::@ integral>
    forth::rstack-base forth::@ forth::stack-base forth::@
    code[
    \ tos: forth::stack-base forth::@
    r0 1 dp ldm \ r0: forth::rstack-base forth::@
    r0 sp mov4_,4_
    r0 1 dp ldm \ r0: rstack-count
    0 dp r1 ldr_,[_,#_] \ r1: stack-count
    4 dp r2 ldr_,[_,#_] \ r2: saved state
    4 forth::cells r2 adds_,#_
    r1 r2 r2 adds_,_,_
    mark>
    0 r0 cmp_,#_
    eq bc>
    forth::cell r0 subs_,#_
    r0 r2 r3 ldr_,[_,_]
    r3 1 push
    2swap b<
    >mark
    4 dp r2 ldr_,[_,#_] \ r2: saved state
    8 dp r0 ldr_,[_,#_] \ r0: return value
    4 forth::cells r2 adds_,#_
    tos dp movs_,_
    r0 tos movs_,_
    mark>
    0 r1 cmp_,#_
    eq bc>
    forth::cell r1 subs_,#_
    r1 r2 r3 ldr_,[_,_]
    forth::cell dp subs_,#_
    0 dp r3 str_,[_,#_]
    2swap b<
    >mark
    ]code
  ;

  \ Force a thunk, if not already forced, and get its value
  : force ( xt | force -- x )
    dup >type force-type = if
      forth::cell+ forth::@
    else
      dup { thunk } execute
      [ force-type integral> 2 forth::- type-shift forth::lshift
      2 forth::cells 1 forth::lshift forth::or ] literal thunk forth::!
      dup thunk forth::cell+ forth::!
    then
  ;
  
  \ Call with the current saved state
  : save ( xt -- ? )
    allocate-save swap execute
  ;

  \ Try a closure
  : try ( xt | closure -- exception | 0 )
    [: { save }
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
      try >integral dup if save execute then
    ;] save dup if nip then
  ;

  \ Execute a non-null closure
  : ?execute ( xt | closure | 0  -- )
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
          [ 4 forth::cells >small-int ] literal ensure
          forth::cell+ >integral offset +
        endof
        const-bytes-type of
          [ 4 forth::cells >small-int ] literal ensure
          forth::cell+ @ >integral offset +
        endof
        ['] x-incorrect-type ?raise
      endcase
      len
    ;

    \ Move data from a bytes or constant bytes value into a buffer safely
    : bytes>buffer { bytes offset dest count -- }
      bytes bytes? averts x-incorrect-type
      bytes >len { len }
      offset count + len <= averts x-offset-out-of-range
      bytes >raw-offset offset + { src-offset }
      bytes >raw dup >type case
        bytes-type of
          forth::cell+ src-offset integral> forth::+ dest integral>
          count integral> forth::move
        endof
        const-bytes-type of
          forth::cell+ forth::@ src-offset integral> forth::+ dest integral>
          count integral> forth::move
        endof
        ['] x-incorrect-type ?raise
      endcase
    ;

    \ Move data from a buffer to a bytes value safely
    : buffer>bytes { src bytes offset count -- }
      bytes bytes? averts x-incorrect-type
      bytes >len { len }
      offset count + len <= averts x-offset-out-of-range
      bytes >raw-offset offset + { dest-offset }
      bytes >raw dup >type case
        bytes-type of
          src integral> forth::cell+ dest-offset integral> forth::+
          count integral> forth::move
        endof
        ['] x-incorrect-type ?raise
      endcase
    ;
    
    \ Redefine @
    : @ ( addr -- x )
      dup averts x-null-dereference
      dup 3 and triggers x-unaligned-access
      integral> @ >integral
    ;

    \ Redefine BIT@
    : bit@ ( bits addr -- flag )
      dup averts x-null-dereference
      dup 3 and triggers x-unaligned-access
      2integral> forth::bit@ >integral
    ;
  
    \ Redefine !
    : ! ( x addr -- )
      dup averts x-null-dereference
      dup 3 and triggers x-unaligned-access
      2integral> !
    ;

    \ Redefine +!
    : +! ( x addr -- )
      dup averts x-null-dereference
      dup 3 and triggers x-unaligned-access
      2integral> +!
    ;

    \ Redefine BIS!
    : bis! ( x addr -- ) 
      dup averts x-null-dereference
      dup 3 and triggers x-unaligned-access
      2integral> bis!
    ;
    
    \ Redefine BIC!
    : bic! ( x addr -- )
      dup averts x-null-dereference
      dup 3 and triggers x-unaligned-access
      2integral> bic!
    ;

    \ Redefine XOR!
    : xor! ( x addr -- )
      dup averts x-null-dereference
      dup 3 and triggers x-unaligned-access
      2integral> xor!
    ;

    \ Redefine H@
    : h@ ( addr -- h )
      dup averts x-null-dereference
      dup 1 and triggers x-unaligned-access
      integral> h@ >integral
    ;
  
    \ Redefine HBIT@
    : hbit@ ( bits addr -- flag )
      dup averts x-null-dereference
      dup 1 and triggers x-unaligned-access
      2integral> hbit@ >integral
    ;

    \ Redefine H!
    : h! ( h addr -- )
      dup averts x-null-dereference
      dup 1 and triggers x-unaligned-access
      2integral> h!
    ;
  
    \ Redefine H+!
    : h+! ( h addr -- )
      dup averts x-null-dereference
      dup 1 and triggers x-unaligned-access
      2integral> h+!
    ;

    \ Redefine HBIS!
    : hbis! ( x addr -- )
      dup averts x-null-dereference
      dup 1 and triggers x-unaligned-access
      2integral> hbis!
    ;
    
    \ Redefine HBIC!
    : hbic! ( x addr -- )
      dup averts x-null-dereference
      dup 1 and triggers x-unaligned-access
      2integral> hbic!
    ;

    \ Redefine HXOR!
    : hxor! ( x addr -- )
      dup averts x-null-dereference
      dup 1 and triggers x-unaligned-access
      2integral> hxor!
    ;

    \ Redefine C@
    : c@ ( addr -- c )
      dup averts x-null-dereference
      integral> c@ >integral
    ;

    \ Redefine CBIT@
    : cbit@ ( bits addr -- flag )
      dup averts x-null-dereference
      2integral> cbit@ >integral
    ;

    \ Redefine C!
    : c! ( c addr -- )
      dup averts x-null-dereference
      2integral> c!
    ;

    \ Redefine C+!
    : c+! ( c addr -- )
      dup averts x-null-dereference
      2integral> c+!
    ;

    \ Redefine CBIS!
    : cbis! ( c addr -- )
      dup averts x-null-dereference
      2integral> cbis!
    ;
    
    \ Redefine CBIC!
    : cbic! ( c addr -- )
      dup averts x-null-dereference
      2integral> cbic!
    ;

    \ Redefine CXOR!
    : cxor! ( c addr -- )
      dup averts x-null-dereference
      2integral> cxor!
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

    \ Cast a value to an integer
    : >integral ( x -- value ) >integral ;
    
    \ Cast a value from an integer
    : integral> ( value -- x ) integral> ;

    \ Cast two values to integers
    : 2>integral ( x0 x1 -- value0 value1 ) 2>integral ;

    \ Cast two values from integers
    : 2integral> ( value0 value1 -- x0 x1 ) 2integral> ;

    \ Cast a value to an integer
    : >double ( d -- value ) >double ;
    
    \ Cast a value from an integer
    : double> ( value -- d ) double> ;

    \ Cast two values to integers
    : 2>double ( d0 d1 -- value0 value1 ) 2>double ;

    \ Cast two values from integers
    : 2double> ( value0 value1 -- d0 xd ) 2double> ;

    \ Convert an xt to an integral
    : xt>integral ( xt -- value ) xt>integral ;

    \ Convert an integral to an xt
    : integral>xt ( value -- xt ) integral>xt ;

    \ Get the HERE pointer
    : here ( -- x ) here >integral ;

    \ ALLOT space
    : allot ( x -- ) integral> allot ;

    \ Get the RAM HERE pointer
    : ram-here ( -- x ) ram-here >integral ;

    \ Set the RAM HERE pointer
    : ram-here! ( x -- ) integral> ram-here! ;
    
    \ ALLOT RAM space
    : ram-allot ( x -- ) integral> ram-allot ;

    \ Get the flash HERE pointer
    : flash-here ( -- x ) flash-here >integral ;
    
    \ Set the flash HERE pointer
    : flash-here! ( x -- ) integral> flash-here! ;
    
    \ ALLOT flash space
    : flash-allot ( x -- ) integral> flash-allot ;

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
    postpone integral>xt
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
    over small-int? if
      unsafe::bytes>addr-len 2integral> internal::constant-with-name
    else
      over >type dup big-int-type = over double-type = or if
        drop start-compile visible lit, end-compile,
      else
        dup bytes-type = swap const-bytes-type = or if
          swap unsafe::bytes>addr-len { current len }
          len { count }
          unsafe::here { start }
          begin count while
            current unsafe::c@ integral> forth::c,
            current 1+ to current
            count 1- to count
          repeat
          forth::cell forth::align,
          start-compile visible
          start lit, len lit, postpone addr-len>const-bytes
          end-compile,
        else
          raise x-incorrect-type
        then
      then
    then
  ;

  \ Define a raw constant with a name
  : raw-constant-with-name ( x name -- )
    unsafe::bytes>addr-len 2integral> internal::constant-with-name
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

  \ Make a foreign buffer; note that this is aligned
  : foreign-buffer ( bytes "name" -- )
    token dup 0<> averts x-token-expected
    forth::cell forth::align,
    unsafe::here rot unsafe::allot
    forth::cell forth::align,
    swap constant-with-name
  ;

  \ Make a foreign constant
  : foreign-constant ( "foreign-name" "new-name" -- )
    token-word word>xt { xt }
    token dup 0<> averts x-token-expected
    xt execute >integral swap constant-with-name
  ;

  \ Make a foreign double constant
  : foreign-double-constant ( "foreign-name" "new-name" -- )
    token-word word>xt { xt }
    token dup 0<> averts x-token-expected
    xt execute >double swap constant-with-name
  ;

  \ Make a foreign variable
  : foreign-variable ( "foreign-name" "new-name" -- )
    token-word word>xt { xt }
    token dup 0<> averts x-token-expected { name }
    name >len { len }
    len 1+ make-bytes { accessor-name }
    name 0 accessor-name 0 len copy
    [char] @ len accessor-name c!+
    accessor-name start-compile visible
    xt execute >integral raw-lit,
    postpone forth::@
    postpone >integral
    end-compile,
    [char] ! len accessor-name c!+
    accessor-name start-compile visible
    postpone integral>
    xt execute >integral raw-lit,
    postpone forth::!
    end-compile,
  ;

  \ Make a foreign hook variable
  : foreign-hook-variable ( "foreign-name" "new-name" -- )
    token-word word>xt { xt }
    token dup 0<> averts x-token-expected { name }
    name >len { len }
    len 1+ make-bytes { accessor-name }
    name 0 accessor-name 0 len copy
    [char] @ len accessor-name c!+
    accessor-name start-compile visible
    xt execute >integral raw-lit,
    postpone forth::@
    postpone >integral
    postpone integral>xt
    end-compile,
    [char] ! len accessor-name c!+
    accessor-name start-compile visible
    postpone xt>integral
    postpone integral>
    xt execute >integral raw-lit,
    postpone forth::!
    end-compile,
  ;

  \ Get a word's name
  : word>name { word -- name }
    word >type tagged-type = averts x-incorrect-type
    word >tag word-tag = averts x-incorrect-type
    0 word t@+ integral> internal::word-name forth::count
    2>integral addr-len>const-bytes
  ;

  \ Get the flags for a word
  : word>flags { word -- flags }
    word >type tagged-type = averts x-incorrect-type
    word >tag word-tag = averts x-incorrect-type
    0 word t@+ integral> internal::word-flags h@ >integral
  ;
  
  \ Compile a word
  : compile, ( xt -- )
    xt>integral integral> forth::compile,
  ;

  \ Inline a word
  : inline, ( xt -- )
    xt>integral integral> internal::inline,
  ;

  \ Word flags
  forth::visible-flag >small-int constant visible-flag
  forth::immediate-flag >small-int constant immediate-flag
  forth::compiled-flag >small-int constant compiled-flag
  forth::inlined-flag >small-int constant inlined-flag
  forth::fold-flag >small-int constant fold-flag
  forth::init-value-flag >small-int constant init-value-flag
  
  \ Make a foreign word usable
  : foreign ( in-count out-count "foreign-name" "new-name" -- )
    { in-count out-count }
    token-word { word }
    token dup 0<> averts x-token-expected
    start-compile visible
    in-count 0 > if
      in-count [ 1 >small-int ] literal = if
        postpone integral>
      else
        in-count [ 2 >small-int ] literal = if
          postpone 2integral>
        else
          in-count lit,
          postpone nintegral>
        then
      then
    then
    word word>flags
    dup immediate-flag and if immediate then
    dup compiled-flag and if compile-only then
    inlined-flag fold-flag or and if
      word word>xt inline,
    else
      word word>xt compile,
    then
    out-count 0 > if
      out-count [ 1 >small-int ] literal = if
        postpone >integral
      else
        out-count [ 2 >small-int ] literal = if
          postpone 2>integral
        else
          out-count lit,
          postpone n>integral
        then
      then
    then
    internal::end-compile,
  ;

  \ Execute a foreign word
  : execute-foreign { in-count out-count xt -- }
    in-count 0 > if
      in-count [ 1 >small-int ] literal = if
        integral>
      else
        in-count [ 2 >small-int ] literal = if
          2integral>
        else
          in-count nintegral>
        then
      then
    then
    xt execute
    out-count 0 > if
      out-count [ 1 >small-int ] literal = if
        >integral
      else
        out-count [ 2 >small-int ] literal = if
          2>integral
        else
          out-count n>integral
        then
      then
    then
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
      cell word-tag allocate-tagged { tagged-word }
      word 0 tagged-word t!+
      tagged-word
    then
  ;

  \ Find a word in all dictionaries
  : find-all-dict { seq word -- word | 0 }
    word >type tagged-type = averts x-incorrect-type
    word >tag word-tag = averts x-incorrect-type
    seq unsafe::bytes>addr-len 2integral>
    0 word t@+ integral>
    find-all-dict dup forth::if
      >integral { word }
      cell word-tag allocate-tagged { tagged-word }
      word 0 tagged-word t!+
      tagged-word
    then
  ;

  \ Flash latest word
  : flash-latest ( -- word )
    flash-latest >integral { word }
    cell word-tag allocate-tagged { tagged-word }
    word 0 tagged-word t!+
    tagged-word
  ;

  \ RAM latest word
  : ram-latest ( -- word )
    ram-latest >integral { word }
    cell word-tag allocate-tagged { tagged-word }
    word 0 tagged-word t!+
    tagged-word
  ;

  \ Latest word
  : latest ( -- word )
    latest >integral { word }
    cell word-tag allocate-tagged { tagged-word }
    word 0 tagged-word t!+
    tagged-word
  ;

  \ Get the compilation state
  : state? state forth::@ >integral ;
  
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
      over >type dup big-int-type = over double-type = or if
        drop start-compile visible lit, end-compile,
      else
        dup bytes-type = swap const-bytes-type = or if
          swap unsafe::bytes>addr-len { current len }
          len { count }
          unsafe::here { start }
          begin count while
            current unsafe::c@ integral> forth::c,
            current 1+ to current
            count 1- to count
          repeat
          forth::cell forth::align,
          start-compile visible
          start lit, len lit, postpone addr-len>const-bytes
          end-compile,
        else
          raise x-incorrect-type
        then
      then
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
      ram-globals-array@ 0 new-globals 0 index copy
      new-globals ram-globals-array!
      len 1+ bytes-type allocate-bytes { accessor-name }
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
      index 1+ cells-type allocate-cells { new-globals }
      flash-globals-array@ 0 new-globals 0 index copy
      new-globals flash-globals-array!
      len 1+ bytes-type allocate-bytes { accessor-name }
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
    dup >type case
      xt-type of
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
      endof
      closure-type of
        { new-arg-count old-closure }
        old-closure >size >integral
        [ 2 >small-int ] literal rshift [ 1 >small-int ] literal -
        new-arg-count + { full-count }
        full-count closure-type allocate-cells { new-closure }
        new-closure forth::cell+ { new-current }
        begin
          old-closure forth::cell+ { old-current }
          old-closure dup >size forth::+ { old-end }
          begin old-current old-end forth::< while
            old-current @ new-current !
            old-current forth::cell+ to old-current
            new-current forth::cell+ to new-current
          repeat
        end
        new-closure dup >size forth::+ { new-end }
        begin new-current new-end forth::< while
          new-current !
          new-current forth::cell+ to new-current
        repeat
        new-closure
      endof
      save-type of
        swap { arg-count }
        arg-count 1+ closure-type allocate-cells { closure }
        closure forth::cell+
        swap over ! forth::cell+
        begin arg-count 0> while
          tuck ! forth::cell+
          arg-count 1- to arg-count
        repeat
        drop
        closure
      endof
      raise x-incorrect-type
    endcase
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

  \ Parse an integral
  : parse-integral ( seq -- n success )
    unsafe::bytes>addr-len 2integral> parse-integer 2>integral
  ;

  \ Parse an unsigned integral
  : parse-integral-unsigned ( seq -- u success )
    unsafe::bytes>addr-len 2integral> parse-unsigned 2>integral
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
  : iter { seq xt -- } \ xt ( item -- )
    seq cells? if
      seq >len 0 ?do i seq @+ xt execute loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ xt execute loop
    then
  ;

  \ Iterate over a cell sequence with an index
  : iteri { seq xt -- } \ xt ( item index -- )
    seq cells? if
      seq >len 0 ?do i seq @+ i xt execute loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ i xt execute loop
    then
  ;
  
  \ Get the index of an element that meets a predicate; note that the lowest
  \ matching index is returned, and xt will not necessarily be called against
  \ all items
  : find-index { seq xt -- index found? } \ xt ( item -- flag )
    seq cells? if
      seq >len 0 ?do i seq @+ xt execute if i true unloop exit then loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ xt execute if i true unloop exit then loop
    then
    0 false
  ;

  \ Get the index of an element that meets a predicate with an index; note that
  \ the lowest matching index is returned, and xt will not necessarily be
  \ called against all items
  : find-indexi { seq xt -- index found? } \ xt ( item index -- flag )
    seq cells? if
      seq >len 0 ?do i seq @+ i xt execute if i true unloop exit then loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ i xt execute if i true unloop exit then loop
    then
    0 false
  ;

  \ Map a cell or byte sequence into a new cell sequence
  : map { seq xt -- seq' } \ xt ( item -- item' )
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

  \ Map a cell or byte sequence into a new cell sequence with an index
  : mapi { seq xt -- seq' } \ xt ( item index -- item' )
    seq cells? if
      seq >len { len }
      len make-cells { seq' }
      len 0 ?do i seq @+ i xt execute i seq' !+ loop
      seq'
    else
      seq bytes? averts x-incorrect-type
      seq >len { len }
      len make-bytes { seq' }
      len 0 ?do i seq c@+ i xt execute i seq' c!+ loop
      seq'
    then
  ;

  \ Map a cell or byte sequence in place
  : map! { seq xt -- } \ xt ( item -- item' )
    seq cells? if
      seq >len 0 ?do i seq @+ xt execute i seq !+ loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ xt execute i seq c!+ loop
    then      
  ;

  \ Map a cell or byte sequence in place with an index
  : mapi! { seq xt -- } \ xt ( item index -- item' )
    seq cells? if
      seq >len 0 ?do i seq @+ i xt execute i seq !+ loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ i xt execute i seq c!+ loop
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
  : filter { seq xt -- seq' } \ xt ( item -- filtered? )
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
  
  \ Filter a cell or byte sequence with an index
  : filteri { seq xt -- seq' } \ xt ( item index -- filtered? )
    seq cells? seq bytes? or averts x-incorrect-type
    seq >len { len }
    len make-bits { bits }
    0 { count }
    len 0 ?do
      i seq @+ i xt execute if
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
  : foldl ( x ) { seq xt -- x' } \ xt ( x item -- x' )
    seq cells? if
      seq >len 0 ?do i seq @+ xt execute loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ xt execute loop
    then
  ;

  \ Fold left over a cell or byte sequence with an index
  : foldli ( x ) { seq xt -- x' } \ xt ( x item index -- x' )
    seq cells? if
      seq >len 0 ?do i seq @+ i xt execute loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do i seq c@+ i xt execute loop
    then
  ;

  \ Fold right over a cell or byte sequence
  : foldr ( x ) { seq xt -- x' } \ xt ( item x -- x' )
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
  
  \ Fold right over a cell or byte sequence with an index
  : foldri ( x ) { seq xt -- x' } \ xt ( item x index -- x' )
    seq cells? if
      seq >len dup 0> if
        0 swap 1- ?do i seq @+ swap i xt execute -1 +loop
      else
        drop
      then
    else
      seq bytes? averts x-incorrect-type
      seq >len dup 0> if
        0 swap 1- ?do i seq c@+ swap i xt execute -1 +loop
      else
        drop
      then
    then
  ;

  \ Collect elements of a cell sequence from left to right
  : collectl-cells { x len xt -- cells } \ xt ( x -- x item )
    len make-cells { seq }
    len 0 ?do x xt execute i seq !+ to x loop
    seq
  ;

  \ Collect elements of a cell sequence from left to right with an index
  : collectli-cells { x len xt -- cells } \ xt ( x index -- x item )
    len make-cells { seq }
    len 0 ?do x i xt execute i seq !+ to x loop
    seq
  ;

  \ Collect elements of a cell sequence from right to left
  : collectr-cells { x len xt -- cells } \ xt ( x -- x item )
    len make-cells { seq }
    len 0 ?do x xt execute len i - 1- seq !+ to x loop
    seq
  ;

  \ Collect elements of a cell sequence from right to left with an index
  : collectri-cells { x len xt -- cells } \ xt ( x -- x item )
    len make-cells { seq }
    len 0 ?do len i - 1- { index } x index xt execute index seq !+ to x loop   
    seq
  ;

  \ Collect elements of a byte sequence from left to right
  : collectl-bytes { x len xt -- bytes } \ xt ( x -- x item )
    len make-bytes { seq }
    len 0 ?do x xt execute i seq c!+ to x loop
    seq
  ;

  \ Collect elements of a byte sequence from left to right with an index
  : collectli-bytes { x len xt -- bytes } \ xt ( x index -- x item )
    len make-bytes { seq }
    len 0 ?do x i xt execute i seq c!+ to x loop
    seq
  ;

  \ Collect elements of a byte sequence from right to left
  : collectr-bytes { x len xt -- bytes } \ xt ( x -- x item )
    len make-bytes { seq }
    len 0 ?do x xt execute len i - 1- seq c!+ to x loop
    seq
  ;

  \ Collect elements of a byte sequence from right to left with an index
  : collectri-bytes { x len xt -- bytes } \ xt ( x -- x item )
    len make-bytes { seq }
    len 0 ?do len i - 1- { index } x index xt execute index seq c!+ to x loop
    seq
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

  \ Zip two sequences into a new sequence, using the length of the shorter
  \ sequence
  : zip { seq0 seq1 -- seq2 }
    seq0 cells? seq0 bytes? or averts x-incorrect-type
    seq1 cells? seq1 bytes? or averts x-incorrect-type
    seq0 >len seq1 >len min { len }
    len make-cells { seq2 }
    len 0 ?do
      i seq0 x@+ i seq1 x@+ [ 2 >small-int ] literal >cells i seq2 !+
    loop
    seq2
  ;

  \ Zip three sequences into a new sequence, using the length of the shorter
  \ sequence
  : zip3 { seq0 seq1 seq2 -- seq3 }
    seq0 cells? seq0 bytes? or averts x-incorrect-type
    seq1 cells? seq1 bytes? or averts x-incorrect-type
    seq2 cells? seq2 bytes? or averts x-incorrect-type
    seq0 >len seq1 >len min seq2 >len min { len }
    len make-cells { seq3 }
    len 0 ?do
      i seq0 x@+ i seq1 x@+ i seq2 x@+
      [ 3 >small-int ] literal >cells i seq3 !+
    loop
    seq3
  ;

  \ Zip two sequences into the first sequence in-place; note that if the ranges
  \ do not match an exception is raised, and the first sequence must be a cell
  \ sequence
  : zip! { seq0 seq1 -- }
    seq0 cells? averts x-incorrect-type
    seq1 cells? seq1 bytes? or averts x-incorrect-type
    seq0 >len { len }
    len seq1 >len = averts x-offset-out-of-range
    len 0 ?do
      i seq0 x@+ i seq1 x@+ [ 2 >small-int ] literal >cells i seq0 !+
    loop
  ;
  
  \ Zip three sequences into the first sequence in-place; note that if the
  \ ranges do not match an exception is raised, and the first sequence must be
  \ a cell sequence
  : zip3! { seq0 seq1 seq2 -- }
    seq0 cells? averts x-incorrect-type
    seq1 cells? seq1 bytes? or averts x-incorrect-type
    seq2 cells? seq2 bytes? or averts x-incorrect-type
    seq0 >len { len }
    len seq1 >len = averts x-offset-out-of-range
    len seq2 >len = averts x-offset-out-of-range
    len 0 ?do
      i seq0 x@+ i seq1 x@+ i seq2 x@+
      [ 3 >small-int ] literal >cells i seq0 !+
    loop
  ;

  \ ---------------------------------------------------------------------------
  \ Note that below the heapsort was chosen because it functions in constant
  \ space while providing adequate performance.
  \ ---------------------------------------------------------------------------
  
  continue-module zscript-internal

    \ Left child
    : left-child ( index -- index' )
      [ 1 >small-int ] literal lshift 1+
    ;

    \ Heapsort a cell sequence
    : sort-cells! { seq xt -- }
      seq >len { len }
      len [ 1 >small-int ] literal rshift { start }
      len { end }
      begin end [ 1 >small-int ] literal > while
        start 0> if
          start 1- to start
        else
          end 1- to end
          0 seq @+ end seq @+ 0 seq !+ end seq !+
        then
        start { root }
        true { continue }
        begin root left-child end < continue and while
          root left-child { child }
          child 1+ end < if
            child seq @+ child 1+ seq @+ xt execute if
              child 1+ to child
            then
          then
          root seq @+ child seq @+ xt execute if
            root seq @+ child seq @+ root seq !+ child seq !+
            child to root
          else
            false to continue
          then
        repeat
      repeat
    ;

    \ Heapsort a byte sequence
    : sort-bytes! { seq xt -- }
      seq >len { len }
      len [ 1 >small-int ] literal rshift { start }
      len { end }
      begin end [ 1 >small-int ] literal > while
        start 0> if
          start 1- to start
        else
          end 1- to end
          0 seq c@+ end seq c@+ 0 seq c!+ end seq c!+
        then
        start { root }
        true { continue }
        begin root left-child end < continue and while
          root left-child { child }
          child 1+ end < if
            child seq c@+ child 1+ seq c@+ xt execute if
              child 1+ to child
            then
          then
          root seq c@+ child seq c@+ xt execute if
            root seq c@+ child seq c@+ root seq c!+ child seq c!+
            child to root
          else
            false to continue
          then
        repeat
      repeat
    ;

  end-module

  \ Heapsort a cell or byte sequence in place
  : sort! { seq xt -- }
    seq cells? if
      seq xt sort-cells!
    else
      seq bytes? averts x-incorrect-type
      seq xt sort-bytes!
    then
  ;

  \ Heapsort a cell or byte sequence, copying it
  : sort ( seq xt -- )
    swap duplicate tuck swap sort!
  ;

  \ Get whether a predicate applies to all elements of a sequence; note that
  \ not all elements will be iterated over if an element returns false, and
  \ true will be returned if the sequence is empty
  : all { seq xt -- all? } \ xt ( element -- match? )
    seq cells? if
      seq >len 0 ?do
        i seq @+ xt execute not if false exit then
      loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do
        i seq c@+ xt execute not if false exit then
      loop
    then
    true
  ;
  
  \ Get whether a predicate applies to all elements of a sequence; note that
  \ not all elements will be iterated over if an element returns false, and
  \ true will be returned if the sequence is empty
  : alli { seq xt -- all? } \ xt ( element index -- match? )
    seq cells? if
      seq >len 0 ?do
        i seq @+ i xt execute not if false exit then
      loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do
        i seq c@+ i xt execute not if false exit then
      loop
    then
    true
  ;

  \ Get whether a predicate applies to any element of a sequence; note that
  \ not all elements will be iterated over if an element returns true, and
  \ false will be returned if the sequence is empty
  : any { seq xt -- any? } \ xt ( element -- match? )
    seq cells? if
      seq >len 0 ?do
        i seq @+ xt execute if true exit then
      loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do
        i seq c@+ xt execute if true exit then
      loop
    then
    false
  ;

  \ Get whether a predicate applies to any element of a sequence; note that
  \ not all elements will be iterated over if an element returns true, and
  \ false will be returned if the sequence is empty
  : anyi { seq xt -- any? } \ xt ( element index -- match? )
    seq cells? if
      seq >len 0 ?do
        i seq @+ i xt execute if true exit then
      loop
    else
      seq bytes? averts x-incorrect-type
      seq >len 0 ?do
        i seq c@+ i xt execute if true exit then
      loop
    then
    false
  ;

  \ Join a cell sequence of cell or byte sequences
  : join { list-seq join-seq -- seq' }
    list-seq cells? averts x-incorrect-type
    list-seq [: cells? ;] all if
      join-seq cells? averts x-incorrect-type
      list-seq >len { list-seq-len }
      list-seq-len 0= if 0 make-cells exit then
      0 list-seq [: >len + ;] foldl { elements-len }
      join-seq >len { join-seq-len }
      elements-len list-seq-len 1- 0 max join-seq-len * + { total-len }
      total-len make-cells { seq' }
      0 { dest-index }
      list-seq-len 1- 0 max 0 ?do
        i list-seq @+ { current-seq }
        current-seq >len { current-len }
        current-seq 0 seq' dest-index current-len copy
        dest-index current-len + to dest-index
        join-seq 0 seq' dest-index join-seq-len copy
        dest-index join-seq-len + to dest-index
      loop
      list-seq-len 1- list-seq @+ { current-seq }
      current-seq >len { current-len }
      current-seq 0 seq' dest-index current-len copy
      seq'
    else
      list-seq [: bytes? ;] all averts x-incorrect-type
      join-seq bytes? averts x-incorrect-type
      list-seq >len { list-seq-len }
      list-seq-len 0= if 0 make-bytes exit then
      0 list-seq [: >len + ;] foldl { elements-len }
      join-seq >len { join-seq-len }
      elements-len list-seq-len 1- 0 max join-seq-len * + { total-len }
      total-len make-bytes { seq' }
      0 { dest-index }
      list-seq-len 1- 0 max 0 ?do
        i list-seq @+ { current-seq }
        current-seq >len { current-len }
        current-seq 0 seq' dest-index current-len copy
        dest-index current-len + to dest-index
        join-seq 0 seq' dest-index join-seq-len copy
        dest-index join-seq-len + to dest-index
      loop
      list-seq-len 1- list-seq @+ { current-seq }
      current-seq >len { current-len }
      current-seq 0 seq' dest-index current-len copy
      seq'
    then
  ;

  \ Get the current depth
  : depth ( -- depth )
    depth >integral
  ;

  continue-module zscript-internal

    \ Start sequence definition
    : begin-seq-define { define-type -- }
      depth define-type seq-define-index ram-global@
      >triple seq-define-index ram-global!
    ;

    \ End sequence definition
    : end-seq-define { define-type -- count }
      depth seq-define-index ram-global@
      dup 0<> averts x-no-seq-being-defined
      triple> seq-define-index ram-global!
      define-type = averts x-wrong-seq-type
      - 0 max
    ;
    
  end-module

  \ Start defining a cell sequence
  : #( ( -- ) define-cells begin-seq-define ;

  \ Start defining a byte sequence
  : #< ( -- ) define-bytes begin-seq-define ;

  \ Finish definining a cell sequence
  : )# ( xn ... x0 -- cells ) define-cells end-seq-define >cells ;

  \ Finish defining a byte sequence
  : ># ( cn ... c0 -- bytes ) define-bytes end-seq-define >bytes ;

  continue-module zscript-internal
    
    \ Our 32-bit FNV-1 prime
    $01000193 forth::constant FNV-prime
    
    \ Our 32-bit FNV-1 offset basis
    $811C9DC5 forth::constant FNV-offset-basis

  end-module

  \ Comparse two strings
  : equal-bytes? { bytes0 bytes1 -- flag }
    bytes0 >raw { raw0 }
    bytes1 >raw { raw1 }
    raw0 bytes? averts x-incorrect-type
    raw1 bytes? averts x-incorrect-type
    bytes0 >len { len }
    bytes1 >len len = if
      bytes0 >raw-offset { offset0 }
      bytes1 >raw-offset { offset1 }
      raw0 >type { type0 }
      raw1 >type { type1 }
      type0 const-bytes-type = if
        raw0 forth::cell+ @
      else
        raw0 forth::cell+
      then offset0 integral> forth::+ { addr0 }
      type1 const-bytes-type = if
        raw1 forth::cell+ @
      else
        raw1 forth::cell+
      then offset1 integral> forth::+ { addr1 }
      addr0 len integral> addr1 over forth::equal-strings? >integral
    else
      false
    then
  ;

  \ Comparse two strings case-insensitively
  : equal-case-bytes? { bytes0 bytes1 -- flag }
    bytes0 >raw { raw0 }
    bytes1 >raw { raw1 }
    raw0 bytes? averts x-incorrect-type
    raw1 bytes? averts x-incorrect-type
    bytes0 >len { len }
    bytes1 >len len = if
      bytes0 >raw-offset { offset0 }
      bytes1 >raw-offset { offset1 }
      raw0 >type { type0 }
      raw1 >type { type1 }
      type0 const-bytes-type = if
        raw0 forth::cell+ @
      else
        raw0 forth::cell+
      then offset0 integral> forth::+ { addr0 }
      type1 const-bytes-type = if
        raw1 forth::cell+ @
      else
        raw1 forth::cell+
      then offset1 integral> forth::+ { addr1 }
      addr0 len integral> addr1 over forth::equal-case-strings? >integral
    else
      false
    then
  ;

  \ A 32-bit FNV-1 hash function for byte sequences
  : hash-bytes ( bytes -- hash )
    unsafe::bytes>addr-len unsafe::2integral>
    FNV-prime
    FNV-offset-basis
    code[
    r0 1 dp ldm \ r0: prime
    r1 1 dp ldm \ r1: bytes
    r2 1 dp ldm \ r2: c-addr
    mark<
    0 r1 cmp_,#_
    eq bc> 2swap
    r0 tos muls_,_
    0 r2 r3 ldrb_,[_,#_]
    r3 tos eors_,_
    1 r2 adds_,#_
    1 r1 subs_,#_
    b<
    >mark
    ]code
    unsafe::>integral
  ;

  \ Format an signed integral
  : format-integral { n -- bytes }
    [ 33 >small-int ] literal make-bytes { bytes }
    bytes unsafe::bytes>addr-len drop zscript-internal::integral>
    n integral> forth::format-integer nip
    zscript-internal::>integral
    0 swap bytes >slice duplicate
  ;

  \ Format an signed integral
  : format-integral-unsigned { u -- bytes }
    [ 32 >small-int ] literal make-bytes { bytes }
    bytes unsafe::bytes>addr-len drop zscript-internal::integral>
    u integral> forth::format-unsigned nip
    zscript-internal::>integral
    0 swap bytes >slice duplicate
  ;

  \ Begin a module definition
  : begin-module ( "name" -- )
    forth::token dup forth::0<> forth::averts forth::x-token-expected
    2dup forth::find forth::?dup forth::if
    forth::['] forth::x-already-defined forth::?raise
    else
      forth::wordlist
      dup >r make-new-style -rot internal::constant-with-name r>
    then
    dup internal::push-stack
    internal::add
  ;
  
  \ Continue an existing module definition
  : continue-module ( "name" -- )
    forth::token dup forth::0<> forth::averts forth::x-token-expected
    2dup forth::find forth::?dup forth::if
      nip nip forth::>xt forth::execute dup new-style? if
        filter-new-style-flag
      then
    else
      forth::['] forth::x-not-found forth::?raise
    then
    dup internal::push-stack
    internal::add
  ;
  
  \ Start a private module definition
  : private-module [inlined] forth::private-module ;

  \ End a module definition
  : end-module [inlined] forth::end-module ;
  
  \ End a module definition and place the module on the stack
  : end-module> ( -- module )
    forth::end-module> make-new-style
  ;

  \ Import a module
  : import ( module -- )
    dup new-style? if filter-new-style-flag then internal::add
  ;
  
  \ Una module import
  : unimport ( module -- )
    dup new-style? if filter-new-style-flag then internal::remove
  ;
  
  \ Immediate
  : immediate [inlined] forth::immediate ;
  : [immediate]
    [inlined] [immediate] [compile-only] postpone forth::[immediate]
  ;
  
  \ Compile-only
  : compile-only [inlined] forth::compile-only ;
  : [compile-only]
    [inlined] [immediate] [compile-only] postpone forth::[compile-only]
  ;

  \ Inlined
  : inlined [inlined] forth::inlined ;
  : [inlined]
    [inlined] [immediate] [compile-only] postpone forth::[inlined]
  ;

  \ Visible
  : visible [inlined] forth::visible ;

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
  forth::here
  cells-type integral> 2 forth::-
  type-shift forth::lshift
  forth::cell 1 forth::lshift forth::or ,
  forth::constant 0cells

  \ Empty bytes
  \
  \ This is not garbage collected so does not need to be in the heap.
  forth::here
  bytes-type integral> 2 forth::-
  type-shift forth::lshift
  forth::cell 1 forth::lshift forth::or ,
  forth::constant 0bytes

  \ Start a constant byte sequence
  : begin-const-bytes ( "name" -- start-addr name )
    token dup 0<> averts x-token-expected
    forth::cell forth::align,
    unsafe::here swap
    syntax-const-bytes internal::push-syntax
  ;

  \ End a constant byte sequence
  : end-const-bytes ( start-addr name -- )
    syntax-const-bytes internal::verify-syntax internal::drop-syntax
    unsafe::here cell align { const-bytes }
    swap integral> forth::here
    forth::cell forth::align,
    [ const-bytes-type integral> 2 forth::- type-shift forth::lshift
    3 forth::cells 1 forth::lshift forth::or ] forth::literal forth::,
    swap dup forth::, forth::- forth::,
    const-bytes integral> swap
    unsafe::bytes>addr-len 2integral> internal::constant-with-name
  ;

  continue-module unsafe

    \ Write a cell to the dictionary
    : , ( x -- )
      unsafe::here [ 3 >small-int ] forth::literal
      and triggers x-unaligned-access
      integral> forth::,
    ;

    \ Write a halfword to the dictionary
    : h, ( h -- )
      unsafe::here [ 1 >small-int ] forth::literal
      and triggers x-unaligned-access
      integral> forth::h,
    ;

    \ Write a byte to the dictionary
    : c, ( c -- )
      integral> forth::c,
    ;

    \ Write a cell to the RAM dictionary
    : ram, ( x -- )
      unsafe::ram-here [ 3 >small-int ] forth::literal
      and triggers x-unaligned-access
      integral> forth::ram,
    ;

    \ Write a halfword to the RAM dictionary
    : hram, ( h -- )
      unsafe::ram-here [ 1 >small-int ] forth::literal
      and triggers x-unaligned-access
      integral> forth::hram,
    ;

    \ Write a byte to the RAM dictionary
    : cram, ( c -- )
      integral> forth::cram,
    ;

    \ Write a cell to the flash dictionary
    : flash, ( x -- )
      unsafe::flash-here [ 3 >small-int ] forth::literal
      and triggers x-unaligned-access
      integral> forth::flash,
    ;

    \ Write a halfword to the flash dictionary
    : hflash, ( h -- )
      unsafe::flash-here [ 1 >small-int ] forth::literal
      and triggers x-unaligned-access
      integral> forth::hflash,
    ;

    \ Write a byte to the flash dictionary
    : cflash, ( c -- )
      integral> forth::cflash,
    ;

    \ Write a cell at an arbitrary address to the dictionary
    : current! ( x addr -- )
      dup [ 3 >small-int ] forth::literal and triggers x-unaligned-access
      2integral> forth::current!
    ;

    \ Write a halfword at an arbitrary address to the dictionary
    : hcurrent! ( h addr -- )
      dup [ 1 >small-int ] forth::literal and triggers x-unaligned-access
      2integral> forth::hcurrent!
    ;

    \ Write a byte at an arbitrary address to the dictionary
    : ccurrent! ( c addr -- )
      2integral> forth::ccurrent!
    ;

    \ Align space in a constant byte sequence
    : align, ( size -- )
      integral> forth::align,
    ;

    \ Reserve a cell
    : reserve ( -- addr )
      forth::reserve >integral
    ;

    \ Reserve a halfword
    : hreserve ( -- addr )
      forth::hreserve >integral
    ;

    \ Reserve a byte
    : creserve ( -- addr )
      forth::creserve >integral
    ;
    
  end-module

  \ Write a cell to a constant byte sequence
  : , ( x -- )
    syntax-const-bytes internal::verify-syntax
    unsafe::here [ 3 >small-int ] forth::literal and triggers x-unaligned-access
    integral> forth::,
  ;

  \ Write a halfword to a constant byte sequence
  : h, ( h -- )
    syntax-const-bytes internal::verify-syntax
    unsafe::here [ 1 >small-int ] forth::literal and triggers x-unaligned-access
    integral> forth::h,
  ;

  \ Write a byte to a constant byte sequence
  : c, ( c -- )
    syntax-const-bytes internal::verify-syntax
    integral> forth::c,
  ;

  \ Align space in a constant byte sequence
  : align, ( size -- )
    syntax-const-bytes internal::verify-syntax
    integral> forth::align,
  ;
  
  \ Create a symbol
  : symbol ( "name" -- )
    forth::here
    symbol-type integral> 2 forth::-
    type-shift forth::lshift
    2 forth::cells 1 forth::lshift forth::or forth::,
    forth::reserve
    swap forth::constant
    forth::latest swap forth::current!
  ;

  \ Get the name of a symbol
  : symbol>name ( symbol -- bytes )
    dup >type symbol-type = averts x-incorrect-type
    forth::cell+ forth::@ internal::word-name forth::count
    2>integral addr-len>const-bytes
  ;

  \ Convert a symbol to an integral
  : symbol>integral ( symbol -- integral )
    dup >type symbol-type = averts x-incorrect-type
    >integral
  ;

  \ Convert a double to an integral pair
  : double>2integral ( d -- x0 x1 ) double> 2>integral ;

  \ Convert an integral pair to a double
  : 2integral>double ( x0 x1 -- double ) 2integral> >double ;
  
  \ Redefine ?DUP
  : ?dup dup if dup else then ;

  \ Trigger the garbage collector
  : gc gc ;

  \ Print out the amount of space available (execute GC first)
  : heap-free ( -- space ) to-space-top@ to-space-current@ forth::- >integral ;

  \ Check whether a word is defined
  : defined? ( 'name" -- defined? )
    token dup 0<> averts x-token-expected find 0<>
  ;
  
  true >small-int constant true
  false >small-int constant false
  type-shift >small-int constant type-shift
  : [: [immediate] postpone [: ;
  : \ [immediate] postpone \ ;
  : ( [immediate] postpone ( ;
  : { [immediate] postpone { ;
  : to [immediate] postpone to ;
  : exit [immediate] [compile-only] postpone exit ;
  : else [immediate] [compile-only] postpone forth::else ;
  : then [immediate] [compile-only] postpone forth::then ;
  : begin [immediate] postpone forth::begin ;
  : repeat [immediate] [compile-only] postpone forth::repeat ;
  : again [immediate] [compile-only] postpone forth::again ;
  : end [immediate] [compile-only] postpone forth::end ;
  : endof [immediate] [compile-only] postpone forth::endof ;
  : endcase [immediate] [compile-only] postpone forth::endcase ;
  : goto [immediate] [compile-only] postpone forth::goto ;
  : drop [inlined] forth::drop ;
  : dup [inlined] forth::dup ;
  : swap [inlined] forth::swap ;
  : over [inlined] forth::over ;
  : rot [inlined] forth::rot ;
  : -rot [inlined] forth::-rot ;
  : nip [inlined] forth::nip ;
  : tuck [inlined] forth::tuck ;
  : 2drop [inlined] forth::2drop ;
  : 2swap [inlined] forth::2swap ;
  : 2over [inlined] forth::2over ;
  : 2dup [inlined] forth::2dup ;
  : 2nip [inlined] forth::2nip ;
  : 2tuck [inlined] forth::2tuck ;
  : bit integral> forth::bit >integral ;
  : emit integral> forth::emit ;
  : emit? forth::emit? >integral ;
  : space forth::space ;
  : cr forth::cr ;
  : space forth::space ;
  : spaces integral> forth::spaces ;
  : .s forth::.s ;
  : key forth::key integral> ;
  : key? forth::key? integral> ;
  : enable-int forth::enable-int ;
  : disable-int forth::disable-int ;
  : sleep forth::sleep ;
  : [ [immediate] postpone forth::[ ;
  : ] forth::] ;
  : compile-to-ram forth::compile-to-ram ;
  : compile-to-flash forth::compile-to-flash ;
  : compiling-to-flash? forth::compiling-to-flash? ;
  : cornerstone forth::cornerstone ;
  : marker forth::marker ;
  : postpone [immediate] [compile-only] postpone forth::postpone ;
  : literal [immediate] [compile-only] forth::postpone forth::literal ;
  : recurse [immediate] [compile-only] forth::postpone forth::recurse ;
  : reboot forth::reboot ;
  : pause forth::pause ;
  : quit forth::quit ;
  : systick-counter forth::systick::systick-counter >integral ;
  : unused forth::unused ;
  : see forth::see ;
  : disassemble 2integral> forth::disassemble ;
  : words forth::words ;
  : lookup forth::lookup ;
  : words-in
    dup new-style? if filter-new-style-flag then forth::words-in
  ;
  : lookup-in
    dup new-style? if filter-new-style-flag then forth::lookup-in
  ;
  : i [immediate] [compile-only] postpone i ;
  : j [immediate] [compile-only] postpone j ;
  : : : ;
  : ; [immediate] [compile-only] postpone ; ;
  : ." [immediate] postpone ." ;
  : .\" [immediate] postpone .\" ;
  : .( [immediate] postpone .( ;
  : .\( [immediate] postpone .\( ;
  : x-token-expected x-token-expected ;
  : initializer initializer ;
  : [if] [immediate] integral> postpone [if] ;
  : [else] [immediate] postpone [else] ;
  : [then] [immediate] postpone [then] ;
  : defer
    defer
  ;
  : is xt>integral integral>
    is
  ;
    
  \ Forth module
  forth make-new-style constant forth

  \ Internal module
  internal make-new-style constant internal

  \ Zeptoscript module
  zscript make-new-style constant zscript
  
end-module
