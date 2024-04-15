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

begin-module zscript-map

  zscript import

  \ Not a valid key
  : x-not-valid-key ( -- ) ." not a valid key" cr ;

  begin-module zscript-map-internal

    \ Validate a key
    : validate-key ( key -- ) 0<> averts x-not-valid-key ;
    
    \ Map minimum size
    4 constant map-min-size
    
    \ Map outer record
    begin-record map-outer

      \ Map inner structure
      item: map-inner

      \ Map entry count
      item: map-entry-count
      
      \ Map hash function
      item: map-hash-xt

      \ Map equality function
      item: map-equal-xt
      
    end-record

    \ Primitive insert
    : prim-insert-map { val key hash inner -- }
      inner >len 1 rshift { len }
      hash len umod { index }
      begin
        index 1 lshift { current }
        current inner @+ 0= if
          key current inner !+ val current 1+ inner !+ true
        else
          1 +to index
          index len = if 0 to index then
          false
        then
      until
    ;
    
    \ Expand a map
    : expand-map { map -- }
      map map-inner@ { old-inner }
      old-inner >len 1 rshift { old-len }
      old-len 3 * make-cells { new-inner }
      0 { index }
      map map-hash-xt@ { hash-xt }
      begin index old-len < while
        index 1 lshift { current }
        current old-inner @+ ?dup if { current-key }
          current 1+ old-inner @+ current-key dup hash-xt execute
          new-inner prim-insert-map
        then
        1 +to index
      repeat
      new-inner map map-inner!
    ;
    
    \ Get the index of an entry in a map
    : map-index { key map -- index success? }
      map map-inner@ { inner }
      inner >len 1 rshift { len }
      map map-equal-xt@ { equal }
      key map map-hash-xt@ execute len umod { index }
      begin
        index 1 lshift { current }
        current inner @+ { current-key }
        current-key if
          key current-key equal execute if
            index true true
          else
            1 +to index
            index len = if 0 to index then
            false
          then
        else
          index false true
        then
      until
    ;

  end-module> import

  \ Make a map (a size of 0 indicates a default size)
  : make-map { size hash-xt equal-xt -- map }
    size map-min-size max to size
    size 1 lshift make-cells 0 hash-xt equal-xt >map-outer
  ;

  \ Duplicate a map
  : duplicate-map { map -- map' }
    map map-inner@ duplicate { inner' }
    map duplicate { map' }
    inner' map' map-inner!
    map'
  ;

  \ Iterate over the elements of a map
  : iter-map { map xt -- } \ xt ( value key -- )
    map map-inner@ { inner }
    map map-entry-count@ { count }
    0 0 { index found-count }
    begin found-count count < while
      index 1 lshift { current }
      current inner @+ { current-key }
      current-key if
        current 1+ inner @+ current-key xt execute
        1 +to found-count
      then
      1 +to index
    repeat
  ;

  \ Map over a map and create a new map with identical keys but new values
  : map-map { map xt -- map' } \ xt ( value key -- value' )
    map map-inner@ { inner }
    map map-entry-count@ { count }
    map duplicate { map' }
    inner >len make-cells { inner' }
    inner' map' map-inner!
    0 0 { index found-count }
    begin found-count count < while
      index 1 lshift { current }
      current inner @+ { current-key }
      current-key if
        current-key current inner' !+
        current 1+ inner @+ current-key xt execute current 1+ inner' !+
        1 +to found-count
      then
      1 +to index
    repeat
    map'
  ;

  \ Map over a map and mutate its values in place
  : map!-map { map xt -- } \ xt ( value key -- value' )
    map map-inner@ { inner }
    map map-entry-count@ { count }
    0 0 { index found-count }
    begin found-count count < while
      index 1 lshift { current }
      current inner @+ { current-key }
      current-key if
        current 1+ inner @+ current-key xt execute current 1+ inner !+
        1 +to found-count
      then
      1 +to index
    repeat
  ;

  \ Get whether any element of a map meet a predicate
  : any-map { map xt -- } \ xt ( value key -- flag )
    map map-inner@ { inner }
    map map-entry-count@ { count }
    0 0 { index found-count }
    begin found-count count < while
      index 1 lshift { current }
      current inner @+ { current-key }
      current-key if
        current 1+ inner @+ current-key xt execute if true exit then
        1 +to found-count
      then
      1 +to index
    repeat
    false
  ;

  \ Get whether all elements of a map meet a predicate
  : all-map { map xt -- } \ xt ( value key -- flag )
    map map-inner@ { inner }
    map map-entry-count@ { count }
    0 0 { index found-count }
    begin found-count count < while
      index 1 lshift { current }
      current inner @+ { current-key }
      current-key if
        current 1+ inner @+ current-key xt execute not if false exit then
        1 +to found-count
      then
      1 +to index
    repeat
    true
  ;

  \ Get the keys of a map
  : map-keys { map -- keys }
    map map-inner@ { inner }
    map map-entry-count@ { count }
    count make-cells { keys }
    0 0 { src-index dest-index }
    begin dest-index count < while
      src-index 1 lshift inner @+ { current-key }
      current-key if current-key dest-index keys !+ 1 +to dest-index then
      1 +to src-index
    repeat
    keys
  ;

  \ Get the values of a map
  : map-values { map -- values }
    map map-inner@ { inner }
    map map-entry-count@ { count }
    count make-cells { values }
    0 0 { src-index dest-index }
    begin dest-index count < while
      src-index 1 lshift { current }
      current inner @+ if
        current 1+ inner @+ dest-index values !+ 1 +to dest-index
      then
      1 +to src-index
    repeat
    values
  ;

  \ Get the keys and values of a map as pairs
  : map-key-values { map -- pairs }
    map map-inner@ { inner }
    map map-entry-count@ { count }
    count make-cells { key-values }
    0 0 { src-index dest-index }
    begin dest-index count < while
      src-index 1 lshift { current }
      current inner @+ { current-key }
      current-key if
        current-key current 1+ inner @+ >pair dest-index key-values !+
        1 +to dest-index
      then
      1 +to src-index
    repeat
    key-values
  ;

  \ Insert an entry in a map
  : insert-map { val key map -- }
    key validate-key
    key map map-index if
      1 lshift { current }
      map map-inner@ { inner }
      key current inner !+
      val current 1+ inner !+
    else
      { index } map map-entry-count@ map map-inner@ >len 1 rshift 1- = if
        map expand-map
        key map map-index not if
          1 lshift { current }
          map map-inner@ { inner }
          key current inner !+
          val current 1+ inner !+
        else
          [: ." should not happen!" cr ;] ?raise
        then
      else
        index 1 lshift { current }
        map map-inner@ { inner }
        key current inner !+
        val current 1+ inner !+
      then
      map map-entry-count@ 1+ map map-entry-count!
    then
  ;

  \ Remove an entry from a map
  : remove-map { key map -- }
    key validate-key
    key map map-index if
      1 lshift { current }
      map map-inner@ { inner }
      0 current inner !+
      0 current 1+ inner !+
      map map-entry-count@ 1- map map-entry-count!
    else
      drop
    then
  ;
  
  \ Find an entry in a map
  : find-map { key map -- val found? }
    key validate-key
    map map-inner@ { inner }
    inner >len 1 rshift { len }
    map map-equal-xt@ { equal }
    key map map-hash-xt@ execute len umod { index }
    begin
      index 1 lshift { current }
      current inner @+ { current-key }
      current-key if
        key current-key equal execute if
          current 1+ inner @+ true true
        else
          1 +to index
          index len = if 0 to index then
          false
        then
      else
        0 false true
      then
    until
  ;

  \ Test for membership in a map
  : in-map? { key map -- found? }
    key validate-key
    map map-inner@ { inner }
    inner >len 1 rshift { len }
    map map-equal-xt@ { equal }
    key map map-hash-xt@ execute len umod { index }
    begin
      index 1 lshift { current }
      current inner @+ { current-key }
      current-key if
        key current-key equal execute if
          true true
        else
          1 +to index
          index len = if 0 to index then
          false
        then
      else
        false true
      then
    until
  ;

end-module
