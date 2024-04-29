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

begin-module zscript-task

  zscript-queue import
  
  begin-module zscript-task-internal

    \ Queue is empty exception
    : x-queue-empty ( -- ) ." queue is empty" cr ;
    
    \ The systick counter
    0 1 foreign forth::systick::systick-counter systick-counter

    \ Number of ticks per millisecond
    10 constant ticks-per-ms
    
    \ The task queue
    global tasks

    \ Initialize the task queue
    : init-tasks ( -- )
      make-queue tasks!
    ;

    initializer init-tasks

    \ Dequeue from a queue while raising an exception if empty
    : dequeue ( queue -- ) dequeue averts x-queue-empty ;
    
  end-module> import

  \ Schedule a task, which will begin execution with the specified closure or
  \ continuation
  : schedule { task -- }
    task tasks@ enqueue
  ;

  \ Yield the current task and execute the first task in the queue unless no
  \ other tasks are queued, where then execution continues as before
  : yield ( -- )
    tasks@ queue-empty? not if
      [: { current-task }
        tasks@ dequeue { next-task }
        current-task schedule
        0 next-task execute
      ;] call/cc drop
    then
  ;

  \ Fork the current task, with the new task being enqueued for future
  \ execution
  : fork ( -- parent? )
    [: { cont }
      false 1 cont bind schedule
      true cont execute
    ;] call/cc
  ;

  \ Start execution of the first enqueued task; note that this word does not
  \ return
  : start ( -- )
    tasks@ queue-empty? not if
      0 tasks@ dequeue execute
    then
  ;

  \ Wake up a task in a queue
  : wake { queue -- }
    queue queue-empty? not if
      queue dequeue schedule
    then
  ;

  \ Block a task in a queue
  : block { queue -- }
    tasks@ queue-empty? not if
      queue [: { queue cont }
        tasks@ dequeue { next-task }
        cont queue enqueue
        0 next-task execute
      ;] call/cc 2drop
    then
  ;

  \ Wait for a given delay from a time
  : wait-delay { start-time delay -- }
    begin
      systick-counter start-time - delay < if
        yield false
      else
        true
      then
    until
  ;

  \ Delay execution of the current task by a specified number of milliseconds
  : ms ( time -- )
    ticks-per-ms * systick-counter swap wait-delay
  ;
  
end-module
