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
  
  zscript-task import
  zscript-chan import
  
  128 constant chan-count
  256 constant msg-count
  
  : create-relay { input output -- }
    fork not if begin input recv output send again then
  ;
  
  : create-start { input -- }
    fork not if msg-count 0 ?do i input send loop terminate then
  ;
  
  : create-end { output -- }
    fork not if msg-count 0 ?do output recv . loop then
  ;
  
  : run-test ( -- )
    0 chan-count [: 1 make-chan ;] collectl-cells { chans }
    chan-count 1- 0 ?do i chans @+ i 1+ chans @+ create-relay loop
    0 chans @+ create-start
    chan-count 1- chans @+ create-end
    start
  ;

end-module
