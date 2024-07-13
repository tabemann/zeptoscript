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

begin-module zscript-array

  zscript-oo import
  zscript-special-oo import

  begin-module zscript-array-internal

    \ Symbol for defining a cell array
    symbol define-cell-array

    \ Symbol for defining a byte array
    symbol define-byte-array
    
    \ Get the underlying sequence for the array
    method array-seq@ ( self -- seq )

    \ Set the underlying sequence for the array
    method array-seq! ( seq self -- )

    \ Array type
    begin-class array-head

      \ The underlying sequence for the array
      member: my-array-seq

      \ Constructor
      :method new ( seq self -- ) my-array-seq! ;
      
      \ Get the underlying sequence for the array
      :method array-seq@ ( self -- seq ) my-array-seq@ ;

      \ Set the underlying sequence for the array
      :method array-seq! ( seq self -- ) my-array-seq! ;

      \ Convert an array to a string
      :method show { self -- }
        self array-seq@ cells? if
          self my-array-seq@ to self
          self >len { len }
          len 2 + make-cells { seq }
          s" #!" 0 seq !+
          self seq 1
          [: { element index seq } element try-show index 1+ seq !+ ;]
          bind iteri
          s" !#" len 1+ seq !+
          seq s"  " join
        else
          self my-array-seq@ to self
          self >len { len }
          len 2 + make-cells { seq }
          s" #$" 0 seq !+
          self seq 1 [: { byte index seq } byte try-show index 1+ seq !+ ;]
          bind iteri
          s" $#" len 1+ seq !+
          seq s"  " join
          exit
        then
      ;

      \ Get the hash of an array
      :method hash ( self -- ) my-array-seq@ hash ;

      \ Test for equality
      :method equal? { other self -- }
        other class@ self class@ = if
          other my-array-seq@ self my-array-seq@ equal?
        else
          false
        then
      ;

    end-class
    
  end-module> import

  \ Get whether something is an array
  : array? ( x -- array? ) ['] array-seq@ swap has-method? ;

  \ Get whether something is a cell array
  : cell-array? ( x -- cell-array? )
    dup array? if array-seq@ cells? else drop false then
  ;

  \ Get whether something is a byte array
  : byte-array? ( x -- byte-array? )
    dup array? if array-seq@ bytes? else drop false then
  ;

  \ Get the underlying sequence in an array
  : array-seq@ ( array -- seq )
    dup array? averts x-incorrect-type
    array-seq@
  ;

  \ Get the length of an array
  : array>len ( array -- len )
    array-seq@ >len
  ;

  \ Wrap a sequence in an array
  : wrap-seq-as-array ( seq -- array )
    dup cells? over bytes? or averts x-incorrect-type
    make-array-head
  ;

  \ Create a cell array from the stack
  : >cell-array ( x0 ... xn count -- array )
    >cells wrap-seq-as-array
  ;

  \ Create a byte array from the stack
  : >byte-array ( c0 ... cn count -- array )
    >bytes wrap-seq-as-array
  ;

  \ Explode an array onto the stack
  : array> ( array -- x0 ... xn count )
    array-seq@ seq>
  ;

  \ Begin defining a cell array
  : #! ( -- ) define-cell-array zscript-internal::begin-seq-define ;

  \ End defining a cell array
  : !# ( -- )
    define-cell-array zscript-internal::end-seq-define >cell-array
  ;

  \ Begin defining a byte array
  : #$ ( -- ) define-byte-array zscript-internal::begin-seq-define ;

  \ End defining a byte array
  : $# ( -- )
    define-byte-array zscript-internal::end-seq-define >byte-array
  ;

  \ Get an element of an array
  : array@ ( index array -- x )
    array-seq@ x@+
  ;

  \ Set an element of an array
  : array! ( x index array -- )
    array-seq@ dup cells? if !+ else c!+ then
  ;

  \ Duplicate an array
  : duplicate-array ( array -- array' )
    array-seq@ duplicate wrap-seq-as-array
  ;

  \ Concatenate two cell or byte arrays
  : concat-arrays ( array0 array1 -- array2 )
    array-seq@ swap array-seq@ swap concat wrap-seq-as-array
  ;

  \ Concatenate two cell or byte arrays in place
  : concat!-arrays { array0 array1 -- }
    array0 array-seq@ array1 array-seq@ concat array0 array-seq!
  ;

  \ Convert a sequence to an array, duplicating it
  : seq>array ( seq -- array )
    duplicate wrap-seq-as-array
  ;

  \ Convert an array to a sequence, duplicating it
  : array>seq ( array -- seq )
    array-seq@ duplicate
  ;

  \ Get a slice of an array
  : array>slice ( offset count array -- array' )
    array-seq@ >slice wrap-seq-as-array
  ;

  \ Get a slice of an array in place
  : array>slice! { offset count array -- }
    offset count array array-seq@ >slice array array-seq!
  ;

  \ Truncate the start of an array as a slice
  : truncate-start-array ( count array -- array' )
    array-seq@ truncate-start wrap-seq-as-array
  ;

  \ Truncate the end of an array as a slice
  : truncate-end-array ( count array -- array' )
    array-seq@ truncate-end wrap-seq-as-array
  ;

  \ Truncate the start of an array in place as a slice
  : truncate-start!-array { count array -- }
    count array array-seq@ truncate-start array array-seq!
  ;

  \ Truncate the end of an array in place as a slice
  : truncate-end!-array { count array -- }
    count array array-seq@ truncate-end array array-seq!
  ;

  \ Iterate over an array
  : iter-array ( array xt -- )
    swap array-seq@ swap iter
  ;

  \ Iterate over an array with an index
  : iteri-array ( array xt -- )
    swap array-seq@ swap iteri
  ;

  \ Get the index of an element that meets a predicate; note that the lowest
  \ matching index is returned, and xt will not necessarily be called against
  \ all items
  : find-index-array ( array xt -- index found? ) \ xt ( item -- flag )
    swap array-seq@ swap find-index
  ;

  \ Get the index of an element that meets a predicate with an index; note that
  \ the lowest matching index is returned, and xt will not necessarily be
  \ called against all items
  : find-indexi-array ( array xt -- index found? ) \ xt ( item index -- flag )
    swap array-seq@ swap find-indexi
  ;

  \ Map a cell or byte array into a new cell or byte array
  : map-array ( array xt -- array' ) \ xt ( item -- item' )
    swap array-seq@ swap map wrap-seq-as-array
  ;

  \ Map a cell or byte array into a new cell or byte array with an index
  : mapi-array ( array xt -- array' ) \ xt ( item -- item' )
    swap array-seq@ swap mapi wrap-seq-as-array
  ;

  \ Map a cell or byte array in place
  : map!-array ( array xt -- ) \ xt ( item -- item' )
    swap array-seq@ swap map!
  ;

  \ Map a cell or byte array in place with an index
  : mapi!-array ( array xt -- ) \ xt ( item -- item' )
    swap array-seq@ swap mapi!
  ;

  \ Filter a cell or byte array into a new cell or byte array
  : filter-array ( array xt -- array' ) \ xt ( item -- item' )
    swap array-seq@ swap filter wrap-seq-as-array
  ;

  \ Filter a cell or byte array into a new cell or byte array with an index
  : filteri-array ( array xt -- array' ) \ xt ( item -- item' )
    swap array-seq@ swap filteri wrap-seq-as-array
  ;

  \ Filter a cell or byte array in place
  : filter!-array { array xt -- } \ xt ( item -- item' )
    array array-seq@ xt filter array array-seq!
  ;

  \ Filter a cell or byte array in place with an index
  : filteri!-array { array xt -- } \ xt ( item -- item' )
    array array-seq@ xt filteri array array-seq!
  ;

  \ Fold left over a cell or byte array
  : foldl-array ( x array xt -- x' ) \ xt ( x item -- x' )
    swap array-seq@ swap foldl
  ;

  \ Fold left over a cell or byte array with an index
  : foldli-array ( x array xt -- x' ) \ xt ( x item index -- x' )
    swap array-seq@ swap foldli
  ;

  \ Fold right over a cell or byte array
  : foldr-array ( x array xt -- x' ) \ xt ( item x -- x' )
    swap array-seq@ swap foldr
  ;
  
  \ Fold right over a cell or byte array with an index
  : foldri ( x array xt -- x' ) \ xt ( item x index -- x' )
    swap array-seq@ swap foldri
  ;

  \ Collect elements of a cell array from left to right
  : collectl-cell-array ( x len xt -- array ) \ xt ( x -- x item )
    collectl-cells wrap-seq-as-array
  ;

  \ Collect elements of a cell array from left to right with an index
  : collectli-cell-array ( x len xt -- array ) \ xt ( x index -- x item )
    collectli-cells wrap-seq-as-array
  ;

  \ Collect elements of a cell array from right to left
  : collectr-cell-array ( x len xt -- array ) \ xt ( x -- x item )
    collectr-cells wrap-seq-as-array
  ;

  \ Collect elements of a cell array from right to left with an index
  : collectri-cell-array ( x len xt -- array ) \ xt ( x -- x item )
    collectri-cells wrap-seq-as-array
  ;

  \ Collect elements of a byte array from left to right
  : collectl-byte-array ( x len xt -- array ) \ xt ( x -- x item )
    collectl-bytes wrap-seq-as-array
  ;

  \ Collect elements of a byte array from left to right with an index
  : collectli-byte-array ( x len xt -- array ) \ xt ( x index -- x item )
    collectli-bytes wrap-seq-as-array
  ;

  \ Collect elements of a byte array from right to left
  : collectr-byte-array ( x len xt -- array ) \ xt ( x -- x item )
    collectr-bytes wrap-seq-as-array
  ;

  \ Collect elements of a byte array from right to left with an index
  : collectri-byte-array ( x len xt -- array ) \ xt ( x -- x item )
    collectri-bytes wrap-seq-as-array
  ;

  \ Reverse an array producing a new array
  : reverse-array ( array -- array' )
    array-seq@ reverse wrap-seq-as-array
  ;

  \ Reverse an array in place
  : reverse!-array ( array -- )
    array-seq@ reverse!
  ;

  \ Zip two arrays into a new array, using the length of the shorter array
  : zip-arrays ( array0 array1 -- array2 )
    array-seq@ swap array-seq@ swap zip wrap-seq-as-array
  ;

  \ Zip three arrays into a new array, using the length of the shorter array
  : zip3-arrays ( array0 array1 array2 -- array3 )
    array-seq@ rot array-seq@ rot array-seq@ rot zip3 wrap-seq-as-array
  ;

  \ Zip two arrays into the first array in-place, using the length of the
  \ shorter array
  : zip!-arrays { array0 array1 -- }
    array0 array-seq@ array1 array-seq@ zip array0 array-seq!
  ;

  \ Zip three arrays into the first array in-place, using the length of the
  \ shorter array
  : zip3!-arrays { array0 array1 array2 -- }
    array0 array-seq@ array1 array-seq@ array2 array-seq@ zip3 array0 array-seq!
  ;

  \ Heapsort an array in place
  : sort!-array ( array xt -- )
    swap array-seq@ swap sort!
  ;

  \ Heapsort an array, copying it
  : sort-array ( array xt -- array' )
    swap array-seq@ swap sort wrap-seq-as-array
  ;

  \ Get whether a predicate applies to all elements of a array; note that
  \ not all elements will be iterated over if an element returns false, and
  \ true will be returned if the array is empty
  : all-array ( array xt -- all? ) \ xt ( element -- match? )
    swap array-seq@ swap all
  ;
  
  \ Get whether a predicate applies to all elements of a array; note that
  \ not all elements will be iterated over if an element returns false, and
  \ true will be returned if the array is empty
  : alli-array ( array xt -- all? ) \ xt ( element index -- match? )
    swap array-seq@ swap alli
  ;

  \ Get whether a predicate applies to any element of a array; note that
  \ not all elements will be iterated over if an element returns true, and
  \ false will be returned if the array is empty
  : any-array ( array xt -- any? ) \ xt ( element -- match? )
    swap array-seq@ swap any
  ;

  \ Get whether a predicate applies to any element of a array; note that
  \ not all elements will be iterated over if an element returns true, and
  \ false will be returned if the array is empty
  : anyi-array ( array xt -- any? ) \ xt ( element index -- match? )
    swap array-seq@ swap anyi
  ;

  \ Split an array based on a predicate
  : split-array ( array xt -- array' ) \ xt ( item -- flag )
    swap array-seq@ swap split ['] wrap-seq-as-array map wrap-seq-as-array
  ;

  \ Split an array based on a predicate with an index
  : spliti-array ( array xt -- array' ) \ xt ( item index -- flag )
    swap array-seq@ swap spliti ['] wrap-seq-as-array map wrap-seq-as-array
  ;

  \ Split an array in place based on a predicate
  : split!-array { array xt -- } \ xt ( item -- flag )
    array array-seq@ xt split ['] wrap-seq-as-array map array array-seq!
  ;

  \ Split an array in place based on a predicate with an index
  : spliti!-array { array xt -- } \ xt ( item index -- flag )
    array array-seq@ xt spliti ['] wrap-seq-as-array map array array-seq!
  ;

  \ Join an array of arrays
  : join-arrays { list-array join-array -- array' }
    list-array array-seq@ ['] array-seq@ map join-array array-seq@
    join wrap-seq-as-array
  ;

end-module
