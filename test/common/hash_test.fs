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

begin-module test

  zscript-oo import
  zscript-special-oo import

  : do-test { x -- }
    cr x try-show type space x hash try-show type
  ;
  
  : run-test ( -- )
    0 do-test
    1 do-test
    $BFFF_FFFF do-test
    1. do-test
    #< 0 1 2 ># do-test
    s" foo" do-test
    #( 0 1 2 )# do-test
    #( s" foo" s" bar" s" baz" )# do-test
    1 2 #< 0 1 2 ># >slice do-test
    1 2 s" foo" >slice do-test
    1 2 #( 0 1 2 )# >slice do-test
    1 2 #( s" foo" s" bar" s" baz" )# >slice do-test
    ['] + do-test
    0 ['] + bind do-test
    0 1 2 ['] + bind do-test
    s" +" find do-test
    0 @class do-test
  ;
  
end-module
