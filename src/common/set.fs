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

  zscript-oo import
  zscript-special-oo import
  
  \ Iterate over the elements of a set
  defer iter-set ( set xt -- ) \ xt ( val -- )

  begin-module zscript-set-internal

    \ Define a set
    symbol define-set

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

    \ Get the set inner structure
    method set-inner@ ( self -- set-inner )
    
    \ Set the set inner structure
    method set-inner! ( set-inner self -- )
    
    \ Get the set entry count
    method set-entry-count@ ( self -- set-entry-count )
    
    \ Set the set entry count
    method set-entry-count! ( set-entry-count self -- )
    
    \ Get the set hash function
    method set-hash-xt@ ( self -- set-hash-xt )
    
    \ Set the set hash function
    method set-hash-xt! ( set-hash-xt self -- )
    
    \ Get the set equality function
    method set-equal-xt@ ( self -- set-equal-xt )
    
    \ Set the set equality function
    method set-equal-xt! ( set-equal-xt self -- )
    
    \ Set type
    begin-class set-head

      \ Set inner structure
      member: my-set-inner

      \ Set entry count
      member: my-set-entry-count
      
      \ Set hash function
      member: my-set-hash-xt

      \ Set equality function
      member: my-set-equal-xt
      
      \ Constructor
      :method new { size hash-xt equal-xt self -- }
        equal-xt self my-set-equal-xt!
        hash-xt self my-set-hash-xt!
        size set-min-size max
        1+ dup to size make-cells dup { inner } self my-set-inner!
        0 self my-set-entry-count!
        size 0 ?do empty-value i inner !+ loop
      ;
      
      \ Get the set inner structure
      :method set-inner@ ( self -- set-inner ) my-set-inner@ ;

      \ Set the set inner structure
      :method set-inner! ( set-inner self -- ) my-set-inner! ;

      \ Get the set entry count
      :method set-entry-count@ ( self -- set-entry-count ) my-set-entry-count@ ;

      \ Set the set entry count
      :method set-entry-count! ( set-entry-count self -- ) my-set-entry-count! ;

      \ Get the set hash function
      :method set-hash-xt@ ( self -- set-hash-xt ) my-set-hash-xt@ ;

      \ Set the set hash function
      :method set-hash-xt! ( set-hash-xt self -- ) my-set-hash-xt! ;

      \ Get the set equality function
      :method set-equal-xt@ ( self -- set-equal-xt ) my-set-equal-xt@ ;

      \ Set the set equality function
      :method set-equal-xt! ( set-equal-xt self -- ) my-set-equal-xt! ;

      \ Show a set
      :method show { self -- bytes }
        self set-entry-count@ { len }
        len 2 + make-cells { seq }
        s" #|" 0 seq !+
        0 ref { index }
        self index seq 2 [: { val index seq }
          val try-show index ref@ 1+ seq !+
          index ref@ 1+ index ref!
        ;] bind iter-set
        s" |#" len 1+ seq !+
        seq s"  " join
      ;

      \ Get the hash of a set
      :method hash ( self -- hash ) drop 0 ;

      \ Test two sets for equality
      :method equal? ( other self -- equal? ) = ;
      
    end-class

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

  \ Get whether something is a set
  : set? ( x -- set? ) ['] set-inner@ swap has-method? ;
  
  \ Make a set (a size of 0 indicates a default size)
  \
  \ Note that the real size of the set is one entry larger than the specified
  \ size
  : make-set ( size hash-xt equal-xt -- set ) make-set-head ;

  \ Duplicate a set
  : duplicate-set { set -- set' }
    set set? averts x-incorrect-type
    set set-inner@ duplicate { inner' }
    set duplicate { set' }
    inner' set' set-inner!
    set'
  ;

  \ Iterate over the elements of a set
  [: { set xt -- } \ xt ( val -- )
    set set? averts x-incorrect-type
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
  ;] is iter-set

  \ Get whether any element of a set meet a predicate
  : any-set { set xt -- } \ xt ( val -- flag )
    set set? averts x-incorrect-type
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
    set set? averts x-incorrect-type
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
    set set? averts x-incorrect-type
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
    set set? averts x-incorrect-type
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
    set set? averts x-incorrect-type
    val set set-hash-xt@ execute val set set-index if
      removed-value swap set set-inner@ !+
      set set-entry-count@ 1- set set-entry-count!
    else
      drop
    then
  ;
  
  \ Test for membership in a set
  : in-set? { val set -- found? }
    set set? averts x-incorrect-type
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

  \ Get the entry count of a set
  : set-entry-count@ { set -- count }
    set set? averts x-incorrect-type
    set set-entry-count@
  ;

  \ Define a generic set
  : >generic-set ( valn ... val0 ) { count -- set }
    count ['] hash ['] equal? make-set { set }
    count 0 ?do set insert-set loop
    set
  ;
  
  \ Begin defining a generic set
  : #| ( -- ) define-set zscript-internal::begin-seq-define ;
  
  \ End defining a generic set
  : |# ( valn ... val0 -- set )
    define-set zscript-internal::end-seq-define >generic-set
  ;
  
end-module
