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
  
  : do-test { a b equal -- }
    cr a try-show type space b try-show type ."  equal? "
    equal not if ." not " then
    a b try-equal? equal = if ." PASS " else ." FAIL " then
  ;
  
  : run-test ( -- )
    0 0 true do-test
    0 1 false do-test
    1 0 false do-test
    1 1 true do-test
    2 1 false do-test
    1 2 false do-test
    $BFFF_FFFF $BFFF_FFFF true do-test
    0 $BFFF_FFFF false do-test
    $BFFF_FFFF 0 false do-test
    1 $BFFF_FFFF false do-test
    $BFFF_FFFF 1 false do-test
    0. 0. true do-test
    1. 1. true do-test
    0. 1. false do-test
    0 1. false do-test
    1. 0 false do-test
    1 1. false do-test
    1. 1 false do-test
    $BFFF_FFFF 1. false do-test
    1. $BFFF_FFFF false do-test
    #< 0 1 2 ># #< 0 1 2 ># true do-test
    #< 0 1 2 ># #< 0 1 3 ># false do-test
    #< 0 1 2 ># #< 0 1 2 3 ># false do-test
    0 #< 0 1 2 ># false do-test
    #< 0 1 2 ># 0 false do-test
    #< 0 1 2 ># 1 false do-test
    #< 0 1 2 ># $BFFF_FFFF false do-test
    #< 0 1 2 ># 1. false do-test
    s" foo" s" foo" true do-test
    s" foo" s" bar" false do-test
    s" foo" s" fooo" false do-test
    s" foo" s" foo" duplicate true do-test
    s" foo" s" bar" duplicate false do-test
    s" foo" duplicate s" foo" true do-test
    s" foo" duplicate s" bar" false do-test
    0 s" foo" false do-test
    1 s" foo" false do-test
    $BFFF_FFFF s" foo" false do-test
    1. s" foo" false do-test
    s" foo" 0 false do-test
    s" foo" 1 false do-test
    s" foo" $BFFF_FFFF false do-test
    s" foo" 1. false do-test
    #( 0 1 2 )# #( 0 1 2 )# true do-test
    #( 0 1 2 )# #( 0 1 3 )# false do-test
    #( 0 1 2 )# #( 0 1 2 3 )# false do-test
    0 #( 0 1 2 )# false do-test
    1 #( 0 1 2 )# false do-test
    $BFFF_FFFF #( 0 1 2 )# false do-test
    1. #( 0 1 2 )# false do-test
    #< 0 1 2 ># #( 0 1 2 )# false do-test
    s" foo" #( 0 1 2 )# false do-test
    #( 0 1 2 )# 0 false do-test
    #( 0 1 2 )# 1 false do-test
    #( 0 1 2 )# $BFFF_FFFF false do-test
    #( 0 1 2 )# 1. false do-test
    #( 0 1 2 )# #< 0 1 2 ># false do-test
    #( 0 1 2 )# s" foo" false do-test
    1 2 #( 0 1 2 )# >slice 1 2 #( 0 1 2 )# >slice true do-test
    1 2 #( 0 1 2 )# >slice 1 2 #( 0 1 3 )# >slice false do-test
    1 2 #( 0 1 2 )# >slice 1 3 #( 0 1 2 3 )# >slice false do-test
    1 2 #( 0 1 2 )# >slice 0 false do-test
    1 2 #( 0 1 2 )# >slice 1 false do-test
    1 2 #( 0 1 2 )# >slice $BFFF_FFFF false do-test
    1 2 #( 0 1 2 )# >slice 1. false do-test
    0 1 2 #( 0 1 2 )# >slice false do-test
    1 1 2 #( 0 1 2 )# >slice false do-test
    $BFFF_FFFF 1 2 #( 0 1 2 )# >slice false do-test
    1. 1 2 #( 0 1 2 )# >slice false do-test
    1 2 #< 0 1 2 ># >slice 1 2 #< 0 1 2 ># >slice true do-test
    1 2 #< 0 1 2 ># >slice 1 2 #< 0 1 3 ># >slice false do-test
    1 2 #< 0 1 2 ># >slice 1 3 #< 0 1 2 3 ># >slice false do-test
    1 2 #< 0 1 2 ># >slice 0 false do-test
    1 2 #< 0 1 2 ># >slice 1 false do-test
    1 2 #< 0 1 2 ># >slice $BFFF_FFFF false do-test
    1 2 #< 0 1 2 ># >slice 1. false do-test
    0 1 2 #< 0 1 2 ># >slice false do-test
    1 1 2 #< 0 1 2 ># >slice false do-test
    $BFFF_FFFF 1 2 #< 0 1 2 ># >slice false do-test
    1. 1 2 #< 0 1 2 ># >slice false do-test
    1 2 s" foo" >slice 1 2 s" foo" >slice true do-test
    1 2 s" foo" >slice 1 2 s" bar" >slice false do-test
    1 2 s" foo" >slice 1 3 s" fooo" >slice false do-test
    1 2 s" foo" >slice 0 false do-test
    1 2 s" foo" >slice 1 false do-test
    1 2 s" foo" >slice $BFFF_FFFF false do-test
    1 2 s" foo" >slice 1. false do-test
    0 1 2 s" foo" >slice false do-test
    1 1 2 s" foo" >slice false do-test
    $BFFF_FFFF 1 2 s" foo" >slice false do-test
    1. 1 2 s" foo" >slice false do-test
    ['] + ['] + true do-test
    ['] + ['] - false do-test
    0 ['] + false do-test
    1 ['] + false do-test
    $BFFF_FFFF ['] + false do-test
    1. ['] + false do-test
    #( 0 1 2 )# ['] + false do-test
    #< 0 1 2 ># ['] + false do-test
    s" foo" ['] + false do-test
    1 2 #( 0 1 2 )# >slice ['] + false do-test
    1 2 #< 0 1 2 ># >slice ['] + false do-test
    1 2 s" foo" >slice ['] + false do-test
    ['] + 0 false do-test
    ['] + 1 false do-test
    ['] + $BFFF_FFFF false do-test
    ['] + 1. false do-test
    ['] + #( 0 1 2 )# false do-test
    ['] + #< 0 1 2 ># false do-test
    ['] + s" foo" ['] + false do-test
    ['] + 1 2 #( 0 1 2 )# >slice false do-test
    ['] + 1 2 #< 0 1 2 ># >slice false do-test
    ['] + 1 2 s" foo" >slice false do-test
    0 1 2 ['] + bind 0 1 2 ['] + bind true do-test
    0 1 2 ['] + bind 0 2 2 ['] + bind false do-test
    0 1 2 ['] + bind 0 1 2 3 ['] + bind false do-test
    0 ['] + bind ['] + true do-test
    ['] + 0 ['] + bind true do-test
    0 ['] + bind ['] - false do-test
    ['] - 0 ['] + bind false do-test
    0 1 2 ['] + bind ['] - false do-test
    0 0 1 2 ['] + bind false do-test
    1 0 1 2 ['] + bind false do-test
    $BFFF_FFFF 0 1 2 ['] + bind false do-test
    1. 0 1 2 ['] + bind false do-test
    #( 0 1 2 )# 0 1 2 ['] + bind false do-test
    #< 0 1 2 ># 0 1 2 ['] + bind false do-test
    s" foo" 0 1 2 ['] + bind false do-test
    1 2 #( 0 1 2 )# >slice 0 1 2 ['] + bind false do-test
    1 2 #< 0 1 2 ># >slice 0 1 2 ['] + bind false do-test
    1 2 s" foo" >slice 0 1 2 ['] + bind false do-test
    0 1 2 ['] + bind 0 false do-test
    0 1 2 ['] + bind 1 false do-test
    0 1 2 ['] + bind $BFFF_FFFF false do-test
    0 1 2 ['] + bind 1. false do-test
    0 1 2 ['] + bind #( 0 1 2 )# false do-test
    0 1 2 ['] + bind #< 0 1 2 ># false do-test
    0 1 2 ['] + bind s" foo" ['] + false do-test
    0 1 2 ['] + bind 1 2 #( 0 1 2 )# >slice false do-test
    0 1 2 ['] + bind 1 2 #< 0 1 2 ># >slice false do-test
    0 1 2 ['] + bind 1 2 s" foo" >slice false do-test
    s" +" find s" +" find true do-test
    s" +" find s" -" find false do-test
    0 s" +" find false do-test
    1 s" +" find false do-test
    $BFFF_FFFF s" +" find false do-test
    1. s" +" find false do-test
    #( 0 1 2 )# s" +" find false do-test
    #< 0 1 2 ># s" +" find false do-test
    s" foo" s" +" find false do-test
    1 2 #( 0 1 2 )# >slice s" +" find false do-test
    1 2 #< 0 1 2 ># >slice s" +" find false do-test
    1 2 s" foo" >slice s" +" find false do-test
    ['] + s" +" find false do-test
    0 1 2 ['] + bind s" +" find false do-test
    s" +" find 0 false do-test
    s" +" find 1 false do-test
    s" +" find $BFFF_FFFF false do-test
    s" +" find 1. false do-test
    s" +" find #( 0 1 2 )# false do-test
    s" +" find #< 0 1 2 ># false do-test
    s" +" find s" foo" false do-test
    s" +" find 1 2 #( 0 1 2 )# >slice false do-test
    s" +" find 1 2 #< 0 1 2 ># >slice false do-test
    s" +" find 1 2 s" foo" >slice false do-test
    s" +" find ['] + false do-test
    s" +" find 0 1 2 ['] + bind false do-test
    0 class@ 0 class@ true do-test
    0 class@ 1 class@ false do-test
    0 class@ s" +" find false do-test
    s" +" find 0 class@ false do-test
    0 0 class@ false do-test
    1 0 class@ false do-test
    $BFFF_FFFF 0 class@ false do-test
    1. 0 class@ false do-test
    #( 0 1 2 )# 0 class@ false do-test
    #< 0 1 2 ># 0 class@ false do-test
    s" foo" 0 class@ false do-test
    1 2 #( 0 1 2 )# >slice 0 class@ false do-test
    1 2 #< 0 1 2 ># >slice 0 class@ false do-test
    1 2 s" foo" >slice 0 class@ false do-test
    ['] + 0 class@ false do-test
    0 1 2 ['] + bind 0 class@ false do-test
    0 class@ 0 false do-test
    0 class@ 1 false do-test
    0 class@ $BFFF_FFFF false do-test
    0 class@ 1. false do-test
    0 class@ #( 0 1 2 )# false do-test
    0 class@ #< 0 1 2 ># false do-test
    0 class@ s" foo" false do-test
    0 class@ 1 2 #( 0 1 2 )# >slice false do-test
    0 class@ 1 2 #< 0 1 2 ># >slice false do-test
    0 class@ 1 2 s" foo" >slice false do-test
    0 class@ ['] + false do-test
    0 class@ 0 1 2 ['] + bind false do-test
;
  
end-module
  