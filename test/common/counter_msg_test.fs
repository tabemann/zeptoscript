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

  zscript-action import
  
  begin-record counter
    item: current-count
  end-record
  
  global sender-action
  global receiver-action
  
  global do-send
  global do-receive
  
  0 1 foreign forth::key? key?
  
  : sender { counter -- }
    do-send@ counter current-count@ receiver-action@ send-action
    counter current-count@ 1+ counter current-count!
  ;
  
  : receiver { data src -- }
    data .
    do-receive@ recv-action
    key? if current-schedule stop-schedule then
  ;
  
  : run-test ( -- )
    0 >counter { counter }
    counter 1 ['] sender bind do-send!
    ['] receiver do-receive!
    make-schedule { schedule }
    do-send@ make-action sender-action!
    [: do-receive@ recv-action ;] make-action receiver-action!
    schedule receiver-action@ add-action
    schedule sender-action@ add-action
    schedule run-schedule
  ;
  
end-module
