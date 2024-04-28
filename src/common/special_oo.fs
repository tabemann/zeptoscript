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
      s" unknown:"
      swap 16 [: unsafe::>integral format-integral-unsigned ;] with-base
      concat
    then
  ;

  \ Hash method
  method hash ( object -- hash )

  \ Try to hash word
  : try-hash ( object -- hash )
    ['] hash over has-method? if
      hash
    else
      0
    then
  ;

  \ Equality method
  method equal? ( object1 object0 -- equal? )

  \ Try to test for equality
  : try-equal? ( object1 object0 -- equal? )
    ['] equal? over has-method? if
      equal?
    else
      =
    then
  ;

  \ Type class for nulls
  null-type begin-type-class

    \ Show a null
    :method show { self -- } self format-integral ;

    \ Hash a null
    :method hash { self -- } 0 ;

    \ Test a null for equality
    :method equal? { other self -- equal? } other 0= ;
    
  end-class

  \ Type class for small ints
  int-type begin-type-class

    \ Show a small int
    :method show { self -- } self format-integral ;

    \ Hash a small int
    :method hash { self -- } self ;

    \ Test a small int for equality
    :method equal? { other self -- equal? } other self = ;
    
  end-class

  \ Type class for big ints
  big-int-type begin-type-class

    \ Show a big int
    :method show { self -- } self format-integral ;

    \ Hash a big int
    :method hash { self -- } self ;

    \ Test a big int for equality
    :method equal? { other self -- equal? } other self = ;
    
  end-class

  \ Type class for symbols
  symbol-type begin-type-class

    \ Show a symbol
    :method show { self -- } self symbol>name ;

    \ Hash a symbol
    :method hash { self -- } self symbol>integral ;

    \ Test a symbol for equality
    :method equal? { other self -- equal? } other self = ;
    
  end-class

  defined? zscript-double [if]
    
    \ Type class for doubles
    double-type begin-type-class
      
      \ Show a double
      :method show { self -- } self zscript-double::format-double s" ." concat ;

      \ Hash a double
      :method hash { self -- } self double>2integral xor ;

      \ Test a double for equality
      :method equal? { other self -- equal? }
        other >type double-type = if
          other self zscript-double::d=
        else
          false
        then
      ;
      
    end-class

  [else]

    \ Type class for doubles
    double-type begin-type-class

      \ Hash a double
      :method hash { self -- } self double>2integral xor ;
      
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

    \ Hash a byte sequence
    :method hash { self -- } self hash-bytes ;

    \ Test a byte sequence for equality
    :method equal? { other self -- equal? }
      other bytes? if
        other self equal-bytes?
      else
        false
      then
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

    \ Hash a constant byte sequence
    :method hash { self -- } self hash-bytes ;
    
    \ Test a constant byte sequence for equality
    :method equal? { other self -- equal? }
      other bytes? if
        other self equal-bytes?
      else
        false
      then
    ;
    
  end-class

  \ Type class for cell sequences
  cells-type begin-type-class

    \ Detect whether a cell sequence is a list, and if so, get its length
    :private test-list { self -- len list? }
      0 { len }
      begin
        self 0= if len true exit then
        self cells? not if 0 false exit then
        self >len 2 <> if 0 false exit then
        1 self @+ to self
        1 +to len
      again
    ;
    
    \ Show a cell sequence
    :method show { self -- }
      self test-list if { len }
        len 2 + make-cells { seq }
        s" #[" 0 seq !+
        len 0 ?do
          0 self @+ try-show i 1+ seq !+ 1 self @+ to self
        loop
        s" ]#" len 1+ seq !+
        seq s"  " join
      else
        drop self >len { len }
        len 2 + make-cells { seq }
        s" #(" 0 seq !+
        self seq 1 [: { element index seq } element try-show index 1+ seq !+ ;]
        bind iteri
        s" )#" len 1+ seq !+
        seq s"  " join
      then
    ;

    \ Hash a cell sequence
    :method hash { self -- }
      self test-list if { len }
        0 len 0 ?do
          0 self @+ try-hash xor dup 5 lshift swap 27 rshift or
          1 self @+ to self
        loop
      else
        drop 0 self [: try-hash xor dup 5 lshift swap 27 rshift or ;] foldl
      then
    ;

    \ Test a cells equence for equality
    :method equal? { other self -- equal? }
      other cells? if
        self test-list if { self-len }
          other test-list if { other-len }
            self-len other-len = if
              self-len 0 ?do
                0 other @+ 0 self @+ try-equal? not if false exit then
                1 other @+ to other 1 self @+ to self
              loop
              true
            else
              false
            then
          else
            drop false
          then
        else
          drop self >len { len }
          other >len len = if
            len 0 ?do
              i other @+ i self @+ try-equal? not if false exit then
            loop
            true
          else
            false
          then
        then
      else
        false
      then
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
    
    \ Hash a slice
    :method hash { self -- }
      self >raw >type cells-type = if
        0 self [: try-hash xor dup 5 lshift swap 27 rshift or ;] foldl
      else
        self hash-bytes
      then
    ;

    \ Test a slice for equality
    :method equal? { other self -- equal? }
      self cells? other cells? and if
        self >len { len }
        other >len len = if
          len 0 ?do
            i other @+ i self @+ try-equal? not if false exit then
          loop
          true
        else
          false
        then
      else
        self bytes? other bytes? and if
          other self equal-bytes?
        else
          false
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

    \ Hash an xt
    :method hash { self -- } self unsafe::xt>integral ;

    \ Test an xt for equality
    :method equal? { other self -- equal? }
      other >type xt-type = if
        other self =
      else
        other >type closure-type = if
          other zscript-internal::>size unsafe::>integral
          2 cells - 2 rshift 0= if
            other unsafe::>integral cell+ unsafe::@ unsafe::integral>
            self unsafe::xt>integral =
          else
            false
          then
        else
          false
        then
      then
    ;
    
  end-class

  \ Type class for closures
  closure-type begin-type-class
    
    \ Show a closure
    :method show { self -- }
      self zscript-internal::>size unsafe::>integral
      2 cells - 2 rshift { bound }
      bound 2 * 4 + make-cells { seq }
      s" closure:" 0 seq !+
      self unsafe::>integral cell+ unsafe::@ unsafe::integral>
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

    \ Hash a closure
    :method hash { self -- }
      self zscript-internal::>size unsafe::>integral
      2 cells - 2 rshift { bound }
      self unsafe::>integral cell+ unsafe::@ unsafe::integral> { my-hash }
      bound 0 ?do
        self unsafe::>integral bound 1- i - 2 + cells + unsafe::@
        unsafe::integral> try-hash my-hash xor dup 5 lshift swap 27 rshift or
        to my-hash
      loop
      my-hash
    ;

    \ Test a closure for equality
    :method equal? { other self -- equal? }
      self zscript-internal::>size unsafe::>integral
      2 cells - 2 rshift { bound }
      self unsafe::>integral cell+ unsafe::@ unsafe::integral> { my-xt }
      other >type xt-type = if
        bound 0= my-xt other unsafe::xt>integral = and
      else
        other >type closure-type = if
          other zscript-internal::>size unsafe::>integral
          2 cells - 2 rshift bound = if
            other unsafe::>integral cell+ unsafe::@ unsafe::integral> my-xt = if
              bound 0 ?do
                self unsafe::>integral bound 1- i - 2 + cells + unsafe::@
                other unsafe::>integral bound 1- i - 2 + cells + unsafe::@
                unsafe::2integral> try-equal? not if false exit then
              loop
              true
            else
              false
            then
          else
            false
          then
        else
          false
        then
      then
    ;

  end-class

  \ Type class for tagged values
  tagged-type begin-type-class

    \ Show a tagged value
    :method show { self -- }
      self zscript-internal::>tag dup { tag } zscript-internal::word-tag = if
        s" word:"
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

    \ Hash a tagged value
    :method hash { self -- }
      self zscript-internal::>tag { my-hash }
      self zscript-internal::>size unsafe::>integral
      2 cells - 2 rshift { bound }
      bound 0 ?do
        i self zscript-internal::t@+ try-hash my-hash xor
        dup 5 lshift swap 27 rshift or to my-hash
      loop
      my-hash
    ;

    \ Test a tagged value for equality
    :method equal? { other self -- equal? }
      other >type tagged-type = if
        self zscript-internal::>tag other zscript-internal::>tag = if
          self zscript-internal::>size unsafe::>integral
          2 cells - 2 rshift { bound }
          other zscript-internal::>size unsafe::>integral
          2 cells - 2 rshift bound = if
            bound 0 ?do
              i other zscript-internal::t@+ i self zscript-internal::t@+ <> if
                false exit
              then
            loop
            true
          else
            false
          then
        else
          false
        then
      else
        false
      then
    ;
    
  end-class

  \ Type class for classes
  class-type begin-type-class

    \ Show a class
    :method show { self -- }
      s" class:" self unsafe::>integral format-integral concat
    ;

    \ Hash a class
    :method hash { self -- } self unsafe::>integral ;

    \ Test a class for equality
    :method equal? { other self -- equal? }
      other >type class-type = if
        other self =
      else
        false
      then
    ;
    
  end-class
  
end-module
