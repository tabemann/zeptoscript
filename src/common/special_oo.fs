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

begin-module zscript-special-oo

  zscript-oo import
  
  \ Show method
  method show ( object -- bytes )
  
  \ Try to show word
  : try-show ( object -- bytes )
    ['] show over has-method? if
      show
    else
      drop s" <object>"
    then
  ;

  \ Type class for nulls
  null-type begin-type-class

    \ Show a null
    :method show { self -- } self format-integral ;
    
  end-class

  \ Type class for small ints
  int-type begin-type-class

    \ Show a small int
    :method show { self -- } self format-integral ;
    
  end-class

  \ Type class for big ints
  big-int-type begin-type-class

    \ Show a big int
    :method show { self -- } self format-integral ;

  end-class

  \ Type class for symbols
  symbol-type begin-type-class

    \ Show a big int
    :method show { self -- } self symbol>name ;

  end-class

  defined? zscript-double [if]
    
    \ Type class for doubles
    double-type begin-type-class
      
      \ Show a big int
      :method show { self -- } self format-double ;
      
    end-class

  [then]

  \ Type class for byte sequences
  bytes-type begin-type-class

    \ Show a byte sequence
    :method show { self -- }
      self >len { len }
      len 2 + make-cells { seq }
      s" #<" 0 seq !+
      self seq 1 [: { byte index seq } byte try-show index 1+ seq !+ ;]
      bind iteri
      s" >#" len 1+ seq !+
      seq s"  " join
    ;
    
  end-class

  \ Type class for byte sequences
  const-bytes-type begin-type-class

    \ Show a constant byte sequence
    :method show { self -- }
      self >len { len }
      len 2 + make-cells { seq }
      s" #const<" 0 seq !+
      self seq 1 [: { byte index seq } byte try-show index 1+ seq !+ ;]
      bind iteri
      s" >#" len 1+ seq !+
      seq s"  " join
    ;
    
  end-class

  \ Type class for byte sequences
  cells-type begin-type-class

    \ Show a cell sequence
    :method show { self -- }
      self >len { len }
      len 2 + make-cells { seq }
      s" #(" 0 seq !+
      self seq 1 [: { element index seq } element try-show index 1+ seq !+ ;]
      bind iteri
      s" )#" len 1+ seq !+
      seq s"  " join
    ;
    
  end-class
  
  \ Type class for slices
  slice-type begin-type-class

    \ Show a slice
    :method show { self -- }
      self >raw >type cells-type = if
        self >len { len }
        len 2 + make-cells { seq }
        s" #slice(" 0 seq !+
        self seq 1 [: { element index seq } element try-show index 1+ seq !+ ;]
        bind iteri
        s" )#" len 1+ seq !+
        seq s"  " join
      else
        self >raw >type bytes-type = if
          self >len { len }
          len 2 + make-cells { seq }
          s" #slice<" 0 seq !+
          self seq 1 [: { byte index seq } byte try-show index 1+ seq !+ ;]
          bind iteri
          s" >#" len 1+ seq !+
          seq s"  " join
        else
          self >len { len }
          len 2 + make-cells { seq }
          s" #const-slice<" 0 seq !+
          self seq 1 [: { byte index seq } byte try-show index 1+ seq !+ ;]
          bind iteri
          s" >#" len 1+ seq !+
          seq s"  " join
        then
      then
    ;
    
  end-class
  
  \ Type class for xt's
  xt-type begin-type-class

    \ Show an xt
    :method show { self -- }
      s" xt:"
      self 16 [: unsafe::xt>integral format-integral-unsigned ;] with-base
      concat
    ;

  end-class

  \ Type class for closures's
  closure-type begin-type-class
    
    \ Show a closure
    :method show { self -- }
      self zscript-internal::>size unsafe::>integral
      2 cells - 2 rshift { bound }
      bound 2 * 4 + make-cells { seq }
      s" closure:" 0 seq !+
      self unsafe::>integral cell+ unsafe::@
      16 [: format-integral-unsigned ;] with-base 1 seq !+
      s" :(" 2 seq !+
      bound 0 ?do
        s"  " i 2 * 3 + seq !+
        self unsafe::>integral bound 1- i - 2 + cells + unsafe::@
        unsafe::integral> try-show
        i 2 * 4 + seq !+
      loop
      s"  )" bound 2 * 3 + seq !+
      seq 0bytes join
    ;
    
  end-class

  \ Type class for tagged values
  tagged-type begin-type-class

    \ Show a tagged value
    :method show { self -- }
      self zscript-internal::>tag dup { tag } zscript-internal::word-tag = if
        s" xt:"
        self 16 [:
          0 swap zscript-internal::t@+ format-integral-unsigned
        ;] with-base
        concat
      else
        3 make-cells { seq }
        s" <unknown tag " 0 seq !+
        tag 16 [: format-integral-unsigned ;] with-base 1 seq !+
        s" >" 2 seq !+
        seq 0bytes join
      then
    ;

  end-class

  
end-module
