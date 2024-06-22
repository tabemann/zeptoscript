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

begin-module pina-colada-test

  zscript-set import
  zscript-map import
  zscript-special-oo import
  zscript-task import
  zscript-chan import

  \ Symbols for our actors
  symbol actor-serve
  symbol actor-get-pink-umbrellas
  symbol actor-get-glasses
  symbol actor-open-blender-1
  symbol actor-liquify
  symbol actor-close-blender
  symbol actor-add-two-cups-ice
  symbol actor-pour-in-rum
  symbol actor-measure-rum
  symbol actor-put-mix-in
  symbol actor-open-mix
  symbol actor-open-blender-0
  symbol actor-make-pina-colada
  symbol start-make-pina-colada
  
  \ Make an actor; size is the number of entries in the actor's channel and
  \ xt is executed for the actor, passed the actors channel, with the actor
  \ terminating when it returns. The actor's channel is returned in the parent
  \ task.
  : make-actor ( size xt -- chan )
    swap make-chan
    fork not if
      swap execute
      terminate
    else
      nip
    then
  ;

  \ Check whether a condition has been met
  : check-cond { conds actor id map -- met? }
    id map find-map not if drop 4 ['] hash ['] equal? make-set then { actors }
    actor actors insert-set
    actors id map insert-map
    conds actors 1 ['] in-set? bind all
  ;

  \ Handle messages and send messages
  : actor-main { chan actors this-actor conds outputs text -- }
    4 ['] hash ['] equal? make-map { map }
    begin
      chan recv pair> { src-actor id }
      conds src-actor id map check-cond if
        cr id show type ." : " text type
        outputs actors this-actor id >pair 2 [: { output actors msg }
          output actors find-map if { dest }
            msg dest send
          else
            drop
          then
        ;] bind iter
        id map remove-map
        yield
      then
    again
  ;
  
  \ Serve a pina colada
  : serve ( chan actors -- )
    actor-serve
    #( actor-get-pink-umbrellas actor-get-glasses actor-open-blender-1 )#
    #( )#
    s" Served a pina colada!"
    actor-main
  ;

  \ Get pink umbrellas
  : get-pink-umbrellas ( chan actors -- )
    actor-get-pink-umbrellas
    #( actor-make-pina-colada )#
    #( actor-serve )#
    s" Got pink umbrellas"
    actor-main
  ;

  \ Get glasses
  : get-glasses ( chan actors -- )
    actor-get-glasses
    #( actor-make-pina-colada )#
    #( actor-serve )#
    s" Got glasses "
    actor-main
  ;

  \ Open the blender a second time
  : open-blender-1 ( chan actors -- )
    actor-open-blender-1
    #( actor-liquify )#
    #( actor-serve )#
    s" Opened the blender a second time"
    actor-main
  ;

  \ Liquify
  : liquify ( chan actors -- )
    actor-liquify
    #( actor-close-blender )#
    #( actor-open-blender-1 )#
    s" Liquified"
    actor-main
  ;

  \ Close the blender
  : close-blender ( chan actors -- )
    actor-close-blender
    #( actor-add-two-cups-ice actor-pour-in-rum actor-put-mix-in )#
    #( actor-liquify )#
    s" Closed the blender"
    actor-main
  ;

  \ Add two cups ice
  : add-two-cups-ice ( chan actors -- )
    actor-add-two-cups-ice
    #( actor-open-blender-0 )#
    #( actor-close-blender )#
    s" Added two cups ice"
    actor-main
  ;

  \ Pour in rum
  : pour-in-rum ( chan actors -- )
    actor-pour-in-rum
    #( actor-measure-rum actor-open-blender-0 )#
    #( actor-close-blender )#
    s" Poured in rum"
    actor-main
  ;

  \ Measure rum
  : measure-rum ( chan actors -- )
    actor-measure-rum
    #( actor-make-pina-colada )#
    #( actor-pour-in-rum )#
    s" Measured rum"
    actor-main
  ;

  \ Put mix in
  : put-mix-in ( chan actors -- )
    actor-put-mix-in
    #( actor-open-mix actor-open-blender-0 )#
    #( actor-close-blender )#
    s" Put mix in"
    actor-main
  ;

  \ Open mix
  : open-mix ( chan actors -- )
    actor-open-mix
    #( actor-make-pina-colada )#
    #( actor-put-mix-in )#
    s" Opened mix"
    actor-main
  ;

  \ Open the blender the first itme
  : open-blender-0 ( chan actors -- )
    actor-open-blender-0
    #( actor-make-pina-colada )#
    #( actor-add-two-cups-ice actor-pour-in-rum actor-put-mix-in )#
    s" Opened the blender the first time"
    actor-main
  ;

  \ Make a pina colada
  : make-pina-colada ( chan actors -- )
    actor-make-pina-colada
    #( start-make-pina-colada )#
    #( actor-get-pink-umbrellas actor-get-glasses actor-measure-rum
    actor-open-mix actor-open-blender-0 )#
    s" Making a pina colada..."
    actor-main
  ;

  \ Set up a pina colada maker.
  : make-pina-colada-maker ( -- chan )
    32 ['] hash ['] equal? make-map { actors }
    3 actors 1 ['] serve bind make-actor
    actor-serve actors insert-map
    1 actors 1 ['] get-pink-umbrellas bind make-actor
    actor-get-pink-umbrellas actors insert-map
    1 actors 1 ['] get-glasses bind make-actor
    actor-get-glasses actors insert-map
    1 actors 1 ['] open-blender-1 bind make-actor
    actor-open-blender-1 actors insert-map
    1 actors 1 ['] liquify bind make-actor
    actor-liquify actors insert-map
    3 actors 1 ['] close-blender bind make-actor
    actor-close-blender actors insert-map
    1 actors 1 ['] add-two-cups-ice bind make-actor
    actor-add-two-cups-ice actors insert-map
    2 actors 1 ['] pour-in-rum bind make-actor
    actor-pour-in-rum actors insert-map
    1 actors 1 ['] measure-rum bind make-actor
    actor-measure-rum actors insert-map
    2 actors 1 ['] put-mix-in bind make-actor
    actor-put-mix-in actors insert-map
    1 actors 1 ['] open-mix bind make-actor
    actor-open-mix actors insert-map
    1 actors 1 ['] open-blender-0 bind make-actor
    actor-open-blender-0 actors insert-map
    1 actors 1 ['] make-pina-colada bind make-actor
  ;

  \ Run the test
  : run-test { id -- }
    start-make-pina-colada id >pair make-pina-colada-maker send yield
  ;
  
end-module
