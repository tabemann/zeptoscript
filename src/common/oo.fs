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

begin-module zscript-oo

  zscript-armv6m import

  \ Types
  9 constant class-type
  17 constant object-type

  \ Cannot define a member for a type class
  : x-type-classes-have-no-members ( -- ) ." type classe have no members" cr ;
  
  \ Member not for class exception
  : x-member-not-for-class ( -- ) ." member not for class" cr ;

  \ Unimplemented method exception
  : x-unimplemented-method ( -- ) ." unimplemented method" cr ;

  \ Word is not a methtod
  : x-not-a-method ( -- ) ." not a method" cr ;
  
  begin-module zscript-oo-internal

    \ The class wordlist stack
    global class-wordlist-stack

    \ Class syntax
    253 constant syntax-class

    \ Push syntax
    1 0 foreign internal::push-syntax push-syntax

    \ Verify syntax
    1 0 foreign internal::verify-syntax verify-syntax

    \ Get the current wordlist
    0 1 foreign forth::get-current get-current

    \ Set the current wordlist
    1 0 foreign forth::set-current set-current

    \ The type shift
    foreign-constant zscript-internal::type-shift type-shift
    
    \ The type class table for non-objects
    global type-classes
    
    \ Method record
    begin-record method-record

      \ Method id
      item: method-id

      \ Method code address
      item: method-code
      
    end-record

    \ Class building record
    begin-record class-record

      \ Class constructor address
      item: class-construct
      
      \ Class address
      item: class-addr

      \ Class ID
      item: class-id
      
      \ Method sequence
      item: class-methods
      
      \ Member count
      item: class-member-count

      \ Class address
      item: class-addr

      \ Class type
      item: class-type
      
    end-record
    
    \ Hash a methodID
    : hash-method { id -- hash }
      0 31 0 ?do id i lshift id 32 i - rshift or xor loop
      dup $FFFFFFFF = if drop 1 then
    ;
    
    \ Look up a member
    : lookup-member ( object member-offset class-id -- addr )
      rot
      code[
      0 tos r0 ldr_,[_,#_]
      type-shift r0 r0 lsrs_,_,#_
      object-type 2 - r0 cmp_,#_
      ne bc>
      r0 1 dp ldm
      cell tos r1 ldr_,[_,#_]
      cell r1 r1 ldr_,[_,#_]
      r1 r0 cmp_,_
      ne bc>
      r1 1 dp ldm
      r1 tos tos adds_,_,_
      pc 1 pop
      >mark
      0 tos movs_,#_
      b>
      swap
      >mark
      0 tos movs_,#_
      tos tos mvns_,_
      >mark
      ]code
      if ['] x-incorrect-type else ['] x-member-not-for-class then
      ?raise
    ;

    \ Prepare for calling a method
    : prepare-method ( object -- class )
      code[
      0 tos cmp_,#_
      eq bc>
      1 r0 movs_,#_
      r0 tos tst_,_
      ne bc>
      0 tos r0 ldr_,[_,#_]
      type-shift r0 r0 lsrs_,_,#_
      object-type 2 - r0 cmp_,#_
      ne bc>
      cell tos tos ldr_,[_,#_]
      pc 1 pop
      >mark
      >mark
      >mark
      ]code
      type-classes@ { classes }
      classes 0= if
        ['] x-incorrect-type ?raise
      else
        >type { type }
        classes >len type > if
          type classes @+
        else
          ['] x-incorrect-type ?raise
        then
      then
    ;

    \ Look up and execute a method
    : execute-method ( ? class method-hash method-id -- ? )
      code[
      r2 1 dp ldm
      r0 1 dp ldm
      0 r0 r1 ldr_,[_,#_]
      32 type-shift - r1 r1 lsls_,_,#_
      32 type-shift - 1+ r1 r1 lsrs_,_,#_
      2 cells 1+ r1 subs_,#_
      2 cells r0 adds_,#_
      3 r2 r2 lsls_,_,#_
      mark>
      r1 r2 ands_,_
      r0 r2 r3 ldr_,[_,_]
      tos r3 cmp_,_
      ne bc>
      cell r2 adds_,#_
      r0 r2 r3 ldr_,[_,_]
      tos 1 dp ldm
      r3 bx_
      >mark
      0 r3 cmp_,#_
      eq bc>
      2 cells r2 adds_,#_
      swap
      b<
      >mark
      ]code
      ['] x-unimplemented-method ?raise
    ;

    \ Method ID marker
    $C0DEDEAD constant method-id-marker

    \ POP {PC}
    $BD00 constant pop-pc

    \ BX LR
    $4770 constant bx-lr

    \ Compile a method
    : compile-method { id name -- }
      name start-compile visible
      postpone dup
      postpone prepare-method
      id hash-method raw-lit, id raw-lit, postpone execute-method
      pop-pc unsafe::h,
      cell unsafe::align, method-id-marker unsafe::, id unsafe::, end-compile,
    ;

    \ Get a method ID
    : get-method-id ( addr -- id )
      begin
        dup unsafe::h@ dup pop-pc = swap bx-lr = or swap 2 + swap
      until
      cell align
      dup unsafe::@ method-id-marker = averts x-not-a-method
      cell+ unsafe::@
    ;

    \ The current RAM method ID
    global ram-method-id

    \ Get a method ID
    : get-next-method-id ( -- id )
      compiling-to-flash? if
        s" *METHOD*" flash-latest find-all-dict ?dup if
          word>xt execute
        else
          0
        then
        1+ dup
        get-current
        swap
        forth::internal unsafe::>integral set-current
        s" *METHOD*" constant-with-name
        set-current
      else
        ram-method-id@
        dup 0 = if drop -1 then
        1- dup ram-method-id!
      then
    ;

    \ The current RAM class ID
    global ram-class-id

    \ Get a class ID
    : get-next-class-id ( -- id )
      compiling-to-flash? if
        s" *CLASS*" flash-latest find-all-dict ?dup if
          word>xt execute
        else
          0
        then
        1+ dup
        get-current
        swap
        forth::internal unsafe::>integral set-current
        s" *CLASS*" constant-with-name
        set-current
      else
        ram-class-id@
        dup 0 = if drop -1 then
        1- dup ram-class-id!
      then
    ;

    \ Generate a member
    : generate-member { member-name class-rec -- }
      forth::get-current zscript-internal::make-new-style { old-current }
      0 class-wordlist-stack@ @+
      zscript-internal::filter-new-style-flag forth::set-current
      class-rec class-id@ { id }
      class-rec class-member-count@ { member-count }
      member-name s" @" concat start-compile visible
      member-count 2 + cells raw-lit, id raw-lit,
      postpone lookup-member
      postpone forth::@
      end-compile,
      member-name s" !" concat start-compile visible
      member-count 2 + cells raw-lit, id raw-lit,
      postpone lookup-member
      postpone forth::!
      end-compile,
      member-count 1+ class-rec class-member-count!
      old-current zscript-internal::filter-new-style-flag forth::set-current
    ;

    \ Round up to the next power of two
    : round-to-pow2 { n -- }
      1 begin dup n < while 1 lshift repeat
    ;

    \ Generate a method
    : generate-method { method-rec class-table table-size -- }
      table-size 1- method-rec method-id@ hash-method and { method-index }
      begin method-index 2* cells class-table + unsafe::@ $FFFFFFFF <> while
        method-index 1+ table-size 1- and to method-index
      repeat
      method-index 2* cells class-table + { method-addr }
      method-rec method-id@ method-addr unsafe::current!
      method-rec method-code@ 1 or method-addr cell+ unsafe::current!
    ;

    \ Generate methods
    : generate-methods { class-rec -- }
      class-rec class-methods@ { methods }
      methods >len 1+ 2 * round-to-pow2 { table-size }
      unsafe::here { class-table }
      table-size 2* cells [ 2 cells ] literal + unsafe::allot
      class-type 2 - type-shift lshift
      table-size 2* cells [ 2 cells ] literal + 1 lshift or class-table
      unsafe::current!
      class-rec class-id@ class-table cell+ unsafe::current!
      compiling-to-flash? not if
        class-table [ 2 cells ] literal + table-size 2* cells $FF unsafe::fill
      then
      methods class-table [ 2 cells ] literal + table-size
      2 ['] generate-method bind iter
      class-table [ 2 cells ] literal + dup table-size 2* cells + swap ?do
        i unsafe::@ $FFFFFFFF = if
          0 i unsafe::current!
          0 i cell+ unsafe::current!
        then
      [ 2 cells ] literal +loop
      class-table class-rec class-addr!
    ;

  end-module> import

  \ Define a method
  : method ( "name" -- )
    get-next-method-id token dup 0<> averts x-token-expected compile-method
  ;

  \ The new method
  method new

  continue-module zscript-oo-internal

    \ Does a class have a method?
    : class-has-method? { xt our-class -- }
      our-class unsafe::>integral to our-class
      xt unsafe::xt>integral get-method-id { id }
      our-class unsafe::@
      32 type-shift - lshift 32 type-shift - 1+ rshift { size }
      our-class size + our-class [ 2 cells ] literal + ?do
        i unsafe::@ id = if true exit then
      [ 2 cells ] literal +loop
      false
    ;

    \ Make an object for a class, without calling its constructor
    : make-object { member-count our-class -- our-object }
      member-count cells cell+ object-type zscript-internal::allocate-bytes
      our-class unsafe::>integral over unsafe::>integral cell+ unsafe::!
    ;

    \ Add a type class
    : add-type-class { our-class type -- }
      type-classes@ dup { classes } 0= if
        type 1+ make-cells dup type-classes! to classes
      else
        classes >len { len }
        type len >= if
          type 1+ make-cells type-classes!
          classes 0 type-classes@ 0 len copy
          type-classes@ to classes
        then
      then
      our-class type classes !+
    ;

  end-module
  
  \ Define a class
  : begin-class ( "name" -- class-rec )
    token dup 0<> averts x-token-expected
    s" make-" swap concat start-compile visible
    [ zscript-armv6m-instr import ]
    r0 ldr_,[pc]
    r0 blx_
    pc 1 pop
    cell unsafe::align,
    >mark
    unsafe::reserve
    [ zscript-armv6m-instr unimport ]
    end-compile,
    0 get-next-class-id 0cells 0 0 -1 >class-record
    syntax-class push-syntax
    forth::wordlist zscript-internal::make-new-style
    dup class-wordlist-stack@ >pair class-wordlist-stack!
    import
  ;

  \ Define a class for a type
  : begin-type-class { type -- class-rec }
    0 0 get-next-class-id 0cells 0 0 type >class-record
    syntax-class push-syntax
    forth::wordlist zscript-internal::make-new-style
    dup class-wordlist-stack@ >pair class-wordlist-stack!
    import
  ;

  \ Define a member
  : member: ( class-rec "name" -- class-rec )
    { class-rec }
    class-rec class-type@ -1 = averts x-type-classes-have-no-members
    syntax-class verify-syntax
    token dup 0<> averts x-token-expected
    class-rec generate-member
    class-rec
  ;

  \ Implement a method
  : :method ( class-rec "name" -- class-rec )
    { class-rec }
    syntax-class verify-syntax
    token-word word>xt { method-xt }
    forth:::noname
    unsafe::>integral { code-xt }
    class-rec class-methods@ { methods }
    methods method-xt unsafe::xt>integral get-method-id
    code-xt >method-record 1 >cells concat
    class-rec class-methods!
    class-rec
  ;

  \ Implement a private word
  : :private ( "name" -- )
    syntax-class verify-syntax
    forth::get-current zscript-internal::make-new-style { old-current }
    0 class-wordlist-stack@ @+
    zscript-internal::filter-new-style-flag forth::set-current
    :
    old-current zscript-internal::filter-new-style-flag forth::set-current
  ;

  \ Implement a class member
  : class-member: ( "name" -- )
    syntax-class verify-syntax
    forth::get-current zscript-internal::make-new-style { old-current }
    0 class-wordlist-stack@ @+
    zscript-internal::filter-new-style-flag forth::set-current
    global
    old-current zscript-internal::filter-new-style-flag forth::set-current
  ;

  \ Finish defining a class
  : end-class { class-rec -- }
    syntax-class verify-syntax internal::drop-syntax
    class-wordlist-stack@ pair> swap unimport class-wordlist-stack!
    class-rec generate-methods
    class-rec class-addr@ unsafe::integral> { our-class }
    class-rec class-type@ dup { type } -1 = if
      forth:::noname unsafe::>integral
      1 or class-rec class-construct@ unsafe::current!
      class-rec class-member-count@ lit,
      our-class unsafe::>integral raw-lit,
      postpone make-object
      ['] new our-class class-has-method? if
        postpone dup
        postpone forth::>r
        postpone new
        postpone forth::r>
      then
      postpone ;
    else
      compiling-to-flash? if
        s" init" flash-latest find-all-dict
        get-current swap
        forth::forth unsafe::>integral set-current
        s" init" start-compile visible
        ?dup if word>xt compile, then
        our-class unsafe::>integral lit, postpone unsafe::integral>
        type lit, postpone add-type-class
        end-compile,
        set-current
      then
      our-class type add-type-class
    then
  ;

  \ Get whether an object has a method
  : has-method? { xt object -- has-method? }
    object >type { type }
    type object-type = if
      xt object unsafe::>integral cell+ unsafe::@
      unsafe::integral> class-has-method?
    else
      type-classes@ { classes }
      type classes >len < if
        xt type classes @+ class-has-method?
      else
        false
      then
    then
  ;

  \ Get an object's class
  : class@ { object -- class }
    object >type { type }
    type object-type = if
      object unsafe::>integral cell+ unsafe::@ unsafe::integral>
    else
      type-classes@ { classes }
      type classes >len < averts x-incorrect-type
      type classes @+
    then
  ;

end-module
