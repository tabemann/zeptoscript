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

begin-module zscript-queue
  
  zscript-list import

  begin-module zscript-queue-internal

    \ The queue record
    begin-record queue
      
      \ The old list
      item: queue-old

      \ The new list
      item: queue-new

      \ The queue size
      item: queue-size
      
    end-record
    
  end-module> import

  \ Make a queue
  : make-queue { -- queue }
    empty empty 0 >queue
  ;

  \ Get whether a queue is empty
  : queue-empty? ( queue -- empty? )
    queue-size@ 0=
  ;
  
  \ Push an element onto the queue
  : enqueue { element queue -- }
    element queue queue-new@ cons queue queue-new!
    queue queue-size@ 1+ queue queue-size!
  ;

  \ Pop an element off the queue
  : dequeue { queue -- element success? }
    queue queue-old@ { old }
    old if
      old pair> queue queue-old! true
    else
      queue queue-new@ { new }
      new if
        new rev-list queue queue-old!
        empty queue queue-new!
        queue queue-old@ pair> queue queue-old! true
      else
        0 false
      then
    then
    dup if queue queue-size@ 1- queue queue-size! then
  ;

  \ Get the queue size
  : queue-size ( queue -- size ) queue-size@ ;
  
end-module
