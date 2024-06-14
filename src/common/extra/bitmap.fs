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

begin-module zscript-bitmap

  zscript-armv6m import
  zscript-oo import
  
  \ Pixel drawing operations

  \ Set pixels
  symbol op-set

  \ Or pixels
  symbol op-or

  \ And pixels
  symbol op-and

  \ Bit-clear pixels
  symbol op-bic

  \ Exclusive-or pixels
  symbol op-xor

  \ Invalid operation exception
  : x-invalid-op ( -- ) ." invalid drawing operation" cr ;

  \ Clear the dirty rectnagle
  method clear-dirty ( bitmap -- )
  
  \ Get whether a bitmap is dirty
  method dirty? ( bitmap -- dirty? )

  \ Get the dirty rectangle
  method dirty-rect@ ( bitmap -- start-col start-row end-col end-row )
  
  \ Get bitmap dimensions
  method dim@ ( bitmap -- cols rows )

  \ Clear the bitmap
  method clear-bitmap ( bitmap -- )

  \ Get a backing bitmap
  method back-bitmap@ ( bitmap -- bitmap )
  
  \ Get the state of a pixel
  method pixel@ ( col row bitmap -- state )

  \ Get the state of a pixel without validation
  method unsafe-pixel@ ( col row bitmap -- state )

  \ Draw a constant pixel on a bitmap
  method draw-pixel-const ( const dst-col dst-row op dst -- )

  \ Draw a constant rectangle on a bitmap
  method draw-rect-const
  ( const start-dst-col start-dst-row cols rows op dst -- )

  \ Draw a rectangle on a bitmap from another bitmap
  method draw-rect
  ( start-src-col start-src-row start-dst-col start-dst-row cols rows op src )
  ( dst -- )

  \ Copy pixels from a bitmap into a buffer
  method copy-pixels ( col page dest count self -- )

  \ Bitmap class
  begin-class bitmap
    
    \ Number of columns in bitmap
    member: bitmap-cols

    \ Number of rows in bitmap
    member: bitmap-rows

    \ Bitmap buffer
    member: bitmap-buf

    \ Dirty rectangle start column
    member: dirty-start-col

    \ Dirty rectangle end column
    member: dirty-end-col

    \ Dirty rectangle start row
    member: dirty-start-row

    \ Dirty rectangle end row
    member: dirty-end-row

    \ Get the size of a bitmap buffer in bytes for a given number of columsn and
    \ rows
    :private bitmap-buf-size { cols rows -- bytes }
      rows 8 align 3 rshift cols *
    ;

    \ Get the address of a column and a row
    :private col-row-addr { col row self -- addr }
      self bitmap-cols@ row 3 rshift * col +
      [ 4 cells ] literal ensure
      self bitmap-buf@ forth::cell+ unsafe::>integral +
    ;

    \ Get the address of two columns and two rows in two buffers
    :private 2col-row-addr
      { src-col src-row dest-col dest-row src self -- src-addr dest-addr }
      self bitmap-cols@ dest-row 3 rshift * dest-col +
      src bitmap-cols@ src-row 3 rshift * src-col +
      [ 8 cells ] literal ensure
      src bitmap-buf@ forth::cell+ unsafe::>integral + swap
      self bitmap-buf@ forth::cell+ unsafe::>integral +
    ;
    
    \ Copy pixels from a bitmap to a buffer
    :method copy-pixels { col page dest count self -- }
      self bitmap-buf@ self bitmap-cols@ page * col + dest count
      unsafe::bytes>buffer
    ;

    \ Set the entire bitmap to be dirty
    :private set-dirty { self -- }
      0 self dirty-start-col!
      self bitmap-cols@ self dirty-end-col!
      0 self dirty-start-row!
      self bitmap-rows@ self dirty-end-row!
    ;

    \ Dirty a pixel on an bitmap
    :private dirty-pixel { col row self -- }
      self dirty? if
        row self dirty-start-row@ min self dirty-start-row!
        row 1+ self dirty-end-row@ max self dirty-end-row!
        col self dirty-start-col@ min self dirty-start-col!
        col 1+ self dirty-end-col@ max self dirty-end-col!
      else
        row self dirty-start-row!
        row 1+ self dirty-end-row!
        col self dirty-start-col!
        col 1+ self dirty-end-col!
      then
    ;

    \ Dirty an area on an bitmap
    :private dirty-area { start-col end-col start-row end-row self -- }
      start-col end-col < start-row end-row < and if
        start-col start-row self dirty-pixel
        end-col 1- end-row 1- self dirty-pixel
      then
    ;

    \ Set a strip from a constant to another bitmap
    :private set-strip-const
      { const dst-row row-count col-count dst-col self -- }
      dst-col dst-row self col-row-addr
      dst-row 7 and to dst-row
      unsafe::integral>
      const unsafe::integral> to const
      dst-row unsafe::integral> to dst-row
      row-count unsafe::integral> to row-count
      col-count unsafe::integral> to col-count
      dst-col unsafe::integral> to dst-col
      code[
      8 r0 ldr_,[sp,#_]
      12 r1 ldr_,[sp,#_]
      16 r2 ldr_,[sp,#_]
      20 r3 ldr_,[sp,#_]
      r5 r4 2 push
      \ tos: dst-addr
      \ r0: col-count
      \ r1: row-count
      \ r2: start-dst-row
      \ r3: const
      $FF r5 movs_,#_
      8 r4 movs_,#_
      r1 r4 r4 subs_,_,_
      r4 r5 lsrs_,_
      r2 r5 lsls_,_
      \ r5: mask
      r5 r3 ands_,_
      0 r0 cmp_,#_
      eq bc>
      mark<
      0 tos r4 ldrb_,[_,#_]
      \ r4: dst-byte
      r5 r4 bics_,_
      r3 r4 orrs_,_
      0 tos r4 strb_,[_,#_]
      1 tos adds_,#_
      1 r0 subs_,#_
      ne bc<
      >mark
      tos 1 dp ldm
      r5 r4 2 pop        
      ]code        
    ;
    
    \ Or a strip from a constant to another bitmap
    :private or-strip-const
      { const dst-row row-count col-count dst-col self -- }
      dst-col dst-row self col-row-addr
      dst-row 7 and to dst-row        
      unsafe::integral>
      const unsafe::integral> to const
      dst-row unsafe::integral> to dst-row
      row-count unsafe::integral> to row-count
      col-count unsafe::integral> to col-count
      dst-col unsafe::integral> to dst-col
      code[
      8 r0 ldr_,[sp,#_]
      12 r1 ldr_,[sp,#_]
      16 r2 ldr_,[sp,#_]
      20 r3 ldr_,[sp,#_]
      r5 r4 2 push
      \ tos: dst-addr
      \ r0: col-count
      \ r1: row-count
      \ r2: start-dst-row
      \ r3: const
      $FF r5 movs_,#_
      8 r4 movs_,#_
      r1 r4 r4 subs_,_,_
      r4 r5 lsrs_,_
      r2 r5 lsls_,_
      \ r5: mask
      r5 r3 ands_,_
      0 r0 cmp_,#_
      eq bc>
      mark<
      0 tos r4 ldrb_,[_,#_]
      \ r4: dst-byte
      r3 r4 orrs_,_
      0 tos r4 strb_,[_,#_]
      1 tos adds_,#_
      1 r0 subs_,#_
      ne bc<
      >mark
      tos 1 dp ldm
      r5 r4 2 pop        
      ]code        
    ;

    \ And a strip from a constant to another bitmap
    :private and-strip-const
      { const dst-row row-count col-count dst-col self -- }
      dst-col dst-row self col-row-addr
      dst-row 7 and to dst-row        
      unsafe::integral>
      const unsafe::integral> to const
      dst-row unsafe::integral> to dst-row
      row-count unsafe::integral> to row-count
      col-count unsafe::integral> to col-count
      dst-col unsafe::integral> to dst-col
      code[
      8 r0 ldr_,[sp,#_]
      12 r1 ldr_,[sp,#_]
      16 r2 ldr_,[sp,#_]
      20 r3 ldr_,[sp,#_]
      r5 r4 2 push
      \ tos: dst-addr
      \ r0: col-count
      \ r1: row-count
      \ r2: start-dst-row
      \ r3: const
      $FF r5 movs_,#_
      8 r4 movs_,#_
      r1 r4 r4 subs_,_,_
      r4 r5 lsrs_,_
      r2 r5 lsls_,_
      \ r5: mask
      r3 r3 mvns_,_
      r5 r3 ands_,_
      0 r0 cmp_,#_
      eq bc>
      mark<
      0 tos r4 ldrb_,[_,#_]
      \ r4: dst-byte
      r3 r4 bics_,_
      0 tos r4 strb_,[_,#_]
      1 tos adds_,#_
      1 r0 subs_,#_
      ne bc<
      >mark
      tos 1 dp ldm
      r5 r4 2 pop        
      ]code        
    ;

    \ Bit-clear a strip from a constant to another bitmap
    :private bic-strip-const
      { const dst-row row-count col-count dst-col self -- }
      dst-col dst-row self col-row-addr
      dst-row 7 and to dst-row        
      unsafe::integral>
      const unsafe::integral> to const
      dst-row unsafe::integral> to dst-row
      row-count unsafe::integral> to row-count
      col-count unsafe::integral> to col-count
      dst-col unsafe::integral> to dst-col
      code[
      8 r0 ldr_,[sp,#_]
      12 r1 ldr_,[sp,#_]
      16 r2 ldr_,[sp,#_]
      20 r3 ldr_,[sp,#_]
      r5 r4 2 push
      \ tos: dst-addr
      \ r0: col-count
      \ r1: row-count
      \ r2: start-dst-row
      \ r3: const
      $FF r5 movs_,#_
      8 r4 movs_,#_
      r1 r4 r4 subs_,_,_
      r4 r5 lsrs_,_
      r2 r5 lsls_,_
      \ r5: mask
      r5 r3 ands_,_
      0 r0 cmp_,#_
      eq bc>
      mark<
      0 tos r4 ldrb_,[_,#_]
      \ r4: dst-byte
      r3 r4 bics_,_
      0 tos r4 strb_,[_,#_]
      1 tos adds_,#_
      1 r0 subs_,#_
      ne bc<
      >mark
      tos 1 dp ldm
      r5 r4 2 pop        
      ]code        
    ;
    
    \ Exclusive-or a strip from a constant to another bitmap
    :private xor-strip-const
      { const dst-row row-count col-count dst-col self -- }
      dst-col dst-row self col-row-addr
      dst-row 7 and to dst-row        
      unsafe::integral>
      const unsafe::integral> to const
      dst-row unsafe::integral> to dst-row
      row-count unsafe::integral> to row-count
      col-count unsafe::integral> to col-count
      dst-col unsafe::integral> to dst-col
      code[
      8 r0 ldr_,[sp,#_]
      12 r1 ldr_,[sp,#_]
      16 r2 ldr_,[sp,#_]
      20 r3 ldr_,[sp,#_]
      r5 r4 2 push
      \ tos: dst-addr
      \ r0: col-count
      \ r1: row-count
      \ r2: start-dst-row
      \ r3: const
      $FF r5 movs_,#_
      8 r4 movs_,#_
      r1 r4 r4 subs_,_,_
      r4 r5 lsrs_,_
      r2 r5 lsls_,_
      \ r5: mask
      r5 r3 ands_,_
      0 r0 cmp_,#_
      eq bc>
      mark<
      0 tos r4 ldrb_,[_,#_]
      \ r4: dst-byte
      r3 r4 eors_,_
      0 tos r4 strb_,[_,#_]
      1 tos adds_,#_
      1 r0 subs_,#_
      ne bc<
      >mark
      tos 1 dp ldm
      r5 r4 2 pop        
      ]code        
    ;

    \ Set a strip from one bitmap to another bitmap
    :private set-strip
      { src-row dst-row row-count col-count src-col dst-col src dst -- }
      src-col src-row dst-col dst-row src dst 2col-row-addr
      src-row 7 and to src-row
      dst-row 7 and to dst-row
      unsafe::2integral>
      src-row unsafe::integral> to src-row
      dst-row unsafe::integral> to dst-row
      row-count unsafe::integral> to row-count
      col-count unsafe::integral> to col-count
      src-col unsafe::integral> to src-col
      dst-col unsafe::integral> to dst-col
      code[
      r0 1 dp ldm
      16 r1 ldr_,[sp,#_]
      20 r2 ldr_,[sp,#_]
      24 r3 ldr_,[sp,#_]
      r4 1 push
      32 r4 ldr_,[sp,#_]
      r5 1 push
      \ tos: dst-addr
      \ r0: src-addr
      \ r1: col-count
      \ r2: row-count
      \ r3: start-dst-row
      \ r4: start-src-row
      $FF r5 movs_,#_
      r1 1 push
      8 r1 movs_,#_
      r2 r1 r1 subs_,_,_
      r1 r5 lsrs_,_
      \ r5: mask
      r1 1 pop
      0 r1 cmp_,#_
      eq bc>
      mark<
      r5 1 push
      tos r0 2 push
      0 tos tos ldrb_,[_,#_]
      \ tos: dst-byte
      0 r0 r0 ldrb_,[_,#_]
      \ r0: src-byte
      r4 r0 lsrs_,_
      \ r0: src-byte start-src-row rshift
      r5 r0 ands_,_
      \ r0: src-byte start-src-row rshift mask and
      r3 r0 lsls_,_
      \ r0: src-byte start-src-row rshift mask and start-dst-row lshift
      r3 r5 lsls_,_
      \ r5: mask start-dst-row lshift
      r5 tos bics_,_
      \ tos: dst-byte mask start-dst-row lshift bic
      tos r5 movs_,_
      \ r5: dst-byte mask start-dst-row lshift bic
      r0 r5 orrs_,_
      \ r5: combined-byte
      tos r0 2 pop
      \ tos: dst-addr
      \ r0: src-addr
      0 tos r5 strb_,[_,#_]
      r5 1 pop
      \ r5: mask
      1 tos adds_,#_
      1 r0 adds_,#_
      1 r1 subs_,#_
      ne bc<
      >mark
      tos 1 dp ldm
      r5 r4 2 pop        
      ]code
    ;

    \ Or a strip from one bitmap to another bitmap
    :private or-strip
      { src-row dst-row row-count col-count src-col dst-col src dst -- }
      src-col src-row dst-col dst-row src dst 2col-row-addr
      src-row 7 and to src-row
      dst-row 7 and to dst-row
      unsafe::2integral>
      src-row unsafe::integral> to src-row
      dst-row unsafe::integral> to dst-row
      row-count unsafe::integral> to row-count
      col-count unsafe::integral> to col-count
      src-col unsafe::integral> to src-col
      dst-col unsafe::integral> to dst-col
      code[
      r0 1 dp ldm
      16 r1 ldr_,[sp,#_]
      20 r2 ldr_,[sp,#_]
      24 r3 ldr_,[sp,#_]
      r4 1 push
      32 r4 ldr_,[sp,#_]
      r5 1 push
      \ tos: dst-addr
      \ r0: src-addr
      \ r1: col-count
      \ r2: row-count
      \ r3: start-dst-row
      \ r4: start-src-row
      $FF r5 movs_,#_
      r1 1 push
      8 r1 movs_,#_
      r2 r1 r1 subs_,_,_
      r1 r5 lsrs_,_
      \ r5: mask
      r1 1 pop
      0 r1 cmp_,#_
      eq bc>
      mark<
      r5 1 push
      tos r0 2 push
      0 tos tos ldrb_,[_,#_]
      \ tos: dst-byte
      0 r0 r0 ldrb_,[_,#_]
      \ r0: src-byte
      r4 r0 lsrs_,_
      \ r0: src-byte start-src-row rshift
      r5 r0 ands_,_
      \ r0: src-byte start-src-row rshift mask and
      r3 r0 lsls_,_
      \ r0: src-byte start-src-row rshift mask and start-dst-row lshift
      tos r5 movs_,_
      \ r5: dst-byte
      r0 r5 orrs_,_
      \ r5: combined-byte
      tos r0 2 pop
      \ tos: dst-addr
      \ r0: src-addr
      0 tos r5 strb_,[_,#_]
      r5 1 pop
      \ r5: mask
      1 tos adds_,#_
      1 r0 adds_,#_
      1 r1 subs_,#_
      ne bc<
      >mark
      tos 1 dp ldm
      r5 r4 2 pop        
      ]code
    ;

    \ And a strip from one bitmap to another bitmap
    :private and-strip
      { src-row dst-row row-count col-count src-col dst-col src dst -- }
      src-col src-row dst-col dst-row src dst 2col-row-addr
      src-row 7 and to src-row
      dst-row 7 and to dst-row
      unsafe::2integral>
      src-row unsafe::integral> to src-row
      dst-row unsafe::integral> to dst-row
      row-count unsafe::integral> to row-count
      col-count unsafe::integral> to col-count
      src-col unsafe::integral> to src-col
      dst-col unsafe::integral> to dst-col
      code[
      r0 1 dp ldm
      16 r1 ldr_,[sp,#_]
      20 r2 ldr_,[sp,#_]
      24 r3 ldr_,[sp,#_]
      r4 1 push
      32 r4 ldr_,[sp,#_]
      r5 1 push
      \ tos: dst-addr
      \ r0: src-addr
      \ r1: col-count
      \ r2: row-count
      \ r3: start-dst-row
      \ r4: start-src-row
      $FF r5 movs_,#_
      r1 1 push
      8 r1 movs_,#_
      r2 r1 r1 subs_,_,_
      r1 r5 lsrs_,_
      \ r5: mask
      r1 1 pop
      0 r1 cmp_,#_
      eq bc>
      mark<
      r5 1 push
      tos r0 2 push
      0 tos tos ldrb_,[_,#_]
      \ tos: dst-byte
      0 r0 r0 ldrb_,[_,#_]
      \ r0: src-byte
      r0 r0 mvns_,_
      \ r0: src-byte not
      r4 r0 lsrs_,_
      \ r0: src-byte not start-src-row rshift
      r5 r0 ands_,_
      \ r0: src-byte not start-src-row rshift mask and
      r3 r0 lsls_,_
      \ r0: src-byte not start-src-row rshift mask and start-dst-row lshift
      tos r5 movs_,_
      \ r5: dst-byte
      r0 r5 bics_,_
      \ r5: combined-byte
      tos r0 2 pop
      \ tos: dst-addr
      \ r0: src-addr
      0 tos r5 strb_,[_,#_]
      r5 1 pop
      \ r5: mask
      1 tos adds_,#_
      1 r0 adds_,#_
      1 r1 subs_,#_
      ne bc<
      >mark
      tos 1 dp ldm
      r5 r4 2 pop        
      ]code
    ;

    \ Bit-clear a strip from one bitmap to another bitmap
    :private bic-strip
      { src-row dst-row row-count col-count src-col dst-col src dst -- }
      src-col src-row dst-col dst-row src dst 2col-row-addr
      src-row 7 and to src-row
      dst-row 7 and to dst-row
      unsafe::2integral>
      src-row unsafe::integral> to src-row
      dst-row unsafe::integral> to dst-row
      row-count unsafe::integral> to row-count
      col-count unsafe::integral> to col-count
      src-col unsafe::integral> to src-col
      dst-col unsafe::integral> to dst-col
      code[
      r0 1 dp ldm
      16 r1 ldr_,[sp,#_]
      20 r2 ldr_,[sp,#_]
      24 r3 ldr_,[sp,#_]
      r4 1 push
      32 r4 ldr_,[sp,#_]
      r5 1 push
      \ tos: dst-addr
      \ r0: src-addr
      \ r1: col-count
      \ r2: row-count
      \ r3: start-dst-row
      \ r4: start-src-row
      $FF r5 movs_,#_
      r1 1 push
      8 r1 movs_,#_
      r2 r1 r1 subs_,_,_
      r1 r5 lsrs_,_
      \ r5: mask
      r1 1 pop
      0 r1 cmp_,#_
      eq bc>
      mark<
      r5 1 push
      tos r0 2 push
      0 tos tos ldrb_,[_,#_]
      \ tos: dst-byte
      0 r0 r0 ldrb_,[_,#_]
      \ r0: src-byte
      r4 r0 lsrs_,_
      \ r0: src-byte start-src-row rshift
      r5 r0 ands_,_
      \ r0: src-byte start-src-row rshift mask and
      r3 r0 lsls_,_
      \ r0: src-byte start-src-row rshift mask and start-dst-row lshift
      tos r5 movs_,_
      \ r5: dst-byte
      r0 r5 bics_,_
      \ r5: combined-byte
      tos r0 2 pop
      \ tos: dst-addr
      \ r0: src-addr
      0 tos r5 strb_,[_,#_]
      r5 1 pop
      \ r5: mask
      1 tos adds_,#_
      1 r0 adds_,#_
      1 r1 subs_,#_
      ne bc<
      >mark
      tos 1 dp ldm
      r5 r4 2 pop        
      ]code
    ;

    \ Exclusive-or a strip from one bitmap to another bitmap
    :private xor-strip
      { src-row dst-row row-count col-count src-col dst-col src dst -- }
      src-col src-row dst-col dst-row src dst 2col-row-addr
      src-row 7 and to src-row
      dst-row 7 and to dst-row
      unsafe::2integral>
      src-row unsafe::integral> to src-row
      dst-row unsafe::integral> to dst-row
      row-count unsafe::integral> to row-count
      col-count unsafe::integral> to col-count
      src-col unsafe::integral> to src-col
      dst-col unsafe::integral> to dst-col
      code[
      r0 1 dp ldm
      16 r1 ldr_,[sp,#_]
      20 r2 ldr_,[sp,#_]
      24 r3 ldr_,[sp,#_]
      r4 1 push
      32 r4 ldr_,[sp,#_]
      r5 1 push
      \ tos: dst-addr
      \ r0: src-addr
      \ r1: col-count
      \ r2: row-count
      \ r3: start-dst-row
      \ r4: start-src-row
      $FF r5 movs_,#_
      r1 1 push
      8 r1 movs_,#_
      r2 r1 r1 subs_,_,_
      r1 r5 lsrs_,_
      \ r5: mask
      r1 1 pop
      0 r1 cmp_,#_
      eq bc>
      mark<
      r5 1 push
      tos r0 2 push
      0 tos tos ldrb_,[_,#_]
      \ tos: dst-byte
      0 r0 r0 ldrb_,[_,#_]
      \ r0: src-byte
      r4 r0 lsrs_,_
      \ r0: src-byte start-src-row rshift
      r5 r0 ands_,_
      \ r0: src-byte start-src-row rshift mask and
      r3 r0 lsls_,_
      \ r0: src-byte start-src-row rshift mask and start-dst-row lshift
      tos r5 movs_,_
      \ r5: dst-byte
      r0 r5 eors_,_
      \ r5: combined-byte
      tos r0 2 pop
      \ tos: dst-addr
      \ r0: src-addr
      0 tos r5 strb_,[_,#_]
      r5 1 pop
      \ r5: mask
      1 tos adds_,#_
      1 r0 adds_,#_
      1 r1 subs_,#_
      ne bc<
      >mark
      tos 1 dp ldm
      r5 r4 2 pop        
      ]code
    ;
    
    \ Get the next page-aligned row
    :private next-page-row { row -- row' } row 8 align dup row = if 8 + then ;
    
    \ Get the number of rows for an iteration with no source
    :private strip-rows-single { row total-row-count -- row-count }
      row total-row-count + row next-page-row min row -
    ;

    \ Get the number of rows for an iteration
    :private strip-rows { src-row dst-row total-row-count -- row-count }
      src-row total-row-count strip-rows-single dst-row swap strip-rows-single
    ;
    
    \ Carry out an operation on an area with a constant value
    :private blit-const { const dst-col col-count dst-row row-count dst op -- }
      begin row-count 0> while
        dst-row row-count strip-rows-single { strip-row-count }
        const dst-row strip-row-count col-count dst-col dst op execute
        strip-row-count negate +to row-count
        strip-row-count +to dst-row
      repeat
    ;

    \ Carry out an operation on an area
    :private blit
      { src-col dst-col col-count src-row dst-row row-count src dst op -- }
      begin row-count 0> while
        src-row dst-row row-count strip-rows { strip-row-count }
        src-row dst-row strip-row-count col-count src-col dst-col
        src dst op execute
        strip-row-count negate +to row-count
        strip-row-count +to src-row
        strip-row-count +to dst-row
      repeat
    ;
    
    \ Set a pixel with a constant value
    :private set-pixel-const { const dst-col dst-row dst -- }
      0 dst-col <= 0 dst-row <= and
      dst-col dst bitmap-cols@ < dst-row dst bitmap-rows@ < and and if
        dst-col dst-row dst dirty-pixel
        const dst-row 1 1 dst-col dst set-strip-const
      then
    ;
    
    \ Or a pixel with a constant value
    :private or-pixel-const { const dst-col dst-row dst -- }
      0 dst-col <= 0 dst-row <= and
      dst-col dst bitmap-cols@ < dst-row dst bitmap-rows@ < and and if
        dst-col dst-row dst dirty-pixel
        const dst-row 1 1 dst-col dst or-strip-const
      then
    ;

    \ And a pixel with a constant value
    :private and-pixel-const { const dst-col dst-row dst -- }
      0 dst-col <= 0 dst-row <= and
      dst-col dst bitmap-cols@ < dst-row dst bitmap-rows@ < and and if
        dst-col dst-row dst dirty-pixel
        const dst-row 1 1 dst-col dst and-strip-const
      then
    ;

    \ Bit-clear a pixel with a constant value
    :private bic-pixel-const { const dst-col dst-row dst -- }
      0 dst-col <= 0 dst-row <= and
      dst-col dst bitmap-cols@ < dst-row dst bitmap-rows@ < and and if
        dst-col dst-row dst dirty-pixel
        const dst-row 1 1 dst-col dst bic-strip-const
      then
    ;

    \ Exclusive-or a pixel with a constant value
    :private xor-pixel-const { const dst-col dst-row dst -- }
      0 dst-col <= 0 dst-row <= and
      dst-col dst bitmap-cols@ < dst-row dst bitmap-rows@ < and and if
        dst-col dst-row dst dirty-pixel
        const dst-row 1 1 dst-col dst xor-strip-const
      then
    ;
    
    \ Set a rectangle with a constant value
    :private set-rect-const
      ( const start-dst-col start-dst-row col-count row-count dst-bitmap -- )
      { dst } dst dim@ zscript-clip::clip-dst-only
      { const dst-col dst-row col-count row-count }
      dst-col dup col-count + dst-row dup row-count + dst dirty-area
      const dst-col col-count dst-row row-count dst
      ['] set-strip-const blit-const
    ;

    \ Or a rectangle with a constant value
    :private or-rect-const
      ( const start-dst-col start-dst-row col-count row-count dst-bitmap -- )
      { dst } dst dim@ zscript-clip::clip-dst-only
      { const dst-col dst-row col-count row-count }
      dst-col dup col-count + dst-row dup row-count + dst dirty-area
      const dst-col col-count dst-row row-count dst
      ['] or-strip-const blit-const
    ;

    \ And a rectangle with a constant value
    :private and-rect-const
      ( const start-dst-col start-dst-row col-count row-count dst-bitmap -- )
      { dst } dst dim@ zscript-clip::clip-dst-only
      { const dst-col dst-row col-count row-count }
      dst-col dup col-count + dst-row dup row-count + dst dirty-area
      const dst-col col-count dst-row row-count dst
      ['] and-strip-const blit-const
    ;

    \ Bit-clear a rectangle with a constant value
    :private bic-rect-const
      ( const start-dst-col start-dst-row col-count row-count dst-bitmap -- )
      { dst } dst dim@ zscript-clip::clip-dst-only
      { const dst-col dst-row col-count row-count }
      dst-col dup col-count + dst-row dup row-count + dst dirty-area
      const dst-col col-count dst-row row-count dst
      ['] bic-strip-const blit-const
    ;

    \ Exclusive-or a rectangle with a constant value
    :private xor-rect-const
      ( const start-dst-col start-dst-row col-count row-count dst-bitmap -- )
      { dst } dst dim@ zscript-clip::clip-dst-only
      { const dst-col dst-row col-count row-count }
      dst-col dup col-count + dst-row dup row-count + dst dirty-area
      const dst-col col-count dst-row row-count dst
      ['] xor-strip-const blit-const
    ;

    \ Set a rectangle
    :private set-rect
      ( start-src-col start-src-row start-dst-col start-dst-row col-count )
      ( row-count src-bitmap dst-bitmap -- )
      { src dst } src dim@ dst dim@ zscript-clip::clip-src-dst
      { src-col src-row dst-col dst-row col-count row-count }
      dst-col dup col-count + dst-row dup row-count + dst dirty-area
      src-col dst-col col-count src-row dst-row row-count src dst
      ['] set-strip blit
    ;
    
    \ Or a rectangle
    :private or-rect
      ( start-src-col start-src-row start-dst-col start-dst-row col-count )
      ( row-count src-bitmap dst-bitmap -- )
      { src dst } src dim@ dst dim@ zscript-clip::clip-src-dst
      { src-col src-row dst-col dst-row col-count row-count }
      dst-col dup col-count + dst-row dup row-count + dst dirty-area
      src-col dst-col col-count src-row dst-row row-count src dst
      ['] or-strip blit
    ;
    
    \ And a rectangle
    :private and-rect
      ( start-src-col start-src-row start-dst-col start-dst-row col-count )
      ( row-count src-bitmap dst-bitmap -- )
      { src dst } src dim@ dst dim@ zscript-clip::clip-src-dst
      { src-col src-row dst-col dst-row col-count row-count }
      dst-col dup col-count + dst-row dup row-count + dst dirty-area
      src-col dst-col col-count src-row dst-row row-count src dst
      ['] and-strip blit
    ;

    \ Bit-clear a rectangle
    :private bic-rect
      ( start-src-col start-src-row start-dst-col start-dst-row col-count )
      ( row-count src-bitmap dst-bitmap -- )
      { src dst } src dim@ dst dim@ zscript-clip::clip-src-dst
      { src-col src-row dst-col dst-row col-count row-count }
      dst-col dup col-count + dst-row dup row-count + dst dirty-area
      src-col dst-col col-count src-row dst-row row-count src dst
      ['] bic-strip blit
    ;

    \ Exclusive-or a rectangle
    :private xor-rect
      ( start-src-col start-src-row start-dst-col start-dst-row col-count )
      ( row-count src-bitmap dst-bitmap -- )
      { src dst } src dim@ dst dim@ zscript-clip::clip-src-dst
      { src-col src-row dst-col dst-row col-count row-count }
      dst-col dup col-count + dst-row dup row-count + dst dirty-area
      src-col dst-col col-count src-row dst-row row-count src dst
      ['] xor-strip blit
    ;

    \ Clear a bitmap
    :method clear-bitmap { self -- }
      self bitmap-buf@ dup >len unsafe::integral>
      swap forth::cell+ swap 0 unsafe::integral> forth::fill
      self set-dirty
    ;

    \ Get a backing bitmap
    :method back-bitmap@ ( self -- ) ;
    
    \ Clear dirty rectangle
    :method clear-dirty { self -- }
      0 self dirty-start-col!
      0 self dirty-end-col!
      0 self dirty-start-row!
      0 self dirty-end-row!
    ;
    
    \ Get whether an bitmap is dirty
    :method dirty? { self -- dirty? }
      self dirty-start-col@ self dirty-end-col@ <>
      self dirty-start-row@ self dirty-end-row@ <> and
    ;

    \ Get the dirty rectangle
    :method dirty-rect@ { self -- start-col start-row end-col end-row }
      self dirty-start-col@ self dirty-start-row@
      self dirty-end-col@ self dirty-end-row@
    ;
  
    \ Constructor
    :method new { cols rows self -- }
      cols rows bitmap-buf-size make-bytes self bitmap-buf!
      cols self bitmap-cols!
      rows self bitmap-rows!
      0 self dirty-start-col!
      0 self dirty-start-row!
      0 self dirty-end-col!
      0 self dirty-end-row!
    ;

    \ Get the size of a bitmap
    :method dim@ { self -- cols rows }
      self bitmap-cols@
      self bitmap-rows@
    ;
    
    \ Get the state of a pixel
    :method pixel@ { col row self -- state }
      0 col <= 0 row <= and
      col self bitmap-cols@ < row self bitmap-rows@ < and and if
        col row self col-row-addr unsafe::c@ row 7 and rshift 1 and 0<>
      else
        false
      then
    ;

    \ Get the state of a pixel without validation
    :method unsafe-pixel@ { col row self -- state }
      col row self col-row-addr unsafe::c@ row 7 and rshift 1 and 0<>
    ;

    \ Draw a constant pixel on a bitmap
    :method draw-pixel-const ( const dst-col dst-row op dst -- )
      swap case
        op-set of set-pixel-const endof
        op-or of or-pixel-const endof
        op-and of and-pixel-const endof
        op-bic of bic-pixel-const endof
        op-xor of xor-pixel-const endof
        ['] x-invalid-op ?raise
      endcase
    ;

    \ Draw a constant rectangle on a bitmap
    :method draw-rect-const
      ( const start-dst-col start-dst-row cols rows op dst -- )
      swap case
        op-set of set-rect-const endof
        op-or of or-rect-const endof
        op-and of and-rect-const endof
        op-bic of bic-rect-const endof
        op-xor of xor-rect-const endof
        ['] x-invalid-op ?raise
      endcase
    ;

    \ Draw a rectangle on a bitmap from another bitmap
    :method draw-rect
      ( start-src-col start-src-row start-dst-col start-dst-row cols rows )
      ( op src dst -- )
      swap back-bitmap@ swap
      rot case
        op-set of set-rect endof
        op-or of or-rect endof
        op-and of and-rect endof
        op-bic of bic-rect endof
        op-xor of xor-rect endof
        ['] x-invalid-op ?raise
      endcase
    ;

  end-class
  
end-module
