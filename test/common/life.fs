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

begin-module life

  zscript-oo import
  zscript-armv6m import
  life-ssd1306 import

  \ Cycle the life world
  method cycle ( life -- )

  \ Draw the life world
  method draw ( display life -- )

  \ Clear the life world
  method clear ( life -- )
  
  \ Set the state of a cell
  method cell! ( state col row life -- )

  \ Set a row
  method row! ( cells orient col row life -- )

  \ Set a block
  method rows! ( rows orient col row life -- )

  \ Run world until a key is presed
  method run ( life -- )

  \ Run world with a delay between cycles until a key is pressed
  method run-slow ( delay life -- )

  \ Single-step the world
  method step ( life -- )

  \ Orientation

  \ Left to right, top to bottom
  symbol lrtb

  \ Left to right, bottom to top
  symbol lrbt

  \ Right to left, top to bottom
  symbol rltb

  \ Right to left, bottom to top
  symbol rlbt

  \ Invalid orientation exception
  : x-invalid-orient ( -- ) ." invalid orientation" cr  ;
  
  begin-class life

    member: life-display
    member: life-width
    member: life-height
    member: life-old-world
    member: life-new-world

    \ Constructor
    :method new { display width height self -- }
      display self life-display!
      width self life-width!
      height self life-height!
      width height * 5 rshift cells { size }
      size make-bytes self life-old-world!
      size make-bytes self life-new-world!
    ;
    
    \ Get a column portion
    :private column@ { col row self -- val }
      self life-width@ row * col + cells self life-old-world@ w@+
    ;

    \ Set a column portion
    :private column! { val col row self -- }
      val self life-width@ row * col + cells self life-new-world@ w!+
    ;

    \ Cycle the middle of a column
    :private cycle-column-middle
      { left-column current-column right-column -- new-column }
      left-column unsafe::integral>
      current-column unsafe::integral>
      right-column unsafe::integral>
      code[
      r5 r4 2 push
      r0 1 dp ldm
      r1 1 dp ldm
      1 r2 movs_,#_
      0 r3 movs_,#_
      
      mark<

      31 r2 cmp_,#_

      eq bc>

      0 r5 movs_,#_
      1 r4 movs_,#_

      r4 tos tst_,_
      eq bc>
      1 r5 adds_,#_
      >mark

      r4 r0 tst_,_
      eq bc>
      1 r5 adds_,#_
      >mark
      
      r4 r1 tst_,_
      eq bc>
      1 r5 adds_,#_
      >mark
      
      2 r4 movs_,#_
      r4 tos tst_,_
      eq bc>
      1 r5 adds_,#_
      >mark
      
      r4 r1 tst_,_
      eq bc>
      1 r5 adds_,#_
      >mark
      
      4 r4 movs_,#_
      r4 tos tst_,_
      eq bc>
      1 r5 adds_,#_
      >mark
      
      r4 r0 tst_,_
      eq bc>
      1 r5 adds_,#_
      >mark
      
      r4 r1 tst_,_
      eq bc>
      1 r5 adds_,#_
      >mark
      
      2 r4 movs_,#_
      r4 r0 tst_,_
      eq bc>

      2 r5 cmp_,#_
      lt bc>
      3 r5 cmp_,#_
      gt bc>
      1 r4 movs_,#_
      r2 r4 lsls_,_
      r4 r3 orrs_,_
      >mark
      >mark

      b>
      swap
      >mark

      3 r5 cmp_,#_
      ne bc>
      1 r4 movs_,#_
      r2 r4 lsls_,_
      r4 r3 orrs_,_
      >mark

      >mark
      1 r2 adds_,#_
      1 tos tos lsrs_,_,#_
      1 r0 r0 lsrs_,_,#_
      1 r1 r1 lsrs_,_,#_
      swap
      b<

      >mark
      r3 tos movs_,_
      r5 r4 2 pop
      ]code

      unsafe::>integral
    ;

    \ Cycle the middle of columns
    :private cycle-columns { self -- }
      0 0 { col row }

      self life-width@ { width }
      self life-height@ 5 rshift { height }
      
      begin row height < while

        width 1- { left-col }
        left-col row self column@ { left-column }
        row 1- dup 0< if drop height 1- then { upper-row }
        left-col upper-row self column@ { upper-left-column }
        row 1+ dup height = if drop 0 then { lower-row }
        left-col lower-row self column@ { lower-left-column }
        0 row self column@ { current-column }
        0 upper-row self column@ { upper-column }
        0 lower-row self column@ { lower-column }
        
        begin col width < while

          col 1+ dup width = if drop 0 then { right-col }
          right-col row self column@ { right-column }
          right-col upper-row self column@ { upper-right-column }
          right-col lower-row self column@ { lower-right-column }

          left-column current-column right-column cycle-column-middle
          0 upper-left-column 31 rshift 1 and +
          upper-column 31 rshift 1 and +
          upper-right-column 31 rshift 1 and +
          left-column 1 and +
          right-column 1 and +
          left-column 1 rshift 1 and +
          current-column 1 rshift 1 and +
          right-column 1 rshift 1 and +
          current-column 1 and if
            dup 2 = swap 3 = or 1 and or
          else
            3 = 1 and or
          then
          0 left-column 30 rshift 1 and +
          current-column 30 rshift 1 and +
          right-column 30 rshift 1 and +
          left-column 31 rshift 1 and +
          right-column 31 rshift 1 and +
          lower-left-column 1 and +
          lower-column 1 and +
          lower-right-column 1 and +
          current-column 31 rshift 1 and if
            dup 2 = swap 3 = or 31 bit and or
          else
            3 = 31 bit and or
          then
          col row self column!

          current-column to left-column
          upper-column to upper-left-column
          lower-column to lower-left-column
          right-column to current-column
          upper-right-column to upper-column
          lower-right-column to lower-column

          1 +to col
          
        repeat
        
        0 to col
        1 +to row
        
      repeat
    ;
    
    \ Cycle the life world
    :method cycle { self -- }
      self life-old-world@
      self life-new-world@ self life-old-world!
      self life-new-world!
      self cycle-columns
    ;

    \ Draw the life world
    :method draw { self -- }
      self life-new-world@ self life-display@ draw-world
    ;

    \ Clear the life world
    :method clear { self -- }
      self life-height@ 5 rshift 0 ?do
        self life-width@ 0 ?do
          0 i j self column!
        loop
      loop
    ;

    \ Set the state of a cell
    :method cell! { state col row self -- }
      self life-width@ { width }
      self life-height@ { height }
      col width mod dup 0< if width + then to col
      row height mod dup 0< if height + then to row
      width row 5 rshift * col + cells { offset }
      offset self life-new-world@ w@+
      row $1F and bit state if or else bic then offset self life-new-world@ w!+
    ;      

    \ Set a row
    :method row! { cells orient col row self -- }
      orient lrtb = orient lrbt = or if
        cells >len 0 ?do
          i cells c@+ [char] # = col i + row self cell!
        loop
      else
        orient rltb = orient rlbt = or averts x-invalid-orient
        cells >len dup col + 1- to col 0 ?do
          i cells c@+ [char] # = col i - row self cell!
        loop
      then
    ;

    \ Set a block
    :method rows! { rows orient col row self -- }
      orient lrtb = orient rltb = or if
        rows orient col row self 4 [: { cells index orient col row self }
          cells orient col row index + self row!
        ;] bind iteri
      else
        orient lrbt = orient rlbt = or averts x-invalid-orient
        rows orient col rows >len + 1- row self 4 [:
          { cells index orient col row self }
          cells orient col row index - self row!
        ;] bind iteri
      then
    ;

    \ Run the world until a key is pressed
    :method run { self -- }
      begin key? not while
        self cycle
        self draw
      repeat
      key drop
    ;

    \ Run the world slowly until a key is pressed
    :method run-slow { delay self -- }
      begin key? not while
        self cycle
        self draw
        delay zscript-task::ms
      repeat
      key drop
    ;

    \ Step the world
    :method step { self -- }
      self cycle
      self draw
    ;
    
  end-class

  \ Add a block to the world
  : block { col row life -- }
    #( s" ##" s" ##" )# lrtb col row life rows!
  ;
  
  \ Add a tub to the world
  : tub { col row life -- }
    #( s" .#." s" #.#" s" .#." )# lrtb col row life rows!
  ;
  
  \ Add a boat to the world
  : boat { orient col row life -- }
    #( s" .#." s" #.#" s" .##" )# orient col row life rows!
  ;

  \ Add a blinker to the world
  : blinker { orient phase col row life -- }
    phase case
      0 of #( s" .#." s" .#." s" .#." )# orient col row life rows! endof
      1 of #( s" ..." s" ###" s" ..." )# orient col row life rows! endof
    endcase
  ;
  
  \ Add a glider to the world
  : glider { orient phase col row life -- }
    phase case
      0 of #( s" .#." s" ..#" s" ###" )# orient col row life rows! endof
      1 of #( s" #.#" s" .##" s" .#." )# orient col row life rows! endof
      2 of #( s" ..#" s" #.#" s" .##" )# orient col row life rows! endof
      3 of #( s" #.." s" .##" s" ##." )# orient col row life rows! endof
    endcase
  ;

  \ Add an R-pentomino to the world
  : r-pentomino { orient col row life -- }
    #( s" .##" s" ##." s" .#." )# orient col row life rows!
  ;
  
end-module