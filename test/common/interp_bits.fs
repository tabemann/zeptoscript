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

begin-module interp-bits
  
  zscript-list import
  
  \ Split a byte sequence into parts based on a single-byte delimiter
  : split { string delimiter -- parts }
    0 { parts }
    begin
      string delimiter 1 ['] = bind find-index if { index }
        0 index string >slice duplicate parts cons to parts
        index 1+ string >len over - string >slice to string
        false
      else
        drop
        string duplicate parts cons rev-list>cells
        true
      then
    until
  ;
  
  \ Add gaps between elements of a cell sequence
  : add-gaps { parts -- parts-with-gaps }
    parts >len { len }
    len 0> if
      len 1- 2 * 1+ make-cells { parts-with-gaps }
      parts parts-with-gaps 1 [: { part index parts-with-gaps }
        part index 2 * parts-with-gaps !+
      ;] bind iteri
      parts-with-gaps
    else
      0cells
    then
  ;
  
  \ Take a cell sequence and create a new cell sequence with 1's and 0's
  \ representing bits inserted in between in all permutations.
  : insert-bits { parts -- parts-with-bits }
    parts >len { len }
    len 0> if
      parts add-gaps { parts-with-gaps }
      s" 0" s" 1" { 0bit 1bit }
      0 { variants }
      1 len 1- lshift 0 ?do
        parts-with-gaps duplicate { these-parts }
        len 1- 0 ?do
          j i rshift 1 and 0<> if 1bit else 0bit then
          len 1- i - 2 * 1- these-parts !+
        loop
        these-parts variants cons to variants
      loop
      variants rev-list>cells
    else
      0cells
    then
  ;
  
  \ Take a byte sequence and return a sequence of byte sequences where
  \ each . is replaced with all permutations of 1's and 0's representing
  \ bits.
  : variants-with-bits { string -- strings }
    string [char] . split insert-bits [: 0bytes join ;] map
  ;
  
end-module
