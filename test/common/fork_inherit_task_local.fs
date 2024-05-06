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
  zscript-map import
  
  symbol foo
  
  : make-task-local ( -- )
    16 ['] symbol>integral ['] = make-map task-local!
  ;
  
  : run-test ( -- )
    make-task-local
    0 foo task-local@ insert-map
    fork not if
      100 0 ?do foo task-local@ find-map drop . yield loop
      terminate
    then
    make-task-local
    16 foo task-local@ insert-map
    fork not if
      100 0 ?do foo task-local@ find-map drop . yield loop
      terminate
    then
    make-task-local
    256 foo task-local@ insert-map
    fork not if
      100 0 ?do foo task-local@ find-map drop . yield loop
      terminate
    then
    start
  ;
  
end-module
