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

begin-module coroutine-iter-test

  zscript-coroutine import
  zscript-list import

  : test-coroutine ( -- )
    [: begin dup 10 < while suspend repeat ;] make-coroutine
  ;

  : run-test ( -- )
    cr ." iter-coroutine test: "
    test-coroutine ['] . iter-coroutine

    cr ." iteri-coroutine test: "
    test-coroutine [: + . ;] iteri-coroutine

    cr ." map>list-coroutine test: "
    test-coroutine [: 2 * ;] map>list-coroutine ['] . iter-list

    cr ." mapi>list-coroutine test: "
    test-coroutine [: swap 2 * + ;] mapi>list-coroutine ['] . iter-list

    cr ." filter>list-coroutine test: "
    test-coroutine [: 2 mod 0= ;] filter>list-coroutine ['] . iter-list

    cr ." filteri>list-coroutine test: "
    test-coroutine [: + 3 mod 0= ;] filteri>list-coroutine ['] . iter-list

    cr ." collectl>list-coroutine test: "
    test-coroutine collectl>list-coroutine ['] . iter-list

    cr ." foldl-coroutine test: "
    0 test-coroutine ['] + foldl-coroutine .
  ;
  
end-module
