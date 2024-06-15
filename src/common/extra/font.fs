\ Copyright (c) 2022-2024 Travis Bemann
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

begin-module zscript-font

  zscript-oo import
  zscript-bitmap import
  
  \ Out of range character exception
  : x-out-of-range-char ( -- ) ." out of range character" cr ;

  \ Set a row in a character
  method char-row! ( xn ... x0 row c font -- )

  \ Get the column of a character
  method find-char-col ( c self -- col )
    
  \ Get a pixel of font
  method raw-pixel@ ( pixel-col pixel-row font -- pixel? )

  \ Draw a character onto a bitmap
  method draw-char ( c col row op bitmap font -- )
  
  \ Draw a string onto a bitmap
  method draw-string ( bytes col row op bitmap font -- )

  \ Get the size of a character
  method char-dim@ ( c font -- cols rows )

  \ Get the size of a string
  method string-dim@ ( bytes self -- cols rows )

  begin-class font

    \ The backing bitmap
    member: font-bitmap

    \ Number of columns per character
    member: char-cols

    \ Number of rows per character
    member: char-rows

    \ Minimum character index
    member: min-char-index

    \ Maximum character index
    member: max-char-index

    \ THe default character index for un-included characters
    member: default-char-index
    
    \ Our constructor
    :method new { default cols rows min-char max-char self -- }
      min-char self min-char-index!
      max-char self max-char-index!
      rows self char-rows!
      cols self char-cols!
      default self default-char-index!
      cols max-char min-char - 1+ * rows make-bitmap self font-bitmap!
      self font-bitmap@ clear-bitmap
    ;

    \ Get the column of a character
    :method find-char-col { c self -- col }
      c self min-char-index@ - self char-cols@ *
    ;

    \ Get a pixel of a font
    :method raw-pixel@ { pixel-col pixel-row self -- pixel? }
      pixel-col pixel-row self font-bitmap@ unsafe-pixel@
    ;
    
    \ Set a row in a character
    :method char-row! ( xn ... x0 row c font -- )
      { row c self }
      c self min-char-index@ u>= averts x-out-of-range-char
      c self max-char-index@ u<= averts x-out-of-range-char
      self char-cols@ 1- { col }
      begin col 0>= while
        dup 1 and if $FF else $00 then
        c self find-char-col col + row op-set self font-bitmap@ draw-pixel-const
        1 rshift
        self char-cols@ col - 32 umod 0= col 0<> and if drop then
        -1 +to col
      repeat
      drop
    ;
    
    \ Draw a character onto a bitmap
    :method draw-char { c col row op bitmap self -- }
      c self min-char-index@ u< if self default-char-index@ to c then
      c self max-char-index@ u> if self default-char-index@ to c then
      c self find-char-col 0 col row self char-cols@ self char-rows@
      op self font-bitmap@ bitmap draw-rect
    ;
    
    \ Draw a string onto a bitmap
    :method draw-string { bytes col row op bitmap self -- }
      bytes col row op bitmap self 5 [: { c index col row op bitmap self }
        c col index self char-cols@ * + row op bitmap self draw-char
      ;] bind iteri
    ;

    \ Get the size of a character
    :method char-dim@ { c self -- cols rows }
      self char-cols@ self char-rows@
    ;

    \ Get the size of a string
    :method string-dim@ { bytes self -- cols rows }
      bytes >len self char-cols@ * self char-rows@
    ;
    
  end-class
  
end-module
