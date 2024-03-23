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
    user zscript-state

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
    10 constant cells-type

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
    
    \ Value to integer
    : >int ( value -- n ) [inlined] 1 arshift ;

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

    \ Protect the stack
    : protect-stack ( -- )
      rp@ dup rstack-base @ <= swap rstack-end @ >= and if
        dp@ dup stack-base @ > swap stack-end @ < or if
          dp@ stack-end @ >= averts stack-overflow
          dp@ stack-base @ <= averts stack-underflow
        then
      else
        rp@ rstack-end @ >= averts rstack-overflow
        rp@ rstack-base @ <= averts rstack-underflow
      then
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
    : allocate-cells ( count -- addr )
      1+ cells { bytes }
      second-space-current@ { current }
      bytes current + second-space-top@ > if
        gc
        second-space-current@ to current
        bytes current + second-space-top@ <= averts x-out-of-meory
      then
      bytes current + second-space-current!
      current
      dup cell+ bytes cell - 0 fill
      [ cells-type 2 - type-shift lshift ] literal bytes 1 lshift or over !
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
    
    \ Allocate a word
    : allocate-word { -- addr }
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
      [ word-type 2 - type-shift lshift ] literal
      [ 2 cells 1 lshift ] literal or over !
    ;

    \ Allocate a double-word
    : allocate-2word { -- addr }
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
      [ 2word-type 2 - type-shift lshift ] literal
      [ 3 cells 1 lshift ] literal or over !
    ;

    \ Cast nulls, integers, and words to cells, validating them
    : cast-from-int ( 0 | int | addr -- x' )
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
    : cast-from-int ( x -- 0 | int | addr )
      dup if
        dup $3FFFFFFF u<= over $BFFFFFFF > and if
          1 lshift 1 or
        else
          allocate-word tuck cell+ !
        then
      then
    ;
    
  end-module> import
  
  \ Define words with stack protection
  : : ( "name" -- )
    :
    postpone protect-stack
  ;

  \ Define anonymous words with stack protection
  : :noname ( -- )
    :noname
    postpone protect-stack
  ;

  \ Define lambdas with stack protection
  : [: ( -- )
    [immediate]
    postpone [:
    postpone protect-stack
  ;

  \ End words with stack protection
  : ; ( -- )
    [immediate]
    postpone protect-stack
    postpone ;
  ;

  \ End lambdas with stack protection
  : ;] ( -- )
    [immediate]
    [compile-only]
    postpone protect-stack
    postpone ;]
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
      ['] x-incorrect-type ?raise
    endcase
    cast-to-int
  ;

  \ Convert an address/length pair into bytes
  : >bytes ( c-addr u -- bytes )
    dup allocate-bytes { bytes }
    bytes cell+ swap move
    bytes
  ;

  \ Convert two values into a pair
  : >pair ( x0 x1 -- pair )
    2 allocate-cells { pair }
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
    3 allocate-cells { triple }
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
      postpone >bytes
      postpone seq>triple
    else
      [char] " parse-to-char
      >bytes
      seq>triple
    then
  ;

  \ Get a value in a cells data structure
  : v@+ ( index object -- value )
    dup >type cells-type = averts x-incorrect-type
    swap cast-from-int 1+ cells
    over >size over u< x-offset-out-of-range
    + @
  ;

  \ Set a value in a cells data structure
  : v!+ ( value index object -- )
    dup >type cells-type = averts x-incorrect-type
    swap cast-from-int 1+ cells
    over >size over u< x-offset-out-of-range
    + !
  ;

  \ Get a word in a bytes data structure
  : @+ ( index object -- byte )
    dup >type cells-type = averts x-incorrect-type
    swap cast-from-int cell+
    over >size over u< x-offset-out-of-range
    dup 3 and triggers x-unaligned-dereference
    + @ cast-to-int
  ;

  \ Set a word in a bytes data structure
  : h!+ ( byte index object -- )
    dup >type cells-type = averts x-incorrect-type
    swap cast-from-int cell+
    over >size over u< x-offset-out-of-range
    dup 3 and triggers x-unaligned-dereference
    + swap cast-from-int swap !
  ;

  \ Get a halfword in a bytes data structure
  : h@+ ( index object -- byte )
    dup >type cells-type = averts x-incorrect-type
    swap cast-from-int cell+
    over >size over u< x-offset-out-of-range
    dup 1 and triggers x-unaligned-dereference
    + h@ 1 lshift 1 or
  ;

  \ Set a halfword in a bytes data structure
  : h!+ ( byte index object -- )
    dup >type cells-type = averts x-incorrect-type
    swap cast-from-int cell+
    over >size over u< x-offset-out-of-range
    dup 1 and triggers x-unaligned-dereference
    + swap cast-from-int swap h!
  ;

  \ Get a byte in a bytes data structure
  : c@+ ( index object -- byte )
    dup >type cells-type = averts x-incorrect-type
    swap cast-from-int cell+
    over >size over u< x-offset-out-of-range
    + c@ 1 lshift 1 or
  ;

  \ Set a byte in a bytes data structure
  : c!+ ( byte index object -- )
    dup >type cells-type = averts x-incorrect-type
    swap cast-from-int cell+
    over >size over u< x-offset-out-of-range
    + swap cast-from-int swap c!
  ;
  
  \ Add two integers
  : + ( x0 x1 -- x2 )
    cast-from-int
    swap cast-from-int + cast-to-int
  ;

  \ Add one to an integer
  : 1+ ( x0 -- x1 )
    cast-from-int 1+ cast-to-int
  ;

  \ Add a cell to an integer
  : cell+ ( x0 -- x1 )
    cast-from-int cell+ cast-to-int
  ;

  \ Subtract two integers
  : - ( x0 x1 -- x2 )
    swap cast-from-int
    swap cast-from-int - cast-to-int
  ;

  \ Subtract one from an integer
  : 1- ( x0 -- x1 )
    cast-from-int 1- cast-to-int
  ;

  \ Multiply two integers
  : * ( x0 x1 -- x2 )
    cast-from-int
    swap cast-from-int * cast-to-int
  ;

  \ Multiply an integer by two
  : 2* ( x0 -- x1 )
    cast-from-int 1 lshift cast-to-int
  ;

  \ Multiple an integer by four
  : 4* ( x0 -- x1 )
    cast-from-int 2 lshift cast-to-int
  ;

  \ Multiple an integer by a cell
  : cells ( x0 -- x1 )
    cast-from-int 2 lshift cast-to-int
  ;

  \ Divide two signed integers
  : / ( n0 n1 -- n2 )
    swap cast-from-int
    swap cast-from-int / cast-to-int
  ;

  \ Divide a signed integer by two
  : 2/ ( n0 -- n1 )
    cast-from-int 1 arshift cast-to-int
  ;

  \ Divide a signed integer by four
  : 4/ ( n0 -- n1 )
    cast-from-int 2 arshift cast-to-int
  ;
  
  \ Divide two unsigned integers
  : u/ ( u0 u1 -- u2 )
    swap cast-from-int
    swap cast-from-int u/ cast-to-int
  ;

  \ Get the modulus of two signed integers
  : mod ( n0 n1 -- n2 )
    swap cast-from-int
    swap cast-from-int mod cast-to-int
  ;

  \ Get the modulus of two unsigned integers
  : umod ( u0 u1 -- u2 )
    swap cast-from-int
    swap cast-from-int umod cast-to-int
  ;

  \ Or two integers
  : or ( x0 x1 -- x2 )
    cast-from-int
    swap cast-from-int or cast-to-int
  ;

  \ And two integers
  : and ( x0 x1 -- x2 )
    cast-from-int
    swap cast-from-int and cast-to-int
  ;

  \ Exclusive-or two integers
  : xor ( x0 x1 -- x2 )
    cast-from-int
    swap cast-from-int xor cast-to-int
  ;

  \ Clear bits in an integer
  : bic ( x0 x1 -- x2 )
    cast-from-int
    swap cast-from-int bic cast-to-int
  ;    

  \ Not an integer
  : not ( x0 -- x1 )
    cast-from-int not cast-to-int
  ;

  \ Invert an integer
  : invert ( x0 -- x1 )
    cast-from-int forth::not cast-to-int
  ;

  \ Left shift an integer
  : lshift ( x0 x1 -- x2 )
    swap cast-from-int
    swap cast-from-int lshift cast-to-int
  ;

  \ Logical right shift an integer
  : rshift ( x0 x1 -- x2 )
    swap cast-from-int
    swap cast-from-int rshift cast-to-int
  ;

  \ Arithmetic right shift an integer
  : arshift ( x0 x1 -- x2 )
    swap cast-from-int
    swap cast-from-int arshift cast-to-int
  ;

  \ Get whether two values are equal
  : = { x0 x1 -- flag }
    x0 integral? x1 integral? forth::and if
      x0 cast-from-int x1 cast-from-int =
    else
      x0 x1 =
    then
  ;

  \ Get whether two values are unequal
  : <> { x0 x1 -- flag }
    x0 integral? x1 integral? forth::and if
      x0 cast-from-int x1 cast-from-int <>
    else
      x0 x1 <>
    then
  ;

  \ Signed less than
  : < ( x0 x1 -- flag )
    cast-from-int swap cast-from-int >
  ;

  \ Signed greater than
  : > ( x0 x1 -- flag )
    cast-from-int swap cast-from-int forth::<
  ;

  \ Signed less than or equal
  : <= ( x0 x1 -- flag )
    cast-from-int swap cast-from-int >=
  ;

  \ Signed greater than or equal
  : >= ( x0 x1 -- flag )
    cast-from-int swap cast-from-int forth::<=
  ;

  \ Unsigned less than
  : u< ( x0 x1 -- flag )
    cast-from-int swap cast-from-int u>
  ;

  \ Unsigned greater than
  : u> ( x0 x1 -- flag )
    cast-from-int swap cast-from-int forth::u<
  ;

  \ Unsigned less than or equal
  : u<= ( x0 x1 -- flag )
    cast-from-int swap cast-from-int u>=
  ;

  \ Unsigned greater than or equal
  : u>= ( x0 x1 -- flag )
    cast-from-int swap cast-from-int forth::u<=
  ;

  \ Equal to zero
  : 0= ( n -- flag )
    dup integral? if cast-from-int 0= else drop false then
  ;

  \ Not equal to zero
  : 0<> ( n -- flag )
    dup integral? if cast-from-int 0<> else drop true then
  ;

  \ Less than zero
  : 0< ( n -- flag )
    cast-from-int 0<
  ;

  \ Greater than zero
  : 0> ( n -- flag )
    cast-from-int 0>
  ;

  \ Less than or equal to zero
  : 0<= ( n -- flag )
    cast-from-int 0<=
  ;

  \ Greater than or equal to zero
  : 0>= ( n -- flag )
    cast-from-int 0>=
  ;

  \ False constant
  1 constant false

  \ Redefine IF
  : if ( flag -- )
    [immediate]
    state @ if
      postpone cast-from-int
    else
      cast-from-int
    then
    postpone if
  ;

  \ Redefine WHILE
  : while ( flag -- )
    [immediate]
    [compile-only]
    postpone cast-from-int
    postpone while
  ;

  \ Redefine UNTIL
  : until ( flag -- )
    [immediate]
    [compile-only]
    postpone cast-from-int
    postpone while
  ;

  \ Redefine DO
  : do ( end start -- )
    [immediate]
    state @ forth::if
      postpone cast-from-int postpone swap postpone cast-from-int postpone swap
    else
      cast-from-int swap cast-from-int swap
    then
    postpone do
  ;

  \ Redefine ?DO
  : ?do ( end start -- )
    [immediate]
    state @ forth::if
      postpone cast-from-int postpone swap postpone cast-from-int postpone swap
    else
      cast-from-int swap cast-from-int swap
    then
    postpone ?do
  ;

  \ Redefine +LOOP
  : +loop ( change -- )
    [immediate]
    [compile-only]
    postpone cast-from-int
    postpone +loop
  ;

  \ Redefine .
  : . ( n -- )
    cast-from-int .
  ;

  \ Redefine U.
  : u. ( u -- )
    cast-from-int u.
  ;

  \ Redefine (.)
  : (.) ( n -- )
    cast-from-int (.)
  ;

  \ Redefine (U.)
  : (u.) ( n -- )
    cast-from-int (u.)
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

  \ Get whether a value is an int
  : int? ( value -- int? )
    int? cast-to-int
  ;
  
  \ Get whether a value is integral
  : integral? ( value -- integral? )
    integral? cast-to-int
  ;
  
  begin-module unsafe
  
    \ Redefine @
    : @ ( addr -- x )
      cast-from-int
      dup averts x-null-dereference
      dup 3 forth::and triggers x-unaligned-deference
      @
    ;
  
    \ Redefine !
    : ! ( x addr -- )
      cast-from-int
      dup averts x-null-dereference
      dup 3 forth::and triggers x-unaligned-deference
      swap cast-from-int swap !
    ;
  
    \ Redefine H@
    : h@ ( addr -- h )
      cast-from-int
      dup averts x-null-dereference
      dup 1 forth::and triggers x-unaligned-deference
      h@
    ;
  
    \ Redefine H!
    : h! ( h addr -- )
      cast-from-int
      dup averts x-null-dereference
      dup 1 forth::and triggers x-unaligned-deference
      swap cast-from-int swap h!
    ;
  
    \ Redefine C@
    : c@ ( addr -- h )
      cast-from-int
      dup averts x-null-dereference
      c@
    ;
  
    \ Redefine C!
    : c! ( h addr -- )
      cast-from-int
      dup averts x-null-dereference
      swap cast-from-int swap c!
    ;

    \ Redefine FILL
    : fill { addr bytes val -- }
      addr cast-from-int to addr
      bytes cast-from-int to bytes
      val cast-from-int to bytes
      addr bytes val fill
    ;
    
  end-module

  \ Unsafe operations raising exceptions outside of UNSAFE module
  : @ ['] x-unsafe-op ?raise ;
  : ! ['] x-unsafe-op ?raise ;
  : h@ ['] x-unsafe-op ?raise ;
  : h! ['] x-unsafe-op ?raise ;
  : c@ ['] x-unsafe-op ?raise ;
  : c! ['] x-unsafe-op ?raise ;
  : fill ['] x-unsafe-op ?raise ;
  
end-module
