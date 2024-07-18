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

begin-module zscript-weak
  
  \ Broken weak reference
  symbol broken-weak

  \ Is something a weak reference
  : weak? ( weak -- flag ) >type weak-type = ;

  \ Is something a weak pair
  : weak-pair? ( weak -- flag )
    dup weak? if
      zscript-internal::>size unsafe::>integral 3 cells =
    else
      drop false
    then
  ;
  
  \ Create a weak reference
  : >weak ( x -- weak ) zscript-internal::allocate-weak ;

  \ Create a weak pair
  : >weak-pair ( x y -- weak-pair ) zscript-internal::allocate-weak-pair ;

  \ Get the value of a weak reference
  : weak@ { weak -- x }
    weak weak? averts x-incorrect-type
    weak forth::cell+ forth::@
    dup zscript-internal::broken-weak forth::= if drop broken-weak then
  ;
  
  \ Set the value of a weak reference
  : weak! { x weak -- }
    weak weak? averts x-incorrect-type
    x weak forth::cell+ forth::!
  ;

  \ Get whether a weak reference is broken
  : weak-broken? { weak -- broken? }
    weak weak? averts x-incorrect-type
    weak forth::cell+ forth::@ zscript-internal::broken-weak forth::=
  ;

  \ Get the tail value of a weak pair
  : weak-pair-tail@ { weak-pair -- tail }
    weak-pair weak-pair? averts x-incorrect-type
    weak-pair 2 cells unsafe::integral> forth::+ forth::@
  ;

  \ Set the tail value of a weak pair
  : weak-pair-tail! { tail weak-pair -- }
    weak-pair weak-pair? averts x-incorrect-type
    tail weak-pair 2 cells unsafe::integral> forth::+ forth::!
  ;
  
end-module
