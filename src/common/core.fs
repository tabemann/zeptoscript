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
      
      \ The bottom of the first semi-space
      field: first-space-bottom
      
      \ The top of the first semi-space
      field: first-space-top
      
      \ The current second semi-space address
      field: second-space-current
      
      \ The bottom of the second semi-space
      field: second-space-bottom
      
      \ The top of the second semi-space
      field: second-space-top

    end-structure

    \ Get the RAM globals array
    : ram-globals-array@ zscript-state @ ram-globals-array @ ;

    \ Get the new RAM globals array
    : new-ram-globals-array@ zscript-state @ new-ram-globals-array @ ;

    \ Get the flash globals array
    : flash-globals-array@ zscript-state @ flash-globals-array @ ;

    \ Get the bottom of the first semi-space
    : first-space-bottom@ zscript-state @ first-space-bottom @ ;
    
    \ Get the top of the first semi-space
    : first-space-top@ zscript-state @ first-space-top @ ;
    
    \ Get the current second semi-space address
    : second-space-current@ zscript-state @ second-space-current @ ;
    
    \ Get the bottom of the second semi-space
    : second-space-bottom@ zscript-state @ second-space-bottom @ ;
    
    \ Get the top of the second semi-space
    : second-space-top@ zscript-state @ second-space-top @ ;

    \ Set the RAM globals array
    : ram-globals-array! zscript-state @ ram-globals-array ! ;

    \ Set the new RAM globals array
    : new-ram-globals-array! zscript-state @ new-ram-globals-array ! ;

    \ Set the flash globals array
    : flash-globals-array! zscript-state @ flash-globals-array ! ;
    
    \ Set the bottom of the first semi-space
    : first-space-bottom! zscript-state @ first-space-bottom ! ;
    
    \ Set the top of the first semi-space
    : first-space-top! zscript-state @ first-space-top ! ;
    
    \ Set the current second semi-space address
    : second-space-current! zscript-state @ second-space-current ! ;
    
    \ Set the bottom of the second semi-space
    : second-space-bottom! zscript-state @ second-space-bottom ! ;
    
    \ Set the top of the second semi-space
    : second-space-top! zscript-state @ second-space-top ! ;

    \ Types
    0 constant null-type
    1 constant int-type
    2 constant bytes-type
    3 constant word-type
    4 constant 2word-type
    5 constant const-bytes-type
    6 constant xt-type
    7 constant tagged-type
    10 constant cells-type
    11 constant closure-type

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
      ]code
    ;

    \ Get whether a value is null, an integer, or a word
    : integral? ( value -- integral? )
      code[
      1 r0 movs_,#_
      r0 tos tst_,_
      eq bc>
      31 r0 tos lsls_,_,#_
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
      word-type 2 - tos cmp_,#_
      31 r0 tos lsls_,_,#_
      31 tos tos asrs_,_,#_
      pc 1 pop
      ne bc>
      >mark
      0 tos movs_,#_
      ]code
    ;

    \ Get whether a value is an xt or closure
    : xt? ( value -- xt? )
      >type dup xt-type = swap closure-type = or
    ;

    \ Validate whether a value is an integer
    : validate-int ( value -- ) 1 and 0<> averts x-incorrect-type ;

    \ Validate whether a value is a word
    : validate-word ( value -- ) >type word-type = averts x-incorrect-type ;
    
    \ Get an allocation's size
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
    
    \ Relocate a block of memory
    : relocate { orig -- new }
      orig addr? if
        orig first-space-bottom@ >= orig first-space-top@ < and if
          orig @ { header }
          dup 1 and 0= if
            header size-mask and 1 rshift { size }
            second-space-current@ { current }
            second-space-top@ current size + - <= averts x-out-of-memory
            orig current size move
            current 1 or orig !
            current size cell align + second-space-current!
            current
          else
            1 bic
          then
        else
          orig
        then
      else
        orig
      then
    ;

    \ Relocate the stack
    : relocate-stack ( -- )
      sp@ stack-base @ swap ?do i @ relocate i ! loop
      rp@ rstack-base @ swap ?do i @ relocate i ! loop
    ;

    \ Swap spaces
    : swap-spaces ( -- )
      second-space-bottom@
      second-space-top@
      first-space-bottom@
      first-space-top@
      second-space-top!
      dup second-space-current!
      second-space-bottom!
      first-space-top!
      first-space-bottom!
    ;

    \ Carry out a GC cycle
    : gc ( -- )
      swap-spaces
      relocate-stack
      ram-globals-array@ relocate ram-globals-array!
      new-ram-globals-array@ relocate new-ram-globals-array!
      flash-globals-array@ relocate flash-globals-array!
      second-space-bottom@ { gc-current }
      begin gc-current second-space-current@ < while
        gc-current @ { header }
        header type-shift rshift { type }
        header size-mask and 1 rshift cell align { aligned-size }
        type has-values and if
          gc-current aligned-size + { gc-current-end }
          gc-current cell+ begin dup gc-current-end < while
            dup @ relocate over ! cell+
          repeat
          to gc-current
        else
          aligned-size +to gc-current
        then
      repeat
    ;
    
    \ Allocate memory as cells
    : allocate-cells { count type -- addr }
      count 1+ cells { bytes }
      second-space-current@ { current }
      bytes current + second-space-top@ > if
        gc
        second-space-current@ to current
        bytes current + second-space-top@ <= averts x-out-of-memory
      then
      bytes current + second-space-current!
      current
      dup cell+ bytes cell - 0 fill
      bytes 1 lshift type 2 - type-shift lshift or over !
    ;

    \ Allocate memory as bytes
    : allocate-bytes { count -- addr }
      count cell+ cell align { bytes }
      second-space-current@ { current }
      bytes current + second-space-top@ > if
        gc
        second-space-current@ to current
        bytes current + second-space-top@ <= averts x-out-of-memory
      then
      bytes current + second-space-current!
      current
      dup cell+ bytes cell - 0 fill
      count cell+ 1 lshift
      [ bytes-type 2 - type-shift lshift ] literal or over !
    ;
    
    \ Allocate a cell
    : allocate-cell { type -- addr }
      second-space-current@ { current }
      current [ 2 cells ] literal + second-space-top@ > if
        gc
        second-space-current@ to current
        current [ 2 cells ] literal + second-space-top@ <=
        averts x-out-of-memory
      then
      current [ 2 cells ] literal + second-space-current!
      current
      dup cell+ 0 !
      type 2 - type-shift lshift
      [ 2 cells 1 lshift ] literal or over !
    ;

    \ Allocate a double-word
    : allocate-2cell { type -- addr }
      second-space-current@ { current }
      current [ 3 cells ] literal + second-space-top@ > if
        gc
        second-space-current@ to current
        current [ 3 cells ] literal + second-space-top@
        <= averts x-out-of-memory
      then
      current [ 3 cells ] literal + second-space-current!
      current
      0 over cell+ !
      0 over [ 2 cells ] literal + !
      type 2 - type-shift lshift
      [ 3 cells 1 lshift ] literal or over !
    ;

    \ Allocate a tagged value
    : allocate-tagged { count tag -- addr }
      count [ 2 cells ] literal + cell align { bytes }
      second-space-current@ { current }
      bytes current + second-space-top@ > if
        gc
        second-space-current@ to current
        bytes current + second-space-top@ <= averts x-out-of-memory
      then
      bytes current + second-space-current!
      current
      tag dup cell+ !
      dup [ 2 cells ] literal + bytes [ 2 cells ] literal - 0 fill
      count cell+ 1 lshift
      [ tagged-type 2 - type-shift lshift ] literal or over !
    ;

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
      word-type 2 - r0 cmp_,#_
      ne bc>
      cell tos tos ldr_,[_,#_]
      pc 1 pop
      >mark
      ]code
      ['] x-incorrect-type ?raise
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
      word-type allocate-cell tuck cell+ !
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

    \ Convert a pair of cells to nulls, integers, and words
    : 2>integral ( x0 x1 -- 0|int|addr 0|int|addr )
      >integral swap >integral swap
    ;

    \ Convert a pair of nulls, integers, or words to cells
    : 2integral> ( 0|int|addr 0|int|addr -- x0 x1 )
      integral> swap integral> swap
    ;

    \ Cast cells to xts
    : >xt ( x -- xt )
      xt-type allocate-cell { xt-value }
      xt-value cell+ !
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
      normalize dup integral? if
        >type word-type <>
      else
        drop false
      then
    ;
    
    \ Get the tag of a tagged value
    : >tag ( value -- tag )
      dup >type tagged-type = averts x-incorrect-type
      cell+ @
    ;
    
    \ Get a value in a tagged data structure
    : t@+ ( index object -- value )
      dup >type tagged-type = averts x-incorrect-type
      swap integral> 1+ cells
      over >size over u< averts x-offset-out-of-range
      + @
    ;

    \ Set a value in a tagged data structure
    : t!+ ( value index object -- )
      dup >type tagged-type = averts x-incorrect-type
      swap integral> 1+ cells
      over >size over u< averts x-offset-out-of-range
      + !
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

  \ Get the raw LIT,
  : raw-lit, ( x -- ) integral> lit, ;
  
  \ Redefine LIT,
  : lit, ( xt -- )
    dup small-int? if lit, else integral> lit, postpone >integral then
  ;

  \ Get the length of cells in entries or bytes in bytes
  : >len ( cells | bytes -- len )
    dup >type case
      cells-type of
        >size cell - 2 rshift
      endof
      bytes-type of
        >size cell -
      endof
      const-bytes-type of
        [ 2 cells ] literal + @
      endof
      ['] x-incorrect-type ?raise
    endcase
    >integral
  ;

  \ Convert an address/length pair into constant bytes
  : >const-bytes ( c-addr u -- const-bytes )
    const-bytes-type allocate-2cell { const-bytes }
    integral> const-bytes [ 2 cells ] literal + !
    integral> const-bytes cell+ !
    const-bytes
  ;

  \ Convert an address/length pair into bytes
  : >bytes ( c-addr u -- bytes )
    swap integral> swap
    integral> dup allocate-bytes { bytes }
    bytes cell+ swap move
    bytes
  ;

  \ Convert two values into a pair
  : >pair ( x0 x1 -- pair )
    2 cells-type allocate-cells { pair }
    pair [ 2 cells ] literal + !
    pair cell+ !
    pair
  ;

  \ Convert a pair into two values
  : pair> ( pair -- x0 x1 )
    dup >type cells-type = averts x-incorrect-type
    dup >size [ 3 cells ] literal = averts x-incorrect-size
    dup cell+ @
    swap [ 2 cells ] literal + @
  ;

  \ Convert three values into a triple
  : >triple ( x0 x1 x2 -- triple )
    3 cells-type allocate-cells { triple }
    triple [ 3 cells ] literal + !
    triple [ 2 cells ] literal + !
    triple cell+ !
    triple
  ;

  \ Convert a triple into three values
  : triple> ( pair -- x0 x1 x2 )
    dup >type cells-type = averts x-incorrect-type
    dup >size [ 4 cells ] literal = averts x-incorrect-size
    dup cell+ @
    over [ 2 cells ] literal + @
    rot [ 3 cells ] literal + @
  ;

  \ Create a tuple
  : >tuple ( xn ... x0 count -- tuple )
    integral> dup cells-type allocate-cells { tuple }
    tuple over 1+ cells + swap 0 ?do cell - tuck ! loop drop
    tuple
  ;

  \ Explode a tuple
  : tuple> ( tuple -- xn ... x0 count )
    dup >type cells-type = averts x-incorrect-type
    dup >size cell - 2 rshift { count }
    count 0 ?do cell + dup @ swap loop drop
    count >integral
  ;

  \ Explode a tuple without pushing its count
  : tuple-no-count> ( tuple -- xn ... x0 )
    dup >type cells-type = averts x-incorrect-type
    dup >size cell - 2 rshift { count }
    count 0 ?do cell + dup @ swap loop drop
  ;
  
  \ Convert a sequence into a sequence triple
  : seq>triple ( seq -- triple )
    0 over >len >triple
  ;

  \ Get a value in a cells data structure
  : v@+ ( index object -- value )
    dup >type cells-type = averts x-incorrect-type
    swap integral> 1+ cells
    over >size over u>= averts x-offset-out-of-range
    + @
  ;

  \ Set a value in a cells data structure
  : v!+ ( value index object -- )
    dup >type cells-type = averts x-incorrect-type
    swap integral> 1+ cells
    over >size over u>= averts x-offset-out-of-range
    + !
  ;

  \ Get a word in a bytes data structure
  : @+ ( index object -- byte )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over u>= averts x-offset-out-of-range
        dup 3 and triggers x-unaligned-dereference
        + @ >integral
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        over len u>= averts x-offset-out-of-range
        over 3 and triggers x-unaligned-dereference
        cell+ @ + @ >integral
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Set a word in a bytes data structure
  : !+ ( byte index object -- )
    dup >type bytes-type = averts x-incorrect-type
    swap integral> cell+
    over >size over u>= averts x-offset-out-of-range
    dup 3 and triggers x-unaligned-dereference
    + swap integral> swap !
  ;

  \ Get a halfword in a bytes data structure
  : h@+ ( index object -- byte )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over u>= averts x-offset-out-of-range
        dup 1 and triggers x-unaligned-dereference
        + h@ 1 lshift 1 or
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        over len u>= averts x-offset-out-of-range
        over 1 and triggers x-unaligned-dereference
        cell+ @ + h@ 1 lshift 1 or
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Set a halfword in a bytes data structure
  : h!+ ( byte index object -- )
    dup >type bytes-type = averts x-incorrect-type
    swap integral> cell+
    over >size over u>= averts x-offset-out-of-range
    dup 1 and triggers x-unaligned-dereference
    + swap integral> swap h!
  ;

  \ Get a byte in a bytes data structure
  : c@+ ( index object -- byte )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over u>= averts x-offset-out-of-range
        + c@ 1 lshift 1 or
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        over len u>= averts x-offset-out-of-range
        cell+ @ + c@ 1 lshift 1 or
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Set a byte in a bytes data structure
  : c!+ ( byte index object -- )
    dup >type bytes-type = averts x-incorrect-type
    swap integral> cell+
    over >size over u>= averts x-offset-out-of-range
    + swap integral> swap c!
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
    heap first-space-bottom!
    heap size 1 rshift +
    dup first-space-top! dup second-space-current! second-space-bottom!
    heap size + second-space-top!
    0 ram-globals-array!
    0 new-ram-globals-array!
    0 flash-globals-array!
    init-ram-global-count integral>
    cells-type allocate-cells { ram-globals }
    init-ram-global-id current-ram-global-id-index ram-globals v!+
    ram-globals ram-globals-array!
    ram-globals new-ram-globals-array!
    get-current-flash-global-id integral>
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
    value0 >type { type0 }
    type0 value1 >type = averts x-incorrect-type
    count 0>= averts x-offset-out-of-range
    value0 >size cell - offset0 count + >= averts x-offset-out-of-range
    value1 >size cell - offset1 count + >= averts x-offset-out-of-range
    type0 2 - has-values and if
      count cells to count
      offset0 cells to offset0
      offset1 cells to offset1
    then
    value0 cell+ offset0 + value1 cell+ offset1 + count move
  ;
  
  \ Redefine S"
  : s" ( "string" -- triple )
    [immediate]
    state @ if
      postpone s"
      postpone 2>integral
      postpone >const-bytes
      postpone seq>triple
    else
      [char] " internal::parse-to-char
      2>integral
      >bytes
      seq>triple
    then
  ;

  \ Redefer S\"
  : s\" ( "string" -- triple )
    [immediate]
    state @ if
      postpone s\"
      postpone 2>integral
      postpone >const-bytes
      postpone seq>triple
    else
      [:
        here dup [char] " esc-string::parse-esc-string
        here over -
        2>integral
        >bytes
        seq>triple
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
  : = { x0 x1 -- flag }
    x0 integral? x1 integral? forth::and if
      x0 integral> x1 integral> =
    else
      x0 x1 =
    then
  ;

  \ Get whether two values are unequal
  : <> { x0 x1 -- flag }
    x0 integral? x1 integral? forth::and if
      x0 integral> x1 integral> <>
    else
      x0 x1 <>
    then
  ;

  \ Signed less than
  : < ( x0 x1 -- flag )
    integral> swap integral> >
  ;

  \ Signed greater than
  : > ( x0 x1 -- flag )
    integral> swap integral> forth::<
  ;

  \ Signed less than or equal
  : <= ( x0 x1 -- flag )
    integral> swap integral> >=
  ;

  \ Signed greater than or equal
  : >= ( x0 x1 -- flag )
    integral> swap integral> forth::<=
  ;

  \ Unsigned less than
  : u< ( x0 x1 -- flag )
    integral> swap integral> u>
  ;

  \ Unsigned greater than
  : u> ( x0 x1 -- flag )
    integral> swap integral> forth::u<
  ;

  \ Unsigned less than or equal
  : u<= ( x0 x1 -- flag )
    integral> swap integral> u>=
  ;

  \ Unsigned greater than or equal
  : u>= ( x0 x1 -- flag )
    integral> swap integral> forth::u<=
  ;

  \ Equal to zero
  : 0= ( n -- flag )
    dup integral? if integral> 0= else drop false then
  ;

  \ Not equal to zero
  : 0<> ( n -- flag )
    dup integral? if integral> 0<> else drop true then
  ;

  \ Less than zero
  : 0< ( n -- flag )
    integral> 0<
  ;

  \ Greater than zero
  : 0> ( n -- flag )
    integral> 0>
  ;

  \ Less than or equal to zero
  : 0<= ( n -- flag )
    integral> 0<=
  ;

  \ Greater than or equal to zero
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

  \ Redefine DO
  : do ( end start -- )
    [immediate]
    state @ forth::if
      postpone 2integral>
    else
      2integral>
    then
    postpone do
  ;

  \ Redefine ?DO
  : ?do ( end start -- )
    [immediate]
    state @ forth::if
      postpone 2integral>
    else
      2integral>
    then
    postpone ?do
  ;

  \ Redefine +LOOP
  : +loop ( change -- )
    [immediate]
    [compile-only]
    postpone integral>
    postpone +loop
  ;

  \ Redefine I
  : i ( -- x )
    [immediate]
    [compile-only]
    postpone i
    postpone >integral
  ;

  \ Redefine J
  : j ( -- x )
    [immediate]
    [compile-only]
    postpone j
    postpone >integral
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

  \ Truncate the start of a sequence triple
  : truncate-start { count triple -- triple' }
    triple triple> { seq start len }
    count len <  if
      seq start count + len count -
    else
      seq start len + 0
    then
    >triple
  ;

  \ Truncate the end of a sequence triple
  : truncate-end { count triple -- triple' }
    triple triple> { seq start len }
    count len < if
      seq start len count -
    else
      seq start 0
    then
    >triple
  ;

  \ Get the type of a value
  : >type ( value -- type )
    >type >integral
  ;

  \ Get whether a value is integral
  : integral? ( value -- integral? )
    integral? >integral
  ;
  
  \ Get whether a value is a small integer
  : small-int? ( value -- small-int? )
    small-int? >integral
  ;
  
  \ The types, this time as values
  null-type >small-int constant null-type
  int-type >small-int constant int-type
  bytes-type >small-int constant bytes-type
  word-type >small-int constant word-type
  2word-type >small-int constant 2word-type
  const-bytes-type >small-int constant const-bytes-type
  xt-type >small-int constant xt-type
  tagged-type >small-int constant tagged-type
  cells-type >small-int constant cells-type
  closure-type >small-int constant closure-type

  \ Get whether a value is an int
  : int? ( value -- int? )
    int? >integral
  ;
  
  \ Get whether a value is integral
  : integral? ( value -- integral? )
    integral? >integral
  ;

  \ Get a token
  : token ( runtime: "name" -- seq | 0 )
    token >integral dup 0<> if >bytes else 2drop 0 then
  ;

  \ Get a word from a token
  : token-word ( runtime: "name" -- word )
    token-word { word }
    word-tag forth::cell allocate-tagged { tagged-word }
    word 0 tagged-word t!+
    tagged-word
  ;

  \ Get an execution token from a word
  : word>xt ( word -- xt )
    dup >type tagged-type = averts x-incorrect-type
    dup >tag word-tag = averts x-incorrect-type
    xt-type allocate-cell { xt-value }
    0 swap t@+ xt-value forth::cell+ !
    xt-value
  ;
  
  \ End a lambda
  : ;]
    [immediate]
    [compile-only]
    postpone ;]
    xt-type allocate-cell
    tuck
    forth::cell+ !
  ;
  
  \ Execute a closure
  : execute ( xt -- )
    dup >type case
      xt-type of forth::cell+ @ execute endof
      closure-type of
        dup >size over forth::+ swap forth::cell+ { xt-addr }
        begin
          forth::cell forth::- dup @ swap dup xt-addr forth::=
        forth::until drop
        integral> execute
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Execute a non-null closure
  : ?execute ( xt -- )
    dup integral? not if
      execute
    else
      integral> forth::0= averts x-incorrect-type
    then
  ;
  
  \ Bind a scope to a lambda
  : bind ( xn ... x0 count xt -- closure )
    dup >type xt-type = averts x-incorrect-type
    forth::cell+ @ { xt-cell } integral> { arg-count }
    arg-count forth::1+ forth::cells closure-type allocate-cells { closure }
    closure forth::cell+
    xt-cell over ! forth::cell+
    begin arg-count 0> forth::while
      tuck ! forth::cell+
      -1 forth::+to arg-count
    repeat
    drop
    closure
  ;
  
  begin-module unsafe
    
    \ Get the address and size of a bytes or constant bytes value
    : bytes> ( value -- addr len )
      dup >type case
        bytes-type of
          dup forth::cell+
          swap >len
        endof
        const-bytes-type of
          dup forth::cell+ @ >integral
          swap [ 2 forth::cells ] literal + @ >integral
        endof
        cells-type of
          triple> { bytes offset len }
          bytes >type case
            bytes-type of
              bytes forth::cell+ >integral offset +
              bytes >len offset - len min 0 max
            endof
            const-bytes-type of
              bytes forth::cell+ @ >integral offset +
              bytes >len offset - len min 0 max
            endof
            ['] x-incorrect-type ?raise
          endcase
        endof
        ['] x-incorrect-type ?raise
      endcase
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
    unsafe::bytes> 2integral> type
  ;
  
  \ Get the compilation state
  : state? state forth::@ >integral ;
  
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
    unsafe::bytes> 2integral> internal::start-compile
  ;

  \ End compiling, exported to match start-compile
  : end-compile, ( -- )
    internal::end-compile,
  ;

  \ Define a constant with a name
  : constant-with-name ( x name -- )
    swap dup small-int? if
      integral> swap unsafe::bytes> 2integral> internal::constant-with-name
    else
      swap start-compile visible lit, end-compile,
    then
  ;

  \ Redefine CHAR
  : char ( "name" -- x )
    token dup 0<> averts x-token-expected
    unsafe::bytes> drop 0 swap unsafe::c@
  ;

  \ Redefine [CHAR]
  : [char] ( "name" -- x )
    [immediate]
    [compile-only]
    token dup 0<> averts x-token-expected
    unsafe::bytes> drop 0 swap unsafe::c@ lit,
  ;

  \ Begin declaring a record
  : begin-record ( "name" -- offset )
    syntax-record internal::push-syntax
    token dup 0<> averts x-token-expected
    0
  ;

  \ Finish declaring a record
  : end-record ( offset -- )
    syntax-record internal::verify-syntax internal::drop-syntax
    { name count }
    name >len { len }
    len 1+ allocate-bytes { accessor-name }
    forth::[char] > >integral 0 accessor-name c!+
    name 0 accessor-name 1 len copy
    accessor-name start-compile visible
    count lit,
    postpone >tuple
    end-compile,
    forth::[char] > >integral len accessor-name c!+
    name 0 accessor-name 0 len copy
    accessor-name start-compile visible
    postpone tuple-no-count>
    end-compile,
    s" make-" { make-bytes make-offset make-len }
    make-len len + allocate-bytes to accessor-name
    make-bytes make-offset accessor-name 0 make-len copy
    name 0 accessor-name make-len len copy
    accessor-name start-compile visible
    count raw-lit,
    cells-type raw-lit,
    postpone allocate-cells
    end-compile,
    s" -size" triple> { size-bytes size-offset size-len }
    [ false ] [if] \ This is only needed if make-len size-len <>
      size-len len + allocate-bytes to accessor-name
    [then]
    name 0 accessor-name 0 len copy
    size-bytes size-offset accessor-name len size-len copy
    count accessor-name constant-with-name
  ;

  \ Create a field in a record
  : item: ( offset "name" -- offset' )
    { offset }
    syntax-record internal::verify-syntax
    token dup 0<> averts x-token-expected { name }
    name >len { len }
    len 1+ allocate-bytes { accessor-name }
    name 0 accessor-name 0 len copy
    forth::[char] @ >integral len 1+ accessor-name c!+
    accessor-name start-compile visible
    offset lit,
    postpone swap
    postpone v@+
    end-compile,
    forth::[char] ! >integral len 1+ accessor-name c!+
    accessor-name start-compile visible
    offset lit,
    postpone swap
    postpone v!+
    end-compile,
    offset 1+
  ;
  
  \ Make a foreign word usable
  : foreign ( in-count out-count xt "name" -- )
    { in-count out-count xt }
    token dup 0<> averts x-token-expected
    start-compile visible
    in-count 0 ?do postpone integral> loop
    xt xt>integral raw-lit, postpone forth::execute
    out-count 0 ?do postpone >integral loop
    internal::end-compile,
  ;

  \ Execute a foreign word
  : execute-foreign ( in-count out-count xt -- )
    rot 0 ?do integral> loop
    execute
    0 ?do >integral loop
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
    : parse-add-local ( c-addr u -- match? )
      2>r 0 internal::local-buf-top @ begin
        dup internal::local-buf-bottom @ forth::<
      forth::while
        dup forth::1+ forth::count 2r@ forth::equal-case-strings? forth::if
          rdrop rdrop c@ case
            internal::cell-local of compile-add-cell-local endof
            internal::cell-addr-local of compile-add-cell-local endof
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
      internal::value-addr@ raw-lit,
      postpone dup
      postpone forth::@
      postpone rot
      postpone +
      postpone swap
      postpone forth::!
    ;
    
  end-module

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
      ram-globals-array@ v@+
    ;

    \ Set a RAM global at an index
    : ram-global! ( x index -- )
      ram-globals-array@ v!+
    ;

    \ Get a flash global at an index
    : flash-global@ ( index -- x )
      flash-globals-array@ v@+
    ;

    \ Set a flash global at an index
    : flash-global! ( x index -- )
      flash-globals-array@ v@+
    ;

    \ Create a RAM global
    : ram-global ( "name" --)
      { offset }
      token dup 0<> averts x-token-expected { name }
      name >len { len }
      current-ram-global-id-index ram-global@ { index }
      index 1+ current-ram-global-id-index ram-global!
      index 1+ integral> cells-type integral> allocate-cells { new-globals }
      new-globals new-ram-globals-array!
      ram-globals-array@ 0 new-ram-globals-array@ 0 index copy
      new-ram-globals-array@ ram-globals-array!
      len 1+ allocate-bytes { accessor-name }
      name 0 accessor-name 0 len copy
      forth::[char] @ >integral len 1+ accessor-name c!+
      accessor-name start-compile visible
      index lit,
      postpone ram-global@
      end-compile,
      forth::[char] ! >integral len 1+ accessor-name c!+
      accessor-name start-compile visible
      index lit,
      postpone ram-global!
      end-compile,
    ;

    \ Create a flash global
    : flash-global ( "name" --)
      { offset }
      token dup 0<> averts x-token-expected { name }
      name >len { len }
      get-current-flash-global-id { index }
      len 1+ allocate-bytes { accessor-name }
      name 0 accessor-name 0 len copy
      forth::[char] @ >integral len 1+ accessor-name c!+
      accessor-name start-compile visible
      index lit,
      postpone flash-global@
      end-compile,
      forth::[char] ! >integral len 1+ accessor-name c!+
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
  
end-module
