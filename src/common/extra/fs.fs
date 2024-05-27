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

begin-module zscript-fs

  zscript-oo import
  
  \ Unsupported file name format exception
  : x-file-name-format ( -- ) ." unsupported filename" cr ;

  \ Directory entry not found exception
  : x-entry-not-found ( -- ) ." directory entry not found" cr ;
  
  \ Directory entry already exists exception
  : x-entry-already-exists ( -- ) ." directory entry already exists" cr ;
  
  \ Directory entry is not a file exception
  : x-entry-not-file ( -- ) ." directory entry not file" cr ;
  
  \ Directory entry is not a directory exception
  : x-entry-not-dir ( -- ) ." directory entry not directory" cr ;
  
  \ Directory is not empty exception
  : x-dir-is-not-empty ( -- ) ." directory is not empty" cr ;
  
  \ Directory name being changed or set is forbidden exception
  : x-forbidden-dir ( -- ) ." forbidden directory name" cr ;
  
  \ No file or directory referred to in path within directory exception
  : x-empty-path ( -- ) ." empty path" cr ;
  
  \ Invalid path exception
  : x-invalid-path ( -- ) ." invalid path" cr ;

  \ File or directory is not open exception
  : x-not-open ( -- ) ." file/directory is not open" cr ;

  \ File is shared exception
  : x-shared-file ( -- ) ." file is shared" cr ;

  \ File or directory is open exception
  : x-open ( -- ) ." file/directory is open" cr ;

  \ Seek from the beginning of a file
  symbol seek-set

  \ Seek from the current position in a file
  symbol seek-cur

  \ Seek from the end of a file
  symbol seek-end

  \ Flush a filesystem
  method flush ( fs -- )
  
  \ Close a file or directory
  method close ( file|dir -- )
  
  \ Read data from a file
  method read-file ( buffer file -- bytes )
  
  \ Write data to a file
  method write-file ( buffer file -- bytes )
  
  \ Truncate a file
  method truncate-file ( file -- )
  
  \ Seek in a file
  method seek-file ( offset whence file -- )
  
  \ Get the current offset in a file
  method tell-file ( file -- offset )
  
  \ Get the size of a file
  method file-size@ ( file -- bytes )

  \ Get the filesystem of a file or directory
  method fs@ ( file|dir -- fs )
  
  \ Read an entry from a directory, and return whether an entry was read
  method read-dir ( entry dir -- entry-read? )
  
  \ Create a file
  method create-file ( path dir -- file )
  
  \ Open a file
  method open-file ( path dir -- file )
  
  \ Remove a file
  method remove-file ( path dir -- )
  
  \ Create a directory
  method create-dir ( path dir -- dir' )
  
  \ Open a directory
  method open-dir ( path dir -- dir' )
  
  \ Remove a directory
  method remove-dir ( path dir -- )
  
  \ Rename a file or directory
  method rename ( new-name path dir -- )
  
  \ Get whether a directory is empty
  method dir-empty? ( dir -- empty? )

  \ Get whether a directory entry exists
  method exists? ( path dir -- exists? )

  \ Get whether a directory entry is a file
  method file? ( path dir -- file? )

  \ Get whether a directory entry is a directory
  method dir? ( path dir -- dir? )

  \ Get the filesystem root directory
  method root-dir@ ( fs -- dir )

  \ Get whether an entry is a file
  method entry-file? ( entry -- file? )
  
  \ Get whether an entry is a directory
  method entry-dir? ( entry -- dir? )

  \ Get an entry's file or directory name
  method name@ ( entry -- name )

  \ Get an entry's creation date and time
  method create-date-time@ ( entry -- date-time )

  \ Get an entry's modification date and time
  method modify-date-time@ ( entry -- date-time )

  
end-module
