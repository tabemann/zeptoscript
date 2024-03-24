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
  
  begin-module zscript-internal
    
    \ The per-task zeptoscript structure
    \ This will ultimately be a user variable, but for testing purposes it will
    \ be a plain variable.
    variable zscript-state

    \ The zeptoscript structure
    begin-structure zscript-size
      
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
    $7FFFFFFF constant size-mask

    \ Set whether a value is an integer
    : int? ( value -- int? ) [inlined] 1 and 0<> ;

    \ Get whether a value is an address
    : addr? ( value -- addr? ) [inlined] 1 and 0= ;

    \ Value to address
    : >addr ( value -- addr ) [inlined] 1 bic ;
    
    \ Get an allocation's type
    : >type ( value -- type )
      dup int? if
        drop int-type
      else
        dup 0= if
          drop null-type
        else
          @ type-shift rshift 2 +
        then
      then
    ;

    \ Get whether a value is null, an integer, or a word
    : integral? ( value -- integral? )
      dup 0= if
        drop true
      else
        dup int? if
          drop true
        else
          >type word-type =
        then
      then
    ;

    \ Get whether a value is an xt or closure
    : xt? ( value -- xt? )
      >type dup xt-type = swap closure-type = or
    ;

    \ Get an xt
    : xt> ( value -- xt )
      dup >type case
        xt-type of
          cell+ @
        endof
        closure-type of
          cell+ @ integral>
        then
        ['] x-incorrect-type ?raise
      endcase
    ;

    \ Get an unbound xt
    : unbound-xt> ( value -- xt )
      dup >type xt-type = averts x-incorrect-type
      cell+ @
    ;

    \ Validate whether a value is an integer
    : validate-int ( value -- ) 1 and 0<> averts x-incorrect-type ;

    \ Validate whether a value is a word
    : validate-word ( value -- ) >type word-type = averts x-incorrect-type ;
    
    \ Get an allocation's size
    : >size ( value -- size )
      dup int? if
        drop 0
      else
        dup 0<> if
          @ size-mask and 1 rshift
        then
      then
    ;
    
    \ Relocate a block of memory
    : relocate { orig -- new }
      orig addr? if
        orig first-space-bottom@ >= orig first-space-top@ < and if
          orig @ { header }
          dup 1 and 0= if
            header size-mask and 1 rshift { size }
            second-space-current@ { current }
            second-space-top@ current size + - <= averts x-out-of-meory
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
      depth dup
      begin dup 0> while rot relocate >r 1- repeat
      drop
      begin dup 0> while r> swap 1- repeat
      drop
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
        bytes current + second-space-top@ <= averts x-out-of-meory
      then
      bytes current + second-space-current!
      current
      dup cell+ bytes cell - 0 fill
      type 2 - type-shift lshift bytes 1 lshift or over !
    ;

    \ Allocate memory as bytes
    : allocate-bytes { count -- addr }
      count cell+ cell align { bytes }
      second-space-current@ { current }
      bytes current + second-space-top@ > if
        gc
        second-space-current@ to current
        bytes current + second-space-top@ <= averts x-out-of-meory
      then
      bytes current + second-space-current!
      current
      dup cell+ bytes cell - 0 fill
      [ bytes-type 2 - type-shift lshift ] literal count 1 lshift or over !
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
        bytes current + second-space-top@ <= averts x-out-of-meory
      then
      bytes current + second-space-current!
      current
      tag dup cell+ !
      dup [ 2 cells ] literal + bytes [ 2 cells ] literal - 0 fill
      [ tagged-type 2 - type-shift lshift ] literal count 1 lshift or over !
    ;

    \ Cast nulls, integers, and words to cells, validating them
    : integral> ( 0 | int | addr -- x' )
      dup if
        dup int? if
          1 arshift
        else
          dup validate-word
          cell+ @
        then
      then
    ;

    \ Cast cells to nulls, integers, and words
    : >integral ( x -- 0 | int | addr )
      dup if
        dup $3FFFFFFF u<= over $BFFFFFFF > and if
          1 lshift 1 or
        else
          word-type allocate-cell tuck cell+ !
        then
      then
    ;

    \ Cast cells to xts
    : cast-to-xt ( x -- xt )
      xt-type allocate-cell { xt-value }
      xt-value cell+ !
      xt-value
    ;

    \ Normalize a value
    : normalize ( value -- value' )
      dup integral? if
        dup if
          dup int? if
            1 arshift
          else
            cell+ @
          then
        then
        >integral
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
    : do-handle-number { addr bytes -- [value] -1 | 0 }
      parse-integer if
        state @ if
          lit, postpone >integral
        else
          >integral
        then
        true
      else
        drop false
      then
    ;

  end-module> import
  
  \ Initialize zeptoscript
  : init-zscript { size -- }
    size [ 2 cells ] literal align to size
    cell align, here zscript-size allot zscript-state !
    here size allot { heap }
    heap first-space-bottom!
    heap size 1 lshift +
    dup first-space-top! dup second-space-current! second-space-bottom!
    heap size + second-space-top!
    ['] do-handle-number handle-number-hook !
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
    const-bytes [ 2 cells ] literal + !
    const-bytes cell+ !
    const-bytes
  ;

  \ Convert an address/length pair into bytes
  : >bytes ( c-addr u -- bytes )
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
    swap [ 2 cells ] literal + @
    swap [ 3 cells ] literal + @
  ;
  
  \ Convert a sequence into a sequence triple
  : seq>triple ( seq -- triple )
    0 over >len >triple
  ;

  \ Redefine s"
  : s" ( "string" -- triple )
    state @ if
      postpone s"
      postpone >integral
      postpone >const-bytes
      postpone seq>triple
    else
      [char] " parse-to-char
      >integral
      >bytes
      seq>triple
    then
  ;
  
  \ Get a value in a cells data structure
  : v@+ ( index object -- value )
    dup >type cells-type = averts x-incorrect-type
    swap integral> 1+ cells
    over >size over u< averts x-offset-out-of-range
    + @
  ;

  \ Set a value in a cells data structure
  : v!+ ( value index object -- )
    dup >type cells-type = averts x-incorrect-type
    swap integral> 1+ cells
    over >size over u< averts x-offset-out-of-range
    + !
  ;

  \ Get a word in a bytes data structure
  : @+ ( index object -- byte )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over u< averts x-offset-out-of-range
        dup 3 and triggers x-unaligned-dereference
        + @ >integral
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        over len u< averts x-offset-out-of-range
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
    over >size over u< averts x-offset-out-of-range
    dup 3 and triggers x-unaligned-dereference
    + swap integral> swap !
  ;

  \ Get a halfword in a bytes data structure
  : h@+ ( index object -- byte )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over u< averts x-offset-out-of-range
        dup 1 and triggers x-unaligned-dereference
        + h@ 1 lshift 1 or
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        over len u< averts x-offset-out-of-range
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
    over >size over u< averts x-offset-out-of-range
    dup 1 and triggers x-unaligned-dereference
    + swap integral> swap h!
  ;

  \ Get a byte in a bytes data structure
  : c@+ ( index object -- byte )
    dup >type case
      bytes-type of
        swap integral> cell+
        over >size over u< averts x-offset-out-of-range
        + c@ 1 lshift 1 or
      endof
      const-bytes-type of
        dup [ 2 cells ] literal + @ { len }
        over len u< averts x-offset-out-of-range
        cell+ @ + c@ 1 shift 1 or
      endof
      ['] x-incorrect-type ?raise
    endcase
  ;

  \ Set a byte in a bytes data structure
  : c!+ ( byte index object -- )
    dup >type bytes-type = averts x-incorrect-type
    swap integral> cell+
    over >size over u< averts x-offset-out-of-range
    + swap integral> swap c!
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
  
  \ False constant
  1 constant false

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
    postpone while
  ;

  \ Redefine DO
  : do ( end start -- )
    [immediate]
    state @ forth::if
      postpone integral> postpone swap postpone integral> postpone swap
    else
      integral> swap integral> swap
    then
    postpone do
  ;

  \ Redefine ?DO
  : ?do ( end start -- )
    [immediate]
    state @ forth::if
      postpone integral> postpone swap postpone integral> postpone swap
    else
      integral> swap integral> swap
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
  
  \ The types, this time as values
  0 >integral constant null-type
  1 >integral constant int-type
  2 >integral constant bytes-type
  3 >integral constant word-type
  4 >integral constant 2word-type
  5 >integral constant const-bytes-type
  10 >integral constant cells-type

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
        begin forth::cell forth::- dup @ swap dup xt-addr forth::= until drop
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
    forth::cell+ @ { arg-count xt-cell }
    arg-count 1+ cells closure-type allocate-cells { closure }
    closure forth::cell+
    xt-cell over ! forth::cell+
    begin arg-count 0> while
      tuck ! forth::cell+
      -1 +to arg-count
    then
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
          swap [ 2 cells ] literal + @ >integral
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
    
    \ Cast a value from an integer
    : integral> ( value -- x ) integral> ;

    \ Redefine @
    : @ ( addr -- x )
      integral>
      dup averts x-null-dereference
      dup 3 forth::and triggers x-unaligned-deference
      @ >integral
    ;
  
    \ Redefine !
    : ! ( x addr -- )
      integral>
      dup averts x-null-dereference
      dup 3 forth::and triggers x-unaligned-deference
      swap integral> swap !
    ;
  
    \ Redefine H@
    : h@ ( addr -- h )
      integral>
      dup averts x-null-dereference
      dup 1 forth::and triggers x-unaligned-deference
      h@ >integral
    ;
  
    \ Redefine H!
    : h! ( h addr -- )
      integral>
      dup averts x-null-dereference
      dup 1 forth::and triggers x-unaligned-deference
      swap integral> swap h!
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
    
  end-module
  
  \ Get an xt at interpretation-time
  : ' ( "name" -- xt )
    token-word word>xt
  ;

  \ Get an xt at compile-time
  : ['] ( 'name" -- xt )
    [immediate]
    [compile-time]
    token-word word>xt
    forth::cell+ @ lit,
    postpone cast-to-xt
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
    token-word
    word>xt
    state unsafe::@ if
      postpone 0=
      postpone if
      rot lit,
      postpone ?raise
      postpone then
    else
      swap 0= if
        ?raise
      else
        drop
      then
    then
  ;

  \ Assert that a value is false, otherwise raise a specified exception
  : triggers ( f "name" -- )
    [immediate]
    token-word
    word>xt
    state unsafe::@ if
      postpone 0<>
      postpone if
      rot lit,
      postpone ?raise
      postpone then
    else
      swap 0<> if
        ?raise
      else
        drop
      then
    then
  ;
  
  \ Start compiling a word with a name
  : start-compile ( seq -- )
    unsafe::bytes> integral> swap integral> swap internal::start-compile
  ;

  \ Make a foreign word usable
  : foreign ( in-count out-count xt "name" -- )
    { in-count out-count xt }
    token dup 0<> averts x-token-expected
    start-compile visible
    in-count 0 ?do postpone integral> loop
    xt unbound-xt> lit, postpone forth::execute
    out-count 0 ?do postpone >integral loop
    internal::end-compile,
  ;

  \ Execute a foreign word
  : execute-foreign ( in-count out-count xt -- )
    rot 0 ?do integral> loop
    execute
    0 ?do >integral loop
  ;

  \ Unsafe operations raising exceptions outside of UNSAFE module
  : @ ['] x-unsafe-op ?raise ;
  : ! ['] x-unsafe-op ?raise ;
  : h@ ['] x-unsafe-op ?raise ;
  : h! ['] x-unsafe-op ?raise ;
  : c@ ['] x-unsafe-op ?raise ;
  : c! ['] x-unsafe-op ?raise ;
  : move ['] x-unsafe-op ?raise ;
  : fill ['] x-unsafe-op ?raise ;
  
end-module
