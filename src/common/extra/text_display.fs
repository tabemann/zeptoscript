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

begin-module zscript-text-display

  zscript-oo import
  zscript-bitmap import
  zscript-font import

  \ Clear the display
  method clear-display ( self -- )
  
  \ Set the entire display as dirty
  method set-dirty ( self -- )

  \ Set a character as dirty
  method dirty-char ( col row self -- )

  \ Get whether a display is dirty
  method dirty? ( self -- dirty? )

  \ Get the display's dirty rectangle in pixels
  method dirty-rect@ ( self -- start-col start-row end-col end-row )

  \ Clear the entire display's dirty state
  method clear-dirty ( self -- )
  
  \ Get the display dimensions in characters
  method dim@ ( self -- cols rows )

  \ Set a character
  method char! ( c col row self -- )

  \ Get a character
  method char@ ( col row self -- c )

  \ Set a string
  method string! ( c-addr u col row self -- )

  \ Set inverted video
  method invert! ( invert? col row self -- )

  \ Toggle inverted video
  method toggle-invert! ( col row self -- )
  
  \ Get inverted video
  method invert@ ( col row self -- invert? )

  \ Get the state of a pixel - note that this does no validation
  method pixel@ ( pixel-col pixel-row self -- pixel-set? )

  \ Text display class
  begin-class text-display

    \ Text display buffer
    member: text-display-text-buf

    \ Text display inverted video bitmap buffer
    member: text-display-invert-buf
    
    \ Text display font
    member: text-display-font

    \ Text display character columns
    member: text-display-char-cols

    \ Text display character rows
    member: text-display-char-rows
    
    \ Text display columns
    member: text-display-cols

    \ Text display rows
    member: text-display-rows

    \ Text display dirty start column
    member: text-display-dirty-start-col

    \ Text display dirty start row
    member: text-display-dirty-start-row

    \ Text display dirty end column
    member: text-display-dirty-end-col

    \ Text display dirty end row
    member: text-display-dirty-end-row

    \ Constructor
    :method new { font cols rows self -- }
      font self text-display-font!
      $20 font char-dim@ { char-cols char-rows }
      char-cols self text-display-char-cols!
      char-rows self text-display-char-rows!
      cols char-cols / self text-display-cols!
      rows char-rows / self text-display-rows!
      self text-display-cols@ self text-display-cols@ * make-bytes
      self text-display-text-buf!
      self text-display-cols@ self text-display-cols@ 8 align 8 / * make-bytes
      self text-display-invert-buf!
      self clear-display
    ;

    \ Clear the display
    :method clear-display { self -- }
      self text-display-rows@ 0 ?do
        self text-display-cols@ 0 ?do
          $20 i j self char!
          false i j self invert!
        loop
      loop
      self set-dirty
    ;
    
    \ Set the entire display as dirty
    :method set-dirty { self -- }
      0 self text-display-dirty-start-col!
      0 self text-display-dirty-start-row!
      self text-display-cols@ self text-display-dirty-end-col!
      self text-display-rows@ self text-display-dirty-end-row!
    ;

    \ Set a character as dirty
    :method dirty-char { col row self -- }
      self dirty? if
        self text-display-dirty-start-col@ col min
        self text-display-dirty-start-col!
        self text-display-dirty-start-row@ row min
        self text-display-dirty-start-row!
        self text-display-dirty-end-col@ col 1+ max
        self text-display-dirty-end-col!
        self text-display-dirty-end-row@ row 1+ max
        self text-display-dirty-end-row!
      else
        col self text-display-dirty-start-col!
        row self text-display-dirty-start-row!
        col 1+ self text-display-dirty-end-col!
        row 1+ self text-display-dirty-end-row!
      then
    ;

    \ Get whether a display is dirty
    :method dirty? { self -- dirty? }
      self text-display-dirty-start-col@ self text-display-dirty-end-col@ <>
      self text-display-dirty-start-row@ self text-display-dirty-end-row@ <>
      or
    ;

    \ Get the display's dirty rectangle in pixels
    :method dirty-rect@ { self -- start-col start-row end-col end-row }
      self text-display-char-cols@ self text-display-char-rows@
      { char-cols char-rows }
      self text-display-dirty-start-col@ char-cols *
      self text-display-dirty-start-row@ char-rows *
      self text-display-dirty-end-col@ char-cols *
      self text-display-dirty-end-row@ char-rows *
    ;

    \ Clear the entire display's dirty state
    :method clear-dirty { self -- }
      0 self text-display-dirty-start-col!
      0 self text-display-dirty-start-row!
      0 self text-display-dirty-end-col!
      0 self text-display-dirty-end-row!
    ;
    
    \ Get the display dimensions in characters
    :method dim@ { self -- cols rows }
      self text-display-cols@ self text-display-rows@
    ;

    \ Set a character
    :method char! { c col row self -- }
      col 0< col self text-display-cols@ >= or
      row 0< or row self text-display-rows@ >= or if exit then
      c row self text-display-cols@ * col + self text-display-text-buf@ c!+
      col row self dirty-char
    ;

    \ Get a character
    :method char@ { col row self -- c }
      col 0< col self text-display-cols@ >= or
      row 0< or row self text-display-rows@ >= or if 0 exit then
      row self text-display-cols@ * col + self text-display-text-buf@ c@+
    ;

    \ Set a string
    :method string! { c-addr u col row self -- }
      u 0 ?do c-addr i + c@ col i + row self char! loop
    ;

    \ Set inverted video
    :method invert! { invert? col row self -- }
      col 0< col self text-display-cols@ >= or
      row 0< or row self text-display-rows@ >= or if exit then
      self text-display-cols@ row 3 rshift * + col + { offset }
      offset self text-display-invert-buf@ c@+
      row $7 and bit
      invert? if or else bic then
      offset self text-display-invert-buf@ c!+
      col row self dirty-char
    ;

    \ Toggle inverted video
    :method toggle-invert! { col row self -- }
      col 0< col self text-display-cols@ >= or
      row 0< or row self text-display-rows@ >= or if exit then
      self text-display-cols@ row 3 rshift * + col + { offset }
      offset self text-display-invert-buf@ c@+
      row $7 and bit
      xor
      offset self text-display-invert-buf@ c!+
      col row self dirty-char
    ;
    
    \ Get inverted video
    :method invert@ { col row self -- invert? }
      col 0< col self text-display-cols@ >= or
      row 0< or row self text-display-rows@ >= or if false exit then
      self text-display-cols@ row 3 rshift * + col + { offset }
      offset self text-display-invert-buf@ c@+
      row $7 and bit
      and 0<>
    ;

    \ Get the state of a pixel - note that this does no validation
    :method pixel@ { pixel-col pixel-row self -- pixel-set? }
      pixel-col self text-display-char-cols@ /mod { font-pixel-col char-col }
      pixel-row self text-display-char-rows@ /mod { font-pixel-row char-row }

      char-row self text-display-cols@ * char-col +
      self text-display-text-buf@ + c@+
      font-pixel-col font-pixel-row self text-display-font@ char-pixel@
      
      self text-display-cols@ char-row 3 rshift * + char-col +
      self text-display-invert-buf@ c@+
      char-row 7 and rshift 1 and 0<> xor
    ;

  end-class
  
end-module
