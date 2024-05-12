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
  zscript-font import
  zscript-simple-font import
  zscript-double import
  
  128 constant display-width
  64 constant display-height
  1 constant i2c-device
  14 constant i2c-pin0
  15 constant i2c-pin1
  
  0 1 foreign forth::rng::random random

  25,0 constant init-x-delta
  25,0 constant init-y-delta
    
  : run-test ( -- )
    
    i2c-pin0 i2c-pin1 display-width display-height SSD1306_I2C_ADDR i2c-device
    make-ssd1306 { my-ssd1306 }

    make-simple-font { my-font }
    
    s" Hello, World!" my-font string-dim@ { hello-cols hello-rows }
        
    my-ssd1306 clear-bitmap
    my-ssd1306 update-display
    
    display-width hello-cols - s>f { x-bound }
    display-height hello-rows - s>f { y-bound }

    display-width 2 / hello-cols 2 / - s>f { x }
    display-height 2 / hello-rows 2 / - s>f { y }
    
    init-x-delta { x-delta }
    init-y-delta { y-delta }
    
    systick-counter { old-systick }
    
    begin key? not while

      s" Hello, World!"
      x round-half-zero y round-half-zero
      op-xor
      my-ssd1306
      my-font
      draw-string
      
      my-ssd1306 update-display
      
      s" Hello, World!"
      x round-half-zero y round-half-zero
      op-xor
      my-ssd1306
      my-font
      draw-string
      
      systick-counter { new-systick }
      
      new-systick old-systick - s>f 10000,0 f/ { factor }
      x-delta factor f* x d+ to x
      y-delta factor f* y d+ to y
      x d0< x x-bound d> or if
        x-delta dnegate to x-delta
        x-delta factor f* x d+ to x
      then
      y d0< y y-bound d> or if
        y-delta dnegate to y-delta
        y-delta factor f* y d+ to y
      then
      
      new-systick to old-systick
      
    repeat
    
    my-ssd1306 clear-bitmap
    my-ssd1306 update-display
  ;
  
end-module

