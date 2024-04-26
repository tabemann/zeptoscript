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

begin-module zscript-set

  begin-module zscript-set-internal

    \ Empty values
    \
    \ This is not garbage collected so does not need to be in the heap.
    symbol empty-value

    \ Removed values
    \
    \ This is not garbage collected so does not need to be in the heap.
    symbol removed-value
    
    \ Set minimum size
    4 constant set-min-size
    
    \ Set outer record
    begin-record set-outer

      \ Set inner structure
      item: set-inner

      \ Set entry count
      item: set-entry-count
      
      \ Set hash function
      item: set-hash-xt

      \ Set equality function
      item: set-equal-xt
      
    end-record

    \ Primitive insert
    : prim-insert-set { val hash inner -- }
      inner >len { len }
      hash len umod { index }
      begin
        index inner @+ empty-value forth::= if
          val index inner !+ true
        else
          1 +to index
          index len = if 0 to index then
          false
        then
      until
    ;
    
    \ Expand a set
    : expand-set { set -- }
      set set-inner@ { old-inner }
      old-inner >len { old-len }
      old-len 3 * 1 rshift make-cells { new-inner }
      0 { index }
      set set-hash-xt@ { hash-xt }
      old-len 3 * 1 rshift 0 ?do empty-value i new-inner !+ loop
      begin index old-len < while
        index old-inner @+ dup empty-value <> over removed-value <> and if
          { current-val }
          current-val dup hash-xt execute new-inner prim-insert-set
        else
          drop
        then
        1 +to index
      repeat
      new-inner set set-inner!
    ;
    
    \ Get the index of an entry in a set
    : set-index { hash val set -- index success? }
      set set-inner@ { inner }
      inner >len { len }
      set set-equal-xt@ { equal }
      hash len umod { index }
      begin
        index inner @+ { current-val }
        current-val empty-value forth::<> if
          current-val removed-value forth::<> if
            val current-val equal execute if
              index true true
            else
              1 +to index
              index len = if 0 to index then
              false
            then
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
    
    \ Get the index of an entry in a set to insert
    : insert-set-index { hash val set -- index success? }
      set set-inner@ { inner }
      inner >len { len }
      set set-equal-xt@ { equal }
      hash len umod { index }
      begin
        index inner @+ { current-val }
        current-val dup empty-value <> swap removed-value <> and if
          val current-val equal execute if
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

  \ Make a set (a size of 0 indicates a default size)
  \
  \ Note that the real size of the set is one entry larger than the specified
  \ size
  : make-set { size hash-xt equal-xt -- set }
    size set-min-size max
    1+ dup to size make-cells dup { inner } 0 hash-xt equal-xt >set-outer
    size 0 ?do empty-value i inner !+ loop
  ;

  \ Duplicate a set
  : duplicate-set { set -- set' }
    set set-inner@ duplicate { inner' }
    set duplicate { set' }
    inner' set' set-inner!
    set'
  ;

  \ Iterate over the elements of a set
  : iter-set { set xt -- } \ xt ( val -- )
    set set-inner@ { inner }
    set set-entry-count@ { count }
    0 0 { index found-count }
    begin found-count count < while
      index inner @+ { current-val }
      current-val dup empty-value <> swap removed-value <> and if
        current-val xt execute
        1 +to found-count
      then
      1 +to index
    repeat
  ;

  \ Get whether any element of a set meet a predicate
  : any-set { set xt -- } \ xt ( val -- flag )
    set set-inner@ { inner }
    set set-entry-count@ { count }
    0 0 { index found-count }
    begin found-count count < while
      index inner @+ { current-val }
      current-val dup empty-value <> swap removed-value <> and if
        current-val xt execute if true exit then
        1 +to found-count
      then
      1 +to index
    repeat
    false
  ;

  \ Get whether all elements of a set meet a predicate
  : all-set { set xt -- } \ xt ( val -- flag )
    set set-inner@ { inner }
    set set-entry-count@ { count }
    0 0 { index found-count }
    begin found-count count < while
      index inner @+ { current-val }
      current-val dup empty-value <> swap removed-value <> and if
        current-val xt execute not if false exit then
        1 +to found-count
      then
      1 +to index
    repeat
    true
  ;

  \ Get the values of a set
  : set>values { set -- values }
    set set-inner@ { inner }
    set set-entry-count@ { count }
    count make-cells { values }
    0 0 { src-index dest-index }
    begin dest-index count < while
      src-index inner @+ { current-val }
      current-val dup empty-value <> swap removed-value <> and if
        current-val dest-index values !+ 1 +to dest-index
      then
      1 +to src-index
    repeat
    values
  ;

  \ Insert an entry in a set
  : insert-set { val set -- }
    val set set-hash-xt@ execute { hash }
    hash val set set-index if
      val swap set set-inner@ !+
    else
      drop set set-entry-count@ set set-inner@ >len 1- = if
        set expand-set
        hash val set insert-set-index not if
          val swap set set-inner@ !+
        else
          [: ." should not happen!" cr ;] ?raise
        then
      else
        hash val set insert-set-index not if
          val swap set set-inner@ !+
        else
          [: ." should not happen!" cr ;] ?raise
        then
      then
      set set-entry-count@ 1+ set set-entry-count!
    then
  ;

  \ Remove an entry from a set
  : remove-set { val set -- }
    val set set-hash-xt@ execute val set set-index if
      removed-value swap set set-inner@ !+
      set set-entry-count@ 1- set set-entry-count!
    else
      drop
    then
  ;
  
  \ Test for membership in a set
  : in-set? { val set -- found? }
    set set-inner@ { inner }
    inner >len { len }
    set set-equal-xt@ { equal }
    val set set-hash-xt@ execute len umod { index }
    begin
      index inner @+ { current-val }
      current-val empty-value forth::<> if
        current-val removed-value forth::<> if
          val current-val equal execute if
            true true
          else
            1 +to index
            index len = if 0 to index then
            false
          then
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
