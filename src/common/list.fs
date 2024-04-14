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
  
  zscript import

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

  \ Get the length of a list
  : list>len { list -- len }
    0 begin list while 1+ list tail@ to list repeat
  ;
  
  \ Convert a list to a sequence
  : list>seq { list -- seq }
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

  \ Reverse a list and convert it to a sequence
  : rev-list>seq { list -- seq }
    list list>len { len }
    len make-cells { seq }
    len { index }
    begin index 0> while
      -1 +to index list head@ index seq !+ list tail@ to list
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
      list head@ xt execute 0 cons { new-list }
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
      list head@ index xt execute 0 cons { new-list }
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

  \ Filter a list
  : filter-list { list xt -- list' } \ xt ( item -- flag )
    empty { list' }
    empty { list'-last }
    begin list while
      list head@ dup { head } xt execute if
        head 0 cons { new-list }
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
        head 0 cons { new-list }
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
    x list list>seq xt foldr
  ;

  \ Fold right over a list with an index
  : foldri-list { x list xt -- xt' } \ xt ( item x index -- x' )
    x list list>seq xt foldri
  ;

  \ Sort a list
  : sort-list { list xt -- list' } \ xt ( item0 item1 -- lt? )
    list list>seq dup xt sort! seq>list
  ;

end-module
