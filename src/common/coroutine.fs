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

begin-module zscript-coroutine

  zscript-list import
  
  \ The coroutine is suspended
  symbol suspended
  
  \ The coroutine is running
  symbol running

  \ The coroutine is dead
  symbol dead

  \ Not in coroutine exception
  : x-not-in-coroutine ( -- ) ." not in coroutine" cr ;

  \ Coroutine is dead
  : x-dead-coroutine ( -- ) ." coroutine is dead" cr ;

  \ Coroutine is running
  : x-running-coroutine ( -- ) ." coroutine is running" cr ;
  
  begin-module zscript-coroutine-internal
    
    \ The current coroutine
    global current-coroutine
    
    \ The non-coroutine-local data
    global non-coroutine-local
    
    \ Initial coroutine return value
    symbol init-coroutine
    
    \ The coroutine type
    begin-record coroutine
      
      \ The coroutine state
      item: coroutine-state

      \ The coroutine saved state
      item: coroutine-save
      
      \ The coroutine's parent state
      item: coroutine-parent
      
      \ Coroutine local data
      item: coroutine-local
      
    end-record
    
  end-module> import

  \ Create a coroutine
  : make-coroutine { xt -- coroutine }
    current-coroutine@ { current-coroutine }
    current-coroutine 0<> if
      current-coroutine coroutine-local@
    else
      non-coroutine-local@
    then { coroutine-local }
    suspended 0 0 coroutine-local >coroutine { coroutine }
    coroutine 1 [: coroutine-save! init-coroutine ;] bind save
    dup init-coroutine = if
      drop coroutine
    else
      running coroutine coroutine-state!
      coroutine current-coroutine!
      xt execute
      dead coroutine coroutine-state!
      0 current-coroutine!
      coroutine coroutine-parent@ execute
    then
  ;

  \ Create a dead coroutine
  : make-dead-coroutine ( -- coroutine ) dead 0 0 0 >coroutine ;
  
  \ Suspend a coroutine
  : suspend ( x -- x' )
    current-coroutine@ { coroutine }
    coroutine 0<> averts x-not-in-coroutine
    suspended coroutine coroutine-state!
    coroutine 2 [: { state x coroutine }
      state coroutine coroutine-save!
      x coroutine coroutine-parent@ execute
    ;] bind save
    coroutine current-coroutine!
    running coroutine coroutine-state!
  ;

  \ Resume a coroutine
  : resume ( x coroutine -- x' )
    dup coroutine-state@ dup suspended = if
      drop current-coroutine@ { current-coroutine }
      2 [: { state x coroutine }
        state coroutine coroutine-parent!
        x coroutine coroutine-save@ execute
      ;] bind save
      current-coroutine current-coroutine!
    else
      running = triggers x-running-coroutine
      ['] x-dead-coroutine ?raise
    then
  ;
  
  \ Get the current coroutine
  : current-coroutine ( -- coroutine ) current-coroutine@ ;
  
  \ Get the state of a coroutine
  : coroutine-state@ ( coroutine -- state ) coroutine-state@ ;
  
  \ Get the coroutine-local state
  : coroutine-local@ ( -- x )
    current-coroutine@ { coroutine }
    coroutine 0<> if
      coroutine coroutine-local@
    else
      non-coroutine-local@
    then
  ;

  \ Set the coroutine-local state
  : coroutine-local! { x -- }
    current-coroutine@ { coroutine }
    coroutine 0<> if
      x coroutine coroutine-local!
    else
      x non-coroutine-local!
    then
  ;


  \ Iterate over a coroutine
  : iter-coroutine { coroutine xt -- } \ xt: ( item -- )
    0 { index }
    begin coroutine coroutine-state@ dead <> while
      index coroutine resume xt execute
      1 +to index
    repeat
  ;

  \ Iterate over a coroutine with an index
  : iteri-coroutine { coroutine xt -- } \ xt: ( item index -- )
    0 { index }
    begin coroutine coroutine-state@ dead <> while
      index coroutine resume index xt execute
      1 +to index
    repeat
  ;

  \ Map a coroutine to a list
  : map>list-coroutine { coroutine xt -- list } \ xt: ( item -- item' )
    empty { list }
    empty { list-last }
    0 { index }
    begin coroutine coroutine-state@ dead <> while
      index coroutine resume xt execute empty cons { new-list }
      list if
        new-list list-last tail!
      else
        new-list to list
      then
      new-list to list-last
      1 +to index
    repeat
    list
  ;

  \ Map a coroutine to a list with an index
  : mapi>list-coroutine { coroutine xt -- list } \ xt: ( item index -- item' )
    empty { list }
    empty { list-last }
    0 { index }
    begin coroutine coroutine-state@ dead <> while
      index coroutine resume index xt execute empty cons { new-list }
      list if
        new-list list-last tail!
      else
        new-list to list
      then
      new-list to list-last
      1 +to index
    repeat
    list
  ;

  \ Filter a coroutine to a list
  : filter>list-coroutine { coroutine xt -- list } \ xt: ( item -- flag )
    empty { list }
    empty { list-last }
    0 { index }
    begin coroutine coroutine-state@ dead <> while
      index coroutine resume dup { val } xt execute if
        val empty cons { new-list }
        list if
          new-list list-last tail!
        else
          new-list to list
        then
        new-list to list-last
      then
      1 +to index
    repeat
    list
  ;

  \ Filter a coroutine to a list with an index
  : filteri>list-coroutine { coroutine xt -- list } \ xt: ( item index -- flag )
    empty { list }
    empty { list-last }
    0 { index }
    begin coroutine coroutine-state@ dead <> while
      index coroutine resume dup { val } index xt execute if
        val empty cons { new-list }
        list if
          new-list list-last tail!
        else
          new-list to list
        then
        new-list to list-last
      then
      1 +to index
    repeat
    list
  ;

  \ Collect a coroutine to a list
  : collectl>list-coroutine { coroutine -- list }
    empty { list }
    empty { list-last }
    0 { index }
    begin coroutine coroutine-state@ dead <> while
      index coroutine resume empty cons { new-list }
      list if
        new-list list-last tail!
      else
        new-list to list
      then
      new-list to list-last
      1 +to index
    repeat
    list
  ;

  \ Fold left a coroutine
  : foldl-coroutine { x coroutine xt -- x' } \ xt: ( x item -- x' )
    0 { index }
    begin coroutine coroutine-state@ dead <> while
      x index coroutine resume xt execute to x
      1 +to index
    repeat
    x
  ;

end-module
