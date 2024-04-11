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
  
  zscript import
  zscript-oo import

  method next-value ( self -- value )
  
  begin-class counter

    member: my-counter-value

    :method new { init-value self -- }
      init-value self my-counter-value!
    ;

    :method next-value { self -- value }
      self my-counter-value@ dup 1+ self my-counter-value!
    ;

  end-class

  begin-class rev-counter

    member: my-rev-counter-value

    :method new { init-value self -- }
      init-value self my-rev-counter-value!
    ;

    :method next-value { self -- value }
      self my-rev-counter-value@ 1- dup self my-rev-counter-value!
    ;

  end-class

  method add-counter ( counter self -- )
  method next-values ( self -- values )

  begin-class counter-set
    
    member: my-counters

    :method new { self -- }
      0cells self my-counters!
    ;

    :method add-counter { counter self -- }
      self my-counters@ counter 1 >cells concat self my-counters!
    ;

    :method next-values { self -- values }
      self my-counters@ ['] next-value map
    ;
    
  end-class

  : run-test ( -- )
    make-counter-set { counters }
    0 make-counter counters add-counter
    16 make-counter counters add-counter
    256 make-counter counters add-counter
    0 make-rev-counter counters add-counter
    16 make-rev-counter counters add-counter
    256 make-rev-counter counters add-counter
    counters next-values ['] . iter
    counters next-values ['] . iter
    counters next-values ['] . iter
  ;
  
end-module
