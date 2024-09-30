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

begin-module zscript-iter
  
  zscript-oo import
  zscript-array import
  zscript-list import
  zscript-map import
  zscript-set import
  zscript-coroutine import
  
  \ Are we at the end of an iterator
  method iter>end? ( iter -- end? )
  
  \ Get the current value and the next iterator
  method iter>next ( iter -- next-iter current-value )
  
  \ Get the length of an iterator
  method iter>len ( iter -- elements )

  \ Range elements iterator
  begin-class step-range-elts-inner

    \ The range current value
    member: my-current

    \ The range step value
    member: my-step

    \ The remaining number of elements in the range
    member: my-len

    \ Constructor
    :method new { current step len self -- }
      current self my-current!
      step self my-step!
      len self my-len!
    ;

    \ Are we at the end of the range
    :method iter>end? { self -- end? } self my-len@ 0= ;

    \ Get the next element of the range
    :method iter>next { self -- next-iter current-value }
      self my-current@ { current }
      self my-len@ { len }
      len 0> if
        self my-step@ { step }
        current step + step self my-len@ 1- make-step-range-elts-inner current
      else
        self current
      then
    ;

    \ Get the number of elements in the range
    :method iter>len { self -- len } self my-len@ ;

  end-class

  \ Create a 1-step range elements iterator
  : make-range-elts { start-val end-val -- elts }
    start-val 1 end-val start-val - make-step-range-elts-inner
  ;

  \ Create an arbitrary-step range elements iterator
  : make-step-range-elts { start-val end-val step -- elts }
    start-val step end-val start-val - step / make-step-range-elts-inner
  ;

  \ Truncated elements iterator
  begin-class truncate-elts-inner

    \ The parent iterator
    member: my-iter

    \ The truncation length
    member: my-len

    \ Constructor
    :method new { len iter self -- }
      iter self my-iter!
      len self my-len!
    ;

    \ Are we at the end of the iterator?
    :method iter>end? { self -- end? } self my-len@ 0= ;

    \ Get the next element of the iterator
    :method iter>next { self -- next-iter current-value }
      self my-iter@ { iter }
      self my-len@ { len }
      len if
        iter iter>next { next-iter val }
        len 1- next-iter make-truncate-elts-inner val
      else
        iter iter>next nip self swap
      then
    ;

    \ Get the length of an iterator
    :method iter>len { self -- } self my-len@ ;

  end-class

  \ Lengthless truncated elements iterator
  begin-class no-len-truncate-elts-inner

    \ The parent iterator
    member: my-iter

    \ The truncation length
    member: my-len

    \ Constructor
    :method new { len iter self -- }
      iter self my-iter!
      len self my-len!
    ;

    \ Are we at the end of the iterator?
    :method iter>end? { self -- end? }
      self my-iter@ iter>end? self my-len@ 0= or
    ;

    \ Get the next element of the iterator
    :method iter>next { self -- next-iter current-value }
      self my-iter@ { iter }
      self my-len@ { len }
      self my-iter@ iter>end? not len 0<> and if
        iter iter>next { next-iter val }
        len 1- next-iter make-no-len-truncate-elts-inner val
      else
        iter iter>next nip self swap
      then
    ;

  end-class

  \ Truncate an iterator
  : truncate-elts { len iter -- }
    ['] iter>len iter has-method? if
      len iter make-truncate-elts-inner
    else
      len iter make-no-len-truncate-elts-inner
    then
  ;
  
  \ Cell sequence elements iterator
  begin-class cells-elts

    \ The cell sequence
    member: my-seq

    \ Constructor
    :method new { seq self -- } seq self my-seq! ;
    
    \ Are we at the end of the sequence?
    :method iter>end? { self -- end? } self my-seq@ >len 0= ;

    \ Get the next element of the cell sequence
    :method iter>next { self -- next-iter current-value }
      self my-seq@ { seq }
      seq >len if
        1 seq truncate-start make-cells-elts 0 seq @+
      else
        0cells make-cells-elts 0
      then
    ;

    \ Get the length of an iterator
    :method iter>len { self -- len } self my-seq@ >len ;

  end-class

  \ Byte sequence elements iterator
  begin-class bytes-elts

    \ The byte sequence
    member: my-seq

    \ Constructor
    :method new { seq self -- } seq self my-seq! ;
    
    \ Are we at the end of the sequence?
    :method iter>end? { self -- end? } self my-seq@ >len 0= ;

    \ Get the next element of the byte sequence
    :method iter>next { self -- next-iter current-value }
      self my-seq@ { seq }
      seq >len if
        1 seq truncate-start make-bytes-elts 0 seq c@+
      else
        0bytes make-bytes-elts 0
      then
    ;

    \ Get the length of an iterator
    :method iter>len { self -- len } self my-seq@ >len ;

  end-class

  \ Halfword sequence elements iterator
  begin-class halfwords-elts

    \ The halfword sequence
    member: my-seq

    \ Constructor
    :method new { seq self -- } seq self my-seq! ;
    
    \ Are we at the end of the sequence?
    :method iter>end? { self -- end? } self my-seq@ >len 2 < ;

    \ Get the next element of the halfword sequence
    :method iter>next { self -- next-iter current-value }
      self my-seq@ { seq }
      seq >len 1 > if
        2 seq truncate-start make-halfwords-elts 0 seq h@+
      else
        0bytes make-halfwords-elts 0
      then
    ;

    \ Get the length of an iterator
    :method iter>len { self -- len } self my-seq@ >len 2 / ;

  end-class

  \ Word sequence elements iterator
  begin-class words-elts

    \ The word sequence
    member: my-seq

    \ Constructor
    :method new { seq self -- } seq self my-seq! ;
    
    \ Are we at the end of the sequence?
    :method iter>end? { self -- end? } self my-seq@ >len 4 < ;

    \ Get the next element of the word sequence
    :method iter>next { self -- next-iter current-value }
      self my-seq@ { seq }
      seq >len 3 > if
        4 seq truncate-start make-words-elts 0 seq w@+
      else
        0bytes make-words-elts 0
      then
    ;

    \ Get the length of an iterator
    :method iter>len { self -- len } self my-seq@ >len 4 / ;

  end-class

  \ Construct a cell iterator from an array
  : make-cell-array-elts { array -- iter }
    array zscript-array-internal::array-seq@ make-cells-elts
  ;
  
  \ Construct a byte iterator from an array
  : make-byte-array-elts { array -- iter }
    array zscript-array-internal::array-seq@ make-bytes-elts
  ;  
  
  \ Construct a halfword iterator from an array
  : make-halfword-array-elts { array -- iter }
    array zscript-array-internal::array-seq@ make-halfwords-elts
  ;
  
  \ Construct a word iterator from an array
  : make-word-array-elts { array -- iter }
    array zscript-array-internal::array-seq@ make-words-elts
  ;  

  \ List elements iterator
  begin-class list-elts

    \ The list
    member: my-list

    \ Constructor
    :method new { list self -- } list self my-list! ;
    
    \ Are we at the end of the list?
    :method iter>end? { self -- end? } self my-list@ empty? ;

    \ Get the next element of the list
    :method iter>next { self -- next-iter current-value }
      self my-list@ { list }
      list if
        list tail@ make-list-elts list head@
      else
        0 make-list-elts 0
      then
    ;

    \ Get the length of an iterator
    :method iter>len { self -- len } self my-list@ list>len ;

  end-class

  \ Map elements iterator implementation
  begin-class map-elts-inner

    \ The map
    member: my-map

    \ The count
    member: my-count

    \ The index
    member: my-index
    
    \ The found count
    member: my-found-count

    \ Get the actual full length
    :private full-len@ { self -- }
      self my-map@ map-entry-count@ self my-count@ min
    ;
    
    \ Constructor
    :method new { found-count index count map self -- }
      map self my-map!
      count self my-count!
      index self my-index!
      found-count self my-found-count!
    ;

    \ Are we at the end of the map?
    :method iter>end? { self -- end? } self my-index@ self full-len@ >= ;

    \ Get the next element of the map
    :method iter>next { self -- next-iter current-value }
      self my-map@ { map }
      self full-len@ { len }
      self my-index@ { index }
      self my-found-count@ { found-count }
      self map zscript-map-internal::map-inner@ { inner }
      begin
        found-count len < if
          index 1 lshift { current }
          current inner @+ { current-key }
          current-key dup zscript-map-internal::empty-key <>
          swap zscript-map-internal::removed-key <> and if
            found-count 1+ index 1+ self my-count@ map make-map-elts-inner
            current-key current 1+ inner @+ >pair true
          else
            1 +to index false
          then
        else
          self 0 true
        then
      until
    ;
    
    \ Get the length of an iterator
    :method iter>len { self -- len }
      self full-len@ self my-found-count@ - 0 max
    ;
    
  end-class

  \ Map elements iterator
  : make-map-elts { map -- iter }
    0 0 map map-entry-count@ map make-map-elts-inner
  ;

  \ Set elements iterator implementation
  begin-class set-elts-inner

    \ The set
    member: my-set

    \ The count
    member: my-count

    \ The index
    member: my-index
    
    \ The found count
    member: my-found-count

    \ Get the actual full length
    :private full-len@ { self -- }
      self my-set@ set-entry-count@ self my-count@ min
    ;
    
    \ Constructor
    :method new { found-count index count set self -- }
      set self my-set!
      count self my-count!
      index self my-index!
      found-count self my-found-count!
    ;

    \ Are we at the end of the set?
    :method iter>end? { self -- end? } self my-index@ self full-len@ >= ;

    \ Get the next element of the set
    :method iter>next { self -- next-iter current-value }
      self my-set@ { set }
      self full-len@ { len }
      self my-index@ { index }
      self my-found-count@ { found-count }
      self set zscript-set-internal::set-inner@ { inner }
      begin
        found-count len < if
          index inner @+ { current-key }
          current-key dup zscript-set-internal::empty-value <>
          swap zscript-set-internal::removed-value <> and if
            found-count 1+ index 1+ self my-count@ set make-set-elts-inner
            current-key true
          else
            1 +to index false
          then
        else
          self 0 true
        then
      until
    ;
    
    \ Get the length of an iterator
    :method iter>len { self -- len }
      self full-len@ self my-found-count@ - 0 max
    ;
    
  end-class

  \ Set elements iterator
  : make-set-elts { set -- iter }
    0 0 set set-entry-count@ set make-set-elts-inner
  ;

  \ Coroutine iterator implementation
  begin-class coroutine-elts-inner

    \ Our coroutine
    member: my-coroutine

    \ Our index
    member: my-index
    
    \ Constructor
    :method new { index coroutine self -- }
      coroutine self my-coroutine!
      index self my-index!
    ;

    \ Are we at the end of the list?
    :method iter>end? { self -- end? }
      self my-coroutine@ coroutine-state@ dead =
    ;

    \ Get the next element of the list
    :method iter>next { self -- next-iter current-value }
      self my-coroutine@ { coroutine }
      self my-index@ { index }
      coroutine coroutine-state@ dead <> if
        index coroutine resume
        index 1+ coroutine make-coroutine-elts-inner swap
      else
        self 0
      then
    ;
    
  end-class

  \ Coroutine iterator
  : make-coroutine-elts { coroutine -- iter }
    0 coroutine make-coroutine-elts-inner
  ;

  \ Iterate over elements
  : iter-elts { elts xt -- } \ xt: ( item -- )
    begin elts iter>end? not while
      elts iter>next swap to elts xt execute
    repeat
  ;

  \ Iterate over elements with an index
  : iteri-elts { elts xt -- } \ xt: ( item index -- )
    0 { index }
    begin elts iter>end? not while
      elts iter>next swap to elts index xt execute
      1 +to index
    repeat
  ;

  \ Convert elements to a cell sequence
  : elts>cells { elts -- cells }
    0 { index }
    elts iter>len make-cells { cells }
    begin elts iter>end? not while
      elts iter>next swap to elts index cells !+
      1 +to index
    repeat
    cells
  ;

  \ Convert elements to a byte sequence
  : elts>bytes { elts -- bytes }
    0 { index }
    elts iter>len make-bytes { bytes }
    begin elts iter>end? not while
      elts iter>next swap to elts index bytes c!+
      1 +to index
    repeat
    bytes
  ;

  \ Convert elements to a halfword sequence
  : elts>halfwords { elts -- halfwords }
    0 { index }
    elts iter>len 1 lshift make-bytes { halfwords }
    begin elts iter>end? not while
      elts iter>next swap to elts index 1 lshift halfwords h!+
      1 +to index
    repeat
    halfwords
  ;

  \ Convert elements to a word sequence
  : elts>words { elts -- words }
    0 { index }
    elts iter>len 2 lshift make-bytes { words }
    begin elts iter>end? not while
      elts iter>next swap to elts index 2 lshift words w!+
      1 +to index
    repeat
    words
  ;

  \ Convert elements to a list
  : elts>list { elts -- list }
    empty { list-first }
    empty { list-last }
    begin elts iter>end? not while
      elts iter>next swap to elts empty cons dup
      list-last if list-last tail! else to list-first then
      to list-last
    repeat
    list-first
  ;
  
  \ Map elements to a cell sequence
  : map>cells-elts { elts xt -- cells } \ xt: ( item -- item' )
    0 { index }
    elts iter>len make-cells { cells }
    begin elts iter>end? not while
      elts iter>next swap to elts xt execute index cells !+
      1 +to index
    repeat
    cells
  ;

  \ Map elements to a cell sequence with an index
  : mapi>cells-elts { elts xt -- cells } \ xt: ( item index -- item' )
    0 { index }
    elts iter>len make-cells { cells }
    begin elts iter>end? not while
      elts iter>next swap to elts index xt execute index cells !+
      1 +to index
    repeat
    cells
  ;

  \ Map elements to a byte sequence
  : map>bytes-elts { elts xt -- bytes } \ xt: ( item -- item' )
    0 { index }
    elts iter>len make-bytes { bytes }
    begin elts iter>end? not while
      elts iter>next swap to elts xt execute index bytes c!+
      1 +to index
    repeat
    bytes
  ;

  \ Map elements to a byte sequence with an index
  : mapi>bytes-elts { elts xt -- bytes } \ xt: ( item index -- item' )
    0 { index }
    elts iter>len make-bytes { bytes }
    begin elts iter>end? not while
      elts iter>next swap to elts index xt execute index bytes c!+
      1 +to index
    repeat
    bytes
  ;

  \ Map elements to a halfword sequence
  : map>halfwords-elts { elts xt -- halfwords } \ xt: ( item -- item' )
    0 { index }
    elts iter>len 1 lshift make-bytes { halfwords }
    begin elts iter>end? not while
      elts iter>next swap to elts xt execute index 1 lshift halfwords h!+
      1 +to index
    repeat
    halfwords
  ;

  \ Map elements to a halfword sequence with an index
  : mapi>halfwords-elts { elts xt -- halfwords } \ xt: ( item index -- item' )
    0 { index }
    elts iter>len 1 lshift make-bytes { halfwords }
    begin elts iter>end? not while
      elts iter>next swap to elts index xt execute index 1 lshift halfwords h!+
      1 +to index
    repeat
    halfwords
  ;

  \ Map elements to a word sequence
  : map>words-elts { elts xt -- words } \ xt: ( item -- item' )
    0 { index }
    elts iter>len 2 lshift make-bytes { words }
    begin elts iter>end? not while
      elts iter>next swap to elts xt execute index 2 lshift words w!+
      1 +to index
    repeat
    words
  ;

  \ Map elements to a word sequence with an index
  : mapi>words-elts { elts xt -- words } \ xt: ( item index -- item' )
    0 { index }
    elts iter>len 2 lshift make-bytes { words }
    begin elts iter>end? not while
      elts iter>next swap to elts index xt execute index 2 lshift words w!+
      1 +to index
    repeat
    words
  ;

  \ Map elements to a list
  : map>list-elts { elts xt -- list } \ xt: ( item -- item' )
    empty { list-first }
    empty { list-last }
    begin elts iter>end? not while
      elts iter>next swap to elts xt execute empty cons dup
      list-last if list-last tail! else to list-first then
      to list-last
    repeat
    list-first
  ;
  
  \ Map elements to a list with an index
  : mapi>list-elts { elts xt -- list } \ xt: ( item index -- item' )
    0 { index }
    empty { list-first }
    empty { list-last }
    begin elts iter>end? not while
      elts iter>next swap to elts index xt execute empty cons dup
      list-last if list-last tail! else to list-first then
      to list-last
      1 +to index
    repeat
    list-first
  ;
  
  \ Filter elements to a list
  : filter>list-elts { elts xt -- list } \ xt: ( item -- match? )
    empty { list-first }
    empty { list-last }
    begin elts iter>end? not while
      elts iter>next swap to elts dup { item } xt execute if
        item empty cons dup
        list-last if list-last tail! else to list-first then
        to list-last
      then
    repeat
    list-first
  ;
  
  \ Filter elements to a list with an index
  : filteri>list-elts { elts xt -- list } \ xt: ( item index -- match? )
    0 { index }
    empty { list-first }
    empty { list-last }
    begin elts iter>end? not while
      elts iter>next swap to elts dup { item } index xt execute if
        item empty cons dup
        list-last if list-last tail! else to list-first then
        to list-last
      then
      1 +to index
    repeat
    list-first
  ;
  
  \ Test whether a predicate applies to all elements; note that not
  \ all elements will be iterated over if an element returns false, and true
  \ with be returned if there are no elements
  : all-elts { elts xt -- all? } \ xt ( element -- match? )
    begin elts iter>end? not while
      elts iter>next swap to elts xt execute not if false exit then
    repeat
    true
  ;

  \ Test whether a predicate applies to all elements; note that not
  \ all elements will be iterated over if an element returns false, and true
  \ with be returned if there are no elements
  : alli-elts { elts xt -- all? } \ xt ( element index -- match? )
    0 { index }
    begin elts iter>end? not while
      elts iter>next swap to elts index xt execute not if false exit then
      1 +to index
    repeat
    true
  ;

  \ Test whether a predicate applies to any element; note that not
  \ all elements will be iterated over if an element returns true, and false
  \ will be returned if there are no elements
  : any-elts { elts xt -- any? } \ xt ( element -- match? )
    begin elts iter>end? not while
      elts iter>next swap to elts xt execute if true exit then
    repeat
    false
  ;

  \ Test whether a predicate applies to any element; note that not
  \ all elements will be iterated over if an element returns true, and false
  \ will be returned if there are no elements
  : anyi-elts { elts xt -- any? } \ xt ( element index -- match? )
    0 { index }
    begin elts iter>end? not while
      elts iter>next swap to elts index xt execute if true exit then
      1 +to index
    repeat
    false
  ;

  \ Fold left over an iterator
  : foldl-elts ( x ) { elts xt -- x' } \ xt ( x item -- x' )
    begin elts iter>end? not while
      elts iter>next swap to elts xt execute
    repeat
  ;

  \ Fold left over an iterator with an index
  : foldli-elts ( x ) { elts xt -- x' } \ xt ( x item index -- x' )
    0 { index }
    begin elts iter>end? not while
      elts iter>next swap to elts index xt execute
      1 +to index
    repeat
  ;

  \ Fold right over an iterator
  : foldr-elts { x elts xt -- x' } \ xt ( item x -- x' )
    x elts elts>cells xt foldr
  ;

  \ Fold right over an iterator with an index
  : foldri-elts { x elts xt -- x' } \ xt ( item x index -- x' )
    x elts elts>cells xt foldri
  ;

end-module
