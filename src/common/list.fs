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

begin-module zscript-list

  begin-module zscript-list-internal

    \ List definition type
    symbol define-list

  end-module> import

  \ Cons a value onto the head of a list
  : cons ( x list -- list' ) >pair ;

  \ An empty list
  0 constant empty

  \ Get the head of a list
  : head@ ( list -- x ) 0 swap @+ ;

  \ Get the tail of a list
  : tail@ ( list -- list' ) 1 swap @+ ;

  \ Set the head of a list
  : head! ( x list -- ) 0 swap !+ ;

  \ Set the tail of a list
  : tail! ( tail-list list -- ) 1 swap !+ ;
  
  \ Is a list empty
  : empty? ( list -- flag ) 0= ;

  \ Get the last element of a list
  : last ( list -- x )
    dup if begin dup tail@ ?dup if nip false else head@ true then until then
  ;

  \ Get the nth element of a list, indexed from zero
  : nth { index list -- x }
    begin index 0> while
      list if list tail@ to list -1 +to index else 0 to index then
    repeat
    list if list head@ else empty then
  ;

  \ Get the tail of a list starting at the nth element of a list, indexed from
  \ zero; 0 returns the entire list
  : nth-tail { index list -- tail }
    begin index 0> while
      list if list tail@ to list -1 +to index else 0 to index then
    repeat
    list
  ;
    
  \ Get the length of a list
  : list>len { list -- len }
    0 begin list while 1+ list tail@ to list repeat
  ;
  
  \ Convert a list to a cell sequence
  : list>cells { list -- seq }
    list if
      list list>len { len }
      len make-cells { seq }
      0 { index }
      begin list while
        list head@ index seq !+ 1 +to index list tail@ to list
      repeat
      seq
    else
      0cells
    then
  ;

  \ Convert a list to a byte sequence
  : list>bytes { list -- seq }
    list if
      list list>len { len }
      len make-bytes { seq }
      0 { index }
      begin list while
        list head@ index seq c!+ 1 +to index list tail@ to list
      repeat
      seq
    else
      0cells
    then
  ;

  \ Convert a sequence to a list
  : seq>list { seq -- list }
    seq >len { len }
    len 0> if
      0 { list }
      len begin ?dup while 1- dup seq x@+ list cons to list repeat
      list
    else
      empty
    then
  ;

  \ Iterate over a list
  : iter-list { list xt -- } \ xt ( item -- )
    begin list while
      list head@ xt execute list tail@ to list
    repeat
  ;

  \ Iter over a list with an index
  : iteri-list { list xt -- } \ xt ( item index -- )
    0 { index }
    begin list while
      list head@ index xt execute list tail@ to list 1 +to index
    repeat
  ;

  \ Get the index of an element of a list that meets a predicate; note that the
  \ lowest matching index is returned, and xt will not necessarily be called
  \ against all items
  : find-index-list { list xt -- index found? } \ xt ( item -- flag )
    0 { index }
    begin list while
      list head@ xt execute if
        index true exit
      else
        list tail@ to list 1 +to index
      then
    repeat
    0 false
  ;

  \ Get the index of an element of a list that meets a predicate with an index;
  \ note that the lowest matching index is returned, and xt will not
  \ necessarily be called against all items
  : find-indexi-list { list xt -- index found? } \ xt ( item index -- flag )
    0 { index }
    begin list while
      list head@ index xt execute if
        index true exit
      else
        list tail@ to list 1 +to index
      then
    repeat
    0 false
  ;

  \ Map a list in reverse
  : rev-map-list { list xt -- list' } \ xt ( item -- item' )
    empty { list' }
    begin list while
      list head@ xt execute list' cons to list' list tail@ to list
    repeat
    list'
  ;

  \ Map a list with an index in reverse
  : rev-mapi-list { list xt -- list' } \ xt ( item index -- item' )
    empty { list' }
    0 { index }
    begin list while
      list head@ index xt execute list' cons to list'
      list tail@ to list
      1 +to index
    repeat
    list'
  ;

  \ Filter a list in reverse
  : rev-filter-list { list xt -- list' } \ xt ( item -- flag )
    empty { list' }
    begin list while
      list head@ dup { head } xt execute if
        head list' cons to list'
      then
      list tail@ to list
    repeat
    list'
  ;

  \ Filter a list with an index in reverse
  : rev-filteri-list { list xt -- list' } \ xt ( item index -- flag )
    empty { list' }
    0 { index }
    begin list while
      list head@ dup { head } index xt execute if
        head list' cons to list'
      then
      list tail@ to list
      1 +to index
    repeat
    list'
  ;

  \ Split a list into a list of lists based on a predicate
  : split-list { list xt -- parts-list } \ xt ( item -- flag )
    list 0= if 0 exit then
    empty { parts }
    0 { parts-last }
    empty { sub }
    0 { sub-last }
    begin list empty? not while
      list head@ xt execute if
        sub empty cons { part }
        parts empty? if
          part to parts
        else
          part parts-last tail!
        then
        part to parts-last
        empty to sub
      else
        list head@ empty cons { sub-part }
        sub empty? if
          sub-part to sub
        else
          sub-part sub-last tail!
        then
        sub-part to sub-last
      then
      list tail@ to list
    repeat
    sub empty cons { part }
    parts empty? if
      part to parts
    else
      part parts-last tail!
    then
    parts
  ;
  
  \ Split a list into a list of lists based on a predicate with an index
  : spliti-list { list xt -- parts-list } \ xt ( item index -- flag )
    list 0= if 0 exit then
    empty { parts }
    0 { parts-last }
    empty { sub }
    0 { sub-last }
    0 { index }
    begin list empty? not while
      list head@ index xt execute if
        sub empty cons { part }
        parts empty? if
          part to parts
        else
          part parts-last tail!
        then
        part to parts-last
        empty to sub
      else
        list head@ empty cons { sub-part }
        sub empty? if
          sub-part to sub
        else
          sub-part sub-last tail!
        then
        sub-part to sub-last
      then
      list tail@ to list
      1 +to index
    repeat
    sub empty cons { part }
    parts empty? if
      part to parts
    else
      part parts-last tail!
    then
    parts
  ;

  \ Reverse a list and convert it to a cell sequence
  : rev-list>cells { list -- seq }
    list list>len { len }
    len make-cells { seq }
    len { index }
    begin index 0> while
      -1 +to index list head@ index seq !+ list tail@ to list
    repeat
    seq
  ;
  
  \ Reverse a list and convert it to a byte sequence
  : rev-list>bytes { list -- seq }
    list list>len { len }
    len make-bytes { seq }
    len { index }
    begin index 0> while
      -1 +to index list head@ index seq c!+ list tail@ to list
    repeat
    seq
  ;

  \ Reverse a list
  : rev-list { list -- list' }
    empty { list' }
    begin list while
      list head@ list' cons to list'
      list tail@ to list
    repeat
    list'
  ;

  \ Map a list
  : map-list { list xt -- list' } \ xt ( item -- item' )
    empty { list' }
    empty { list'-last }
    begin list while
      list head@ xt execute empty cons { new-list }
      list' if
        new-list list'-last tail!
      else
        new-list to list'
      then
      new-list to list'-last
      list tail@ to list
    repeat
    list'
  ;

  \ Map a list with an index
  : mapi-list { list xt -- list' } \ xt ( item index -- item' )
    empty { list' }
    empty { list'-last }
    0 { index }
    begin list while
      list head@ index xt execute empty cons { new-list }
      list' if
        new-list list'-last tail!
      else
        new-list to list'
      then
      new-list to list'-last
      list tail@ to list
      1 +to index
    repeat
    list'
  ;

  \ Mutating map a list
  : map!-list { list xt -- } \ xt ( item -- item' )
    begin list while
      list head@ xt execute list head!
      list tail@ to list
    repeat
  ;

  \ Mutating a list with an index
  : mapi!-list { list xt -- } \ xt ( item index -- item' )
    0 { index }
    begin list while
      list head@ index xt execute list head!
      list tail@ to list
      1 +to index
    repeat
  ;

  \ Filter a list
  : filter-list { list xt -- list' } \ xt ( item -- flag )
    empty { list' }
    empty { list'-last }
    begin list while
      list head@ dup { head } xt execute if
        head empty cons { new-list }
        list' if
          new-list list'-last tail!
        else
          new-list to list'
        then
        new-list to list'-last
        list tail@ to list
      then
    repeat
    list'
  ;

  \ Filter a list with an index
  : filteri-list { list xt -- list' } \ xt ( item index -- flag )
    empty { list' }
    empty { list'-last }
    0 { index }
    begin list while
      list head@ dup { head } index xt execute if
        head empty cons { new-list }
        list' if
          new-list list'-last tail!
        else
          new-list to list'
        then
        new-list to list'-last
        list tail@ to list
      then
      1 +to index
    repeat
    list'
  ;

  \ Test whether a predicate applies to all elements of a list; note that not
  \ all elements will be iterated over if an element returns false, and true
  \ with be returned if the list is empty
  : all-list { list xt -- all? } \ xt ( element -- match? )
    begin list while
      list head@ xt execute not if false exit then list tail@ to list
    repeat
    true
  ;

  \ Test whether a predicate applies to all elements of a list; note that not
  \ all elements will be iterated over if an element returns false, and true
  \ with be returned if the list is empty
  : alli-list { list xt -- all? } \ xt ( element index -- match? )
    0 { index }
    begin list while
      list head@ index xt execute not if false exit then list tail@ to list
      1 +to index
    repeat
    true
  ;

  \ Test whether a predicate applies to any element of a list; note that not
  \ all elements will be iterated over if an element returns true, and false
  \ will be returned if the list is empty
  : any-list { list xt -- any? } \ xt ( element -- match? )
    begin list while
      list head@ xt execute if true exit then list tail@ to list
    repeat
    false
  ;

  \ Test whether a predicate applies to any element of a list; note that not
  \ all elements will be iterated over if an element returns true, and false
  \ will be returned if the list is empty
  : anyi-list { list xt -- any? } \ xt ( element index -- match? )
    0 { index }
    begin list while
      list head@ index xt execute if true exit then list tail@ to list
      1 +to index
    repeat
    false
  ;

  \ Fold left over a list
  : foldl-list ( x ) { list xt -- x' } \ xt ( x item -- x' )
    begin list while list head@ xt execute list tail@ to list repeat
  ;

  \ Fold left over a list with an index
  : foldli-list ( x ) { list xt -- x' } \ xt ( x item index -- x' )
    0 { index }
    begin list while
      list head@ index xt execute
      list tail@ to list
      1 +to index
    repeat
  ;

  \ Fold right over a list
  : foldr-list { x list xt -- xt' } \ xt ( item x -- x' )
    x list list>cells xt foldr
  ;

  \ Fold right over a list with an index
  : foldri-list { x list xt -- xt' } \ xt ( item x index -- x' )
    x list list>cells xt foldri
  ;

  \ Collect elements of a list from left to right
  : collectl-list { x len xt -- list } \ xt ( x -- x item )
    empty { list }
    empty { list-last }
    len 0 ?do
      x xt execute empty cons { new-list } to x
      list if
        new-list list-last tail!
      else
        new-list to list
      then
      new-list to list-last
    loop
    list
  ;

  \ Collect elements of a list from left to right with an index
  : collectli-list { x len xt -- list } \ xt ( x index -- x item )
    empty { list }
    empty { list-last }
    len 0 ?do
      x i xt execute empty cons { new-list } to x
      list if
        new-list list-last tail!
      else
        new-list to list
      then
      new-list to list-last
    loop
    list
  ;

  \ Collect elements of a list from right to left
  : collectr-list { x len xt -- list } \ xt ( x -- x item )
    empty { list }
    len 0 ?do x xt execute list cons to list to x loop
    list
  ;

  \ Collect elements of a list from right to left with an index
  : collectri-list { x len xt -- list } \ xt ( x -- x item )
    empty { list }
    len 0 ?do x len i - 1- xt execute list cons to list to x loop
    list
  ;

  \ Sort a list
  : sort-list { list xt -- list' } \ xt ( item0 item1 -- lt? )
    list list>cells dup xt sort! seq>list
  ;

  \ Duplicate a list
  : duplicate-list { list -- list' }
    empty { list' }
    empty { list'-last }
    begin list while
      list head@ empty cons { new-list }
      list' if
        new-list list'-last tail!
      else
        new-list to list'
      then
      new-list to list'-last
      list tail@ to list
    repeat
    list'
  ;
  
  \ Create a reverse list from the stack
  : >rev-list ( xn ... x0 count -- list )
    { count }
    empty { list }
    empty { list-last }
    begin count 0> while
      empty cons { new-list }
      list if
        new-list list-last tail!
      else
        new-list to list
      then
      new-list to list-last
      -1 +to count
    repeat
    list
  ;

  \ Create a list from the stack
  : >list ( x0 ... xn count -- list )
    { count }
    empty begin count 0> while cons -1 +to count repeat
  ;

  \ Explode a list onto the stack
  : list> ( list -- x0 ... xn count )
    0 { list count }
    begin list while list head@ list tail@ to list 1 +to count repeat
    count
  ;

  \ Begin defining a list
  : #[ ( -- ) define-list zscript-internal::begin-seq-define ;

  \ End defining a list
  : ]# ( x0 ... xn -- list )
    define-list zscript-internal::end-seq-define >list
  ;

end-module
