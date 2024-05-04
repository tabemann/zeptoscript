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
  
  symbol foo
  symbol bar
  
  : run-test-0 ( -- )
    1 make-chan { my-chan }
    fork not if 1000 ms foo my-chan send bar my-chan send terminate then
    fork not if my-chan peek symbol>name type my-chan peek symbol>name type terminate then
    start
  ;
  
  : my-peek { chan -- }
    begin chan peek-non-block dup not if nip ." *" yield then until
   ;
  
  : run-test-1 ( -- )
    1 make-chan { my-chan }
    fork not if 1000 ms foo my-chan send bar my-chan send terminate then
    fork not if my-chan my-peek symbol>name type my-chan my-peek symbol>name type terminate then
    start
  ;
  
  : my-recv { chan -- }
    begin chan recv-non-block dup not if nip ." *" yield then until
  ;
  
  : run-test-2 ( -- )
    1 make-chan { my-chan }
    fork not if 1000 ms foo my-chan send bar my-chan send terminate then
    fork not if my-chan my-recv symbol>name type my-chan my-recv symbol>name type terminate then
    start
  ;
  
  : my-send { msg chan -- }
    begin msg chan send-non-block dup not if ." *" yield then until
  ;
  
  : run-test-3 ( -- )
    1 make-chan { my-chan }
    fork not if foo my-chan my-send bar my-chan my-send terminate then
    fork not if 1000 ms my-chan recv symbol>name type my-chan recv symbol>name type terminate then
    start
  ;
  
end-module