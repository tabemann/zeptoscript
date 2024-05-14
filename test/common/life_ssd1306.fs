\ Copyright (c) 2022-2023 Travis Bemann
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

begin-module life-ssd1306

  zscript-oo import

  \ Default I2C address
  $3C constant SSD1306_I2C_ADDR

  begin-module life-ssd1306-internal

    \ Foreign word imports for I2C
    2 0 foreign forth::i2c::i2c-pin i2c-pin
    1 0 foreign forth::i2c::master-i2c master-i2c
    1 0 foreign forth::i2c::7-bit-i2c-addr 7-bit-i2c-addr
    2 0 foreign forth::i2c::i2c-target-addr! i2c-target-addr!
    1 0 foreign forth::i2c::enable-i2c enable-i2c
    3 1 foreign forth::i2c::>i2c-stop >i2c-stop

    \ SSD1306 Commands - see Datasheet

    \ Indicates following bytes are commands
    $00 constant SSD1306_CMD_START

    \ Indicates following bytes are data
    $40 constant SSD1306_DATA_START

    \ Fundamental Command Table (p. 28)

    \ Double-byte command to set contrast (1-256)
    $81 constant SSD1306_SETCONTRAST

    \ Set entire display on
    $A5 constant SSD1306_ENTIREDISPLAY_ON

    \ Use RAM contents for display
    $A4 constant SSD1306_ENTIREDISPLAY_OFF

    \ Invert RAM contents to display
    $A7 constant SSD1306_SETINVERT_ON

    \ Normal display
    $A6 constant SSD1306_SETINVERT_OFF

    \ Display OFF (sleep mode)
    $AE constant SSD1306_SETDISPLAY_OFF

    \ Display ON (normal mode)
    $AF constant SSD1306_SETDISPLAY_ON

    \ Scrolling Command Table (pp. 28-30)

    \ Configure right horizontal scroll
    $26 constant SSD1306_SCROLL_SETUP_H_RIGHT

    \ Configure left horizontal scroll
    $27 constant SSD1306_SCROLL_SETUP_H_LEFT

    \ Configure right & vertical scroll
    $29 constant SSD1306_SCROLL_SETUP_HV_RIGHT

    \ Configure left & vertical scroll
    $2A constant SSD1306_SCROLL_SETUP_HV_LEFT

    \ Configure vertical scroll area
    $A3 constant SSD1306_SCROLL_SETUP_V

    \ Stop scrolling
    $2E constant SSD1306_SCROLL_DEACTIVATE

    \ Addressing Setting Command Table (pp. 30-31)

    \ Start scrolling
    $2F constant SSD1306_SCROLL_ACTIVATE

    \ Set lower 4 bits of column start address by ORing 4 LSBs
    $00 constant SSD1306_PAGE_COLSTART_LOW

    \ Set upper 4 bits of column start address by ORing 4 LSBs
    $10 constant SSD1306_PAGE_COLSTART_HIGH

    \ Set page start address by ORing 4 LSBs
    $B0 constant SSD1306_PAGE_PAGESTART

    \ Set addressing mode (horizontal, vertical, or page)
    $20 constant SSD1306_SETADDRESSMODE

    \ Send 2 more bytes to set start and end columns for hor/vert modes
    $21 constant SSD1306_SETCOLRANGE

    \ Send 2 more bytes to set start and end pages
    $22 constant SSD1306_SETPAGERANGE

    \ Hardware Configuration Commands (p. 31)

    \ Set RAM display start line by ORing 6 LSBs
    $40 constant SSD1306_SETSTARTLINE

    \ Set column address 0 to display column 0
    $A0 constant SSD1306_COLSCAN_ASCENDING

    \ Set column address 127 to display column 127
    $A1 constant SSD1306_COLSCAN_DESCENDING

    \ Set size of multiplexer based on display height (31 for 32 rows)
    $A8 constant SSD1306_SETMULTIPLEX

    \ Set COM 0 to display row 0
    $C0 constant SSD1306_COMSCAN_ASCENDING

    \ Set COM N-1 to display row 0
    $C8 constant SSD1306_COMSCAN_DESCENDING

    \ Set display vertical shift
    $D3 constant SSD1306_VERTICALOFFSET

    \ Set COM pin hardware configuration
    $DA constant SSD1306_SETCOMPINS

    \ Timing and Driving Scheme Settings Commands (p. 32)

    \ Set display clock divide ratio and frequency
    $D5 constant SSD1306_SETDISPLAYCLOCKDIV

    \ Set pre-charge period
    $D9 constant SSD1306_SETPRECHARGE

    \ Set V_COMH voltage level
    $DB constant SSD1306_SETVCOMLEVEL

    \ No operation
    $E3 constant SSD1306_NOP

    \ Charge Pump Commands (p. 62)

    \ Enable / disable charge pump
    $8D constant SSD1306_SETCHARGEPUMP
    
  end-module> import
  
  \ Update the display
  method draw-world ( world display -- )

  \ Change the SSD1306 device contrast
  method display-contrast! ( contrast display -- )

  \ An I2C SSD1306 device
  begin-class ssd1306

    \ I2C device
    member: ssd1306-device

    \ I2C address
    member: ssd1306-addr

    \ I2C pin 0 (SCK or SDA, does not matter)
    member: ssd1306-pin0

    \ I2C pin 1 (SCK or SDA, does not matter)
    member: ssd1306-pin1

    \ SSD1306 columns
    member: ssd1306-cols
    
    \ SSD1306 rows
    member: ssd1306-rows

    \ Current I2C command base address
    member: ssd1306-cmd-base
    
    \ Write to the I2C command
    :private >cmd ( byte -- ) unsafe::c, ;
    
    \ Begin constructing a command to send to the SSD1306
    :private begin-cmd ( self -- )
      unsafe::ram-here swap ssd1306-cmd-base!
    ;
    
    \ Send the current command that has been constructed
    :private send-cmd { self -- }
      self 1 [: { self }
        self ssd1306-cmd-base@ unsafe::ram-here over - self ssd1306-device@
        >i2c-stop drop
      ;] bind try self ssd1306-cmd-base@ unsafe::ram-here! ?raise
    ;

    \ Initialize I2C
    :private init-i2c { self -- }
      self ssd1306-device@ { device }
      device self ssd1306-pin0@ i2c-pin
      device self ssd1306-pin1@ i2c-pin
      device master-i2c
      device 7-bit-i2c-addr
      self ssd1306-addr@ device i2c-target-addr!
      device enable-i2c
    ;

    \ Initialize an SSD1306 display
    :private init-display { self -- }
      self begin-cmd
      \ Begin a command
      SSD1306_CMD_START >cmd
      \ Turn off display
      SSD1306_SETDISPLAY_OFF >cmd
      \ Set the clock to Fosc = 8, divide ratio = 1
      SSD1306_SETDISPLAYCLOCKDIV >cmd $80 >cmd
      \ Set the display multiplexer to the number of rows - 1
      SSD1306_SETMULTIPLEX >cmd self ssd1306-rows@ nip 1 - >cmd
      \ Set the vertical offset to 0
      SSD1306_VERTICALOFFSET >cmd 0 >cmd
      \ RAM start line 0
      SSD1306_SETSTARTLINE $00 or >cmd
      \ Set the charge pump on
      SSD1306_SETCHARGEPUMP >cmd $14 >cmd
      \ Set the addressing mode to horizontal mode
      SSD1306_SETADDRESSMODE >cmd $00 >cmd
      \ Set flip columns
      SSD1306_COLSCAN_DESCENDING >cmd
      \ Set to not flip pages
      SSD1306_COMSCAN_DESCENDING >cmd
      \ Set COM pins mode
      SSD1306_SETCOMPINS >cmd self ssd1306-rows@ 32 > if
        $12
      else
        $02
      then >cmd
      \ Set contrast to minimal
      SSD1306_SETCONTRAST >cmd $01 >cmd
      \ Set precharge period to phase1 = 15, phase2 = 1
      SSD1306_SETPRECHARGE >cmd $F1 >cmd
      \ Set VCOMH deselect level to (0, 2, 3)
      SSD1306_SETVCOMLEVEL >cmd $40 >cmd
      \ Set u se RAM contents for display
      SSD1306_ENTIREDISPLAY_OFF >cmd
      \ Set no inversion
      SSD1306_SETINVERT_OFF >cmd
      \ Set no scrolling
      SSD1306_SCROLL_DEACTIVATE >cmd
      \ Turn on display in normal mode
      SSD1306_SETDISPLAY_ON >cmd
      self send-cmd
    ;

    \ Send a row of data to the SSD1305 device
    :private send-row { world page self -- }
      page 2 rshift { world-page }
      page 3 and { world-page-part }
      self ssd1306-cols@ { count }
      count world-page * cells world-page-part + { offset }
      self begin-cmd SSD1306_DATA_START >cmd
      begin count 0> while
        offset world c@+ unsafe::c,
        cell +to offset
        -1 +to count
      repeat
      self send-cmd
    ;
    
    \ Update a rectangular space on the SSD1306 device
    :method draw-world { world self -- }
      self begin-cmd
      SSD1306_CMD_START >cmd
      SSD1306_SETPAGERANGE >cmd 0 >cmd self ssd1306-rows@ 3 rshift 1- >cmd
      SSD1306_SETCOLRANGE >cmd 0 >cmd self ssd1306-cols@ 1- >cmd    
      self send-cmd
      self ssd1306-rows@ 3 rshift 0 ?do world i self send-row loop
    ;

    \ Initialize an SSD1306 device
    :method new { pin0 pin1 cols rows i2c-addr i2c-device self -- }
      cols self ssd1306-cols!
      rows self ssd1306-rows!
      i2c-device self ssd1306-device!
      i2c-addr self ssd1306-addr!
      pin1 self ssd1306-pin1!
      pin0 self ssd1306-pin0!
      self init-i2c
    ;

    \ Change the SSD1306 device contrast
    :method display-contrast! { contrast self -- }
      self begin-cmd
      SSD1306_CMD_START >cmd
      SSD1306_SETCONTRAST >cmd contrast $FF and >cmd
      self send-cmd
    ;

  end-class
  
end-module
