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

begin-module pie-a-la-mode-test

  zscript-special-oo import
  zscript-task import
  zscript-uchan import
  zscript-queue import
  zscript-list import
  zscript-map import

  \ Request the ability to order food
  symbol request-ordering

  \ Ordering granted
  symbol ordering-granted

  \ Done ordering food
  symbol done-ordering
  
  \ Request a kind of food
  symbol request-food
  begin-record food-request
    item: requesting-waiter
    item: food-request-quantity
  end-record

  \ Can serve request
  symbol able-to-serve-food
  
  \ Unable to serve request
  symbol unable-to-serve-food

  \ Get a kind of food
  symbol get-food

  \ Try to order food
  symbol order-food
  begin-record food-order
    item: ordering-customer
    item: order-map
  end-record

  \ Able to order food
  symbol able-to-order-food

  \ Unable to order food
  symbol unable-to-order-food

  \ Food is not on menu
  symbol not-on-menu
  
  \ The types of food
  symbol pie
  symbol ice-cream

  \ The waiters
  symbol waiter-0
  symbol waiter-1

  \ The customers
  symbol customer-0
  symbol customer-1
  
  \ Make an actor; xt is executed for the actor, passed the actor's channel,
  \ with the actor terminating when it returns. The actor's channel is returned
  \ in the parent task.
  : make-actor ( xt -- chan )
    make-uchan
    fork not if
      swap execute
      terminate
    else
      nip
    then
  ;

  \ Ordering arbiter
  : arbiter { chan -- }
    false { ordering? }
    make-queue { queue }
    begin
      chan recv pair> swap case
        request-ordering of { requester }
          cr ." Got order request"
          ordering? if
            cr ." Enqueueing order request"
            requester queue enqueue
          else
            cr ." Granting order request"
            true to ordering?
            ordering-granted 0 >pair requester send
          then
        endof
        done-ordering of
          drop
          ordering? if
            queue dequeue if { next-requester }
              cr ." Dequeueing and granting order request"
              ordering-granted 0 >pair next-requester send
            else
              drop false to ordering?
            then
          then
        endof
        nip
      endcase
    again
  ;

  \ Food type actor
  : food { chan name quantity -- }
    begin
      chan recv pair> swap case
        request-food of { request }
          quantity request food-request-quantity@ >= if
            cr name show type ." : Able to serve quantity of "
            request food-request-quantity@ (.)
            able-to-serve-food
          else
            cr name show type ." : Unable to serve quantity of "
            request food-request-quantity@ (.)
            unable-to-serve-food 
          then
          chan >pair request requesting-waiter@ send
        endof
        get-food of { food-get-quantity }
          food-get-quantity negate +to quantity
          cr name show type ." : Got quantity of "
          food-get-quantity show type
          ." , " quantity show type ."  left"
        endof
        nip
      endcase
    again
  ;

  \ Waiter state
  begin-record waiter-state
    item: waiter-chan
    item: waiter-name
    item: waiter-food-map
    item: waiter-arbiter
    item: waiter-ordering?
    item: waiter-waiting-for-arbiter?
    item: waiter-queue
    item: waiter-foods
    item: waiter-recv-foods
    item: waiter-ordering-customer
  end-record

  \ Handle receiving an order
  : handle-order-food { request state -- }
    request state waiter-queue@ enqueue
    cr state waiter-name@ show type ." : Enqueueing "
    request order-map@ [: show type space show type space ;] iter-map
    state waiter-ordering?@ not if
      cr state waiter-name@ show type ." : Requesting ordering"
      true state waiter-ordering?!
      true state waiter-waiting-for-arbiter?!
      request-ordering state waiter-chan@ >pair state waiter-arbiter@ send
    then
  ;

  \ Handle ordering granted
  : handle-order-granted { state -- }
    state waiter-waiting-for-arbiter?@ if
      cr state waiter-name@ show type ." : Ordering granted"
      false state waiter-waiting-for-arbiter?!
      state waiter-queue@ dequeue if { request }
        cr state waiter-name@ show type ." : Dequeueing "
        request order-map@ [: show type space show type space ;] iter-map
        empty state waiter-foods!
        empty state waiter-recv-foods!
        false >ref { food-not-found? }
        request order-map@ state food-not-found? 2 [:
          { quantity food state food-not-found? }
          food state waiter-food-map@ find-map if
            cr state waiter-name@ show type ." : Found food " food show type
            food swap quantity >triple state waiter-foods@ cons
            state waiter-foods!
          else
            cr state waiter-name@ show type ." : Food "
            food show type ." not found"
            drop true food-not-found? ref!
          then
        ;] bind iter-map
        food-not-found? ref@ not if
          request ordering-customer@ state waiter-ordering-customer!
          state waiter-foods@ state 1 [: { triple state }
            triple triple> { food food-actor quantity }
            cr state waiter-name@ show type
            ." : Requesting " food show type
            request-food state waiter-chan@ quantity >food-request >pair
            food-actor send
          ;] bind iter-list
        else
          not-on-menu 0 >pair request ordering-customer@ send
        then
      else
        terminate
      then
    then
  ;
  
  \ Add a food to the received foods
  : add-recv-food { good? food-actor state -- }
    state waiter-ordering?@ state waiter-waiting-for-arbiter?@ not and if
      state waiter-foods@ food-actor 1 [:
        swap triple> { new-food-actor food food-actor quantity }
        new-food-actor new-food-actor =
      ;] bind any-list if
        state waiter-recv-foods@ food-actor 1 ['] <> bind all-list if
          food-actor good? >pair state waiter-recv-foods@ cons
          state waiter-recv-foods!
        then
      then
      good? if
        cr state waiter-name@ show type ." : Able to serve food ("
        state waiter-recv-foods@ list>len (.) ." )"
      else
        cr state waiter-name@ show type ." : Unable to serve food ("
        state waiter-recv-foods@ list>len (.) ." )"
      then
    then
  ;

  \ Check whether all foods have been received
  : recv-all-foods? { state -- all-received? }
    state waiter-ordering?@ state waiter-waiting-for-arbiter?@ not and if
      state waiter-foods@ list>len state waiter-recv-foods@ list>len =
    else
      false
    then
  ;
  
  \ Handle all foods being received
  : handle-recv-all-foods { state -- }
    state waiter-recv-foods@ [: 1 swap @+ ;] all-list if
      state waiter-foods@ state 1 [:
        { state } triple> { food food-actor quantity }
        cr state waiter-name@ show type ." : Getting " food show type
        get-food quantity >pair food-actor send
      ;] bind iter-list
      cr state waiter-name@ show type ." : Ordered food"
      done-ordering 0 >pair state waiter-arbiter@ send
      able-to-order-food 0 >pair state waiter-ordering-customer@ send
    else
      cr state waiter-name@ show type ." : Unable to order food"
      done-ordering 0 >pair state waiter-arbiter@ send
      unable-to-order-food 0 >pair state waiter-ordering-customer@ send
    then
    empty state waiter-foods!
    empty state waiter-recv-foods!
    0 state waiter-ordering-customer!
    false state waiter-ordering?!
  ;

  \ Waiter actor
  : waiter { chan name food-map arbiter -- }
    make-waiter-state { state }
    chan state waiter-chan!
    name state waiter-name!
    food-map state waiter-food-map!
    arbiter state waiter-arbiter!
    false state waiter-ordering?!
    false state waiter-waiting-for-arbiter?!
    make-queue state waiter-queue!
    empty state waiter-foods!
    empty state waiter-recv-foods!
    0 state waiter-ordering-customer!
    begin
      state waiter-chan@ recv pair> swap case
        order-food of state handle-order-food endof
        ordering-granted of drop state handle-order-granted endof
        able-to-serve-food of true swap state add-recv-food endof
        unable-to-serve-food of false swap state add-recv-food endof
        nip
      endcase
      state recv-all-foods? if state handle-recv-all-foods then
    again
  ;

  \ Customer
  : customer { chan name order-map waiter -- }
    cr name show type ." : Ordering "
    order-map [: show type space show type space ;] iter-map
    order-food chan order-map >food-order >pair waiter send
    begin
      chan recv pair> drop case
        able-to-order-food of
          cr name show type ." : Ordered food"
        endof
        unable-to-order-food of
          cr name show type ." : Unable to order food"
        endof
        nip
      endcase
    again
  ;
  
  \ Run the test
  : run-test ( -- )
    ['] arbiter make-actor { arbiter-actor }
    pie 3 2 ['] food bind make-actor { pie-actor }
    ice-cream 2 2 ['] food bind make-actor { ice-cream-actor }
    #{ pie pie-actor ice-cream ice-cream-actor }# { food-map }
    waiter-0 food-map arbiter-actor 3 ['] waiter bind
    make-actor { waiter-0-actor }
    waiter-1 food-map arbiter-actor 3 ['] waiter bind
    make-actor { waiter-1-actor }
    customer-0 #{ pie 1 ice-cream 1 }# waiter-0-actor 3 ['] customer bind
    make-actor drop
    customer-1 #{ pie 2 ice-cream 2 }# waiter-1-actor 3 ['] customer bind
    make-actor drop
    yield
  ;
  
end-module
