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

begin-module zscript-chan

  zscript-queue import
  zscript-task import

  begin-module zscript-chan-internal
    
    \ The channel record, defining the channel's maximum size and the channel's
    \ queue
    begin-record chan

      \ The maximum size of the channel
      item: chan-max-size

      \ The channel messages
      item: chan-messages

      \ The channel send wait queue
      item: chan-send-wait

      \ The channel receive wait queue
      item: chan-recv-wait

      \ The channel send count
      item: chan-send-count

      \ The channel receive count
      item: chan-recv-count

      \ The channel send pending boolean
      item: chan-send-pending

      \ The channel receive pending boolean
      item: chan-recv-pending
      
    end-record

    \ Wake a sending task for a channel
    : wake-send { chan -- }
      chan chan-send-wait@ queue-empty? not
      chan chan-send-pending@ not and if
        true chan chan-send-pending!
        chan chan-send-wait@ wake
      then
    ;

    \ Wake a receiving task for a channel
    : wake-recv { chan -- }
      chan chan-recv-wait@ queue-empty? not
      chan chan-recv-pending@ not and if
        true chan chan-recv-pending!
        chan chan-recv-wait@ wake
      then
    ;

    \ Actually send a message on a channel
    : actual-send { msg chan -- }
      false chan chan-send-pending!
      msg chan chan-messages@ enqueue
      chan chan-send-count@ 1- chan chan-send-count!
      chan wake-recv
    ;

    \ Actually send a message on a channel for non-blocking
    : actual-send-non-block { msg chan -- }
      false chan chan-send-pending!
      msg chan chan-messages@ enqueue
      chan wake-recv
    ;

    \ Actually receive a message on a channel
    : actual-recv { chan -- msg }
      chan chan-messages@ dequeue if
        false chan chan-recv-pending!
        chan chan-recv-count@ 1- chan chan-recv-count!
        chan wake-send
      else
        [: ." should not happen!" cr ;] ?raise
      then
    ;

    \ Actually receive a message on a channel for non-blocking
    : actual-recv-non-block { chan -- msg }
      chan chan-messages@ dequeue if
        false chan chan-recv-pending!
        chan wake-send
      else
        [: ." should not happen!" cr ;] ?raise
      then
    ;

    \ Actually peek a message on a channel
    : actual-peek { chan -- msg }
      chan chan-messages@ peek-queue if
        false chan chan-recv-pending!
        chan chan-recv-count@ 1- chan chan-recv-count!
      else
        [: ." should not happen!" cr ;] ?raise
      then
    ;

    \ Actually peek a message on a channel for non-blocking
    : actual-peek-non-block { chan -- msg }
      chan chan-messages@ peek-queue if
        false chan chan-recv-pending!
      else
        [: ." should not happen!" cr ;] ?raise
      then
    ;

    \ Wait on sending a message on a channel
    : wait-send { msg chan -- }
      begin
        chan chan-send-wait@ block
        chan chan-messages@ queue-size chan chan-max-size@ < if
          msg chan actual-send true
        else
          false
        then
      until
    ;

    \ Wait on receiving a message from a channel
    : wait-recv { chan -- msg }
      begin
        chan chan-recv-wait@ block
        chan chan-messages@ queue-empty? not if
          chan actual-recv true
        else
          false
        then
      until
    ;

    \ Wait on peeking a message from a channel
    : wait-peek { chan -- msg }
      begin
        chan chan-recv-wait@ block
        chan chan-messages@ queue-empty? not if
          chan actual-peek true
        else
          false
        then
      until
    ;

  end-module> import

  \ Make a channel
  : make-chan { size -- chan }
    size make-queue make-queue make-queue 0 0 false false >chan
  ;

  \ Send a message on a channel, blocking until the channel's queue is not full
  \ if necessary
  : send { msg chan -- }
    chan chan-send-count@ 1+ chan chan-send-count!
    chan chan-messages@ queue-size chan chan-max-size@ < if
      chan chan-send-count@ 1 = if
        msg chan actual-send
      else
        chan wake-send msg chan wait-send
      then
    else
      msg chan wait-send
    then
  ;
  
  \ Send a message on a channel in a non-blocking fashion, returning whether
  \ sending was successful
  : send-non-block { msg chan -- success? }
    chan chan-messages@ queue-size chan chan-max-size@ < if
      chan chan-send-count@ 0= if
        msg chan actual-send-non-block true
      else
        chan wake-send false
      then
    else
      false
    then
  ;
  
  \ Receive a message from a channel, blocking until the channel's queue is not
  \ empty if necessary
  : recv { chan -- msg }
    chan chan-recv-count@ 1+ chan chan-recv-count!
    chan chan-messages@ queue-empty? not if
      chan chan-recv-count@ 1 = if
        chan actual-recv
      else
        chan wake-recv chan wait-recv
      then
    else
      chan wait-recv
    then
  ;

  \ Receive a message from a channel in a non-blocking fashion, returning
  \ whether receiving was successful
  : recv-non-block { chan -- msg success? }
    chan chan-messages@ queue-empty? not if
      chan chan-recv-count@ 0= if
        chan actual-recv-non-block true
      else
        chan wake-recv 0 false
      then
    else
      0 false
    then
  ;

  \ Receive a message from a channel, blocking until the channel's queue is not
  \ empty if necessary
  : peek { chan -- msg }
    chan chan-recv-count@ 1+ chan chan-recv-count!
    chan chan-messages@ queue-empty? not if
      chan chan-recv-count@ 1 = if
        chan actual-peek
      else
        chan wake-recv chan wait-peek
      then
    else
      chan wait-peek
    then
  ;

  \ Receive a message from a channel in a non-blocking fashion, returning
  \ whether receiving was successful
  : peek-non-block { chan -- msg success? }
    chan chan-messages@ queue-empty? not if
      chan chan-recv-count@ 0= if
        chan actual-peek-non-block true
      else
        chan wake-recv 0 false
      then
    else
      0 false
    then
  ;

end-module