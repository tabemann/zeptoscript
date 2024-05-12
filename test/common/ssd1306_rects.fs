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

begin-module test

  zscript-oo import
  zscript-bitmap import
  zscript-bitmap-utils import
  zscript-ssd1306 import
  
  128 constant display-width
  64 constant display-height
  1 constant i2c-device
  14 constant i2c-pin0
  15 constant i2c-pin1

  : run-test ( -- )
    
    i2c-pin0 i2c-pin1 display-width display-height SSD1306_I2C_ADDR i2c-device
    make-ssd1306 { my-ssd1306 }
    
    $FF
    display-width 4 / display-height 4 /
    display-width 2 / display-height 2 /
    op-xor
    my-ssd1306 draw-rect-const
    my-ssd1306 update-display
    
    display-width display-height make-bitmap { my-bitmap }
    
    $FF
    display-width 2 / display-width 8 / -
    display-height 2 / display-height 8 / -
    display-width 4 / display-height 4 /
    op-set
    my-bitmap draw-rect-const
    
    0 0
    0 0
    display-width display-height
    op-xor
    my-bitmap
    my-ssd1306 draw-rect
    my-ssd1306 update-display

  ;

end-module
