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

begin-module zscript-simple-blocks-fat32

  zscript-oo import
  zscript-block-dev import
  zscript-blk import
  zscript-fs import
  zscript-fat32 import

  \ Simple FAT32 filesystem class
  begin-class simple-blocks-fat32-fs

    member: my-blocks
    member: my-fs
    
    \ Constructor
    :method new { self -- }
      make-blocks self my-blocks!
      0 self my-blocks@ make-mbr partition@
      self my-blocks@ make-fat32-fs self my-fs!
    ;

    \ Get root directory
    :method root-dir@ ( self -- dir ) my-fs@ root-dir@ ;

    \ Flush a filesystem
    :method flush ( self -- ) my-fs@ flush ;

    \ Set write-through cache mode
    :method write-through! ( write-through self -- ) my-blocks@ write-through! ;

    \ Get write-through cache mode
    :method write-through@ ( self -- write-through ) my-blocks@ write-through@ ;

    \ Create a file
    :method create-file ( path dir -- file )
      my-fs@ create-file
    ;
  
    \ Open a file
    :method open-file ( path dir -- file )
      my-fs@ open-file
    ;
  
    \ Remove a file
    :method remove-file ( path dir -- )
      my-fs@ remove-file
    ;
  
    \ Create a directory
    :method create-dir ( path dir -- dir' )
      my-fs@ create-dir
    ;
  
    \ Open a directory
    :method open-dir ( path dir -- dir' )
      my-fs@ open-dir
    ;
  
    \ Remove a directory
    :method remove-dir ( path dir -- )
      my-fs@ remove-dir
    ;
  
    \ Rename a file or directory
    :method rename ( new-name path dir -- )
      my-fs@ rename
    ;
  
    \ Get whether a directory is empty
    :method dir-empty? ( dir -- empty? )
      my-fs@ dir-empty?
    ;

    \ Get whether a directory entry exists
    :method exists? ( path dir -- exists? )
      my-fs@ exists?
    ;

    \ Get whether a directory entry is a file
    :method file? ( path dir -- file? )
      my-fs@ file?
    ;

    \ Get whether a directory entry is a directory
    :method dir? ( path dir -- dir? )
      my-fs@ dir?
    ;

  end-class
  
end-module
