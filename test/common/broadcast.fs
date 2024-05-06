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
  zscript-queue import
  
  symbol done
  symbol do-send
  symbol do-subscribe
  symbol do-terminate
  
  global wait-count
  
  : relay-task ( -- cmd-chan )
    1 make-chan { cmd-chan }
    fork not if
      make-queue ref { send-queue }
      begin
        cmd-chan recv pair> over do-send = if
          nip { msg }
          make-queue { new-queue }
          begin send-queue ref@ dequeue while
            dup new-queue enqueue
            msg swap send
          repeat
          drop
          new-queue send-queue ref!
        else
          over do-subscribe = if
            send-queue ref@ enqueue drop
          else
            swap do-terminate = if
              drop terminate
            else
              drop
            then
          then
        then
      again
    then
    cmd-chan
  ;
  
  : subscribe { subscribe-chan cmd-chan -- }
    do-subscribe subscribe-chan >pair cmd-chan send
  ;
  
  : broadcast { msg cmd-chan -- }
    do-send msg >pair cmd-chan send
  ;
  
  : send-task { end-index start-index incr cmd-chan -- }
    fork not if
      end-index start-index ?do i cmd-chan broadcast incr +loop
      wait-count@ 1- wait-count!
      done cmd-chan broadcast
      terminate
    then
  ;
  
  : recv-task { label cmd-chan -- }
    fork not if
      1 make-chan { recv-chan }
      recv-chan cmd-chan subscribe
      begin wait-count@ 0> while
        recv-chan recv dup done <> if label type . else drop then
      repeat
      terminate
    then
  ;
  
  : terminate-task { cmd-chan -- }
    fork not if
      1 make-chan { recv-chan }
      recv-chan cmd-chan subscribe
      begin wait-count@ 0> while
        recv-chan recv drop
      repeat
      do-terminate 0 >pair cmd-chan send
      terminate
    then
  ;
  
  : run-test ( -- )
    relay-task { cmd-chan }
    s" A: " cmd-chan recv-task
    s" B: " cmd-chan recv-task
    s" C: " cmd-chan recv-task
    cmd-chan terminate-task
    10 0 1 cmd-chan send-task
    -10 -1 -1 cmd-chan send-task
    3 wait-count!
    start
  ;
  
end-module
