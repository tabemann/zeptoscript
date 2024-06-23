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

begin-module zscript-fat32

  zscript-oo import
  zscript-special-oo import
  zscript-rtc import
  zscript-block-dev import
  zscript-fs import
  zscript-map import
  zscript-task import
  
  \ Sector size exception
  : x-sector-size-not-supported ( -- )
    ." sector sizes other than 512 are not supported" cr
  ;
  
  \ Filesystem version not supported exception
  : x-fs-version-not-supported ( -- ) ." FAT32 version not supported" cr ;
  
  \ Bad info sector exception
  : x-bad-info-sector ( -- ) ." bad info sector" cr ;
  
  \ No clusters free exception
  : x-no-clusters-free ( -- ) ." no clusters free" cr ;
  
  \ Unsupported file name format exception
  : x-file-name-format ( -- ) ." unsupported filename" cr ;
  
  \ Out of range directory entry index exception
  : x-out-of-range-entry ( -- ) ." out of range directory entry" cr ;
  
  \ Out of range partition index exception
  : x-out-of-range-partition ( -- ) ." out of range partition" cr ;

  \ Directory with no end marker exception
  : x-no-end-marker ( -- ) ." no end marker" cr ;
  
  begin-module zscript-fat32-internal
    
    \ Sector size
    512 constant sector-size

    \ Directory entry size
    32 constant entry-size

    \ Default open map size
    8 constant default-open-map-size

    \ Read an unaligned halfword
    : unaligned-h@+ { index bytes -- h }
      index bytes c@+ index 1+ bytes c@+ 8 lshift or
    ;
    
    \ Is a cluster free?
    : free-cluster? ( cluster-link -- free? ) $0FFFFFFF and 0= ;
    
    \ Is a cluster an end cluster?
    : end-cluster? ( cluster-link -- end? ) $0FFFFFF8 and $0FFFFFF8 = ;
    
    \ Is cluster a linked cluster?
    : link-cluster? ( cluster-link -- link? )
      $0FFFFFFF and dup $00000002 >= swap $0FFFFFEF <= and
    ;
    
    \ Get the link of a linked cluster
    : cluster-link ( cluster-link -- cluster ) $0FFFFFFF and ;
    
    \ Free cluster marker
    $00000000 constant free-cluster-mark
    
    \ End cluster marker
    $0FFFFFF8 constant end-cluster-mark

    \ Strip the spaces from the end of a string
    : strip-end-spaces { bytes -- bytes' }
      bytes >len { len }
      0 { count }
      begin count len < while
        count bytes c@+ $20 <> if
          1 +to count
        else
          0 count bytes >slice exit
        then
      repeat
      bytes
    ;

    \ Write a string into another string, limited by its size, and return the
    \ remaining buffer
    : >string { bytes0 bytes1 -- bytes' }
      bytes0 >len bytes1 >len dup { len1 } min { len }
      bytes0 0 bytes1 0 len copy
      len len len1 - 0 max bytes1 >slice
    ;

    \ Find the index of a dot in a string
    : dot-index { bytes -- index | -1 }
      bytes [: [char] . = ;] find-index not if drop -1 then
    ;

    \ Get the dot count in a string
    : dot-count { bytes -- count }
      0 bytes [: [char] . = if 1+ then ;] foldl
    ;

    \ Validate a filename character
    : validate-file-name-char ( c -- )
      s\" \"*/:<>?\\|" swap 1 ['] = bind find-index triggers x-file-name-format
      drop
    ;

    \ Validate filename characters
    : validate-file-name-chars { bytes -- }
      bytes ['] validate-file-name-char iter
    ;

    \ Convert a character to uppercase
    : upcase-char ( c -- c' )
      dup [char] a >= over [char] z <= and if
        [char] a - [char] A +
      then
    ;

    \ Convert a string to uppercase
    : upcase-bytes ( bytes -- )
      ['] upcase-char map
    ;

    \ Validate a filename
    : validate-file-name { bytes -- }
      bytes validate-file-name-chars
      bytes >len { len }
      len 12 <= averts x-file-name-format
      bytes dot-count 1 = averts x-file-name-format
      bytes dot-index { index }
      index 0> averts x-file-name-format
      index 8 <= averts x-file-name-format
      len index - 4 <= averts x-file-name-format
      len index - 1 > averts x-file-name-format
    ;

    \ Validate a directory name
    : validate-dir-name { bytes -- }
      bytes validate-file-name-chars
      bytes s" ." equal? if exit then
      bytes s" .." equal? if exit then
      bytes dot-count 0= averts x-file-name-format
      bytes >len { len }
      len 8 <= averts x-file-name-format
      len 0> averts x-file-name-format
    ;

    \ Validate a filename or directory name
    : validate-name { bytes -- }
      bytes validate-file-name-chars
      bytes s" ." equal? if exit then
      bytes s" .." equal? if exit then
      bytes dot-count dup 1 = if
        drop
        bytes >len { len }
        len 12 <= averts x-file-name-format
        bytes dot-index { index }
        index 0> averts x-file-name-format
        index 8 <= averts x-file-name-format
        len index - 4 <= averts x-file-name-format
        len index - 1 > averts x-file-name-format
      else
        0= averts x-file-name-format
        bytes >len { len }
        len 8 <= averts x-file-name-format
        len 0> averts x-file-name-format
      then
    ;

    \ Copy to a buffer, padded with spaces
    : copy-space-pad { bytes0 bytes1 -- }
      bytes0 >len bytes1 >len dup { len1 } min { len }
      bytes0 0 bytes1 0 len copy
      len1 len ?do $20 i bytes1 c@+ loop
    ;

    \ Is name a directory name
    : dir-name? ( bytes -- dir-name? )
      s" ." over equal? if
        drop true
      else
        s" .." over equal? if
          drop true
        else
          dot-index -1 =
        then
      then
    ;

    \ Check whether a directory name is forbidden
    : forbidden-dir? ( bytes -- forbidden? )
      s" ." over equal? if
        drop true
      else
        s" .." swap equal?
      then
    ;

    \ Find a path separator
    : find-path-separator ( bytes -- index | -1 )
      [: [char] / = ;] find-index not if drop -1 then
    ;

    \ Strip a final path separator
    : strip-final-path-separator { bytes -- }
      bytes >len { len }
      len { index }
      begin index 0> while
        index 1- bytes c@+ [char] / <> if
          index len  = if bytes else 0 index bytes >slice then exit
        else
          -1 to index
        then
      repeat
      0bytes
    ;

    \ Clean path segments
    : clean-path { parts -- parts' }
      parts >len { len }
      0 parts [: >len 0> if 1+ then ;] foldl { count }
      count make-cells { parts' }
      0 0 { index index' }
      begin index len < while
        index parts @+ dup >len 0<> if
          index' parts' !+
          1 +to index'
        else
          drop
        then
        1 +to index
      repeat
      parts'
    ;

    \ Split a path into parts
    : segment-path { path -- parts }
      path >len { len }
      0 path [: [char] / = if 1+ then ;] foldl { count }
      count 1+ make-cells { parts }
      0 0 { index part-index }
      path { remainder }
      0 path c@+ [char] / = if
        s" /" 0 parts !+
        1 len 1- remainder >slice to remainder
        1 to index
        1 to part-index
      then
      begin part-index count < while
        remainder [: [char] / = ;] find-index drop
        0 over remainder >slice part-index parts !+
        1+ remainder >len over - remainder >slice to remainder
        1 +to part-index
      repeat
      remainder part-index parts !+
      parts clean-path
    ;

    \ Validate a file path
    : validate-file-path { parts -- }
      parts >len { len }
      len 0> averts x-empty-path
      0 parts @+ dup s" /" equal? not if
        validate-file-name
      else
        len 1 > averts x-invalid-path
        drop
      then
      len 2 > if
        len 1- 1 ?do i parts @+ validate-dir-name loop
      then
      len 1- parts @+ validate-file-name
    ;

    \ Validate a directory path
    : validate-dir-path { parts -- }
      parts >len { len }
      len 0> averts x-empty-path
      0 parts @+ dup s" /" equal? not if
        validate-dir-name
      else
        len 1 > averts x-invalid-path
        drop
      then
      len 1 > if
        len 1 ?do i parts @+ validate-dir-name loop
      then
    ;

    \ Validate a file or directory path
    : validate-path { parts -- }
      parts >len { len }
      len 0> averts x-empty-path
      0 parts @+ dup s" /" equal? not if
        len 1 > if validate-dir-name else validate-name then
      else
        len 1 > averts x-invalid-path
        drop
      then
      len 2 > if
        len 1- 1 ?do i parts @+ validate-dir-name loop
      then
      len 1- parts @+ validate-name
    ;
    
  end-module> import

  \ Is the partition really active?
  method partition-active? ( self -- active? )

  \ Is the partition active
  method partition-active@ ( self -- active )

  \ Get the partition type
  method partition-type@ ( self -- type )

  \ Get the partition first sector
  method partition-first-sector@ ( self -- first-sector )

  \ Get the partition sector count
  method partition-sectors@ ( self -- sectors )

  \ Partition class
  begin-class partition

    member: my-partition-active
    member: my-partition-type
    member: my-partition-first-sector
    member: my-partition-sectors

    \ Constructor
    :method new { active type first-sector sectors self -- }
      active self my-partition-active!
      type self my-partition-type!
      first-sector self my-partition-first-sector!
      sectors self my-partition-sectors!
    ;

    \ Is the partition really active?
    :method partition-active? ( self -- active? )
      my-partition-active@ $80 >=
    ;

    \ Is the partition active
    :method partition-active@ ( self -- active )
      my-partition-active@
    ;

    \ Get the partition type
    :method partition-type@ ( self -- type )
      my-partition-type@
    ;

    \ Get the partition first sector
    :method partition-first-sector@ ( self -- first-sector )
      my-partition-first-sector@
    ;

    \ Get the partition sector count
    :method partition-sectors@ ( self -- sectors )
      my-partition-sectors@
    ;
    
  end-class
  
  \ Get whether the master boot record is valid
  method mbr-valid? ( self -- valid? )
  
  \ Read a partition
  method partition@ ( index self -- partition )
  
  \ Write a partition
  method partition! ( partition index self -- )

  continue-module zscript-fat32-internal

    \ Copy a buffer into an entry
    method buffer>entry ( bytes entry -- )

    \ Copy an entry into a buffer
    method entry>buffer ( bytes entry -- )

    \ Initialize a blank entry
    method init-blank-entry ( entry -- )

    \ Initialize a file entry
    method init-file-entry ( file-size first-cluster name entry -- )

    \ Initialize a directory entry
    method init-dir-entry ( first-cluster name entry -- )

    \ Initialize an end entry
    method init-end-entry ( entry -- )

    \ Mark an entry as deleted
    method mark-entry-deleted ( entry -- )

    \ Get whether an entry is deleted
    method entry-deleted? ( entry -- deleted? )

    \ Get whether an entry is an end entry
    method entry-end? ( entry -- end? )

    \ Get the first cluster of an entry
    method first-cluster@ ( entry -- cluster )

    \ Set the first cluster of an entry
    method first-cluster! ( cluster entry -- )

    \ Set an entry's file name
    method file-name! ( name entry -- )

    \ Set an entry's directory name
    method dir-name! ( name entry -- )

    \ Set an entry's create date and time
    method create-date-time! ( date-time entry -- )

    \ Set an entry's modification date and time
    method modify-date-time! ( date-time entry -- )

    \ Set an entry's file size
    method entry-file-size! ( size entry -- )
    
    \ Read the FAT
    method fat@ ( cluster fat fs -- link )
  
    \ Allocate a cluster
    method allocate-cluster ( fs -- cluster )

    \ Allocate and link a cluster
    method allocate-link-cluster ( cluster fs -- cluster' )

    \ Free a cluster chain
    method free-cluster-chain ( cluster fs -- )

    \ Free a cluster tail
    method free-cluster-tail ( cluster fs -- )

    \ Get the number of sectors per cluster
    method cluster-sectors@ ( self -- sectors )

    \ Cluster offset to sector
    method cluster-offset>sector ( offset cluster self -- sector )

    \ Directory cluster entry count
    method dir-cluster-entry-count@ ( fs -- )

    \ Find an entry
    method find-entry ( index cluster fs -- index' cluster' | -1 -1 )

    \ Read an entry
    method entry@ ( index cluster fs -- entry )

    \ Write an entry
    method entry! ( entry index cluster fs -- entry )

    \ Look up an entry
    method lookup-entry ( name cluster fs -- index cluster )

    \ Get whether an entry exists
    method entry-exists? ( name cluster fs -- exists? )

    \ Expand a directory
    method expand-dir ( index cluster fs -- )

    \ Allocate an entry
    method allocate-entry ( cluster fs -- index cluster )

    \ Update entry date and time
    method update-entry-date-time ( index cluster fs -- )

    \ Get filesystem device
    method fat32-device@ ( fs -- device )

    \ Register an open file or directory
    method register-open ( cluster fs -- )

    \ Unregister an open file or directory
    method unregister-open ( cluster fs -- )

    \ Get the number of open references to a file or directory
    method open-count@ ( cluster fs -- count )

    \ Actually create a file
    method do-create-file ( name parent-dir file -- )

    \ Actually open a file
    method do-open-file ( name parent-dir file -- )

    \ Actually create a directory
    method do-create-dir ( name parent-dir dir -- )

    \ Actually open a directory
    method do-open-dir ( name parent-dir dir -- )

    \ Get directory first cluster
    method dir-first-cluster@ ( dir -- cluster )

    \ Get the root directory
    method do-root-dir ( cluster dir -- )

  end-module
  
  \ Master boot record class
  begin-class mbr

    member: my-mbr-device

    \ Constructor
    :method new { mbr-device self -- }
      mbr-device self my-mbr-device!
    ;

    \ Get whether the master boot record is valid
    :method mbr-valid? { self -- valid? }
      sector-size make-bytes dup { data } 0 self my-mbr-device@ block@
      $1FE data h@+ $AAFF =
    ;

    \ Read a partition
    :method partition@ { index self -- partition }
      index 0>= index 4 < and averts x-out-of-range-partition
      $10 make-bytes dup { data } $1BE $10 index * + 0
      self my-mbr-device@ block-part@
      $00 data c@+ $04 data c@+ $08 data w@+ $0C data w@+ make-partition
    ;

    \ Write a partition
    :method partition! { partition index self -- }
      index 0>= index 4 < and averts x-out-of-range-partition
      $10 make-bytes { data }
      partition partition-active@ $00 data c!+
      partition partition-type@ $04 data c!+
      partition partition-first-sector@ $08 data w!+
      partition partition-sectors@ $0C data w!+
      data $10 $1BE $10 index * + 0 self my-mbr-device@ block-part!
    ;
    
  end-class

  \ FAT32 directory entry class
  begin-class fat32-entry
    
    member: my-short-file-name
    member: my-short-file-ext
    member: my-file-attributes
    member: my-nt-vfat-case
    member: my-create-time-fine
    member: my-create-time-coarse
    member: my-create-date
    member: my-access-date
    member: my-first-cluster-high
    member: my-modify-time-coarse
    member: my-modify-date
    member: my-first-cluster-low
    member: my-entry-file-size

    \ Copy a buffer into an entry
    :method buffer>entry { bytes self -- }
      0 8 bytes >slice duplicate self my-short-file-name!
      8 3 bytes >slice duplicate self my-short-file-ext!
      11 bytes c@+ self my-file-attributes!
      12 bytes c@+ self my-nt-vfat-case!
      13 bytes c@+ self my-create-time-fine!
      14 bytes h@+ self my-create-time-coarse!
      16 bytes h@+ self my-create-date!
      18 bytes h@+ self my-access-date!
      20 bytes h@+ self my-first-cluster-high!
      22 bytes h@+ self my-modify-time-coarse!
      24 bytes h@+ self my-modify-date!
      26 bytes h@+ self my-first-cluster-low!
      28 bytes w@+ self my-entry-file-size!
    ;

    \ Copy an entry into a buffer
    :method entry>buffer { bytes self -- }
      self my-short-file-name@ 0 bytes 0 8 copy
      self my-short-file-ext@ 0 bytes 8 3 copy
      self my-file-attributes@ 11 bytes c!+
      self my-nt-vfat-case@ 12 bytes c!+
      self my-create-time-fine@ 13 bytes c!+
      self my-create-time-coarse@ 14 bytes h!+
      self my-create-date@ 16 bytes h!+
      self my-access-date@ 18 bytes h!+
      self my-first-cluster-high@ 20 bytes h!+
      self my-modify-time-coarse@ 22 bytes h!+
      self my-modify-date@ 24 bytes h!+
      self my-first-cluster-low@ 26 bytes h!+
      self my-entry-file-size@ 28 bytes w!+
    ;

    \ Initialize a blank entry
    :method init-blank-entry { self -- }
      8 make-bytes self my-short-file-name!
      3 make-bytes self my-short-file-ext!
      $E5 0 self my-short-file-name@ c!+
      8 1 ?do $20 i self my-short-file-name@ c!+ loop
      3 0 ?do $20 i self my-short-file-ext@ c!+ loop
      0 self my-file-attributes!
      0 self first-cluster!
      0 self my-entry-file-size!
      0 self my-nt-vfat-case!
      0 self my-create-time-fine!
      0 self my-create-time-coarse!
      0 9 lshift 1 5 lshift or 1 0 lshift or self my-create-date!
      0 9 lshift 1 5 lshift or 1 0 lshift or self my-access-date!
      0 self my-modify-time-coarse!
      0 9 lshift 1 5 lshift or 1 0 lshift or self my-modify-date!
    ;

    \ Initialize a file entry
    :method init-file-entry { file-size first-cluster name self -- }
      8 make-bytes self my-short-file-name!
      3 make-bytes self my-short-file-ext!
      name self file-name!
      first-cluster self first-cluster!
      file-size self my-entry-file-size!
      0 self my-file-attributes!
      0 self my-nt-vfat-case!
      0 9 lshift 1 5 lshift or 1 0 lshift or self my-access-date!
      make-date-time { date-time }
      date-time self create-date-time!
      date-time self modify-date-time!
    ;

    \ Initialize a directory entry
    :method init-dir-entry { first-cluster name self -- }
      8 make-bytes self my-short-file-name!
      3 make-bytes self my-short-file-ext!
      name self dir-name!
      first-cluster self first-cluster!
      0 self my-entry-file-size!
      $10 self my-file-attributes!
      0 self my-nt-vfat-case!
      0 9 lshift 1 5 lshift or 1 0 lshift or self my-access-date!
      make-date-time { date-time }
      date-time self create-date-time!
      date-time self modify-date-time!
    ;

    \ Initialize an end entry
    :method init-end-entry { self -- }
      8 make-bytes self my-short-file-name!
      3 make-bytes self my-short-file-ext!
      0 self my-file-attributes!
      0 self my-nt-vfat-case!
      0 self my-create-time-fine!
      0 self my-create-time-coarse!
      0 self my-create-date!
      0 self my-access-date!
      0 self my-first-cluster-high!
      0 self my-modify-time-coarse!
      0 self my-modify-date!
      0 self my-first-cluster-low!
      0 self my-entry-file-size!
    ;

    \ Mark an entry as deleted
    :method mark-entry-deleted ( self -- )
      $E5 0 rot my-short-file-name@ c!+
    ;

    \ Get whether an entry is deleted
    :method entry-deleted? ( self -- deleted? )
      0 swap my-short-file-name@ c@+ $E5 =
    ;

    \ Get whether an entry is an end entry
    :method entry-end? ( self -- end? )
      0 swap my-short-file-name@ c@+ $00 =
    ;

    \ Get whether an entry is a file
    :method entry-file? ( self -- file? )
      my-file-attributes@ $58 and 0=
    ;

    \ Get whether an entry is a directory
    :method entry-dir? ( self -- dir? )
      my-file-attributes@ $10 and 0<>
    ;

    \ Get the first cluster of an entry
    :method first-cluster@ ( self -- cluster )
      dup my-first-cluster-low@ swap my-first-cluster-high@ 16 lshift or
    ;

    \ Set the first cluster of an entry
    :method first-cluster! { cluster self -- }
      cluster $FFFF and self my-first-cluster-low!
      cluster 16 rshift self my-first-cluster-high!
    ;

    \ Set an entry's file name
    :method file-name! { name self -- }
      name validate-file-name
      name upcase-bytes to name
      0 name c@+ $E5 = if $05 0 name c!+ then
      8 0 ?do $20 i self my-short-file-name@ c!+ loop
      3 0 ?do $20 i self my-short-file-ext@ c!+ loop
      name dot-index { index }
      name 0 self my-short-file-name@ 0 index copy
      name index 1+ self my-short-file-ext@ 0 name >len index - 1- copy
    ;

    \ Set an entry's directory name
    :method dir-name! { name self -- }
      name validate-dir-name
      name upcase-bytes to name
      0 name c@+ $E5 = if $05 0 name c!+ then
      8 0 ?do $20 i self my-short-file-name@ c!+ loop
      3 0 ?do $20 i self my-short-file-ext@ c!+ loop
      name 0 self my-short-file-name@ 0 name >len copy
    ;

    \ Get an entry's file or directory name
    :method name@ { self -- name }
      self entry-file? if
        self my-short-file-name@ strip-end-spaces
        self my-short-file-ext@ strip-end-spaces
        >pair s" ." join
      else
        self entry-dir? if
          self my-short-file-name@ strip-end-spaces
        then
      then
      duplicate { name }
      0 name c@+ $05 = if $E5 0 name c!+ then
      name
    ;

    \ Set an entry's creation date and time
    :method create-date-time! { date-time self -- }
      date-time date-time-year@ 1980 < if
        0 9 lshift 1 5 lshift or 1 or self my-create-date!
      else
        date-time date-time-year@ 1980 - $7F and 9 lshift
        date-time date-time-month@ 5 lshift or
        date-time date-time-day@ or self my-create-date!
      then
      date-time date-time-hour@ 11 lshift
      date-time date-time-minute@ 5 lshift or
      date-time date-time-second@ 2 / or self my-create-time-coarse!
      date-time date-time-second@ 2 umod 100 * self my-create-time-fine!
    ;

    \ Get an entry's creation date and time
    :method create-date-time@ { self -- date-time }
      make-date-time { date-time }
      self my-create-date@ { date-field }
      date-field 9 rshift 1980 + date-time date-time-year!
      date-field 5 rshift $F and date-time date-time-month!
      date-field $1F and date-time date-time-day!
      self my-create-time-coarse@ { time-field }
      time-field 11 rshift date-time date-time-hour!
      time-field 5 rshift $3F and date-time date-time-minute!
      time-field $1F and 2 *
      self my-create-time-fine@ 100 / + date-time date-time-second!
      date-time update-dotw
      date-time
    ;

    \ Set an entry's modification date and time
    :method modify-date-time! { date-time self -- }
      date-time date-time-year@ 1980 < if
        0 9 lshift 1 5 lshift or 1 or self my-modify-date!
      else
        date-time date-time-year@ 1980 - $7F and 9 lshift
        date-time date-time-month@ 5 lshift or
        date-time date-time-day@ or self my-modify-date!
      then
      date-time date-time-hour@ 11 lshift
      date-time date-time-minute@ 5 lshift or
      date-time date-time-second@ 2 / or self my-modify-time-coarse!
    ;

    \ Get an entry's modification date and time
    :method modify-date-time@ { self -- date-time }
      make-date-time { date-time }
      self my-modify-date@ { date-field }
      date-field 9 rshift 1980 + date-time date-time-year!
      date-field 5 rshift $F and date-time date-time-month!
      date-field $1F and date-time date-time-day!
      self my-modify-time-coarse@ { time-field }
      time-field 11 rshift date-time date-time-hour!
      time-field 5 rshift $3F and date-time date-time-minute!
      time-field $1F and 2 * date-time date-time-second!
      date-time update-dotw
      date-time
    ;

    \ Get an entry's file size
    :method entry-file-size@ ( entry -- size ) my-entry-file-size@ ;

    \ Set an entry's file size
    :method entry-file-size! ( size entry -- ) my-entry-file-size! ;
    
  end-class

  \ FAT32 file class
  begin-class fat32-file

    member: my-file-fs
    member: my-file-open
    member: my-file-parent-index
    member: my-file-parent-cluster
    member: my-file-first-cluster
    member: my-file-offset
    member: my-file-current-cluster
    member: my-file-current-cluster-index

    \ Get the file size
    :method file-size@ { self -- size }
      self my-file-open@ averts x-not-open
      self my-file-parent-index@ self my-file-parent-cluster@
      self my-file-fs@ entry@ entry-file-size@
    ;

    \ Set the file size
    :private file-size! { size self -- }
      self my-file-parent-index@ self my-file-parent-cluster@
      self my-file-fs@ entry@ { entry }
      make-date-time { date-time }
      date-time entry modify-date-time!
      size entry entry-file-size!
      entry self my-file-parent-index@ self my-file-parent-cluster@
      self my-file-fs@ entry!
    ;

    \ Get the current file sector
    :private current-file-sector@ { self -- sector }
      self my-file-offset@
      self my-file-fs@ cluster-sectors@ sector-size * umod
      self my-file-current-cluster@
      self my-file-fs@ cluster-offset>sector
    ;
    
    \ Advance to the next cluster if necessary
    :private advance-cluster { self -- }
      self my-file-offset@ self file-size@ <= if
        self my-file-current-cluster-index@ 1+ dup { new-index }
        self my-file-fs@ cluster-sectors@ sector-size * *
        self my-file-offset@ = if
          self my-file-current-cluster@ 0 self my-file-fs@ fat@
          dup link-cluster? if
            cluster-link self my-file-current-cluster!
            new-index self my-file-current-cluster-index!
          else
            drop
          then
        then
      then
    ;

    \ Expand a file
    :private expand-file { self -- }
      self my-file-current-cluster-index@ 1+ dup { new-index }
      self my-file-fs@ cluster-sectors@ sector-size * *
      self my-file-offset@ = if
        self my-file-current-cluster@ 0 self my-file-fs@ fat@ end-cluster? if
          self my-file-current-cluster@ self my-file-fs@ allocate-link-cluster
          self my-file-current-cluster!
          new-index self my-file-current-cluster-index!
        then
      then
    ;
    
    \ Read a file sector
    :private read-file-sector { bytes self -- count }
      self current-file-sector@ { sector }
      sector-size self my-file-offset@ sector-size umod -
      self file-size@ self my-file-offset@ - bytes >len min min { count }
      self my-file-offset@ sector-size umod { offset }
      0 count bytes >slice offset sector
      self my-file-fs@ fat32-device@ block-part@
      self my-file-offset@ count + self my-file-offset!
      self advance-cluster
      count
    ;

    \ Write a file sector
    :private write-file-sector { bytes self -- count }
      self current-file-sector@ { sector }
      sector-size self my-file-offset@ sector-size umod -
      bytes >len min { count }
      self my-file-offset@ sector-size umod { offset }
      0 count bytes >slice offset sector
      self my-file-fs@ fat32-device@ block-part!
      self my-file-offset@ count + self my-file-offset!
      self file-size@ self my-file-offset@ max self file-size!
      self advance-cluster
      count
    ;
    
    \ Constructor
    :method new { fs self -- }
      fs self my-file-fs!
      false self my-file-open!
      -1 self my-file-parent-index!
      -1 self my-file-parent-cluster!
      -1 self my-file-first-cluster!
      0 self my-file-offset!
      -1 self my-file-current-cluster!
      0 self my-file-current-cluster-index!
    ;

    \ Close a directory
    :method close { self -- }
      self my-file-open@ averts x-not-open
      false self my-file-open!
      self my-file-first-cluster@ self my-file-fs@ unregister-open
    ;

    \ Actually create a file
    :method do-create-file { name parent-dir self -- }
      name parent-dir dir-first-cluster@ self my-file-fs@ entry-exists?
      triggers x-entry-already-exists
      parent-dir dir-first-cluster@ self my-file-fs@ allocate-entry
      self my-file-parent-cluster!
      self my-file-parent-index!
      self my-file-fs@ allocate-cluster
      dup self my-file-first-cluster!
      self my-file-current-cluster!
      make-fat32-entry { entry }
      0 self my-file-first-cluster@ name entry init-file-entry
      entry self my-file-parent-index@ self my-file-parent-cluster@
      self my-file-fs@ entry!
      true self my-file-open!
      self my-file-first-cluster@ self my-file-fs@ register-open
    ;

    \ Actually open a file
    :method do-open-file { name parent-dir self -- }
      name parent-dir dir-first-cluster@ self my-file-fs@ lookup-entry
      self my-file-parent-cluster!
      self my-file-parent-index!
      self my-file-parent-index@ self my-file-parent-cluster@
      self my-file-fs@ entry@ { entry }
      entry entry-file? averts x-entry-not-file
      entry first-cluster@ dup self my-file-first-cluster!
      self my-file-current-cluster!
      true self my-file-open!
      self my-file-first-cluster@ self my-file-fs@ register-open
    ;

    \ Actually seek to an offset in a file
    :private do-seek-file { offset self -- }
      offset self file-size@ min 0 max self my-file-offset!
      0 self my-file-current-cluster-index!
      self my-file-first-cluster@ { cluster }
      self my-file-offset@ { offset }
      begin
        offset sector-size < if
          cluster self my-file-current-cluster! true
        else
          cluster 0 self my-file-fs@ fat@ dup link-cluster? if
            self my-file-current-cluster-index@ 1+
            self my-file-current-cluster-index!
            cluster-link to cluster
            offset sector-size - to offset
            false
          else
            drop cluster self my-file-current-cluster! true
          then
        then
      until
    ;

    \ Read a file
    :method read-file { bytes self -- count }
      self my-file-open@ averts x-not-open
      bytes >len { len }
      0 { count }
      begin
        count len < if
          count len count - bytes >slice self read-file-sector dup 0<> if
            +to count false
          else
            drop true
          then
        else
          true
        then
      until
      count
    ;

    \ Write a file
    :method write-file { bytes self -- count }
      self my-file-open@ averts x-not-open
      bytes >len { len }
      0 { count }
      begin
        count len < if
          count len count - bytes >slice self write-file-sector dup 0<> if
            +to count self expand-file false
          else
            drop true
          then
        else
          true
        then
      until
      count
    ;

    \ Seek in a file
    :method seek-file { offset whence self -- }
      self my-file-open@ averts x-not-open
      whence case
        seek-set of endof
        seek-cur of offset self my-file-offset@ + to offset endof
        seek-end of offset self file-size@ + to offset endof
      endcase
      offset self do-seek-file
    ;

    \ Tell a file
    :method tell-file { self -- }
      self my-file-open@ averts x-not-open
      self my-file-offset@
    ;

    \ Truncate a file
    :method truncate-file { self -- }
      self my-file-open@ averts x-not-open
      self my-file-first-cluster@ self my-file-fs@ open-count@
      1 = averts x-shared-file
      self my-file-current-cluster@ self my-file-fs@ free-cluster-tail
      self my-file-offset@ self file-size!
    ;

    \ Get a file's filesystem
    :method fs@ ( self -- fs )
      my-file-fs@
    ;

    \ Redirect input to be from a file
    :method with-file-input { xt self -- }
      sector-size make-bytes { buffer }
      0 0 buffer >slice ref { slice }
      buffer slice self 3 [: { buffer slice self }
        slice ref@ >len 0> if
          true
        else
          self my-file-offset@ self file-size@ <
        then
      ;] bind
      buffer slice self 3 [: { buffer slice self }
        begin
          slice ref@ >len 0= if
            buffer self read-file 0 swap buffer >slice slice ref!
          then
          slice ref@ { current-slice }
          current-slice >len { len }
          len 0> if
            0 current-slice c@+
            1 len 1- current-slice >slice slice ref!
            true
          else
            yield
            false
          then
        until
      ;] bind
      xt with-input
    ;

    \ Actually hande output redirection
    :private with-file-generic-output { xt self -- }
      sector-size make-bytes { buffer }
      0 ref { offset }
      ['] true
      buffer offset self 3 [: { c buffer offset self }
        offset ref@ { current-offset }
        c current-offset buffer c!+
        1 +to current-offset
        current-offset buffer >len = if
          buffer { current-buffer }
          begin
            current-buffer >len { len }
            current-buffer self write-file dup len = if
              drop
              0 offset ref!
              true
            else
              len over - current-buffer >slice to current-buffer
              yield
              false
            then
          until
        else
          current-offset offset ref!
        then
      ;] bind
      xt execute
      offset ref@ 0> if
        0 offset ref@ buffer >slice to buffer
        begin
          buffer >len { len }
          buffer self write-file dup len = if
            drop
            true
          else
            len over - buffer >slice to buffer
            yield
            false
          then
        until
      then
    ;
    
    \ Redirect output to be to a file
    :method with-file-output { xt self -- }
      xt 1 ['] with-output bind self with-file-generic-output
    ;

    \ Redirect error output to be to a file
    :method with-file-error-output { xt self -- }
      xt 1 ['] with-error-output bind self with-file-generic-output
    ;

    \ Redirect output and error output to be to a file
    :method with-file-all-output { xt self -- }
      xt 1 [: { emit?-xt emit-xt xt }
        emit?-xt emit-xt
        emit?-xt emit-xt xt 3 ['] with-output bind
        with-error-output
      ;] bind self with-file-generic-output
    ;
    
  end-class

  \ FAT32 directory class
  begin-class fat32-dir

    member: my-dir-fs
    member: my-dir-open
    member: my-dir-root
    member: my-dir-parent-index
    member: my-dir-parent-cluster
    member: my-dir-first-cluster
    member: my-dir-offset
    member: my-dir-current-cluster
    member: my-dir-current-cluster-index

    \ Get directory first cluster
    :method dir-first-cluster@ ( dir -- cluster ) my-dir-first-cluster@ ;

    \ Update the directory date and time
    :private update-dir-date-time { self -- }
      self my-dir-root@ not if
        self my-dir-parent-index@ self my-dir-parent-cluster@ self my-dir-fs@
        update-entry-date-time
      then
    ;

    \ Find a containing directory and a file name
    :private resolve-file-path { path self -- name dir opened? }
      path segment-path { parts }
      parts validate-file-path
      0 parts @+ s" /" equal? if
        self my-dir-root@ averts x-invalid-path
        1 parts >len 1- parts >slice to parts
      then
      parts >len { len }
      len 0> averts x-empty-path
      len 1 = if 0 parts @+ self false exit then
      0 parts @+ self open-dir { current-dir }
      len 1- 1 ?do
        i parts @+ current-dir open-dir { new-dir }
        current-dir close
        new-dir to current-dir
      loop
      len 1-  parts @+ current-dir true
    ;

    \ Find a containing directory and a directory name
    :private resolve-dir-path { path self -- name dir opened? }
      path segment-path { parts }
      parts validate-dir-path
      0 parts @+ s" /" equal? if
        self my-dir-root@ averts x-invalid-path
        1 parts >len 1- parts >slice to parts
      then
      parts >len { len }
      len 0> averts x-empty-path
      len 1 = if 0 parts @+ self false exit then
      0 parts @+ self open-dir { current-dir }
      len 1- 1 ?do
        i parts @+ current-dir open-dir { new-dir }
        current-dir close
        new-dir to current-dir
      loop
      len 1- parts @+ current-dir true
    ;

    \ Find a containing directory and a file or directory name
    :private resolve-path { path self -- name dir opened? }
      path segment-path { parts }
      parts validate-path
      0 parts @+ s" /" equal? if
        self my-dir-root@ averts x-invalid-path
        1 parts >len 1- parts >slice to parts
      then
      parts >len { len }
      len 0> averts x-empty-path
      len 1 = if 0 parts @+ self false exit then
      0 parts @+ self open-dir { current-dir }
      len 1- 1 ?do
        i parts @+ current-dir open-dir { new-dir }
        current-dir close
        new-dir to current-dir
      loop
      len 1-  parts @+ current-dir true
    ;

    \ Constructor
    :method new { fs self -- }
      fs self my-dir-fs!
      false self my-dir-open!
      false self my-dir-root!
      -1 self my-dir-parent-index!
      -1 self my-dir-parent-cluster!
      -1 self my-dir-first-cluster!
      0 self my-dir-offset!
      -1 self my-dir-current-cluster!
      0 self my-dir-current-cluster-index!
    ;

    \ Close a directory
    :method close { self -- }
      self my-dir-open@ averts x-not-open
      false self my-dir-open!
      self my-dir-first-cluster@ self my-dir-fs@ unregister-open
    ;
    
    \ Read a directory
    :method read-dir { self -- entry|0 entry-read? }
      self my-dir-open@ averts x-not-open
      begin
        self my-dir-current-cluster-index@ 1+ { next-index }
        self my-dir-offset@ next-index
        self my-dir-fs@ dir-cluster-entry-count@ * < if
          self my-dir-offset@
          self my-dir-fs@ dir-cluster-entry-count@ umod
          self my-dir-current-cluster@ self my-dir-fs@ entry@ { entry }
          entry entry-end? if
            0 false true
          else
            self my-dir-offset@ 1+ self my-dir-offset!
            entry entry-deleted? not if
              entry entry-file? entry entry-dir? or if
                entry true true
              else
                false
              then
            else
              false
            then
          then
        else
          self my-dir-current-cluster@ 0 self my-dir-fs@ fat@
          dup link-cluster? if
            cluster-link self my-dir-current-cluster!
            next-index self my-dir-current-cluster-index!
            false
          else
            0 false true
          then
        then
      until
    ;

    \ Create a file
    :method create-file { path self -- file }
      self my-dir-open@ averts x-not-open
      path self resolve-file-path { name dir opened? }
      name dir [: { name dir }
        dir my-dir-fs@ make-fat32-file { file }
        name dir file do-create-file
        dir update-dir-date-time
        file
      ;] try
      opened? if dir close then
      ?raise
    ;

    \ Open a file
    :method open-file { path self -- file }
      self my-dir-open@ averts x-not-open
      path self resolve-file-path { name dir opened? }
      name dir [: { name dir }
        dir my-dir-fs@ make-fat32-file { file }
        name dir file do-open-file
        file
      ;] try
      opened? if dir close then
      ?raise
    ;

    \ Remove a file
    :method remove-file { path self -- }
      self my-dir-open@ averts x-not-open
      path self resolve-file-path { name dir opened? }
      name dir my-dir-first-cluster@ dir my-dir-fs@ lookup-entry
      dir [: { index cluster dir }
        index cluster dir my-dir-fs@ entry@ { entry }
        entry entry-file? averts x-entry-not-file
        entry first-cluster@ dir my-dir-fs@ open-count@ 0= averts x-open
        entry first-cluster@ dir my-dir-fs@ free-cluster-chain
        entry mark-entry-deleted
        entry index cluster dir my-dir-fs@ entry!
        dir update-dir-date-time
      ;] try
      opened? if dir close then
      ?raise
    ;

    \ Create a directory
    :method create-dir { path self -- dir }
      self my-dir-open@ averts x-not-open
      path self resolve-dir-path { name parent-dir opened? }
      name parent-dir [: { name parent-dir }
        parent-dir my-dir-fs@ make-fat32-dir { dir }
        name parent-dir dir do-create-dir
        parent-dir update-dir-date-time
        dir
      ;] try
      opened? if parent-dir close then
      ?raise
    ;

    \ Open a directory
    :method open-dir { path self -- dir }
      self my-dir-open@ averts x-not-open
      path self resolve-dir-path { name parent-dir opened? }
      name parent-dir [: { name parent-dir }
        parent-dir my-dir-fs@ make-fat32-dir { dir }
        name parent-dir dir do-open-dir
        dir
      ;] try
      opened? if parent-dir close then
      ?raise
    ;

    \ Remove a directory
    :method remove-dir { path self -- }
      self my-dir-open@ averts x-not-open
      path self resolve-dir-path { name parent-dir opened? }
      name parent-dir open-dir { removed-dir }
      removed-dir dir-empty? averts x-dir-is-not-empty
      removed-dir close
      name parent-dir my-dir-first-cluster@ parent-dir my-dir-fs@ lookup-entry
      parent-dir [:
        { index cluster parent-dir }
        index cluster parent-dir my-dir-fs@ entry@ { entry }
        entry entry-dir? averts x-entry-not-dir
        entry first-cluster@ parent-dir my-dir-fs@ open-count@ 0= averts x-open
        entry first-cluster@ parent-dir my-dir-fs@ free-cluster-chain
        entry mark-entry-deleted
        entry index cluster parent-dir my-dir-fs@ entry!
        parent-dir update-dir-date-time
      ;] try
      opened? if parent-dir close then
      ?raise
    ;

    \ Rename an entry
    :method rename { new-name old-path self -- }
      self my-dir-open@ averts x-not-open
      old-path self resolve-path { old-name parent-dir opened? }
      old-name parent-dir my-dir-first-cluster@ parent-dir
      my-dir-fs@ lookup-entry
      new-name old-name parent-dir [:
        { index cluster new-name old-name parent-dir }
        index cluster parent-dir my-dir-fs@ entry@ { entry }
        entry entry-dir? if
          old-name forbidden-dir? triggers x-forbidden-dir
          new-name forbidden-dir? triggers x-forbidden-dir
          new-name entry dir-name!
        else
          new-name entry file-name!
        then
        entry index cluster parent-dir my-dir-fs@ entry!
        parent-dir update-dir-date-time
      ;] try
      opened? if parent-dir close then
      ?raise
    ;

    \ Get whether a directory is empty
    :method dir-empty? { self -- empty? }
      self my-dir-open@ averts x-not-open
      0 self my-dir-first-cluster@ { index cluster }
      begin
        index cluster self my-dir-fs@ find-entry
        over -1 = if 2drop true exit then
        to cluster to index
        index cluster self my-dir-fs@ entry@ { entry }
        entry entry-end? if true exit then
        entry entry-deleted? not if
          entry name@
          dup s" ." equal?
          over s" .." equal? or
          swap s" " equal? or not if false exit then
        then
        1 +to index
      again
    ;

    \ Get whether an entry exists
    :method exists? { path self -- exists? }
      self my-dir-open@ averts x-not-open
      path self resolve-path { name parent-dir opened? }
      name parent-dir my-dir-first-cluster@ parent-dir my-dir-fs@ entry-exists?
      opened? if parent-dir close then
    ;

    \ Get whether an entry is a file
    :method file? { path self -- file? }
      self my-dir-open@ averts x-not-open
      path self resolve-path { name parent-dir opened? }
      name parent-dir my-dir-first-cluster@ parent-dir my-dir-fs@ lookup-entry
      parent-dir my-dir-fs@ entry@ entry-file?
      opened? if parent-dir close then
    ;

    \ Get whether an entry is a directory
    :method dir? { path self -- dir? }
      self my-dir-open@ averts x-not-open
      path self resolve-path { name parent-dir opened? }
      name parent-dir my-dir-first-cluster@ parent-dir my-dir-fs@ lookup-entry
      parent-dir my-dir-fs@ entry@ entry-dir?
      opened? if parent-dir close then
    ;

    \ Get a directory's filesystem
    :method fs@ ( self -- fs ) my-dir-fs@ ;

    \ Raw directory creation
    :private do-create-dir-raw { name parent-dir self -- }
      parent-dir my-dir-first-cluster@ self my-dir-fs@ allocate-entry
      self my-dir-parent-cluster!
      self my-dir-parent-index!
      self my-dir-fs@ allocate-cluster { dir-cluster }
      make-fat32-entry { entry }
      entry init-end-entry
      entry 0 dir-cluster self my-dir-fs@ entry!
      dir-cluster self my-dir-first-cluster!
      make-fat32-entry { entry }
      dir-cluster name entry init-dir-entry
      entry self my-dir-parent-index@ self my-dir-parent-cluster@
      self my-dir-fs@ entry!
    ;

    \ Create . directory entry
    :private do-create-dot-entry { self -- }
      self my-dir-first-cluster@ self my-dir-fs@ allocate-entry
      { index cluster }
      make-fat32-entry { entry }
      self my-dir-first-cluster@ s" ." entry init-dir-entry
      entry index cluster self my-dir-fs@ entry!
    ;

    \ Create .. directory entry
    :private do-create-dot-dot-entry { parent-dir self -- }
      self my-dir-first-cluster@ self my-dir-fs@ allocate-entry
      { index cluster }
      make-fat32-entry { entry }
      parent-dir my-dir-first-cluster@ s" .." entry init-dir-entry
      entry index cluster self my-dir-fs@ entry!
    ;
    
    \ Actually create a directory
    :method do-create-dir { name parent-dir self -- }
      name parent-dir my-dir-first-cluster@ self my-dir-fs@ entry-exists?
      triggers x-entry-already-exists
      name parent-dir self do-create-dir-raw
      self do-create-dot-entry
      parent-dir self do-create-dot-dot-entry
      true self my-dir-open!
      false self my-dir-root!
      self my-dir-first-cluster@ self my-dir-fs@ register-open
    ;

    \ Actually open a directory
    :method do-open-dir { name parent-dir self -- }
      name parent-dir my-dir-first-cluster@ self my-dir-fs@ lookup-entry
      self my-dir-parent-cluster!
      self my-dir-parent-index!
      self my-dir-parent-index@ self my-dir-parent-cluster@
      self my-dir-fs@ entry@ { entry }
      entry entry-dir? averts x-entry-not-dir
      entry first-cluster@ dup
      self my-dir-first-cluster! self my-dir-current-cluster!
      true self my-dir-open!
      false self my-dir-root!
      self my-dir-first-cluster@ self my-dir-fs@ register-open
    ;

    \ Get the root directory
    :method do-root-dir { cluster self -- }
      -1 self my-dir-parent-cluster!
      -1 self my-dir-parent-index!
      cluster self my-dir-first-cluster!
      cluster self my-dir-current-cluster!
      true self my-dir-open!
      true self my-dir-root!
      self my-dir-first-cluster@ self my-dir-fs@ register-open
    ;

  end-class

  \ FAT32 filesystem class
  begin-class fat32-fs

    member: my-device
    member: my-open-map
    member: my-first-sector
    member: my-cluster-sectors
    member: my-reserved-sectors
    member: my-fat-count
    member: my-sector-count
    member: my-fat-sectors
    member: my-root-dir-cluster
    member: my-info-sector
    member: my-free-cluster-count
    member: my-recent-allocated-cluster

    \ Get the cluster count
    :private cluster-count@ { self -- }
      self my-sector-count@ self my-reserved-sectors@ -
      self my-fat-count@ self my-fat-sectors@ * -
      self my-fat-sectors@ sector-size * 4 / 2 - min
    ;

    \ Read the info sector
    :private read-info-sector { self -- }
      sector-size make-bytes { data }
      data self my-info-sector@ self my-first-sector@ + self my-device@ block@
      $000 data w@+ $41615252 = averts x-bad-info-sector
      $1E4 data w@+ $61417272 = averts x-bad-info-sector
      $1FC data w@+ $AA550000 = averts x-bad-info-sector
      $1E8 data w@+ self cluster-count@ min self my-free-cluster-count!
      $1EC data w@+ self my-recent-allocated-cluster!
    ;

    \ Write the info sector
    :private write-info-sector { self -- }
      sector-size make-bytes { data }
      data self my-info-sector@ self my-first-sector@ + self my-device@ block@
      self my-free-cluster-count@ $1E8 data w!+
      self my-recent-allocated-cluster@ $1EC data w!+
      data self my-info-sector@ self my-first-sector@ + self my-device@ block!
    ;
    
    \ Get a FAT sector for a cluster
    :private cluster>fat-sector { cluster fat self -- sector }
      self my-first-sector@ self my-reserved-sectors@ +
      self my-fat-sectors@ fat * + cluster sector-size 4 / / +
    ;

    \ Get a sector for a cluster
    :private cluster>sector { cluster self -- sector }
      self my-first-sector@ self my-reserved-sectors@ +
      self my-fat-count@ self my-fat-sectors@ * +
      cluster 2 - self my-cluster-sectors@ * +
    ;

    \ Get the number of sectors per cluster
    :method cluster-sectors@ ( self -- sectors ) my-cluster-sectors@ ;
    
    \ Read the FAT
    :method fat@ { cluster fat self -- link }
      cluster fat self cluster>fat-sector { sector }
      cell make-bytes { data }
      data cluster sector-size 4 / umod cells sector self my-device@ block-part@
      0 data w@+
    ;

    \ Write the FAT
    :private fat! { link cluster fat self -- }
      cluster fat self cluster>fat-sector { sector }
      cell make-bytes { data }
      cluster sector-size 4 / umod cells { offset }
      data offset sector self my-device@ block-part@
      0 data w@+ $F0000000 and link $0FFFFFFF and or 0 data w!+
      data offset sector self my-device@ block-part!
    ;

    \ Write all the FAT's
    :private all-fat! { link cluster self -- }
      self my-fat-count@ 0 ?do link cluster i self fat! loop
    ;

    \ Find a free cluster
    :private find-free-cluster { self -- cluster }
      self my-recent-allocated-cluster@
      dup -1 = if drop 2 else 1+ then { recent }
      recent self cluster-count@ 2 + recent ?do
        i 0 self fat@ free-cluster? if i exit then
      loop
      recent 2 ?do
        i 0 self fat@ free-cluster? if i exit then
      loop
      ['] x-no-clusters-free ?raise
    ;

    \ Allocate a cluster
    :method allocate-cluster { self -- cluster }
      self find-free-cluster { cluster }
      end-cluster-mark cluster self all-fat!
      cluster self my-recent-allocated-cluster!
      -1 self my-free-cluster-count@ + self my-free-cluster-count!
      self write-info-sector
      cluster
    ;

    \ Allocate and link a cluster
    :method allocate-link-cluster { cluster self -- cluster' }
      self allocate-cluster { cluster' }
      cluster' cluster self all-fat!
      cluster'
    ;

    \ Free a cluster
    :private free-cluster { cluster self -- }
      free-cluster-mark cluster self all-fat!
      1 self my-free-cluster-count@ + self my-free-cluster-count!
      self write-info-sector
    ;

    \ Free a cluster chain
    :method free-cluster-chain { cluster self -- }
      begin
        cluster 0 self fat@
        cluster self free-cluster
        dup link-cluster? if cluster-link to cluster false else drop true then
      until
    ;

    \ Free a cluster tail
    :method free-cluster-tail { cluster self -- }
      cluster 0 self fat@
      dup link-cluster? if cluster-link self free-cluster-chain else drop then
    ;

    \ Cluster offset to sector
    :method cluster-offset>sector ( offset cluster self -- sector )
      cluster>sector swap sector-size / +
    ;
    
    \ Directory cluster entry count
    :method dir-cluster-entry-count@ ( self -- )
      my-cluster-sectors@ sector-size entry-size / *
    ;

    \ Find an entry
    :method find-entry { index cluster self -- index' cluster' | -1 -1 }
      begin index self dir-cluster-entry-count@ >= while
        self dir-cluster-entry-count@ negate +to index
        cluster 0 self fat@ dup link-cluster? if
          cluster-link to cluster
        else
          drop -1 -1 exit
        then
      repeat
      index cluster
    ;

    \ Read an entry
    :method entry@ { index cluster self -- entry }
      index cluster self find-entry dup -1 <> averts x-out-of-range-entry
      to cluster to index
      make-fat32-entry { entry }
      cluster self cluster>sector
      index sector-size entry-size / u/ + { sector }
      index sector-size entry-size / umod to index
      entry-size make-bytes { data }
      data index entry-size * sector self my-device@ block-part@
      data entry buffer>entry
      entry
    ;

    \ Write an entry
    :method entry! { entry index cluster self -- entry }
      index cluster self find-entry dup -1 <> averts x-out-of-range-entry
      to cluster to index
      cluster self cluster>sector
      index sector-size entry-size / u/ + { sector }
      index sector-size entry-size / umod to index
      entry-size make-bytes { data }
      data entry entry>buffer
      data index entry-size * sector self my-device@ block-part!
    ;

    \ Look up an entry
    :method lookup-entry { name cluster self -- index cluster }
      name upcase-bytes to name
      name validate-name
      0 { index }
      begin
        index cluster self find-entry
        dup -1 <> averts x-entry-not-found
        to cluster to index
        index cluster self entry@ { entry }
        entry entry-end? not averts x-entry-not-found \ first char is not $00
        entry entry-deleted? not if \ first char is not $E5
          name entry name@ equal? if
            index cluster exit
          then
        then
        1 +to index
      again
    ;

    \ Get whether an entry exists
    :method entry-exists? { name cluster self -- exists? }
      name upcase-bytes to name
      name validate-name
      0 { index }
      begin
        index cluster self find-entry
        dup -1 <> averts x-no-end-marker
        to cluster to index
        index cluster self entry@ { entry }
        entry entry-end? if false exit then \ first char is $00
        entry entry-deleted? not if \ first char is not $E5
          name entry name@ equal? if
            true exit
          then
        then
        1 +to index
      again
    ;

    \ Expand a directory
    :method expand-dir { index cluster self -- }
      index 1+ self cluster-sectors@ sector-size * entry-size u/ umod to index
      index 0= if cluster self allocate-link-cluster to cluster then
      make-fat32-entry { entry }
      entry init-end-entry
      entry index cluster self entry!
    ;

    \ Allocate an entry
    :method allocate-entry { cluster self -- index cluster }
      0 { index }
      begin
        index cluster self find-entry
        dup -1 <> averts x-no-end-marker
        to cluster to index
        index cluster self entry@ { entry }
        entry entry-end? if
          index cluster self expand-dir
          index cluster exit
        then
        entry entry-deleted? if
          index cluster exit
        then
        1 +to index
      again
    ;

    \ Update entry date and time
    :method update-entry-date-time { index cluster self -- }
      index -1 <> cluster -1 <> and if
        make-date-time { date-time }
        index cluster self entry@ { entry }
        date-time entry modify-date-time!
        entry index cluster self entry!
      then
    ;

    \ Constructor
    :method new { partition device self -- }
      device self my-device!
      default-open-map-size [: ;] ['] = make-map self my-open-map!
      partition partition-first-sector@ self my-first-sector!
      sector-size make-bytes { data }
      data self my-first-sector@ self my-device@ block@
      $00B data unaligned-h@+ sector-size = averts x-sector-size-not-supported
      $00D data c@+ self my-cluster-sectors!
      $00E data h@+ self my-reserved-sectors!
      $010 data c@+ self my-fat-count!
      $020 data w@+ self my-sector-count!
      $024 data w@+ self my-fat-sectors!
      $02A data h@+ 0= averts x-fs-version-not-supported
      $02C data w@+ self my-root-dir-cluster!
      $030 data h@+ self my-info-sector!
      self read-info-sector
    ;

    \ Get filesystem device
    :method fat32-device@ ( self -- device ) my-device@ ;

    \ Get root directory
    :method root-dir@ { self -- dir }
      self make-fat32-dir { dir }
      self my-root-dir-cluster@ dir do-root-dir
      dir
    ;

    \ Flush a filesystem
    :method flush ( self -- ) my-device@ flush-blocks ;
    
    \ Register an open file or directory
    :method register-open { cluster self -- }
      cluster self my-open-map@ find-map if
        1+ cluster self my-open-map@ insert-map
      else
        drop 1 cluster self my-open-map@ insert-map
      then
    ;

    \ Unregister an open file or directory
    :method unregister-open { cluster self -- }
      cluster self my-open-map@ find-map if
        dup 1 > if
          1- cluster self my-open-map@ insert-map
        else
          drop cluster self my-open-map@ remove-map
        then
      else
        drop
      then
    ;

    \ Get the number of open references to a file or directory
    :method open-count@ { cluster self -- count }
      cluster self my-open-map@ find-map not if drop 0 then
    ;

    \ Create a file
    :method create-file { path self -- file }
      self root-dir@ { root-dir }
      path root-dir ['] create-file try
      root-dir close
      ?raise
    ;
    
    \ Open a file
    :method open-file { path self -- file }
      self root-dir@ { root-dir }
      path root-dir ['] open-file try
      root-dir close
      ?raise
    ;
    
    \ Remove a file
    :method remove-file { path self -- }
      self root-dir@ { root-dir }
      path root-dir ['] remove-file try
      root-dir close
      ?raise
    ;
    
    \ Create a directory
    :method create-dir { path self -- dir' }
      self root-dir@ { root-dir }
      path root-dir ['] create-dir try
      root-dir close
      ?raise
    ;
    
    \ Open a directory
    :method open-dir { path self -- dir' }
      0 path [: [char] / <> if 1+ then ;] foldl 0<> if
        self root-dir@ { root-dir }
        path root-dir ['] open-dir try
        root-dir close
        ?raise
      else
        path >len 0> averts x-empty-path
        self root-dir@
      then
    ;
    
    \ Remove a directory
    :method remove-dir { path self -- }
      self root-dir@ { root-dir }
      path root-dir ['] remove-dir try
      root-dir close
      ?raise
    ;
    
    \ Repath a file or directory
    :method rename { new-name path self -- }
      self root-dir@ { root-dir }
      new-name path root-dir ['] rename try
      root-dir close
      ?raise
    ;
    
    \ Get whether a directory is empty
    :method dir-empty? { self -- empty? }
      self root-dir@ { root-dir }
      root-dir ['] dir-empty? try
      root-dir close
      ?raise
    ;

    \ Get whether a directory entry exists
    :method exists? { path self -- exists? }
      self root-dir@ { root-dir }
      path root-dir ['] exists? try
      root-dir close
      ?raise
    ;

    \ Get whether a directory entry is a file
    :method file? { path self -- file? }
      self root-dir@ { root-dir }
      path root-dir ['] file? try
      root-dir close
      ?raise
    ;

    \ Get whether a directory entry is a directory
    :method dir? { path self -- dir? }
      self root-dir@ { root-dir }
      path root-dir ['] dir? try
      root-dir close
      ?raise
    ;
    
  end-class

end-module