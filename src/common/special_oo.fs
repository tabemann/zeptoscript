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
  zscript-array import
  zscript-map import
  zscript-set import
  
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
    :method show { self -- bytes } self format-integral ;

    \ Hash a null
    :method hash { self -- hash } 0 ;

    \ Test a null for equality
    :method equal? { other self -- equal? } other 0= ;
    
  end-class

  \ Type class for small ints
  int-type begin-type-class

    \ Show a small int
    :method show { self -- bytes } self format-integral ;

    \ Hash a small int
    :method hash { self -- hash } self ;

    \ Test a small int for equality
    :method equal? { other self -- equal? } other self = ;
    
  end-class

  \ Type class for big ints
  big-int-type begin-type-class

    \ Show a big int
    :method show { self -- bytes } self format-integral ;

    \ Hash a big int
    :method hash { self -- hash } self ;

    \ Test a big int for equality
    :method equal? { other self -- equal? } other self = ;
    
  end-class

  \ Type class for symbols
  symbol-type begin-type-class

    \ Show a symbol
    :method show { self -- bytes } self symbol>name ;

    \ Hash a symbol
    :method hash { self -- hash } self symbol>integral ;

    \ Test a symbol for equality
    :method equal? { other self -- equal? } other self = ;
    
  end-class

  defined? zscript-double [if]
    
    \ Type class for doubles
    double-type begin-type-class
      
      \ Show a double
      :method show { self -- bytes }
        self zscript-double::format-double s" ." concat
      ;

      \ Hash a double
      :method hash { self -- hash } self double>2integral xor ;

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
      :method hash { self -- hash } self double>2integral xor ;
      
    end-class

  [then]

  \ Type class for byte sequences
  bytes-type begin-type-class

    \ Show a byte sequence
    :method show { self -- bytes }
      self >len { len }
      len 2 + make-cells { seq }
      s" #<" 0 seq !+
      self seq 1 [: { byte index seq } byte try-show index 1+ seq !+ ;]
      bind iteri
      s" >#" len 1+ seq !+
      seq s"  " join
    ;

    \ Hash a byte sequence
    :method hash { self -- hash } self hash-bytes ;

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
    :method show { self -- bytes }
      self >len { len }
      len 2 + make-cells { seq }
      s" #const<" 0 seq !+
      self seq 1 [: { byte index seq } byte try-show index 1+ seq !+ ;]
      bind iteri
      s" >#" len 1+ seq !+
      seq s"  " join
    ;

    \ Hash a constant byte sequence
    :method hash { self -- hash } self hash-bytes ;
    
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
    :method show { self -- bytes }
      self cell-array? if
        self array-seq@ to self
        self >len { len }
        len 2 + make-cells { seq }
        s" #!" 0 seq !+
        self seq 1 [: { element index seq } element try-show index 1+ seq !+ ;]
        bind iteri
        s" !#" len 1+ seq !+
        seq s"  " join
        exit
      then
      self byte-array? if
        self array-seq@ to self
        self >len { len }
        len 2 + make-cells { seq }
        s" #$" 0 seq !+
        self seq 1 [: { byte index seq } byte try-show index 1+ seq !+ ;]
        bind iteri
        s" $#" len 1+ seq !+
        seq s"  " join
        exit
      then
      self map? if
        self map-entry-count@ { len }
        len 2 * 2 + make-cells { seq }
        s" #{" 0 seq !+
        0 ref { index }
        self index seq 2 [: { val key index seq }
          key try-show index ref@ 2 * 1+ seq !+
          val try-show index ref@ 2 * 2 + seq !+
          index ref@ 1+ index ref!
        ;] bind iter-map
        s" }#" len 2 * 1+ seq !+
        seq s"  " join
        exit
      then
      self set? if
        self set-entry-count@ { len }
        len 2 + make-cells { seq }
        s" #|" 0 seq !+
        0 ref { index }
        self index seq 2 [: { val index seq }
          val try-show index ref@ 1+ seq !+
          index ref@ 1+ index ref!
        ;] bind iter-set
        s" |#" len 1+ seq !+
        seq s"  " join
        exit
      then
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
    :method hash { self -- hash }
      self cell-array? if
        self array-seq@ hash exit
      then
      self byte-array? if
        self array-seq@ hash exit
      then
      self map? if
        0 exit
      then
      self set? if
        0 exit
      then
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
      self cell-array? if
        other cell-array? if
          other array-seq@ self array-seq@ equal?
        else
          false
        then
        exit
      then
      self byte-array? if
        other byte-array? if
          other array-seq@ self array-seq@ equal?
        else
          false
        then
        exit
      then
      self map? if
        false exit
      then
      other map? if
        false exit
      then
      self set? if
        false exit
      then
      other set? if
        false exit
      then
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
    :method show { self -- bytes }
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
    :method hash { self -- hash }
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
    :method show { self -- bytes }
      s" xt:"
      self 16 [: unsafe::xt>integral format-integral-unsigned ;] with-base
      concat
    ;

    \ Hash an xt
    :method hash { self -- hash } self unsafe::xt>integral ;

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
    :method show { self -- bytes }
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
    :method hash { self -- hash }
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
    :method show { self -- bytes }
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
    :method hash { self -- hash }
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
    :method show { self -- bytes }
      s" class:" self unsafe::>integral format-integral concat
    ;

    \ Hash a class
    :method hash { self -- hash } self unsafe::>integral ;

    \ Test a class for equality
    :method equal? { other self -- equal? }
      other >type class-type = if
        other self =
      else
        false
      then
    ;
    
  end-class

  \ Type class for references
  ref-type begin-type-class

    \ Show a reference
    :method show { self -- bytes }
      s" ref:(" self ref@ try-show s" )" 3 >cells 0bytes join
    ;

    \ Hash a reference
    :method hash { self -- hash }
      self ref@ try-hash 1+
    ;

    \ Test a reference for equality
    :method equal? { other self -- equal? }
      other >type ref-type = if
        other ref@ self ref@ try-equal?
      else
        false
      then
    ;
    
  end-class

  \ Type class for saved states
  save-type begin-type-class

    \ Show a saved state
    :method show { self -- bytes }
      self unsafe::>integral cell+ unsafe::@ unsafe::integral> { stack-count }
      self unsafe::>integral 2 cells + unsafe::@ unsafe::integral>
      { rstack-count }
      stack-count rstack-count + 2* 3 + make-cells { seq }
      s" save:stack:(" 0 seq !+
      stack-count 0 ?do
        self unsafe::>integral 4 i + cells + unsafe::@
        i 2* 1+ seq 16 [: rot format-integral -rot !+ ;] with-base
        i stack-count 1- <> if
          s"  " i 2* 2 + seq !+
        else
          s" ),rstack:(" i 2* 2 + seq !+
        then
      loop
      rstack-count 0 ?do
        self unsafe::>integral 4 stack-count + i + cells + unsafe::@
        i stack-count + 2* 1+ seq 16 [: rot format-integral -rot !+ ;] with-base
        i rstack-count 1- <> if
          s"  " i stack-count + 2* 2 + seq !+
        else
          s" ),handler:(" i stack-count + 2* 2 + seq !+
        then
      loop
      self unsafe::>integral 3 cells + unsafe::@ unsafe::integral> { handler }
      handler stack-count rstack-count + 2* 1+ seq
      16 [: rot format-integral -rot !+ ;] with-base
      s" )" stack-count rstack-count + 2* 2 + seq !+
      seq 0bytes join
    ;

    \ Hash a saved state - note that saved states cannot actually be hashed
    \ for reasons, so this just returns a placeholder value
    :method hash { self -- hash }
      1
    ;

    \ Test two saved states for equality
    :method equal? { other self -- }
      other >type save-type = averts x-incorrect-type
      self unsafe::>integral cell+ unsafe::@ unsafe::integral>
      { self-stack-count }
      self unsafe::>integral 2 cells + unsafe::@ unsafe::integral>
      { self-rstack-count }
      self unsafe::>integral 3 cells + unsafe::@ unsafe::integral>
      { self-handler }
      other unsafe::>integral cell+ unsafe::@ unsafe::integral>
      { other-stack-count }
      other unsafe::>integral 2 cells + unsafe::@ unsafe::integral>
      { other-rstack-count }
      other unsafe::>integral 3 cells + unsafe::@ unsafe::integral>
      { other-handler }
      self-stack-count other-stack-count =
      self-rstack-count other-rstack-count = and
      self-handler other-handler = and if
        self-stack-count self-rstack-count + 4 + 4 ?do
          self unsafe::>integral i cells + unsafe::@
          other unsafe::>integral i cells + unsafe::@ <> if false exit then
        loop
        true
      else
        false
      then
    ;
      
  end-class
  
  \ Type classes for forced thunks
  force-type begin-type-class
    
    \ Show a forced thunk
    :method show { self -- bytes }
      3 make-cells { seq }
      s" force:(" 0 seq !+
      self unsafe::>integral cell+ unsafe::@ unsafe::integral> try-show 1 seq !+
      s" )" 2 seq !+
      seq 0bytes join
    ;

    \ Hash a forced thunk
    :method hash { self -- hash }
      self unsafe::>integral cell+ unsafe::@ unsafe::integral> try-hash 1+
    ;

    \ Test two force thunks for equality
    :method equal? { other self -- equal? }
      other >type force-type = if
        other unsafe::>integral cell+ unsafe::@ unsafe::integral>
        self unsafe::>integral cell+ unsafe::@ unsafe::integral> try-equal?
      else
        false
      then
    ;
    
  end-class

  defined? zscript-map [if]

    continue-module zscript-map

      continue-module zscript-map-internal
  
        \ Define a map
        symbol define-map
        
      end-module> import
      
      \ Incorrect number of items for map exception
      : x-incorrect-item-count ( -- ) ." incorrect item count for map" cr ;
      
      \ Define a generic map
      : >generic-map ( keyn valn ... key0 val0 ) { count -- map }
        count ['] hash ['] equal? make-map { map }
        count 0 ?do swap map insert-map loop
        map
      ;
    
      \ Begin defining a generic map
      : #{ ( -- ) define-map zscript-internal::begin-seq-define ;
      
      \ End defining a generic map
      : }# ( keyn valn ... key0 val0 -- map )
        define-map zscript-internal::end-seq-define
        dup 1 and 0= averts x-incorrect-item-count
        1 rshift >generic-map
      ;
      
    end-module
    
  [then]

  defined? zscript-set [if]

    continue-module zscript-set
      
      continue-module zscript-set-internal
        
        \ Define a set
        symbol define-set
        
      end-module> import
    
      \ Define a generic set
      : >generic-set ( valn ... val0 ) { count -- set }
        count ['] hash ['] equal? make-set { set }
        count 0 ?do set insert-set loop
        set
      ;
    
      \ Begin defining a generic set
      : #| ( -- ) define-set zscript-internal::begin-seq-define ;
      
      \ End defining a generic set
      : |# ( valn ... val0 -- set )
        define-set zscript-internal::end-seq-define >generic-set
      ;

    end-module
    
  [then]

end-module
