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

begin-module zscript-uchan

  zscript-queue import
  zscript-task import

  begin-module zscript-uchan-internal
    
    \ The unbounded channel record, defining the unbounded channel's maximum
    \ size and the unbounded channel's queue
    begin-record uchan

      \ The unbounded channel messages
      item: uchan-messages

      \ The unbounded channel receive wait queue
      item: uchan-recv-wait

      \ The unbounded channel receive count
      item: uchan-recv-count

      \ The unbounded channel receive pending boolean
      item: uchan-recv-pending
      
    end-record

    \ Wake a receiving task for a unbounded channel
    : wake-recv { uchan -- }
      uchan uchan-recv-wait@ queue-empty? not
      uchan uchan-recv-pending@ not and if
        true uchan uchan-recv-pending!
        uchan uchan-recv-wait@ wake
      then
    ;

    \ Actually send a message on a unbounded channel
    : actual-send { msg uchan -- }
      msg uchan uchan-messages@ enqueue
      uchan wake-recv
    ;

    \ Actually receive a message on a unbounded channel
    : actual-recv { uchan -- msg }
      uchan uchan-messages@ dequeue if
        false uchan uchan-recv-pending!
        uchan uchan-recv-count@ 1- uchan uchan-recv-count!
      else
        [: ." should not happen!" cr ;] ?raise
      then
    ;

    \ Actually receive a message on a unbounded channel for non-blocking
    : actual-recv-non-block { uchan -- msg }
      uchan uchan-messages@ dequeue if
        false uchan uchan-recv-pending!
      else
        [: ." should not happen!" cr ;] ?raise
      then
    ;

    \ Actually peek a message on a unbounded channel
    : actual-peek { uchan -- msg }
      uchan uchan-messages@ peek-queue if
        false uchan uchan-recv-pending!
        uchan uchan-recv-count@ 1- uchan uchan-recv-count!
      else
        [: ." should not happen!" cr ;] ?raise
      then
    ;

    \ Actually peek a message on a unbounded channel for non-blocking
    : actual-peek-non-block { uchan -- msg }
      uchan uchan-messages@ peek-queue if
        false uchan uchan-recv-pending!
      else
        [: ." should not happen!" cr ;] ?raise
      then
    ;

    \ Wait on receiving a message from a unbounded channel
    : wait-recv { uchan -- msg }
      begin
        uchan uchan-recv-wait@ block
        uchan uchan-messages@ queue-empty? not if
          uchan actual-recv true
        else
          false
        then
      until
    ;

    \ Wait on peeking a message from a unbounded channel
    : wait-peek { uchan -- msg }
      begin
        uchan uchan-recv-wait@ block
        uchan uchan-messages@ queue-empty? not if
          uchan actual-peek true
        else
          false
        then
      until
    ;

  end-module> import

  \ Make a unbounded channel
  : make-uchan { -- uchan }
    make-queue make-queue 0 false >uchan
  ;

  \ Send a message on a unbounded channel, blocking until the unbounded
  \ channel's queue is not full if necessary
  : send { msg uchan -- }
    msg uchan actual-send
  ;
  
  \ Receive a message from a unbounded channel, blocking until the unbounded
  \ channel's queue is not empty if necessary
  : recv { uchan -- msg }
    uchan uchan-recv-count@ 1+ uchan uchan-recv-count!
    uchan uchan-messages@ queue-empty? not if
      uchan uchan-recv-count@ 1 = if
        uchan actual-recv
      else
        uchan wake-recv uchan wait-recv
      then
    else
      uchan wait-recv
    then
  ;

  \ Receive a message from a unbounded channel in a non-blocking fashion,
  \ returning whether receiving was successful
  : recv-non-block { uchan -- msg success? }
    uchan uchan-messages@ queue-empty? not if
      uchan uchan-recv-count@ 0= if
        uchan actual-recv-non-block true
      else
        uchan wake-recv 0 false
      then
    else
      0 false
    then
  ;

  \ Receive a message from a unbounded channel, blocking until the unbounded
  \ channel's queue is not empty if necessary
  : peek { uchan -- msg }
    uchan uchan-recv-count@ 1+ uchan uchan-recv-count!
    uchan uchan-messages@ queue-empty? not if
      uchan uchan-recv-count@ 1 = if
        uchan actual-peek
      else
        uchan wake-recv uchan wait-peek
      then
    else
      uchan wait-peek
    then
  ;

  \ Receive a message from a unbounded channel in a non-blocking fashion,
  \ returning whether receiving was successful
  : peek-non-block { uchan -- msg success? }
    uchan uchan-messages@ queue-empty? not if
      uchan uchan-recv-count@ 0= if
        uchan actual-peek-non-block true
      else
        uchan wake-recv 0 false
      then
    else
      0 false
    then
  ;

end-module