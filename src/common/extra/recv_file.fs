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

begin-module zscript-recv-file

  zscript-oo import
  zscript-fs import
  zscript-crc32 import
  zscript-base64 import
  
  begin-module zscript-recv-file-internal

    \ The size of a received packet body
    512 constant packet-body-size

    \ Send packets done
    0 constant send-packet-done

    \ Sent the next packet
    1 constant send-packet-next

    \ Resent the current packet
    2 constant send-packet-resend
    
    \ Successfully received packet
    3 constant recv-packet-ack

    \ Unsuccessfully received packet
    4 constant recv-packet-nak

    \ Receive packets failed
    5 constant recv-packet-fail

    \ Send/receive Base64-encoded packet buffer size - do not change this
    700 constant base64-packet-buffer-size

    \ Send/receive non-Base64-encoded packet buffer size - do not change this
    packet-body-size 12 + constant packet-buffer-size

    \ Persistant state
    begin-record state

      \ Send/receive Base64-encoded packet buffer
      item: base64-packet-buffer

      \ Send/receive non-Base64-encoded packet buffer
      item: packet-buffer

      \ Packet body received length
      item: packet-body-len
    
      \ Packet body buffer
      item: packet-body

      \ The file being written to
      item: my-file

    end-record

    \ Initialize a state
    : init-state { file -- state }
      make-state { state }
      base64-packet-buffer-size make-bytes state base64-packet-buffer!
      packet-buffer-size make-bytes state packet-buffer!
      0 state packet-body-len!
      packet-body-size make-bytes state packet-body!
      file state my-file!
      state
    ;

    \ Receive data into a buffer
    : recv-data { state -- }
      base64-packet-buffer-size 0 ?do
        key i state base64-packet-buffer@ c!+
      loop
    ;
    
    \ Receive a packet
    : recv-packet { state -- valid? }
      state recv-data
      state base64-packet-buffer@ state packet-buffer@ decode-base64 nip
    ;

    \ Send a packet
    : send-packet { message state -- }
      message 0 state packet-buffer@ w!+
      0 1 cells state packet-buffer@ w!+
      0 2 cells state packet-buffer@ >slice generate-crc32
      2 cells state packet-buffer@ w!+
      0 3 cells state packet-buffer@ >slice
      state base64-packet-buffer@ encode-base64
      0 swap state base64-packet-buffer@ >slice type
    ;

    \ Handle a packet
    : handle-packet { state -- done? valid? }
      false true { done? valid? }
      state recv-packet
      0 packet-buffer-size cell - state packet-buffer@ >slice generate-crc32
      packet-buffer-size cell - state packet-buffer@ w@+ = and if
        cell state packet-buffer@ w@+ { new-packet-body-len }
        new-packet-body-len packet-body-size u> if
          recv-packet-fail state send-packet true false exit
          true to done?
          false to valid?
        else
          0 state packet-buffer@ w@+ case
            send-packet-done of
              0 state packet-body-len@ state packet-body@ >slice
              state my-file@ write-file drop
              true to done?
            endof
            send-packet-next of
              0 state packet-body-len@ state packet-body@ >slice
              state my-file@ write-file drop
              new-packet-body-len state packet-body-len!
              state packet-buffer@ 2 cells state packet-body@ 0
              state packet-body-len@ copy
            endof
            send-packet-resend of
              new-packet-body-len state packet-body-len!
              state packet-buffer@ 2 cells state packet-body@ 0
              state packet-body-len@ copy
            endof
            false to valid?
          endcase
        then
      else
        false to valid?
      then
      done? valid?
    ;

    \ Transfer data
    : transfer-data { state -- valid? }
      false { done? }
      begin done? not while
        state ['] handle-packet try { exception }
        exception 0= if
          swap to done?
          if
            recv-packet-ack state send-packet
          else
            recv-packet-nak state send-packet
          then
        else
          recv-packet-fail state send-packet
          exception ?raise
        then
      repeat
    ;

    \ Open or create a file
    : open-or-create-file { path -- file }
      path zscript-fs-tools::current-fs@ exists? if
        path zscript-fs-tools::current-fs@ open-file
      else
        path zscript-fs-tools::current-fs@ create-file
      then
    ;

  end-module> import

  \ Receive a file
  : recv-file ( path -- )
    open-or-create-file init-state { state }
    state transfer-data
    state my-file@ truncate-file
    state my-file@ fs@ flush
    state my-file@ close
  ;

end-module
