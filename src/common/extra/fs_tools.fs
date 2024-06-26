\ Copyright (c) 2022-2024 Travis Bemann
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

begin-module zscript-fs-tools

  zscript-oo import
  zscript-special-oo import
  zscript-fs import
  zscript-block-dev import
  zscript-rtc import
  
  \ Filesystem not set exception
  : x-fs-not-set ( -- ) ." filesystem not set" cr ;
  
  begin-module zscript-fs-tools-internal

    2 0 foreign forth::feed-input feed-input
  
    \ Include input buffer size
    256 constant include-buffer-size

    \ Read buffer size
    128 constant read-buffer-size
    
    \ Include frame
    begin-record include-frame
      
      \ Frame file
      item: frame-file

      \ End of file condition
      item: frame-eof
      
      \ Frame offset
      item: frame-offset

      \ Next frame
      item: frame-next
      
    end-record
    
    \ The current filesystem
    global current-fs

    \ The include stack
    global include-stack
    
    \ Include input buffer content length
    global include-buffer-content-len
    
    \ Include input buffer
    include-buffer-size foreign-buffer: include-buffer
    
    \ Get the next include stack frame
    : include-stack-next@ ( -- frame )
      include-stack@ frame-next@
    ;

    \ Read code from a file
    : read-file-into-buffer ( -- )
      include-buffer-size include-buffer-content-len@ - make-bytes { data }
      data include-stack@ frame-file@ read-file { count }
      data unsafe::bytes>addr-len drop 0
      include-buffer include-buffer-content-len@ count unsafe::move-offset
      include-buffer-content-len@ count + include-buffer-content-len!
    ;
    
    \ Get the executable line length
    : execute-line-len ( -- bytes )
      include-buffer-content-len@ 0 ?do
        include-buffer i + unsafe::c@ dup $0A = swap $0D = or if
          i 1+ exit
        then
      loop
      include-buffer-content-len@
    ;
    
    \ Update the EOF and get the input length
    : update-line ( -- u )
      execute-line-len dup include-stack@ frame-offset@ +
      include-stack@ frame-file@ file-size@ =
      include-stack@ frame-eof!
      dup dup 0> if
        1- include-buffer + unsafe::c@ dup $0A = swap $0D = or if 1- then
      then
    ;

    \ Refill file
    : frame-eval-refill ( -- )
      execute-line-len dup include-stack@ frame-offset@ +
      include-stack@ frame-offset!
      dup negate include-buffer-content-len@ +
      include-buffer-content-len!
      include-buffer swap include-buffer 0 include-buffer-content-len@
      unsafe::move-offset
      read-file-into-buffer
      include-buffer update-line feed-input
    ;
    
    \ Check end of file condition
    : frame-eval-eof ( -- eof? ) include-stack@ frame-eof@ ;
      
    \ Un-nest an include
    : unnest-include ( -- )
      include-stack@ frame-file@ close
      include-stack-next@ include-stack!
      include-stack@ 0<> if
        include-stack@ frame-offset@ seek-set
        include-stack@ frame-file@ seek-file
        0 include-buffer-content-len!
        read-file-into-buffer
      then 
    ;
    
    \ Execute an included file
    : execute-file ( -- )
      [:
        0 include-buffer-content-len!
        read-file-into-buffer
        include-buffer-content-len@ 0> if
          0 include-buffer update-line ['] frame-eval-refill ['] frame-eval-eof
          evaluate-with-input
        then
      ;] try
      unnest-include
      ?raise
    ;

    \ List a directory with file sizes
    : list-dir ( dir -- )
      cr ." filename     creation date             modification date        "
      ."  file size"
      cr ." ------------ ------------------------- -------------------------"
      ."  ----------"
      begin
        dup read-dir if { entry }
          cr entry name@ dup >len { len } type
          entry entry-dir? if ." /" 1 +to len then
          13 len - spaces
          entry create-date-time@ show type space
          entry modify-date-time@ show type space
          entry entry-file? if
            entry entry-file-size@ format-integral dup >len { size-len }
            10 size-len - spaces type
          then
          false
        else
          drop true
        then
      until
    ;

    \ Dump a file
    : dump-file { file -- }
      512 make-bytes { full-data }
      0 { index }
      file tell-file { start-offset }
      begin
        full-data file read-file { full-count }
        full-count 0> if
          
          begin
            
            index 512 umod dup 16 + full-count min over - 0 max full-data
            >slice { data }
            data >len { count }
            
            count 0> if
              
              cr start-offset index + h.8 space
              
              0 begin
                dup count 4 min < while space dup data c@+ h.2 1+
              repeat drop
              count 4 min 0 max begin dup 4 < while ."  --" 1+ repeat drop
              
              space
              4 begin
                dup count 8 min 4 max < while space dup data c@+ h.2 1+
              repeat drop
              count 8 min 4 max begin dup 8 < while ."  --" 1+ repeat drop
              
              space
              8 begin
                dup count 12 min 8 max < while space dup data c@+ h.2 1+
              repeat drop
              count 12 min 8 max begin dup 12 < while ."  --" 1+ repeat drop
              
              space
              12 begin
                dup count 16 min 12 max < while space dup data c@+ h.2 1+
              repeat drop
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
    ;

    \ Dump a file to a specified length
    : dump-file-length { length file -- }
      512 make-bytes { full-data }
      0 { index }
      file tell-file { start-offset }
      begin
        full-data file read-file length min { full-count }
        full-count negate +to length
        full-count 0> if
          
          begin
            
            index 512 umod dup 16 + full-count min over - 0 max full-data
            >slice { data }
            data >len { count }
            
            count 0> if
              
              cr start-offset index + h.8 space
              
              0 begin
                dup count 4 min < while space dup data c@+ h.2 1+
              repeat drop
              count 4 min 0 max begin dup 4 < while ."  --" 1+ repeat drop
              
              space
              4 begin
                dup count 8 min 4 max < while space dup data c@+ h.2 1+
              repeat drop
              count 8 min 4 max begin dup 8 < while ."  --" 1+ repeat drop
              
              space
              8 begin
                dup count 12 min 8 max < while space dup data c@+ h.2 1+
              repeat drop
              count 12 min 8 max begin dup 12 < while ."  --" 1+ repeat drop
              
              space
              12 begin
                dup count 16 min 12 max < while space dup data c@+ h.2 1+
              repeat drop
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
    ;

    \ Dump a file as ASCII
    : dump-file-ascii { file -- }
      512 make-bytes { full-data }
      0 { index }
      file tell-file { start-offset }
      begin
        full-data file read-file { full-count }
        full-count 0> if
          
          begin
            
            index 512 umod dup 64 + full-count min over - 0 max full-data
            >slice { data }
            data >len { count }
            
            count 0> if
              
              cr start-offset index + h.8 space space

              ." |"
              data count 64 <> if 0 count rot >slice then
              [: dup $20 < over $7E > or if drop [char] . then emit ;] iter
              0 begin dup 64 count - < while ." ." 1+ repeat drop
              ." |"
              
              64 +to index
              
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
    ;

    \ Dump a file as ASCII to a specified length
    : dump-file-ascii-length { length file -- }
      512 make-bytes { full-data }
      0 { index }
      file tell-file { start-offset }
      begin
        full-data file read-file length min { full-count }
        full-count negate +to length
        full-count 0> if
          
          begin
            
            index 512 umod dup 64 + full-count min over - 0 max full-data
            >slice { data }
            data >len { count }
            
            count 0> if
              
              cr start-offset index + h.8 space space

              ." |"
              data count 64 <> if 0 count rot >slice then
              [: dup $20 < over $7E > or if drop [char] . then emit ;] iter
              0 begin dup 64 count - < while ." ." 1+ repeat drop
              ." |"
              
              64 +to index
              
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
    ;

    \ Initialize filesystem tools
    : init-fs-tools ( -- )
      0 current-fs!
      0 include-stack!
      0 include-buffer-content-len!
    ;
  
  end-module> import
  
  \ Set the current filesystem
  : current-fs! ( fs -- ) current-fs! ;
  
  \ Get the current filesystem
  : current-fs@ ( -- fs ) current-fs@ ;
  
  \ Load a file
  : load-file ( file -- )
    current-fs@ averts x-fs-not-set
    dup false over tell-file include-stack@ >include-frame include-stack!
    execute-file
  ;
  
  \ Include a file
  : included ( path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ open-file false 0 include-stack@ >include-frame
    include-stack!
    execute-file
  ;
  
  \ Include a file
  : include ( "path" -- )
    token dup 0<> averts x-token-expected included
  ;

  \ List a directory
  : list-dir ( path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ open-dir list-dir
  ;

  \ Create a directory
  : create-dir ( path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ create-dir drop
    current-fs@ flush
  ;

  \ Copy a file
  : copy-file { path new-path -- }
    current-fs@ averts x-fs-not-set
    path current-fs@ open-file { file }
    new-path current-fs@ exists? if
      new-path current-fs@ open-file
    else
      new-path current-fs@ create-file
    then { new-file }
    512 make-bytes { data }
    begin
      data file read-file { count }
      count 0> if
        0 count data >slice new-file write-file false
      else
        true
      then
    until
    new-file truncate-file
    file close
    new-file close
    current-fs@ flush
  ;

  \ Append to a file
  : append-file ( data path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ open-file { file }
    0 seek-end file seek-file
    file write-file drop
    file close
    current-fs@ flush
  ;

  \ Write data at an offset in a file without truncating it
  : write-file-window { data offset path -- }
    current-fs@ averts x-fs-not-set
    path current-fs@ exists? if
      path current-fs@ open-file
    else
      path current-fs@ create-file
    then { file }
    offset seek-set file seek-file
    data file write-file drop
    file close
    current-fs@ flush
  ;

  \ Overwrite a file and then truncate it afterwards
  : write-file { data path -- }
    current-fs@ averts x-fs-not-set
    path current-fs@ exists? if
      path current-fs@ open-file
    else
      path current-fs@ create-file
    then { file }
    data file write-file drop
    file truncate-file
    file close
    current-fs@ flush
  ;

  \ Create a file
  : create-file ( data path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ create-file tuck write-file drop close
    current-fs@ flush
  ;

  \ Dump the contents of a file to the console as raw data
  : dump-file-raw ( path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ open-file { file }
    512 make-bytes { data }
    begin
      data file read-file { count }
      count 0> if
        0 count data >slice type false
      else
        true
      then
    until
    file close
  ;
  
  \ Dump the contents of a window in a file to the console as raw data
  : dump-file-raw-window { offset length path -- }
    current-fs@ averts x-fs-not-set
    path current-fs@ open-file { file }
    offset seek-set file seek-file
    512 make-bytes { data }
    begin
      0 length 512 min data >slice file read-file { count }
      count 0> if
        0 count data >slice type
        count negate +to length
        false
      else
        true
      then
    until
    file close
  ;

  \ Dump the contents of a file to the console as bytes plus ASCII
  : dump-file ( path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ open-file { file }
    file dump-file
    file close
  ;

  \ Dump the contents of a window in a file to the console as bytes plus ASCII
  : dump-file-window ( offset length path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ open-file { file }
    swap seek-set file seek-file
    file dump-file-length
    file close
  ;

  \ Dump the contents of a file to the console as ASCII
  : dump-file-ascii ( path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ open-file { file }
    file dump-file-ascii
    file close
  ;
  
  \ Dump the contents of a window in a file to the console as ASCII
  : dump-file-ascii-window ( offset length path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ open-file { file }
    swap seek-set file seek-file
    file dump-file-ascii-length
    file close
  ;

  \ Read a file, from an offset from the start of the file, to a fixed-sized
  \ buffer and return the length actually read
  : read-file ( buffer offset path -- count )
    current-fs@ averts x-fs-not-set
    current-fs@ open-file { file }
    seek-set file seek-file
    file read-file
    file close
  ;

  \ Get the size of a file
  : file-size@ ( path -- size )
    current-fs@ averts x-fs-not-set
    current-fs@ open-file { file }
    file file-size@
    file close
  ;
  
  \ Remove a file
  : remove-file ( path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ remove-file
    current-fs@ flush
  ;

  \ Remove a directory
  : remove-dir ( path -- )
    current-fs@ averts x-fs-not-set
    current-fs@ remove-dir
    current-fs@ flush
  ;

  \ Rename a file
  : rename ( path new-name -- )
    current-fs@ averts x-fs-not-set
    swap current-fs@ rename
    current-fs@ flush
  ;

  \ Get whether a file or directory at a given path exists
  : exists? ( path -- exists? )
    current-fs@ averts x-fs-not-set
    current-fs@ exists?
  ;

  \ Get whether a directory entry is a file
  : file? ( path -- file? )
    current-fs@ averts x-fs-not-set
    current-fs@ file?
  ;

  \ Get whether a directory entry is a directory
  : dir? ( path -- dir? )
    current-fs@ averts x-fs-not-set
    current-fs@ dir?
  ;

end-module

initializer zscript-fs-tools::zscript-fs-tools-internal::init-fs-tools

