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

begin-module fat32-test
  
  zscript-fs import
  zscript-block-dev import
  zscript-simple-fat32 import
  
  global my-fs
  
  : init-test
    2 3 4 5 0 make-simple-fat32-fs my-fs!
    true my-fs@ write-through!
  ;
  
  : list-dir { path -- }
    path my-fs@ open-dir { dir }
    begin
      dir read-dir if { entry }
        cr entry name@ type
      else
        drop
        dir close
        exit
      then
    again
  ;
  
  : insert-crlf { bytes -- bytes' }
    0 bytes [: $0A = if 1+ then ;] foldl { lf-count }
    bytes >len { len }
    len lf-count + make-bytes { bytes' }
    0 0 { index index' }
    begin
      index len index - bytes >slice [: $0A = ;] find-index if { lf-index }
        bytes index bytes' index' lf-index copy
        $0D index' lf-index + bytes' c!+
        $0A index' lf-index + 1+ bytes' c!+
        lf-index 1+ +to index
        lf-index 2 + +to index'
      else
        drop
        bytes index bytes' index' len index - copy
        bytes'
        exit
      then
    again
  ;
  
  : dump-file { path -- }
    path my-fs@ open-file { file }
    1024 make-bytes { data }
    begin data file read-file ?dup while
      0 swap data >slice insert-crlf type
    repeat
    file close
  ;
  
  : write-test { count path -- }
    false my-fs@ zscript-block-dev::write-through!
    path my-fs@ exists? if
      path my-fs@ open-file
    else
      path my-fs@ create-file
    then { my-file }
    cell make-bytes { data }
    count 0 ?do
      i 128 umod 0= if i . then
      i 0 data w!+
      data my-file write-file drop
    loop
    count .
    true my-fs@ zscript-block-dev::write-through!
    my-fs@ flush
    my-file close
  ;
  
  : read-test { path -- }
    path my-fs@ open-file { my-file }
    cell make-bytes { data }
    0 { index }
    begin
      data my-file read-file 0<> if
        0 data w@+ index = if
          index 128 umod 0= if index . then
          1 +to index
          false
        else
          ." NOT MATCHING: " index . 0 data w@+ .
          true
        then
      else
        index .
        true
      then
    until
    my-file close
  ;
  
  : hexdump-file { path -- }
    path my-fs@ open-file { my-file }
    512 make-bytes { full-data }
    0 { index }
    begin
      full-data my-file read-file { full-count }
      full-count 0> if
      
        begin
        
          index 512 umod dup 16 + full-count min over - 0 max full-data >slice { data }
          data >len { count }
          
          count 0> if
      
            cr index h.8 space
      
            0 begin dup count 4 min < while space dup data c@+ h.2 1+ repeat drop
            count 4 min 0 max begin dup 4 < while ."  --" 1+ repeat drop
      
            space
            4 begin dup count 8 min 4 max < while space dup data c@+ h.2 1+ repeat drop
            count 8 min 4 max begin dup 8 < while ."  --" 1+ repeat drop
            
            space
            8 begin dup count 12 min 8 max < while space dup data c@+ h.2 1+ repeat drop
            count 12 min 8 max begin dup 12 < while ."  --" 1+ repeat drop
            
            space
            12 begin dup count 16 min 12 max < while space dup data c@+ h.2 1+ repeat drop
            count 16 min 12 max begin dup 16 < while ."  --" 1+ repeat drop
      
            space space
            ." |"
            data count 16 <> if 0 count rot >slice then
            [: dup $20 < over $7E > or if drop [char] . then emit ;] iter
            0 begin dup 16 count - < while ." ." 1+ repeat drop
            ." |"
      
            16 +to index
            
            index 512 umod 0=
            
          else
            true
          then
            
        until
        false
      else
        true
      then
    until
    my-file close
  ;
  
end-module
