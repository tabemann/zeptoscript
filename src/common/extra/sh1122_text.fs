\ Copyright (c) 2023-2024 Travis Bemann
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

begin-module zscript-sh1122-text

  zscript-oo import
  zscript-text-display import
  zscript-font import
  zscript-task import

  \ Update the SH1122 device
  method update-display ( self -- )
  
  \ Change the SH1122 device contrast
  method display-contrast! ( contrast self -- )
  
  \ Set the foreground gray level and dirty the display
  method fg-gray! ( fg-gray self -- )
  
  \ Set the background gray level and dirty the display
  method bg-gray! ( bg-gray self -- )
  
  \ Get the foreground gray level
  method fg-gray@ ( self -- fg-gray )
  
  \ Get the foreground gray level
  method bg-gray@ ( self -- bg-gray )

  begin-module zscript-sh1122-text-internal

    foreign-constant forth::pin::low low
    foreign-constant forth::pin::high high
    1 0 foreign forth::pin::output-pin output-pin
    2 0 foreign forth::pin::pin! pin!
    2 0 foreign forth::spi::spi-pin spi-pin
    1 0 foreign forth::spi::master-spi master-spi
    2 0 foreign forth::spi::spi-baud! spi-baud!
    2 0 foreign forth::spi::spi-data-size! spi-data-size!
    3 0 foreign forth::spi::motorola-spi motorola-spi
    1 0 foreign forth::spi::enable-spi enable-spi
    0 1 foreign forth::dma-pool::allocate-dma allocate-dma
    1 0 foreign forth::dma-pool::free-dma free-dma

    : buffer>spi { bytes device -- }
      bytes unsafe::bytes>addr-len unsafe::2integral>
      device unsafe::integral>
      forth::spi::buffer>spi
    ;

    : buffer>spi-raw-dma { bytes dma1 dma0 device -- output }
      bytes unsafe::bytes>addr-len unsafe::2integral>
      dma1 unsafe::integral>
      dma0 unsafe::integral>
      device unsafe::integral>
      forth::spi::buffer>spi-raw-dma
      unsafe::>integral
    ;
    
  end-module> import

  begin-class sh1122-text

    \ Font
    member: sh1122-text-font
    
    \ SPI device
    member: sh1122-text-device

    \ Reset pin
    member: sh1122-text-reset-pin

    \ DC pin
    member: sh1122-text-dc-pin

    \ CS pin
    member: sh1122-text-cs-pin

    \ CLK pin
    member: sh1122-text-clk-pin

    \ DIN pin
    member: sh1122-text-din-pin

    \ Foreground gray level (grayscale from 0 to 15)
    member: sh1122-text-fg-gray

    \ Background gray level (grayscale from 0 to 15)
    member: sh1122-text-bg-gray

    \ Display columns
    member: sh1122-text-cols

    \ Display rows
    member: sh1122-text-rows

    \ Text display
    member: text-display

    \ One byte command buffer
    member: sh1122-text-cmd1-buf

    \ Two byte command buffer
    member: sh1122-text-cmd2-buf

    \ Four byte command buffer
    member: sh1122-text-cmd4-buf

    \ Data buffer
    member: sh1122-text-data-buf

    \ DMA channel 0
    member: sh1122-text-dma0

    \ DMA channel 1
    member: sh1122-text-dma1

    \ Reset the SH1122
    :private reset-sh1122-text { self -- }
      high self sh1122-text-reset-pin@ pin!
      200 ms
      low self sh1122-text-reset-pin@ pin!
      200 ms
      high self sh1122-text-reset-pin@ pin!
      200 ms
    ;

    \ Start a transfer
    :private start-sh1122-transfer ( self -- )
      low swap sh1122-text-cs-pin@ pin!
    ;

    \ End a transfer
    :private end-sh1122-transfer ( self -- )
      high swap sh1122-text-cs-pin@ pin!
    ;

    \ Send a command to the SH1122
    :private cmd>sh1122-text { cmd self -- }
      self sh1122-text-cmd1-buf@ { cmd-buf }
      cmd 0 cmd-buf c!+
      cmd-buf
      self sh1122-text-dma1@ self sh1122-text-dma0@
      self sh1122-text-device@ buffer>spi-raw-dma drop
    ;

    \ Send a command and argument to the SH1122
    :private cmd-arg>sh1122-text { cmd arg self -- }
      self sh1122-text-cmd2-buf@ { cmd-buf }
      cmd 0 cmd-buf c!+
      arg 1 cmd-buf c!+
      cmd-buf
      self sh1122-text-dma1@ self sh1122-text-dma0@
      self sh1122-text-device@ buffer>spi-raw-dma drop
    ;
    
    \ Send four command bytes
    :private cmd4>sh1122-text { self -- }
      self sh1122-text-cmd4-buf@
      self sh1122-text-dma1@ self sh1122-text-dma0@
      self sh1122-text-device@ buffer>spi-raw-dma drop
    ;

    \ Send data
    :private data>sh1122-text { cols self -- }
      high self sh1122-text-dc-pin@ pin!
      0 cols 1 rshift self sh1122-text-data-buf@ >slice
      self sh1122-text-dma1@ self sh1122-text-dma0@
      self sh1122-text-device@ buffer>spi-raw-dma drop
      low self sh1122-text-dc-pin@ pin!
    ;

    \ Erase the SH1122
    :private erase-sh1122-text { self -- }
      self sh1122-text-bg-gray@ dup 4 lshift or { gray }
      self sh1122-text-cmd4-buf@ { cmd-buf }
      self sh1122-text-data-buf@ { data-buf }

      self sh1122-text-cols@ 1 rshift 0 ?do gray i data-buf c!+ loop

      $B0 0 cmd-buf c!+
      0 2 cmd-buf c!+
      $10 3 cmd-buf c!+

      self start-sh1122-transfer
      self sh1122-text-rows@ 0 ?do
        i 1 cmd-buf c!+
        self cmd4>sh1122-text

        self sh1122-text-cols@ self data>sh1122-text

      loop
      $AF self cmd>sh1122-text \ display on
      self end-sh1122-transfer
    ;

    \ Initialize the SH1122
    :private init-sh1122-text { self -- }
      120 ms

      self start-sh1122-transfer
      
      120 ms

      $AE self cmd>sh1122-text \ display off
      $40 self cmd>sh1122-text \ display start line
      $A0 self cmd>sh1122-text \ remap
      $C0 self cmd>sh1122-text \ remap
      $81 $80 self cmd-arg>sh1122-text \ set display contrast
      $A8 $3F self cmd-arg>sh1122-text \ multiplex ratio 1/64 Duty (0x0F~0x3F)
      $AD $81 self cmd-arg>sh1122-text \ use buildin DC-DC with 0.6 * 500 kHz
      $D5 $50 self cmd-arg>sh1122-text \ set display clock divide ratio (lower 4 bit)/oscillator frequency (upper 4 bit)
      $D3 $00 self cmd-arg>sh1122-text \ display offset, shift mapping ram counter
      $D9 $22 self cmd-arg>sh1122-text \ pre charge (lower 4 bit) and discharge(higher 4 bit) period
      $DB $35 self cmd-arg>sh1122-text \ VCOM deselect level
      $DC $35 self cmd-arg>sh1122-text \ Pre Charge output voltage
      $30 self cmd>sh1122-text \ discharge level

      100 ms

      self end-sh1122-transfer
    ;

    \ Update a rectangular space on the SH1122 device
    :private update-area { start-col end-col start-row end-row self -- }
      self sh1122-text-font@ { font }
      $20 font char-dim@ { char-cols char-rows }
      start-col 1 bic to start-col
      end-col 2 align to end-col
      start-col char-cols /mod { start-char-col start-char-index }
      end-col start-col - { cols }
      char-cols start-char-col - cols min { start-current-cols }
      start-col 1 rshift { start-col2/ }
      self sh1122-text-cmd4-buf@ { cmd-buf }
      self sh1122-text-data-buf@ { data-buf }
      self sh1122-text-fg-gray@ self sh1122-text-bg-gray@ { fg bg }

      $B0 0 cmd-buf c!+
      start-col2/ $F and 2 cmd-buf c!+
      start-col2/ 4 rshift $10 or 3 cmd-buf c!+

      self start-sh1122-transfer

      start-row { row }
      begin row end-row < while

        row 1 cmd-buf c!+
        self cmd4>sh1122-text
        row char-rows /mod { font-pixel-row char-row }
        start-char-index char-row self text-display@ unsafe-invert@ { invert? }
        start-char-index char-row self text-display@ unsafe-char@
        self sh1122-text-font@ find-char-col { offset }
        start-char-col { font-pixel-col }

        0 { col }
        
        begin col cols < while
          
          font-pixel-col char-cols = if
            col start-col + { abs-col }
            abs-col char-cols / { char-index }
            char-index char-row self text-display@ unsafe-invert@ to invert?
            char-index char-row self text-display@ unsafe-char@
            self sh1122-text-font@ find-char-col to offset
            0 to font-pixel-col
          then

          offset font-pixel-col + font-pixel-row font raw-pixel@
          invert? xor
          if fg else bg then 4 lshift
          
          1 +to font-pixel-col

          font-pixel-col char-cols = if
            col 1+ start-col + { abs-col }
            abs-col char-cols / { char-index }
            char-index char-row self text-display@ unsafe-invert@ to invert?
            char-index char-row self text-display@ unsafe-char@
            self sh1122-text-font@ find-char-col to offset
            0 to font-pixel-col
          then

          offset font-pixel-col + font-pixel-row font raw-pixel@
          invert? xor
          if fg else bg then or
          
          col 1 rshift data-buf c!+

          1 +to font-pixel-col

          2 +to col

        repeat
        
        cols self data>sh1122-text

        1 +to row
      repeat
      
      self end-sh1122-transfer
    ;

    \ Constructor
    :method new
      { fg bg din clk dc cs reset font cols rows device self -- }
      font self sh1122-text-font!
      1 make-bytes self sh1122-text-cmd1-buf!
      2 make-bytes self sh1122-text-cmd2-buf!
      4 make-bytes self sh1122-text-cmd4-buf!
      cols 2 align 1 rshift make-bytes self sh1122-text-data-buf!
      font cols rows make-text-display self text-display!
      device self sh1122-text-device!
      reset self sh1122-text-reset-pin!
      dc self sh1122-text-dc-pin!
      cs self sh1122-text-cs-pin!
      clk self sh1122-text-clk-pin!
      din self sh1122-text-din-pin!
      fg $F and self sh1122-text-fg-gray!
      bg $F and self sh1122-text-bg-gray!
      cols self sh1122-text-cols!
      rows self sh1122-text-rows!
      dc output-pin
      cs output-pin
      low dc pin!
      high cs pin!
      reset output-pin
      low reset pin!
      device clk spi-pin
      device din spi-pin
      device master-spi
      20000000 device spi-baud!
      8 device spi-data-size!
      false false device motorola-spi
      device enable-spi
      allocate-dma self sh1122-text-dma0!
      allocate-dma self sh1122-text-dma1!
      self reset-sh1122-text
      self init-sh1122-text
      self erase-sh1122-text
    ;

    \ Update the SH1122 device
    :method update-display { self -- }
      self dirty? if
        self dirty-rect@ { start-col start-row end-col end-row }
        start-col end-col start-row end-row self update-area
        self clear-dirty
      then
    ;

    \ Change the SH1122 device contrast
    :method display-contrast! { contrast self -- }
      self start-sh1122-transfer
      $81 contrast $FF and self cmd-arg>sh1122-text
      self end-sh1122-transfer
    ;

    \ Set the foreground gray level and dirty the display
    :method fg-gray! { fg-gray self -- }
      fg-gray $F and self sh1122-text-fg-gray!
      self set-dirty
    ;

    \ Set the background gray level and dirty the display
    :method bg-gray! { bg-gray self -- }
      bg-gray $F and self sh1122-text-bg-gray!
      self set-dirty
    ;

    \ Get the foreground gray level
    :method fg-gray@ ( self -- ) sh1122-text-fg-gray@ ;

    \ Get the background gray level
    :method bg-gray@ ( self -- ) sh1122-text-bg-gray@ ;

    \ Clear the display
    :method clear-display ( self -- ) text-display@ clear-display ;
    
    \ Set the entire display as dirty
    :method set-dirty ( self -- ) text-display@ set-dirty ;
    
    \ Set a character as dirty
    :method dirty-char ( col row self -- ) text-display@ dirty-char ;
    
    \ Get whether a display is dirty
    :method dirty? ( self -- dirty? ) text-display@ dirty? ;
    
    \ Get the display's dirty rectangle in pixels
    :method dirty-rect@ ( self -- start-col start-row end-col end-row )
      text-display@ dirty-rect@
    ;
    
    \ Clear the entire display's dirty state
    :method clear-dirty ( self -- ) text-display@ clear-dirty ;
    
    \ Get the display dimensions in characters
    :method dim@ ( self -- cols rows ) text-display@ dim@ ;
    
    \ Set a character
    :method char! ( c col row self -- ) text-display@ char! ;
    
    \ Get a character
    :method char@ ( col row self -- c ) text-display@ char@ ;
    
    \ Set a string
    :method string! ( bytes col row self -- ) text-display@ string! ;
    
    \ Set inverted video
    :method invert! ( invert? col row self -- ) text-display@ invert! ;
    
    \ Toggle inverted video
    :method toggle-invert! ( col row self -- ) text-display@ toggle-invert! ;
    
    \ Get inverted video
    :method invert@ ( col row self -- invert? ) text-display@ invert@ ;
    
  end-class
  
end-module