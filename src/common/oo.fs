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

  zscript import
  armv6m import

  \ Types
  8 constant class-type
  13 constant object-type

  \ Member not for class exception
  : x-member-not-for-class ( -- ) ." member not for class" cr ;

  \ Unimplemented method exception
  : x-unimplemented-method ( -- ) ." unimplemented method" cr ;

  \ Word is not a methtod
  : x-not-a-method ( -- ) ." not a method" cr ;
  
  begin-module zscript-oo-internal

    \ Class syntax
    254 constant syntax-class

    \ Push syntax
    1 0 foreign internal::push-syntax push-syntax

    \ Verify syntax
    1 0 foreign internal::verify-syntax verify-syntax

    \ Write a cell to the current dictionary
    2 0 foreign forth::current! current!

    \ Write a word to the dictionary
    1 0 foreign forth::, ,

    \ Write a halfword to the dictionary
    1 0 foreign forth::h, h,

    \ Align the dictionary
    1 0 foreign forth::align, align,

    \ Get the current wordlist
    0 1 foreign forth::get-current get-current

    \ Set the current wordlist
    1 0 foreign forth::set-current set-current

    \ The type shift
    foreign-constant zscript-internal::type-shift type-shift
    
    \ Method record
    begin-record method-record

      \ Method id
      item: method-id

      \ Method code address
      item: method-code
      
    end-record

    \ Member record
    begin-record member-record

      \ Member index
      item: member-index

      \ Member name
      item: member-name

    end-record
    
    \ Class building record
    begin-record class-record

      \ Class name
      item: class-name
      
      \ Class address
      item: class-addr
      
      \ Method sequence
      item: class-methods
      
      \ Member sequence
      item: class-members
      
    end-record

    \ Add a method to a class
    : add-method { method-rec class-rec -- }
      class-rec class-methods@ method-rec 1 >cells concat
      class-rec class-methods!
    ;
    
    \ Add a member to a class
    : add-member { member-rec class-rec -- }
      class-rec class-members@ member-rec 1 >cells concat
      class-rec class-members!
    ;

    \ Look up a member
    : lookup-member ( object class-addr member-index -- addr )
      rot
      code[
      r0 1 unsafe::integral> dp ldm
      0 r0 r1 ldr_,[_,#_]
      object-type 2 - unsafe::integral> r1 cmp_,#_
      ne bc>
      4 unsafe::integral> r0 r1 ldr_,[_,#_]
      r1 tos cmp_,_
      ne bc>
      r1 1 unsafe::integral> dp ldm
      r1 r0 tos adds_,_,_
      pc 1 unsafe::integral> pop
      >mark
      0 tos movs_,#_
      b>
      2swap
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
      1 unsafe::integral> r0 movs_,#_
      r0 tos tst_,_
      ne bc>
      0 tos r0 ldr_,[_,#_]
      type-shift unsafe::integral> r0 r0 lsrs_,_,#_
      object-type 2 - unsafe::integral> r0 cmp_,#_
      ne bc>
      cell unsafe::integral> tos tos ldr_,[_,#_]
      pc 1 unsafe::integral> pop
      >mark
      >mark
      >mark
      ]code
      ['] x-incorrect-type ?raise
    ;

    \ Look up and execute a method
    : execute-method ( ? class method-id -- ? class )
      code[
      r0 1 unsafe::integral> dp ldm
      0 r0 r1 ldr_,[_,#_]
      32 type-shift - unsafe::integral> r1 r1 lsls_,_,#_
      32 type-shift - 1+ unsafe::integral> r1 r1 lsrs_,_,#_
      cell 1+ unsafe::integral> r1 subs_,#_
      cell unsafe::integral> r0 adds_,#_
      3 unsafe::integral> tos r2 lsls_,_,#_
      mark>
      r1 r2 ands_,_
      r0 r2 r3 ldr_,[_,#_]
      tos r3 cmp_,_
      ne bc>
      cell unsafe::integral> r2 adds_,#_
      r0 r2 r3 ldr_,[_,#_]
      tos 1 unsafe::integral> dp ldm
      r3 bx_
      >mark
      0 r3 cmp_,#_
      eq bc>
      2 cells unsafe::integral> r2 adds_,#_
      2swap
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
      id raw-lit, postpone execute-method
      pop-pc h,
      cell align, method-id-marker , id , end-compile,
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

    \ The current flash method ID
    global flash-method-id

    \ Get a method ID
    : get-next-method-id ( -- id )
      compiling-to-flash? if
        flash-method-id@ if
        else
          s" *METHOD*" flash-latest find-all-dict dup if
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
        then
      else
        ram-method-id@ 1- dup ram-method-id!
      then
    ;

    \ Generate a member
    : generate-member { member-rec class-rec -- }
      member-rec member-name@ s" @" concat start-compile visible
      class-rec class-addr@ raw-lit, member-rec member-index@ cells raw-lit,
      postpone lookup-member
      postpone @
      end-compile,
      member-rec member-name@ s" !" concat start-compile visible
      class-rec class-addr@ unsafe::>integral raw-lit,
      member-rec member-index@ cells raw-lit,
      postpone lookup-member
      postpone !
      end-compile,
    ;

    \ Generate members
    : generate-members { class-rec -- }
      class-rec class-members@ class-rec 1 ['] generate-member bind iter
    ;

    \ Round up to the next power of two
    : round-to-pow2 { n -- }
      1 begin dup n < while 1 lshift repeat
    ;

    \ Generate a method
    : generate-method { method-rec class-table table-size -- }
      table-size 1- method-rec method-id@ and { method-index }
      begin method-index 2* cells class-table + unsafe::@ $FFFFFFFF <> while
        method-index 1+ table-size 1- and to method-index
      repeat
      method-index 2* cells class-table + { method-addr }
      method-rec method-id@ method-addr current!
      method-rec method-code@ 1 or method-addr cell+ current!
    ;

    \ Generate methods
    : generate-methods { class-rec -- }
      class-rec class-methods@ { methods }
      methods >len 1+ round-to-pow2 { table-size }
      unsafe::here { class-table }
      table-size 2* cells cell+ unsafe::allot
      class-type 2 - type-shift lshift
      table-size 2* cells cell+ 1 lshift or class-table current!
      compiling-to-flash? not if
        class-table cell+ table-size 2* cells $FF unsafe::fill
      then
      methods class-table cell+ table-size 2 ['] generate-method bind iter
      class-table cell+ dup table-size 2* cells + swap ?do
        i unsafe::@ $FFFFFFFF = if
          0 i current!
          0 i cell+ current!
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

    \ Does a class have a constructor?
    : has-constructor? { our-class -- }
      our-class unsafe::>integral to our-class
      ['] new unsafe::xt>integral get-method-id { id }
      our-class unsafe::@
      32 type-shift - lshift 32 type-shift - 1+ rshift { size }
      our-class size + our-class cell+ ?do
        i unsafe::@ id = if true exit then
      [ 2 cells ] literal +loop
      false
    ;

    \ Make an object for a class, without calling its constructor
    : make-object { member-count our-class -- our-object }
      member-count cells cell+ object-type zscript-internal::allocate-bytes
      our-class unsafe::>integral over unsafe::>integral cell+ unsafe::!
    ;

  end-module
  
  \ Define a class
  : begin-class ( "name" -- class-rec )
    token dup 0<> averts x-token-expected
    0 0cells 0cells >class-record
    syntax-class push-syntax
  ;

  \ Define a member
  : member: ( class-rec "name" -- class-rec )
    { class-rec }
    syntax-class verify-syntax
    token dup 0<> averts x-token-expected { name }
    class-rec class-members@ { members }
    members dup >len name >member-record 1 >cells concat
    class-rec class-members!
    class-rec
  ;

  \ Implement a method
  : :method ( class-rec "name" -- class-rec )
    { class-rec }
    syntax-class verify-syntax
    token-word word>xt { method-xt }
    :noname
    unsafe::>integral { code }
    class-rec class-methods@ { methods }
    methods method-xt unsafe::xt>integral get-method-id
    code >method-record 1 >cells concat
    class-rec class-methods!
    class-rec
  ;

  \ Finish defining a class
  : end-class { class-rec -- }
    syntax-class verify-syntax internal::drop-syntax
    class-rec generate-methods
    class-rec generate-members
    class-rec class-addr@ unsafe::integral> { our-class }
    s" make-" class-rec class-name@ concat { make-name }
    make-name start-compile visible
    class-rec class-members@ >len lit,
    our-class unsafe::>integral raw-lit,
    postpone make-object
    our-class has-constructor? if
      postpone dup
      postpone >r
      postpone new
      postpone r>
    then
    end-compile,
  ;
  
end-module
