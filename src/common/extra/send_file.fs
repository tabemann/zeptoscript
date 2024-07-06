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

begin-module zscript-send-file

  zscript-oo import
  zscript-fs import
  zscript-crc32 import
  zscript-base64 import
  
  begin-module zscript-send-file-internal

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
      4 cells 0 ?do
        key i state base64-packet-buffer@ c!+
      loop
    ;
    
    \ Receive a packet
    : recv-packet { state -- valid? }
      state recv-data
      0 4 cells state base64-packet-buffer@ >slice
      state packet-buffer@ decode-base64 nip
    ;

    \ Send a packet
    : send-packet { resend? state -- }
      resend? if send-packet-resend else send-packet-next then
      0 state packet-buffer@ w!+
      state packet-body-len@ 1 cells state packet-buffer@ w!+
      2 cells packet-body-size over + swap ?do 0 i state packet-buffer@ c!+ loop
      state packet-body@ 0 state packet-buffer@ 2 cells
      state packet-body-len@ copy
      0 2 cells packet-body-size + state packet-buffer@ >slice generate-crc32
      2 cells packet-body-size + state packet-buffer@ w!+
      state packet-buffer@ state base64-packet-buffer@ encode-base64
      0 swap state base64-packet-buffer@ >slice type
    ;

    \ Send a done packet
    : send-done-packet { state -- }
      send-packet-done 0 state packet-buffer@ w!+
      0 1 cells state packet-buffer@ w!+
      2 cells packet-body-size over + swap ?do 0 i state packet-buffer@ c!+ loop
      0 2 cells packet-body-size + state packet-buffer@ >slice generate-crc32
      2 cells packet-body-size + state packet-buffer@ w!+
      state packet-buffer@ state base64-packet-buffer@ encode-base64
      0 swap state base64-packet-buffer@ >slice type
    ;

    \ Handle a packet
    : handle-packet { state -- error? valid? }
      state recv-packet
      0 2 cells state packet-buffer@ >slice generate-crc32
      2 cells state packet-buffer@ w@+ = and if
        0 state packet-buffer@ w@+ case
          recv-packet-ack of false true endof
          recv-packet-nak of false false endof
          recv-packet-fail of true false endof
          false false rot
        endcase
      else
        false false
      then
    ;

    \ Transfer data
    : transfer-data { state -- valid? }
      false { done? }
      state handle-packet 2drop
      begin done? not while
        state packet-body@ state my-file@ read-file state packet-body-len!
        state packet-body-len@ 0> if
          false { resend? }
          begin
            resend? state send-packet
            state handle-packet not to resend? to done?
            resend? not done? or
          until
        else
          true to done?
        then
      repeat
      begin
        state send-done-packet
        state handle-packet or
      until
    ;

  end-module> import

  \ Send a file
  : send-file ( path -- )
    zscript-fs-tools::current-fs@ open-file init-state { state }
    state transfer-data
    state my-file@ close
  ;

end-module
